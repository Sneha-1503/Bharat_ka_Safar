using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// Scales a background sprite (child of the camera) so it always covers the whole camera view,
    /// on any screen shape (4:3 tablets to 20:9 phones). Keeps the image's aspect ratio.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class BKS_CameraBackgroundFit : MonoBehaviour
    {
        [Tooltip("Extra size so camera shake / zoom never shows an edge")]
        [SerializeField, Min(1f)] private float margin = 1.08f;

        private SpriteRenderer sr;
        private Camera cam;
        private float lastAspect, lastSize;
        private Sprite lastSprite;

        private void OnEnable()
        {
            sr = GetComponent<SpriteRenderer>();
            cam = GetComponentInParent<Camera>();
            if (cam == null) cam = Camera.main;
            lastAspect = -1f;
        }

        private void LateUpdate()
        {
            if (sr == null || sr.sprite == null || cam == null || !cam.orthographic) return;
            if (Mathf.Approximately(cam.aspect, lastAspect) && Mathf.Approximately(cam.orthographicSize, lastSize) && sr.sprite == lastSprite) return;
            lastAspect = cam.aspect; lastSize = cam.orthographicSize; lastSprite = sr.sprite;

            float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
            Vector2 sprite = sr.sprite.bounds.size;
            // parent scale (the camera is normally unscaled, but be safe)
            Vector3 ps = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            // keep the designer's offset, but still cover the view on every side
            Vector3 off = transform.localPosition;
            float needW = viewW + 2f * Mathf.Abs(off.x * ps.x), needH = viewH + 2f * Mathf.Abs(off.y * ps.y);
            float s = Mathf.Max(needW / (sprite.x * ps.x), needH / (sprite.y * ps.y)) * margin;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
