using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools = new List<Pool>();
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                if (pool.prefab == null) continue;
                GameObject obj = Instantiate(pool.prefab, this.transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            _poolDictionary[pool.tag] = objectPool;
        }

        // Tự tạo một số pool mặc định dự phòng nếu danh sách rỗng (tăng kích thước từ 8 lên 32 để tránh sinh rác GC động khi nổ combo lớn)
        EnsurePoolExists("BloomExplosion", 32);
        EnsurePoolExists("FloatingText", 32);
    }

    private void EnsurePoolExists(string tag, int defaultSize)
    {
        if (!_poolDictionary.ContainsKey(tag))
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            _poolDictionary[tag] = objectPool;

            for (int i = 0; i < defaultSize; i++)
            {
                GameObject obj = CreateFallbackObject(tag);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
        }
    }

    // TẠO VFX DỰ PHÒNG XỊN SÒ: Sinh các hiệu ứng phát nổ 3D và Text hiển thị mà không phụ thuộc file ngoài
    private GameObject CreateFallbackObject(string tag)
    {
        GameObject obj;

        if (tag == "BloomExplosion")
        {
            // Tạo một GameObject chứa trình điều khiển nổ 3D
            obj = new GameObject("Fallback_BloomExplosion_VFX");
            obj.transform.SetParent(this.transform);
            obj.AddComponent<FallbackExplosionVFX>();
        }
        else if (tag == "FloatingText")
        {
            // Tạo một TextMesh 3D nổi trong không gian 3D
            obj = new GameObject("Fallback_FloatingText_3D");
            obj.transform.SetParent(this.transform);
            
            TextMesh tm = obj.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.08f;
            tm.fontSize = 42;
            tm.color = Color.yellow;
            
            Font safeFont = GameEvents.GetSafeFont();

            if (safeFont != null)
            {
                tm.font = safeFont;
                MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                if (mr != null && safeFont.material != null)
                {
                    mr.material = safeFont.material;
                }
            }
            else
            {
                Debug.LogWarning("[ObjectPooler] Không tìm thấy font hợp lệ cho TextMesh. Sử dụng font mặc định.");
            }

            obj.AddComponent<FallbackFloatingText>();
        }
        else
        {
            obj = new GameObject("Empty_Pooled_Object");
            obj.transform.SetParent(this.transform);
        }

        return obj;
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!_poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[Pooler] Pool với tag '{tag}' không tồn tại!");
            return null;
        }

        Queue<GameObject> poolQueue = _poolDictionary[tag];
        GameObject objectToSpawn = null;

        // Nếu hàng đợi rỗng hoặc vật thể đầu tiên null/đang hoạt động, sinh thêm động để tránh crash (Pool mở rộng linh hoạt)
        if (poolQueue.Count == 0 || poolQueue.Peek() == null || poolQueue.Peek().activeSelf)
        {
            objectToSpawn = CreateFallbackObject(tag);
        }
        else
        {
            objectToSpawn = poolQueue.Dequeue();
        }

        if (objectToSpawn != null)
        {
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            // Đẩy lại vào cuối hàng đợi để tái sử dụng
            poolQueue.Enqueue(objectToSpawn);
        }

        return objectToSpawn;
    }

    // Hàm gọi nhanh để sinh Text điểm nổi
    public void SpawnFloatingText(Vector3 position, string text)
    {
        GameObject textObj = SpawnFromPool("FloatingText", position, Quaternion.identity);
        if (textObj != null)
        {
            FallbackFloatingText script = textObj.GetComponent<FallbackFloatingText>();
            if (script != null)
            {
                script.SetText(text);
            }
        }
    }
}

// ==========================================
// TRÌNH ĐIỀU KHIỂN HIỆU ỨNG PHÁT NỔ 3D GỒM 12 VIÊN ĐÁ LẤP LÁNH BUNG RA
// ==========================================
public class FallbackExplosionVFX : MonoBehaviour
{
    private List<Transform> _particles = new List<Transform>();
    private List<Vector3> _directions = new List<Vector3>();
    private float _elapsed = 0f;
    private float _duration = 0.6f;

