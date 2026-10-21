using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelOpener : MonoBehaviour
{
    public GameObject settingsPanel; // Assign in Inspector
    public float bounceTime = 0.5f;  // Duration of bounce

    // private bool isOpen = false;

public void OpenSettingsPanel()
{
    if (!settingsPanel.activeSelf)
    {
        settingsPanel.SetActive(true);
        settingsPanel.transform.localScale = Vector3.zero;

        LeanTween.scale(settingsPanel, Vector3.one, bounceTime)
                 .setEase(LeanTweenType.easeOutBack);
    }
}

public void CloseSettingsPanel()
{
    if (settingsPanel.activeSelf)
    {
        LeanTween.scale(settingsPanel, Vector3.zero, bounceTime)
                 .setEase(LeanTweenType.easeInBack)
                 .setOnComplete(() => settingsPanel.SetActive(false));
    }
}



}
