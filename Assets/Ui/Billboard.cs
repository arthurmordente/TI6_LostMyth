using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform playerCam;

    void Start()
    {
        playerCam = Camera.main.transform;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + playerCam.forward);
    }
}
