using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext.Unity.Runtime.Controls
{
    public class BandIcon : VisualElement
    {
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlColorAttributeDescription _color = new()
            {
                name = "color"
            };

            private readonly UxmlStringAttributeDescription _code = new()
            {
                name = "code"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((BandIcon)ve).color = _color.GetValueFromBag(bag, cc);
                ((BandIcon)ve).code = _code.GetValueFromBag(bag, cc);
            }
        }

        // Define a factory class to expose this control to UXML.
        public new class UxmlFactory : UxmlFactory<BandIcon, UxmlTraits> { }

        private const string USSClassName = "band__icon";
        private const string USSCodeClassName = "band__code";

        public Color color
        {
            get => _codeLabel.style.color.value;
            set => _codeLabel.style.color = value;
        }

        public string code
        {
            get => _codeLabel.text;
            set => _codeLabel.text = value;
        }

        public void SetBand(string bandCode, Color bandColor)
        {
            code = bandCode;
            color = bandColor;
        }

        private readonly Label _codeLabel;

        public BandIcon()
        {
            AddToClassList(USSClassName);
            _codeLabel = new Label();
            _codeLabel.AddToClassList(USSCodeClassName);
            Add(_codeLabel);
        }
    }
}