using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UpgradeCar : MonoBehaviour
{

	public Sprite _select, _unSelect;

	public UpgradeItem[] _item;

	public Text priceText, contentText,headText;

	public int _type;

	public GameObject loading;

	int id;
	[HideInInspector]public int Engine, Fuel, Suspension, Speed;

	// Use this for initialization
	void Start ()
	{
	
		SetChooseItem (0);
		LoadUpgrade ();
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}

	public void SetChooseItem (int _index)
	{
		contentText.text = _item [_index]._content;
		_type = _index;
		if (_index == 0) {
			_item [0]._border.sprite = _select;
			_item [1]._border.sprite = _unSelect;
			_item [2]._border.sprite = _unSelect;
			_item [3]._border.sprite = _unSelect;
			priceText.text = "Upgrade cost : " + _item [0].price [PlayerPrefs.GetInt ("Engine" + id.ToString ())].ToString () + " coins";
			headText.text = "Upgrade Engine";
		} else if (_index == 1) {

			_item [0]._border.sprite = _unSelect;
			_item [1]._border.sprite = _select;
			_item [2]._border.sprite = _unSelect;
			_item [3]._border.sprite = _unSelect;
			priceText.text = "Upgrade cost : " + _item [1].price [PlayerPrefs.GetInt ("Suspension" + id.ToString ())].ToString () + " coins";
			headText.text = "Upgrade Suspension";
		} else if (_index == 2) {

			_item [0]._border.sprite = _unSelect;
			_item [1]._border.sprite = _unSelect;
			_item [2]._border.sprite = _select;
			_item [3]._border.sprite = _unSelect;
			priceText.text = "Upgrade cost : " + _item [2].price [PlayerPrefs.GetInt ("Speed" + id.ToString ())].ToString () + " coins";
			headText.text = "Upgrade Tired";
		} else if (_index == 3) {

			_item [0]._border.sprite = _unSelect;
			_item [1]._border.sprite = _unSelect;
			_item [2]._border.sprite = _unSelect;
			_item [3]._border.sprite = _select;
			priceText.text = "Upgrade cost : " + _item [3].price [PlayerPrefs.GetInt ("Fuel" + id.ToString ())].ToString () + " coins";
			headText.text = "Upgrade Fuel";
		}
	}

	public void Upgrade ()
	{
		if (_type == 0)
			EngineUpgrade ();
		else if (_type == 1)
			SuspensionUpgrade ();
		else if (_type == 2)
			SpeedUpgrade ();
		else if (_type == 3)
			FuelUpgrade ();
	}

	public void LoadUpgrade ()
	{


		id = PlayerPrefs.GetInt ("SelectedCar");

		Engine = PlayerPrefs.GetInt ("Coins" + id.ToString ());
		Fuel = PlayerPrefs.GetInt ("Fuel" + id.ToString ());
		Suspension = PlayerPrefs.GetInt ("Suspension" + id.ToString ());
		Speed = PlayerPrefs.GetInt ("Speed" + id.ToString ());

		_item [0].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Engine" + id.ToString ()).ToString () + " / " + _item [0].price.Length.ToString ();
		_item [1].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Suspension" + id.ToString ()).ToString () + " / " + _item [1].price.Length.ToString ();
		_item [2].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Speed" + id.ToString ()).ToString () + " / " + _item [2].price.Length.ToString ();
		_item [3].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Fuel" + id.ToString ()).ToString () + " / " + _item [3].price.Length.ToString ();



	}


	public void EngineUpgrade ()
	{
		SetChooseItem (0);
		if (PlayerPrefs.GetInt ("Engine" + id.ToString ()) < _item [0].price.Length) {

			if (PlayerPrefs.GetInt ("Coins") >= _item [0].price [PlayerPrefs.GetInt ("Engine" + id.ToString ())]) {
				HomeManager._homeManager.ChangeCoin (_item [0].price [PlayerPrefs.GetInt ("Engine" + id.ToString ())]);
				PlayerPrefs.SetInt ("Engine" + id.ToString (), PlayerPrefs.GetInt ("Engine" + id.ToString ()) + 1);
				_item [0].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Engine" + id.ToString ()).ToString () + " / " + _item [0].price.Length.ToString ();

				if (PlayerPrefs.GetInt ("Engine" + id.ToString ()) < _item [0].price.Length)
					priceText.text = "Upgrade cost : " + _item [0].price [PlayerPrefs.GetInt ("Engine" + id.ToString ())].ToString () + " coins";
				else
					priceText.text = "Completed";
			} else {
				HomeManager._homeManager.OpenPurchase ();


			}

		}
	}

	public void SuspensionUpgrade ()
	{
		SetChooseItem (1);
		if (PlayerPrefs.GetInt ("Suspension" + id.ToString ()) < _item [1].price.Length) {

			if (PlayerPrefs.GetInt ("Coins") >= _item [1].price [PlayerPrefs.GetInt ("Suspension" + id.ToString ())]) {
				
				HomeManager._homeManager.ChangeCoin (_item [1].price [PlayerPrefs.GetInt ("Suspension" + id.ToString ())]);
				PlayerPrefs.SetInt ("Suspension" + id.ToString (), PlayerPrefs.GetInt ("Suspension" + id.ToString ()) + 1);
				_item [1].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Suspension" + id.ToString ()).ToString () + " / " + _item [1].price.Length.ToString ();

				if (PlayerPrefs.GetInt ("Suspension" + id.ToString ()) < _item [1].price.Length)
					priceText.text = "Upgrade cost : " + _item [1].price [PlayerPrefs.GetInt ("Suspension" + id.ToString ())].ToString () + " coins";
				else
					priceText.text = "Completed";
			} else {
				HomeManager._homeManager.OpenPurchase ();
			}
		}
	}

	public void FuelUpgrade ()
	{
		SetChooseItem (3);
		if (PlayerPrefs.GetInt ("Fuel" + id.ToString ()) < _item [3].price.Length) {

			if (PlayerPrefs.GetInt ("Coins") >= _item [3].price [PlayerPrefs.GetInt ("Fuel" + id.ToString ())]) {
				
				HomeManager._homeManager.ChangeCoin (_item [3].price [PlayerPrefs.GetInt ("Fuel" + id.ToString ())]);
				PlayerPrefs.SetInt ("Fuel" + id.ToString (), PlayerPrefs.GetInt ("Fuel" + id.ToString ()) + 1);
				_item [3].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Fuel" + id.ToString ()).ToString () + " / " + _item [3].price.Length.ToString ();

				if (PlayerPrefs.GetInt ("Fuel" + id.ToString ()) < _item [3].price.Length)
					priceText.text = "Upgrade cost : " + _item [3].price [PlayerPrefs.GetInt ("Fuel" + id.ToString ())].ToString () + " coins";
				else
					priceText.text = "Completed";
			} else {
				HomeManager._homeManager.OpenPurchase ();

			}
		}
	}




	public void SpeedUpgrade ()
	{
		SetChooseItem (2);
		if (PlayerPrefs.GetInt ("Speed" + id.ToString ()) < _item [2].price.Length) {

			if (PlayerPrefs.GetInt ("Coins") >= _item [2].price [PlayerPrefs.GetInt ("Speed" + id.ToString ())]) {
				
				HomeManager._homeManager.ChangeCoin (_item [2].price [PlayerPrefs.GetInt ("Speed" + id.ToString ())]);
				PlayerPrefs.SetInt ("Speed" + id.ToString (), PlayerPrefs.GetInt ("Speed" + id.ToString ()) + 1);
				_item [2].levelInfo.text = "Level: " + PlayerPrefs.GetInt ("Speed" + id.ToString ()).ToString () + " / " + _item [0].price.Length.ToString ();

				if (PlayerPrefs.GetInt ("Speed" + id.ToString ()) < _item [2].price.Length)
					priceText.text = "Upgrade cost : " + _item [2].price [PlayerPrefs.GetInt ("Speed" + id.ToString ())].ToString () + " coins";
				else
					priceText.text = "Completed";
			} else {
				
				HomeManager._homeManager.OpenPurchase ();

			}
		}
	}


	public void StartGame ()
	{

		//Loading.SetActive (true);
		//PlayerPrefs.SetInt ("AllScoreTemp", PlayerPrefs.GetInt ("Coins"));
		UnityEngine.SceneManagement.SceneManager.LoadSceneAsync ("Level"+PlayerPrefs.GetInt ("SelectedLevel").ToString());

		//sceneLoading.ActivateNextScene();
		loading.SetActive(true);
		//gameObject.SetActive (false);

	}

	[System.Serializable]
	public class UpgradeItem
	{
		public Text levelInfo;

		public Image _border;

		public int[] price;

		public string _content;
	}
}
