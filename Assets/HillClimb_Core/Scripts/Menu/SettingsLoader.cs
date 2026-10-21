// using UnityEngine;
// using System.Collections;
// using UnityEngine.SceneManagement;

// public class SettingsLoader : MonoBehaviour {

// 	public GameObject distanceSlider;

// 	public AudioSource musicVolume,coinAudio;

// 	public GameObject musicPrefab;

// 	IEnumerator Start () {

// 		//if (PlayerPrefs.GetInt ("ShowDistance") == 3)  // 3=> true - 0 => false
// 		//	distanceSlider.SetActive(true);
// 		//else
// 		//	distanceSlider.SetActive(false);

// 		//if (PlayerPrefs.GetInt ("CoinAudio") == 3)  // 3=> true - 0 => false
// 			coinAudio.volume =1f; 
// 		//else
// 		//	coinAudio.volume =0;  
// 		/*
// 		if (PlayerPrefs.GetInt ("Resolution") == 0 || PlayerPrefs.GetInt ("Resolution") == 1 || PlayerPrefs.GetInt ("Resolution") == 2) {
// 			if (PlayerPrefs.GetInt ("Resolution") == 0)
// 				Screen.SetResolution (900, 506, true);
// 			if (PlayerPrefs.GetInt ("Resolution") == 1)
// 				Screen.SetResolution (1280, 720, true);
// 			if (PlayerPrefs.GetInt ("Resolution") == 2)
// 				Screen.SetResolution (1920, 1080, true);

// 			Camera.main.aspect = 16f / 9f;
// 		}
// 		*/
// 		/*
// 		if(PlayerPrefs.GetInt("Loaded")!=3)
// 		{
// 			SceneManager.LoadScene (SceneManager.GetActiveScene ().name);
// 			PlayerPrefs.SetInt("Loaded",3);
// 		}
// 		else
// 			PlayerPrefs.SetInt("Loaded",7);

// 		*/


// 		yield return new WaitForEndOfFrame ();

// 		GameObject.FindFirstObjectByType<CarController> ().EngineSoundS.volume = PlayerPrefs.GetFloat ("EngineVolume");

// 		if (!GameObject.Find ("LevelMusic(Clone)")) {
// 			GameObject m = (GameObject)	Instantiate (musicPrefab, Vector3.zero, Quaternion.identity);
// 			//GameObject.DontDestroyOnLoad (m);
// 		}


// 		GameObject.Find("LevelMusic(Clone)").GetComponent<AudioSource>().volume = PlayerPrefs.GetFloat ("MusicVolume"); 

// 	}
// }



using UnityEngine;
using System.Collections;

public class SettingsLoader : MonoBehaviour 
{
    public GameObject distanceSlider;

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        while (AudioManager.instance == null)
        {
            yield return null;
        }

        CarController carController = FindFirstObjectByType<CarController>();
        if (carController && carController.EngineSoundS)
        {
            float engineVolume = AudioManager.instance.GetEngineVolume();
            carController.EngineSoundS.volume = engineVolume * AudioManager.instance.maxEngineVolume;
        }
    }

    public void RefreshAudioSettings()
    {
        if (AudioManager.instance == null) return;

        CarController carController = FindFirstObjectByType<CarController>();
        if (carController && carController.EngineSoundS)
            carController.EngineSoundS.volume = AudioManager.instance.GetEngineVolume() * AudioManager.instance.maxEngineVolume;
    }
}