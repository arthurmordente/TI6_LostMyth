using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Shared;
using Logic.Scripts.GameDomain.MVC.Boss.Telegraph;

namespace Logic.Scripts.GameDomain.MVC.Environment.Orb {
    public class OrbView : MonoBehaviour {
        private GameObject _telegraphGO;
        private LineRenderer _line;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        [SerializeField] private int _segments = 64;
        private int _layerId = -1;
        private float _yOffset = 0.05f;
        private int _rqAdd = 0;

        private GameObject _catalogAreaPrefabTemplate;
        private GameObject _catalogAreaInstance;
        private bool _useCatalogArea;
        private const float CatalogAreaVisualLocalY = -0.1f;

        /// <summary>Call before <see cref="PrepareTelegraph"/>. If non-null, telegraph prefab from catalog is scaled as the AoE disc (same convention as circle telegraphs: localScale x/z = radius).</summary>
        public void ConfigureAreaVisualPrefab(GameObject prefabTemplate)
        {
            _catalogAreaPrefabTemplate = prefabTemplate;
        }

        public void UpdateColor(Color newColor) {
            if (_line != null)
            {
                _line.startColor = newColor;
                _line.endColor = newColor;
            }
            if (_meshRenderer != null)
                _meshRenderer.material = new Material(Shader.Find("Sprites/Default")) { color = newColor };
        }

        public void PrepareTelegraph() {
            if (_telegraphGO != null || _catalogAreaInstance != null) return;

            var layering = TelegraphLayeringLocator.Service;
            if (layering != null)
            {
                var layer = layering.Register(preferTop: false);
                _layerId = layer.Id;
                _yOffset = layer.Y;
                _rqAdd = layer.QueueAdd;
            }

            if (_catalogAreaPrefabTemplate != null)
            {
                _useCatalogArea = true;
                _catalogAreaInstance = Object.Instantiate(_catalogAreaPrefabTemplate, transform);
                _catalogAreaInstance.name = "OrbAreaFromCatalog";
                _catalogAreaInstance.transform.localPosition = new Vector3(0f, CatalogAreaVisualLocalY, 0f);
                _catalogAreaInstance.transform.localRotation = Quaternion.identity;
                _catalogAreaInstance.transform.localScale = Vector3.one;
                return;
            }

            _useCatalogArea = false;
            var provider = TelegraphMaterialService.Provider;
            Material baseLine = provider != null ? provider.GetLineMaterial(false, null) : new Material(Shader.Find("Sprites/Default"));
            Material baseMesh = provider != null ? provider.GetMeshMaterial(false, null) : new Material(Shader.Find("Sprites/Default"));

            _telegraphGO = new GameObject("OrbTelegraph");
            _telegraphGO.transform.SetParent(transform, false);

            _line = _telegraphGO.AddComponent<LineRenderer>();
            var lineMat = new Material(baseLine);
            lineMat.renderQueue += _rqAdd;
            _line.material = lineMat;
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.widthMultiplier = 0.1f;

            _meshFilter = _telegraphGO.AddComponent<MeshFilter>();
            _meshRenderer = _telegraphGO.AddComponent<MeshRenderer>();
            var meshMat = new Material(baseMesh);
            meshMat.renderQueue += _rqAdd;
            _meshRenderer.material = meshMat;
            _mesh = new Mesh();
            _mesh.name = "OrbDiscMesh";
            _meshFilter.sharedMesh = _mesh;
        }

        public void UpdateRadius(float radius) {
            if (_catalogAreaInstance == null && _telegraphGO == null)
                PrepareTelegraph();

            if (_useCatalogArea && _catalogAreaInstance != null)
            {
                float r = Mathf.Max(0.001f, radius);
                _catalogAreaInstance.transform.localScale = new Vector3(r, 1f, r);
                var lp = _catalogAreaInstance.transform.localPosition;
                lp.y = CatalogAreaVisualLocalY;
                _catalogAreaInstance.transform.localPosition = lp;
                return;
            }

            if (_telegraphGO == null) return;

            Vector3 center = transform.position; center.y = _yOffset;
            Vector3[] ring = DiscMath.GenerateDiscVertices(center, radius, _segments);
            for (int i = 0; i < ring.Length; i++) ring[i].y = _yOffset;
            _line.positionCount = ring.Length;
            _line.SetPositions(ring);

            Transform t = _meshFilter.transform;
			t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

            int seg = ring.Length;
            Vector3[] verts = new Vector3[seg + 1];
			verts[0] = t.InverseTransformPoint(center);
            for (int i = 0; i < seg; i++) verts[i + 1] = t.InverseTransformPoint(ring[i]);
            int[] tris = new int[seg * 3];
            int ti = 0;
            for (int i = 1; i <= seg; i++) {
                int next = (i == seg) ? 1 : i + 1;
				tris[ti++] = 0; tris[ti++] = next; tris[ti++] = i;
            }
            _mesh.Clear();
            _mesh.vertices = verts;
            _mesh.triangles = tris;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _meshRenderer.enabled = true;
            _line.enabled = true;
        }

        private void OnDestroy()
        {
            var layering = TelegraphLayeringLocator.Service;
            if (layering != null && _layerId >= 0) layering.Unregister(_layerId);
        }
    }
}
