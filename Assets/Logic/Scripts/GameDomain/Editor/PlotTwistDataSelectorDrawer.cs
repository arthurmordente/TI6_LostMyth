using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

namespace Logic.Scripts.GameDomain.MVC.Abilitys {
    [CustomPropertyDrawer(typeof(PlotTwistDataSelectorAttribute))]
    public class PlotTwistDataSelectorDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.ObjectReference) {
                EditorGUI.LabelField(position, label.text, "Use [PlotTwistDataSelector] with a ScriptableObject field.");
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty targetingStrategyProperty = property.serializedObject.FindProperty("TargetingStrategy");

            if (targetingStrategyProperty == null) {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            Type targetStrategyType = GetTargetingStrategyType(targetingStrategyProperty);
            Type requiredPlotTwistType = GetRequiredPlotTwistDataType(targetStrategyType);

            if (requiredPlotTwistType != null) {
                EditorGUI.BeginChangeCheck();

                ScriptableObject selectedObject = EditorGUI.ObjectField(position, label, property.objectReferenceValue as ScriptableObject, requiredPlotTwistType, false) as ScriptableObject;

                if (EditorGUI.EndChangeCheck()) {
                    property.objectReferenceValue = selectedObject;
                }

                Rect helpRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.HelpBox(new Rect(position.x, position.y + position.height, position.width, EditorGUIUtility.singleLineHeight), $"Tipo esperado: {requiredPlotTwistType.Name}", MessageType.Info);
            }
            else {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.HelpBox(new Rect(position.x, position.y + position.height, position.width, EditorGUIUtility.singleLineHeight), "Estratégia de Targeting não reconhecida para PlotTwistData.", MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight * 2.5f;
        }

        private Type GetTargetingStrategyType(SerializedProperty targetingStrategyProperty) {
            string fullTypeName = targetingStrategyProperty.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullTypeName)) return null;

            string typeAssemblyQualifiedName = fullTypeName.Split(' ').Length > 1 ? fullTypeName.Split(' ')[1] : fullTypeName.Split(' ')[0];
            string className = typeAssemblyQualifiedName.Split('.').Last();

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == className);
        }

        private Type GetRequiredPlotTwistDataType(Type targetingStrategyType) {
            if (targetingStrategyType == typeof(ProjectileTargeting)) {
                return typeof(ProjectilePlotTwistData);
            }
            if (targetingStrategyType == typeof(AoeTargeting)) {
                return typeof(AoePlotTwistData);
            }
            if (targetingStrategyType == typeof(SelfTargeting)) {
                return typeof(SelfPlotTwistData);
            }
            if (targetingStrategyType == typeof(PointTargeting)) {
                return typeof(PointPlotTwistData);
            }

            return null;
        }
    }
}