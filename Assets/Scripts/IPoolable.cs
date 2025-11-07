using UnityEngine;

/// <summary>
/// Base class cho poolable objects
/// SOLID: Liskov Substitution - Base class cho inheritance
/// </summary>
public abstract class PoolableObject : MonoBehaviour, IPoolable
{
    public GameObject GameObject => gameObject;

    /// <summary>
    /// Virtual method - Override nếu cần custom logic
    /// SOLID: Open/Closed - Mở cho extension
    /// </summary>
    public virtual void OnSpawn()
    {
        // Default: reset position, rotation
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public virtual void OnDespawn()
    {
        // Default: nothing
    }

    /// <summary>
    /// Helper method để validate object có nên return pool không
    /// </summary>
    protected bool ShouldReturnToPool(Transform player, float threshold = 20f)
    {
        if (player == null) return false;
        return transform.position.z < player.position.z - threshold;
    }
}