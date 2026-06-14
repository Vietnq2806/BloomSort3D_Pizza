using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// [DefaultExecutionOrder(-200)] đảm bảo SaveSystem.Awake() chạy TRƯỚC
// tất cả các Manager khác (GameManager, ShopManager, AchievementManager)
[DefaultExecutionOrder(-200)]
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private const string SAVE_FILE_NAME = "save_data.json";
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    // ==========================================
    // CẤU TRÚC DỮ LIỆU SAVE - Tuần tự hoá 1-1 thành JSON
    // Mọi thay đổi persistent đều đi qua struct này (Single Source of Truth)
    // ==========================================
    [Serializable]
    public class SaveData
    {
        public int gold = 100;
        public int highScore = 0;
        public string equippedSkin = "Default";
        public List<string> purchasedSkins = new List<string> { "Default" };

        // Tiến trình Achievement
        public int lifetimeBlooms = 0;
        public int maxComboReached = 0;
        public int lifetimeGoldEarned = 100;

        // Timestamp điểm danh (ISO 8601 UTC)
        public string lastDailyClaimTimeUTC = "";
    }

    /// <summary>Điểm truy cập dữ liệu duy nhất toàn game (Single Source of Truth)</summary>
    public SaveData Data { get; private set; } = new SaveData();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Tồn tại xuyên suốt các scene
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // GHI DỮ LIỆU: Tuần tự hoá SaveData → JSON → ổ đĩa
    // ==========================================
    public void Save()
    {
        try
        {
            // prettyPrint=true: dễ đọc khi mở bằng text editor để debug
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveSystem] ✅ Lưu JSON thành công:\n{SaveFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] ❌ Lỗi ghi file JSON: {ex.Message}");
        }
    }

    // ==========================================
    // ĐỌC DỮ LIỆU: ổ đĩa → JSON → SaveData
    // ==========================================
    public void Load()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                Data = (parsed != null) ? parsed : new SaveData();
                Debug.Log($"[SaveSystem] ✅ Tải JSON thành công:\n{SaveFilePath}");
            }
            else
            {
                // Lần đầu chạy game: kiểm tra dữ liệu cũ PlayerPrefs để migrate
                Data = new SaveData();
                MigrateFromPlayerPrefsIfNeeded();
                Save(); // Tạo file JSON lần đầu
                Debug.Log("[SaveSystem] Khởi tạo file save mới cho lần chơi đầu tiên.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveSystem] ❌ Lỗi đọc file JSON: {ex.Message}. Dùng dữ liệu mặc định.");
            Data = new SaveData();
        }
    }

    // ==========================================
    // MIGRATION: PlayerPrefs → JSON (chạy 1 lần khi cập nhật game)
    // Đảm bảo người chơi cũ không mất dữ liệu
    // ==========================================
    private void MigrateFromPlayerPrefsIfNeeded()
    {
        if (!PlayerPrefs.HasKey("PlayerGold")) return; // Không có dữ liệu cũ

        Data.gold                  = PlayerPrefs.GetInt("PlayerGold", 100);
        Data.highScore             = PlayerPrefs.GetInt("PlayerHighScore", 0);
        Data.equippedSkin          = PlayerPrefs.GetString("EquippedSkin", "Default");
        Data.lifetimeBlooms        = PlayerPrefs.GetInt("Achievement_LifetimeBlooms", 0);
        Data.maxComboReached       = PlayerPrefs.GetInt("Achievement_MaxCombo", 0);
        Data.lifetimeGoldEarned    = PlayerPrefs.GetInt("Achievement_LifetimeGold", 100);
        Data.lastDailyClaimTimeUTC = PlayerPrefs.GetString("DailyLastClaimTimeUTC", "");

        // Migrate từng skin đã mua
        string[] legacySkinIds = { "Clay", "Wooden", "Golden" };
        foreach (var id in legacySkinIds)
        {
            if (PlayerPrefs.GetInt("SkinPurchased_" + id, 0) == 1 && !Data.purchasedSkins.Contains(id))
                Data.purchasedSkins.Add(id);
        }

        Debug.Log("[SaveSystem] ✅ Di chuyển dữ liệu từ PlayerPrefs → JSON thành công.");
    }

    // ==========================================
    // HELPER: API tiện lợi cho các Manager khác
    // ==========================================
    public bool IsSkinPurchased(string skinId) => Data.purchasedSkins.Contains(skinId);

    public void MarkSkinPurchased(string skinId)
    {
        if (!Data.purchasedSkins.Contains(skinId))
            Data.purchasedSkins.Add(skinId);
        // Không gọi Save() ở đây để tránh ghi file nhiều lần liên tiếp,
        // người gọi sẽ gọi Save() sau khi cập nhật xong tất cả fields
    }

    // ==========================================
    // DEBUG TOOLS (hiển thị trong Inspector → Right-click component)
    // ==========================================
    [ContextMenu("Show Save File Path")]
    public void ShowSaveFilePath() => Debug.Log($"[SaveSystem] Đường dẫn: {SaveFilePath}");

    [ContextMenu("Delete Save File (Full Reset)")]
    public void DeleteSaveFile()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Data = new SaveData();
            Debug.Log("[SaveSystem] Đã xoá file save và reset về mặc định.");
        }
    }

    [ContextMenu("Print Current Save Data")]
    public void PrintCurrentData() => Debug.Log($"[SaveSystem] Nội dung hiện tại:\n{JsonUtility.ToJson(Data, true)}");
}
