using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.Store
{
    /// <summary>Put on any Button to open the Store (Back returns to this screen).</summary>
    [RequireComponent(typeof(Button))]
    public class BKS_OpenStoreButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (UI.BKS_AudioSettingsPopup.BlocksNavigation) return;
                if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.GoToStore();
                else UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_STORE);
            });
        }
    }
}
