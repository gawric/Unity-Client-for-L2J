using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Party roster HUD - a fixed pool of PartyMemberRow instances (party is capped at 9 members,
/// i.e. 8 others besides the local player), kept in sync with PartyManager's live state.
/// Auto-shows/hides based on whether the local player is currently in a party.
/// Stripped down to name + CP/HP/MP bars for now - no header controls, no buffs yet.
/// </summary>
public class PartyWindow : L2Window
{
    private const int MAX_VISIBLE_MEMBERS = 8;

    private static PartyWindow _instance;
    public static PartyWindow Instance => _instance;

    private VisualTreeAsset _rowTemplate;
    private VisualElement _memberListContainer;
    private Button _toggleSizeButton;
    private Button _toggleEffectsButton;

    private readonly List<PartyMemberRowView> _rows = new List<PartyMemberRowView>();
    private readonly Dictionary<int, PartyMemberRowView> _rowsByMemberId = new Dictionary<int, PartyMemberRowView>();

    private bool _showBuffRows = true;
    private PartyEffectDisplayMode _effectMode = PartyEffectDisplayMode.All;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromPartyManager();
        _instance = null;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Party/PartyWindow");
        _rowTemplate = LoadAsset("Data/UI/_Elements/Template/PartyMemberRow");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        VisualElement dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        _memberListContainer = GetElementById("MemberList");

        // Optional - the current layout doesn't have these yet. Wired up only if/when present,
        // instead of assuming they exist and crashing BuildWindow's coroutine if they don't.
        _toggleSizeButton = _windowEle.Q<Button>("ToggleSizeButton");
        _toggleEffectsButton = _windowEle.Q<Button>("ToggleEffectsButton");

        _toggleSizeButton?.RegisterCallback<ClickEvent>(OnToggleSizeClicked);
        _toggleEffectsButton?.RegisterCallback<ClickEvent>(OnToggleEffectsClicked);

        for (int i = 0; i < MAX_VISIBLE_MEMBERS; i++)
        {
            VisualElement rowElement = _rowTemplate.Instantiate()[0];
            _memberListContainer.Add(rowElement);

            PartyMemberRowView row = new PartyMemberRowView(rowElement);
            row.Show(false);
            _rows.Add(row);
        }

        yield return new WaitForEndOfFrame();

        SubscribeToPartyManager();
        RefreshFromManagerState();
    }

    private void SubscribeToPartyManager()
    {
        if (PartyManager.Instance == null)
        {
            Debug.LogWarning("[PartyDebug] PartyWindow: PartyManager.Instance is null - is the PartyManager component attached/enabled in the scene?");
            return;
        }

        PartyManager.Instance.OnPartyChanged += OnPartyChanged;
        PartyManager.Instance.OnMemberUpdated += OnMemberUpdated;
        PartyManager.Instance.OnMemberBuffsUpdated += OnMemberBuffsUpdated;
    }

    private void UnsubscribeFromPartyManager()
    {
        if (PartyManager.Instance == null)
        {
            return;
        }

        PartyManager.Instance.OnPartyChanged -= OnPartyChanged;
        PartyManager.Instance.OnMemberUpdated -= OnMemberUpdated;
        PartyManager.Instance.OnMemberBuffsUpdated -= OnMemberBuffsUpdated;
    }

    private void RefreshFromManagerState()
    {
        if (PartyManager.Instance == null)
        {
            HideWindow();
            return;
        }

        OnPartyChanged();
    }

    private void OnPartyChanged()
    {
        _rowsByMemberId.Clear();

        int rowIndex = 0;
        foreach (PartyMemberData member in PartyManager.Instance.Members)
        {
            if (rowIndex >= _rows.Count)
            {
                // Classic Interlude caps a party at 9 (8 other members) - if the server ever sends
                // more, the extras are just not shown rather than throwing.
                break;
            }

            PartyMemberRowView row = _rows[rowIndex];
            row.Bind(member);
            row.SetBuffRowForceHidden(!_showBuffRows);
            row.RefreshBuffs(member, _effectMode);
            row.Show(true);
            _rowsByMemberId[member.ObjectId] = row;
            rowIndex++;
        }

        for (int i = rowIndex; i < _rows.Count; i++)
        {
            _rows[i].Show(false);
        }

        if (PartyManager.Instance.IsInParty)
        {
            ShowWindow();
        }
        else
        {
            HideWindow();
        }
    }

    private void OnMemberUpdated(int memberObjectId)
    {
        if (_rowsByMemberId.TryGetValue(memberObjectId, out PartyMemberRowView row))
        {
            PartyMemberData member = PartyManager.Instance.GetMember(memberObjectId);
            if (member != null)
            {
                row.RefreshStats(member);
            }
        }
    }

    private void OnMemberBuffsUpdated(int memberObjectId)
    {
        if (_rowsByMemberId.TryGetValue(memberObjectId, out PartyMemberRowView row))
        {
            PartyMemberData member = PartyManager.Instance.GetMember(memberObjectId);
            if (member != null)
            {
                row.RefreshBuffs(member, _effectMode);
            }
        }
    }

    private void OnToggleSizeClicked(ClickEvent evt)
    {
        // "Small" view just hides the buff icon strip per row - the name/bars stay visible either way.
        _showBuffRows = !_showBuffRows;

        foreach (PartyMemberRowView row in _rows)
        {
            row.SetBuffRowForceHidden(!_showBuffRows);
        }

        foreach (var kvp in _rowsByMemberId)
        {
            PartyMemberData member = PartyManager.Instance.GetMember(kvp.Key);
            if (member != null)
            {
                kvp.Value.RefreshBuffs(member, _effectMode);
            }
        }
    }

    private void OnToggleEffectsClicked(ClickEvent evt)
    {
        _effectMode = (PartyEffectDisplayMode)(((int)_effectMode + 1) % 3);

        foreach (var kvp in _rowsByMemberId)
        {
            PartyMemberData member = PartyManager.Instance.GetMember(kvp.Key);
            if (member != null)
            {
                kvp.Value.RefreshBuffs(member, _effectMode);
            }
        }
    }
}
