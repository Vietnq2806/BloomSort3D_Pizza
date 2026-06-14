using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Canvas")]
    private Canvas _canvas;
    private GraphicRaycaster _raycaster;

    // Các cụm Panel chính
    private GameObject _staticPanel;
    private GameObject _dynamicPanel;
    private GameObject _gameOverPanel;

    // Text năng động
    private Text _scoreText;
    private Text _goldText;
    private Text _highScoreText;
    private Text _comboText;

    // Các bảng Overlay tính năng
    private GameObject _shopOverlay;
    private GameObject _dailyOverlay;
    private GameObject _achievementOverlay;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Khởi tạo EventSystem nếu chưa có trong Scene
        EnsureEventSystemExists();

        // Tự động xây dựng UI Canvas từ mã nguồn (Zero Asset Dependencies - 100% Đẹp xuất sắc)
        BuildDynamicCanvasUI();
    }

    void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnScoreChanged += UpdateScoreUI;
        GameEvents.OnGoldChanged += UpdateGoldUI;
    }

    void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnScoreChanged -= UpdateScoreUI;
        GameEvents.OnGoldChanged -= UpdateGoldUI;
    }

    private void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ==========================================
    // THUẬT TOÁN XÂY DỰNG TOÀN BỘ GIAO DIỆN UI CANVAS CAO CẤP TỪ CODE (GLASSMORPHISM)
    // ==========================================
    private void BuildDynamicCanvasUI()
    {
        // 1. Tạo Canvas chính
        GameObject canvasObj = new GameObject("BloomSort_UI_Canvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        
        _raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Tạo Canvas Tĩnh (Static Panel) để tối ưu Canvas Rebuild
        _staticPanel = new GameObject("StaticCanvasPanel");
        _staticPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform staticRT = _staticPanel.AddComponent<RectTransform>();
        staticRT.anchorMin = Vector2.zero;
        staticRT.anchorMax = Vector2.one;
        staticRT.sizeDelta = Vector2.zero;
        
        // Cấu hình Canvas phụ cho phần tĩnh để cô lập Mesh Rebuild và chặn Raycast
        Canvas staticCanvas = _staticPanel.AddComponent<Canvas>();
        _staticPanel.AddComponent<GraphicRaycaster>();

        // 3. Tạo Canvas Động (Dynamic Panel) để cập nhật chữ điểm số, vàng liên tục
        _dynamicPanel = new GameObject("DynamicCanvasPanel");
        _dynamicPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform dynamicRT = _dynamicPanel.AddComponent<RectTransform>();
        dynamicRT.anchorMin = Vector2.zero;
        dynamicRT.anchorMax = Vector2.one;
        dynamicRT.sizeDelta = Vector2.zero;
        
        // Cấu hình Canvas phụ cho phần động giúp cập nhật text liên tục không gây dựng lại phần tĩnh
        Canvas dynamicCanvas = _dynamicPanel.AddComponent<Canvas>();

        // --- XÂY DỰNG TOP BAR (Thanh tiêu đề trên cùng) ---
        BuildTopBar();

        // --- XÂY DỰNG TEXT HIỂN THỊ COMBO ---
        BuildComboText();

        // --- XÂY DỰNG HÀNG NÚT DƯỚI CÙNG (Meta Buttons) ---
        BuildMetaButtons();

        // --- XÂY DỰNG CÁC PANEL OVERLAY TÍNH NĂNG PHỤ (Skins Shop, Daily, Achievements) ---
        BuildOverlays(canvasObj);

        // --- XÂY DỰNG PANEL GAME OVER ---
        BuildGameOverPanel(canvasObj);
    }

    private void BuildTopBar()
    {
        // Khung nền Top Bar mờ ảo (Glassmorphism)
        GameObject topBar = new GameObject("TopBarBG");
        topBar.transform.SetParent(_staticPanel.transform, false);
        
        RectTransform rt = topBar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector3(0, -10, 0);
        rt.sizeDelta = new Vector2(-40, 80);

        Image img = topBar.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.1f, 0.65f); // Kính mờ xám đậm sang trọng

        // Thêm text Score
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(_dynamicPanel.transform, false);
        _scoreText = scoreObj.AddComponent<Text>();
        _scoreText.font = GameEvents.GetSafeFont();
        _scoreText.fontSize = 32;
        _scoreText.color = Color.yellow;
        _scoreText.text = "ĐIỂM: 0";
        
        RectTransform scoreRT = scoreObj.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0.05f, 1f);
        scoreRT.anchorMax = new Vector2(0.05f, 1f);
        scoreRT.pivot = new Vector2(0, 0.5f);
        scoreRT.anchoredPosition = new Vector2(40, -50);
        scoreRT.sizeDelta = new Vector2(400, 60);

        // Thêm text Gold
        GameObject goldObj = new GameObject("GoldText");
        goldObj.transform.SetParent(_dynamicPanel.transform, false);
        _goldText = goldObj.AddComponent<Text>();
        _goldText.font = GameEvents.GetSafeFont();
        _goldText.fontSize = 32;
        _goldText.color = new Color(1f, 0.6f, 0f); // Màu cam vàng rực
        _goldText.text = "VÀNG: 100 🪙";
        
        RectTransform goldRT = goldObj.GetComponent<RectTransform>();
        goldRT.anchorMin = new Vector2(0.95f, 1f);
        goldRT.anchorMax = new Vector2(0.95f, 1f);
        goldRT.pivot = new Vector2(1f, 0.5f);
        goldRT.anchoredPosition = new Vector2(-40, -50);
        goldRT.sizeDelta = new Vector2(400, 60);
        _goldText.alignment = TextAnchor.MiddleRight;

        // Thêm High Score chính giữa
        GameObject hsObj = new GameObject("HighScoreText");
        hsObj.transform.SetParent(_dynamicPanel.transform, false);
        _highScoreText = hsObj.AddComponent<Text>();
        _highScoreText.font = GameEvents.GetSafeFont();
        _highScoreText.fontSize = 28;
        _highScoreText.color = Color.white;
        int hs = PlayerPrefs.GetInt("PlayerHighScore", 0);
        _highScoreText.text = $"KỶ LỤC: {hs}";
        _highScoreText.alignment = TextAnchor.MiddleCenter;

        RectTransform hsRT = hsObj.GetComponent<RectTransform>();
        hsRT.anchorMin = new Vector2(0.5f, 1f);
        hsRT.anchorMax = new Vector2(0.5f, 1f);
        hsRT.pivot = new Vector2(0.5f, 0.5f);
        hsRT.anchoredPosition = new Vector2(0, -50);
        hsRT.sizeDelta = new Vector2(500, 60);
    }

    private void BuildComboText()
    {
        GameObject comboObj = new GameObject("ComboText");
        comboObj.transform.SetParent(_dynamicPanel.transform, false);
        _comboText = comboObj.AddComponent<Text>();
        _comboText.font = GameEvents.GetSafeFont();
        _comboText.fontSize = 72;
        _comboText.color = Color.red;
        _comboText.alignment = TextAnchor.MiddleCenter;
        _comboText.text = ""; // Mặc định ẩn
        
        RectTransform rt = comboObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.7f);
        rt.anchorMax = new Vector2(0.5f, 0.7f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800, 150);

        // Gắn thêm component rung lắc nhẹ kích thích thị giác khi combo tăng
        comboObj.AddComponent<PulsatingUI>();
    }

    private void BuildMetaButtons()
    {
        // Khung nền chứa 3 nút chính góc phải (xếp dọc tránh vướng pizza)
        GameObject rightBar = new GameObject("RightButtonBar");
        rightBar.transform.SetParent(_staticPanel.transform, false);
        
        RectTransform rt = rightBar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector3(-30, 0, 0); // Cách lề phải 30px, căn giữa dọc
        rt.sizeDelta = new Vector2(220, 350);

        // Tạo các nút xếp dọc (khoảng cách 110px mỗi nút, kích thước vừa vặn 200x75)
        CreateButton(rightBar.transform, "Nút Shop 🛒", new Vector2(0, 110), new Vector2(200, 75), () => OpenOverlay("Shop"));
        CreateButton(rightBar.transform, "Điểm Danh 🎁", new Vector2(0, 0), new Vector2(200, 75), () => OpenOverlay("Daily"));
        CreateButton(rightBar.transform, "Nhiệm Vụ 🏆", new Vector2(0, -110), new Vector2(200, 75), () => OpenOverlay("Achievement"));
    }

    private GameObject CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, System.Action onClickAction)
    {
        GameObject btnObj = new GameObject(label);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.6f, 0.95f, 1f); // Màu xanh dương sang xịn

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClickAction?.Invoke());

        // Gắn chữ lên nút
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text t = txtObj.AddComponent<Text>();
        t.font = GameEvents.GetSafeFont();
        t.fontSize = 28;
        t.color = Color.white;
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform txtRT = txtObj.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        // Hiệu ứng hover tương tác bằng code co giãn nhẹ
        btnObj.AddComponent<HoverScalableUI>();

        return btnObj;
    }

    private void BuildOverlays(GameObject parent)
    {
        // 1. SHOP OVERLAY
        _shopOverlay = CreateTranslucentOverlay(parent.transform, "Shop Skins - Đổi Đĩa Bánh");
        Transform shopContent = _shopOverlay.transform.Find("Panel/Content");
        if (shopContent != null)
        {
            CreateShopItem(shopContent, "Đĩa Đất Sét (Clay)", "50 Vàng", () => ShopManager.Instance.BuyOrEquipSkin("Clay"));
            CreateShopItem(shopContent, "Đĩa Gỗ Thượng Hạng (Wooden)", "100 Vàng", () => ShopManager.Instance.BuyOrEquipSkin("Wooden"));
            CreateShopItem(shopContent, "Đĩa Vàng Hoàng Gia (Golden)", "250 Vàng", () => ShopManager.Instance.BuyOrEquipSkin("Golden"));
        }

        // 2. DAILY REWARD OVERLAY
        _dailyOverlay = CreateTranslucentOverlay(parent.transform, "Quà Điểm Danh 24 Giờ");
        Transform dailyContent = _dailyOverlay.transform.Find("Panel/Content");
        if (dailyContent != null)
        {
            GameObject labelObj = new GameObject("DailyInfoText");
            labelObj.transform.SetParent(dailyContent, false);
            Text t = labelObj.AddComponent<Text>();
            t.font = GameEvents.GetSafeFont();
            t.fontSize = 28;
            t.color = Color.white;
            t.text = "Nhận 50 vàng miễn phí mỗi ngày điểm danh!\nHệ thống được bảo mật UTC chống hack thời gian.";
            t.alignment = TextAnchor.MiddleCenter;

            RectTransform lRT = labelObj.GetComponent<RectTransform>();
            lRT.sizeDelta = new Vector2(700, 100);
            lRT.anchoredPosition = new Vector2(0, 50);

            CreateButton(dailyContent, "Nhận Thưởng 🎁", new Vector2(0, -80), new Vector2(300, 80), () => DailyRewardManager.Instance.ClaimReward());
        }

        // 3. ACHIEVEMENT OVERLAY
        _achievementOverlay = CreateTranslucentOverlay(parent.transform, "Nhiệm Vụ Trọn Đời");
        // Sẽ được cập nhật nội dung tiến trình tự động từ Event-driven ở AchievementManager
    }

    private GameObject CreateTranslucentOverlay(Transform parent, string titleText)
    {
        GameObject overlay = new GameObject(titleText + " Overlay");
        overlay.transform.SetParent(parent, false);

        RectTransform rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // Khung nền đen mờ bao phủ toàn màn hình khóa tương tác
        Image bgImg = overlay.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.75f);
        overlay.AddComponent<Button>(); // Chặn click xuyên qua

        // Bảng gỗ chính giữa
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(800, 600);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f); // Bảng màu tối cực kỳ premium

        // Tiêu đề bảng
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        Text t = title.AddComponent<Text>();
        t.font = GameEvents.GetSafeFont();
        t.fontSize = 36;
        t.color = Color.yellow;
        t.text = titleText;
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -30);
        titleRT.sizeDelta = new Vector2(700, 80);

        // Khung chứa content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.sizeDelta = new Vector2(-40, -160);
        contentRT.anchoredPosition = new Vector2(0, -40);

        // Nút Đóng bảng
        CreateButton(panel.transform, "Đóng ❌", new Vector2(0, -250), new Vector2(180, 60), () => overlay.SetActive(false));

        overlay.SetActive(false); // Mặc định ẩn
        return overlay;
    }

    private void CreateShopItem(Transform parent, string skinName, string price, System.Action onAction)
    {
        GameObject item = new GameObject(skinName);
        item.transform.SetParent(parent, false);
        
        // Sắp xếp bố cục đơn giản
        RectTransform rt = item.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700, 100);
        
        // Thêm giãn cách theo bậc thang
        int siblingIndex = parent.childCount;
        rt.anchoredPosition = new Vector2(0, 120 - (siblingIndex * 110));

        Image img = item.AddComponent<Image>();
        img.color = new Color(0.16f, 0.18f, 0.24f, 0.9f);

        // Tên skin
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(item.transform, false);
        Text nt = nameObj.AddComponent<Text>();
        nt.font = GameEvents.GetSafeFont();
        nt.fontSize = 24;
        nt.color = Color.white;
        nt.text = skinName;
        nt.alignment = TextAnchor.MiddleLeft;

        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.05f, 0.5f);
        nameRT.anchorMax = new Vector2(0.05f, 0.5f);
        nameRT.pivot = new Vector2(0, 0.5f);
        nameRT.anchoredPosition = Vector2.zero;
        nameRT.sizeDelta = new Vector2(300, 60);

        // Nút hành động Mua/Trang bị
        CreateButton(item.transform, "MUA / DÙNG", new Vector2(220, 0), new Vector2(200, 60), onAction);
    }

    private void BuildGameOverPanel(GameObject parent)
    {
        _gameOverPanel = new GameObject("GameOverPanel");
        _gameOverPanel.transform.SetParent(parent.transform, false);

        RectTransform rt = _gameOverPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = _gameOverPanel.AddComponent<Image>();
        img.color = new Color(0.12f, 0f, 0f, 0.85f); // Khung màu đỏ thẫm báo hiệu thất bại

        // Tiêu đề GAME OVER
        GameObject goTextObj = new GameObject("TitleText");
        goTextObj.transform.SetParent(_gameOverPanel.transform, false);
        Text t = goTextObj.AddComponent<Text>();
        t.font = GameEvents.GetSafeFont();
        t.fontSize = 86;
        t.color = Color.yellow;
        t.text = "THUA CUỘC!";
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform tRT = goTextObj.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 0.65f);
        tRT.anchorMax = new Vector2(0.5f, 0.65f);
        tRT.pivot = new Vector2(0.5f, 0.5f);
        tRT.anchoredPosition = Vector2.zero;
        tRT.sizeDelta = new Vector2(900, 150);

        // Nút Chơi Lại
        CreateButton(_gameOverPanel.transform, "Chơi Lại 🔄", new Vector2(0, -100), new Vector2(300, 90), () => GameManager.Instance.RestartGame());

        _gameOverPanel.SetActive(false); // Mặc định ẩn
    }

    public void OpenOverlay(string type)
    {
        _shopOverlay.SetActive(type == "Shop");
        _dailyOverlay.SetActive(type == "Daily");
        _achievementOverlay.SetActive(type == "Achievement");

        // Nếu mở Achievement, cập nhật trực quan tiến trình
        if (type == "Achievement" && AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UpdateAchievementOverlayUI(_achievementOverlay.transform.Find("Panel/Content"));
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            _gameOverPanel.SetActive(true);
        }
        else
        {
            _gameOverPanel.SetActive(false);
        }
    }

    private void UpdateScoreUI(int score, int combo)
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"ĐIỂM: {score}";
        }

        if (_comboText != null)
        {
            if (combo > 1)
            {
                _comboText.text = $"COMBO X{combo}! 🔥";
                
                // Pulsate nhẹ Text combo
                PulsatingUI pulsate = _comboText.GetComponent<PulsatingUI>();
                if (pulsate != null) pulsate.TriggerPulse();
            }
            else
            {
                _comboText.text = "";
            }
        }

        // Cập nhật kỷ lục nếu vượt qua kỷ lục cũ
        int hs = PlayerPrefs.GetInt("PlayerHighScore", 0);
        if (score > hs)
        {
            PlayerPrefs.SetInt("PlayerHighScore", score);
            PlayerPrefs.Save();
            if (_highScoreText != null) _highScoreText.text = $"KỶ LỤC: {score}";
        }
    }

    private void UpdateGoldUI(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = $"VÀNG: {gold} 🪙";
        }
    }
}

// ==========================================
// THÀNH PHẦN HIỆU ỨNG TƯƠNG TÁC HOVER CỦA NÚT
// ==========================================
public class HoverScalableUI : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    private Vector3 _origScale;

    void Start()
    {
        _origScale = transform.localScale;
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        transform.localScale = _origScale * 1.08f; // Phóng to nhẹ
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        transform.localScale = _origScale; // Hồi vị
    }
}

// ==========================================
// THÀNH PHẦN HIỆU ỨNG NHẢY CHỮ COMBO (PULSATING EFFECT)
// ==========================================
public class PulsatingUI : MonoBehaviour
{
    private Vector3 _originalScale;
    private Coroutine _pulseRoutine;

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void TriggerPulse()
    {
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(_originalScale, _originalScale * 1.35f, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(_originalScale * 1.35f, _originalScale, elapsed / 0.15f);
            yield return null;
        }

        transform.localScale = _originalScale;
    }
}
