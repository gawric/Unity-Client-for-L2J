using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TradeRequestWindow : L2PopupWindow
{
    private static TradeRequestWindow _instance;
    private VisualElement _progressBar;
    private VisualElement _progressBarBg;
    private Label _tradeRequestText;
    private Button _confirmButton;
    private Button _cancelButton;

    private string _requesterName;
    private float _timeoutSeconds;
    private int _senderId;
    private Coroutine _countdownCoroutine;

    public static TradeRequestWindow Instance
    {
        get { return _instance; }
    }

    public event Action<bool> OnTradeResponse; // true = подтверждено, false = отменено

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

    public void AddData(SendTradeRequest request)
    {
        _senderId = request.SenderId;

        var target = World.Instance.getEntityName(_senderId);

        _requesterName = target;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/TradeRequestWindow");
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
        _tradeRequestText = (Label)GetElementById("tradeRequestText");
        _confirmButton = (Button)GetElementById("ConfirmButton");
        _cancelButton = (Button)GetElementById("CancelButton");

        RegisterClickWindowEvent(_windowEle, dragArea);
        _confirmButton.RegisterCallback<ClickEvent>(OnConfirmClicked);
        _cancelButton.RegisterCallback<ClickEvent>(OnCancelClicked);
    }

    public void ShowWindow(float timeoutSeconds = 20f)
    {
        _timeoutSeconds = timeoutSeconds;

        base.ShowWindow();

        UpdateTradeRequestText();
        ResetProgressBar();
        StartCountdown();
    }

    private void UpdateTradeRequestText()
    {
        _tradeRequestText.text = $"{_requesterName} is requesting a trade. Do you wish to continue?";
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

    private void OnConfirmClicked(ClickEvent evt)
    {
        SendAcceptResponse();
        HideWindow();
    }

    private void OnCancelClicked(ClickEvent evt)
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
        // Отправляем пакет подтверждения торговли
        var sendPacket = CreatorPacketsUser.CreateAnswerTradeRequest(1);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPacket, enable, enable);

        OnTradeResponse?.Invoke(true);
    }

    private void SendDeclineResponse()
    {
        // Отправляем пакет отклонения торговли
        var sendPacket = CreatorPacketsUser.CreateAnswerTradeRequest(0);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPacket, enable, enable);

        OnTradeResponse?.Invoke(false);
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
        if (_confirmButton != null)
            _confirmButton.UnregisterCallback<ClickEvent>(OnConfirmClicked);
        if (_cancelButton != null)
            _cancelButton.UnregisterCallback<ClickEvent>(OnCancelClicked);
    }
}