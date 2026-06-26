using UnityEngine;
using UnityEngine.VFX;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Per-prefab calibration: maps gameplay hitbox radius (meters) to VFX Graph exposed float (Area, Size, etc.).
    /// propertyValue = gameplayRadiusMeters / metersPerPropertyUnit.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillAreaVfxBinding : MonoBehaviour
    {
        [SerializeField] private string _propertyName = SkillAreaVfxUtility.AreaPropertyName;
        [Tooltip("World meters of visual AoE radius when the exposed property equals 1.")]
        [SerializeField, Min(0.001f)] private float _metersPerPropertyUnit = 0.4f;
        [SerializeField] private bool _includeChildren = true;

        public string PropertyName => string.IsNullOrWhiteSpace(_propertyName)
            ? SkillAreaVfxUtility.AreaPropertyName
            : _propertyName;

        public float MetersPerPropertyUnit => Mathf.Max(0.001f, _metersPerPropertyUnit);

        public void Apply(float gameplayRadiusMeters)
        {
            float propertyValue = Mathf.Max(0.01f, gameplayRadiusMeters) / MetersPerPropertyUnit;
            string propertyName = PropertyName;

            if (_includeChildren)
            {
                var vfxComponents = GetComponentsInChildren<VisualEffect>(true);
                for (int i = 0; i < vfxComponents.Length; i++)
                    TrySetProperty(vfxComponents[i], propertyName, propertyValue);
                return;
            }

            var localVfx = GetComponent<VisualEffect>();
            TrySetProperty(localVfx, propertyName, propertyValue);
        }

        static void TrySetProperty(VisualEffect vfx, string propertyName, float value)
        {
            if (vfx == null || string.IsNullOrWhiteSpace(propertyName)) return;
            if (vfx.HasFloat(propertyName))
                vfx.SetFloat(propertyName, value);
        }
    }
}
