using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JSON Data Classes - UPDATED: Added SupportItemData
/// </summary>

[Serializable]
public class SectionLibrary
{
    public Metadata metadata;
    public List<SectionData> sections;
}

[Serializable]
public class Metadata
{
    public string version;
    public int totalSections;
}

[Serializable]
public class SectionData
{
    public string id;
    public string name;
    public float length;
    public int difficulty;
    // ═══ NEW: Safe Zone Flag ═══
    [Tooltip("If true, no obstacles will spawn in this section")]
    public bool isSafeZone;
    public List<ObstacleData> obstacles;
    public List<CoinGroupData> coins;
    public List<SupportItemData> supportItems; // ← NEW
}

[Serializable]
public class ObstacleData
{
    public string type;        // "barrier", "low", "high"
    public int lane;           // 0, 1, 2
    public float zPosition;
    public float yPosition;
}

[Serializable]
public class CoinGroupData
{
    public string pattern;     // CHỈ "vertical_line"
    public int lane;           // 0, 1, 2
    public float zStart;
    public int count;
    public float spacing;
}

/// <summary>
/// Support Item Data - NEW
/// Type sẽ được random khi spawn theo tỉ lệ:
/// Ice Tea: 40%, Cold Towel: 40%, Medicine: 20%
/// </summary>
[Serializable]
public class SupportItemData
{
    public int lane;           // 0, 1, 2
    public float zPosition;    // Vị trí Z trong section
}

[Serializable]
public class SectionLibraryWrapper
{
    public SectionLibrary sectionLibrary;
}