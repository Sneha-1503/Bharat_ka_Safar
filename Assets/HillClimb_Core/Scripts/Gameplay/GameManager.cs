// using UnityEngine;
// using System.Collections;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.SceneManagement;

// public class GameManager : MonoBehaviour
// {
// 	[Header("Informations")]
// 	public TMP_Text DistanceTXT;
// 	public Text RecordTXT;
// 	public TMP_Text CoinTXT;
// 	public TMP_Text FuelTXT;
// 	public Text LastRecord;

// 	[Header("Sliders")]
// 	public Slider FuelSlider;
// 	public Slider DistanceSlider;

// 	[Header("Audio Control Sliders (Optional)")]
// 	public Slider musicVolumeSlider;
// 	public Slider sfxVolumeSlider;

// 	Transform player;
// 	bool Started, Finished;

// 	[Header("Fuel")]
// 	public float TotalFuel = 100f;
// 	public float FuelTime = .6f;
// 	float FuelVal = .35f;

// 	int Coins;

// 	[Header("Coins And Awards")]
// 	public GameObject coinAwardedBox;
// 	public Text awardedText;
// 	public Animator awardAnimator;

// 	[Header("Complete Level")]
// 	public GameObject youWinMenu;
// 	public GameObject youLostMenu;
// 	public int winnerAwardedCoins = 30000;
// 	public int targetDistance;

// 	[Header("Game Over UI")]
// 	public TMP_Text totalCoinsText;
// 	public TMP_Text totalCoinsText1;
// 	public TMP_Text totalCoinsText2;
// 	public TMP_Text currentDistanceText;
// 	public TMP_Text currentDistanceText1;
// 	public TMP_Text bestDistanceText;

// 	[Header("Pop-out Animation Settings")]
// 	public float bounceTime = 0.5f;
// 	private Vector3 originalScale = Vector3.one;

// 	private bool audioSlidersInitialized = false;

// 	IEnumerator Start()
// 	{
// 		Application.targetFrameRate = 60;
// 		DistanceSlider.gameObject.SetActive(true);
// 		DistanceSlider.value = 0;
// 		DistanceSlider.maxValue = targetDistance;

// 		Coins = PlayerPrefs.GetInt("Coins");
// 		CoinTXT.text = Coins.ToString();

// 		yield return new WaitForEndOfFrame();
// 		player = GameObject.FindGameObjectWithTag("Car").transform;
// 		Started = true;
// 		Finished = false;

// 		InitializeDistanceMilestones();
// 		StartCoroutine(InitializeAudioSliders());

// 		while (!Finished) {
// 			yield return new WaitForSeconds(FuelTime);
// 			TotalFuel -= FuelVal;
// 			FuelSlider.value = TotalFuel;
// 			if (TotalFuel >= 0)
// 				FuelTXT.text = Mathf.Floor(TotalFuel).ToString();
// 			if (TotalFuel < 0) {
// 				fuelFinished = true;
// 				StartFuelFinish();
// 			}
// 		}
// 	}

// 	IEnumerator InitializeAudioSliders()
// 	{
// 		// Wait for AudioManager to be ready
// 		while (AudioManager.instance == null)
// 		{
// 			yield return null;
// 		}

// 		// Setup audio sliders if they exist
// 		SetupAudioSliders();
// 		RefreshAudioSliders();
// 		audioSlidersInitialized = true;
// 	}

// 	void SetupAudioSliders()
// 	{
// 		// Setup music volume slider
// 		if (musicVolumeSlider)
// 		{
// 			musicVolumeSlider.onValueChanged.RemoveAllListeners();
// 			musicVolumeSlider.onValueChanged.AddListener(delegate { SetMusicVolume(); });
// 		}

// 		// Setup SFX volume slider (controls both SFX and engine sounds)
// 		if (sfxVolumeSlider)
// 		{
// 			sfxVolumeSlider.onValueChanged.RemoveAllListeners();
// 			sfxVolumeSlider.onValueChanged.AddListener(delegate { SetSFXVolume(); });
// 		}
// 	}

// 	public void RefreshAudioSliders()
// 	{
// 		if (AudioManager.instance == null) return;

// 		// Temporarily disable listeners to avoid triggering changes during refresh
// 		bool listenersEnabled = audioSlidersInitialized;
// 		if (listenersEnabled)
// 		{
// 			if (musicVolumeSlider) musicVolumeSlider.onValueChanged.RemoveAllListeners();
// 			if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
// 		}

