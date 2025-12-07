using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileBounceController : MonoBehaviour
{
    public Vector3 direction = Vector3.zero;
    public float speed = 10f;
    //public int maxBounces = 3;
    public float raycastDistance = 1f;

    private Rigidbody _rigidBody;
    //private int currentBounces = 0;

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

    private void OnTriggerEnter(Collider other)
    {
        /*if(currentBounces >= maxBounces)
        {
            Destroy(gameObject);
        } */

        if (other.gameObject.name != "HOC_Arena")
        {
            //Dar dano

            Destroy(gameObject);
        }

        Vector3 incoming = _rigidBody.linearVelocity;
        if (incoming.sqrMagnitude < 0.0001f)
        {
            incoming = direction.normalized * speed;
        }

        Vector3 origin = transform.position - incoming.normalized * 0.1f;
        Vector3 normal;

        if (Physics.Raycast(origin, incoming.normalized, out RaycastHit hit, raycastDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
        }
        else
        {
            normal = -incoming.normalized;
        }

        Vector3 reflected = Vector3.Reflect(incoming.normalized, normal);

        direction = reflected.normalized;
        _rigidBody.linearVelocity = direction * speed;

        //currentBounces++;
    }

    void OnCollisionEnter(Collision collision)
    {
        /*if(currentBounces >= maxBounces)
        {
            Destroy(gameObject);
        } */

        if (collision.gameObject.name != "HOC_Arena")
        {
            //Dar dano

            Destroy(gameObject);
        }

        Vector3 incoming = _rigidBody.linearVelocity;
        if (incoming.sqrMagnitude < 0.0001f)
        {
            incoming = direction.normalized * speed;
        }

        Vector3 origin = transform.position - incoming.normalized * 0.1f;
        Vector3 normal;

        if (Physics.Raycast(origin, incoming.normalized, out RaycastHit hit, raycastDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
        }
        else
        {
            normal = -incoming.normalized;
        }

        Vector3 reflected = Vector3.Reflect(incoming.normalized, normal);

        direction = reflected.normalized;
        _rigidBody.linearVelocity = direction * speed;

        //currentBounces++;
    }
}
