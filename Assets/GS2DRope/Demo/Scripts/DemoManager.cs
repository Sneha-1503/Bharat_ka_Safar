using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DemoManager : MonoBehaviour {
    
    public void OnClickRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
