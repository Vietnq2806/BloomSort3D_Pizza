using System;
using UnityEngine;

public static class GameEvents
{
    // Sự kiện thay đổi trạng thái game
    public static event Action<GameState> OnGameStateChanged;
    public static void TriggerGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

    // Sự kiện đặt đĩa bánh thành công vào ô lưới
    public static event Action<Transform, Transform> OnPlatePlaced; // (slot, plate)
    public static void TriggerPlatePlaced(Transform slot, Transform plate) => OnPlatePlaced?.Invoke(slot, plate);

    // Sự kiện một miếng bánh bay thành công (Merge)
    public static event Action<Transform, Transform, int> OnSliceMerged; // (slice, targetSlot, combo)
    public static void TriggerSliceMerged(Transform slice, Transform targetSlot, int combo) => OnSliceMerged?.Invoke(slice, targetSlot, combo);

    // Sự kiện đĩa bánh đầy 6 miếng cùng loại và biến mất (Bloom)
    public static event Action<Transform, string, int> OnPlateBloomed; // (plate, flavor, combo)
    public static void TriggerPlateBloomed(Transform plate, string flavor, int combo) => OnPlateBloomed?.Invoke(plate, flavor, combo);

    // Sự kiện thay đổi điểm số
    public static event Action<int, int> OnScoreChanged; // (currentScore, comboCount)
    public static void TriggerScoreChanged(int score, int combo) => OnScoreChanged?.Invoke(score, combo);

    // Sự kiện thay đổi lượng vàng
    public static event Action<int> OnGoldChanged; // (currentGold)
    public static void TriggerGoldChanged(int gold) => OnGoldChanged?.Invoke(gold);

    // Sự kiện thay đổi trang bị Skin của đĩa bánh
    public static event Action<string> OnSkinEquipped; // (skinId)
    public static void TriggerSkinEquipped(string skinId) => OnSkinEquipped?.Invoke(skinId);

    // Helper lấy Font an toàn tương thích Unity 6 và các bản cũ
    public static Font GetSafeFont()
    {
        Font font = null;
        try
        {
            // Thử LegacyRuntime.ttf trước (chuẩn Unity 6)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch {}

        if (font == null)
        {
            try
            {
                // Thử Arial.ttf (chuẩn các bản Unity cũ)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch {}
        }
        return font;
    }
}
