using UnityEngine;
using System.Collections;

public class DeadTrigger : MonoBehaviour {

	GameManager manager;

	void Start () {
		manager = GameObject.FindFirstObjectByType<GameManager> ();
	}

	public   bool enter;


	void OnTriggerEnter2D(Collider2D col)
	{
		if (manager == null) return;
		if (col.tag == "Ground" || col.tag == "rope2D") {
			manager.isDead = true;
			// BKS confirms the vehicle's tilt and recovery time in its gameplay bridge.
			if (manager != null && !GameManager.UseExternalResultScreen) manager.StartDead ();
			enter = true;
		}
	}
	void OnTriggerExit2D(Collider2D col)
	{
		if (manager == null) return;
		if (col.tag == "Ground" || col.tag == "rope2D") {
			manager.isDead = false;
			enter = false;
		}
	}
}