// 		// Update slider values to match AudioManager settings
// 		if (musicVolumeSlider)
// 			musicVolumeSlider.value = AudioManager.instance.GetMusicVolume();

// 		if (sfxVolumeSlider)
// 			sfxVolumeSlider.value = AudioManager.instance.GetSFXVolume();

// 		// Re-enable listeners
// 		if (listenersEnabled)
// 		{
// 			SetupAudioSliders();
// 		}
// 	}

// 	// This method can be called when the GameManager is enabled (useful for pause menus)
// 	void OnEnable()
// 	{
// 		if (audioSlidersInitialized && AudioManager.instance != null)
// 		{
// 			RefreshAudioSliders();
// 		}
// 	}

// 	public void SetMusicVolume()
// 	{
// 		if (AudioManager.instance && musicVolumeSlider)
// 		{
// 			AudioManager.instance.SetMusicVolume(musicVolumeSlider.value);
// 		}
// 	}

// 	public void SetSFXVolume()
// 	{
// 		if (AudioManager.instance && sfxVolumeSlider)
// 		{
// 			// Set both SFX and engine volume to the same value
// 			AudioManager.instance.SetSFXVolume(sfxVolumeSlider.value);
// 			AudioManager.instance.SetEngineVolume(sfxVolumeSlider.value);
// 		}
// 	}

// 	// Quick toggle methods for buttons (optional)
// 	public void ToggleMusic()
// 	{
// 		if (AudioManager.instance)
// 		{
// 			AudioManager.instance.ToggleMusic();
// 			RefreshAudioSliders();
// 		}
// 	}

// 	public void ToggleSFX()
// 	{
// 		if (AudioManager.instance)
// 		{
// 			AudioManager.instance.ToggleSFX();
// 			// Also toggle engine volume to match SFX volume
// 			AudioManager.instance.SetEngineVolume(AudioManager.instance.GetSFXVolume());
// 			RefreshAudioSliders();
// 		}
// 	}

// 	void InitializeDistanceMilestones()
// 	{
// 		if (PlayerPrefs.GetInt("c500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c500 = true;
// 			LastRecord.text = "Record:500";
// 		}
// 		if (PlayerPrefs.GetInt("c1000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c1000 = true;
// 			LastRecord.text = "Record:1000";
// 		}
// 		if (PlayerPrefs.GetInt("c1500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c1500 = true;
// 			LastRecord.text = "Record:1500";
// 		}
// 		if (PlayerPrefs.GetInt("c2000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c2000 = true;
// 			LastRecord.text = "Record:2000";
// 		}
// 		if (PlayerPrefs.GetInt("c2500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c2500 = true;
// 			LastRecord.text = "Record:2500";
// 		}
// 		if (PlayerPrefs.GetInt("c3000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c3000 = true;
// 			LastRecord.text = "Record:3000";
// 		}
// 		if (PlayerPrefs.GetInt("c3500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c3500 = true;
// 			LastRecord.text = "Record:3500";
// 		}
// 		if (PlayerPrefs.GetInt("c4000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
// 			c4000 = true;
// 			LastRecord.text = "Record:4000";
// 		}
// 	}

// 	float DistanceTemp;

// 	void Update()
// 	{
// 		if (Started) {
// 			CoinDistance();
// 			if (player.position.x >= DistanceTemp) {
// 				DistanceTXT.text = Mathf.Floor(player.position.x).ToString();
// 				DistanceTemp = player.position.x;
// 				DistanceSlider.value = player.position.x;
// 			}
// 		}
// 	}

// 	public void AddCoin(int value)
// 	{
// 		StartCoroutine(TakeCoins());

// 		CoinTXT.transform.localScale = new Vector3(CoinTXT.transform.localScale.x, CoinTXT.transform.localScale.y + 0.7f, CoinTXT.transform.localScale.z);

// 		if (AudioManager.instance)
// 			AudioManager.instance.PlayCoinSound();

// 		Coins += value;
// 		CoinTXT.text = Coins.ToString();
// 		PlayerPrefs.SetInt("Coins", Coins);
// 	}

// 	IEnumerator TakeCoins()
// 	{
// 		yield return new WaitForSeconds(0.03f);
// 		CoinTXT.transform.localScale = new Vector3(CoinTXT.transform.localScale.x, CoinTXT.transform.localScale.y - 0.7f, CoinTXT.transform.localScale.z);
// 	}

