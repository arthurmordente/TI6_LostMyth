using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss
{
    [CustomEditor(typeof(BossAttack))]
    public class BossAttackEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty effects = serializedObject.FindProperty("_effects");
            SerializedProperty attackType = serializedObject.FindProperty("_attackType");
            SerializedProperty displacementPriority = serializedObject.FindProperty("_displacementPriority");
            SerializedProperty protean = serializedObject.FindProperty("_protean");
            SerializedProperty feather = serializedObject.FindProperty("_feather");
            SerializedProperty wingSlash = serializedObject.FindProperty("_wingSlash");
            SerializedProperty orb = serializedObject.FindProperty("_orb");
            SerializedProperty featherIsPull = serializedObject.FindProperty("_featherIsPull");
            SerializedProperty skySwords = serializedObject.FindProperty("_skySwords");
            SerializedProperty skySwordsIsPull = serializedObject.FindProperty("_skySwordsIsPull");
            SerializedProperty circle = serializedObject.FindProperty("_circle");
            SerializedProperty minigameRoundPrefab = serializedObject.FindProperty("_minigameRoundPrefab");
            SerializedProperty diceDisplayName = serializedObject.FindProperty("_diceAttackDisplayName");
            SerializedProperty dicePlayerDie = serializedObject.FindProperty("_diceAttackPlayerDiePrefab");
            SerializedProperty diceBossDie = serializedObject.FindProperty("_diceAttackBossDiePrefab");
            SerializedProperty diceDieHp = serializedObject.FindProperty("_diceAttackDieHp");
            SerializedProperty diceInputDelay = serializedObject.FindProperty("_diceAttackPlayerRollInputConsumeDelay");
            SerializedProperty diceRollPrompt = serializedObject.FindProperty("_diceAttackPlayerRollPromptPrefab");

            EditorGUILayout.PropertyField(effects, true);
            EditorGUILayout.PropertyField(attackType);
            EditorGUILayout.PropertyField(displacementPriority, new GUIContent("Displacement Priority"));

            // Must stay in sync with BossAttack.AttackType enum order
            switch (attackType.enumValueIndex)
            {
                case 0: // ProteanCones
                    EditorGUILayout.PropertyField(protean, true);
                    break;
                case 1: // FeatherLines
                    EditorGUILayout.PropertyField(feather, true);
                    EditorGUILayout.PropertyField(featherIsPull, new GUIContent("Feather Is Pull"));
                    break;
                case 2: // WingSlash
                    EditorGUILayout.PropertyField(wingSlash, true);
                    break;
                case 3: // Orb
                    EditorGUILayout.PropertyField(orb, true);
                    break;
                case 4: // HookAwakening
                    EditorGUILayout.HelpBox("HookAwakening: configure effects list above if needed.", MessageType.Info);
                    break;
                case 5: // SkySwords
                    EditorGUILayout.PropertyField(skySwords, true);
                    EditorGUILayout.PropertyField(skySwordsIsPull, new GUIContent("SkySwords Is Pull"));
                    break;
                case 6: // Minigame (legacy)
                    EditorGUILayout.PropertyField(minigameRoundPrefab, new GUIContent("Minigame Round Prefab"));
                    break;
                case 7: // Circle
                case 8: // GenericPlayerFootCircle
                    EditorGUILayout.PropertyField(circle, true);
                    break;
                case 9: // DiceAttack
                    EditorGUILayout.PropertyField(diceDisplayName, new GUIContent("Display Name"));
                    EditorGUILayout.PropertyField(dicePlayerDie, new GUIContent("Player Die Prefab"));
                    EditorGUILayout.PropertyField(diceBossDie, new GUIContent("Boss Die Prefab"));
                    EditorGUILayout.PropertyField(diceDieHp, new GUIContent("Die HP"));
                    EditorGUILayout.PropertyField(diceInputDelay, new GUIContent("Player Roll Input Consume Delay (s)"));
                    EditorGUILayout.PropertyField(diceRollPrompt, new GUIContent("Player Roll Prompt Prefab"));
                    EditorGUILayout.HelpBox("Dice count and face range come from LakiDiceAttackState at runtime (default 1 die each, 1..6). Assign the DicePrompt UI prefab (root Canvas).", MessageType.None);
                    break;
                default:
                    EditorGUILayout.HelpBox("Unknown attack type index.", MessageType.Warning);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
