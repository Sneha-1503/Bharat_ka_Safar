using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour {

    public void LoadScene(int sceneBuildIndex)
    {
        SceneManager.LoadScene(sceneBuildIndex);
    }

    public void OpenAssetStorePage()
    {
        Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/publisher/10488/");
    }
}
