using UnityEngine;

/// <summary>
/// SceneBootstrapper: Tự động khởi tạo các singleton và spawner cần thiết cho game
/// Gắn script này vào một GameObject rỗng trong Scene để đảm bảo game chạy đúng.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    [Header("Prefab Tham Chiếu")]
    [Tooltip("Kéo prefab SinglePlate vào đây")]
    public GameObject platePrefab;

    [Header("Vị Trí Spawner Khay Chờ")]
    [Tooltip("Vị trí đặt Hold Slot (khay chờ đĩa bánh) phía trước bàn chơi")]
    public Vector3 spawnerPosition = new Vector3(0f, 0f, -3.5f);

    void Awake()
    {
        // ====== SAVE SYSTEM (khởi tạo ĐẦU TIÊN, trước tất cả Manager khác) ======
        // [DefaultExecutionOrder(-200)] đã đảm bảo nó chạy trước, nhưng tạo thủ công để đảm bảo chắc chắn
        if (SaveSystem.Instance == null)
        {
            GameObject ssGO = new GameObject("SaveSystem");
            ssGO.AddComponent<SaveSystem>();
            Debug.Log("[Bootstrapper] Đã tạo SaveSystem.");
        }

        // ====== GAME MANAGER ======
        if (GameManager.Instance == null)
        {
            GameObject gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
            Debug.Log("[Bootstrapper] Đã tạo GameManager.");
        }

        // ====== OBJECT POOLER ======
        if (ObjectPooler.Instance == null)
        {
            GameObject poolGO = new GameObject("ObjectPooler");
            poolGO.AddComponent<ObjectPooler>();
            Debug.Log("[Bootstrapper] Đã tạo ObjectPooler.");
        }

        // ====== TỰ TÌM PREFAB TỪ RESOURCES NẾU CHƯA ĐƯỢC GÁN ======
        if (platePrefab == null)
            platePrefab = Resources.Load<GameObject>("Prefabs/SinglePlate");
        if (platePrefab == null)
            platePrefab = Resources.Load<GameObject>("SinglePlate");
        if (platePrefab == null)
            Debug.LogWarning("[Bootstrapper] Không tìm thấy SinglePlate prefab. Kéo SinglePlate.prefab vào trường 'platePrefab' của SceneBootstrapper trong Inspector.");

        // ====== PIZZA SPAWNER (HOLD SLOT) ======
        // Kiểm tra xem đã có PizzaSpawner trong scene chưa
        PizzaSpawner existingSpawner = FindFirstObjectByType<PizzaSpawner>();
        if (existingSpawner == null && platePrefab != null)
        {
            GameObject spawnerGO = new GameObject("PizzaSpawner_HoldSlot_0");
            spawnerGO.transform.position = spawnerPosition;

            PizzaSpawner spawner = spawnerGO.AddComponent<PizzaSpawner>();
            spawner.platePrefab = platePrefab;

            // Tạo spawnPoint con ngay tại vị trí spawner (nằm phẳng theo mặt bàn, Y=0)
            GameObject spawnPointGO = new GameObject("SpawnPoint");
            spawnPointGO.transform.SetParent(spawnerGO.transform);
            spawnPointGO.transform.localPosition = Vector3.zero;
            // QUAN TRỌNG: SpawnPoint phải có rotation identity (không nghiêng)
            spawnPointGO.transform.localRotation = Quaternion.identity;
            spawner.spawnPoint = spawnPointGO.transform;

            Debug.Log("[Bootstrapper] Đã tạo PizzaSpawner tại: " + spawnerPosition);
        }
    }
}
