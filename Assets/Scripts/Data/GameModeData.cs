using UnityEngine;

/// <summary>
/// Game Mode Enum
/// </summary>
public enum GameMode
{
    Level,
    Endless
}

/// <summary>
/// Game Mode Data
/// </summary>
[System.Serializable]
public class GameModeData
{
    public GameMode mode;
    public string modeName;
    public string description;
    public Sprite icon;
}