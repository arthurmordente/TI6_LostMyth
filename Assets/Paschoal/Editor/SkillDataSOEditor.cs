using Logic.Scripts.GameDomain.Services.Skills;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillDataSO), true)]
public class SkillDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_skillType"), new GUIContent("Skill Type"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_castType"), new GUIContent("Cast Type"));

        var castProp = serializedObject.FindProperty("_castType");
        var castType = (SkillCastType)castProp.enumValueIndex;

        EditorGUILayout.Space(6f);
        switch (castType)
        {
            case SkillCastType.Projectile:
                EditorGUILayout.LabelField("Projectile", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileRange"), new GUIContent("Range"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileNumberOfTargets"), new GUIContent("Number Of Targets"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileTravelSpeed"), new GUIContent("Travel Speed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileAimPrefab"), new GUIContent("Aim Prefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectilePrefab"), new GUIContent("Projectile Prefab"));
                break;
            case SkillCastType.Area:
                EditorGUILayout.LabelField("Area", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_areaMinRange"), new GUIContent("Min Range"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_areaMaxRange"), new GUIContent("Max Range"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_areaRadius"), new GUIContent("Radius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_areaAimPrefab"), new GUIContent("Aim Prefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_areaImpactPrefab"), new GUIContent("Area Impact Prefab"));
                break;
            case SkillCastType.Self:
                EditorGUILayout.LabelField("Self", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_selfAimPrefab"), new GUIContent("Aim Prefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_selfCastPrefab"), new GUIContent("Cast Prefab"));
                break;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Skill", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Power"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Cost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Icon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SkillName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_effects"), new GUIContent("Effects"), true);

        if (target is DeclarativeSkillDataSO)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_runtimeModifiers"), true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
