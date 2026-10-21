// using UnityEngine;
// using System.Collections;
// using UnityEngine.UI;

// public class SettingsMenu : MonoBehaviour
// {
//     [Header("Audio Sliders")]
//     public Slider engineVolume;
//     public Slider musicVolume;
//     public Slider sfxVolume;

//     [Header("Toggles")]
//     public Toggle showDistance;
//     public Toggle coinAudio;

//     [Header("Other Settings")]
//     public InputField cheatBox;
//     public Text currentResolution;
//     public Text currentQuality;

//     public bool activateCheats;

//     void Start()
//     {
//         LoadSettings();
//         SetupSliderListeners();
//         SetupButtonClickSounds();
//     }

//     void LoadSettings()
//     {
//         if (PlayerPrefs.GetInt("ShowDistance") == 3)
//             showDistance.isOn = true;
//         else
//             showDistance.isOn = false;

//         if (PlayerPrefs.GetInt("CoinAudio") == 3)
//             coinAudio.isOn = true;
//         else
//             coinAudio.isOn = false;

//         UpdateResolutionDisplay();
//         UpdateQualityDisplay();

//         StartCoroutine(LoadAudioSettings());
//     }

//     void UpdateResolutionDisplay()
//     {
//         if (PlayerPrefs.GetInt("Resolution") == 0)
//             currentResolution.text = "500";
//         else if (PlayerPrefs.GetInt("Resolution") == 1)
//             currentResolution.text = "720P";
//         else if (PlayerPrefs.GetInt("Resolution") == 2)
//             currentResolution.text = "1080P";
//     }

//     void UpdateQualityDisplay()
//     {
//         if (PlayerPrefs.GetInt("Quality") == 0)
//             currentQuality.text = "Low";
//         else if (PlayerPrefs.GetInt("Quality") == 3)
//             currentQuality.text = "Medium";
//         else if (PlayerPrefs.GetInt("Quality") == 5)
//             currentQuality.text = "High";
//     }

//     IEnumerator LoadAudioSettings()
//     {
//         while (AudioManager.instance == null)
//         {
//             yield return null;
//         }

//         RefreshAudioSliders();
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

//     void SetupSliderListeners()
//     {
//         if (engineVolume)
//             engineVolume.onValueChanged.AddListener(delegate { SetEngineVolume(); });

//         if (musicVolume)
//             musicVolume.onValueChanged.AddListener(delegate { SetMusicVolume(); });

//         if (sfxVolume)
//             sfxVolume.onValueChanged.AddListener(delegate { SetSFXVolume(); });
//     }

//     void SetupButtonClickSounds()
//     {
//         StartCoroutine(SetupButtonClickSoundsDelayed());
//     }

//     IEnumerator SetupButtonClickSoundsDelayed()
//     {
//         yield return new WaitForEndOfFrame();

//         while (AudioManager.instance == null)
//         {
//             yield return null;
//         }

//         Button[] buttons = GetComponentsInChildren<Button>();
//         foreach (Button button in buttons)
//         {
//             button.onClick.AddListener(() => {
//                 if (AudioManager.instance)
//                     AudioManager.instance.PlayButtonClickSound();
//             });
//         }
//     }

//     public void RefreshAudioSliders()
//     {
//         if (AudioManager.instance == null) return;

//         if (engineVolume)
//             engineVolume.value = AudioManager.instance.GetEngineVolume();

//         if (musicVolume)
//             musicVolume.value = AudioManager.instance.GetMusicVolume();

//         if (sfxVolume)
//             sfxVolume.value = AudioManager.instance.GetSFXVolume();
//     }

//     public void SetEngineVolume()
//     {
//         if (AudioManager.instance && engineVolume)
//         {
//             AudioManager.instance.SetEngineVolume(engineVolume.value);
//         }
//     }

//     public void SetMusicVolume()
//     {
//         if (AudioManager.instance && musicVolume)
//         {
//             AudioManager.instance.SetMusicVolume(musicVolume.value);
//         }
//     }

