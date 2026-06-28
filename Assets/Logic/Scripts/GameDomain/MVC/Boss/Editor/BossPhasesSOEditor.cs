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

            EditorGUILayout.LabelField("Laki — dice vulnerability window", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_vulnerabilityFightTurnCount"),
                new GUIContent(
                    "Vulnerability Fight Turn Count",
                    "Fight turns with shield off after the player wins the dice minigame.\n" +
                    "1 = only turn T (dice resolves). 2 = T and T+1. 3 = T, T+1, T+2."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_maxHpFractionLossPerFightTurn"),
                new GUIContent(
                    "Default HP Loss Cap / Dice Window",
                    "Total fraction of max HP Laki may lose across the entire dice vulnerability window (e.g. 0.33 = one third of max HP)."));
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
                            new GUIContent("HP % Threshold (0-1)", "Selects phase Behavior when HP falls at or below this %. Does not clip damage during a dice window."));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(elem.FindPropertyRelative("HealthAbsoluteThreshold"), new GUIContent("Flat HP Threshold"));
                    }
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("Behavior"));
                    EditorGUILayout.PropertyField(
                        elem.FindPropertyRelative("MaxHpFractionLossPerFightTurnOverride"),
                        new GUIContent("HP Loss Cap / Dice Window Override", "0 = use BossPhases default. Max HP fraction losable in one dice vulnerability window while in this phase."));
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
