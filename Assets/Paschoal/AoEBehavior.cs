using UnityEngine;

public class AoEBehavior : MonoBehaviour
{
    public Vector3 position;
    private Vector3 t;
    public SkillDataSO skill;
    enum Type {CenteredOnPlayer, FreeMove}

    [SerializeField] Type type;

    private void Awake()
    {
        position = transform.position;
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
            t = new Vector3 (Mathf.Clamp(hit.point.x,position.x - skill.Range, position.x + skill.Range),0, Mathf.Clamp(hit.point.z, position.z - skill.Range,position.z + skill.Range));
        }
        return t;
    }
}
