using UnityEngine;
using UnityEngine.VFX;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Area skill VFX: fixed transform scale; gameplay radius applied via <see cref="SkillAreaVfxBinding"/> or fallback Area property.
    /// </summary>
    public static class SkillAreaVfxUtility
    {
        public const string AreaPropertyName = "Area";

        public static void ApplyGameplayRadius(GameObject root, float radiusMeters)
        {
            if (root == null) return;
            root.transform.localScale = Vector3.one;

            var bindings = root.GetComponentsInChildren<SkillAreaVfxBinding>(true);
            if (bindings != null && bindings.Length > 0)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i] != null)
                        bindings[i].Apply(radiusMeters);
                }
                return;
            }

            ApplyFallbackAreaProperty(root, radiusMeters);
        }

        static void ApplyFallbackAreaProperty(GameObject root, float radiusMeters)
        {
            float area = Mathf.Max(0.01f, radiusMeters);
            var vfxComponents = root.GetComponentsInChildren<VisualEffect>(true);
            for (int i = 0; i < vfxComponents.Length; i++)
            {
                var vfx = vfxComponents[i];
                if (vfx != null && vfx.HasFloat(AreaPropertyName))
                    vfx.SetFloat(AreaPropertyName, area);
            }
        }

        public static void ApplyGroundAimTransform(Transform root, Vector3 worldCenter, Quaternion worldRotation)
        {
            if (root == null) return;
            root.position = worldCenter;
            root.rotation = worldRotation;
        }
    }
}
