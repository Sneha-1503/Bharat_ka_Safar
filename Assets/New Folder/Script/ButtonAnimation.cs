using UnityEngine;

public class StartButtonRock : MonoBehaviour
{
    public float tiltAngle;     // Maximum tilt angle in degrees
    public float tiltDuration; // Duration of one tilt
    public float pauseDuration; // Time to pause between tilts

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(RockButton());
    }

    System.Collections.IEnumerator RockButton()
    {
        while (true)
        {
            // Tilt right
            LeanTween.rotateZ(gameObject, -tiltAngle, tiltDuration).setEase(LeanTweenType.easeInOutSine);
            yield return new WaitForSeconds(tiltDuration + pauseDuration);

            // Tilt left
            LeanTween.rotateZ(gameObject, tiltAngle, tiltDuration).setEase(LeanTweenType.easeInOutSine);
            yield return new WaitForSeconds(tiltDuration + pauseDuration);

            // Return to center
            LeanTween.rotateZ(gameObject, 0f, tiltDuration).setEase(LeanTweenType.easeInOutSine);
            yield return new WaitForSeconds(tiltDuration + pauseDuration);
        }
    }
}
