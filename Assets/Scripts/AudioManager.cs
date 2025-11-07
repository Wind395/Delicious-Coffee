using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Audio Manager - Centralized audio control
/// SOLID: Single Responsibility - Audio only
/// Design Pattern: Singleton + Object Pool for audio sources
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Singleton
    
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }
    
    #endregion

    #region Audio Clips
    
    [Header("=== MUSIC ===")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip[] gameplayMusicTracks;
    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSourceLayer1;
    [SerializeField] private AudioSource musicSourceLayer2;

    [Header("Layer Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float layer1Volume = 0.7f;
    
    [Range(0f, 1f)]
    [SerializeField] private float layer2Volume = 0.7f;
    
    [Header("=== SFX ===")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip slideSound;
    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private AudioClip obstacleHitSound;
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip buttonClickSound;

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

    [Header("=== FART SFX ===")]
    [Tooltip("Fart sound after drinking (light/normal)")]
    [SerializeField] private AudioClip fartDrinkingSound;
    
    [Tooltip("Fart sound on death (loud/embarrassing)")]
    [SerializeField] private AudioClip fartDeathSound;
    
    [Tooltip("Fart sound on toilet (relief/long)")]
    [SerializeField] private AudioClip fartToiletSound;
    
    [Header("Fart Sound Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float fartVolume = 0.8f;
    
    [Tooltip("Random pitch variation")]
    [SerializeField] private float fartPitchVariation = 0.2f;
    
    #endregion

    #region Audio Sources
    
    [Header("=== AUDIO SOURCES ===")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    // Pool of audio sources for multiple SFX
    private List<AudioSource> _sfxSourcePool = new List<AudioSource>();
    private int _poolSize = 5;
    
    #endregion

    #region Settings
    
    [Header("=== SETTINGS ===")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.7f;
    
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;
    
    #endregion

    #region State
    
    private int _currentMusicTrackIndex = 0;
    
    #endregion

    #region Unity Lifecycle
    
    void Awake()
    {
        // Singleton setup
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
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize audio system
    /// </summary>
    private void Initialize()
    {
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

        Debug.Log("[AudioManager] Initialized");
    }

    /// <summary>
    /// Initialize pool of audio sources for overlapping SFX
    /// Design Pattern: Object Pool
    /// </summary>
    private void InitializeSFXPool()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxSourcePool.Add(source);
        }
    }

    /// <summary>
    /// Subscribe to game events
    /// </summary>
    private void SubscribeToEvents()
    {
        EventManager.Instance.StartListening(GameEvents.GAME_STARTED, OnGameStarted);
        EventManager.Instance.StartListening(GameEvents.GAME_OVER, OnGameOver);
        EventManager.Instance.StartListening(GameEvents.LEVEL_COMPLETE, OnLevelComplete);
        EventManager.Instance.StartListening(GameEvents.COIN_COLLECTED, OnCoinCollected);
    }

    #endregion


    #region PowerUp Sounds

    public void PlayPowerUpCollectSound()
    {
        PlaySFX(powerUpCollectSound, 0.8f);
    }

    public void PlayIceTeaSound()
    {
        PlaySFX(iceTeaActivateSound);
    }

    public void PlayColdTowelSound()
    {
        PlaySFX(coldTowelActivateSound);
    }

    public void PlayMedicineSound()
    {
        PlaySFX(medicineActivateSound);
    }

    public void PlayShieldBreakSound()
    {
        PlayShieldHitSound(); // Use shield hit sound
        PlayObstacleDestroySound(); // Add explosion sound
    }

    #endregion

    #region Meter Sounds

    public void PlayMeterWarningSound()
    {
        PlaySFX(meterWarningSound);
    }

    public void PlayMeterCriticalSound()
    {
        PlaySFX(meterCriticalSound);
    }

    #endregion

    #region Victory Sounds

    public void PlayVictorySound()
    {
        PlaySFX(victorySound);
    }

    public void PlayToiletReachedSound()
    {
        PlaySFX(toiletReachedSound);
    }

    #endregion


    #region Fart Sounds - NEW

    /// <summary>
    /// Play fart sound after drinking
    /// </summary>
    public void PlayFartDrinking()
    {
        PlayFartSound(fartDrinkingSound, "Drinking Fart");
    }

    /// <summary>
    /// Play fart sound on death
    /// </summary>
    public void PlayFartDeath()
    {
        PlayFartSound(fartDeathSound, "Death Fart");
    }

    /// <summary>
    /// Play fart sound on toilet
    /// </summary>
    public void PlayFartToilet()
    {
        PlayFartSound(fartToiletSound, "Toilet Fart");
    }

    /// <summary>
    /// Core fart sound playback with pitch variation
    /// </summary>
    private void PlayFartSound(AudioClip clip, string debugName)
    {
        if (!sfxEnabled || clip == null)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] {debugName} sound not assigned!");
            }
            return;
        }

        // Get available source from pool
        AudioSource source = GetAvailableSFXSource();
        
        if (source != null)
        {
            source.volume = sfxVolume * fartVolume;
            
            // Random pitch variation for variety
            source.pitch = 1f + Random.Range(-fartPitchVariation, fartPitchVariation);
            
            source.PlayOneShot(clip);
            
            Debug.Log($"[AudioManager] 💨 {debugName} played!");
        }
    }

    #endregion


    #region Music Control

    /// <summary>
    /// Play music track
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!musicEnabled || clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    /// <summary>
    /// Play menu music
    /// </summary>
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    /// <summary>
    /// Play layered gameplay music
    /// </summary>
    public void PlayGameplayMusic()
    {
        if (gameplayMusicTracks[0] == null || gameplayMusicTracks[1] == null)
        {
            Debug.LogWarning("[AudioManager] Missing music layers!");
            return;
        }

        Debug.Log("[AudioManager] 🎵 Playing layered music (2 tracks)");
        
        // Play layer 1
        musicSourceLayer1.clip = gameplayMusicTracks[0];
        musicSourceLayer1.loop = true;
        musicSourceLayer1.volume = musicVolume * layer1Volume;
        musicSourceLayer1.Play();
        
        // Play layer 2 (synchronized)
        musicSourceLayer2.clip = gameplayMusicTracks[1];
        musicSourceLayer2.loop = true;
        musicSourceLayer2.volume = musicVolume * layer2Volume;
        musicSourceLayer2.Play();
    }

    /// <summary>
    /// Play next music track
    /// </summary>
    public void PlayNextMusicTrack()
    {
        if (gameplayMusicTracks == null || gameplayMusicTracks.Length == 0)
            return;

        _currentMusicTrackIndex = (_currentMusicTrackIndex + 1) % gameplayMusicTracks.Length;
        PlayMusic(gameplayMusicTracks[_currentMusicTrackIndex]);
    }

    /// <summary>
    /// Stop music
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>
    /// Pause music
    /// </summary>
    public void PauseMusic()
    {
        musicSource.Pause();
    }

    /// <summary>
    /// Resume music
    /// </summary>
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    /// <summary>
    /// Set music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
        SaveAudioSettings();
    }

    /// <summary>
    /// Toggle music on/off
    /// </summary>
    public void ToggleMusic(bool enabled)
    {
        musicEnabled = enabled;
        
        if (musicEnabled)
        {
            musicSource.UnPause();
        }
        else
        {
            musicSource.Pause();
        }
        
        SaveAudioSettings();
    }
    
    #endregion

    #region SFX Control
    
    /// <summary>
    /// Play sound effect
    /// KISS: Simple one-shot playback
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (!sfxEnabled || clip == null) return;

        // Get available source from pool
        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.volume = sfxVolume * volumeScale;
            source.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Get available audio source from pool
    /// </summary>
    private AudioSource GetAvailableSFXSource()
    {
        // Find non-playing source
        foreach (AudioSource source in _sfxSourcePool)
        {
            if (!source.isPlaying)
                return source;
        }

        // All sources busy, use main SFX source
        return sfxSource;
    }

    /// <summary>
    /// Play jump sound
    /// </summary>
    public void PlayJumpSound()
    {
        PlaySFX(jumpSound);
    }

    /// <summary>
    /// Play slide sound
    /// </summary>
    public void PlaySlideSound()
    {
        PlaySFX(slideSound);
    }

    /// <summary>
    /// Play coin collect sound
    /// </summary>
    public void PlayCoinSound()
    {
        PlaySFX(coinCollectSound, 0.8f);
    }

    /// <summary>
    /// Play obstacle hit sound
    /// </summary>
    public void PlayHitSound()
    {
        PlaySFX(obstacleHitSound);
    }

    /// <summary>
    /// Play level complete sound
    /// </summary>
    public void PlayLevelCompleteSound()
    {
        PlaySFX(levelCompleteSound);
    }

    /// <summary>
    /// Play game over sound
    /// </summary>
    public void PlayGameOverSound()
    {
        PlaySFX(gameOverSound);
    }

    /// <summary>
    /// Play button click sound
    /// </summary>
    public void PlayButtonClickSound()
    {
        PlaySFX(buttonClickSound, 0.6f);
    }

    /// <summary>
    /// Set SFX volume
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveAudioSettings();
    }

    /// <summary>
    /// Toggle SFX on/off
    /// </summary>
    public void ToggleSFX(bool enabled)
    {
        sfxEnabled = enabled;
        SaveAudioSettings();
    }

    public void PlayShieldHitSound()
    {
        PlaySFX(shieldHitSound, 0.9f);
    }

    public void PlayObstacleDestroySound()
    {
        PlaySFX(obstacleDestroySound, 1.0f);
    }
    
    #endregion

    #region Event Handlers
    
    /// <summary>
    /// Handle game started event
    /// </summary>
    private void OnGameStarted()
    {
        PlayGameplayMusic();
    }

    /// <summary>
    /// Handle game over event
    /// </summary>
    private void OnGameOver()
    {
        PlayGameOverSound();
    }

    /// <summary>
    /// Handle level complete event
    /// </summary>
    private void OnLevelComplete()
    {
        PlayLevelCompleteSound();
    }

    /// <summary>
    /// Handle coin collected event
    /// </summary>
    private void OnCoinCollected(int count)
    {
        PlayCoinSound();
    }
    
    #endregion

    #region Save/Load Settings
    
    /// <summary>
    /// Save audio settings to PlayerPrefs
    /// </summary>
    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load audio settings from PlayerPrefs
    /// </summary>
    private void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;

        // Apply loaded settings
        musicSource.volume = musicVolume;
        
        if (!musicEnabled)
        {
            musicSource.mute = true;
        }
    }
    
    #endregion

    #region Cleanup
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (EventManager.Instance != null)
        {
            EventManager.Instance.StopListening(GameEvents.GAME_STARTED, OnGameStarted);
            EventManager.Instance.StopListening(GameEvents.GAME_OVER, OnGameOver);
            EventManager.Instance.StopListening(GameEvents.LEVEL_COMPLETE, OnLevelComplete);
        }
    }
    
    #endregion
}