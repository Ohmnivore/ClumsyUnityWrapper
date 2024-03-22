using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Clumsy.Editor
{
    [CustomEditor(typeof(ClumsyConfiguration))]
    public class ClumsyConfigurationEditor : UnityEditor.Editor
    {
        // This field is assigned through a ScriptableObject default reference to avoid dealing with paths
        [SerializeField]
        VisualTreeAsset m_Layout;

        // This field is assigned through a ScriptableObject default reference to avoid dealing with paths
        [SerializeField]
        private StyleSheet m_Style;

        private VisualElement m_Root;
        private TextField m_FilterField;
        private VisualElement m_AutoStartContainer;

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();

            m_Layout.CloneTree(m_Root);
            m_Root.styleSheets.Add(m_Style);

            m_Root.Bind(serializedObject);
            ClampInputs();

            // Tooltips aren't loaded instantly - wait a bit
            EditorApplication.delayCall += CopyTooltips;

            m_FilterField = m_Root.Q<TextField>("filter");
            m_AutoStartContainer = m_Root.Q<VisualElement>("autostart");

            // Disable the auto-start toggle if the filter has parameters
            var filterSerializedProperty = serializedObject.FindProperty(m_FilterField.bindingPath);
            m_FilterField.TrackPropertyValue(filterSerializedProperty, OnFilterChanged);
            OnFilterChanged(filterSerializedProperty);

            return m_Root;
        }

        private void OnFilterChanged(SerializedProperty property)
        {
            var config = target as ClumsyConfiguration;
            m_AutoStartContainer.SetEnabled(config.ParametersDefinition.NumEntries == 0);
        }

        /// <summary>
        /// Share tooltips between toggles bound to serialized properties and their corresponding labels.
        /// </summary>
        private void CopyTooltips()
        {
            m_Root.Query<Toggle>(className: "clumsy-module-toggle").ForEach((toggle) =>
            {
                var parent = toggle.parent;
                var ownIndex = parent.IndexOf(toggle);
                var nextIndex = ownIndex + 1;
                if (nextIndex < parent.childCount)
                {
                    var nextChild = parent.ElementAt(nextIndex);
                    if (nextChild is Label label)
                    {
                        if (string.IsNullOrEmpty(label.tooltip))
                            label.tooltip = toggle.tooltip;
                    }
                }
            });
        }

        /// <summary>
        /// Applies <see cref="RangeAttribute"/> onto all the input fields (UI Toolkit doesn't do this out of the box).
        /// </summary>
        private void ClampInputs()
        {
            m_Root.Query<FloatField>().ForEach((fieldElement) =>
            {
                ClampInput(fieldElement, fieldElement.bindingPath, (x, attribute) => Mathf.Clamp(x, attribute.min, attribute.max));
            });

            m_Root.Query<IntegerField>().ForEach((fieldElement) =>
            {
                ClampInput(fieldElement, fieldElement.bindingPath, (x, attribute) => (int)Mathf.Clamp(x, attribute.min, attribute.max));
            });
        }

        private void ClampInput<T>(TextValueField<T> element, string bindingPath, Func<T, RangeAttribute, T> clampFunction)
        {
            var publicInstanceField = BindingFlags.Public | BindingFlags.Instance;

            // Handle nested properties: resolve final target object
            var fields = bindingPath.Split(".");
            var lastField = fields[fields.Length - 1];
            object obj = target;
            for (var i = 0; i < fields.Length - 1; i++)
            {
                var field = fields[i];
                var fieldInfo = obj.GetType().GetField(field, publicInstanceField);
                obj = fieldInfo.GetValue(obj);
            }

            // Resolve reflection info of the property
            var lastFieldInfo = obj.GetType().GetField(lastField, publicInstanceField);

            // Handle range attribute
            var rangeAttribute = lastFieldInfo.GetCustomAttribute<RangeAttribute>();
            if (rangeAttribute != null)
            {
                element.RegisterValueChangedCallback<T>(x =>
                {
                    var clampedValue = clampFunction(x.newValue, rangeAttribute);
                    element.SetValueWithoutNotify(clampedValue);    // Prevent infinite loop

                    // Don't propagate
                    x.StopImmediatePropagation();
                    x.PreventDefault();
                });
            }
        }
    }
}
