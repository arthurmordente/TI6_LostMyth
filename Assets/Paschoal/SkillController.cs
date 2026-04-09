using UnityEngine;
public class SkillController : MonoBehaviour
{
    public SkillDataSO[] skills = new SkillDataSO[6];
    private SkillDataSO casting;
    private Vector3 point = new Vector3();
    GameObject g;
    [SerializeField] public CharacterView effectable;
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && g == null)
        {
            g = Instantiate(skills[0].AoEPrefab, this.transform.position, this.transform.rotation);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            skills[0] = skills[0].Upgrade;
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (g != null || skills[0].AoEPrefab == null)
            {
                if(effectable != null) skills[0].OnCast(effectable.GetComponent<IEffectable>(), g.transform);
                else skills[0].OnCast(null, g.transform);
                Destroy(g);
            }
        }
    }
}

