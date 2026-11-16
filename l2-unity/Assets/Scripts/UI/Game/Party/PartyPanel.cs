using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PartyPanel : IDisposable
{
    sealed class MemberRow
    {
        public Label Name;
        public ProgressBar Hp;
        public ProgressBar Mp;
    }

    readonly PlayerParty _party;
    readonly VisualElement _panel;
    readonly VisualElement _rows;
    readonly Label _title;
    readonly MouseOverDetectionManipulator _mouse;
    readonly Dictionary<int, MemberRow> _views = new Dictionary<int, MemberRow>();

    public PartyPanel(VisualElement parent, PlayerParty party)
    {
        _party = party;
        _panel = new VisualElement { name = "PartyPanel" };
        _panel.style.position = Position.Absolute;
        _panel.style.left = 6;
        _panel.style.top = 84;
        _panel.style.width = 176;
        _panel.style.paddingLeft = _panel.style.paddingRight = 5;
        _panel.style.paddingTop = _panel.style.paddingBottom = 4;
        _panel.style.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.85f);
        _panel.style.color = Color.white;
        _panel.style.fontSize = 11;

        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        _title = new Label("Party");
        _title.style.flexGrow = 1;
        header.Add(_title);
        header.Add(new Button(PartyActions.Leave) { text = "Leave" });
        _panel.Add(header);
        _rows = new VisualElement();
        _panel.Add(_rows);
        _mouse = new MouseOverDetectionManipulator(_panel);
        _panel.AddManipulator(_mouse);
        Refresh();
        parent.Add(_panel);
        _party.Changed += Refresh;
    }

    public void Dispose()
    {
        _party.Changed -= Refresh;
        _mouse.Disable();
        _panel.RemoveFromHierarchy();
    }

    void Refresh()
    {
        bool visible = _party.IsInParty && _party.Members.Count > 0;
        _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (visible)
        {
            _mouse.Enable();
            _panel.BringToFront();
        }
        else
            _mouse.Disable();
        _title.text = "Party (" + (_party.Members.Count + 1) + ")";
        if (RosterChanged())
            BuildRows();
        foreach (PartyMemberInfo member in _party.Members)
        {
            MemberRow row = _views[member.ObjectId];
            row.Name.text = (member.ObjectId == _party.LeaderId ? "* " : "") + member.Name + "  Lv " + member.Level;
            SetGauge(row.Hp, member.Hp, member.MaxHp, "HP");
            SetGauge(row.Mp, member.Mp, member.MaxMp, "MP");
        }
    }

    bool RosterChanged()
    {
        if (_views.Count != _party.Members.Count)
            return true;
        foreach (PartyMemberInfo member in _party.Members)
            if (!_views.ContainsKey(member.ObjectId))
                return true;
        return false;
    }

    void BuildRows()
    {
        _views.Clear();
        var content = new VisualElement();
        foreach (PartyMemberInfo member in _party.Members)
        {
            int objectId = member.ObjectId;
            var element = new VisualElement();
            element.style.marginTop = 4;
            var name = new Label();
            name.RegisterCallback<ClickEvent>(_ => SelectMember(objectId));
            element.Add(name);
            ProgressBar hp = AddGauge(element, new Color(0.5f, 0.1f, 0.1f));
            ProgressBar mp = AddGauge(element, new Color(0.15f, 0.3f, 0.7f));
            content.Add(element);
            _views[objectId] = new MemberRow { Name = name, Hp = hp, Mp = mp };
        }
        _rows.Clear();
        _rows.Add(content);
    }

    static ProgressBar AddGauge(VisualElement parent, Color color)
    {
        var bar = new ProgressBar { lowValue = 0, highValue = 1 };
        bar.style.height = 13;
        bar.style.fontSize = 9;
        VisualElement fill = bar.Q<VisualElement>(className: "unity-progress-bar__progress");
        if (fill != null)
            fill.style.backgroundColor = color;
        parent.Add(bar);
        return bar;
    }

    static void SetGauge(ProgressBar bar, int current, int maximum, string title)
    {
        bar.value = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0;
        bar.title = title + " " + current + "/" + maximum;
    }

    static void SelectMember(int objectId)
    {
        PlayerEntity player = PlayerEntity.Instance;
        if (player == null || player.Identity == null)
            return;
        Vector3 position = player.Identity.GetL2jPos();
        IncomingPacketActions.Game?.Send(new ClickActionCommand(objectId, (int)position.x, (int)position.y, (int)position.z, 0));
    }
}
