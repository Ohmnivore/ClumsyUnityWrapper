using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Clumsy.Editor
{
    [CustomEditor(typeof(UserSettings))]
    class UserSettingsEditor : UnityEditor.Editor
    {
        // This field is assigned through a ScriptableObject default reference to avoid dealing with paths
        [SerializeField]
        VisualTreeAsset m_Layout;

        public override VisualElement CreateInspectorGUI()
        {
            // ScriptableSingleton sets this flag by default, which grays out PropertyFields
            target.hideFlags &= ~HideFlags.NotEditable;

            var rootElement = new VisualElement();

            m_Layout.CloneTree(rootElement);

            rootElement.Bind(serializedObject);

            rootElement.Q<Button>("reset-button").clicked += () =>
            {
                var settings = target as UserSettings;
                settings.Reset();
            };

            return rootElement;
        }
    }
}
