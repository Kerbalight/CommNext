using System;
using UnityEngine.UIElements;

namespace CommNext.Unity.Runtime.Controls
{
    public class SortDirectionButton : Button
    {
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<SortDirection> _direction = new()
            {
                name = "direction"
            };

            // Use the Init method to assign the value of the progress UXML attribute to the C# progress property.
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((SortDirectionButton)ve).direction = _direction.GetValueFromBag(bag, cc);
            }
        }

        // Define a factory class to expose this control to UXML.
        public new class UxmlFactory : UxmlFactory<SortDirectionButton, UxmlTraits> { }

        private const string USSClassName = "button-sort-direction";

        private SortDirection _direction;

        public SortDirection direction
        {
            get => _direction;
            set
            {
                _direction = value;
                if (_direction == SortDirection.Ascending)
                {
                    RemoveFromClassList("button-sort-direction--descending");
                    AddToClassList("button-sort-direction--ascending");
                }
                else
                {
                    RemoveFromClassList("button-sort-direction--ascending");
                    AddToClassList("button-sort-direction--descending");
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        public event Action<SortDirection> directionChanged;

        public SortDirectionButton()
        {
            AddToClassList(USSClassName);
            clicked += OnClicked;
        }

        private void OnClicked()
        {
            direction = direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
            directionChanged?.Invoke(direction);
        }
    }
}