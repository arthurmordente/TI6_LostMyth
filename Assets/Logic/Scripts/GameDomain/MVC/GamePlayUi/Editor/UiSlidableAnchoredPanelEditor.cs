using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Ui.Editor
{
    [CustomEditor(typeof(UiSlidableAnchoredPanel))]
    public sealed class UiSlidableAnchoredPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var panel = (UiSlidableAnchoredPanel)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Teste do slide (Play Mode)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode: os botões alteram o objeto selecionado. Selecione a instância na Hierarchy (cena em execução), não o prefab no Project.",
                    MessageType.Info);
            }

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Alternar (Toggle)"))
                panel.Toggle();
            if (GUILayout.Button("Abrir"))
                panel.SetExpanded(true);
            if (GUILayout.Button("Fechar"))
                panel.SetExpanded(false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Abrir (instantâneo)"))
                panel.SetExpanded(true, true);
            if (GUILayout.Button("Fechar (instantâneo)"))
                panel.SetExpanded(false, true);
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Estado", panel.IsExpanded ? "Aberto" : "Fechado");
                EditorGUILayout.LabelField("Índice", $"{panel.CurrentStateIndex} / {panel.StateCount - 1}");
                if (panel.StateCount > 2)
                {
                    EditorGUILayout.LabelField("Multi-estado", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    for (int i = 0; i < panel.StateCount; i++)
                    {
                        if (GUILayout.Button($"{i}"))
                            panel.SetStateIndex(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
