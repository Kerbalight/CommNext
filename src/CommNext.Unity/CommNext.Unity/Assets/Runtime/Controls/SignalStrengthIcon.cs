using UnityEngine.UIElements;

namespace CommNext.Unity.Runtime.Controls
{
    public enum SignalStrengthFeedback
    {
        None = 0,
        Weak = 1,
        Moderate = 2,
        Strong = 3,
        Full = 4
    }

    public class SignalStrengthIcon : VisualElement
    {
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<SignalStrengthFeedback> _strength = new()
            {
                name = "strength"
            };

            // Use the Init method to assign the value of the progress UXML attribute to the C# progress property.
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((SignalStrengthIcon)ve).strength = _strength.GetValueFromBag(bag, cc);
            }
        }

        // Define a factory class to expose this control to UXML.
        public new class UxmlFactory : UxmlFactory<SignalStrengthIcon, UxmlTraits> { }

        private const string USSClassName = "signal-strength";
        private const string USSBackgroundClassName = "signal-strength__background";
        private const string USSSignalClassName = "signal-strength__signal";

        private SignalStrengthFeedback _strength;

        public SignalStrengthFeedback strength
        {
            get => _strength;
            set
            {
                _signalIcon.RemoveFromClassList("signal-strength__signal--" + _strength.ToString().ToLower());
                _strength = value;
                _signalIcon.AddToClassList("signal-strength__signal--" + value.ToString().ToLower());
            }
        }

        public void SetStrengthPercentage(double percentage)
        {
            strength = percentage switch
            {
                < 0 => SignalStrengthFeedback.None,
                < 0.25 => SignalStrengthFeedback.Weak,
                < 0.5 => SignalStrengthFeedback.Moderate,
                < 0.75 => SignalStrengthFeedback.Strong,
                < 1 => SignalStrengthFeedback.Full,
                _ => SignalStrengthFeedback.Full
            };
        }

        private readonly VisualElement _backgroundIcon;
        private readonly VisualElement _signalIcon;

        public SignalStrengthIcon()
        {
            AddToClassList(USSClassName);
            _backgroundIcon = new VisualElement();
            _backgroundIcon.AddToClassList(USSBackgroundClassName);
            _backgroundIcon.AddToClassList("icon__small");
            Add(_backgroundIcon);
            _signalIcon = new VisualElement();
            _signalIcon.AddToClassList(USSSignalClassName);
            _signalIcon.AddToClassList("icon__small");
            Add(_signalIcon);
        }
    }
}