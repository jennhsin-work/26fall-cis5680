using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour
{
    public Vector3 thrust;
    public Quaternion heading;

    // Use this for initialization
    void Start()
    {
        // travel straight in the X-axis
        thrust.x = 400.0f;

        // do not passively decelerate
        GetComponent<Rigidbody>().linearDamping = 0;

        // set the direction it will travel in
        GetComponent<Rigidbody>().MoveRotation(heading);

        // apply thrust once, no need to apply it again since it will not decelerate
        GetComponent<Rigidbody>().AddRelativeForce(thrust);
    }

    // Update is called once per frame
    void Update()
    {
        // Physics engine handles movement, empty for now.
    }

    void OnCollisionEnter(Collision collision)
    {
        // the Collision contains a lot of info, but it's the colliding object we're most interested in.
        Collider collider = collision.collider;
        if (collider.CompareTag("Asteroid"))
        {
            Asteroid roid = collider.gameObject.GetComponent<Asteroid>();
            if (roid != null)
            {
                roid.Die();
            }
            Destroy(gameObject);
        }
        else
        {
            // if we collided with something else, print to console what the other thing was
            Debug.Log("Collided with " + collider.tag);
        }
    }
}
