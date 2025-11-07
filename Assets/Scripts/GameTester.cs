#if UNITY_EDITOR

using UnityEngine;

public class GameTester : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private bool enableTestKeys = true;
    
    [Header("PowerUp Test")]
    [SerializeField] private bool showPowerUpTimers = true;

    void Update()
    {
        if (!enableTestKeys) return;

        // Test Ice Tea multiple times
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PowerUpManager.Instance?.ActivatePowerUp<IceTeaPowerUp>();
            Debug.Log("🧊 [Test] Ice Tea activated/refreshed");
            LogPowerUpTimer<IceTeaPowerUp>();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PowerUpManager.Instance?.ActivatePowerUp<ColdTowelPowerUp>();
            Debug.Log("❄️ [Test] Cold Towel activated/refreshed");
            LogPowerUpTimer<ColdTowelPowerUp>();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PowerUpManager.Instance?.ActivatePowerUp<MedicinePowerUp>();
            Debug.Log("💊 [Test] Medicine activated/refreshed");
            LogPowerUpTimer<MedicinePowerUp>();
        }

        // Quick test: Activate Ice Tea twice rapidly
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("═══ RAPID ICE TEA TEST ═══");
            PowerUpManager.Instance?.ActivatePowerUp<IceTeaPowerUp>();
            Debug.Log($"First activation - Timer: {GetPowerUpTimer<IceTeaPowerUp>()}s");
            
            // Wait a frame then activate again
            StartCoroutine(TestRapidActivation());
        }
    }

    /// <summary>
    /// Test rapid activation (simulate collecting 2 Ice Teas quickly)
    /// </summary>
    private System.Collections.IEnumerator TestRapidActivation()
    {
        yield return new WaitForSeconds(0.5f); // Wait 0.5s
        
        Debug.Log("─── Activating Ice Tea again after 0.5s ───");
        PowerUpManager.Instance?.ActivatePowerUp<IceTeaPowerUp>();
        
        float timer = GetPowerUpTimer<IceTeaPowerUp>();
        Debug.Log($"Second activation - Timer: {timer}s");
        
        // Verify timer reset
        if (Mathf.Abs(timer - 2f) < 0.6f) // Should be ~2s (full duration)
        {
            Debug.Log("✓ PASS: Timer properly reset!");
        }
        else
        {
            Debug.LogError($"✗ FAIL: Timer not reset! Expected ~2s, got {timer}s");
        }
    }

    /// <summary>
    /// Log powerup timer
    /// </summary>
    private void LogPowerUpTimer<T>() where T : PowerUpBase
    {
        if (!showPowerUpTimers) return;

        T powerUp = PowerUpManager.Instance?.GetActivePowerUp<T>();
        
        if (powerUp != null)
        {
            Debug.Log($"[Timer] {typeof(T).Name}: {powerUp.TimeRemaining:F2}s / {powerUp.Duration}s");
        }
        else
        {
            Debug.Log($"[Timer] {typeof(T).Name}: Not active");
        }
    }

    /// <summary>
    /// Get powerup timer (for verification)
    /// </summary>
    private float GetPowerUpTimer<T>() where T : PowerUpBase
    {
        T powerUp = PowerUpManager.Instance?.GetActivePowerUp<T>();
        return powerUp != null ? powerUp.TimeRemaining : 0f;
    }

    // void OnGUI()
    // {
    //     if (!enableTestKeys) return;

    //     GUIStyle style = new GUIStyle();
    //     style.fontSize = 14;
    //     style.normal.textColor = Color.white;
    //     style.normal.background = MakeBackgroundTexture(Color.black);

    //     int y = 10;
    //     int lineHeight = 20;
        
    //     GUI.Label(new Rect(10, y, 500, 25), "=== POWERUP TEST CONTROLS ===", style);
    //     y += 30;
        
    //     GUI.Label(new Rect(10, y, 500, lineHeight), "1: Ice Tea (Press multiple times to test refresh)", style);
    //     y += lineHeight;
        
    //     GUI.Label(new Rect(10, y, 500, lineHeight), "2: Cold Towel (Press multiple times to test refresh)", style);
    //     y += lineHeight;
        
    //     GUI.Label(new Rect(10, y, 500, lineHeight), "3: Medicine (Press multiple times to test refresh)", style);
    //     y += lineHeight;
        
    //     GUI.Label(new Rect(10, y, 500, lineHeight), "Q: Rapid Ice Tea Test (Auto test timer reset)", style);
    //     y += 30;

    //     // Show active powerup timers
    //     if (showPowerUpTimers)
    //     {
    //         style.normal.textColor = Color.cyan;
    //         GUI.Label(new Rect(10, y, 500, 25), "=== ACTIVE POWERUPS ===", style);
    //         y += 25;

    //         ShowPowerUpTimer<IceTeaPowerUp>("Ice Tea", ref y, style);
    //         ShowPowerUpTimer<ColdTowelPowerUp>("Cold Towel", ref y, style);
    //         ShowPowerUpTimer<MedicinePowerUp>("Medicine", ref y, style);
    //     }
    // }

    /// <summary>
    /// Display powerup timer in GUI
    /// </summary>
    private void ShowPowerUpTimer<T>(string name, ref int y, GUIStyle style) where T : PowerUpBase
    {
        T powerUp = PowerUpManager.Instance?.GetActivePowerUp<T>();
        
        if (powerUp != null && powerUp.IsActive)
        {
            style.normal.textColor = Color.green;
            float remaining = powerUp.TimeRemaining;
            float duration = powerUp.Duration;
            float percent = (remaining / duration) * 100f;
            
            GUI.Label(new Rect(10, y, 500, 20), 
                $"{name}: {remaining:F1}s / {duration:F1}s ({percent:F0}%)", style);
        }
        else
        {
            style.normal.textColor = Color.gray;
            GUI.Label(new Rect(10, y, 500, 20), $"{name}: Inactive", style);
        }
        
        y += 20;
    }

    private Texture2D MakeBackgroundTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(color.r, color.g, color.b, 0.7f));
        texture.Apply();
        return texture;
    }
}

#endif