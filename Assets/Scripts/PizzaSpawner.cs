using UnityEngine;
using System.Collections;

public class PizzaSpawner : MonoBehaviour
{
    [Header("Cấu Hình Prefab")]
    public GameObject platePrefab;
    public Transform spawnPoint;

    [Header("Cấu Hình Thời Gian")]
    public float spawnDelay = 0.2f;

    private GameObject _currentPlate;
    private bool _isRefilling = false;

    void Start()
    {
        SpawnNewPlate();
    }

    public void OnPlateTaken()
    {
        if (_isRefilling) return;
        StartCoroutine(RefillRoutine());
    }

    public void OnPlatePlacedSuccessfully()
    {
        // Có thể dùng để kích hoạt các hiệu ứng đặt đĩa thành công
    }

    private IEnumerator RefillRoutine()
    {
        _isRefilling = true;
        _currentPlate = null; // Giải phóng khay chờ

        yield return new WaitForSeconds(spawnDelay);
        SpawnNewPlate();
        _isRefilling = false;
    }

    private void SpawnNewPlate()
    {
        if (_currentPlate != null || platePrefab == null || spawnPoint == null) return;

        // Sinh đĩa bánh giữ nguyên rotation thiết kế gốc của prefab (đảm bảo plate nằm phẳng đúng)
        _currentPlate = Instantiate(platePrefab, spawnPoint.position, platePrefab.transform.rotation);
        _currentPlate.name = "Hold_Plate";

        // Gắn liên kết Spawner vào đĩa bánh mới sinh
        DragAndDrop dragScript = _currentPlate.GetComponent<DragAndDrop>();
        if (dragScript != null)
        {
            dragScript.AssignSpawner(this);
        }
    }
}