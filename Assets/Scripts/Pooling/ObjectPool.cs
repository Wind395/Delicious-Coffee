using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic Object Pool Implementation
/// SOLID: Single Responsibility - Chỉ quản lý pooling
/// Design Pattern: Object Pool - Tái sử dụng objects
/// KISS: Simple, reusable
/// </summary>
public class ObjectPool<T> : IObjectPool<T> where T : Component, IPoolable
{
    #region Fields

    // Prefab để clone
    private readonly T _prefab;
    
    // Parent transform để organize hierarchy
    private readonly Transform _parent;
    
    // Queue chứa objects available
    // KISS: Queue - FIFO, đơn giản
    private readonly Queue<T> _availableObjects;
    
    // List chứa tất cả objects (để cleanup)
    private readonly List<T> _allObjects;

    #endregion

    #region Properties

    public int AvailableCount => _availableObjects.Count;
    public int TotalCount => _allObjects.Count;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prefab">Prefab để pool</param>
    /// <param name="parent">Parent transform</param>
    public ObjectPool(T prefab, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;
        _availableObjects = new Queue<T>();
        _allObjects = new List<T>();
    }

    #endregion

    #region IObjectPool Implementation

    /// <summary>
    /// Khởi tạo pool với số lượng ban đầu
    /// </summary>
    public void Initialize(int size)
    {
        // Pre-allocate objects để tránh lag khi spawn
        for (int i = 0; i < size; i++)
        {
            T obj = CreateNewObject();
            obj.GameObject.SetActive(false);
            _availableObjects.Enqueue(obj);
        }
        
        Debug.Log($"[ObjectPool] Initialized pool for {_prefab.name} with {size} objects");
    }

    /// <summary>
    /// Lấy object từ pool
    /// </summary>
    public T Get()
    {
        T obj;

        // Nếu pool còn object, lấy ra
        if (_availableObjects.Count > 0)
        {
            obj = _availableObjects.Dequeue();
        }
        else
        {
            // Nếu hết, tạo mới
            obj = CreateNewObject();
            Debug.LogWarning($"[ObjectPool] Pool empty, created new {_prefab.name}");
        }

        // Activate và gọi OnSpawn
        obj.GameObject.SetActive(true);
        obj.OnSpawn();

        return obj;
    }

    /// <summary>
    /// Trả object về pool
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;

        // Gọi OnDespawn
        obj.OnDespawn();

        // Deactivate
        obj.GameObject.SetActive(false);

        // Đưa về pool
        _availableObjects.Enqueue(obj);
    }

    /// <summary>
    /// Clear pool - Destroy tất cả objects
    /// </summary>
    public void Clear()
    {
        foreach (T obj in _allObjects)
        {
            if (obj != null && obj.GameObject != null)
            {
                Object.Destroy(obj.GameObject);
            }
        }

        _availableObjects.Clear();
        _allObjects.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Tạo object mới
    /// </summary>
    private T CreateNewObject()
    {
        // Instantiate từ prefab
        T obj = Object.Instantiate(_prefab, _parent);
        
        // Đặt tên để dễ debug
        obj.GameObject.name = $"{_prefab.name}_{_allObjects.Count}";
        
        // Thêm vào list
        _allObjects.Add(obj);

        return obj;
    }

    #endregion
}