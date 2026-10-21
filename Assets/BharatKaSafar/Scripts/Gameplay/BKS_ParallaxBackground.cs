using System.Collections.Generic;
using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    /// <summary>
    /// Parallax background made of layers (sky … foreground), placed as a child of the Main Camera.
    /// Each child SpriteRenderer is one layer (set up by Bharat Ka Safar > Parallax > Apply ...):
    ///  • the layer fills the camera height (plus a little margin) and keeps the art's proportions
    ///  • it scrolls against the camera by its own amount: far layers barely move, near layers move more
    ///  • it repeats forever sideways; every second copy is mirrored so the joins never show
    ///  • it also moves a little up/down with the camera so climbing hills feels deep
    /// The art itself lives in the children, so the layers are visible and editable in the Scene view.
    /// </summary>
    public class BKS_ParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public class Layer
        {
            public SpriteRenderer renderer;
            [Tooltip("How much this layer moves with the world: 0 = stays with the camera (very far), 1 = moves like the road")]
            [Range(0f, 1f)] public float scroll = 0.3f;
            [Tooltip("How much it follows height changes (0 = stays fixed on screen)")]
            [Range(0f, 1f)] public float vertical = 0.15f;
        }

        public List<Layer> layers = new List<Layer>();
        [Tooltip("Layer height as a part of the camera height (1.15 = a bit bigger than the screen)")]
        public float cover = 1.15f;
        [Tooltip("Push all layers up/down on screen (in camera heights)")]
        public float verticalOffset = 0f;

        private const int Copies = 5;
        private Camera cam;
        private Vector3 startCam;
        private readonly List<SpriteRenderer[]> tiles = new List<SpriteRenderer[]>();

        private void Start()
        {
            cam = GetComponentInParent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) { enabled = false; return; }
            startCam = cam.transform.position;

            // 5 copies per layer (normal / mirrored in turn) cover even very wide screens while scrolling
            foreach (var l in layers)
            {
                if (l.renderer == null || l.renderer.sprite == null) { tiles.Add(null); continue; }
                var set = new SpriteRenderer[Copies];
                set[0] = l.renderer;
                for (int i = 1; i < Copies; i++)
                {
                    var copy = Instantiate(l.renderer.gameObject, l.renderer.transform.parent);
                    copy.name = l.renderer.name + "_" + i;
                    set[i] = copy.GetComponent<SpriteRenderer>();
                }
                tiles.Add(set);
            }
            LateUpdate();
        }

        /// <summary>Visible size (world units) at distance <paramref name="depth"/> in front of the camera — works for perspective and orthographic cameras.</summary>
        public static Vector2 ViewSize(Camera c, float depth)
        {
            float h = c.orthographic ? 2f * c.orthographicSize : 2f * Mathf.Max(0.01f, depth) * Mathf.Tan(c.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return new Vector2(h * c.aspect, h);
        }

        /// <summary>One scale for all layers (they share one picture frame): fills the height × cover and is never narrower than the screen.</summary>
        public static float FitScale(Sprite sprite, Vector2 view, float cover)
        {
            Vector2 size = sprite.bounds.size;
            return Mathf.Max(view.y * cover / size.y, view.x * 1.05f / size.x);
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            Vector2 view = ViewSize(cam, transform.localPosition.z);
            float viewH = view.y;
            Vector3 camPos = cam.transform.position;
            for (int li = 0; li < layers.Count; li++)
            {
                var set = li < tiles.Count ? tiles[li] : null;
                if (set == null) continue;
                var l = layers[li];
                var sprite = set[0].sprite;
                float scale = FitScale(sprite, view, cover);
                float w = sprite.bounds.size.x * scale;                         // one copy, in world units
                // the layer moves against the camera by "scroll"; the pattern repeats every 2 copies (normal + mirrored)
                float shift = Mathf.Repeat((camPos.x - startCam.x) * l.scroll, 2f * w);
                float y = -(camPos.y - startCam.y) * l.vertical + verticalOffset * viewH;
                for (int i = 0; i < Copies; i++)
                {
                    int k = i - 1;                                              // copies at -1, 0, 1, 2, 3 widths
                    var t = set[i].transform;
                    t.localScale = new Vector3(scale, scale, 1f);
                    t.localPosition = new Vector3(k * w - shift, y, t.localPosition.z);
                    set[i].flipX = ((k % 2) + 2) % 2 == 1;                       // every second copy mirrored
                }
            }
        }
    }
}
