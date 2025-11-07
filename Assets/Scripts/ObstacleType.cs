using UnityEngine;

/// <summary>
/// Obstacle Type Definition
/// SOLID: Single Responsibility - Type definitions only
/// </summary>
public enum ObstacleCategory
{
    Barrier,    // Generic barrier (can be fence, wall, etc.)
    Low,        // Low obstacles (can jump over)
    High        // High obstacles (must slide under)
}

/// <summary>
/// Specific obstacle types with behavior
/// </summary>
public enum ObstacleType
{
    // ═══ DEADLY OBSTACLES (Instant Game Over) ═══
    Car,            // Ô tô
    Motorcycle,     // Xe máy
    Fence,          // Hàng rào
    
    // ═══ SLOW OBSTACLES (Reduce Speed) ═══
    StreetVendor,   // Hàng rong
    TrashCan,       // Thùng rác
    Dog,            // Chó
    
    // ═══ GENERIC (Default behavior - Game Over) ═══
    GenericBarrier, // Generic obstacle
    GenericLow,
    GenericHigh
}

/// <summary>
/// Obstacle Behavior Type
/// </summary>
public enum ObstacleBehavior
{
    Deadly,     // Instant game over (if no protection)
    Slow        // Reduce player speed temporarily
}

/// <summary>
/// Obstacle Type Data - Maps type to behavior
/// </summary>
[System.Serializable]
public class ObstacleTypeData
{
    public ObstacleType type;
    public ObstacleBehavior behavior;
    public string displayName;
    
    [Header("Slow Effect Settings (if behavior = Slow)")]
    [Tooltip("Speed multiplier when hit (0.5 = 50% speed)")]
    [Range(0.1f, 1f)]
    public float slowMultiplier = 0.5f;
    
    [Tooltip("Slow duration in seconds")]
    public float slowDuration = 2f;
}

/// <summary>
/// Obstacle Type Database - ScriptableObject
/// Manages all obstacle type configurations
/// FIXED: Proper default initialization
/// </summary>
[CreateAssetMenu(fileName = "ObstacleTypeDatabase", menuName = "Game/Obstacle Type Database")]
public class ObstacleTypeDatabase : ScriptableObject
{
    [Header("Obstacle Type Definitions")]
    public ObstacleTypeData[] obstacleTypes;

    /// <summary>
    /// Initialize default values - Called in Inspector or OnValidate
    /// </summary>
    void OnValidate()
    {
        // Auto-initialize if empty
        if (obstacleTypes == null || obstacleTypes.Length == 0)
        {
            InitializeDefaultTypes();
        }
    }

