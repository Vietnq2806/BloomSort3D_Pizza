using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ==========================================
    // FSM STATE MACHINE (State Pattern - Tiêu chí nghiệm thu Tuần 2)
    // _fsmState: tham chiếu đến class trạng thái hiện tại, thay thế hoàn toàn enum đơn giản
    // ==========================================
    private IGameState _fsmState;
    private Dictionary<GameState, IGameState> _stateCache;

    /// <summary>Trả về kiểu enum của trạng thái hiện tại để các script bên ngoài nhận diện</summary>
    public GameState CurrentState => _fsmState?.StateType ?? GameState.Setup;

    [Header("Thông số Game")]
    public int score = 0;
    public int gold = 0;
    public int currentCombo = 0;
    public float comboDuration = 4.0f; // Thời gian duy trì combo liên hoàn (giây)

    private float _comboResetTimer = 0f;
    private GridManager _gridManager;
    private List<PizzaSpawner> _spawners = new List<PizzaSpawner>();

    // Bộ đếm số lượng animation đang chạy đồng thời (Merge + Bloom)
    private int _activeAnimationsCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Khởi tạo và cache sẵn các trạng thái của FSM để tránh sinh rác GC khi chuyển trạng thái
        _stateCache = new Dictionary<GameState, IGameState>
        {
            { GameState.Setup, new SetupState() },
            { GameState.Playing, new PlayingState() },
            { GameState.CheckingCombo, new CheckingComboState() },
            { GameState.Animating, new AnimatingState() },
            { GameState.GameOver, new GameOverState() }
        };

        // Load vàng từ SaveSystem JSON (SaveSystem.Awake chạy trước nhờ [DefaultExecutionOrder(-200)])
        gold = (SaveSystem.Instance != null) ? SaveSystem.Instance.Data.gold : PlayerPrefs.GetInt("PlayerGold", 100);
    }

    void Start()
    {
        _gridManager = FindFirstObjectByType<GridManager>();

        PizzaSpawner[] foundSpawners = FindObjectsByType<PizzaSpawner>(FindObjectsSortMode.None);
        _spawners.AddRange(foundSpawners);

        // Khởi động FSM từ trạng thái đầu tiên
        ChangeState(GameState.Setup);
    }

    void OnEnable()
    {
        GameEvents.OnPlatePlaced += HandlePlatePlaced;
    }

    void OnDisable()
    {
        GameEvents.OnPlatePlaced -= HandlePlatePlaced;
    }

    void Update()
    {
        // Uỷ quyền logic Update cho class trạng thái hiện tại (State Pattern)
        _fsmState?.Tick(this);
    }

    // ==========================================
    // CHUYỂN TRẠNG THÁI FSM (CỐT LÕI STATE MACHINE)
    // ==========================================
    public void ChangeState(GameState newState)
    {
        // Ngăn chặn chuyển sang trạng thái đang hiện hành (vô nghĩa)
        if (_fsmState?.StateType == newState) return;

        // Gọi Exit() trên state cũ để dọn dẹp
        _fsmState?.Exit(this);

        // Khởi tạo class state mới tương ứng
        _fsmState = CreateState(newState);

        Debug.Log($"[FSM] Chuyển trạng thái → {_fsmState.StateType}");
        GameEvents.TriggerGameStateChanged(_fsmState.StateType);

        // Gọi Enter() để state mới bắt đầu công việc của nó
        _fsmState.Enter(this);
    }

    private IGameState CreateState(GameState state)
    {
        if (_stateCache != null && _stateCache.TryGetValue(state, out var cachedState))
        {
            return cachedState;
        }

        switch (state)
        {
            case GameState.Setup:         return new SetupState();
            case GameState.Playing:       return new PlayingState();
            case GameState.CheckingCombo: return new CheckingComboState();
            case GameState.Animating:     return new AnimatingState();
            case GameState.GameOver:      return new GameOverState();
            default:                      return new PlayingState();
        }
    }

    // ==========================================
    // PUBLIC BRIDGE METHODS - ĐƯỢC GỌI BỞI CÁC STATE CLASS
    // Tạo ranh giới giao tiếp rõ ràng giữa State và GameManager
    // ==========================================

    /// <summary>Gọi bởi SetupState.Enter() → bắt đầu chuỗi khởi động game</summary>
    public void StartSetupRoutine()
    {
        StartCoroutine(SetupRoutine());
    }

    /// <summary>Gọi bởi CheckingComboState.Enter() → quét combo cascade toàn lưới</summary>
    public void StartCheckCascadeRoutine()
    {
        StartCoroutine(CheckComboCascadeRoutine());
    }

    /// <summary>Gọi bởi PlayingState.Tick() và AnimatingState.Tick() → quản lý đồng hồ combo</summary>
    public void TickComboTimer()
    {
        if (currentCombo <= 0) return;

        _comboResetTimer += Time.deltaTime;
        if (_comboResetTimer >= comboDuration)
        {
            currentCombo = 0;
            GameEvents.TriggerScoreChanged(score, currentCombo);
        }
    }

    // ==========================================
    // QUẢN LÝ SỐ LƯỢNG ANIMATION ĐANG CHẠY
    // ==========================================
    public void RegisterAnimationStart()
    {
        _activeAnimationsCount++;
        if (CurrentState != GameState.Animating)
        {
            ChangeState(GameState.Animating);
        }
    }

    public void RegisterAnimationEnd()
    {
        _activeAnimationsCount = Mathf.Max(0, _activeAnimationsCount - 1);

        if (_activeAnimationsCount == 0
            && CurrentState != GameState.Playing
            && CurrentState != GameState.GameOver
            && CurrentState != GameState.Setup)
        {
            // Nếu đang ở CheckingCombo → start trực tiếp thay vì gọi ChangeState (tránh guard chặn)
            // Nếu ở Animating → chuyển sang CheckingCombo để quét cascade tiếp
            if (CurrentState == GameState.CheckingCombo)
            {
                StartCheckCascadeRoutine();
            }
            else
            {
                ChangeState(GameState.CheckingCombo);
            }
        }
    }

    // ==========================================
    // XỬ LÝ SỰ KIỆN ĐẶT ĐĨA
    // ==========================================
    private void HandlePlatePlaced(Transform slot, Transform plate)
    {
        // Đặt đĩa mới reset combo tay, chuẩn bị đếm combo cascade mới
        currentCombo = 0;
        _comboResetTimer = 0f;

        // Chuyển sang CheckingCombo → CheckingComboState.Enter() tự gọi StartCheckCascadeRoutine()
        ChangeState(GameState.CheckingCombo);
    }

    // ==========================================
    // COROUTINE NỘI BỘ
    // ==========================================
    private IEnumerator SetupRoutine()
    {
        // Chờ 1 frame để Grid và Spawner hoàn tất Awake/Start
        yield return new WaitForSeconds(0.2f);
        ChangeState(GameState.Playing);
        GameEvents.TriggerGoldChanged(gold);
        GameEvents.TriggerScoreChanged(score, 0);
    }

    private IEnumerator CheckComboCascadeRoutine()
    {
        // Chờ ngắn để các Destroy() và SetPlateInSlot() trong frame trước hoàn tất
        yield return new WaitForSeconds(0.1f);

        bool anyMergeHappened = false;

        // Quét toàn bộ các ô lưới, tìm đĩa có thể merge với lân cận
        if (_gridManager != null)
        {
            foreach (var slot in _gridManager.generatedSlots)
            {
                Transform plateTransform = _gridManager.GetPlateInSlot(slot);
                if (plateTransform == null) continue;

                PizzaPlate plate = plateTransform.GetComponent<PizzaPlate>();
                if (plate == null || plate.IsMerging || plate.IsBloomed) continue;

                anyMergeHappened = plate.CheckAndMergeWithNeighbors();
                if (anyMergeHappened) break; // Chờ lượt merge kết thúc rồi quét lại
            }
        }

        // Nếu không còn animation nào đang chạy và không có merge → kiểm tra Game Over
        if (_activeAnimationsCount == 0 && !anyMergeHappened)
        {
            if (CheckGameOverCondition())
            {
                ChangeState(GameState.GameOver);
            }
            else
            {
                ChangeState(GameState.Playing);
            }
        }
    }

    // ==========================================
    // API ĐIỂM SỐ VÀ VÀNG
    // ==========================================
    public void AddScore(int points)
    {
        score += points;
        GameEvents.TriggerScoreChanged(score, currentCombo);
    }

    public void AddGold(int amount)
    {
        gold += amount;

        // Đồng bộ vào SaveSystem JSON (nguồn lưu trữ chính)
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Data.gold = gold;
            SaveSystem.Instance.Save();
        }
        else
        {
            // Fallback PlayerPrefs nếu SaveSystem chưa sẵn sàng
            PlayerPrefs.SetInt("PlayerGold", gold);
            PlayerPrefs.Save();
        }

        GameEvents.TriggerGoldChanged(gold);
    }

    public void IncrementCombo()
    {
        currentCombo++;
        _comboResetTimer = 0f;
        GameEvents.TriggerScoreChanged(score, currentCombo);
    }

    // ==========================================
    // KIỂM TRA ĐIỀU KIỆN GAME OVER 2 TẦNG
    // ==========================================
    private bool CheckGameOverCondition()
    {
        if (_gridManager == null)
        {
            Debug.LogWarning("[GameOver Check] _gridManager is null!");
            return false;
        }

        Debug.Log($"[GameOver Check] Bắt đầu quét lưới. Tổng số ô lưới: {_gridManager.generatedSlots.Count}");

        // Tầng 1: Còn ô lưới trống → chưa game over
        bool hasEmptySlot = false;
        foreach (var slot in _gridManager.generatedSlots)
        {
            Transform plate = _gridManager.GetPlateInSlot(slot);
            if (plate == null)
            {
                Debug.LogWarning($"[GameOver Check] Ô lưới {slot.name} đang bị trống (null plate)!");
                hasEmptySlot = true;
            }
            else
            {
                Debug.Log($"[GameOver Check] Ô lưới {slot.name} có đĩa: {plate.name}");
            }
        }
        if (hasEmptySlot)
        {
            Debug.Log("[GameOver Check] Kết quả: CHƯA GAME OVER vì vẫn còn ô lưới trống (hoặc có ô trống logic).");
            return false;
        }

        // Tầng 2: Lưới đầy 100% → kiểm tra còn cặp lân cận cùng vị có thể merge không
        float checkDistanceX = _gridManager.spacingX * 1.1f;
        float checkDistanceZ = _gridManager.spacingZ * 1.1f;
        Vector3[] crossDirections = new Vector3[]
        {
            new Vector3(checkDistanceX, 0, 0),
            new Vector3(0, 0, checkDistanceZ)
        };

        foreach (var slot in _gridManager.generatedSlots)
        {
            Transform plateA = _gridManager.GetPlateInSlot(slot);
            if (plateA == null) continue;

            PizzaPlate scriptA = plateA.GetComponent<PizzaPlate>();
            if (scriptA == null) continue;

            string flavorA = scriptA.GetTopSliceFlavor();
            if (string.IsNullOrEmpty(flavorA)) continue;

            foreach (Vector3 dir in crossDirections)
            {
                Vector3 neighborPos = slot.position + dir;
                foreach (var otherSlot in _gridManager.generatedSlots)
                {
                    float dist = Vector3.Distance(otherSlot.position, neighborPos);
                    if (dist < 0.2f)
                    {
                        Transform plateB = _gridManager.GetPlateInSlot(otherSlot);
                        if (plateB == null) continue;

                        PizzaPlate scriptB = plateB.GetComponent<PizzaPlate>();
                        if (scriptB == null) continue;

                        string flavorB = scriptB.GetTopSliceFlavor();
                        Debug.Log($"[GameOver Check] Kiểm tra lân cận: {slot.name} ({flavorA}) và {otherSlot.name} ({flavorB}). Khoảng cách: {dist}");

                        if (flavorA == flavorB)
                        {
                            int emptyA = scriptA.GetEmptySlotCount();
                            int emptyB = scriptB.GetEmptySlotCount();
                            Debug.Log($"[GameOver Check] Phát hiện cặp cùng vị trùng nhau: {slot.name} và {otherSlot.name}. Ô trống: A={emptyA}, B={emptyB}");
                            
                            // Còn cặp cùng vị và có ô trống để bay → chưa game over
                            if (emptyA > 0 || emptyB > 0)
                            {
                                Debug.Log($"[GameOver Check] Kết quả: CHƯA GAME OVER vì vẫn còn cặp trùng vị {slot.name} & {otherSlot.name} có thể gộp bánh.");
                                return false;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log("[GameOver Check] Kết quả: THỰC SỰ HẾT NƯỚC ĐI → GAME OVER!");
        return true;
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
