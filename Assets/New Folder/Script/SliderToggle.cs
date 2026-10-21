using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slider))]
public class SliderToggle : MonoBehaviour, IPointerDownHandler
{
    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();

        // Ensure it's a binary slider (0 or 1)
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = true;
    }

    // Called when the user clicks/taps the slider
    public void OnPointerDown(PointerEventData eventData)
    {
        ToggleSlider();
    }

    void ToggleSlider()
    {
        slider.value = (slider.value == 0) ? 1 : 0;
    }
}
