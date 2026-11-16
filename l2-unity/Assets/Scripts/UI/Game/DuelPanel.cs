using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class DuelPanel : IDisposable
{
    readonly PlayerDuel _duel;
    readonly VisualElement _panel;
    readonly Label _status;
    readonly Label _fighters;
    readonly Button _accept;
    readonly Button _decline;
    readonly Button _surrender;
    readonly MouseOverDetectionManipulator _mouse;
    IVisualElementScheduledItem _timeout;
    DuelPhase _previous;

    public DuelPanel(VisualElement parent, PlayerDuel duel)
    {
        _duel = duel;
        _panel = new VisualElement { name = "DuelPanel" };
        _panel.style.position = Position.Absolute;
        _panel.style.top = 150;
        _panel.style.left = Length.Percent(50);
        _panel.style.translate = new Translate(Length.Percent(-50), 0);
        _panel.style.width = 300;
        _panel.style.paddingLeft = _panel.style.paddingRight = 10;
        _panel.style.paddingTop = _panel.style.paddingBottom = 8;
        _panel.style.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.95f);
        _panel.style.color = Color.white;
        _panel.style.fontSize = 12;
        _status = new Label();
        _status.style.whiteSpace = WhiteSpace.Normal;
        _fighters = new Label();
        _panel.Add(_status);
        _panel.Add(_fighters);
        var buttons = new VisualElement();
        buttons.style.flexDirection = FlexDirection.Row;
        _accept = new Button(() => DuelActions.Respond(true)) { text = "Accept" };
        _decline = new Button(() => DuelActions.Respond(false)) { text = "Decline" };
        _surrender = new Button(DuelActions.Surrender) { text = "Surrender" };
        buttons.Add(_accept);
        buttons.Add(_decline);
        buttons.Add(_surrender);
        _panel.Add(buttons);
        _mouse = new MouseOverDetectionManipulator(_panel);
        _panel.AddManipulator(_mouse);
        parent.Add(_panel);
        _duel.Changed += Refresh;
        Refresh();
    }

    public void Dispose()
    {
        _duel.Changed -= Refresh;
        _timeout?.Pause();
        _mouse.Disable();
        _panel.RemoveFromHierarchy();
    }

    void Refresh()
    {
        bool invitation = _duel.Phase == DuelPhase.Invitation;
        bool active = _duel.Phase == DuelPhase.Active;
        bool visible = _duel.Phase != DuelPhase.None;
        _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        _accept.style.display = _decline.style.display = invitation ? DisplayStyle.Flex : DisplayStyle.None;
        _surrender.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        _status.text = invitation ? "Duel request from " + _duel.OpponentName
            : active ? "Duel: " + _duel.OpponentName : "Waiting for the duel to start...";
        var rows = new List<string>();
        foreach (DuelFighterInfo fighter in _duel.Fighters)
            rows.Add(fighter.Name + "  CP " + fighter.Cp + "/" + fighter.MaxCp +
                "  HP " + fighter.Hp + "/" + fighter.MaxHp + "  MP " + fighter.Mp + "/" + fighter.MaxMp);
        _fighters.text = string.Join("\n", rows);
        if (visible)
        {
            _mouse.Enable();
            _panel.BringToFront();
        }
        else
            _mouse.Disable();
        if (_previous != _duel.Phase)
        {
            _timeout?.Pause();
            if (invitation || _duel.Phase == DuelPhase.AwaitingStart)
                _timeout = _panel.schedule.Execute(ExpireRequest).StartingIn(20000);
            _previous = _duel.Phase;
        }
    }

    void ExpireRequest()
    {
        if (_duel.Phase == DuelPhase.Invitation)
            DuelActions.Respond(false);
        else if (_duel.Phase == DuelPhase.AwaitingStart)
            _duel.Clear();
    }
}
