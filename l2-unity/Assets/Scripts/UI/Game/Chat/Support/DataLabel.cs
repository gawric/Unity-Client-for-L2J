
using UnityEngine;
using UnityEngine.UIElements;

public class DataLabel
{

    private int _position;
    private Label _label;
    private bool _visible;

    public DataLabel(int position, Label label, bool visible)
    {
        label.text = "";
        _position = position;
        _label = label;
        _label.text = "";
        SetVisible(visible);
    }

    public void SetText(string text)
    {
        _label.text = text;
    }

    public int GetPosition()
    {
        return _position;
    }

    public Label GetLabel()
    {
        return _label;
    }

    public bool IsVisible()
    {
        return _visible;
    }

    public void SetVisible(bool visible)
    {
        SetLabelVisible(visible);
        _visible = visible;
    }

    private void SetLabelVisible(bool visible)
    {
        if (visible)
        {
            _label.style.display = DisplayStyle.Flex;
        }
        else
        {
            _label.style.display = DisplayStyle.None;
        }
    }
}
