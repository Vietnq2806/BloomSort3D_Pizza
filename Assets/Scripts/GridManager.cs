using UnityEngine;
using System.Collections.Generic;

// Lớp trung gian hứng dữ liệu cấu hình ma trận động từ file JSON (Tiêu chí Tuần 1 - Ngày 4)
[System.Serializable]
public class GridConfig
{
    public int gridWidth;
    public int gridHeight;
    public float cellSize;
}

public class GridManager : MonoBehaviour
{
    private GridConfig _config;

    // Cấu hình khoảng cách ô để phục vụ việc tính toán vị trí 4 hướng bên DragAndDrop
    [HideInInspector] public float spacingX;
    [HideInInspector] public float spacingZ;

    // ĐỒNG BỘ BIẾN: Danh sách các ô lưới công khai giúp script DragAndDrop truy cập dò khoảng cách Snap
    public List<Transform> generatedSlots = new List<Transform>();

    // Từ điển (Dictionary) lưu trữ thông tin: Ô lưới này đang chứa cái đĩa (Plate) nào
    private Dictionary<Transform, Transform> _slotToPlateMap = new Dictionary<Transform, Transform>();

    void Awake()
    {
        LoadGridConfig();
        GenerateGrid3D();
    }

    // ĐỌC CẤU HÌNH ĐỘNG (Data-Driven Design): Tránh việc hardcode viết chết số khung ô lưới
    private void LoadGridConfig()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("LevelConfig");
        if (jsonFile != null)
        {
            _config = JsonUtility.FromJson<GridConfig>(jsonFile.text);
            spacingX = _config.cellSize;
            spacingZ = _config.cellSize;
        }
        else
        {
            // Giá trị dự phòng nếu hệ thống không tìm thấy file JSON
            spacingX = 1.5f;
            spacingZ = 1.5f;
        }
    }

    // TỰ ĐỘNG SINH CĂN TÂM BÀN CHƠI (Fix lỗi lệch đĩa bánh ra khỏi bàn gỗ)
    private void GenerateGrid3D()
    {
        _slotToPlateMap.Clear();
        generatedSlots.Clear();

        if (_config == null) _config = new GridConfig { gridWidth = 3, gridHeight = 3, cellSize = 1.5f };

        // Thuật toán toán học: Tính khoảng lệch Offset đưa lưới về chính giữa trục (0,0,0) của chiếc bàn
        float offsetX = (_config.gridWidth - 1) * _config.cellSize / 2f;
        float offsetZ = (_config.gridHeight - 1) * _config.cellSize / 2f;

        for (int x = 0; x < _config.gridWidth; x++)
        {
            for (int z = 0; z < _config.gridHeight; z++)
            {
                // Tạo ô lưới phẳng dạng dẹt (Scale Y mỏng dính sát mặt bàn)
                GameObject cellVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cellVisual.name = $"Cell_{x}_{z}";

                // Vị trí chuẩn đã cộng trừ khoảng lệch tâm Offset
                Vector3 spawnPosition = new Vector3(x * _config.cellSize - offsetX, 0, z * _config.cellSize - offsetZ);
                cellVisual.transform.SetParent(this.transform);
                cellVisual.transform.localPosition = spawnPosition;
                cellVisual.transform.localRotation = Quaternion.identity;
                cellVisual.transform.localScale = new Vector3(_config.cellSize * 0.9f, 0.1f, _config.cellSize * 0.9f);

                // Huỷ Collider của ô lưới để tránh xung đột Raycast kéo thả đĩa bánh
                Destroy(cellVisual.GetComponent<Collider>());

                // Lưu dữ liệu đồng bộ
                generatedSlots.Add(cellVisual.transform);
                _slotToPlateMap[cellVisual.transform] = null;
            }
        }
        Debug.Log($"[Grid Matrix] Khởi tạo lưới thành công {_config.gridWidth}x{_config.gridHeight} chuẩn cấu hình!");
    }

    // ĐỒNG BỘ HÀM: Lấy thông tin đĩa bánh đang nằm trên ô lưới
    public Transform GetPlateInSlot(Transform slot)
    {
        if (slot == null) return null;
        if (_slotToPlateMap.TryGetValue(slot, out var plate)) return plate;
        return null;
    }

    // ĐỒNG BỘ HÀM: Cập nhật đĩa bánh vào ô lưới khi người chơi kéo thả thành công
    public void SetPlateInSlot(Transform slot, Transform plate)
    {
        if (slot == null) return;
        if (_slotToPlateMap.ContainsKey(slot))
        {
            _slotToPlateMap[slot] = plate;
        }
    }

    // Hàm dọn dẹp bộ nhớ xóa đĩa bánh khi nổ combo Bloom Sort thành công
    public void ClearPlateFromAllSlots(Transform plate)
    {
        List<Transform> keys = new List<Transform>(_slotToPlateMap.Keys);
        foreach (var key in keys)
        {
            if (_slotToPlateMap[key] == plate)
            {
                _slotToPlateMap[key] = null;
                break;
            }
        }
    }
}