using UnityEngine;

namespace BharatKaSafar.Obstacles
{
    /// <summary>
    /// Pattern 3 — Mud / Sand / Snow patch (a trigger area along the road).
    /// While the car is inside: less grip, more drag, weaker engine — the player must keep the accelerator held
    /// to keep momentum; too slow and the car nearly gets stuck. Same mechanic, different preset and sprite
    /// (Rajasthan sand, Assam mud, Sikkim snow).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BKS_SurfaceZone : MonoBehaviour
    {
        public enum Surface { Mud, Sand, Snow, Custom }

        [SerializeField] private Surface surface = Surface.Mud;
        [Header("Custom values (used when Surface = Custom)")]
        [Tooltip("Extra linear drag on the car body")]
        [SerializeField, Min(0f)] private float extraDrag = 1.2f;
        [Tooltip("Engine power multiplier")]
        [SerializeField, Range(0.1f, 1f)] private float enginePower = 0.65f;
        [Tooltip("Tyre friction multiplier")]
        [SerializeField, Range(0.05f, 1f)] private float tyreGrip = 0.5f;

        private CarController car;
        private Rigidbody2D body;
        private float savedDrag, savedPower;
        private readonly System.Collections.Generic.List<(Collider2D col, PhysicsMaterial2D mat)> savedTyres =
            new System.Collections.Generic.List<(Collider2D, PhysicsMaterial2D)>();
        private int inside;

        private void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }
        private void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

        private void Values(out float drag, out float power, out float grip)
        {
            switch (surface)
            {
                case Surface.Mud:  drag = 1.5f; power = 0.6f;  grip = 0.45f; break;
                case Surface.Sand: drag = 1.0f; power = 0.7f;  grip = 0.6f;  break;
                case Surface.Snow: drag = 0.3f; power = 0.85f; grip = 0.2f;  break;
                default: drag = extraDrag; power = enginePower; grip = tyreGrip; break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var c = Vehicle.BKS_VehicleUtil.GetCar(other.gameObject);
            if (c == null) return;
            inside++;
            if (car != null) return; // already applied
            car = c;
            body = car.GetComponent<Rigidbody2D>();
            Values(out float drag, out float power, out float grip);

            if (body != null) { savedDrag = body.linearDamping; body.linearDamping = savedDrag + drag; }
            savedPower = car.motorPower;
            car.motorPower = savedPower * power;

            savedTyres.Clear();
            foreach (var col in car.transform.root.GetComponentsInChildren<CircleCollider2D>())
            {
                if (col.isTrigger) continue;
                savedTyres.Add((col, col.sharedMaterial));
                var src = col.sharedMaterial;
                col.sharedMaterial = new PhysicsMaterial2D(name + "_Tyre")
                {
                    friction = (src != null ? src.friction : 0.6f) * grip,
                    bounciness = src != null ? src.bounciness : 0f
                };
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (Vehicle.BKS_VehicleUtil.GetCar(other.gameObject) == null) return;
            inside = Mathf.Max(0, inside - 1);
            if (inside == 0) Restore();
        }

        private void OnDisable() => Restore();

        private void Restore()
        {
            if (car == null) return;
            if (body != null) body.linearDamping = savedDrag;
            car.motorPower = savedPower;
            foreach (var (col, mat) in savedTyres) if (col != null) col.sharedMaterial = mat;
            savedTyres.Clear();
            car = null; body = null; inside = 0;
        }

        private void OnDrawGizmos()
        {
            var b = GetComponent<BoxCollider2D>();
            if (b == null) return;
            Gizmos.color = surface == Surface.Snow ? new Color(0.8f, 0.9f, 1f, 0.35f)
                         : surface == Surface.Sand ? new Color(1f, 0.85f, 0.4f, 0.35f) : new Color(0.45f, 0.3f, 0.15f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(b.offset, b.size);
        }
    }
}
