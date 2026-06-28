#if UNITY_EDITOR
using Logic.Scripts.Core.Mvc.WorldCamera;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.WorldCamera.Editor
{
    [CustomPropertyDrawer(typeof(SceneCameraEntrySettings))]
    public sealed class SceneCameraEntrySettingsDrawer : PropertyDrawer
    {
        const float ButtonHeight = 24f;
        const float Spacing = 2f;

        static readonly string[] FieldNames =
        {
            "OverrideDefaults",
            "HorizontalAngle",
            "VerticalAngle",
            "OrbitHeight",
            "OrbitRadius",
            "PanOffset",
            "BlendDuration"
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;
            var foldoutRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            y += EditorGUIUtility.singleLineHeight + Spacing;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var buttonRect = new Rect(position.x, y, position.width, ButtonHeight);
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUI.Button(buttonRect, "Capture from Play Mode Camera"))
                        CaptureFromPlayModeCamera(property);
                }
                y += ButtonHeight + Spacing;

                if (!Application.isPlaying)
                {
                    float helpHeight = EditorStyles.helpBox.CalcHeight(
                        new GUIContent("Enter Play Mode, frame the camera, then press Capture to write values into this asset."),
                        position.width);
                    EditorGUI.HelpBox(new Rect(position.x, y, position.width, helpHeight),
                        "Enter Play Mode, frame the camera, then press Capture to write values into this asset.",
                        MessageType.Info);
                    y += helpHeight + Spacing;
                }

                for (int i = 0; i < FieldNames.Length; i++)
                {
                    var field = property.FindPropertyRelative(FieldNames[i]);
                    if (field == null) continue;

                    float fieldHeight = EditorGUI.GetPropertyHeight(field, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, fieldHeight), field, true);
                    y += fieldHeight + Spacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight + Spacing;
            height += ButtonHeight + Spacing;

            if (!Application.isPlaying)
            {
                height += EditorStyles.helpBox.CalcHeight(
                    new GUIContent("Enter Play Mode, frame the camera, then press Capture to write values into this asset."),
                    EditorGUIUtility.currentViewWidth) + Spacing;
            }

            for (int i = 0; i < FieldNames.Length; i++)
            {
                var field = property.FindPropertyRelative(FieldNames[i]);
                if (field == null) continue;
                height += EditorGUI.GetPropertyHeight(field, true) + Spacing;
            }

            return height;
        }

        static void CaptureFromPlayModeCamera(SerializedProperty property)
        {
            float existingBlend = property.FindPropertyRelative("BlendDuration")?.floatValue ?? 0f;

            if (!WorldCameraView.TryCaptureFromActiveCamera(out SceneCameraEntrySettings captured, existingBlend))
            {
                EditorUtility.DisplayDialog(
                    "Capture Scene Camera Entry",
                    "WorldCameraView was not found in the active scene. Make sure CoreScene is loaded and the world camera is active.",
                    "OK");
                return;
            }

            ApplyCapturedSettings(property, captured);
            property.serializedObject.ApplyModifiedProperties();

            var target = property.serializedObject.targetObject;
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Capture Scene Camera Entry",
                "Camera values captured from Play Mode and written to Scene Camera Entry.\nOverride Defaults was enabled.",
                "OK");
        }

        static void ApplyCapturedSettings(SerializedProperty property, SceneCameraEntrySettings captured)
        {
            property.FindPropertyRelative("OverrideDefaults").boolValue = captured.OverrideDefaults;
            property.FindPropertyRelative("HorizontalAngle").floatValue = captured.HorizontalAngle;
            property.FindPropertyRelative("VerticalAngle").floatValue = captured.VerticalAngle;
            property.FindPropertyRelative("OrbitHeight").floatValue = captured.OrbitHeight;
            property.FindPropertyRelative("OrbitRadius").floatValue = captured.OrbitRadius;
            property.FindPropertyRelative("PanOffset").vector3Value = captured.PanOffset;
            property.FindPropertyRelative("BlendDuration").floatValue = captured.BlendDuration;
        }
    }
}
#endif