// 	public void AddFuel(int value)
// 	{
// 		if (AudioManager.instance)
// 			AudioManager.instance.PlayFuelSound();
		
// 		TotalFuel = value;
// 	}

// 	bool c500, c1000, c1500, c2000, c2500, c3000, c3500, c4000;

// 	void CoinDistance()
// 	{
// 		CheckDistanceMilestone(500, ref c500, "c500");
// 		CheckDistanceMilestone(1000, ref c1000, "c1000");
// 		CheckDistanceMilestone(1500, ref c1500, "c1500");
// 		CheckDistanceMilestone(2000, ref c2000, "c2000");
// 		CheckDistanceMilestone(2500, ref c2500, "c2500");
// 		CheckDistanceMilestone(3000, ref c3000, "c3000");
// 		CheckDistanceMilestone(3500, ref c3500, "c3500");
// 		CheckDistanceMilestone(4000, ref c4000, "c4000");

// 		if (player.transform.position.x >= targetDistance && !Finished) {
// 			Finished = true;
			
// 			if (AudioManager.instance)
// 				AudioManager.instance.PlayVictorySound();
			
// 			string key = "BestDistance" + (SceneManager.GetActiveScene().buildIndex - 1).ToString();
// 			int previousBest = PlayerPrefs.GetInt(key);
// 			int current = Mathf.CeilToInt(player.position.x);
// 			if (current > previousBest) {
// 				PlayerPrefs.SetInt(key, current);
// 			}
// 			if (totalCoinsText2)
// 				totalCoinsText2.text = RunCoins.ToString();
// 			PopOutPanel(youWinMenu);
// 			AddCoin(winnerAwardedCoins);
// 			GameObject.FindGameObjectWithTag("Player").transform.parent.gameObject.SetActive(false);
// 		}
// 	}

// 	void CheckDistanceMilestone(int distance, ref bool milestoneReached, string prefKey)
// 	{
// 		int nextDistance = distance == 4000 ? 4500 : distance + 500;
		
// 		if (!milestoneReached && player.transform.position.x >= distance && player.transform.position.x < nextDistance) {
// 			AddCoin(distance);
// 			coinAwardedBox.SetActive(true);
// 			awardAnimator.SetBool("Award", true);
// 			awardedText.text = distance.ToString() + " Coins Awarded";
// 			StartCoroutine(Awardfalse());
// 			milestoneReached = true;
// 			PlayerPrefs.SetInt(prefKey + PlayerPrefs.GetInt("SelectedLevel").ToString(), 3);
// 			LastRecord.text = "Record:" + distance.ToString();
// 		}
// 	}

// 	IEnumerator Awardfalse()
// 	{
// 		yield return new WaitForSeconds(3f);
// 		awardAnimator.SetBool("Award", false);
// 		yield return new WaitForSeconds(3f);
// 		coinAwardedBox.SetActive(false);
// 	}

// 	[HideInInspector] public bool isDead;
// 	[HideInInspector] public bool fuelFinished;

// 	public void StartDead()
// 	{
// 		StartCoroutine(Dead());
// 	}

// 	IEnumerator Dead()
// 	{
// 		yield return new WaitForSeconds(1f);
// 		if (isDead) {
// 			if (AudioManager.instance)
// 				AudioManager.instance.PlayDieSound();
			
// 			string key = "BestDistance" + (SceneManager.GetActiveScene().buildIndex - 1).ToString();
// 			int previousBest = PlayerPrefs.GetInt(key);
// 			int current = Mathf.CeilToInt(player.position.x);
// 			if (current > previousBest) {
// 				PlayerPrefs.SetInt(key, current);
// 			}
// 			PopOutPanel(youLostMenu);
// 			if (totalCoinsText)
// 				totalCoinsText.text = RunCoins.ToString();
// 			if (totalCoinsText1)
// 				totalCoinsText1.text = RunCoins.ToString();
// 			if (currentDistanceText)
// 				currentDistanceText.text = Mathf.FloorToInt(player.position.x).ToString();
// 			if (currentDistanceText1)
// 				currentDistanceText1.text = Mathf.FloorToInt(player.position.x).ToString();
// 			if (bestDistanceText)
// 				bestDistanceText.text = Mathf.Max(previousBest, current).ToString();
// 			Time.timeScale = 0;
// 		}
// 	}

