// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;

// public class AudioManager : MonoBehaviour
// {
//     [Header("---------- Audio Sources ------------")]
//     [SerializeField] AudioSource musicSource;
//     [SerializeField] AudioSource SFXSource;

//     [Header("---------- Background Music Clips ------------")]
//     public AudioClip homeBackgroundMusic;
//     public AudioClip levelBackgroundMusic;
    
//     [Header("---------- SFX Clips ------------")]
//     public AudioClip die;
//     public AudioClip coin;
//     public AudioClip fuel;
//     public AudioClip buttonClick;
//     public AudioClip victory;
//     public AudioClip error;

//     [Header("---------- Volume Settings ------------")]
//     [Range(0f, 1f)] public float maxMusicVolume = 0.6f;
//     [Range(0f, 1f)] public float maxSFXVolume = 1f;
//     [Range(0f, 1f)] public float maxEngineVolume = 1f;

//     private float currentMusicVolume = 1f;
//     private float currentSFXVolume = 1f;
//     private float currentEngineVolume = 1f;

//     private AudioClip currentBackgroundClip;
    
//     public static AudioManager instance;

//     private void Awake()
//     {
//         if (instance == null)
//         {
//             instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//             return;
//         }
//     }

//     private void Start()
//     {
//         LoadAudioSettings();
//         InitializeAudio();
//     }

//     private void LoadAudioSettings()
//     {
//         currentMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
//         currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
//         currentEngineVolume = PlayerPrefs.GetFloat("EngineVolume", 0.74f);
//     }

//     private void InitializeAudio()
//     {
//         if (SFXSource)
//             SFXSource.volume = currentSFXVolume * maxSFXVolume;

//         PlayBackgroundMusicForCurrentScene();
//     }

//     public void PlayBackgroundMusicForCurrentScene()
//     {
//         string sceneName = SceneManager.GetActiveScene().name.ToLower();
        
//         AudioClip clipToPlay = null;
        
//         if (sceneName.Contains("menu") || sceneName.Contains("home") || sceneName.Contains("main"))
//         {
//             clipToPlay = homeBackgroundMusic;
//         }
//         else if (sceneName.Contains("level") || sceneName.Contains("game"))
//         {
//             clipToPlay = levelBackgroundMusic;
//         }
        
//         if (clipToPlay != null && clipToPlay != currentBackgroundClip)
//         {
//             PlayBackgroundMusic(clipToPlay);
//         }
//     }

//     public void PlayBackgroundMusic(AudioClip clip)
//     {
//         if (musicSource && clip)
//         {
//             currentBackgroundClip = clip;
//             musicSource.clip = clip;
//             musicSource.volume = currentMusicVolume * maxMusicVolume;
//             musicSource.loop = true;
            
//             if (currentMusicVolume > 0f)
//                 musicSource.Play();
//             else
//                 musicSource.Stop();
//         }
//     }

//     public void SetMusicVolume(float volume)
//     {
//         currentMusicVolume = Mathf.Clamp01(volume);
//         if (musicSource)
//         {
//             musicSource.volume = currentMusicVolume * maxMusicVolume;

//             if (currentMusicVolume > 0f && musicSource.clip)
//             {
//                 if (!musicSource.isPlaying)
//                     musicSource.Play();
//             }
//             else
//             {
//                 musicSource.Stop();
//             }
//         }

//         PlayerPrefs.SetFloat("MusicVolume", currentMusicVolume);
//         PlayerPrefs.Save();
        
//         NotifyVolumeSliders();
//     }

//     public void SetSFXVolume(float volume)
//     {
//         currentSFXVolume = Mathf.Clamp01(volume);
//         if (SFXSource)
//             SFXSource.volume = currentSFXVolume * maxSFXVolume;

//         PlayerPrefs.SetFloat("SFXVolume", currentSFXVolume);
//         PlayerPrefs.Save();
        
//         NotifyVolumeSliders();
//     }

//     public void SetEngineVolume(float volume)
//     {
//         currentEngineVolume = Mathf.Clamp01(volume);
//         PlayerPrefs.SetFloat("EngineVolume", currentEngineVolume);
//         PlayerPrefs.Save();

//         UpdateCarEngineVolume();
//         NotifyVolumeSliders();
//     }

