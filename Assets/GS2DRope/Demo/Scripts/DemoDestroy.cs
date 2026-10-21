using UnityEngine;
using System.Collections;

public class DemoDestroy : MonoBehaviour {
    
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.name.Contains("CarWheel"))
        {
            Destroy(gameObject);
        }
    }
}
