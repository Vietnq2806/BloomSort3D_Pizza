using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public class DailyRewardManager : MonoBehaviour
{
    public static DailyRewardManager Instance { get; private set; }

    [Header("Cấu Hình Phần Thưởng")]
    public int dailyRewardGold = 50;

    private const string LastClaimTimeKey = "DailyLastClaimTimeUTC";
    private string _apiURL = "https://worldtimeapi.org/api/timezone/Etc/UTC";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ClaimReward()
    {
        // Sử dụng Coroutine gửi Request kiểm tra giờ mạng online bảo mật chống hack thiết bị
        StartCoroutine(ClaimRewardRoutine());
    }

    private IEnumerator ClaimRewardRoutine()
    {
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1f, "Đang xác thực thời gian UTC...");
        }
        
        // 1. Gửi request lấy giờ UTC chuẩn mạng internet
        using (UnityWebRequest webRequest = UnityWebRequest.Get(_apiURL))
        {
            // Thiết lập timeout ngắn để tránh chờ lâu nếu mất mạng
            webRequest.timeout = 4;
            yield return webRequest.SendWebRequest();

            DateTime currentUtcTime;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Parse dữ liệu Json trả về từ WorldTimeAPI
                try
                {
                    string json = webRequest.downloadHandler.text;
                    // Lấy trường "utc_datetime" từ chuỗi json thô
                    string utcString = GetJsonValue(json, "utc_datetime");
                    currentUtcTime = DateTime.Parse(utcString).ToUniversalTime();
                    Debug.Log($"[Daily Reward] Giờ UTC mạng xác thực thành công: {currentUtcTime}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Daily Reward] Không thể parse giờ mạng: {ex.Message}. Chuyển sang fallback.");
                    currentUtcTime = DateTime.UtcNow;
                }
            }
            else
            {
                // FALLBACK: Không có mạng internet, dùng giờ hệ thống nội bộ nhưng có kiểm tra bảo mật
                Debug.LogWarning("[Daily Reward] Không kết nối được API mạng. Dùng giờ nội bộ fallback.");
                currentUtcTime = DateTime.UtcNow;
            }

            // 2. Thuật toán kiểm tra Anti-Cheat:
            // Tải thời điểm nhận quà lần cuối từ SaveSystem JSON
            string lastClaimStr = (SaveSystem.Instance != null)
                ? SaveSystem.Instance.Data.lastDailyClaimTimeUTC
                : PlayerPrefs.GetString(LastClaimTimeKey, "");
            
            if (!string.IsNullOrEmpty(lastClaimStr))
            {
                DateTime lastClaimTime = DateTime.Parse(lastClaimStr).ToUniversalTime();

                // Kiểm tra 1: Xem người chơi có chỉnh lùi thời gian thiết bị về quá khứ không
                if (currentUtcTime < lastClaimTime)
                {
                    if (ObjectPooler.Instance != null)
                    {
                        ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1.2f, "Lỗi: Thời gian hệ thống không hợp lệ (Đi lùi)!");
                    }
                    Debug.LogWarning("[Anti-Cheat] Phát hiện gian lận: Thời gian đi lùi so với mốc cũ.");
                    yield break;
                }

                // Kiểm tra 2: Đã đủ 24 giờ kể từ lần nhận cuối chưa
                TimeSpan elapsed = currentUtcTime - lastClaimTime;
                if (elapsed.TotalHours < 24)
                {
                    double hoursLeft = 24 - elapsed.TotalHours;
                    int h = (int)hoursLeft;
                    int m = (int)((hoursLeft - h) * 60);
                    
                    if (ObjectPooler.Instance != null)
                    {
                        ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1.2f, $"Hãy đợi thêm {h}h {m}m để điểm danh tiếp!");
                    }
                    Debug.Log($"[Daily Reward] Từ chối: Đợi thêm {hoursLeft:F2} giờ nữa.");
                    yield break;
                }
            }

            // 3. Đạt điều kiện: Phát quà và lưu vết
            GameManager.Instance.AddGold(dailyRewardGold);

            // Lưu timestamp vào SaveSystem JSON (chuẩn ISO 8601)
            string isoTimestamp = currentUtcTime.ToString("o");
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Data.lastDailyClaimTimeUTC = isoTimestamp;
                SaveSystem.Instance.Save();
            }
            else
            {
                PlayerPrefs.SetString(LastClaimTimeKey, isoTimestamp);
                PlayerPrefs.Save();
            }

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.SpawnFloatingText(Vector3.up * 1f, $"+{dailyRewardGold} Vàng Điểm Danh! 🎁");
            }
            
            // Đóng bảng UI sau khi nhận thành công
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenOverlay("None");
            }
        }
    }

    // Hàm phân tích cú pháp JSON đơn giản thủ công để tránh rác GC của thư viện lớn
    private string GetJsonValue(string json, string key)
    {
        string searchKey = "\"" + key + "\":\"";
        int idx = json.IndexOf(searchKey);
        if (idx == -1)
        {
            searchKey = "\"" + key + "\":";
            idx = json.IndexOf(searchKey);
            if (idx == -1) return null;
            
            int start = idx + searchKey.Length;
            int end = json.IndexOf(",", start);
            if (end == -1) end = json.IndexOf("}", start);
            return json.Substring(start, end - start).Trim('"', ' ', '\n', '\r');
        }
        else
        {
            int start = idx + searchKey.Length;
            int end = json.IndexOf("\"", start);
            return json.Substring(start, end - start);
        }
    }
}
