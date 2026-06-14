using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // ==========================================
    // CẤU TRÚC DỮ LIỆU ĐỌC TỪ shop_config.json
    // ==========================================
    [System.Serializable]
    public class SkinConfig
    {
        public string skinId;
        public string displayName;
        public int price;
        public string description;
    }

    [System.Serializable]
    private class ShopConfigWrapper
    {
        public List<SkinConfig> skins;
    }

    // ==========================================
    // DỮ LIỆU RUNTIME (bao gồm trạng thái isPurchased)
    // ==========================================
    [System.Serializable]
    public class SkinItem
    {
        public string skinId;
        public string displayName;
        public int price;
        public string description;
        public bool isPurchased;
    }

    public List<SkinItem> skins = new List<SkinItem>();
    private string _equippedSkin = "Default";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // BƯỚC 1: Đọc danh sách skin từ file JSON cấu hình (Data-Driven)
        LoadShopConfigFromJSON();

        // BƯỚC 2: Chồng trạng thái mua hàng từ SaveSystem JSON
        LoadPurchaseStateFromSaveSystem();
    }

    // ==========================================
    // ĐỌC CẤU HÌNH SHOP TỪ JSON (Data-Driven Design)
    // Chỉ cần sửa shop_config.json để thêm/xoá skin, không cần rebuild code
    // ==========================================
    private void LoadShopConfigFromJSON()
    {
        skins.Clear();

        TextAsset configFile = Resources.Load<TextAsset>("shop_config");
        if (configFile == null)
        {
            Debug.LogWarning("[ShopManager] Không tìm thấy Resources/shop_config.json. Dùng danh sách mặc định.");
            LoadHardcodedDefaultSkins();
            return;
        }

        try
        {
            ShopConfigWrapper config = JsonUtility.FromJson<ShopConfigWrapper>(configFile.text);
            if (config?.skins == null || config.skins.Count == 0)
            {
                Debug.LogWarning("[ShopManager] shop_config.json rỗng hoặc thiếu trường 'skins'.");
                LoadHardcodedDefaultSkins();
                return;
            }

            foreach (var sc in config.skins)
            {
                skins.Add(new SkinItem
                {
                    skinId      = sc.skinId,
                    displayName = sc.displayName,
                    price       = sc.price,
                    description = sc.description,
                    // Skin giá 0 = miễn phí, mặc định đã sở hữu
                    isPurchased = (sc.price == 0)
                });
            }

            Debug.Log($"[ShopManager] ✅ Tải {skins.Count} skin từ shop_config.json thành công.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopManager] ❌ Lỗi parse shop_config.json: {ex.Message}");
            LoadHardcodedDefaultSkins();
        }
    }

    // Fallback cứng nếu file JSON không tồn tại
    private void LoadHardcodedDefaultSkins()
    {
        skins.Add(new SkinItem { skinId = "Default", displayName = "Đĩa sứ trắng (Mặc định)",  price = 0,   isPurchased = true });
        skins.Add(new SkinItem { skinId = "Clay",    displayName = "Đĩa đất sét (Clay)",        price = 50  });
        skins.Add(new SkinItem { skinId = "Wooden",  displayName = "Đĩa gỗ sồi (Wooden)",       price = 100 });
        skins.Add(new SkinItem { skinId = "Golden",  displayName = "Đĩa vàng ròng (Golden)",     price = 250 });
    }

    // ==========================================
    // TẢI TRẠNG THÁI MUA HÀNG TỪ SAVESYSTEM JSON
    // ==========================================
    private void LoadPurchaseStateFromSaveSystem()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[ShopManager] SaveSystem chưa khởi tạo. Fallback về PlayerPrefs.");
            FallbackLoadFromPlayerPrefs();
            return;
        }

        // Ghi đè isPurchased theo danh sách đã mua trong SaveSystem
        foreach (var skin in skins)
        {
            skin.isPurchased = SaveSystem.Instance.IsSkinPurchased(skin.skinId);
        }

        _equippedSkin = SaveSystem.Instance.Data.equippedSkin;
        Debug.Log($"[ShopManager] ✅ Trạng thái mua hàng tải từ SaveSystem. Đang dùng: {_equippedSkin}");
    }

    private void FallbackLoadFromPlayerPrefs()
    {
        foreach (var skin in skins)
        {
            if (skin.price == 0) continue;
            skin.isPurchased = PlayerPrefs.GetInt("SkinPurchased_" + skin.skinId, 0) == 1;
        }
        _equippedSkin = PlayerPrefs.GetString("EquippedSkin", "Default");
    }

    // ==========================================
    // MUA HOẶC TRANG BỊ SKIN (Public API)
    // ==========================================
    public void BuyOrEquipSkin(string skinId)
    {
        SkinItem item = skins.Find(s => s.skinId == skinId);
        if (item == null) return;

        if (item.isPurchased)
        {
            EquipSkin(item);
        }
        else
        {
            TryPurchaseSkin(item);
        }
    }

    private void EquipSkin(SkinItem item)
    {
        _equippedSkin = item.skinId;

        // Lưu vào SaveSystem JSON
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.Data.equippedSkin = _equippedSkin;
            SaveSystem.Instance.Save();
        }
        else
        {
            PlayerPrefs.SetString("EquippedSkin", _equippedSkin);
            PlayerPrefs.Save();
        }

        // Phát Event để PizzaPlate.ApplySkin() cập nhật Mesh 3D ngay lập tức
        GameEvents.TriggerSkinEquipped(item.skinId);

        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1f, $"Đã trang bị: {item.displayName}!");

        Debug.Log($"[Shop] Trang bị skin: {item.skinId}");
    }

    private void TryPurchaseSkin(SkinItem item)
    {
        int playerGold = (GameManager.Instance != null) ? GameManager.Instance.gold : 0;

        if (playerGold >= item.price)
        {
            // Trừ vàng thông qua GameManager (GameManager sẽ tự đồng bộ SaveSystem)
            GameManager.Instance.AddGold(-item.price);

            item.isPurchased = true;
            _equippedSkin = item.skinId;

            // Lưu vào SaveSystem JSON
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.MarkSkinPurchased(item.skinId);
                SaveSystem.Instance.Data.equippedSkin = _equippedSkin;
                SaveSystem.Instance.Save();
            }
            else
            {
                PlayerPrefs.SetInt("SkinPurchased_" + item.skinId, 1);
                PlayerPrefs.SetString("EquippedSkin", _equippedSkin);
                PlayerPrefs.Save();
            }

            GameEvents.TriggerSkinEquipped(item.skinId);

            if (ObjectPooler.Instance != null)
                ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1f, $"Đã mua & trang bị: {item.displayName}!");

            Debug.Log($"[Shop] ✅ Mua thành công: {item.skinId} ({item.price} vàng)");
        }
        else
        {
            if (ObjectPooler.Instance != null)
                ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1f, "Không đủ vàng! Tích luỹ thêm nhé!");

            Debug.LogWarning($"[Shop] ❌ Thiếu vàng để mua: {item.skinId} (Cần {item.price}, có {playerGold})");
        }
    }

    // ==========================================
    // PUBLIC API CHO UIMANAGER
    // ==========================================
    public bool IsSkinPurchased(string skinId)
    {
        SkinItem item = skins.Find(s => s.skinId == skinId);
        return item != null && item.isPurchased;
    }

    public string GetEquippedSkin() => _equippedSkin;

    /// <summary>Trả về toàn bộ danh sách skin để UIManager build Shop UI tự động</summary>
    public List<SkinItem> GetAllSkins() => skins;
}
