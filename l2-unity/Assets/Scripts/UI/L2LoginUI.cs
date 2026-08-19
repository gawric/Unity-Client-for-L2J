using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class L2LoginUI : L2UI
{
    [SerializeField] private VisualElement _loadingElement;

    private bool loading = false;

    private static L2LoginUI _instance;
    public static L2LoginUI Instance { get { return _instance; } }

    public bool IsLoading { get { return loading; } }

    public void SetLoading(bool loading)
    {
        this.loading = loading;
    }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
            DiBootstrap.EnsureLoginScope();
        } else {
            Destroy(this);
        }
    }

    protected override void Update() {
        base.Update();
    }

    public void OnManualDestroy() {
        
        if (loading)
        {
           //Debug.Log("L2LoginUI Manual : DESTROYYYY");
            _instance = null;
        }
    }

    private void OnDestroy()
    { 
        IncomingPacketActions.BindLogin(null);
        if (loading)
        {
            //Debug.Log("L2LoginUI Auto : DESTROYYYY");
            _instance = null;
        }
    }

    protected override void LoadUI() {
        base.LoadUI();

        IncomingPacketActions.LoginWindow.AddWindow(_rootVisualContainer);
        IncomingPacketActions.CharSelect.AddWindow(_rootVisualContainer);
        IncomingPacketActions.CharSelect.HideWindow();
        IncomingPacketActions.CharCreate.AddWindow(_rootVisualContainer);
        IncomingPacketActions.CharCreate.HideWindow();
        IncomingPacketActions.LicenseWindow.AddWindow(_rootVisualContainer);
        IncomingPacketActions.LicenseWindow.HideWindow();
        IncomingPacketActions.ServerSelect.AddWindow(_rootVisualContainer);
        IncomingPacketActions.ServerSelect.HideWindow();
        SkillAnimationDatabase.Initialize();
    }

    public void ShowServerSelectWindow() {
        IncomingPacketActions.LoginWindow.HideWindow();
        IncomingPacketActions.LicenseWindow.HideWindow();
        IncomingPacketActions.ServerSelect.ShowWindow();
    }

    public void ShowLicenseWindow() {
        IncomingPacketActions.LoginWindow.HideWindow();
        IncomingPacketActions.LicenseWindow.ShowWindow();
        IncomingPacketActions.ServerSelect.HideWindow();
    }

    public void ShowCharSelectWindow() {
        IncomingPacketActions.LoginWindow.HideWindow();
        IncomingPacketActions.CharCreate.HideWindow();
        IncomingPacketActions.CharSelect.ShowWindow();
        IncomingPacketActions.ServerSelect.HideWindow();
    }

    public void ShowLoginWindow() {
        IncomingPacketActions.CharSelect.HideWindow();
        IncomingPacketActions.LoginWindow.ShowWindow();
        IncomingPacketActions.CharCreate.HideWindow();
        IncomingPacketActions.CharSelect.HideWindow();
        IncomingPacketActions.LicenseWindow.HideWindow();
        IncomingPacketActions.ServerSelect.HideWindow();
    }

    public void ShowCharCreationWindow() {
        IncomingPacketActions.CharSelect.HideWindow();
        IncomingPacketActions.CharCreate.Init();
        IncomingPacketActions.CharCreate.ShowWindow();

    }

    public void SetCharTemplations(List<PlayerTemplates> playerTemplates)
    {
        IncomingPacketActions.CharCreate.Clear();
        IncomingPacketActions.CharCreate.SetPlayerTemplates(playerTemplates);
    }

    public void ShowCharCreationError(string text)
    {
        IncomingPacketActions.CharCreate.SetlabelError(text);
    }
}
