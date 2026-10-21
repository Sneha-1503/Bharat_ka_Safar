using System.Collections.Generic;
using UnityEngine;

namespace Gelzo_Games
{
    public class Next_Previous : MonoBehaviour
    {
        int index;
        int amountUI;
        [SerializeField] List<GameObject> canvases = new List<GameObject>();

        private void Start()
        {
            amountUI = canvases.Count;
        }
        void Update()
        {

            if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > Screen.width / 2)
            {
                if (index < amountUI - 1)
                {
                    index++;
                    UpdateActiveListCanvas();
                }
                Debug.Log("right" + index + " index");

            }
            else if (Input.GetMouseButtonDown(0) && Input.mousePosition.x <= Screen.width / 2)
            {
                if (index > 0)
                {
                    index--;
                    UpdateActiveListCanvas();
                }
                Debug.Log("left" + index + " index");
            }
        }
        void UpdateActiveListCanvas()
        {
            for (int i = 0; i < canvases.Count; i++)
            {
                canvases[i].SetActive(false);
            }
            canvases[index].SetActive(true);
        }
    }

}
