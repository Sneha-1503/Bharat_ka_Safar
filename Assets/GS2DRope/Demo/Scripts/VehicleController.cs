using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public Transform car;
    public int power;
    public int reverse;
    public Vector2 force;
    private Vector3 trigfunction;
    public float maxSpeed = 200f;
    
    void Update()
    {
        trigfunction = car.TransformDirection(Vector3.right);
        force.Set(trigfunction.x, trigfunction.y);

        if (Input.GetKey(KeyCode.F))
            GetComponent<Rigidbody2D>().AddForce(force * power);
        else if (Input.GetKey(KeyCode.R))
            GetComponent<Rigidbody2D>().AddForce(force * -reverse);
    }

    void FixedUpdate()
    {
        if (GetComponent<Rigidbody2D>().linearVelocity.magnitude > maxSpeed)
        {
            GetComponent<Rigidbody2D>().linearVelocity = GetComponent<Rigidbody2D>().linearVelocity.normalized * maxSpeed;
        }
    }
}
