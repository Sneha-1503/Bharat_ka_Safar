using UnityEngine;
using System.Collections;

public class StartPoint : MonoBehaviour {

	public GameObject[] cars;

	void Start () {
		// BKS: guard against a saved car index that this level does not have (was IndexOutOfRange crash)
		int carIndex = PlayerPrefs.GetInt ("SelectedCar");
		if (cars == null || cars.Length == 0) { Debug.LogError ("[StartPoint] No cars assigned."); return; }
		if (carIndex < 0 || carIndex >= cars.Length || cars [carIndex] == null) carIndex = 0;
		Instantiate (cars [carIndex], transform.position, transform.rotation);
		//Instantiate (cars [10], transform.position, transform.rotation);
	}
}
