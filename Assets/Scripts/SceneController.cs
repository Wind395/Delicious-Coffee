using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Scene Controller - FIXED: Always reset timeScale before loading
/// </summary>
public class SceneController : MonoBehaviour
{
    #region Singleton
    
    private static SceneController _instance;
    public static SceneController Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SceneController");
                _instance = go.AddComponent<SceneController>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    #endregion

    #region Scene Names - Constants
    
    public const string SCENE_MENU = "Menu";
    public const string SCENE_GAMEPLAY = "Gameplay";
    
    #endregion

    #region State
    
    private string _currentScene;
    private bool _isLoading = false;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        _currentScene = SceneManager.GetActiveScene().name;
    }
    
    #endregion

    #region Scene Loading - FIXED
    
    /// <summary>
    /// Load Menu Scene - FIXED: Force reset timeScale
    /// </summary>
    public void LoadMenuScene()
    {
        if (_isLoading) return;
        
        Debug.Log("[SceneController] Loading Menu Scene...");
        
        // ← FIX: CRITICAL - Reset timeScale IMMEDIATELY
        Time.timeScale = 1f;
        
        StartCoroutine(LoadSceneAsync(SCENE_MENU));
    }

    /// <summary>
    /// Load Gameplay Scene - FIXED: Force reset timeScale
    /// </summary>
    public void LoadGameplayScene()
    {
        if (_isLoading) return;
        
        Debug.Log("[SceneController] Loading Gameplay Scene...");
        
        // ← FIX: CRITICAL - Reset timeScale IMMEDIATELY
        Time.timeScale = 1f;
        
        StartCoroutine(LoadSceneAsync(SCENE_GAMEPLAY));
    }

    /// <summary>
    /// Reload current scene - FIXED
    /// </summary>
    public void ReloadCurrentScene()
    {
        if (_isLoading) return;
        
        Debug.Log("[SceneController] Reloading Current Scene...");
        
        // ← FIX: CRITICAL - Reset timeScale IMMEDIATELY
        Time.timeScale = 1f;
        
        StartCoroutine(LoadSceneAsync(_currentScene));
    }

    /// <summary>
    /// Load scene asynchronously - FIXED: Enable input after gameplay load
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        _isLoading = true;
        
        // Reset timeScale
        Time.timeScale = 1f;
        
        Debug.Log($"[SceneController] ▶ Loading scene: {sceneName} (TimeScale: {Time.timeScale})");
        
        // Cleanup before loading
        CleanupBeforeSceneLoad();
        
        // Small delay for UI feedback
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Load scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wait until loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        _currentScene = sceneName;
        
        // ← FIX: Enable input after gameplay scene loads
        if (sceneName == SCENE_GAMEPLAY)
        {
            yield return new WaitForSecondsRealtime(0.2f); // Wait for scene init
            
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetEnabled(true);
                InputManager.Instance.ClearInputState();
                Debug.Log("[SceneController] ✓ Input enabled for gameplay scene");
            }
        }
        
        _isLoading = false;
        
        Debug.Log($"[SceneController] ✓ Scene loaded: {sceneName}");
    }

    /// <summary>
    /// Cleanup before loading new scene - FIXED: Ensure timeScale
    /// </summary>
    private void CleanupBeforeSceneLoad()
    {
        // ← FIX: TRIPLE-CHECK timeScale
        Time.timeScale = 1f;
        
        // Clear powerups if leaving gameplay
        if (_currentScene == SCENE_GAMEPLAY)
        {
            PowerUpManager.Instance?.ClearAllPowerUps();
        }
        
        Debug.Log($"[SceneController] Cleanup complete (TimeScale: {Time.timeScale})");
    }
    
    #endregion

    #region Public API
    
    public string GetCurrentScene() => _currentScene;
    public bool IsInGameplay() => _currentScene == SCENE_GAMEPLAY;
    public bool IsInMenu() => _currentScene == SCENE_MENU;
    
    #endregion

    #region Quick Actions
    
    /// <summary>
    /// Quit game
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[SceneController] Quitting game...");
        
        // Reset timeScale before quit
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    #endregion
}