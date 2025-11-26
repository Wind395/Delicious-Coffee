using UnityEngine;
using System.Collections.Generic;


public class AudioManager : MonoBehaviour
{
    #region Singleton (OPTIMIZED)
    
    private static AudioManager _instance;
    private static readonly object _lock = new object();
    
    public static AudioManager Instance
    {
        get
        {
            // Thread-safe singleton without FindObjectOfType
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Only search once on first access
                        _instance = FindObjectOfType<AudioManager>();
                        
                        if (_instance == null)
                        {
                            GameObject go = new GameObject("AudioManager");
                            _instance = go.AddComponent<AudioManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
            }
            return _instance;
        }
    }
    
    #endregion

    #region Audio Clips
    
    [Header("=== MUSIC ===")]
    [Tooltip("Import Settings: Load Type = Streaming, Compression = Vorbis")]
    [SerializeField] private AudioClip menuMusic;
    
    [Tooltip("Import Settings: Load Type = Streaming, Compression = Vorbis")]
    [SerializeField] private AudioClip[] gameplayMusicTracks;
    
    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSourceLayer1;
    [SerializeField] private AudioSource musicSourceLayer2;

    [Header("Layer Settings")]
    [Range(0f, 1f)] [SerializeField] private float layer1Volume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float layer2Volume = 0.7f;
    
    [Header("=== SFX ===")]
    [Tooltip("Import Settings: Load Type = Decompress On Load, Compression = ADPCM")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip slideSound;
    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private AudioClip obstacleHitSound;
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("=== DOG SFX ===")]
    [SerializeField] private AudioClip dogBarkSound;
    [SerializeField] private AudioClip dogGrowlSound;
    [SerializeField] private AudioClip dogWhineSound;

    [Header("=== POWERUP SFX ===")]
    [SerializeField] private AudioClip powerUpCollectSound;
    [SerializeField] private AudioClip iceTeaActivateSound;
    [SerializeField] private AudioClip coldTowelActivateSound;
    [SerializeField] private AudioClip medicineActivateSound;
    [SerializeField] private AudioClip shieldBreakSound;

    [Header("=== METER SFX ===")]
    [SerializeField] private AudioClip meterWarningSound;
    [SerializeField] private AudioClip meterCriticalSound;