// 	public void StartFuelFinish()
// 	{
// 		StartCoroutine(DeadFuel());
// 	}

// 	IEnumerator DeadFuel()
// 	{
// 		yield return new WaitForSeconds(0.2f);
// 		if (fuelFinished) {
// 			if (AudioManager.instance)
// 				AudioManager.instance.PlayDieSound();
			
// 			string key = "BestDistance" + (SceneManager.GetActiveScene().buildIndex - 1).ToString();
// 			int previousBest = PlayerPrefs.GetInt(key);
// 			int current = Mathf.CeilToInt(player.position.x);
// 			if (current > previousBest) {
// 				PlayerPrefs.SetInt(key, current);
// 			}
// 			PopOutPanel(youLostMenu);
// 			if (totalCoinsText)
// 				totalCoinsText.text = RunCoins.ToString();
// 			if (totalCoinsText1)
// 				totalCoinsText1.text = RunCoins.ToString();
// 			if (currentDistanceText)
// 				currentDistanceText.text = Mathf.FloorToInt(player.position.x).ToString();
// 			if (currentDistanceText1)
// 				currentDistanceText1.text = Mathf.FloorToInt(player.position.x).ToString();
// 			if (bestDistanceText)
// 				bestDistanceText.text = Mathf.Max(previousBest, current).ToString();
// 			Time.timeScale = 0;
// 		}
// 	}

// 	void PopOutPanel(GameObject panel)
// 	{
// 		if (panel == null) return;
// 		panel.SetActive(true);
// 		panel.transform.localScale = Vector3.zero;
// 		LeanTween.scale(panel, originalScale, bounceTime)
// 			.setEase(LeanTweenType.easeOutBack)
// 			.setIgnoreTimeScale(true);
// 	}
// }






