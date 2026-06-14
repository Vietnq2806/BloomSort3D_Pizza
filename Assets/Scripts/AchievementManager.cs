using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public int targetValue;
        public int currentValue;
        public bool isUnlocked;
    }

    public List<Achievement> achievements = new List<Achievement>();

    // Chỉ số tích luỹ trọn đời
    private int _lifetimeBlooms = 0;
    private int _maxComboReached = 0;
    private int _lifetimeGoldEarned = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadAchievementData();
    }

    void OnEnable()
    {
        // EVENT-DRIVEN (Observer Pattern): Lắng nghe sự kiện để tự động cộng dồn chỉ số
        GameEvents.OnPlateBloomed += HandlePlateBloomed;
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnGoldChanged += HandleGoldChanged;
    }

    void OnDisable()
    {
        GameEvents.OnPlateBloomed -= HandlePlateBloomed;
        GameEvents.OnScoreChanged -= HandleScoreChanged;
        GameEvents.OnGoldChanged -= HandleGoldChanged;
    }

    private void LoadAchievementData()
    {
        // Tải các thông số tích lũy từ SaveSystem JSON
        if (SaveSystem.Instance != null)
        {
            _lifetimeBlooms     = SaveSystem.Instance.Data.lifetimeBlooms;
            _maxComboReached    = SaveSystem.Instance.Data.maxComboReached;
            _lifetimeGoldEarned = SaveSystem.Instance.Data.lifetimeGoldEarned;
        }
        else
        {
            // Fallback PlayerPrefs
            _lifetimeBlooms     = PlayerPrefs.GetInt("Achievement_LifetimeBlooms", 0);
            _maxComboReached    = PlayerPrefs.GetInt("Achievement_MaxCombo", 0);
            _lifetimeGoldEarned = PlayerPrefs.GetInt("Achievement_LifetimeGold", 100);
        }

        // Khởi tạo các nhiệm vụ
        achievements.Clear();
        achievements.Add(new Achievement { 
            id = "Bloom100", 
            title = "Người Làm Bánh Chăm Chỉ 👩‍🍳", 
            description = "Nổ thành công 100 đĩa Pizza Bloom Sort.", 
            targetValue = 100, 
            currentValue = _lifetimeBlooms 
        });
        achievements.Add(new Achievement { 
            id = "ComboX5", 
            title = "Bậc Thầy Nhịp Điệu ⚡", 
            description = "Đạt chuỗi combo liên hoàn x5.", 
            targetValue = 5, 
            currentValue = _maxComboReached 
        });
        achievements.Add(new Achievement { 
            id = "Gold1000", 
            title = "Triệu Phú Pizza 🪙", 
            description = "Tích luỹ tổng cộng 1000 vàng.", 
            targetValue = 1000, 
            currentValue = _lifetimeGoldEarned 
        });

        // Tải trạng thái mở khóa
        foreach (var ach in achievements)
        {
            ach.isUnlocked = PlayerPrefs.GetInt("AchievementUnlocked_" + ach.id, 0) == 1;
            CheckUnlockStatus(ach, false);
        }
    }

    private void HandlePlateBloomed(Transform plate, string flavor, int combo)
    {
        _lifetimeBlooms++;

        // Đồng bộ vào SaveSystem JSON
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Data.lifetimeBlooms = _lifetimeBlooms;
            // Save() sẽ gọi sau khi CheckUnlockStatus xử lý xong
        }
        else
        {
            PlayerPrefs.SetInt("Achievement_LifetimeBlooms", _lifetimeBlooms);
            PlayerPrefs.Save();
        }

        // Cập nhật nhiệm vụ
        Achievement ach = achievements.Find(a => a.id == "Bloom100");
        if (ach != null && !ach.isUnlocked)
        {
            ach.currentValue = _lifetimeBlooms;
            CheckUnlockStatus(ach, true);
        }
    }

    private void HandleScoreChanged(int score, int combo)
    {
        if (combo > _maxComboReached)
        {
            _maxComboReached = combo;

            // Đồng bộ vào SaveSystem JSON
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.Data.maxComboReached = _maxComboReached;
            else
            {
                PlayerPrefs.SetInt("Achievement_MaxCombo", _maxComboReached);
                PlayerPrefs.Save();
            }

            Achievement ach = achievements.Find(a => a.id == "ComboX5");
            if (ach != null && !ach.isUnlocked)
            {
                ach.currentValue = _maxComboReached;
                CheckUnlockStatus(ach, true);
            }
        }
    }

    private void HandleGoldChanged(int currentGold)
    {
        // Chỉ cộng dồn nếu vàng tăng lên (kiếm thêm vàng)
        int lastSavedGold = (SaveSystem.Instance != null)
            ? SaveSystem.Instance.Data.lifetimeGoldEarned
            : PlayerPrefs.GetInt("Achievement_LastSavedGold", 100);

        if (currentGold > lastSavedGold)
        {
            int diff = currentGold - lastSavedGold;
            _lifetimeGoldEarned += diff;

            // Đồng bộ vào SaveSystem JSON
            if (SaveSystem.Instance != null)
                SaveSystem.Instance.Data.lifetimeGoldEarned = _lifetimeGoldEarned;
            else
            {
                PlayerPrefs.SetInt("Achievement_LifetimeGold", _lifetimeGoldEarned);
                PlayerPrefs.SetInt("Achievement_LastSavedGold", currentGold);
                PlayerPrefs.Save();
            }

            Achievement ach = achievements.Find(a => a.id == "Gold1000");
            if (ach != null && !ach.isUnlocked)
            {
                ach.currentValue = _lifetimeGoldEarned;
                CheckUnlockStatus(ach, true);
            }
        }
        else
        {
            if (SaveSystem.Instance == null)
            {
                PlayerPrefs.SetInt("Achievement_LastSavedGold", currentGold);
                PlayerPrefs.Save();
            }
        }
    }

    private void CheckUnlockStatus(Achievement ach, bool triggerVisualEffect)
    {
        if (ach.isUnlocked) return;

        if (ach.currentValue >= ach.targetValue)
        {
            ach.isUnlocked = true;
            PlayerPrefs.SetInt("AchievementUnlocked_" + ach.id, 1);
            PlayerPrefs.Save();

            // Lưu toàn bộ trạng thái achievement vào SaveSystem JSON một lần
            if (SaveSystem.Instance != null) SaveSystem.Instance.Save();

            if (triggerVisualEffect)
            {
                if (ObjectPooler.Instance != null)
                    ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1.5f, $"🏆 NHIỆM VỤ HOÀN THÀNH: {ach.title}!");
                Debug.Log($"[Achievement Unlocked] Mở khóa nhiệm vụ: {ach.title}");
            }
        }
    }

    // ==========================================
    // TỰ ĐỘNG SINH & CẬP NHẬT GIAO DIỆN NHIỆM VỤ DỰA TRÊN EVENT
    // ==========================================
    public void UpdateAchievementOverlayUI(Transform contentParent)
    {
        if (contentParent == null) return;

        // Dọn sạch các phần tử cũ trong bảng trước khi vẽ lại
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Vẽ danh sách nhiệm vụ trọn đời
        for (int i = 0; i < achievements.Count; i++)
        {
            Achievement ach = achievements[i];

            GameObject item = new GameObject($"AchItem_{ach.id}");
            item.transform.SetParent(contentParent, false);

            RectTransform rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700, 110);
            rt.anchoredPosition = new Vector2(0, 110 - (i * 130));

            Image img = item.AddComponent<Image>();
            // Nếu đã mở khóa dùng màu vàng nhạt sang chảnh, chưa mở khóa màu xám đậm
            img.color = ach.isUnlocked ? new Color(0.18f, 0.28f, 0.18f, 0.95f) : new Color(0.16f, 0.18f, 0.24f, 0.9f);

            // Tiêu đề & Mô tả nhiệm vụ
            GameObject titleObj = new GameObject("TitleAndDesc");
            titleObj.transform.SetParent(item.transform, false);
            Text txt = titleObj.AddComponent<Text>();
            txt.font = GameEvents.GetSafeFont();
            txt.fontSize = 22;
            txt.color = Color.white;
            txt.text = $"{ach.title}\n<size=18>{ach.description}</size>";
            txt.alignment = TextAnchor.MiddleLeft;
            txt.supportRichText = true;

            RectTransform txtRT = titleObj.GetComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0.05f, 0.5f);
            txtRT.anchorMax = new Vector2(0.05f, 0.5f);
            txtRT.pivot = new Vector2(0, 0.5f);
            txtRT.anchoredPosition = Vector2.zero;
            txtRT.sizeDelta = new Vector2(400, 90);

            // Hiển thị Tiến Trình (Progress Text)
            GameObject progressObj = new GameObject("ProgressText");
            progressObj.transform.SetParent(item.transform, false);
            Text ptxt = progressObj.AddComponent<Text>();
            ptxt.font = GameEvents.GetSafeFont();
            ptxt.fontSize = 22;
            ptxt.color = ach.isUnlocked ? Color.green : Color.yellow;
            ptxt.text = ach.isUnlocked ? "ĐÃ XONG ✔" : $"{ach.currentValue} / {ach.targetValue}";
            ptxt.alignment = TextAnchor.MiddleRight;

            RectTransform pRT = progressObj.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.95f, 0.5f);
            pRT.anchorMax = new Vector2(0.95f, 0.5f);
            pRT.pivot = new Vector2(1f, 0.5f);
            pRT.anchoredPosition = Vector2.zero;
            pRT.sizeDelta = new Vector2(200, 60);
        }
    }
}
