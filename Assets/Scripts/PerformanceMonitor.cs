using UnityEngine;

/// <summary>
/// Performance Monitor - Track FPS and performance metrics
/// Development tool - Remove in production build
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    #region Settings
    
    [Header("=== SETTINGS ===")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showMemory = true;
    [SerializeField] private float updateInterval = 0.5f;
    
    #endregion

    #region Stats
    
    private float _fps;
    private float _ms;
    private float _deltaTime;
    private float _updateTimer;
    
    private int _frameCount;
    private float _frameDeltaSum;
    
    #endregion

    #region Unity Lifecycle
    
    void Update()
    {
        // Accumulate frame data
        _frameCount++;
        _frameDeltaSum += Time.unscaledDeltaTime;
        _updateTimer += Time.unscaledDeltaTime;

        // Update stats at interval
        if (_updateTimer >= updateInterval)
        {
            _fps = _frameCount / _updateTimer;
            _ms = (_frameDeltaSum / _frameCount) * 1000f;
            _deltaTime = Time.unscaledDeltaTime;

            // Reset
            _frameCount = 0;
            _frameDeltaSum = 0f;
            _updateTimer = 0f;
        }
    }
    
    #endregion

    #region Helpers
    
    /// <summary>
    /// Get color based on FPS
    /// </summary>
    private Color GetFPSColor()
    {
        if (_fps >= 50f)
            return Color.green;
        else if (_fps >= 30f)
            return Color.yellow;
        else
            return Color.red;
    }
    
    #endregion

    #region Public API
    
    public float GetFPS() => _fps;
    public float GetMS() => _ms;
    
    #endregion
}