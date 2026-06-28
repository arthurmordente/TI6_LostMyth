using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(RandomTurnPassiveBehaviorSO))]
public class RandomTurnPassiveBehaviorSOEditor : Editor
{
    ReorderableList _poolList;

    void OnEnable()
    {
        SerializedProperty pool = serializedObject.FindProperty("_pool");
        _poolList = new ReorderableList(serializedObject, pool, true, true, true, true)
        {
            drawHeaderCallback = rect =>
            {
                var behavior = (RandomTurnPassiveBehaviorSO)target;
                int total = behavior.TotalWeight;
                EditorGUI.LabelField(rect, $"Pool (total weight: {total})");
            },
            elementHeightCallback = index => EditorGUI.GetPropertyHeight(pool.GetArrayElementAtIndex(index), true),
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty entry = pool.GetArrayElementAtIndex(index);
                float value = entry.FindPropertyRelative("Value").floatValue;
                int weight = entry.FindPropertyRelative("Weight").intValue;
                var behavior = (RandomTurnPassiveBehaviorSO)target;
                float probability = behavior.GetEntryProbability(index) * 100f;

                string label = $"Value {FormatValue(value)}  —  {probability:0.#}%";
                EditorGUI.PropertyField(rect, entry, new GUIContent(label), true);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _poolList.DoLayoutList();

        var behavior = (RandomTurnPassiveBehaviorSO)target;
        if (behavior.TotalWeight > 0)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Computed probabilities", EditorStyles.boldLabel);
            SerializedProperty pool = serializedObject.FindProperty("_pool");
            for (int i = 0; i < pool.arraySize; i++)
            {
                float value = pool.GetArrayElementAtIndex(i).FindPropertyRelative("Value").floatValue;
                float pct = behavior.GetEntryProbability(i) * 100f;
                EditorGUILayout.LabelField($"Value {FormatValue(value)}", $"{pct:0.##}%");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    static string FormatValue(float value) =>
        Mathf.Approximately(value, Mathf.Round(value))
            ? value.ToString("0")
            : value.ToString("0.##");
}
