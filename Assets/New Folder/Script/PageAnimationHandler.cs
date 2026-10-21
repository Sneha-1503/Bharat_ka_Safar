using UnityEngine;

public class PageAnimationHandler : MonoBehaviour
{
    [Header("References")]
    public RectTransform vehicle;
    public RectTransform wheelLeft;
    public RectTransform wheelRight;

    [Header("Vehicle Pop Effect")]
    public float scaleUpSize;
    public float scaleDuration;
    public LeanTweenType scaleEaseType = LeanTweenType.easeOutBack;

    [Header("Wheel Rotation")]
    public float wheelRotationSpeed = 180f;
    private bool isActive = false;
    private Vector3 originalScale;

    private void Start()
    {
        if (vehicle != null)
        {
            originalScale = vehicle.localScale;
        }
    }

    private void Update()
    {
        if (isActive)
        {
            float rotationThisFrame = wheelRotationSpeed * Time.deltaTime;

            if (wheelLeft != null)
                wheelLeft.Rotate(Vector3.forward, -rotationThisFrame);

            if (wheelRight != null)
                wheelRight.Rotate(Vector3.forward, -rotationThisFrame);
        }
    }

    public void Activate()
    {
        isActive = true;

        if (vehicle != null)
        {
            LeanTween.cancel(vehicle.gameObject);
            vehicle.LeanScale(originalScale * scaleUpSize, scaleDuration).setEase(scaleEaseType);
        }
    }

    public void Deactivate()
    {
        isActive = false;

        if (vehicle != null)
        {
            LeanTween.cancel(vehicle.gameObject);
            vehicle.LeanScale(originalScale, scaleDuration).setEase(scaleEaseType);
        }

        ResetWheelRotation(wheelLeft);
        ResetWheelRotation(wheelRight);
    }

    private void ResetWheelRotation(RectTransform wheel)
    {
        if (wheel != null)
        {
            wheel.localRotation = Quaternion.identity;
        }
    }
}
