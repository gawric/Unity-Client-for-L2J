using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Wraps one pre-instantiated PartyMemberRow.uxml element (see PartyWindow, which owns a fixed
/// pool of these - not a MonoBehaviour, just a plain data-bound view like Nameplate).
/// </summary>
public class PartyMemberRowView
{
    private const int BUFF_ICON_COUNT = 12;

    public VisualElement Root { get; }
    public int MemberObjectId { get; private set; } = -1;

    private readonly Label _nameLabel;
    private readonly VisualElement _cpBarBg;
    private readonly VisualElement _cpBar;
    private readonly VisualElement _hpBarBg;
    private readonly VisualElement _hpBar;
    private readonly VisualElement _mpBarBg;
    private readonly VisualElement _mpBar;
    private readonly VisualElement _buffRow;
    private readonly VisualElement[] _buffIcons;

    private bool _forceHideBuffRow;

    public PartyMemberRowView(VisualElement root)
    {
        Root = root;
        _nameLabel = root.Q<Label>("NameLabel");
        _cpBarBg = root.Q<VisualElement>("CPBarBG");
        _cpBar = root.Q<VisualElement>("CPBar");
        _hpBarBg = root.Q<VisualElement>("HPBarBG");
        _hpBar = root.Q<VisualElement>("HPBar");
        _mpBarBg = root.Q<VisualElement>("MPBarBG");
        _mpBar = root.Q<VisualElement>("MPBar");
        _buffRow = root.Q<VisualElement>("BuffRow");

        _buffIcons = new VisualElement[BUFF_ICON_COUNT];
        for (int i = 0; i < BUFF_ICON_COUNT; i++)
        {
            _buffIcons[i] = root.Q<VisualElement>("BuffIcon" + i);
        }
    }

    public void Show(bool show)
    {
        Root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Bind(PartyMemberData member)
    {
        MemberObjectId = member.ObjectId;
        RefreshStats(member);
    }

    public void RefreshStats(PartyMemberData member)
    {
        if (_nameLabel != null)
        {
            _nameLabel.text = member.Name;
        }

        SetBarRatio(_cpBarBg, _cpBar, member.MaxCp > 0 ? (float)member.CurCp / member.MaxCp : 0f);
        SetBarRatio(_hpBarBg, _hpBar, member.MaxHp > 0 ? (float)member.CurHp / member.MaxHp : 0f);
        SetBarRatio(_mpBarBg, _mpBar, member.MaxMp > 0 ? (float)member.CurMp / member.MaxMp : 0f);
    }

    private void SetBarRatio(VisualElement barBg, VisualElement bar, float ratio)
    {
        if (barBg == null || bar == null)
        {
            return;
        }

        float bgWidth = barBg.resolvedStyle.width;
        bar.style.width = bgWidth * Mathf.Clamp01(ratio);
    }

    /// <summary>Small/large toggle - forces the buff row hidden regardless of actual buff count.</summary>
    public void SetBuffRowForceHidden(bool hidden)
    {
        _forceHideBuffRow = hidden;
        if (hidden && _buffRow != null)
        {
            _buffRow.style.display = DisplayStyle.None;
        }
    }

    public void RefreshBuffs(PartyMemberData member, PartyEffectDisplayMode mode)
    {
        if (_buffRow == null)
        {
            return;
        }

        if (_forceHideBuffRow)
        {
            _buffRow.style.display = DisplayStyle.None;
            return;
        }

        int shown = 0;
        for (int i = 0; i < member.Buffs.Count && shown < _buffIcons.Length; i++)
        {
            PartyBuffInfo buff = member.Buffs[i];
            Skillgrp skill = SkillgrpTable.Instance.GetSkill(buff.SkillId, buff.SkillLevel);
            if (skill == null || !MatchesMode(skill, mode))
            {
                continue;
            }

            VisualElement icon = _buffIcons[shown];
            icon.style.backgroundImage = IconManager.Instance.LoadTextureByName(skill.Icon);
            icon.style.display = DisplayStyle.Flex;
            shown++;
        }

        for (int i = shown; i < _buffIcons.Length; i++)
        {
            _buffIcons[i].style.display = DisplayStyle.None;
        }

        _buffRow.style.display = shown > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private bool MatchesMode(Skillgrp skill, PartyEffectDisplayMode mode)
    {
        if (mode == PartyEffectDisplayMode.All)
        {
            return true;
        }

        bool isDebuff = skill.Debuff == 1;
        return mode == PartyEffectDisplayMode.DebuffsOnly ? isDebuff : !isDebuff;
    }
}
