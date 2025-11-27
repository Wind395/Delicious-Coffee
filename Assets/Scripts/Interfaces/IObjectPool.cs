using UnityEngine;

/// <summary>
/// Interface cho Object Pool
/// SOLID: Dependency Inversion - Depend on abstraction
/// Design Pattern: Object Pool
/// </summary>
public interface IObjectPool<T> where T : Component
{
    /// Lấy object từ pool
    T Get();
    
    /// Trả object về pool
    void Return(T obj);

    /// Khởi tạo pool với số lượng
    void Initialize(int size);

    /// Clear pool
    void Clear();

    /// Số lượng object available
    int AvailableCount { get; }
}

/// <summary>
/// Interface cho objects có thể pool
/// SOLID: Interface Segregation
/// </summary>
public interface IPoolable
{
    /// Gọi khi object được lấy từ pool
    void OnSpawn();

    /// Gọi khi object được trả về pool
    void OnDespawn();

    /// GameObject của object này
    GameObject GameObject { get; }
}