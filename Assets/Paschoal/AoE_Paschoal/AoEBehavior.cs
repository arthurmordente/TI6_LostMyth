using UnityEngine;

public class AoEBehavior : MonoBehaviour
{
    Vector3 position;
    private Vector3 t;
    enum Type {CenteredOnPlayer, FreeMove}

    [SerializeField] Type type;

    private void Awake()
    {
        //transform.position = position;
    }
    void Update()
    {
        switch (type)
        {
            case Type.CenteredOnPlayer: transform.LookAt(GetMousePostion()) ; break;
            case Type.FreeMove: transform.position = GetMousePostion(); break;
        }
    }

    public Vector3 GetMousePostion() 
    {
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(r, out hit))
        {
            t = new Vector3 (hit.point.x,0, hit.point.z);
        }
        return t;
    }
}
