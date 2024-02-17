using UnityEngine.UIElements;

namespace CommNext.UI.Tooltip;

public class TooltipManipulator : MouseManipulator
{
    private string _tooltipText;
    private bool _isTooltipVisible;

    public string TooltipText
    {
        get => _tooltipText;
        set
        {
            _tooltipText = value;
            // Update the tooltip text if it's visible
            if (_isTooltipVisible) MainUIManager.Instance.TooltipWindow.ToggleTooltip(true, target, _tooltipText);
        }
    }

    public TooltipManipulator(string tooltipText)
    {
        _tooltipText = tooltipText;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<MouseEnterEvent>(OnMouseIn);
        target.RegisterCallback<MouseLeaveEvent>(OnMouseOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<MouseEnterEvent>(OnMouseIn);
        target.UnregisterCallback<MouseLeaveEvent>(OnMouseOut);
    }

    private void OnMouseIn(MouseEnterEvent evt)
    {
        _isTooltipVisible = true;
        MainUIManager.Instance.TooltipWindow.ToggleTooltip(true, target, _tooltipText);
    }

    private void OnMouseOut(MouseLeaveEvent evt)
    {
        MainUIManager.Instance.TooltipWindow.ToggleTooltip(false, target);
        _isTooltipVisible = false;
    }
}