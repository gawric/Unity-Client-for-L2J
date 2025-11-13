using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class BufferPanel : L2Window
{

    private static BufferPanel _instance;
    private VisualTreeAsset _barSlotTemplate;
    private float _defaultLeftOffset = 200;
    private Dictionary<int, DataCell> _dictElement;

    private int[] _deathPenalty;
    private int[] _weightPenalty;
    private int[] _weaponPenalty;
    public static BufferPanel Instance { get { return _instance; } }
    public BLink _bLink;
    public VisualElement _content;
    private FilterData _filterData;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _dictElement = new Dictionary<int, DataCell>();
            _filterData = new FilterData(_dictElement);
            _bLink = new BLink(this);
        }
        else
        {
            Destroy(this);
        }
    }


    protected override void LoadAssets()
    {
        _barSlotTemplate = LoadAsset("Data/UI/_Elements/Template/SlotBuffer");
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Buffer/SkillTray");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        var dragArea = GetElementByClass("drag-area");
        _content = GetElementById("content");
        
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        CreateItemsPanel1();

        yield return new WaitForEndOfFrame();

        _content.style.opacity = 0;
        MovePanelToChatPosition(_defaultLeftOffset);
    }

    public void RefreshPenalty(EtcStatusUpdate etcStatusUpdatePacket)
    {
        _deathPenalty = etcStatusUpdatePacket.DeathPenalty;
        _weightPenalty = etcStatusUpdatePacket.WeightPenalty;
        _weaponPenalty = etcStatusUpdatePacket.WeaponPenalty;

        AddDataCell(_deathPenalty[0], _deathPenalty[1]);
        AddDataCell(_weightPenalty[0], _weightPenalty[1]);
        AddDataCell(_weaponPenalty[0], _weaponPenalty[1]);

        DeleteDataCell(_deathPenalty[0], _deathPenalty[1]);
        DeleteDataCell(_weightPenalty[0], _weightPenalty[1]);
        DeleteDataCell(_weaponPenalty[0], _weaponPenalty[1]);
    }

    public void AddDataCell(int skillId, int skillLevel)
    {
        if (skillLevel == 0) return;
        if (_content.style.opacity == 0) _content.style.opacity = 1;
        if (_filterData.IsContain(skillId)) return;

        DataCell data = _filterData.GetEmptyCell();
        data.RefreshData(skillId, true, skillLevel);
        data.ShowCell(true);
    }

    public void AddDataCellToTime(int skillId, int skillLevel, int time)
    {

        Skillgrp skill = SkillgrpTable.Instance.GetSkill(skillId, skillLevel);

        if (skill != null)
        {

            if (skillLevel == 0) return;
            if (_content.style.opacity == 0) _content.style.opacity = 1;
            if (_filterData.IsContain(skillId)) return;

            RebindCellElseOnlyPenalty();
            DataCell data = _filterData.GetEmptyCell();
            data.RefreshData(skillId, true, skillLevel);
            data.ShowCell(true);
            data.SetActiveTime(time);
            _bLink.StartBlinking(data);
        }
        else
        {
            Debug.LogWarning("Buffpanel > AddDataCellToTime: Не критическая ошибка, мы не нашли skill id при попытке обновить herb effect!!!! skillID " + skillId + " level " + skillLevel);
        }


        //RemoveElementAfterDelay();
    }

  
    public void RemoveAllEffects()
    {
        List<DataCell> list = _filterData.GetListActiveAndBusy();
        ResetList(list);
        HideContentElseNoElements();
    }

    private void HideContentElseNoElements()
    {
        if (!_filterData.HasElements())
        {
            _content.style.opacity = 0;
        }
    }

    private void ResetList(List<DataCell> list)
    {
        if (list != null && list.Count > 0)
        {
            foreach (DataCell cell in list)
            {
                cell.ResetData();
                cell.ShowCell(false);
            }
        }
    }

    public void DeleteDataCell(int skillId, int skillLevel)
    {
        if(skillLevel == 0)
        {
            DataCell data = _filterData.FilterBySkillId(skillId);

            if(data != null)
            {
                data.RefreshData(-1, false, 0);
                data.ShowCell(false);
            }
        }
    }
   


    private void CreateItemsPanel1()
    {
        for(int i = 1; i < 25; i++)
        {
            VisualElement cell = GetElementById("SlotBuffer"+i);
            VisualElement labelTemplate = GetElementById("LabelBuffer" + i);
            VisualElement slot = GetElementById("border"+i);

            if (cell != null)
            {
                 cell.style.display = DisplayStyle.None;
                 labelTemplate.style.display = DisplayStyle.None;
                _dictElement.Add(i, new DataCell(-1, cell , labelTemplate  , slot ,  i));
            }
        }
    }




    private void RebindCellElseOnlyPenalty()
    {
        if (_filterData.IsPassiveFirstRows() != null)
        {
            MoveAllPassiveTo2Rows();
        }
    }

    public void  MoveAllPassiveTo2Rows()
    {
        List<DataCell> list = _filterData.GetAllPassive(1, 12);

        foreach (DataCell moveCell in list)
        {
            //position 13 -24 (rows2)
            DataCell empty = _filterData.GetEmptyCell(13, 24);
            empty.RefreshData(moveCell.GetSkillId(), true, moveCell.GetLevel());
            empty.ShowCell(true);

            moveCell.ResetData();
            moveCell.ShowCell(false);
            _dictElement[moveCell.GetPosition()] = moveCell;
        }
    }

  

    public void MovePanelToChatPosition(float sourceY)
    {
        _windowEle.style.top = 3;
        _windowEle.style.left = sourceY;

    }


    public SkillInstance GetCellByPosition(int position)
    {
        DataCell data = _filterData.GetCellByPosition(position);
        return (data != null)? data.GetSkillInstance() : null;
    }
    private void OnDestroy()
    {
        _instance = null;
        _dictElement.Clear();
        _dictElement = null;
        _bLink.OnDisable();
    }

}