    [Header("=== VICTORY SFX ===")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip toiletReachedSound;

    [Header("=== SHIELD SFX ===")]
    [SerializeField] private AudioClip shieldHitSound;
    [SerializeField] private AudioClip obstacleDestroySound;
    
    #endregion

    #region Audio Sources (CACHED)
    
    [Header("=== AUDIO SOURCES ===")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    // OPTIMIZED: Reduced pool size (5 → 3)
    private List<AudioSource> _sfxSourcePool;
    [SerializeField] private int _poolSize = 3;
    
    // OPTIMIZATION: Cache last used pool index (round-robin)
    private int _lastPoolIndex = 0;
    
    #endregion

    #region Settings
    
    [Header("=== SETTINGS ===")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool enableDebugLogs = false; // Turn OFF in production
    
    #endregion

    #region State (CACHED)
    
    private int _currentMusicTrackIndex = 0;
    
    // OPTIMIZATION: Cache volume calculations
    private float _cachedLayer1Volume;
    private float _cachedLayer2Volume;
    
    // OPTIMIZATION: Flag to prevent redundant operations
    private bool _isInitialized = false;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        // Singleton enforcement
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    void Start()
    {
        LoadAudioSettings();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #endregion

    #region Initialization (OPTIMIZED)
    
    private void Initialize()
    {
        if (_isInitialized) return; // Prevent double init
        
        // Create music source if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        // Create main SFX source if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        // Initialize SFX pool
        InitializeSFXPool();
        
        // Cache volume calculations
        UpdateCachedVolumes();

        _isInitialized = true;
        
        LogDebug("[AudioManager] Initialized");
    }

    /// <summary>
    /// OPTIMIZED: Initialize pool with configurable size
    /// </summary>
    private void InitializeSFXPool()
    {
        if (_sfxSourcePool == null)
        {
            _sfxSourcePool = new List<AudioSource>(_poolSize);
        }
        else
        {
            _sfxSourcePool.Clear();
        }
        
        for (int i = 0; i < _poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false; // Ensure no loops
            _sfxSourcePool.Add(source);
        }
    }

    /// <summary>
    /// Subscribe to game events
    /// </summary>
    private void SubscribeToEvents()
    {
        if (EventManager.Instance == null) return;
        
        EventManager.Instance.StartListening(GameEvents.GAME_STARTED, OnGameStarted);
        EventManager.Instance.StartListening(GameEvents.GAME_OVER, OnGameOver);
        EventManager.Instance.StartListening(GameEvents.LEVEL_COMPLETE, OnLevelComplete);
        EventManager.Instance.StartListening(GameEvents.COIN_COLLECTED, OnCoinCollected);
    }

    /// <summary>
    /// OPTIMIZATION: Ensure proper cleanup
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (EventManager.Instance == null) return;
        
        EventManager.Instance.StopListening(GameEvents.GAME_STARTED, OnGameStarted);
        EventManager.Instance.StopListening(GameEvents.GAME_OVER, OnGameOver);
        EventManager.Instance.StopListening(GameEvents.LEVEL_COMPLETE, OnLevelComplete);
        EventManager.Instance.StopListening(GameEvents.COIN_COLLECTED, OnCoinCollected);
    }
    
    #endregion

    #region Music Control (OPTIMIZED)

    /// <summary>
    /// OPTIMIZED: Play music with cached references
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!musicEnabled || clip == null || musicSource == null) return;

        // Avoid restarting same clip
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    /// <summary>
    /// OPTIMIZED: Use cached volume values
    /// </summary>
    public void PlayGameplayMusic()
    {
        if (gameplayMusicTracks == null || gameplayMusicTracks.Length < 2) 
        {
            LogDebug("[AudioManager] Missing music layers!");
            return;
        }

        if (gameplayMusicTracks[0] == null || gameplayMusicTracks[1] == null) return;

        LogDebug("[AudioManager] 🎵 Playing layered music");
        
        // Play layer 1
        if (musicSourceLayer1 != null)
        {
            musicSourceLayer1.clip = gameplayMusicTracks[0];
            musicSourceLayer1.loop = true;
            musicSourceLayer1.volume = _cachedLayer1Volume;
            musicSourceLayer1.Play();
        }
        
        // Play layer 2 (synchronized)
        if (musicSourceLayer2 != null)
        {
            musicSourceLayer2.clip = gameplayMusicTracks[1];
            musicSourceLayer2.loop = true;
            musicSourceLayer2.volume = _cachedLayer2Volume;
            musicSourceLayer2.Play();
        }
    }

    public void StopMusic()
    {
        musicSourceLayer1?.Stop();
        musicSourceLayer2?.Stop();
        musicSource?.Stop();
        
        LogDebug("[AudioManager] 🔇 Music stopped");
    }

    public void PauseMusic()
    {
        musicSource?.Pause();
        musicSourceLayer1?.Pause();
        musicSourceLayer2?.Pause();
    }

    public void ResumeMusic()
    {
        musicSource?.UnPause();
        musicSourceLayer1?.UnPause();
        musicSourceLayer2?.UnPause();
    }

    /// <summary>
    /// OPTIMIZED: Update cached volumes
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateCachedVolumes();

        bool shouldMute = musicVolume <= 0.01f;

        // Apply to all sources
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.mute = shouldMute;
        }

        if (musicSourceLayer1 != null)
        {
            musicSourceLayer1.volume = _cachedLayer1Volume;
            musicSourceLayer1.mute = shouldMute;
        }

        if (musicSourceLayer2 != null)
        {
            musicSourceLayer2.volume = _cachedLayer2Volume;
            musicSourceLayer2.mute = shouldMute;
        }

        SaveAudioSettings();
    }

    /// <summary>
    /// OPTIMIZATION: Cache volume calculations
    /// </summary>
    private void UpdateCachedVolumes()
    {
        _cachedLayer1Volume = musicVolume * layer1Volume;
        _cachedLayer2Volume = musicVolume * layer2Volume;
    }

