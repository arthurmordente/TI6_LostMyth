using UnityEngine;

public class TesteDash : MonoBehaviour
{
    [SerializeField] private float distance = 6f;
    [SerializeField] private float stripWidth = 1.0f;
    [SerializeField] private float groundY = 0.2f;

    [SerializeField] private KeyCode preCastKey = KeyCode.Alpha1;
    [SerializeField] private int confirmMouseButton = 0;
    [SerializeField] private int cancelMouseButton  = 1;

    [SerializeField] private LayerMask groundMask = 0;

    [SerializeField] private Color lineColor = new Color(1f, 1f, 0.2f, 1f);
    [SerializeField] private Color fillColor = new Color(1f, 1f, 0.2f, 0.25f);
    [SerializeField] private float lineWidth = 0.08f;

    Camera _cam;
    GameObject _gfx;
    LineRenderer _line;
    MeshFilter _mf;
    MeshRenderer _mr;
    Mesh _mesh;

    bool _preCastActive;

    void Start()
    {
        _cam = Camera.main;
        BuildGfx();
        SetGfxVisible(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(preCastKey))
        {
            _preCastActive = true;
            SetGfxVisible(true);
        }

        if (!_preCastActive) return;

        if (_cam == null) _cam = Camera.main;

        Vector3 start = transform.position; start.y = groundY;
        Vector3 aim = GetAimPoint();
        Vector3 dir = aim - start; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) { dir = transform.forward; dir.y = 0f; }
        dir.Normalize();

        Vector3 end = start + dir * distance; end.y = groundY;

        UpdateStrip(start, end, stripWidth);

        if (Input.GetMouseButtonDown(confirmMouseButton))
        {
            DoDash(dir);
            _preCastActive = false;
            SetGfxVisible(false);
        }
        else if (Input.GetMouseButtonDown(cancelMouseButton) || Input.GetKeyDown(KeyCode.Escape))
        {
            _preCastActive = false;
            SetGfxVisible(false);
        }
    }

    void DoDash(Vector3 dir)
    {
        Vector3 dst = transform.position + dir * distance;
        dst.y = transform.position.y;

        if (TryGetComponent<Rigidbody>(out var rb))
            rb.MovePosition(dst);
        else
            transform.position = dst;
    }

    Vector3 GetAimPoint()
    {
        if (_cam == null) return transform.position + transform.forward * distance;

        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        if (groundMask.value != 0)
        {
            if (Physics.Raycast(ray, out var hit, 500f, groundMask, QueryTriggerInteraction.Ignore))
            {
                var p = hit.point; p.y = groundY;
                return p;
            }
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
        if (plane.Raycast(ray, out float t))
        {
            var p = ray.GetPoint(t); p.y = groundY;
            return p;
        }

        return transform.position + transform.forward * distance;
    }

    void BuildGfx()
    {
        _gfx = new GameObject("DashTelegraph");
        _line = _gfx.AddComponent<LineRenderer>();
        _mf   = _gfx.AddComponent<MeshFilter>();
        _mr   = _gfx.AddComponent<MeshRenderer>();
        _mesh = new Mesh { name = "DashTelegraphMesh" };

        _line.useWorldSpace = true;
        _line.loop = true;
        _line.widthMultiplier = lineWidth;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = lineColor;
        _line.endColor   = lineColor;

        _mr.material = new Material(Shader.Find("Sprites/Default")) { color = fillColor };
        _mf.sharedMesh = _mesh;
    }

    void SetGfxVisible(bool v)
    {
        if (_gfx != null) _gfx.SetActive(v);
    }

    void UpdateStrip(Vector3 start, Vector3 end, float width)
    {
        Vector3 fwd = end - start; fwd.y = 0f;
        float len = fwd.magnitude; if (len < 1e-6f) len = 0.001f;
        fwd /= len;

        Vector3 side = new Vector3(-fwd.z, 0f, fwd.x) * (width * 0.5f);

        Vector3 v0 = start + side;
        Vector3 v1 = end   + side;
        Vector3 v2 = end   - side;
        Vector3 v3 = start - side;

        _line.positionCount = 4;
        _line.SetPosition(0, v0);
        _line.SetPosition(1, v1);
        _line.SetPosition(2, v2);
        _line.SetPosition(3, v3);

        _mesh.Clear();
        _mesh.vertices  = new Vector3[] { v0, v1, v2, v3 };
        _mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    void OnDisable()
    {
        if (_gfx != null) Destroy(_gfx);
        _gfx = null; _line = null; _mf = null; _mr = null; _mesh = null;
    }
}