//     private void UpdateCarEngineVolume()
//     {
//         CarController carController = FindFirstObjectByType<CarController>();
//         if (carController && carController.EngineSoundS)
//         {
//             carController.EngineSoundS.volume = currentEngineVolume * maxEngineVolume;
//         }
//     }

//     private void NotifyVolumeSliders()
//     {
//         // Find and update all settings menus in the scene
//         SettingsMenu[] settingsMenus = FindObjectsByType<SettingsMenu>(FindObjectsSortMode.None);
//         foreach (SettingsMenu settingsMenu in settingsMenus)
//         {
//             if (settingsMenu != null)
//                 settingsMenu.RefreshAudioSliders();
//         }

//         // Also update HomeManage if it exists
//         HomeManage homeManage = FindFirstObjectByType<HomeManage>();
//         if (homeManage)
//         {
//             homeManage.RefreshAudioSliders();
//         }

//         // Update SettingsLoader if it exists
//         SettingsLoader settingsLoader = FindFirstObjectByType<SettingsLoader>();
//         if (settingsLoader)
//         {
//             settingsLoader.RefreshAudioSettings();
//         }
//     }

//     public float GetMusicVolume() => currentMusicVolume;
//     public float GetSFXVolume() => currentSFXVolume;
//     public float GetEngineVolume() => currentEngineVolume;

//     public void PlaySFX(AudioClip clip)
//     {
//         if (SFXSource && clip && currentSFXVolume > 0f)
//         {
//             SFXSource.PlayOneShot(clip);
//         }
//     }

//     public void PlayCoinSound() => PlaySFX(coin);
//     public void PlayDieSound() => PlaySFX(die);
//     public void PlayFuelSound() => PlaySFX(fuel ? fuel : coin);
//     public void PlayButtonClickSound() => PlaySFX(buttonClick);
//     public void PlayVictorySound() => PlaySFX(victory);
//     public void PlayErrorSound() => PlaySFX(error);

//     private void OnEnable()
//     {
//         SceneManager.sceneLoaded += OnSceneLoaded;
//     }

//     private void OnDisable()
//     {
//         SceneManager.sceneLoaded -= OnSceneLoaded;
//     }

//     private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//     {
//         StartCoroutine(RefreshAudioAfterSceneLoad());
//     }

//     private System.Collections.IEnumerator RefreshAudioAfterSceneLoad()
//     {
//         yield return new WaitForEndOfFrame();
        
//         PlayBackgroundMusicForCurrentScene();
//         UpdateCarEngineVolume();
        
//         yield return new WaitForSeconds(0.1f);
//         NotifyVolumeSliders();
//     }

//     public void ToggleMusic()
//     {
//         SetMusicVolume(currentMusicVolume > 0f ? 0f : 1f);
//     }

