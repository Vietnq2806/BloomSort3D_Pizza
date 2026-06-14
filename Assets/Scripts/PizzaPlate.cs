using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PizzaPlate : MonoBehaviour
{
    private GridManager _grid;
    private Transform[] _pizzaSlots = new Transform[6];
    private Transform _currentSlot;

    private bool _isMerging = false;
    public bool IsMerging => _isMerging;

    private bool _isBloomed = false;
    public bool IsBloomed => _isBloomed;

    [Header("Cấu Hình Bay Bánh")]
    public float flyDuration = 0.25f;

    // Các biến cache toạ độ gốc từ Editor
    private Vector3[] _cachedLocalPositions = new Vector3[6];
    private Quaternion[] _cachedLocalRotations = new Quaternion[6];
    private Vector3[] _cachedLocalScales = new Vector3[6];

    public Vector3 GetCachedLocalPosition(int index) => _cachedLocalPositions[index];
    public Quaternion GetCachedLocalRotation(int index) => _cachedLocalRotations[index];

    public void ApplyCachedTransformToSlice(Transform slice, int index)
    {
        slice.localPosition = _cachedLocalPositions[index];
        slice.localRotation = _cachedLocalRotations[index];
        slice.localScale = _cachedLocalScales[index];
    }

    void Awake()
    {
        _grid = FindFirstObjectByType<GridManager>();
        
        // Cấu hình các slot trên đĩa bánh và cache toạ độ thiết kế gốc
        for (int i = 0; i < 6; i++)
        {
            _pizzaSlots[i] = transform.Find($"Pizza_Slot_{i}");
            if (_pizzaSlots[i] == null)
            {
                // Tự động tạo slot dự phòng nếu không tìm thấy trong prefab
                GameObject newSlot = new GameObject($"Pizza_Slot_{i}");
                newSlot.transform.SetParent(this.transform);
                newSlot.transform.localRotation = Quaternion.Euler(0, i * 60f, 0);
                newSlot.transform.localPosition = Vector3.up * 0.05f;
                _pizzaSlots[i] = newSlot.transform;
            }

            // Cache toạ độ gốc của lát bánh từ Editor để tái lập chính xác 100%
            if (_pizzaSlots[i].childCount > 0)
            {
                Transform child = _pizzaSlots[i].GetChild(0);
                _cachedLocalPositions[i] = child.localPosition;
                _cachedLocalRotations[i] = child.localRotation;
                _cachedLocalScales[i] = child.localScale;
            }
            else
            {
                // Giá trị dự phòng chuẩn từ SinglePlate
                _cachedLocalPositions[i] = new Vector3(0f, 0.25f, -0.28f);
                _cachedLocalRotations[i] = Quaternion.Euler(-90f, 0f, 50f);
                _cachedLocalScales[i] = Vector3.one * 0.3f;
            }
        }
    }

    void Start()
    {
        // Nhận đăng ký skin
        GameEvents.OnSkinEquipped += ApplySkin;
        string equippedSkin = PlayerPrefs.GetString("EquippedSkin", "Default");
        ApplySkin(equippedSkin);

        // Tự động khởi tạo ngẫu nhiên lát bánh khi bắt đầu chơi game (khắc phục lỗi không kích hoạt được randomizer của Unity)
        InitializeRandomSlices();
    }

    void OnDestroy()
    {
        GameEvents.OnSkinEquipped -= ApplySkin;
    }

    // ĐỒNG BỘ: Thiết lập ô lưới hiện tại đang chứa đĩa bánh
    public void SetGridSlot(Transform slot)
    {
        _currentSlot = slot;
    }

    public Transform GetGridSlot() => _currentSlot;

    // KHỞI TẠO BÁNH NGẪU NHIÊN: Sinh 2 đến 5 miếng bánh với độ khó tăng dần theo điểm số (Dynamic Balance)
    public void InitializeRandomSlices()
    {
        // 1. Dọn dẹp toàn bộ bánh mặc định có sẵn trong Prefab
        for (int i = 0; i < 6; i++)
        {
            foreach (Transform child in _pizzaSlots[i])
            {
                Destroy(child.gameObject);
            }
        }

        // 2. Quyết định ngẫu nhiên số lượng miếng bánh (từ 2 đến 5)
        int totalSlices = Random.Range(2, 6);

        // 3. Đọc điểm số hiện tại để điều chỉnh độ khó động
        int currentScore = (GameManager.Instance != null) ? GameManager.Instance.score : 0;

        // Giới hạn số lượng vị pizza xuất hiện trong game (Pool size)
        int maxFlavorId = 3;
        if (currentScore >= 800) maxFlavorId = 6;
        else if (currentScore >= 400) maxFlavorId = 5;
        else if (currentScore >= 150) maxFlavorId = 4;

        // Tỷ lệ xuất hiện đĩa bánh chứa 2 vị (Mix plate)
        float doubleFlavorChance = 0f; // Dưới 100 điểm thì 100% đĩa đơn vị để dễ ghép bánh ban đầu
        if (currentScore >= 600) doubleFlavorChance = 0.35f;
        else if (currentScore >= 300) doubleFlavorChance = 0.25f;
        else if (currentScore >= 100) doubleFlavorChance = 0.15f;

        // Quyết định số vị của đĩa này (1 hoặc 2 vị)
        int flavorCount = 1;
        if (totalSlices >= 3 && Random.value < doubleFlavorChance)
        {
            flavorCount = 2;
        }

        // Chọn vị ngẫu nhiên trong pool cho phép
        string flavorA = $"Pizza {Random.Range(1, maxFlavorId + 1)}";
        string flavorB = flavorA;
        if (flavorCount == 2)
        {
            int safety = 0;
            do
            {
                flavorB = $"Pizza {Random.Range(1, maxFlavorId + 1)}";
                safety++;
            } while (flavorB == flavorA && safety < 10);
        }

        // 4. Sinh miếng bánh vào các slot bắt đầu từ slot 0
        int slicesA = (flavorCount == 1) ? totalSlices : Random.Range(1, totalSlices);
        int slicesB = totalSlices - slicesA;

        for (int i = 0; i < totalSlices; i++)
        {
            string chosenFlavor = (i < slicesA) ? flavorA : flavorB;
            SpawnSliceInSlot(i, chosenFlavor);
        }
    }

    private void SpawnSliceInSlot(int slotIndex, string flavorName)
    {
        // Tải Mesh của miếng pizza từ Resources
        GameObject slicePrefab = Resources.Load<GameObject>($"Models/{flavorName}");
        if (slicePrefab != null)
        {
            GameObject sliceInstance = Instantiate(slicePrefab, _pizzaSlots[slotIndex]);
            sliceInstance.name = flavorName; // Dùng tên này làm Flavor định vị
            
            // Căn chỉnh vị trí phẳng lỳ theo toạ độ cache thiết kế gốc
            ApplyCachedTransformToSlice(sliceInstance.transform, slotIndex);
        }
        else
        {
            Debug.LogError($"[Resources Error] Không thể tải model miếng bánh: Models/{flavorName}");
        }
    }

    // LẤY HƯƠNG VỊ NGOÀI CÙNG (Top slice flavor)
    public string GetTopSliceFlavor()
    {
        for (int i = 5; i >= 0; i--)
        {
            if (_pizzaSlots[i].childCount > 0)
            {
                return _pizzaSlots[i].GetChild(0).name;
            }
        }
        return null;
    }

    // LẤY SỐ LƯỢNG MIẾNG BÁNH HIỆN TẠI
    public int GetSliceCount()
    {
        int count = 0;
        for (int i = 0; i < 6; i++)
        {
            if (_pizzaSlots[i].childCount > 0) count++;
        }
        return count;
    }

    public int GetEmptySlotCount() => 6 - GetSliceCount();

    // Lấy số lượng miếng bánh liên tục ở trên cùng có cùng flavor
    public int GetTopFlavorSequenceCount(string flavor)
    {
        int count = 0;
        for (int i = 5; i >= 0; i--)
        {
            if (_pizzaSlots[i].childCount > 0)
            {
                if (_pizzaSlots[i].GetChild(0).name == flavor)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
        }
        return count;
    }

    // Áp dụng Skin cho đĩa bánh (mesh 3D)
    public void ApplySkin(string skinId)
    {
        // Tìm MeshFilter trên chính đĩa bánh hoặc các con trực tiếp của nó
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.gameObject.name.Contains("Pizza")) return; // Tránh đổi mesh của miếng pizza con

        MeshRenderer mr = mf.GetComponent<MeshRenderer>();

        if (skinId == "Clay")
        {
            // Skin đĩa đất sét đỏ mộc mạc
            Mesh clayMesh = Resources.Load<Mesh>("Models/DoublePlate");
            if (clayMesh != null) mf.sharedMesh = clayMesh;

            Texture2D clayTex = Resources.Load<Texture2D>("textures/plate02");
            if (clayTex != null && mr != null) mr.material.mainTexture = clayTex;
        }
        else if (skinId == "Wooden")
        {
            // Skin đĩa gỗ sang xịn mịn
            Mesh woodenMesh = Resources.Load<Mesh>("Models/SinglePlate");
            if (woodenMesh != null) mf.sharedMesh = woodenMesh;

            Texture2D woodTex = Resources.Load<Texture2D>("textures/plate03");
            if (woodTex != null && mr != null) mr.material.mainTexture = woodTex;
        }
        else if (skinId == "Golden")
        {
            // Skin đĩa vàng hoàng gia
            Mesh goldMesh = Resources.Load<Mesh>("Models/SinglePlate");
            if (goldMesh != null) mf.sharedMesh = goldMesh;

            Texture2D goldTex = Resources.Load<Texture2D>("textures/plate05");
            if (goldTex != null && mr != null) mr.material.mainTexture = goldTex;
        }
        else
        {
            // Mặc định ban đầu
            Mesh defaultMesh = Resources.Load<Mesh>("Models/SinglePlate");
            if (defaultMesh != null) mf.sharedMesh = defaultMesh;

            Texture2D defaultTex = Resources.Load<Texture2D>("textures/plate01");
            if (defaultTex != null && mr != null) mr.material.mainTexture = defaultTex;
        }
    }

    // THUẬT TOÁN QUÉT 4 HƯỚNG BÊN TRONG LƯỚI
    public bool CheckAndMergeWithNeighbors()
    {
        if (_isMerging || _isBloomed || _currentSlot == null || _grid == null) return false;

        string myFlavor = GetTopSliceFlavor();
        if (string.IsNullOrEmpty(myFlavor)) return false;

        float checkDistanceX = _grid.spacingX * 1.1f;
        float checkDistanceZ = _grid.spacingZ * 1.1f;

        Vector3[] crossDirections = new Vector3[]
        {
            new Vector3(checkDistanceX, 0, 0),
            new Vector3(-checkDistanceX, 0, 0),
            new Vector3(0, 0, checkDistanceZ),
            new Vector3(0, 0, -checkDistanceZ)
        };

        foreach (Vector3 dir in crossDirections)
        {
            Vector3 neighborPos = _currentSlot.position + dir;

            foreach (var s in _grid.generatedSlots)
            {
                if (Vector3.Distance(s.position, neighborPos) < 0.2f)
                {
                    Transform neighborPlateTrans = _grid.GetPlateInSlot(s);
                    if (neighborPlateTrans != null && neighborPlateTrans != transform)
                    {
                        PizzaPlate neighborPlate = neighborPlateTrans.GetComponent<PizzaPlate>();
                        if (neighborPlate != null && !neighborPlate.IsMerging && !neighborPlate.IsBloomed)
                        {
                            string neighborFlavor = neighborPlate.GetTopSliceFlavor();
                            if (neighborFlavor == myFlavor)
                            {
                                // Tìm đĩa nào ít miếng bánh hơn thì gộp sang đĩa nhiều hơn
                                int myFlavorCount = GetSliceCountOfFlavor(myFlavor);
                                int neighborFlavorCount = neighborPlate.GetSliceCountOfFlavor(myFlavor);

                                PizzaPlate source = (myFlavorCount < neighborFlavorCount) ? this : neighborPlate;
                                PizzaPlate target = (source == this) ? neighborPlate : this;

                                if (target.GetEmptySlotCount() > 0 && source.GetTopSliceFlavor() == myFlavor)
                                {
                                    // Chạy coroutine trên GameManager (persistent) thay vì 'this'
                                    // để tránh coroutine bị kill khi source plate bị Destroy()
                                    GameManager.Instance.StartCoroutine(PerformMergeRoutine(source, target, myFlavor));
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Tự động kiểm tra Bloom (nở đĩa) nếu đĩa này đầy khít 6 miếng cùng vị
        if (GetSliceCount() == 6 && IsPureFlavor())
        {
            // Chạy BloomRoutine trên GameManager để tránh bị kill khi plate bị Destroy()
            GameManager.Instance.StartCoroutine(BloomRoutine());
            return true;
        }

        return false;
    }

    private int GetSliceCountOfFlavor(string flavor)
    {
        int count = 0;
        for (int i = 0; i < 6; i++)
        {
            if (_pizzaSlots[i].childCount > 0 && _pizzaSlots[i].GetChild(0).name == flavor)
            {
                count++;
            }
        }
        return count;
    }

    private bool IsPureFlavor()
    {
        string firstFlavor = null;
        for (int i = 0; i < 6; i++)
        {
            if (_pizzaSlots[i].childCount > 0)
            {
                string f = _pizzaSlots[i].GetChild(0).name;
                if (firstFlavor == null) firstFlavor = f;
                else if (firstFlavor != f) return false;
            }
        }
        return true;
    }

    // COROUTINE GỘP BÁNH CHUYÊN NGHIỆP
    private IEnumerator PerformMergeRoutine(PizzaPlate source, PizzaPlate target, string flavor)
    {
        source._isMerging = true;
        target._isMerging = true;

        GameManager.Instance.RegisterAnimationStart();

        // Di chuyển toàn bộ các miếng bánh có cùng vị từ source sang target
        while (target.GetEmptySlotCount() > 0 && source.GetTopSliceFlavor() == flavor)
        {
            // 1. Lấy vị trí miếng bánh ở trên cùng của source
            int sourceIdx = -1;
            for (int i = 5; i >= 0; i--)
            {
                if (source._pizzaSlots[i].childCount > 0)
                {
                    sourceIdx = i;
                    break;
                }
            }

            // 2. Lấy slot trống tiếp theo của target
            int targetIdx = -1;
            for (int i = 0; i < 6; i++)
            {
                if (target._pizzaSlots[i].childCount == 0)
                {
                    targetIdx = i;
                    break;
                }
            }

            if (sourceIdx != -1 && targetIdx != -1)
            {
                Transform pizzaSlice = source._pizzaSlots[sourceIdx].GetChild(0);
                Transform targetSlot = target._pizzaSlots[targetIdx];

                // Chạy FlyToTargetBezier trên GameManager để coroutine không bị kill giữa chừng
                yield return GameManager.Instance.StartCoroutine(FlyToTargetBezier(pizzaSlice, target, targetSlot, targetIdx));

                // Tăng Combo và kích hoạt sự kiện âm thanh
                GameManager.Instance.IncrementCombo();
                GameEvents.TriggerSliceMerged(pizzaSlice, targetSlot, GameManager.Instance.currentCombo);
            }
            else
            {
                break; // Thoát vòng lặp an toàn tránh đứng hình vô tận
            }

            yield return new WaitForSeconds(0.05f); // Khoảng chờ ngắn giữa các miếng bánh bay liên tiếp
        }

        source._isMerging = false;
        target._isMerging = false;

        // Xoá đĩa bánh cũ nếu hết sạch bánh
        if (source.GetSliceCount() == 0)
        {
            if (source._currentSlot != null && source._grid != null)
            {
                source._grid.SetPlateInSlot(source._currentSlot, null);
            }
            Destroy(source.gameObject);
        }

        // Kiểm tra Bloom trên đĩa đích sau khi gộp
        // BloomRoutine có RegisterAnimationStart/End riêng → counter tự cân bằng:
        // PerformMerge: +1 | Bloom: +1 → -1 | PerformMerge: -1 → tổng = 0 ✓
        bool willBloom = target != null && !target._isBloomed
                         && target.GetSliceCount() == 6 && target.IsPureFlavor();
        if (willBloom)
        {
            GameManager.Instance.StartCoroutine(target.BloomRoutine());
        }

        // LUÔN gọi End để cân bằng với Start ở đầu routine
        GameManager.Instance.RegisterAnimationEnd();
    }

    private IEnumerator FlyToTargetBezier(Transform slice, PizzaPlate targetPlate, Transform targetSlot, int targetIdx)
    {
        Vector3 startPos = slice.position;
        Quaternion startRot = slice.rotation;

        slice.SetParent(targetSlot);
        Vector3 endPos = targetSlot.TransformPoint(targetPlate.GetCachedLocalPosition(targetIdx));
        // Chuyển đổi local rotation của slice sang world rotation theo đúng parent slot
        Quaternion endRot = targetSlot.rotation * targetPlate.GetCachedLocalRotation(targetIdx);

        slice.SetParent(null); // Tạm thời tháo parent để bay tự do

        float elapsed = 0f;
        // Điểm uốn cong Bezier bay vồng lên trời
        Vector3 controlPoint = (startPos + endPos) / 2f + Vector3.up * 1.5f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;

            // Nội suy Bezier bậc 2
            Vector3 m1 = Vector3.Lerp(startPos, controlPoint, t);
            Vector3 m2 = Vector3.Lerp(controlPoint, endPos, t);
            slice.position = Vector3.Lerp(m1, m2, t);
            slice.rotation = Quaternion.Lerp(startRot, endRot, t);

            yield return null;
        }

        slice.SetParent(targetSlot);
        targetPlate.ApplyCachedTransformToSlice(slice, targetIdx);
    }

    // COROUTINE BÙM NỔ HOA (BLOOM SORT) THÀNH CÔNG!
    public IEnumerator BloomRoutine()
    {
        if (_isBloomed) yield break;
        _isBloomed = true;

        GameManager.Instance.RegisterAnimationStart();
        
        string myFlavor = GetTopSliceFlavor();
        Debug.Log($"[Bloom Sort] Đĩa bánh vị {myFlavor} nở thành công! BÙM!");

        // Phát tín hiệu nổ Bloom
        GameEvents.TriggerPlateBloomed(this.transform, myFlavor, GameManager.Instance.currentCombo);

        // Hiệu ứng co dãn nhẹ trước khi nổ
        float elapsed = 0f;
        Vector3 originScale = transform.localScale;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            // Squash and Stretch co dãn nhẹ chuẩn UX
            transform.localScale = new Vector3(originScale.x * 1.15f, originScale.y * 0.7f, originScale.z * 1.15f);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.1f;
            transform.localScale = new Vector3(originScale.x * 0.8f, originScale.y * 1.3f, originScale.z * 0.8f);
            yield return null;
        }

        // Tích luỹ điểm số và vàng dựa trên combo
        int scoreGained = 60 * (1 + GameManager.Instance.currentCombo);
        int goldGained = 5 * (1 + GameManager.Instance.currentCombo);

        GameManager.Instance.AddScore(scoreGained);
        GameManager.Instance.AddGold(goldGained);

        // Thu hồi và huỷ khỏi lưới chơi
        if (_currentSlot != null && _grid != null)
        {
            _grid.SetPlateInSlot(_currentSlot, null);
        }

        // Gọi nổ hiệu ứng từ Pool
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.SpawnFromPool("BloomExplosion", transform.position + Vector3.up * 0.1f, Quaternion.identity);
            ObjectPooler.Instance.SpawnFloatingText(transform.position + Vector3.up * 0.5f, $"+{scoreGained} Điểm!");
        }
        else
        {
            Debug.LogWarning("[PizzaPlate] Không tìm thấy ObjectPooler.Instance. Bỏ qua hiệu ứng nổ và chữ nổi.");
        }

        // QUAN TRỌNG: Phải gọi RegisterAnimationEnd() TRƯỚC Destroy(gameObject)
        // vì Destroy sẽ kill coroutine ngay lập tức, mọi lệnh sau Destroy sẽ không chạy
        // gây ra game bị kẹt ở trạng thái Animating mãi mãi
        GameManager.Instance.RegisterAnimationEnd();

        Destroy(gameObject);
    }
}
