#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace CommNext.Unity.Runtime.Controls
{
    public class TabSelector : VisualElement
    {
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            // The progress property is exposed to UXML.
            // var m_ProgressAttribute = new TypedUxmlAttributeDescription<List<string>>()
            // {
            //     name = "progress"
            // };
            private UxmlStringAttributeDescription _items = new()
            {
                name = "items"
            };

            // Use the Init method to assign the value of the progress UXML attribute to the C# progress property.
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((TabSelector)ve).items = _items.GetValueFromBag(bag, cc)?.Split(',').ToList() ?? new List<string> { };
                // (ve as RadialProgress).progress = m_ProgressAttribute.GetValueFromBag(bag, cc);
            }
        }

        // Define a factory class to expose this control to UXML.
        public new class UxmlFactory : UxmlFactory<TabSelector, UxmlTraits> { }

        private const string USSClassName = "tabs-selector";
        private const string USSItemClassName = "tabs-selector__item";
        private const string USSItemSelectedClassName = "tabs-selector__item--selected";
        private const string USSItemLastClassName = "tabs-selector__item--last";

        private List<string> _items = new() { };

        public int SelectedIndex => _items.IndexOf(SelectedItem ?? string.Empty);
        public string? SelectedItem { get; private set; }

        public event Action<string, int>? OnTabSelected;

        public List<string> items
        {
            get => _items;
            set
            {
                var previousSelectedItem = SelectedItem;

                _items = value;
                SelectedItem = _items.Contains(previousSelectedItem ?? string.Empty)
                    ? previousSelectedItem
                    : _items.FirstOrDefault();

                Clear();
                for (var i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    var element = new Button();

                    element.AddToClassList(USSItemClassName);
                    if (item == SelectedItem) element.AddToClassList(USSItemSelectedClassName);

                    if (i == _items.Count - 1) element.AddToClassList(USSItemLastClassName);

                    var itemIndex = i;
                    element.clicked += () => OnButtonClicked(item, itemIndex);
                    element.userData = item;
                    element.text = item;
                    Add(element);
                }
            }
        }

        private void OnButtonClicked(string item, int index)
        {
            SelectedItem = item;
            foreach (var child in Children())
            {
                if (child.userData is not string childItem) continue;

                if (childItem == item)
                    child.AddToClassList(USSItemSelectedClassName);
                else
                    child.RemoveFromClassList(USSItemSelectedClassName);
            }

            OnTabSelected?.Invoke(item, index);
        }


        public TabSelector()
        {
            AddToClassList(USSClassName);

            // _input = new Button();
            // _input.AddToClassList(USSInputClassName);
            // Add(_input);
            //
            // // var inputContent = new VisualElement();
            // // inputContent.AddToClassList(USSInputContentClassName);
            // _arrow = new VisualElement();
            // _arrow.AddToClassList(USSInputArrowClassName);
            // _input.Add(_arrow);
            //
            // _dropdown = new VisualElement();
            // _dropdown.AddToClassList(USSDropdownClassName);
            // _dropdown.style.display = DisplayStyle.None;
            //
            // // Events
            // _input.clicked += OnInputClicked;
            //
            // choices = new List<string> { "Option 1", "Option 2", "Option 3" };
        }

        // private void OnInputClicked()
        // {
        //     var root = panel.visualTree.Q<VisualElement>(null, USSDropdownRootClassName);
        //     root.Add(_dropdown);
        //     _dropdown.BringToFront();
        //
        //     var inputPosition = _input.LocalToWorld(_input.transform.position);
        //     var targetPosition = root.WorldToLocal(inputPosition);
        //     _dropdown.style.position = Position.Absolute;
        //     _dropdown.style.width = 300;
        //     _dropdown.style.height = 120;
        //     _dropdown.style.left = targetPosition.x;
        //     _dropdown.style.top = targetPosition.y + _input.resolvedStyle.height + 4;
        //     _dropdown.style.display = DisplayStyle.Flex;
        //     _dropdown.AddManipulator(new Clickable());
        //     
        //     new GenericDropdownMenu()
        // }
    }
}