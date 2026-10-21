// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;

// public class MapSelector : MonoBehaviour
// {


// 	public Item[] _item;

// 	public GameObject purchasePanel;

// 	public enum ITEM_TYPE
// 	{
// 		LEVEL,
// 		VEHICLE}

// 	;

// 	public ITEM_TYPE _type;

// 	// Use this for initialization
// 	void Start ()
// 	{
// 		//PlayerPrefs.DeleteAll ();
// 		PlayerPrefs.SetInt ("Level0", 1);
// 		PlayerPrefs.SetInt ("Car0", 1);
// 		LoadItemInfor ();
// 	}
	
// 	// Update is called once per frame
// 	void Update ()
// 	{
	
// 	}

// 	public void ChooseItem (int _index)
// 	{
// 		string prefix = "";
// 		if (_type == ITEM_TYPE.LEVEL)
// 			prefix = "Level";
// 		else
// 			prefix = "Car";
// 		int _lock = PlayerPrefs.GetInt (prefix + _index.ToString ());

// 		if (_lock != 0) {
// 			//buy
// 			//Debug.Log ("opened");
// 			if (_type == ITEM_TYPE.LEVEL) {
// 				HomeManager._homeManager.OpenVehiclePanel ();
// 				PlayerPrefs.SetInt ("SelectedLevel", _index);
// 			} else if (_type == ITEM_TYPE.VEHICLE) {
// 				HomeManager._homeManager.OpenUpgradeVehiclePanel ();
// 				PlayerPrefs.SetInt ("SelectedCar", _index);
// 			}

// 		} 
// 		else {
// 			//Debug.Log ("map is locked : ");

// 			if (PlayerPrefs.GetInt ("Coins") >= _item [_index].price) {

// 				HomeManager._homeManager.ChangeCoin (_item [_index].price);

// 				PlayerPrefs.SetInt (prefix + _index.ToString (), 1);
// 				LoadItemInfor ();

// 			} else
// 				purchasePanel.SetActive (true);

// 		}
// 	}


// 	public void LoadItemInfor ()
// 	{
// 		Debug.Log ("map is locked : " + gameObject.name);
// 		string prefix = "";
// 		if (_type == ITEM_TYPE.LEVEL)
// 			prefix = "Level";
// 		else
// 			prefix = "Car";
// 		for (int i = 0; i < _item.Length; i++) {

// 			int _lock = PlayerPrefs.GetInt (prefix + i.ToString ());
// 			if (_lock == 0) {

// 				_item [i].lockMask.enabled = true;
// 				_item [i].mapPrice.enabled = true;
// 				_item [i].mapPrice.text = "Cost : " + _item [i].price.ToString () + " coins";
// 				if (_type == ITEM_TYPE.LEVEL)
// 					_item [i].bestScore.enabled = false;
// 			} else {

// 				_item [i].lockMask.enabled = false;
// 				_item [i].mapPrice.enabled = false;
// 				//_item [i].mapPrice.text = _item [i].price.ToString ();
// 				if (_type == ITEM_TYPE.LEVEL) {
// 					_item [i].bestScore.enabled = true;
// 					int highDistance = PlayerPrefs.GetInt ("BestDistance" + i.ToString ());
// 					_item [i].bestScore.text = "Best : " + highDistance.ToString () + " m";
// 				}
// 			}
// 		}
// 	}

// 	[System.Serializable]
// 	public class Item
// 	{
// 		public Text bestScore;

// 		public Text mapPrice;

// 		public Image lockMask;

// 		public int price;
// 	}
// }
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MapSelector : MonoBehaviour
{
    public Item[] _item;
    public GameObject purchasePanel;

    void Start()
    {
        // Ensure first level is always unlocked
        PlayerPrefs.SetInt("Level0", 1);
        LoadItemInfo();
    }

    public void ChooseItem(int index)
    {
        string levelKey = "Level" + index.ToString();
        bool isUnlocked = PlayerPrefs.GetInt(levelKey, 0) == 1;

        if (isUnlocked)
        {
            // Directly load the selected level
            PlayerPrefs.SetInt("SelectedLevel", index);
            LoadLevel(index);
        }
        else
        {
            // Handle level purchase
            if (PlayerPrefs.GetInt("Coins", 0) >= _item[index].price)
            {
                // Purchase the level
                PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins") - _item[index].price);
                PlayerPrefs.SetInt(levelKey, 1);
                LoadItemInfo();
            }
            else
            {
                // Show purchase panel if not enough coins
                purchasePanel.SetActive(true);
            }
        }
    }

    public void LoadItemInfo()
    {
        for (int i = 0; i < _item.Length; i++)
        {
            string levelKey = "Level" + i.ToString();
            bool isUnlocked = PlayerPrefs.GetInt(levelKey, 0) == 1;

            _item[i].lockMask.enabled = !isUnlocked;
            _item[i].mapPrice.enabled = !isUnlocked;

            if (isUnlocked)
            {
                _item[i].mapPrice.text = "";
                _item[i].bestScore.enabled = true;
                int highDistance = PlayerPrefs.GetInt("BestDistance" + i.ToString(), 0);
                _item[i].bestScore.text = "Best: " + highDistance.ToString() + " m";
            }
            else
            {
                _item[i].mapPrice.text = "Cost: " + _item[i].price.ToString() + " coins";
                _item[i].bestScore.enabled = false;
            }
        }
    }

    private void LoadLevel(int levelIndex)
    {
        // Implement your level loading logic here
        // Example: SceneManager.LoadScene("Level" + (levelIndex + 1));
        // Or use your custom level loading system
        
        Debug.Log("Loading level " + levelIndex);
        // Temporary implementation - replace with your actual level loading code
        SceneManager.LoadScene(levelIndex + 1); // Assuming levels start from scene 1 in build settings
    }

    [System.Serializable]
    public class Item
    {
        public Text bestScore;
        public Text mapPrice;
        public Image lockMask;
        public int price;
    }
}