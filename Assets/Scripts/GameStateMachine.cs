using UnityEngine;

// ==========================================
// INTERFACE CHUẨN STATE PATTERN (Tiêu chí nghiệm thu Tuần 2)
// Mỗi trạng thái game là một class riêng biệt, tự quản lý logic Enter/Exit/Tick
// Tuân theo nguyên lý Open/Closed: thêm trạng thái mới không cần sửa GameManager
// ==========================================
public interface IGameState
{
    /// <summary>Kiểu enum tương ứng để bên ngoài nhận diện trạng thái hiện tại</summary>
    GameState StateType { get; }

    /// <summary>Được gọi một lần khi FSM chuyển VÀO trạng thái này</summary>
    void Enter(GameManager gm);

    /// <summary>Được gọi một lần khi FSM rời KHỎI trạng thái này</summary>
    void Exit(GameManager gm);

    /// <summary>Được gọi mỗi frame từ GameManager.Update()</summary>
    void Tick(GameManager gm);
}

// ==========================================
// TRẠNG THÁI 1: SETUP
// Khởi tạo bàn chơi, chờ Grid và Spawner sẵn sàng
// ==========================================
public class SetupState : IGameState
{
    public GameState StateType => GameState.Setup;

    public void Enter(GameManager gm)
    {
        Debug.Log("[FSM] Bắt đầu Setup - Khởi tạo bàn chơi...");
        gm.StartSetupRoutine();
    }

    public void Exit(GameManager gm) { }

    public void Tick(GameManager gm) { }
}

// ==========================================
// TRẠNG THÁI 2: PLAYING
// Người chơi đang tương tác, kéo thả bình thường
// ==========================================
public class PlayingState : IGameState
{
    public GameState StateType => GameState.Playing;

    public void Enter(GameManager gm)
    {
        Debug.Log("[FSM] Đang chơi - Chờ tương tác người chơi.");
    }

    public void Exit(GameManager gm) { }

    public void Tick(GameManager gm)
    {
        // Quản lý đếm ngược combo: combo sẽ reset nếu người chơi không hành động đủ lâu
        gm.TickComboTimer();
    }
}

// ==========================================
// TRẠNG THÁI 3: CHECKING COMBO
// Hệ thống đang quét toàn bộ lưới tìm cơ hội merge/bloom cascade
// ==========================================
public class CheckingComboState : IGameState
{
    public GameState StateType => GameState.CheckingCombo;

    public void Enter(GameManager gm)
    {
        Debug.Log("[FSM] Kiểm tra Combo Cascade...");
        gm.StartCheckCascadeRoutine();
    }

    public void Exit(GameManager gm) { }

    public void Tick(GameManager gm) { }
}

// ==========================================
// TRẠNG THÁI 4: ANIMATING
// Hệ thống đang chạy hoạt cảnh bay miếng bánh / nổ Bloom
// Chặn toàn bộ tương tác của người chơi trong thời gian này
// ==========================================
public class AnimatingState : IGameState
{
    public GameState StateType => GameState.Animating;

    public void Enter(GameManager gm)
    {
        Debug.Log("[FSM] Đang chạy Animation - Chặn Input...");
    }

    public void Exit(GameManager gm) { }

    public void Tick(GameManager gm)
    {
        // Combo timer vẫn tiếp tục đếm trong khi animation chạy
        // để giữ chuỗi combo liên hoàn không bị reset giữa chừng
        gm.TickComboTimer();
    }
}

// ==========================================
// TRẠNG THÁI 5: GAME OVER
// Lưới đầy, không còn nước đi hợp lệ
// ==========================================
public class GameOverState : IGameState
{
    public GameState StateType => GameState.GameOver;

    public void Enter(GameManager gm)
    {
        Debug.Log("[FSM] GAME OVER - Lưới đầy, hết nước đi!");
    }

    public void Exit(GameManager gm) { }

    public void Tick(GameManager gm) { }
}
