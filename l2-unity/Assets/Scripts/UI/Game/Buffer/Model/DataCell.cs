
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.Port;
using static UnityEngine.Rendering.DebugUI;


public class DataCell
{
    private bool _isBusy;
    private int _idSkill;
    private int _level;
    private VisualElement _element;
    private int _position = 0;
    private Skillgrp _skill;
    private float _activeTime = 0;
    private float _startTime;

    //Smooth movement counter
    private bool _switchHide = false;
    private bool _switchShow = false;
    private float _durationSmooth = 0;
    //procent of the total
    public float _intervalUse = 0.25f;
    private Label _label;
    private Label _labelShadow;
    private VisualElement _labelTemplate;
    public DataCell(int idSkill , VisualElement element  ,  VisualElement labelTemplate, int position)
    {
        _idSkill = idSkill;
        _element = element;
        _isBusy = false;
        _position = position;
        _label = labelTemplate.Q<Label>("LabelText");
        _labelShadow = labelTemplate.Q<Label>("LabelTextShadow");
        _labelTemplate = labelTemplate;
        RegisterLabelChange(_label);
    }


    public void ResetData()
    {
        SetSkillId(-1);
        SetLevel(-1);
        SetBusy(false);
        SetText("");
        ResetDurationSmooth();
        _skill = null;
    }

    public void SetText(string text)
    {
        _label.text = text;
    }

 
    public void RefreshData(int skillId , bool isbusy ,int level)
    {
        _skill = SkillgrpTable.Instance.GetSkill(skillId , level);
        SetSkillId(skillId);
        SetLevel(level);
        SetBusy(isbusy);

        if(isbusy != false) SetIcon(skillId, level);
    }

    public int GetSkillId()
    {
        return _idSkill;
    }

    public void SetSkillId(int skillId)
    {
        _idSkill = skillId;
    }

    public void SetLevel(int level)
    {
        _level = level;
    }

    public void ResetDurationSmooth()
    {
        _durationSmooth = 0;
    }

    public int GetLevel()
    {
        return _level;
    }

    public bool IsBusy()
    {
        return _isBusy;
    }

    public void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
    }

    public bool IsPassiveSkill()
    {
        if (_skill != null)
        {
            if (_skill.OperateType == 2) return true;
        }
        
        return false;
    }

    public void ShowCell(bool show)
    {
        if (show)
        {
            
            _element.style.display = DisplayStyle.Flex;
            _labelTemplate.style.display = DisplayStyle.Flex;
        }
        else
        {
            _element.style.display = DisplayStyle.None;
            _labelTemplate.style.display = DisplayStyle.None;
        }
        
    }

    public bool IsHideElements()
    {
        float opacity = _element.resolvedStyle.opacity;

        if (opacity >= 1 & _switchHide != true)
        {
            _switchHide = true;
            _switchShow = false;
            _durationSmooth = 0;
        }
        
        return _switchHide;
    }

    public bool IsShowElements()
    {
        if (_element.resolvedStyle.opacity <= 0.4f & _switchShow != true)
        {
            _switchShow = true;
            _switchHide = false;
            _durationSmooth = 0;
        }

        return _switchShow;
    }

    public void SetTimeDeltaTime(float deltatime)
    {
        _durationSmooth += deltatime;
    }
    public float GetSmoothTime()
    {
        return _durationSmooth;
    }

    public VisualElement GetElement()
    {
        return _element;
    }

    public int GetPosition()
    {
        return _position;
    }

    public void SetIcon(int skillId  , int level)
    {
        if (_skill == null) _skill = SkillgrpTable.Instance.GetSkill(skillId , level);
        var icon = IconManager.Instance.LoadTextureByName(_skill.Icon);
        var foundChild = _element.Query<VisualElement>(name: "SlotBg").First();
        if(foundChild != null) foundChild.style.backgroundImage = icon;
    }

    public float GetRemainingTime(float time)
    {
        float elapsedTime = time - _startTime;
        float timeEnd = _activeTime - elapsedTime;
        return Mathf.Max(0, timeEnd);
    }

    public void SetActiveTime(float activeTime)
    {
        _activeTime  = activeTime;
        _startTime = Time.time;
    }

    public int GetIntervalUse()
    {
        return Mathf.RoundToInt(_activeTime * _intervalUse);
    }

    private void RegisterLabelChange(Label label)
    {
        label.RegisterValueChangedCallback(evt =>
        {
            _label.text = evt.newValue;
            _labelShadow.text = evt.newValue;
        });
    }




}
