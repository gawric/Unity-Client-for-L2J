using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PartyInvitationWindow : L2PopupWindow
{
    private static PartyInvitationWindow _instance;
    private VisualElement _progressBar;
    private VisualElement _progressBarBg;
    private Label _invitationText;
    private Button _yesButton;
    private Button _noButton;

    private string _inviterName;
    private string _lootDistribution;
    private float _timeoutSeconds = 20f;
    private Coroutine _countdownCoroutine;

    public static PartyInvitationWindow Instance
    {
        get { return _instance; }
    }

    public event Action<bool> OnInvitationResponse;

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

    public void AddData(AskJoinParty request)
    {
        _inviterName = request.RequestorName;
        _lootDistribution = request.DistributionType.ToString();
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/JoinPartyWindow");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        OnCenterScreen(root);
    }

    protected override void InitWindow(VisualElement root)
    {
        base.InitWindow(root);

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        _progressBar = GetElementById("ProgressGauge");
        _progressBarBg = GetElementById("ProgressBg");
        _invitationText = (Label)GetElementById("invitationText");
        _yesButton = (Button)GetElementById("YesButton");
        _noButton = (Button)GetElementById("NoButton");

        RegisterClickWindowEvent(_windowEle, dragArea);
        _yesButton.RegisterCallback<ClickEvent>(OnYesClicked);
        _noButton.RegisterCallback<ClickEvent>(OnNoClicked);
    }

    public override void ShowWindow()
    {
        base.ShowWindow();

        UpdateInvitationText();
        ResetProgressBar();
        StartCountdown();
    }

    private void UpdateInvitationText()
    {
        _invitationText.text = $"Do you accept {_inviterName}'s party invitation?\n(Item Distribution: {_lootDistribution})"; //todo: get from resources(localization)
    }

    private void ResetProgressBar()
    {
        _progressBar.style.width = Length.Percent(100);
    }

    private void StartCountdown()
    {
        if (_countdownCoroutine != null)
            StopCoroutine(_countdownCoroutine);

        _countdownCoroutine = StartCoroutine(CountdownTimer());
    }

    private IEnumerator CountdownTimer()
    {
        float currentTime = _timeoutSeconds;

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            float progress = currentTime / _timeoutSeconds;

            // Обновляем прогресс-бар
            _progressBar.style.width = Length.Percent(progress * 100);

            yield return null;
        }

        OnTimeout();
    }

    private void OnYesClicked(ClickEvent evt)
    {
        SendAcceptResponse();
        HideWindow();
    }

    private void OnNoClicked(ClickEvent evt)
    {
        SendDeclineResponse();
        HideWindow();
    }

    private void OnTimeout()
    {
        SendDeclineResponse();
        HideWindow();
    }

    private void SendAcceptResponse()
    {
        var sendPacket = CreatorPacketsUser.CreateRequestAnswerJoinParty(1);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPacket, enable, enable);

        OnInvitationResponse?.Invoke(true);
    }

    private void SendDeclineResponse()
    {
        var sendPacket = CreatorPacketsUser.CreateRequestAnswerJoinParty(0);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPacket, enable, enable);

        OnInvitationResponse?.Invoke(false);
    }

    public override void HideWindow()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        base.HideWindow();
    }

    private void OnDestroy()
    {
        _instance = null;

        // Отписываемся от событий
        if (_yesButton != null)
            _yesButton.UnregisterCallback<ClickEvent>(OnYesClicked);
        if (_noButton != null)
            _noButton.UnregisterCallback<ClickEvent>(OnNoClicked);
    }
}