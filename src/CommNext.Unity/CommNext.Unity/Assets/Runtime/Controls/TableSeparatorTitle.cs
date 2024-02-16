using UnityEngine.UIElements;

namespace CommNext.Unity.Runtime.Controls
{
    public class TableSeparatorTitle : VisualElement
    {
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _text = new()
            {
                name = "text"
            };

            // Use the Init method to assign the value of the progress UXML attribute to the C# progress property.
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((TableSeparatorTitle)ve).text = _text.GetValueFromBag(bag, cc);
                // (ve as RadialProgress).progress = m_ProgressAttribute.GetValueFromBag(bag, cc);
            }
        }

        // Define a factory class to expose this control to UXML.
        public new class UxmlFactory : UxmlFactory<TableSeparatorTitle, UxmlTraits> { }

        private const string USSClassName = "table-title";
        private const string USSContainerClassName = "table-title__container";

        private string _text;

        public string text
        {
            get => _text;
            set
            {
                _text = value;
                _labelElement.text = "<color=#595DD5>//</color> " + _text;
            }
        }

        private Label _labelElement;

        public TableSeparatorTitle()
        {
            AddToClassList(USSClassName);
            _labelElement = new Label();
            _labelElement.AddToClassList(USSContainerClassName);
            Add(_labelElement);
        }
    }
}