//     public void ToggleSFX()
//     {
//         SetSFXVolume(currentSFXVolume > 0f ? 0f : 1f);
//     }
// }


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("---------- Audio Sources ------------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("---------- Background Music Clips ------------")]
    public AudioClip homeBackgroundMusic;
    public AudioClip levelBackgroundMusic;
    
    [Header("---------- SFX Clips ------------")]
    public AudioClip die;
    public AudioClip coin;
    public AudioClip fuel;
    public AudioClip buttonClick;
    public AudioClip victory;
    public AudioClip error;

    [Header("---------- Volume Settings ------------")]
    [Range(0f, 1f)] public float maxMusicVolume = 0.6f;
    [Range(0f, 1f)] public float maxSFXVolume = 1f;
    [Range(0f, 1f)] public float maxEngineVolume = 1f;

    private float currentMusicVolume = 1f;
    private float currentSFXVolume = 1f;
    private float currentEngineVolume = 1f;

    private AudioClip currentBackgroundClip;
    
    public static AudioManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadAudioSettings();
        InitializeAudio();
    }

    private void LoadAudioSettings()
    {
        currentMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        currentEngineVolume = PlayerPrefs.GetFloat("EngineVolume", 0.74f);
    }

    private void InitializeAudio()
    {
        if (SFXSource)
            SFXSource.volume = currentSFXVolume * maxSFXVolume;

        PlayBackgroundMusicForCurrentScene();
    }

    public void PlayBackgroundMusicForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        
        AudioClip clipToPlay = null;
        
        if (sceneName.Contains("menu") || sceneName.Contains("home") || sceneName.Contains("main"))
        {
            clipToPlay = homeBackgroundMusic;
        }
        else if (sceneName.Contains("level") || sceneName.Contains("game"))
        {
            clipToPlay = levelBackgroundMusic;
        }
        
        if (clipToPlay != null && clipToPlay != currentBackgroundClip)
        {
            PlayBackgroundMusic(clipToPlay);
        }
    }

    public void PlayBackgroundMusic(AudioClip clip)
    {
        if (musicSource && clip)
        {
            currentBackgroundClip = clip;
            musicSource.clip = clip;
            musicSource.volume = currentMusicVolume * maxMusicVolume;
            musicSource.loop = true;
            
            if (currentMusicVolume > 0f)
                musicSource.Play();
            else
                musicSource.Stop();
        }
    }

    public void SetMusicVolume(float volume)
    {
        currentMusicVolume = Mathf.Clamp01(volume);
        if (musicSource)
        {
            musicSource.volume = currentMusicVolume * maxMusicVolume;

            if (currentMusicVolume > 0f && musicSource.clip)
            {
                if (!musicSource.isPlaying)
                    musicSource.Play();
            }
            else
            {
                musicSource.Stop();
            }
        }

        PlayerPrefs.SetFloat("MusicVolume", currentMusicVolume);
        PlayerPrefs.Save();
        
        NotifyVolumeSliders();
    }

    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = Mathf.Clamp01(volume);
        if (SFXSource)
            SFXSource.volume = currentSFXVolume * maxSFXVolume;

        PlayerPrefs.SetFloat("SFXVolume", currentSFXVolume);
        PlayerPrefs.Save();
        
        NotifyVolumeSliders();
    }

    public void SetEngineVolume(float volume)
    {
        currentEngineVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("EngineVolume", currentEngineVolume);
        PlayerPrefs.Save();

        UpdateCarEngineVolume();
        NotifyVolumeSliders();
    }

    private void UpdateCarEngineVolume()
    {
        CarController carController = FindFirstObjectByType<CarController>();
        if (carController && carController.EngineSoundS)
        {
            carController.EngineSoundS.volume = currentEngineVolume * maxEngineVolume;
        }
    }

    private void NotifyVolumeSliders()
    {
        // Find and update all settings menus in the scene
        SettingsMenu[] settingsMenus = FindObjectsByType<SettingsMenu>(FindObjectsSortMode.None);
        foreach (SettingsMenu settingsMenu in settingsMenus)
        {
            if (settingsMenu != null)
                settingsMenu.RefreshAudioSliders();
        }

        // Update HomeManage if it exists
        HomeManage homeManage = FindFirstObjectByType<HomeManage>();
        if (homeManage)
        {
            homeManage.RefreshAudioSliders();
        }

        // Update GameManager if it exists (NEW!)
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager)
        {
            gameManager.RefreshAudioSliders();
        }

        // Update SettingsLoader if it exists
        SettingsLoader settingsLoader = FindFirstObjectByType<SettingsLoader>();
        if (settingsLoader)
        {
            settingsLoader.RefreshAudioSettings();
        }
    }

    public float GetMusicVolume() => currentMusicVolume;
    public float GetSFXVolume() => currentSFXVolume;
    public float GetEngineVolume() => currentEngineVolume;

    public void PlaySFX(AudioClip clip)
    {
        if (SFXSource && clip && currentSFXVolume > 0f)
        {
            SFXSource.PlayOneShot(clip);
        }
    }

    public void PlayCoinSound() => PlaySFX(coin);
    public void PlayDieSound() => PlaySFX(die);
    public void PlayFuelSound() => PlaySFX(fuel ? fuel : coin);
    public void PlayButtonClickSound() => PlaySFX(buttonClick);
    public void PlayVictorySound() => PlaySFX(victory);
    public void PlayErrorSound() => PlaySFX(error);

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RefreshAudioAfterSceneLoad());
    }

    private System.Collections.IEnumerator RefreshAudioAfterSceneLoad()
    {
        yield return new WaitForEndOfFrame();
        
        PlayBackgroundMusicForCurrentScene();
        UpdateCarEngineVolume();
        
        // Wait a bit more for all scripts to initialize
        yield return new WaitForSeconds(0.2f);
        NotifyVolumeSliders();
    }

    public void ToggleMusic()
    {
        SetMusicVolume(currentMusicVolume > 0f ? 0f : 1f);
    }

    public void ToggleSFX()
    {
        SetSFXVolume(currentSFXVolume > 0f ? 0f : 1f);
    }
}