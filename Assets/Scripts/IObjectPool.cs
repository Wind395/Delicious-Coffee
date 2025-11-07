using UnityEngine;

/// <summary>
/// Interface cho Object Pool
/// SOLID: Dependency Inversion - Depend on abstraction
/// Design Pattern: Object Pool
/// </summary>
public interface IObjectPool<T> where T : Component
{
    /// <summary>
    /// Lấy object từ pool
    /// </summary>
    T Get();

    /// <summary>
    /// Trả object về pool
    /// </summary>
    void Return(T obj);

    /// <summary>
    /// Khởi tạo pool với số lượng
    /// </summary>
    void Initialize(int size);

    /// <summary>
    /// Clear pool
    /// </summary>
    void Clear();

    /// <summary>
    /// Số lượng object available
    /// </summary>
    int AvailableCount { get; }
}

/// <summary>
/// Interface cho objects có thể pool
/// SOLID: Interface Segregation
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Gọi khi object được lấy từ pool
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// Gọi khi object được trả về pool
    /// </summary>
    void OnDespawn();

    /// <summary>
    /// GameObject của object này
    /// </summary>
    GameObject GameObject { get; }
}