//     public void SetSFXVolume()
//     {
//         if (AudioManager.instance && sfxVolume)
//         {
//             AudioManager.instance.SetSFXVolume(sfxVolume.value);
//         }
//     }

//     public void SetShowDistance()
//     {
//         StartCoroutine(saveDistance());
//     }

//     IEnumerator saveDistance()
//     {
//         yield return new WaitForEndOfFrame();

//         if (showDistance.isOn)
//             PlayerPrefs.SetInt("ShowDistance", 3);
//         else
//             PlayerPrefs.SetInt("ShowDistance", 0);
//     }

//     public void SetCoinAudio()
//     {
//         StartCoroutine(saveCoinAudio());
//     }

//     IEnumerator saveCoinAudio()
//     {
//         yield return new WaitForEndOfFrame();

//         if (coinAudio.isOn)
//             PlayerPrefs.SetInt("CoinAudio", 3);
//         else
//             PlayerPrefs.SetInt("CoinAudio", 0);
//     }

//     public void SetResolution(int val)
//     {
//         if (AudioManager.instance)
//             AudioManager.instance.PlayButtonClickSound();

//         PlayerPrefs.SetInt("Resolution", val);
//         UpdateResolutionDisplay();
//     }

//     public void SetQualityLevel(int val)
//     {
//         if (AudioManager.instance)
//             AudioManager.instance.PlayButtonClickSound();

//         PlayerPrefs.SetInt("Quality", val);
//         UpdateQualityDisplay();
//         QualitySettings.SetQualityLevel(val);
//     }

//     public void EnterCheat()
//     {
//         if (AudioManager.instance)
//             AudioManager.instance.PlayButtonClickSound();

//         if (activateCheats)
//         {
//             bool cheatExecuted = ProcessCheatCode();
            
//             if (!cheatExecuted && AudioManager.instance)
//             {
//                 AudioManager.instance.PlayErrorSound();
//             }
//         }
//     }

//     private bool ProcessCheatCode()
//     {
//         if (cheatBox.text == "DeveloperCoins-10000")
//         {
//             PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 10000);
//             return true;
//         }
//         else if (cheatBox.text == "DeveloperCoins-100000")
//         {
//             PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 100000);
//             return true;
//         }
//         else if (cheatBox.text == "DeveloperCoins-1000000")
//         {
//             PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 1000000);
//             return true;
//         }
//         else if (cheatBox.text.StartsWith("DeveloperLevels-Unlock") && cheatBox.text != "DeveloperLevels-UnlockAll")
//         {
//             string levelNum = cheatBox.text.Replace("DeveloperLevels-Unlock", "");
//             PlayerPrefs.SetInt("Level" + levelNum, 3);
//             return true;
//         }
//         else if (cheatBox.text == "DeveloperLevels-UnlockAll")
//         {
//             for (int a = 0; a < 100; a++)
//                 PlayerPrefs.SetInt("Level" + a.ToString(), 3);
//             return true;
//         }
//         else if (cheatBox.text.StartsWith("DeveloperCars-Unlock") && cheatBox.text != "DeveloperCars-UnlockAll")
//         {
//             string carNum = cheatBox.text.Replace("DeveloperCars-Unlock", "");
//             PlayerPrefs.SetInt("Car" + carNum, 3);
//             return true;
//         }
//         else if (cheatBox.text == "DeveloperCars-UnlockAll")
//         {
//             for (int a = 0; a < 100; a++)
//                 PlayerPrefs.SetInt("Car" + a.ToString(), 3);
//             return true;
//         }
        
//         return false;
//     }
// }



