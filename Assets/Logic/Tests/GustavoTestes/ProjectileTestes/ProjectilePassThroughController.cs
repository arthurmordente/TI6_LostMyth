using UnityEngine;

public class ProjectilePassThroughController : MonoBehaviour
{
    public Vector3 direction = Vector3.zero;
    public float speed = 10f;
    private Rigidbody _rigidBody;

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        if (direction == Vector3.zero)
        {
            direction = transform.forward;
        }

        Vector3 moveDir = direction.normalized;
        _rigidBody.linearVelocity = moveDir * speed;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name != "HOC_Arena")
        {
            //Dar dano
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name != "HOC_Arena")
        {
            //Dar dano
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
