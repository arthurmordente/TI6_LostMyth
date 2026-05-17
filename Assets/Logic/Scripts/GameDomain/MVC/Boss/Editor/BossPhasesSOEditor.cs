using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss
{
    [CustomEditor(typeof(BossPhasesSO))]
    public class BossPhasesSOEditor : Editor
    {
        private SerializedProperty _phasesProp;

        private void OnEnable()
        {
            _phasesProp = serializedObject.FindProperty("_phases");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_maxHpFractionLossPerFightTurn"),
                new GUIContent("Default HP Loss Cap / Fight Turn", "Fraction of max HP Laki can lose in one fight turn when damage is allowed."));
            EditorGUILayout.Space(4f);

            if (_phasesProp != null)
            {
                EditorGUILayout.LabelField("Phases", EditorStyles.boldLabel);
                for (int i = 0; i < _phasesProp.arraySize; i++)
                {
                    SerializedProperty elem = _phasesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginVertical(GUI.skin.box);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Phase {i}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        _phasesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("Name"));
                    SerializedProperty triggerType = elem.FindPropertyRelative("TriggerType");
                    EditorGUILayout.PropertyField(triggerType);
                    BossPhasesSO.PhaseTriggerType mode = (BossPhasesSO.PhaseTriggerType)triggerType.enumValueIndex;
                    if (mode == BossPhasesSO.PhaseTriggerType.HealthPercentBelow)
                    {
                        EditorGUILayout.PropertyField(
                            elem.FindPropertyRelative("HealthPercentThreshold"),
                            new GUIContent("HP % Floor (0-1)", "Laki: boss stops taking damage once HP reaches this %. Also selects phase Behavior."));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(elem.FindPropertyRelative("HealthAbsoluteThreshold"), new GUIContent("Flat HP Threshold"));
                    }
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("Behavior"));
                    EditorGUILayout.PropertyField(
                        elem.FindPropertyRelative("MaxHpFractionLossPerFightTurnOverride"),
                        new GUIContent("HP Loss Cap / Turn Override", "0 = use BossPhases default (e.g. 1/3 max HP per fight turn)."));
                    EditorGUILayout.EndVertical();
                }
            }

            if (GUILayout.Button("Add Phase"))
            {
                int idx = _phasesProp != null ? _phasesProp.arraySize : 0;
                if (_phasesProp != null)
                {
                    _phasesProp.InsertArrayElementAtIndex(idx);
                }
            }

            using (new EditorGUI.DisabledScope(_phasesProp == null || _phasesProp.arraySize == 0))
            {
                if (GUILayout.Button("Clear All Phases"))
                {
                    if (_phasesProp != null)
                    {
                        _phasesProp.ClearArray();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

