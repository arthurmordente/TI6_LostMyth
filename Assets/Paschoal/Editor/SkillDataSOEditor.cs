using Logic.Scripts.GameDomain.Services.Skills;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillDataSO), true)]
public class SkillDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Loadout UI", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_divinity"), new GUIContent("Divinity"));
        EditorGUILayout.HelpBox(
            "Divindade + Skill Type definem background e frame no menu de loadout (SkillVisualCatalog).",
            MessageType.None);

        EditorGUILayout.Space(4f);
        var skillTypeProp = serializedObject.FindProperty("_skillType");
        var castProp = serializedObject.FindProperty("_castType");

        EditorGUILayout.PropertyField(skillTypeProp, new GUIContent("Skill Type"));

        var skillType = (SkillType)skillTypeProp.enumValueIndex;
        bool isPassive = skillType == SkillType.Passive;

        if (isPassive && castProp.enumValueIndex != (int)SkillCastType.Self)
        {
            castProp.enumValueIndex = (int)SkillCastType.Self;
        }

        var castType = (SkillCastType)castProp.enumValueIndex;
        bool showMovementProjectileOptions =
            !isPassive && castType == SkillCastType.Projectile && skillType == SkillType.Movement;

        if (isPassive)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Passiva: Cast = Self (fixo). Não usa aim/cast prefabs nem efeitos no input — só modificadores de combate ao entrar na luta.",
                MessageType.Info);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Passive — modificadores de combate", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_passiveModifiers"), new GUIContent("Passive modifiers"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_passiveTurnBehavior"), new GUIContent("Passive turn behavior"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_passiveCombatBehavior"), new GUIContent("Passive combat behavior"), true);
        }
        else
        {
            EditorGUILayout.PropertyField(castProp, new GUIContent("Cast Type"));
            castType = (SkillCastType)castProp.enumValueIndex;

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

                    if (showMovementProjectileOptions)
                    {
                        EditorGUILayout.Space(6f);
                        EditorGUILayout.LabelField("Movement (projectile)", EditorStyles.boldLabel);
                        SerializedProperty moveCasterProp = serializedObject.FindProperty("_moveCasterToProjectileHit");
                        EditorGUILayout.PropertyField(moveCasterProp, new GUIContent("Move Caster On IEffectable Hit"));

                        if (moveCasterProp.boolValue)
                        {
                            string advKey = "TI6.SkillDataSO.AdvProjMove." + target.GetInstanceID();
                            bool advOpen = EditorPrefs.GetBool(advKey, false);
                            advOpen = EditorGUILayout.Foldout(advOpen, "Advanced movement options", true);
                            EditorPrefs.SetBool(advKey, advOpen);

                            if (advOpen)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectilePullStandoffFromTargetMeters"), new GUIContent("Pull Standoff From Target (m)"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileDealsDamage"), new GUIContent("Deals Damage On Hit"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileDefersArenaSyncUntilHit"), new GUIContent("Defer Arena Sync Until Hit"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileSpawnForwardOffset"), new GUIContent("Spawn Forward Offset (m)"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileMinTravelBeforeHitMeters"), new GUIContent("Min Travel Before Hit (m)"));
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("_projectileHitDisplacementDurationSeconds"), new GUIContent("Pull Move Duration (s)"));
                                EditorGUI.indentLevel--;
                            }
                        }
                    }

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
