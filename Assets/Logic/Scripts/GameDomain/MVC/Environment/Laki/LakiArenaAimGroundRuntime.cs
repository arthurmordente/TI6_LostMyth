using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Invisible collider on <c>LakiAimGround</c> covering the central hole so skill aim raycasts work over the boss platform.
    /// </summary>
    public static class LakiArenaAimGroundRuntime
    {
        public const string LayerName = "LakiAimGround";

        const float ColliderHeight = 0.05f;

        static GameObject _aimGroundRoot;

        public static void Ensure(Vector3 centerWorld, float innerRadius, float floorY)
        {
            Clear();

            int layer = LayerMask.NameToLayer(LayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[LakiArenaAimGroundRuntime] Layer '{LayerName}' is not defined — central aim collider skipped.");
                return;
            }

            float radius = Mathf.Max(0.01f, innerRadius);
            _aimGroundRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _aimGroundRoot.name = "LakiArenaAimGround";
            _aimGroundRoot.layer = layer;

            var renderer = _aimGroundRoot.GetComponent<MeshRenderer>();
            if (renderer != null)
                Object.Destroy(renderer);

            // Default cylinder: height 2 along Y, radius 0.5 on XZ.
            _aimGroundRoot.transform.position = new Vector3(centerWorld.x, floorY + ColliderHeight * 0.5f, centerWorld.z);
            _aimGroundRoot.transform.localScale = new Vector3(radius * 2f, ColliderHeight * 0.5f, radius * 2f);
        }

        public static void Clear()
        {
            if (_aimGroundRoot == null) return;
            Object.Destroy(_aimGroundRoot);
            _aimGroundRoot = null;
        }
    }
}
