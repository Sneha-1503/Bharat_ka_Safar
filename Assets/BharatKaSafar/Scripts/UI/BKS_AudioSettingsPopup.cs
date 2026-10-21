using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BharatKaSafar.UI
{
    public class BKS_AudioSettingsPopup : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider soundSlider;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text soundValueText;
        [SerializeField] private TMP_Text musicValueText;
        [SerializeField] private bool keepInScene;
        private bool initialized;
        private static BKS_AudioSettingsPopup instance;
        private static int blockNavigationThroughFrame = -1;
        public static bool BlocksNavigation => instance != null || Time.frameCount <= blockNavigationThroughFrame;

        public static void Open(BKS_AudioSettingsPopup savedPopup = null)
        {
            if (instance != null) return;
            if (savedPopup != null)
            {
                instance = savedPopup;
                savedPopup.gameObject.SetActive(true);
                if (savedPopup.initialized) savedPopup.LoadValues();
                blockNavigationThroughFrame = Time.frameCount + 1;
                return;
            }
            var prefab = Resources.Load<BKS_AudioSettingsPopup>("BKS_AudioSettingsPopup");
            if (prefab == null) { Debug.LogError("Missing shared audio settings prefab."); return; }
            instance = Instantiate(prefab);
            blockNavigationThroughFrame = Time.frameCount + 1;
        }

        private void Start()
        {
            initialized = true;
            musicSlider.onValueChanged.AddListener(SetMusic);
            soundSlider.onValueChanged.AddListener(SetSound);
            closeButton.onClick.AddListener(Close);
            LoadValues();
        }

        private void LoadValues()
        {
            musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 1f));
            soundSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 1f));
            RefreshLabels();
        }

        private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) Close(); }
        public void Close()
        {
            PlayerPrefs.Save();
            blockNavigationThroughFrame = Time.frameCount + 1;
            instance = null;
            // Closing Settings never resumes gameplay or changes the current page.
            if (keepInScene) gameObject.SetActive(false);
            else Destroy(gameObject);
        }
        private void OnDestroy() { if (instance == this) instance = null; }
        private void SetMusic(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            if (AudioManager.instance != null) AudioManager.instance.SetMusicVolume(value);
            PlayerPrefs.Save();
            RefreshLabels();
        }
        private void SetSound(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.SetFloat("EngineVolume", value);
            if (AudioManager.instance != null)
            {
                AudioManager.instance.SetSFXVolume(value);
                AudioManager.instance.SetEngineVolume(value);
            }
            PlayerPrefs.Save();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (soundValueText != null) soundValueText.text = "SOUND  " + VolumeLabel(soundSlider.value);
            if (musicValueText != null) musicValueText.text = "MUSIC  " + VolumeLabel(musicSlider.value);
        }
        private static string VolumeLabel(float value) => value <= 0.001f ? "OFF" : Mathf.RoundToInt(value * 100f) + "%";
    }
}
