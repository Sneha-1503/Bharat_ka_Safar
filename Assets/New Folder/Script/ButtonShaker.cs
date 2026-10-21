/*using UnityEngine;
using UnityEngine.UI;

public class ButtonShaker : MonoBehaviour
{
    public float shakeDuration;
    public float shakeMagnitude;
    public float timeBetweenShakes;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private float timer;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        timer = timeBetweenShakes;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            StartCoroutine(Shake());
            timer = timeBetweenShakes;
        }
    }

    System.Collections.IEnumerator Shake()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            rectTransform.anchoredPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
    }
}
*/

using UnityEngine;

public class ButtonPulse : MonoBehaviour
{
    public float pulseScale = 1.1f;       // How big the scale gets
    public float pulseTime = 0.3f;        // Duration of one scale in/out
    public float delayBetweenPulses = 3f; // Time between each pulse

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
        InvokeRepeating(nameof(DoPulse), delayBetweenPulses, delayBetweenPulses + (pulseTime * 2));
    }

    void DoPulse()
    {
        LeanTween.scale(gameObject, originalScale * pulseScale, pulseTime).setEaseOutQuad().setOnComplete(() =>
        {
            LeanTween.scale(gameObject, originalScale, pulseTime).setEaseInQuad();
        });
    }
}
