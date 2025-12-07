using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileDivideController : MonoBehaviour
{
    public Vector3 direction = Vector3.zero;
    public float speed = 10f;

    public float splitTime = 2f;
    public int splitCount = 2;
    public float splitAngle = 30f;
    public GameObject projectilePrefab;

    private Rigidbody _rigidBody;
    private bool hasSplit = false;

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        if (direction == Vector3.zero)
        {
            direction = transform.forward;
        }

        Vector3 moveDir = direction.normalized;
        _rigidBody.linearVelocity = moveDir * speed;

        Invoke(nameof(Split), splitTime);
    }

    private void Split()
    {
        if (hasSplit || projectilePrefab == null || splitCount <= 0)
            return;

        hasSplit = true;

        Vector3 baseDir = _rigidBody.linearVelocity.normalized;
        if (baseDir == Vector3.zero)
            baseDir = direction.normalized;

        float angleStep = 0f;
        if (splitCount > 1)
            angleStep = splitAngle / (splitCount - 1);

        float startAngle = -splitAngle / 2f;

        for (int i = 0; i < splitCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;

            Quaternion rot = Quaternion.AngleAxis(currentAngle, Vector3.up);
            Vector3 newDir = rot * baseDir;

            GameObject newProj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

            ProjectileDivideController _projectileDivideController = newProj.GetComponent<ProjectileDivideController>();
            if (_projectileDivideController != null)
            {
                _projectileDivideController.direction = newDir;
                _projectileDivideController.speed = speed;
                _projectileDivideController.splitTime = splitTime;
                _projectileDivideController.splitCount = splitCount;
                _projectileDivideController.splitAngle = splitAngle;
                _projectileDivideController.projectilePrefab = projectilePrefab;
            }
        }

        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.name != "SphereDivide")
        {
            //Dar dano se possivel
            Debug.LogWarning("Projectile Divide colidiu");

            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name != "SphereDivide")
        {
            //Dar dano se possivel
            Debug.LogWarning("Projectile Divide colidiu");

            Destroy(gameObject);
        }
    }
}