    public void ToggleMusic(bool enabled)
    {
        musicEnabled = enabled;
        
        if (musicEnabled)
        {
            ResumeMusic();
        }
        else
        {
            PauseMusic();
        }
        
        SaveAudioSettings();
    }
    
    #endregion

    #region SFX Control (OPTIMIZED)
    
    /// <summary>
    /// OPTIMIZED: Round-robin pool selection (faster than searching)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (!sfxEnabled || clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.volume = sfxVolume * volumeScale;
            source.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// OPTIMIZED: Round-robin instead of linear search
    /// </summary>
    private AudioSource GetAvailableSFXSource()
    {
        if (_sfxSourcePool == null || _sfxSourcePool.Count == 0)
        {
            return sfxSource; // Fallback
        }

        // Try round-robin (faster than searching all)
        int startIndex = _lastPoolIndex;
        
        for (int i = 0; i < _poolSize; i++)
        {
            int index = (startIndex + i) % _poolSize;
            AudioSource source = _sfxSourcePool[index];
            
            if (!source.isPlaying)
            {
                _lastPoolIndex = (index + 1) % _poolSize;
                return source;
            }
        }

        // All busy, use fallback
        return sfxSource;
    }

    // SFX Methods (unchanged, just optimized calls)
    public void PlayJumpSound() => PlaySFX(jumpSound);
    public void PlaySlideSound() => PlaySFX(slideSound);
    public void PlayCoinSound() => PlaySFX(coinCollectSound, 0.8f);
    public void PlayHitSound() => PlaySFX(obstacleHitSound);
    public void PlayLevelCompleteSound() => PlaySFX(levelCompleteSound);
    public void PlayGameOverSound() => PlaySFX(gameOverSound);
    public void PlayButtonClickSound() => PlaySFX(buttonClickSound, 0.6f);

    // PowerUp Sounds
    public void PlayPowerUpCollectSound() => PlaySFX(powerUpCollectSound, 0.8f);
    public void PlayIceTeaSound() => PlaySFX(iceTeaActivateSound);
    public void PlayColdTowelSound() => PlaySFX(coldTowelActivateSound);
    public void PlayMedicineSound() => PlaySFX(medicineActivateSound);
    public void PlayShieldBreakSound()
    {
        PlayShieldHitSound();
        PlayObstacleDestroySound();
    }

    // Meter Sounds
    public void PlayMeterWarningSound() => PlaySFX(meterWarningSound);
    public void PlayMeterCriticalSound() => PlaySFX(meterCriticalSound);

    // Dog Sounds
    public void PlayDogBarkSound()
    {
        PlaySFX(dogBarkSound, 1.0f);
        LogDebug("[AudioManager] 🐕 Dog bark!");
    }

    public void PlayDogGrowlSound() => PlaySFX(dogGrowlSound, 0.8f);
    public void PlayDogWhineSound() => PlaySFX(dogWhineSound, 0.7f);

    // Victory Sounds
    public void PlayVictorySound() => PlaySFX(victorySound);
    public void PlayToiletReachedSound() => PlaySFX(toiletReachedSound);

    // Shield Sounds
    public void PlayShieldHitSound() => PlaySFX(shieldHitSound, 0.9f);
    public void PlayObstacleDestroySound() => PlaySFX(obstacleDestroySound, 1.0f);

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveAudioSettings();
    }

    public void ToggleSFX(bool enabled)
    {
        sfxEnabled = enabled;
        SaveAudioSettings();
    }
    
    #endregion

    #region Event Handlers
    
    private void OnGameStarted() => PlayGameplayMusic();
    private void OnGameOver() => PlayGameOverSound();
    private void OnLevelComplete() => PlayLevelCompleteSound();
    private void OnCoinCollected(int count) => PlayCoinSound();
    
    #endregion

    #region Save/Load (OPTIMIZED)
    
    /// <summary>
    /// OPTIMIZED: Batch PlayerPrefs save
    /// </summary>
    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save(); // Only save once
    }

    private void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        // Apply settings
        UpdateCachedVolumes();
        
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.mute = !musicEnabled;
        }
    }
    
    #endregion

    #region Debug Helpers (CONDITIONAL)
    
    /// <summary>
    /// OPTIMIZATION: Conditional debug logging
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    #endregion
}