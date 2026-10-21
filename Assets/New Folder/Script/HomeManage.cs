// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;
// using TMPro;

// public class HomeManage : MonoBehaviour
// {
//     public TextMeshProUGUI coinText;   
    
//     [Header("Audio Control Sliders")]
//     public Slider musicSlider;         
//     public Slider soundSlider;         

//     [Header("Loading System")]
//     public GameObject loadingPanel;
    
//     [Header("Quit System")]
//     public Button QuitButton;
//     public Button NoButton;
//     public Button YesButton;
//     public GameObject QuitPanel;
    
//     [Header("Loading Progress")]
//     public static bool isLoad;
//     public TextMeshProUGUI _progressingText;
//     public int _progressingValue;
    
//     public static HomeManage _homeManager;

//     void Awake()
//     {
//         _progressingValue = 0;
//         loadingPanel.SetActive(false);
//         if (!isLoad)
//         {
//             isLoad = true;
//             loadingPanel.SetActive(true);
//             StartCoroutine(Loading());
//         }
//         _homeManager = this;
//     }

//     IEnumerator Loading()
//     {
//         for (int i = 0; i < 100; i++)
//         {
//             yield return new WaitForSeconds(0.05f);
//             _progressingValue++;
//             _progressingText.text = _progressingValue.ToString() + "%";
//         }
//         loadingPanel.SetActive(false);
//     }

//     void Start()
//     {
//         UpdateCoinDisplay();
//         SetupButtons();
//         StartCoroutine(SetupAudioSliders());
//     }

//     IEnumerator SetupAudioSliders()
//     {
//         yield return new WaitForEndOfFrame();
        
//         while (AudioManager.instance == null)
//         {
//             yield return null;
//         }
        
//         LoadAudioSettings();
//         SetupSliderListeners();
//         SetupButtonClickSounds();
//     }

//     void UpdateCoinDisplay()
//     {
//         int coins = PlayerPrefs.GetInt("Coins", 0);
//         coinText.text = coins.ToString();
//     }

//     void SetupButtons()
//     {
//         if (QuitButton) QuitButton.onClick.AddListener(OnQuitButtonClicked);
//         if (NoButton) NoButton.onClick.AddListener(OnNoButtonClicked);
//         if (YesButton) YesButton.onClick.AddListener(OnYesButtonClicked);
//     }

//     void SetupButtonClickSounds()
//     {
//         Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        
//         foreach (Button button in allButtons)
//         {
//             button.onClick.AddListener(() => {
//                 if (AudioManager.instance)
//                     AudioManager.instance.PlayButtonClickSound();
//             });
//         }
//     }

//     void LoadAudioSettings()
//     {
//         if (AudioManager.instance == null) return;

//         if (musicSlider)
//             musicSlider.value = AudioManager.instance.GetMusicVolume();

//         if (soundSlider)
//             soundSlider.value = AudioManager.instance.GetSFXVolume();
//     }

//     void SetupSliderListeners()
//     {
//         if (musicSlider)
//             musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        
//         if (soundSlider)
//             soundSlider.onValueChanged.AddListener(OnSoundSliderChanged);
//     }

//     public void OnMusicSliderChanged(float value)
//     {
//         if (AudioManager.instance)
//         {
//             AudioManager.instance.SetMusicVolume(value);
//         }
//     }

//     public void OnSoundSliderChanged(float value)
//     {
//         if (AudioManager.instance)
//         {
//             AudioManager.instance.SetSFXVolume(value);
//             AudioManager.instance.SetEngineVolume(value);
//         }
//     }

//     public void OnQuitButtonClicked()
//     {
//         QuitPanel.SetActive(true);
//     }

//     public void OnNoButtonClicked()
//     {
//         QuitPanel.SetActive(false);
//     }

//     public void OnYesButtonClicked()
//     {
//         Application.Quit();
//     }

//     public void RefreshCoinDisplay()
//     {
//         UpdateCoinDisplay();
//     }
// }


