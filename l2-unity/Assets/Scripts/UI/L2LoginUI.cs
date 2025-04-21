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
        if (loading)
        {
            //Debug.Log("L2LoginUI Auto : DESTROYYYY");
            _instance = null;
        }
    }

    protected override void LoadUI() {
        base.LoadUI();

        LoginWindow.Instance.AddWindow(_rootVisualContainer);
        CharSelectWindow.Instance.AddWindow(_rootVisualContainer);
        CharSelectWindow.Instance.HideWindow();
        CharCreationWindow.Instance.AddWindow(_rootVisualContainer);
        CharCreationWindow.Instance.HideWindow();
        LicenseWindow.Instance.AddWindow(_rootVisualContainer);
        LicenseWindow.Instance.HideWindow();
        ServerSelectWindow.Instance.AddWindow(_rootVisualContainer);
        ServerSelectWindow.Instance.HideWindow();
    }

    public void ShowServerSelectWindow() {
        LoginWindow.Instance.HideWindow();
        LicenseWindow.Instance.HideWindow();
        ServerSelectWindow.Instance.ShowWindow();
    }

    public void ShowLicenseWindow() {
        LoginWindow.Instance.HideWindow();
        LicenseWindow.Instance.ShowWindow();
        ServerSelectWindow.Instance.HideWindow();
    }

    public void ShowCharSelectWindow() {
        LoginWindow.Instance.HideWindow();
        CharCreationWindow.Instance.HideWindow();
        CharSelectWindow.Instance.ShowWindow();
        ServerSelectWindow.Instance.HideWindow();
    }

    public void ShowLoginWindow() {
        CharSelectWindow.Instance.HideWindow();
        LoginWindow.Instance.ShowWindow();
        CharCreationWindow.Instance.HideWindow();
        CharSelectWindow.Instance.HideWindow();
        LicenseWindow.Instance.HideWindow();
        ServerSelectWindow.Instance.HideWindow();
    }

    public void ShowCharCreationWindow() {
        CharSelectWindow.Instance.HideWindow();
        CharCreationWindow.Instance.Init();
        CharCreationWindow.Instance.ShowWindow();

    }

    public void SetCharTemplations(List<PlayerTemplates> playerTemplates)
    {
        CharCreationWindow.Instance.Clear();
        CharCreationWindow.Instance.SetPlayerTemplates(playerTemplates);
    }

    public void ShowCharCreationError(string text)
    {
        CharCreationWindow.Instance.SetlabelError(text);
    }
}
