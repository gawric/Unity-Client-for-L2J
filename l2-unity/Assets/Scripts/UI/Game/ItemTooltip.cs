using UnityEngine;

public class ItemTooltip : MonoBehaviour
{
    public string itemName;

    private void OnMouseEnter()
    {
        TooltipUI.Instance.Show(itemName);
    }

    private void OnMouseExit()
    {
        TooltipUI.Instance.Hide();
    }
}
