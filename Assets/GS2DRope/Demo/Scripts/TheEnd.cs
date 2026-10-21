using UnityEngine;
using System.Collections;

public class TheEnd : MonoBehaviour
{
    public GameObject panelTheEnd;
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            //panelTheEnd.SetActive(true);
            print("The End");
        }
    }
}
