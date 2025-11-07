using UnityEngine;

/// <summary>
/// Game State Pattern - Quản lý các trạng thái game
/// SOLID: Single Responsibility - Chỉ định nghĩa states
/// </summary>
public enum GameState
{
    MainMenu,    // Màn hình menu chính
    Playing,     // Đang chơi
    Paused,      // Tạm dừng
    GameOver,    // Thua
    LevelComplete, // Hoàn thành level
    Victory      // Thắng tất cả level
}

/// <summary>
/// Interface cho các state handlers (Strategy Pattern)
/// SOLID: Interface Segregation - Interface nhỏ, focused
/// </summary>
public interface IGameState
{
    void Enter();  // Khi vào state
    void Exit();   // Khi thoát state
    void Update(); // Update mỗi frame
}