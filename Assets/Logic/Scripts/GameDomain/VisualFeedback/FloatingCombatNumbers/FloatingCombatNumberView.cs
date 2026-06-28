using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers
{
    /// <summary>
    /// World-space floating combat number. Render priority is raised so numbers draw above meshes and VFX.
    /// </summary>
    public class FloatingCombatNumberView : MonoBehaviour
    {
        const int RenderSortingOrder = 32767;
        const int RenderQueueOverlay = 4000;

        [SerializeField, FormerlySerializedAs("_text")] private TextMeshPro _text;
        [SerializeField] private FloatingCombatNumberStyleEntry[] _styles =
        {
            new FloatingCombatNumberStyleEntry
            {
                Kind = FloatingCombatNumberKind.Damage,
                TextColor = new Color(1f, 0.35f, 0.35f, 1f),
                MinValue = 1f,
                MaxValue = 50f,
                MinFontSize = 2.5f,
                MaxFontSize = 6f,
            },
            new FloatingCombatNumberStyleEntry
            {
                Kind = FloatingCombatNumberKind.Heal,
                TextColor = new Color(0.45f, 1f, 0.55f, 1f),
                MinValue = 1f,
                MaxValue = 50f,
                MinFontSize = 2.5f,
                MaxFontSize = 6f,
                ShowPlusSignForHeal = true,
            },
            new FloatingCombatNumberStyleEntry
            {
                Kind = FloatingCombatNumberKind.ManaGain,
                TextColor = new Color(0.45f, 0.75f, 1f, 1f),
                MinValue = 1f,
                MaxValue = 3f,
                MinFontSize = 2.5f,
                MaxFontSize = 6f,
                ShowPlusSignForHeal = true,
            },
            new FloatingCombatNumberStyleEntry
            {
                Kind = FloatingCombatNumberKind.ManaLost,
                TextColor = new Color(0.65f, 0.4f, 1f, 1f),
                MinValue = 1f,
                MaxValue = 3f,
                MinFontSize = 2.5f,
                MaxFontSize = 6f,
            },
        };

        [Header("Motion")]
        [SerializeField] private float _durationSeconds = 3f;
        [SerializeField] private float _riseDistance = 2f;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private bool _billboardToCamera = true;

        Transform _anchor;
        float _elapsed;
        Color _baseColor;
        Camera _camera;
        bool _renderOnTopApplied;

        void Awake()
        {
            if (_text == null)
                _text = GetComponentInChildren<TextMeshPro>(true);
        }

        public void Play(Transform anchor, int amount, FloatingCombatNumberKind kind)
        {
            _anchor = anchor;
            _camera = Camera.main;
            _elapsed = 0f;

            ApplyRenderOnTop();

            FloatingCombatNumberStyleEntry style = ResolveStyle(kind);
            float fontSize = ResolveFontSize(Mathf.Abs(amount), style);

            if (_text != null)
            {
                _text.text = kind switch
                {
                    FloatingCombatNumberKind.Damage => $"-{amount}",
                    FloatingCombatNumberKind.ManaGain => $"+{amount}",
                    FloatingCombatNumberKind.ManaLost => $"-{amount}",
                    _ => style.ShowPlusSignForHeal ? $"+{amount}" : amount.ToString(),
                };
                _text.fontSize = fontSize;
                _baseColor = style.TextColor;
                _text.color = _baseColor;
            }

            SyncStartPosition();
        }

        void Update()
        {
            if (_anchor == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float t = _durationSeconds > 0f ? Mathf.Clamp01(_elapsed / _durationSeconds) : 1f;

            Vector3 anchorPos = _anchor.position + _spawnOffset;
            transform.position = anchorPos + Vector3.up * (_riseDistance * t);

            if (_text != null)
            {
                Color c = _baseColor;
                c.a = EvaluateAlpha(t);
                _text.color = c;
            }

            if (_billboardToCamera && _camera != null)
            {
                Vector3 forward = transform.position - _camera.transform.position;
                if (forward.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            if (t >= 1f)
                Destroy(gameObject);
        }

        void ApplyRenderOnTop()
        {
            if (_renderOnTopApplied || _text == null) return;
            _renderOnTopApplied = true;

            Renderer renderer = _text.renderer;
            if (renderer != null)
                renderer.sortingOrder = RenderSortingOrder;

            Material material = _text.fontMaterial;
            if (material == null) return;

            material.renderQueue = RenderQueueOverlay;
            if (material.HasProperty("_ZTest"))
                material.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        /// <summary>Full opacity for the first half; cubic fade during the second half.</summary>
        static float EvaluateAlpha(float normalizedTime)
        {
            if (normalizedTime <= 0.5f) return 1f;

            float fadeT = (normalizedTime - 0.5f) / 0.5f;
            float remaining = 1f - fadeT;
            return remaining * remaining * remaining;
        }

        void SyncStartPosition()
        {
            if (_anchor == null) return;
            transform.position = _anchor.position + _spawnOffset;
        }

        FloatingCombatNumberStyleEntry ResolveStyle(FloatingCombatNumberKind kind)
        {
            if (_styles != null)
            {
                for (int i = 0; i < _styles.Length; i++)
                {
                    if (_styles[i] != null && _styles[i].Kind == kind)
                        return _styles[i];
                }
            }

            return new FloatingCombatNumberStyleEntry { Kind = kind };
        }

        static float ResolveFontSize(float absoluteAmount, FloatingCombatNumberStyleEntry style)
        {
            if (style == null) return 3f;
            float minVal = Mathf.Max(0.01f, style.MinValue);
            float maxVal = Mathf.Max(minVal, style.MaxValue);
            float t = Mathf.InverseLerp(minVal, maxVal, absoluteAmount);
            return Mathf.Lerp(style.MinFontSize, style.MaxFontSize, t);
        }

        /// <summary>Runtime fallback when no prefab is assigned in the installer.</summary>
        public static FloatingCombatNumberView CreateRuntimeFallback()
        {
            var root = new GameObject("FloatingCombatNumber");
            var tmp = root.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4f;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 2f);
            return root.AddComponent<FloatingCombatNumberView>();
        }
    }
}
