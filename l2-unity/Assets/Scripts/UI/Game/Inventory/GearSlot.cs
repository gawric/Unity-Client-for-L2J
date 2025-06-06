
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GearSlot : InventorySlot
{
    private VisualElement _blackElement;
    private string _blackElementName = "backOverlay";

    public GearSlot(int position, VisualElement slotElement, InventoryGearTab tab, SlotType slotType) : base(position, slotElement, tab, slotType , false)
    {
        _blackElement = CreateBlack(_blackElementName);
    }

    protected override void HandleRightClick()
    {
        UseItem();
    }

    public override void UseItem()
    {
        //if (!_empty)
        //{
            ItemInstance item = PlayerInventory.Instance.GetItemEquip(ObjectId);

            if (item != null)
            {
                AddCacheName(item, ObjectId);

                var sendPaket = CreatorPacketsUser.CreateUseItem(ObjectId, 0);
                bool enable = GameClient.Instance.IsCryptEnabled();
                SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
            }
            else
            {
                AssignEmpty();
            }

        //}
    }

    private void AddCacheName(ItemInstance item , int objectId)
    {
        if (item != null)
        {
            StorageVariable.getInstance().AddS1Items(new VariableItem(item.ItemData.ItemName.Name, objectId));
        }
    }

    private VisualElement CreateBlack(string name)
    {

        var overlay = new VisualElement();
        overlay.name = name;
        overlay.style.top = 0;
        overlay.style.left = 0;
        overlay.style.width = new Length(100, LengthUnit.Percent);
        overlay.style.height = new Length(100, LengthUnit.Percent);
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.7f); // ������ ���� � 50% �������������
        return overlay;
    }

    public void RemoveBlackOverlay()
    {
        PrintAllChildNames(_slotElement);
        var blackOverlay = _slotElement.Q<VisualElement>(_blackElementName);

        if (blackOverlay != null)
        {
            _slotElement.Remove(blackOverlay);
        }
    }

    private void PrintAllChildNames(VisualElement parent)
    {
        foreach (var child in parent.Children())
        {
            // �������� ��� ��������� ��������
            Debug.Log(child.name);

            // ���������� �������� ����� ����� �������� ��������� ��������
            PrintAllChildNames(child);
        }
    }

    public void AddBlackOverlay()
    {
        //_slotElement.style.backgroundImage = background;
        var blackOverlay = _slotElement.Q<VisualElement>(_blackElementName);

        if (blackOverlay == null)
        {
            _slotElement.Add(_blackElement);
        }

    }
}
