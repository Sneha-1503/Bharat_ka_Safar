using UnityEngine;
using System.Collections;

public enum ScoreType
{
	Coin,
	Fuel
}

public class ScoreItem : MonoBehaviour
{
	public int Score = 100;
	GameManager man;
	SpriteRenderer sprite;
	public Vector3 Up__Vector3 = new Vector3(0,7f,0);
	public float speed  = 2.3f , FadeSpeed =  2.3f;
	public ScoreType type;
	Transform trans;

	void Start ()
	{
		trans = GetComponent<Transform> ();
		man = GameObject.FindFirstObjectByType<GameManager> ();
		sprite = GetComponent<SpriteRenderer> ();
	}
	//trigger-----------------------------------------------
	void OnTriggerEnter2D (Collider2D col)
	{
		if (col.CompareTag("CoinTrigger")) {
		
			if (type == ScoreType.Coin)
				man.AddCoin (Score);
			if (type == ScoreType.Fuel)
				man.AddFuel (100);
			canCompute = true;
			Fade = true;
		}
	}

	bool Fade;

	void Update ()
	{
		if (canCompute) {
			if (Fade)
			{
				trans.Translate (Up__Vector3 * Time.deltaTime * speed);

				if (sprite.material.color.a > 0) {
					sprite.material.color = new Color (1f, 1f, 1f, sprite.material.color.a - FadeSpeed * Time.deltaTime);
				} else
					Destroy (gameObject);
			}
		}
	}
	public bool canCompute;
}