using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	// BKS: when true (set by BKS_GameplayBridge in Bharat Ka Safar levels) the legacy win / game-over
	// menus are not shown and time is not frozen — the BKS Mission Result screen takes over.
	public static bool UseExternalResultScreen = false;

	[Header("Informations")]
	public TMP_Text DistanceTXT;
	public Text RecordTXT;
	public TMP_Text CoinTXT;
	public TMP_Text FuelTXT;
	public Text LastRecord;

	[Header("Sliders")]
	public Slider FuelSlider;
	public Slider DistanceSlider;

	[Header("Audio Control Sliders")]
	public Slider musicVolumeSlider;
	public Slider sfxVolumeSlider;

	Transform player;
	bool Started, Finished;

	[Header("Fuel")]
	public float TotalFuel = 100f;
	public float FuelTime = .6f;
	float FuelVal = .35f;

	int Coins;
	public int RunCoins => UseExternalResultScreen && BharatKaSafar.Rewards.BKS_RewardManager.Instance != null
		? BharatKaSafar.Rewards.BKS_RewardManager.Instance.currentCoins : Coins;
	// Store the exact distance when game ends for accurate display
	private float finalDistance = 0f;
	private bool gameWon = false;

	[Header("Coins And Awards")]
	public GameObject coinAwardedBox;
	public Text awardedText;
	public Animator awardAnimator;

	[Header("Complete Level")]
	public GameObject youWinMenu;
	public GameObject youLostMenu;
	public int winnerAwardedCoins = 30000;
	public int targetDistance;

	[Header("Game Over UI")]
	public TMP_Text totalCoinsText;
	public TMP_Text totalCoinsText1;
	public TMP_Text totalCoinsText2;
	public TMP_Text currentDistanceText;
	public TMP_Text currentDistanceText1;
	public TMP_Text bestDistanceText;

	[Header("Pop-out Animation Settings")]
	public float bounceTime = 0.5f;
	private Vector3 originalScale = Vector3.one;

	[Header("HUD Settings")]
	public GameObject gameplayHUD;

	private bool audioSlidersInitialized = false;

	IEnumerator Start()
	{
		Application.targetFrameRate = 60;
		DistanceSlider.gameObject.SetActive(true);
		DistanceSlider.value = 0;
		DistanceSlider.maxValue = targetDistance;

		Coins = 0;
		CoinTXT.text = Coins.ToString();

		yield return new WaitForEndOfFrame();
		player = GameObject.FindGameObjectWithTag("Car").transform;
		Started = true;
		Finished = false;
		gameWon = false;
		finalDistance = 0f;

		InitializeDistanceMilestones();
		StartCoroutine(InitializeAudioSliders());

		while (!Finished) {
			yield return new WaitForSeconds(FuelTime);
			TotalFuel -= FuelVal;
			FuelSlider.value = TotalFuel;
			if (TotalFuel >= 0)
				FuelTXT.text = Mathf.Floor(TotalFuel).ToString();
			if (TotalFuel < 0) {
				fuelFinished = true;
				StartFuelFinish();
			}
		}
	}

	IEnumerator InitializeAudioSliders()
	{
		// Wait for AudioManager to be ready
		while (AudioManager.instance == null)
		{
			yield return null;
		}

		// Setup audio sliders if they exist
		SetupAudioSliders();
		RefreshAudioSliders();
		audioSlidersInitialized = true;
	}

	void SetupAudioSliders()
	{
		// Setup music volume slider
		if (musicVolumeSlider)
		{
			musicVolumeSlider.onValueChanged.RemoveAllListeners();
			musicVolumeSlider.onValueChanged.AddListener(delegate { SetMusicVolume(); });
		}

		// Setup SFX volume slider (controls both SFX and engine sounds)
		if (sfxVolumeSlider)
		{
			sfxVolumeSlider.onValueChanged.RemoveAllListeners();
			sfxVolumeSlider.onValueChanged.AddListener(delegate { SetSFXVolume(); });
		}
	}

	public void RefreshAudioSliders()
	{
		if (AudioManager.instance == null) return;

		// Temporarily disable listeners to avoid triggering changes during refresh
		bool listenersEnabled = audioSlidersInitialized;
		if (listenersEnabled)
		{
			if (musicVolumeSlider) musicVolumeSlider.onValueChanged.RemoveAllListeners();
			if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
		}

		// Update slider values to match AudioManager settings
		if (musicVolumeSlider)
			musicVolumeSlider.value = AudioManager.instance.GetMusicVolume();

		if (sfxVolumeSlider)
			sfxVolumeSlider.value = AudioManager.instance.GetSFXVolume();

		// Re-enable listeners
		if (listenersEnabled)
		{
			SetupAudioSliders();
		}
	}

	// This method can be called when the GameManager is enabled (useful for pause menus)
	void OnEnable()
	{
		if (audioSlidersInitialized && AudioManager.instance != null)
		{
			RefreshAudioSliders();
		}
	}

	public void SetMusicVolume()
	{
		if (AudioManager.instance && musicVolumeSlider)
		{
			AudioManager.instance.SetMusicVolume(musicVolumeSlider.value);
		}
	}

	public void SetSFXVolume()
	{
		if (AudioManager.instance && sfxVolumeSlider)
		{
			// Set both SFX and engine volume to the same value
			AudioManager.instance.SetSFXVolume(sfxVolumeSlider.value);
			AudioManager.instance.SetEngineVolume(sfxVolumeSlider.value);
		}
	}

	// Quick toggle methods for buttons (optional)
	public void ToggleMusic()
	{
		if (AudioManager.instance)
		{
			AudioManager.instance.ToggleMusic();
			RefreshAudioSliders();
		}
	}

	public void ToggleSFX()
	{
		if (AudioManager.instance)
		{
			AudioManager.instance.ToggleSFX();
			// Also toggle engine volume to match SFX volume
			AudioManager.instance.SetEngineVolume(AudioManager.instance.GetSFXVolume());
			RefreshAudioSliders();
		}
	}

	void InitializeDistanceMilestones()
	{
		if (PlayerPrefs.GetInt("c500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c500 = true;
			LastRecord.text = "Record:500";
		}
		if (PlayerPrefs.GetInt("c1000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c1000 = true;
			LastRecord.text = "Record:1000";
		}
		if (PlayerPrefs.GetInt("c1500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c1500 = true;
			LastRecord.text = "Record:1500";
		}
		if (PlayerPrefs.GetInt("c2000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c2000 = true;
			LastRecord.text = "Record:2000";
		}
		if (PlayerPrefs.GetInt("c2500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c2500 = true;
			LastRecord.text = "Record:2500";
		}
		if (PlayerPrefs.GetInt("c3000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c3000 = true;
			LastRecord.text = "Record:3000";
		}
		if (PlayerPrefs.GetInt("c3500" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c3500 = true;
			LastRecord.text = "Record:3500";
		}
		if (PlayerPrefs.GetInt("c4000" + PlayerPrefs.GetInt("SelectedLevel").ToString()) == 3) {
			c4000 = true;
			LastRecord.text = "Record:4000";
		}
	}

	float DistanceTemp;

	void Update()
	{
		if (UseExternalResultScreen && CoinTXT != null) CoinTXT.text = RunCoins.ToString();
		if (Started && !Finished) {
			CoinDistance();
			if (player.position.x >= DistanceTemp) {
				DistanceTXT.text = Mathf.Floor(player.position.x).ToString();
				DistanceTemp = player.position.x;
				DistanceSlider.value = player.position.x;
			}
		}
	}

	public void AddCoin(int value)
	{
		if (UseExternalResultScreen)
		{
			var rewards = BharatKaSafar.Rewards.BKS_RewardManager.Instance;
			if (rewards != null) rewards.AddCoin(value);
			if (CoinTXT != null) CoinTXT.text = RunCoins.ToString();
			if (AudioManager.instance) AudioManager.instance.PlayCoinSound();
			return;
		}
		StartCoroutine(TakeCoins());

		CoinTXT.transform.localScale = new Vector3(CoinTXT.transform.localScale.x, CoinTXT.transform.localScale.y + 0.7f, CoinTXT.transform.localScale.z);

		if (AudioManager.instance)
			AudioManager.instance.PlayCoinSound();

		Coins += value;
		CoinTXT.text = Coins.ToString();
		PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins", 0) + value);
	}

	IEnumerator TakeCoins()
	{
		yield return new WaitForSeconds(0.03f);
		CoinTXT.transform.localScale = new Vector3(CoinTXT.transform.localScale.x, CoinTXT.transform.localScale.y - 0.7f, CoinTXT.transform.localScale.z);
	}

	public void AddFuel(int value)
	{
		if (AudioManager.instance)
			AudioManager.instance.PlayFuelSound();
		
		TotalFuel = value;
	}

	bool c500, c1000, c1500, c2000, c2500, c3000, c3500, c4000;

	void CoinDistance()
	{
		CheckDistanceMilestone(500, ref c500, "c500");
		CheckDistanceMilestone(1000, ref c1000, "c1000");
		CheckDistanceMilestone(1500, ref c1500, "c1500");
		CheckDistanceMilestone(2000, ref c2000, "c2000");
		CheckDistanceMilestone(2500, ref c2500, "c2500");
		CheckDistanceMilestone(3000, ref c3000, "c3000");
		CheckDistanceMilestone(3500, ref c3500, "c3500");
		CheckDistanceMilestone(4000, ref c4000, "c4000");

		if (player.transform.position.x >= targetDistance && !Finished) {
			Finished = true;
			gameWon = true; // Mark that the player has won
			finalDistance = player.position.x; // Store exact distance for accurate display

			// BKS owns completion rewards and the result transition. Keep its vehicle
			// alive so the bridge can observe the finish regardless of Update order.
			if (UseExternalResultScreen) return;
			
			if (AudioManager.instance)
				AudioManager.instance.PlayVictorySound();
			
			string key = "BestDistance" + (SceneManager.GetActiveScene().buildIndex - 1).ToString();
			int previousBest = PlayerPrefs.GetInt(key);
			int current = Mathf.CeilToInt(finalDistance);
			if (current > previousBest) {
				PlayerPrefs.SetInt(key, current);
			}
			if (totalCoinsText2)
				totalCoinsText2.text = RunCoins.ToString();
			
			// Show win menu immediately without delay
			PopOutPanel(youWinMenu);
			AddCoin(winnerAwardedCoins);
			GameObject.FindGameObjectWithTag("Player").transform.parent.gameObject.SetActive(false);
		}
	}

	void CheckDistanceMilestone(int distance, ref bool milestoneReached, string prefKey)
	{
		// BKS run coins count pickups; its completion bonus is saved by MissionResult.
		if (UseExternalResultScreen) return;
		int nextDistance = distance == 4000 ? 4500 : distance + 500;
		
		if (!milestoneReached && player.transform.position.x >= distance && player.transform.position.x < nextDistance) {
			AddCoin(distance);
			coinAwardedBox.SetActive(true);
			awardAnimator.SetBool("Award", true);
			awardedText.text = distance.ToString() + " Coins Awarded";
			StartCoroutine(Awardfalse());
			milestoneReached = true;
			PlayerPrefs.SetInt(prefKey + PlayerPrefs.GetInt("SelectedLevel").ToString(), 3);
			LastRecord.text = "Record:" + distance.ToString();
		}
	}

	IEnumerator Awardfalse()
	{
		yield return new WaitForSeconds(3f);
		awardAnimator.SetBool("Award", false);
		yield return new WaitForSeconds(3f);
		coinAwardedBox.SetActive(false);
	}

	[HideInInspector] public bool isDead;
	[HideInInspector] public bool fuelFinished;

	public void StartDead()
	{
		StartCoroutine(Dead());
	}

	IEnumerator Dead()
	{
		// Reduced delay for faster game over
		yield return new WaitForSeconds(0.5f);
		
		// Only show game over if the player hasn't won
		if (isDead && !gameWon) {
			if (AudioManager.instance)
				AudioManager.instance.PlayDieSound();
			
			// Store the exact distance when the player died
			finalDistance = player.position.x;
			
			string key = "BestDistance" + (SceneManager.GetActiveScene().buildIndex - 1).ToString();
			int previousBest = PlayerPrefs.GetInt(key);
			int current = Mathf.FloorToInt(finalDistance); // Use exact final distance
			if (current > previousBest) {
				PlayerPrefs.SetInt(key, current);
			}
			
			PopOutPanel(youLostMenu);
			if (totalCoinsText)
				totalCoinsText.text = RunCoins.ToString();
			if (totalCoinsText1)
				totalCoinsText1.text = RunCoins.ToString();
			if (currentDistanceText)
				currentDistanceText.text = current.ToString(); // Use exact distance
			if (currentDistanceText1)
				currentDistanceText1.text = current.ToString(); // Use exact distance
			if (bestDistanceText)
				bestDistanceText.text = Mathf.Max(previousBest, current).ToString();
			if (!UseExternalResultScreen) Time.timeScale = 0;
		}
	}

	public void StartFuelFinish()
	{
		StartCoroutine(DeadFuel());
	}

	IEnumerator DeadFuel()
	{
		// Reduced delay for faster game over
		yield return new WaitForSeconds(0.1f);
		
		// Only show game over if the player hasn't won
		if (fuelFinished && !gameWon) {
			if (AudioManager.instance)
				AudioManager.instance.PlayDieSound();
			
			// Store the exact distance when fuel finished
			finalDistance = player.position.x;
			
			string key = "BestDistance" + (SceneManager.GetActiveScene().buildIndex - 1).ToString();
			int previousBest = PlayerPrefs.GetInt(key);
			int current = Mathf.FloorToInt(finalDistance); // Use exact final distance
			if (current > previousBest) {
				PlayerPrefs.SetInt(key, current);
			}
			
			PopOutPanel(youLostMenu);
			if (totalCoinsText)
				totalCoinsText.text = RunCoins.ToString();
			if (totalCoinsText1)
				totalCoinsText1.text = RunCoins.ToString();
			if (currentDistanceText)
				currentDistanceText.text = current.ToString(); // Use exact distance
			if (currentDistanceText1)
				currentDistanceText1.text = current.ToString(); // Use exact distance
			if (bestDistanceText)
				bestDistanceText.text = Mathf.Max(previousBest, current).ToString();
			if (!UseExternalResultScreen) Time.timeScale = 0;
		}
	}

	void PopOutPanel(GameObject panel)
	{
		if (panel == null) return;
		if (UseExternalResultScreen && (panel == youLostMenu || panel == youWinMenu)) return; // BKS result screen instead
		panel.SetActive(true);
		panel.transform.localScale = Vector3.zero;
		LeanTween.scale(panel, originalScale, bounceTime)
			.setEase(LeanTweenType.easeOutBack)
			.setIgnoreTimeScale(true);

		// Automatically find and hide the gameplay UI elements so they don't show over the Game Over menu
		string[] elementsToHide = { 
			"Top Left Icons", 
			"DistanceSlider (1)", 
			"Throttle", 
			"Brake",
			"TopStatusPanel",
			"TopScorePanel",
			"TopScore Panel",
			"Speedometer",
			"BKS_HornButton",
			"HornBtn",
			"Horn",
			"Pause Menu"
		};
		
		foreach(string elementName in elementsToHide)
		{
			GameObject uiElement = GameObject.Find(elementName);
			if (uiElement != null)
			{
				uiElement.SetActive(false);
			}
		}

		if (gameplayHUD != null)
		{
			gameplayHUD.SetActive(false);
		}
	}
}