    /// <summary>
    /// Initialize with default obstacle types
    /// </summary>
    [ContextMenu("Initialize Default Types")]
    public void InitializeDefaultTypes()
    {
        obstacleTypes = new ObstacleTypeData[]
        {
            // ═══════════════════════════════════════════
            // DEADLY OBSTACLES
            // ═══════════════════════════════════════════
            new ObstacleTypeData 
            { 
                type = ObstacleType.Car, 
                behavior = ObstacleBehavior.Deadly,
                displayName = "Car (Ô tô)",
                slowMultiplier = 0f,
                slowDuration = 0f
            },
            new ObstacleTypeData 
            { 
                type = ObstacleType.Motorcycle, 
                behavior = ObstacleBehavior.Deadly,
                displayName = "Motorcycle (Xe máy)",
                slowMultiplier = 0f,
                slowDuration = 0f
            },
            new ObstacleTypeData 
            { 
                type = ObstacleType.Fence, 
                behavior = ObstacleBehavior.Deadly,
                displayName = "Fence (Hàng rào)",
                slowMultiplier = 0f,
                slowDuration = 0f
            },
            new ObstacleTypeData 
            { 
                type = ObstacleType.GenericBarrier, 
                behavior = ObstacleBehavior.Deadly,
                displayName = "Generic Barrier",
                slowMultiplier = 0f,
                slowDuration = 0f
            },
            new ObstacleTypeData 
            { 
                type = ObstacleType.GenericLow, 
                behavior = ObstacleBehavior.Deadly,
                displayName = "Generic Low",
                slowMultiplier = 0f,
                slowDuration = 0f
            },
            new ObstacleTypeData 
            { 
                type = ObstacleType.GenericHigh, 
                behavior = ObstacleBehavior.Deadly,
                displayName = "Generic High",
                slowMultiplier = 0f,
                slowDuration = 0f
            },
            
            // ═══════════════════════════════════════════
            // SLOW OBSTACLES - FIXED
            // ═══════════════════════════════════════════
            new ObstacleTypeData 
            { 
                type = ObstacleType.StreetVendor, 
                behavior = ObstacleBehavior.Slow,
                displayName = "Street Vendor (Hàng rong)",
                slowMultiplier = 0.6f,
                slowDuration = 2f
            },
            new ObstacleTypeData 
            { 
                type = ObstacleType.TrashCan, 
                behavior = ObstacleBehavior.Slow,
                displayName = "Trash Can (Thùng rác)",
                slowMultiplier = 0.7f,
                slowDuration = 1.5f
            },
            new ObstacleTypeData 
            { 
                type = ObstacleType.Dog, 
                behavior = ObstacleBehavior.Slow,
                displayName = "Dog (Chó)",
                slowMultiplier = 0.5f,
                slowDuration = 2.5f
            }
        };
        
        Debug.Log("[ObstacleTypeDatabase] ✓ Initialized with default types");
    }
    
    /// <summary>
    /// Get obstacle type data
    /// </summary>
    public ObstacleTypeData GetTypeData(ObstacleType type)
    {
        if (obstacleTypes == null || obstacleTypes.Length == 0)
        {
            Debug.LogError("[ObstacleTypeDatabase] Database is empty! Initializing defaults...");
            InitializeDefaultTypes();
        }

        foreach (var data in obstacleTypes)
        {
            if (data.type == type)
            {
                Debug.Log($"[ObstacleTypeDatabase] Found {type}: Behavior={data.behavior}, Multiplier={data.slowMultiplier}, Duration={data.slowDuration}");
                return data;
            }
        }
        
        // Default: Deadly
        Debug.LogWarning($"[ObstacleTypeDatabase] Type {type} not found! Returning default Deadly behavior");
        return new ObstacleTypeData 
        { 
            type = type, 
            behavior = ObstacleBehavior.Deadly,
            displayName = type.ToString(),
            slowMultiplier = 0f,
            slowDuration = 0f
        };
    }
    
    /// <summary>
    /// Check if obstacle is deadly
    /// </summary>
    public bool IsDeadly(ObstacleType type)
    {
        return GetTypeData(type).behavior == ObstacleBehavior.Deadly;
    }

    /// <summary>
    /// Check if obstacle is slow
    /// </summary>
    public bool IsSlow(ObstacleType type)
    {
        return GetTypeData(type).behavior == ObstacleBehavior.Slow;
    }

    /// <summary>
    /// Print all types to console
    /// </summary>
    [ContextMenu("Print All Types")]
    public void PrintAllTypes()
    {
        if (obstacleTypes == null || obstacleTypes.Length == 0)
        {
            Debug.LogWarning("[ObstacleTypeDatabase] Database is empty!");
            return;
        }

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("OBSTACLE TYPE DATABASE");
        Debug.Log("═══════════════════════════════════════════");
        
        foreach (var data in obstacleTypes)
        {
            string behaviorStr = data.behavior == ObstacleBehavior.Deadly ? "💀 DEADLY" : "🐌 SLOW";
            Debug.Log($"{behaviorStr} | {data.type} ({data.displayName})");
            
            if (data.behavior == ObstacleBehavior.Slow)
            {
                Debug.Log($"   → Speed: {data.slowMultiplier * 100:F0}%, Duration: {data.slowDuration}s");
            }
        }
        
        Debug.Log("═══════════════════════════════════════════");
    }
}