using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio Sliders")]
    public Slider engineVolume;
    public Slider musicVolume;
    public Slider sfxVolume;

    [Header("Toggles")]
    public Toggle showDistance;
    public Toggle coinAudio;

    [Header("Other Settings")]
    public InputField cheatBox;
    public Text currentResolution;
    public Text currentQuality;

    public bool activateCheats;

    private bool slidersInitialized = false;

    void Start()
    {
        LoadSettings();
        StartCoroutine(InitializeAudioSliders());
        // SetupButtonClickSounds();
    }

    void OnEnable()
    {
        // Refresh sliders whenever this menu becomes active
        if (slidersInitialized)
        {
            StartCoroutine(RefreshSlidersDelayed());
        }
    }

    IEnumerator RefreshSlidersDelayed()
    {
        yield return new WaitForEndOfFrame();
        RefreshAudioSliders();
    }

    IEnumerator InitializeAudioSliders()
    {
        // Wait for AudioManager to be ready
        while (AudioManager.instance == null)
        {
            yield return null;
        }

        // Load audio settings and setup listeners
        RefreshAudioSliders();
        SetupSliderListeners();
        slidersInitialized = true;
    }

    void LoadSettings()
    {
        // Load non-audio settings
        if (showDistance)
        {
            if (PlayerPrefs.GetInt("ShowDistance") == 3)
                showDistance.isOn = true;
            else
                showDistance.isOn = false;
        }

        if (coinAudio)
        {
            if (PlayerPrefs.GetInt("CoinAudio") == 3)
                coinAudio.isOn = true;
            else
                coinAudio.isOn = false;
        }

        UpdateResolutionDisplay();
        UpdateQualityDisplay();
    }

    void UpdateResolutionDisplay()
    {
        if (!currentResolution) return;
        
        if (PlayerPrefs.GetInt("Resolution") == 0)
            currentResolution.text = "500";
        else if (PlayerPrefs.GetInt("Resolution") == 1)
            currentResolution.text = "720P";
        else if (PlayerPrefs.GetInt("Resolution") == 2)
            currentResolution.text = "1080P";
    }

    void UpdateQualityDisplay()
    {
        if (!currentQuality) return;
        
        if (PlayerPrefs.GetInt("Quality") == 0)
            currentQuality.text = "Low";
        else if (PlayerPrefs.GetInt("Quality") == 3)
            currentQuality.text = "Medium";
        else if (PlayerPrefs.GetInt("Quality") == 5)
            currentQuality.text = "High";
    }

    void SetupSliderListeners()
    {
        // Remove existing listeners to avoid duplicates
        if (engineVolume)
        {
            engineVolume.onValueChanged.RemoveAllListeners();
            engineVolume.onValueChanged.AddListener(delegate { SetEngineVolume(); });
        }

        if (musicVolume)
        {
            musicVolume.onValueChanged.RemoveAllListeners();
            musicVolume.onValueChanged.AddListener(delegate { SetMusicVolume(); });
        }

        if (sfxVolume)
        {
            sfxVolume.onValueChanged.RemoveAllListeners();
            sfxVolume.onValueChanged.AddListener(delegate { SetSFXVolume(); });
        }
    }

    private bool HasClickSoundListener(Button button)
    {
        // Simple check - you might want to implement a more sophisticated method
        // to avoid duplicate listeners
        return false;
    }

    public void RefreshAudioSliders()
    {
        if (AudioManager.instance == null) return;

        // Temporarily disable listeners to avoid triggering changes
        bool listenersEnabled = slidersInitialized;
        if (listenersEnabled)
        {
            if (engineVolume) engineVolume.onValueChanged.RemoveAllListeners();
            if (musicVolume) musicVolume.onValueChanged.RemoveAllListeners();
            if (sfxVolume) sfxVolume.onValueChanged.RemoveAllListeners();
        }

        // Update slider values
        if (engineVolume)
            engineVolume.value = AudioManager.instance.GetEngineVolume();

        if (musicVolume)
            musicVolume.value = AudioManager.instance.GetMusicVolume();

        if (sfxVolume)
            sfxVolume.value = AudioManager.instance.GetSFXVolume();

        // Re-enable listeners
        if (listenersEnabled)
        {
            SetupSliderListeners();
        }
    }

    public void SetEngineVolume()
    {
        if (AudioManager.instance && engineVolume)
        {
            AudioManager.instance.SetEngineVolume(engineVolume.value);
        }
    }

    public void SetMusicVolume()
    {
        if (AudioManager.instance && musicVolume)
        {
            AudioManager.instance.SetMusicVolume(musicVolume.value);
        }
    }

    public void SetSFXVolume()
    {
        if (AudioManager.instance && sfxVolume)
        {
            AudioManager.instance.SetSFXVolume(sfxVolume.value);
        }
    }

    public void SetShowDistance()
    {
        StartCoroutine(saveDistance());
    }

    IEnumerator saveDistance()
    {
        yield return new WaitForEndOfFrame();

        if (showDistance.isOn)
            PlayerPrefs.SetInt("ShowDistance", 3);
        else
            PlayerPrefs.SetInt("ShowDistance", 0);
    }

    public void SetCoinAudio()
    {
        StartCoroutine(saveCoinAudio());
    }

    IEnumerator saveCoinAudio()
    {
        yield return new WaitForEndOfFrame();

        if (coinAudio.isOn)
            PlayerPrefs.SetInt("CoinAudio", 3);
        else
            PlayerPrefs.SetInt("CoinAudio", 0);
    }

    public void SetResolution(int val)
    {
        if (AudioManager.instance)
            AudioManager.instance.PlayButtonClickSound();

        PlayerPrefs.SetInt("Resolution", val);
        UpdateResolutionDisplay();
    }

    public void SetQualityLevel(int val)
    {
        if (AudioManager.instance)
            AudioManager.instance.PlayButtonClickSound();

        PlayerPrefs.SetInt("Quality", val);
        UpdateQualityDisplay();
        QualitySettings.SetQualityLevel(val);
    }

    public void EnterCheat()
    {
        if (AudioManager.instance)
            AudioManager.instance.PlayButtonClickSound();

        if (activateCheats)
        {
            bool cheatExecuted = ProcessCheatCode();
            
            if (!cheatExecuted && AudioManager.instance)
            {
                AudioManager.instance.PlayErrorSound();
            }
        }
    }

    private bool ProcessCheatCode()
    {
        if (string.IsNullOrEmpty(cheatBox.text))
            return false;
            
        if (cheatBox.text == "DeveloperCoins-10000")
        {
            PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 10000);
            return true;
        }
        else if (cheatBox.text == "DeveloperCoins-100000")
        {
            PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 100000);
            return true;
        }
        else if (cheatBox.text == "DeveloperCoins-1000000")
        {
            PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + 1000000);
            return true;
        }
        else if (cheatBox.text.StartsWith("DeveloperLevels-Unlock") && cheatBox.text != "DeveloperLevels-UnlockAll")
        {
            string levelNum = cheatBox.text.Replace("DeveloperLevels-Unlock", "");
            if (int.TryParse(levelNum, out int level))
            {
                PlayerPrefs.SetInt("Level" + levelNum, 3);
                return true;
            }
        }
        else if (cheatBox.text == "DeveloperLevels-UnlockAll")
        {
            for (int a = 1; a <= 100; a++)
                PlayerPrefs.SetInt("Level" + a.ToString(), 3);
            return true;
        }
        else if (cheatBox.text.StartsWith("DeveloperCars-Unlock") && cheatBox.text != "DeveloperCars-UnlockAll")
        {
            string carNum = cheatBox.text.Replace("DeveloperCars-Unlock", "");
            if (int.TryParse(carNum, out int car))
            {
                PlayerPrefs.SetInt("Car" + carNum, 3);
                return true;
            }
        }
        else if (cheatBox.text == "DeveloperCars-UnlockAll")
        {
            for (int a = 1; a <= 100; a++)
                PlayerPrefs.SetInt("Car" + a.ToString(), 3);
            return true;
        }
        
        return false;
    }
}