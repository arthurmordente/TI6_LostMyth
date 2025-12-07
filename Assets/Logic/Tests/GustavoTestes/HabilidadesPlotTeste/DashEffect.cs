using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;

public class DashEffect : AbilityEffect
{
    [Header("Dash")]
    [SerializeField] private float _distance = 6f;   // metros a “avançar” na direção do mouse
    [SerializeField] private float _stripWidth = 1.0f; // espessura visual do telegraph no chão
    [SerializeField] private float _y = 0.2f;          // altura do desenho (evita z-fighting)

    // estado de aim/telegraph
    private Vector3 _aimPoint;
    private GameObject _gfxGO;
    private LineRenderer _line;
    private MeshFilter _mf;
    private MeshRenderer _mr;
    private Mesh _mesh;

    public override void SetUp(Vector3 point)
    {
        // Atualizado continuamente pelo TargetingStrategy durante o Aim
        _aimPoint = point;
        EnsureTelegraph();

        // Pega a posição atual da Nara (caso o effect seja da Nara).
        // Não altero o CastController; aqui só leio a transform de Nara.
        var naraView = Object.FindFirstObjectByType<NaraView>(FindObjectsInactive.Exclude);
        if (naraView == null) return;

        var start = naraView.transform.position;
        start.y = _y;

        Vector3 dir = (_aimPoint - start);
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f)
        {
            // fallback: frente do player
            dir = naraView.transform.forward;
            dir.y = 0f;
        }
        dir.Normalize();

        var end = start + dir * _distance;
        end.y = _y;

        UpdateStrip(start, end, _stripWidth);
    }

    public override void Execute(AbilityData data, IEffectable caster)
    {
        // Calcula destino a partir do caster na direção do último aimPoint
        Transform casterT = caster.GetReferenceTransform();
        Vector3 start = casterT.position;
        Vector3 dir = (_aimPoint - start);
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f)
        {
            dir = casterT.forward;
            dir.y = 0f;
        }
        dir.Normalize();

        Vector3 destination = start + dir * _distance;

        // Mesma estratégia do TeleportEffect para respeitar o NaraTurnMovementController
        // (recalcula/repõe raio e recentraliza)
        if (caster is INaraController controller &&
            controller.NaraMove is NaraTurnMovementController turnMovement)
        {
            turnMovement.RecalculateRadiusAfterAbility();
            int naraRadius = turnMovement.GetNaraRadius();
            turnMovement.RemoveMovementRadius();
            casterT.position = destination;
            turnMovement.SetNaraRadius(naraRadius);
            turnMovement.SetMovementRadiusCenter();
        }
        else
        {
            casterT.position = destination;
        }

        CleanupTelegraph();
    }

    public override void Cancel(IEffectable caster, IEffectable target)
    {
        CleanupTelegraph();
    }

    private void EnsureTelegraph()
    {
        if (_gfxGO != null) return;

        _gfxGO = new GameObject("DashTelegraph");
        _line = _gfxGO.AddComponent<LineRenderer>();
        _mf   = _gfxGO.AddComponent<MeshFilter>();
        _mr   = _gfxGO.AddComponent<MeshRenderer>();
        _mesh = new Mesh { name = "DashTelegraphMesh" };

        _line.useWorldSpace = true;
        _line.loop = true;
        _line.widthMultiplier = 0.08f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = new Color(1f, 1f, 0.2f, 1f);
        _line.endColor   = _line.startColor;

        _mr.material = new Material(Shader.Find("Sprites/Default"))
        {
            color = new Color(1f, 1f, 0.2f, 0.25f)
        };
        _mf.sharedMesh = _mesh;
    }

    private void UpdateStrip(Vector3 start, Vector3 end, float width)
    {
        Vector3 fwd = end - start;
        fwd.y = 0f;
        float len = fwd.magnitude;
        if (len < 1e-6f) len = 0.001f;
        fwd /= len;

        Vector3 side = new Vector3(-fwd.z, 0f, fwd.x) * (width * 0.5f);

        Vector3 v0 = start + side;
        Vector3 v1 = end   + side;
        Vector3 v2 = end   - side;
        Vector3 v3 = start - side;

        // outline
        _line.positionCount = 4;
        _line.SetPosition(0, v0);
        _line.SetPosition(1, v1);
        _line.SetPosition(2, v2);
        _line.SetPosition(3, v3);

        // fill (mesh)
        _mesh.Clear();
        _mesh.vertices  = new Vector3[] { v0, v1, v2, v3 };
        _mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private void CleanupTelegraph()
    {
        if (_gfxGO != null)
        {
            Object.Destroy(_gfxGO);
            _gfxGO = null;
            _line = null;
            _mf = null;
            _mr = null;
            _mesh = null;
        }
    }
}
