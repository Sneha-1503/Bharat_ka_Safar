// using UnityEngine;
// using System.Collections;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;
// public class MenuTools : MonoBehaviour {



// 	// Player starting game in first time score
// 	public int startScore;

// 	public Text CoinsTXT;

// 	[Header("Menu Resolution")]
// 	public int ResolutionX = 1280;
// 	public int ResolutionY = 720;

// 	public GameObject manuMusic;

// 	void Start () {
	
// 		if(GameObject.Find ("LevelMusic(Clone)"))
// 			Destroy (GameObject.Find ("LevelMusic(Clone)"));

// 		if(!GameObject.Find("MenuMusic(Clone)"))
// 			Instantiate (manuMusic, Vector3.zero, Quaternion.identity);
		
// 		if (PlayerPrefs.GetString ("FirstRun") != "True") {

// 			PlayerPrefs.SetString ("FirstRun", "True");
// 			PlayerPrefs.SetInt ("Coins", PlayerPrefs.GetInt ("Coins") + startScore);

// 			PlayerPrefs.SetInt ("Resolution", 2);// 3 => true | 0 => false

// 			PlayerPrefs.SetFloat ("EngineVolume", 0.74f);
// 			PlayerPrefs.SetFloat ("MusicVolume", 1f);
// 			PlayerPrefs.SetInt ("ShowDistance", 3);
// 			PlayerPrefs.SetInt ("CoinAudio", 3); 

// 			PlayerPrefs.SetInt ("Car0", 3);// 3 => true | 0 => false
// 			PlayerPrefs.SetInt ("Level0", 3);// 3 => true | 0 => false

// 		}



// 			Screen.SetResolution(ResolutionX,ResolutionY,true);
// 			Camera.main.aspect  = 16f/9f;



// 		if(PlayerPrefs.GetInt("Loaded")!=3)
// 		{
// 			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
// 			PlayerPrefs.SetInt("Loaded",3);
// 		}
// 		else
// 			PlayerPrefs.SetInt("Loaded",7);
		
// 		CoinsTXT.text = PlayerPrefs.GetInt ("Coins").ToString ();
// 	}
	

// 	void Update () {
// 		if (Input.GetKeyDown (KeyCode.H)) {
// 			PlayerPrefs.DeleteAll ();
// 			CoinsTXT.text = PlayerPrefs.GetInt ("Coins").ToString ();
// 			#if UNITY_EDITOR
// 			Debug.Log("PlayerPrefs.DeleteAll");
// 			#endif

// 		}
// 		if (Input.GetKeyDown (KeyCode.V)) {
// 			PlayerPrefs.SetInt ("Coins", PlayerPrefs.GetInt ("Coins") + startScore);
// 			CoinsTXT.text = PlayerPrefs.GetInt ("Coins").ToString ();
// 			#if UNITY_EDITOR
// 			Debug.Log(PlayerPrefs.GetInt ("Coins").ToString()); 
// 			#endif

// 		}
// 	}


// 	public void SetTrue(GameObject target)
// 	{
// 		target.SetActive (true);
// 	}
// 	public void SetFalse(GameObject target)
// 	{
// 		target.SetActive (false);
// 	}
// 	public void ToggleObject(GameObject target)
// 	{
// 		target.SetActive (!target.activeSelf);
// 	}
// 	public void LoadLevel(string name)
// 	{
// 		SceneManager.LoadScene (name);
// 	}
// 	public void OpenURL(string url)
// 	{
// 		Application.OpenURL (url);
// 	}
// 	public void LoadLevelAsync(string name)
// 	{
// 		SceneManager.LoadSceneAsync (name);
// 	}
// 	public void Exit()
// 	{
// 		Application.Quit ();
// 	}

// }
	


using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuTools : MonoBehaviour
{
	public int startScore;
	public Text CoinsTXT;

	[Header("Menu Resolution")]
	public int ResolutionX = 1280;
	public int ResolutionY = 720;

	void Start()
	{
		if (PlayerPrefs.GetString("FirstRun") != "True")
		{
			PlayerPrefs.SetString("FirstRun", "True");
			PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + startScore);
			PlayerPrefs.SetInt("Resolution", 2);
			
			PlayerPrefs.SetFloat("EngineVolume", 0.74f);
			PlayerPrefs.SetFloat("MusicVolume", 1f);
			PlayerPrefs.SetFloat("SFXVolume", 1f);
			
			PlayerPrefs.SetInt("ShowDistance", 3);
			PlayerPrefs.SetInt("CoinAudio", 3);
			PlayerPrefs.SetInt("Car0", 3);
			PlayerPrefs.SetInt("Level0", 3);
		}

		Screen.SetResolution(ResolutionX, ResolutionY, true);
		Camera.main.aspect = 16f / 9f;

		if (PlayerPrefs.GetInt("Loaded") != 3)
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
			PlayerPrefs.SetInt("Loaded", 3);
		}
		else
			PlayerPrefs.SetInt("Loaded", 7);

		CoinsTXT.text = PlayerPrefs.GetInt("Coins").ToString();
		// SetupButtonClickSounds();
	}

	// void SetupButtonClickSounds()
	// {
	// 	StartCoroutine(SetupButtonClickSoundsDelayed());
	// }

	// IEnumerator SetupButtonClickSoundsDelayed()
	// {
	// 	yield return new WaitForEndOfFrame();
		
	// 	while (AudioManager.instance == null)
	// 	{
	// 		yield return null;
	// 	}

	// 	Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
		
	// 	foreach (Button button in allButtons)
	// 	{
	// 		button.onClick.AddListener(() => {
	// 			if (AudioManager.instance)
	// 				AudioManager.instance.PlayButtonClickSound();
	// 		});
	// 	}
	// }

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.H))
		{
			PlayerPrefs.DeleteAll();
			CoinsTXT.text = PlayerPrefs.GetInt("Coins").ToString();
			#if UNITY_EDITOR
			Debug.Log("PlayerPrefs.DeleteAll");
			#endif
		}
		
		if (Input.GetKeyDown(KeyCode.V))
		{
			PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") + startScore);
			CoinsTXT.text = PlayerPrefs.GetInt("Coins").ToString();
			#if UNITY_EDITOR
			Debug.Log(PlayerPrefs.GetInt("Coins").ToString());
			#endif
		}
	}

	public void SetTrue(GameObject target)
	{
		target.SetActive(true);
	}

	public void SetFalse(GameObject target)
	{
		target.SetActive(false);
	}

	public void ToggleObject(GameObject target)
	{
		target.SetActive(!target.activeSelf);
	}

	public void LoadLevel(string name)
	{
		SceneManager.LoadScene(name);
	}

	public void OpenURL(string url)
	{
		Application.OpenURL(url);
	}

	public void LoadLevelAsync(string name)
	{
		SceneManager.LoadSceneAsync(name);
	}

	public void Exit()
	{
		Application.Quit();
	}
}