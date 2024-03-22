using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Clumsy.Editor
{
    /// <summary>
    /// A version of <see cref="UnityEditor.UIElements.ToolbarMenu"/> that lets us generate the dropdown when clicked.
    /// </summary>
    public class ToolbarMenuExt : TextElement
    {
        public interface IDropdown
        {
            void Show(Rect rect);
        }

        public IDropdown Dropdown;

        private TextElement m_TextElement;
        private VisualElement m_ArrowElement;

        public override string text
        {
            get => base.text;
            set
            {
                this.m_TextElement.text = value;
                base.text = value;
            }
        }

        public ToolbarMenuExt()
        {
            this.generateVisualContent = (Action<MeshGenerationContext>)null;
            this.AddToClassList(ToolbarMenu.ussClassName);
            this.m_TextElement = new TextElement();
            this.m_TextElement.AddToClassList(ToolbarMenu.textUssClassName);
            this.m_TextElement.pickingMode = PickingMode.Ignore;
            this.Add((VisualElement)this.m_TextElement);
            this.m_ArrowElement = new VisualElement();
            this.m_ArrowElement.AddToClassList(ToolbarMenu.arrowUssClassName);
            this.m_ArrowElement.pickingMode = PickingMode.Ignore;
            this.Add(this.m_ArrowElement);

            // Apply built-in UI Toolkit style
            var regularToolbar = new Toolbar();
            for (var i = 0; i < regularToolbar.styleSheets.count; i++)
            {
                var styleSheet = regularToolbar.styleSheets[i];
                this.styleSheets.Add(styleSheet);
            }

            // Handle click
            this.AddManipulator(new Clickable(() => Dropdown?.Show(worldBound)));
        }

        public new class UxmlFactory : UxmlFactory<ToolbarMenuExt, ToolbarMenu.UxmlTraits>
        {
        }

        public new class UxmlTraits : TextElement.UxmlTraits
        {
        }
    }
}