using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HomeManage : MonoBehaviour
{
    public TextMeshProUGUI coinText;   
    
    [Header("Audio Control Sliders")]
    public Slider musicSlider;         
    public Slider soundSlider;         

    [Header("Loading System")]
    public GameObject loadingPanel;
    
    [Header("Quit System")]
    public Button QuitButton;
    public Button NoButton;
    public Button YesButton;
    public GameObject QuitPanel;
    
    [Header("Loading Progress")]
    public static bool isLoad;
    public TextMeshProUGUI _progressingText;
    public int _progressingValue;
    
    public static HomeManage _homeManager;

    void Awake()
    {
        _progressingValue = 0;
        loadingPanel.SetActive(false);
        if (!isLoad)
        {
            isLoad = true;
            loadingPanel.SetActive(true);
            StartCoroutine(Loading());
        }
        _homeManager = this;
    }

    IEnumerator Loading()
    {
        for (int i = 0; i < 100; i++)
        {
            yield return new WaitForSeconds(0.05f);
            _progressingValue++;
            _progressingText.text = _progressingValue.ToString() + "%";
        }
        loadingPanel.SetActive(false);
    }

    void Start()
    {
        UpdateCoinDisplay();
        SetupButtons();
        StartCoroutine(SetupAudioSliders());
    }

    IEnumerator SetupAudioSliders()
    {
        yield return new WaitForEndOfFrame();
        
        while (AudioManager.instance == null)
        {
            yield return null;
        }
        
        LoadAudioSettings();
        SetupSliderListeners();
        SetupButtonClickSounds();
    }

    void UpdateCoinDisplay()
    {
        int coins = PlayerPrefs.GetInt("Coins", 0);
        coinText.text = coins.ToString();
    }

    void SetupButtons()
    {
        if (QuitButton) QuitButton.onClick.AddListener(OnQuitButtonClicked);
        if (NoButton) NoButton.onClick.AddListener(OnNoButtonClicked);
        if (YesButton) YesButton.onClick.AddListener(OnYesButtonClicked);
    }

    void SetupButtonClickSounds()
    {
        // Only add button click sounds to specific UI buttons
        Button[] uiButtons = { QuitButton, NoButton, YesButton };
        
        foreach (Button button in uiButtons)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => {
                    if (AudioManager.instance)
                        AudioManager.instance.PlayButtonClickSound();
                });
            }
        }
    }

    void LoadAudioSettings()
    {
        if (AudioManager.instance == null) return;

        if (musicSlider)
            musicSlider.value = AudioManager.instance.GetMusicVolume();

        if (soundSlider)
            soundSlider.value = AudioManager.instance.GetSFXVolume();
    }

    void SetupSliderListeners()
    {
        if (musicSlider)
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        
        if (soundSlider)
            soundSlider.onValueChanged.AddListener(OnSoundSliderChanged);
    }

    public void OnMusicSliderChanged(float value)
    {
        if (AudioManager.instance)
        {
            AudioManager.instance.SetMusicVolume(value);
        }
    }

    public void OnSoundSliderChanged(float value)
    {
        if (AudioManager.instance)
        {
            AudioManager.instance.SetSFXVolume(value);
            AudioManager.instance.SetEngineVolume(value);
        }
    }

    public void RefreshAudioSliders()
    {
        if (AudioManager.instance == null) return;

        if (musicSlider)
            musicSlider.value = AudioManager.instance.GetMusicVolume();

        if (soundSlider)
            soundSlider.value = AudioManager.instance.GetSFXVolume();
    }

    public void OnQuitButtonClicked()
    {
        QuitPanel.SetActive(true);
    }

    public void OnNoButtonClicked()
    {
        QuitPanel.SetActive(false);
    }

    public void OnYesButtonClicked()
    {
        Application.Quit();
    }

    public void RefreshCoinDisplay()
    {
        UpdateCoinDisplay();
    }
}