    // Danh sách màu dùng chung để tối ưu hóa Draw Call và tránh rò rỉ bộ nhớ Material
    private static Material[] _sharedColors;
    private const int COLOR_COUNT = 8;

    private static void InitializeSharedMaterials()
    {
        if (_sharedColors != null) return;

        Shader unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
        if (unlitShader == null) unlitShader = Shader.Find("Standard");

        if (unlitShader != null)
        {
            _sharedColors = new Material[COLOR_COUNT];
            for (int i = 0; i < COLOR_COUNT; i++)
            {
                _sharedColors[i] = new Material(unlitShader);
                // Tạo màu sắc rực rỡ ngẫu nhiên theo HSL
                _sharedColors[i].color = Color.HSVToRGB((float)i / COLOR_COUNT, 0.85f, 0.95f);
                _sharedColors[i].name = $"SharedExplosionMaterial_{i}";
            }
        }
    }

    void Awake()
    {
        // Khởi tạo các vật liệu dùng chung một lần duy nhất
        InitializeSharedMaterials();

        // Sinh ra 12 khối cầu nhỏ giả lập hiệu ứng nổ pháo hoa hạt cát lấp lánh
        for (int i = 0; i < 12; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(p.GetComponent<Collider>()); // Bỏ collider tránh va chạm vật lý
            p.transform.SetParent(this.transform);
            p.transform.localScale = Vector3.one * 0.12f;

            // Đặt màu dùng chung từ cache thay vì tạo 'new Material' cho mỗi hạt
            MeshRenderer mr = p.GetComponent<MeshRenderer>();
            if (mr != null && _sharedColors != null && _sharedColors.Length > 0)
            {
                mr.sharedMaterial = _sharedColors[Random.Range(0, _sharedColors.Length)];
            }

            _particles.Add(p.transform);
            
            // Hướng bay ngẫu nhiên hình bán cầu (bắn lên trên bàn chơi)
            Vector3 dir = Random.onUnitSphere;
            if (dir.y < 0) dir.y = -dir.y; // Hướng bay hướng lên
            dir.y += 0.5f; // Tăng cường lực phóng Y
            _directions.Add(dir.normalized * Random.Range(2f, 4.5f));
        }
    }

    void OnEnable()
    {
        _elapsed = 0f;
        // Đưa các hạt về tâm
        for (int i = 0; i < _particles.Count; i++)
        {
            _particles[i].localPosition = Vector3.zero;
            _particles[i].localScale = Vector3.one * 0.12f;
            _particles[i].gameObject.SetActive(true);
        }
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _duration;

        if (t >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Di chuyển các hạt bắn ra ngoài theo quán tính cản khí
        for (int i = 0; i < _particles.Count; i++)
        {
            _particles[i].position += _directions[i] * Time.deltaTime * (1f - t);
            // Co nhỏ dần kích thước về 0
            _particles[i].localScale = Vector3.one * 0.12f * (1f - t);
        }
    }
}

// ==========================================
// TRÌNH ĐIỀU KHIỂN CHỮ HIỂN THỊ ĐIỂM BAY LÊN VÀ MỜ DẦN (Floating Text)
// ==========================================
public class FallbackFloatingText : MonoBehaviour
{
    private TextMesh _textMesh;
    private float _elapsed = 0f;
    private float _duration = 1.0f;
    private float _speed = 1.2f;

    void Awake()
    {
        _textMesh = GetComponent<TextMesh>();
    }

    public void SetText(string val)
    {
        if (_textMesh != null) _textMesh.text = val;
    }

    void OnEnable()
    {
        _elapsed = 0f;
        if (_textMesh != null) _textMesh.color = Color.yellow;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _duration;

        if (t >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Chữ bay lên trên cao
        transform.position += Vector3.up * _speed * Time.deltaTime;
        
        // Mờ dần alpha về 0
        if (_textMesh != null)
        {
            Color c = _textMesh.color;
            c.a = 1f - t;
            _textMesh.color = c;
        }
    }
}
