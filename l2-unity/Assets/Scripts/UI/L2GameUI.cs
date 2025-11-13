using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static NativeFunctions;

public class L2GameUI : L2UI
{
    private NativeCoords _lastMousePosition;
    [SerializeField] private bool _mouseEnabled = true;

    private List<L2PopupWindow> _openedWindows;

    private static L2GameUI _instance;
    public static L2GameUI Instance { get { return _instance; } }

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

        _openedWindows = new List<L2PopupWindow>();
    }

    private void OnDestroy()
    {
        _instance = null;
    }

    protected override void Update()
    {
        base.Update();

        if (InputManager.Instance != null && InputManager.Instance.TurnCamera)
        {
            DisableMouse();
        }
        else
        {
            EnableMouse();
        }

        CheckUIInputs();
    }

    protected override void LoadUI()
    {
        base.LoadUI();

        if (L2LoginUI.Instance != null)
        {
            StartLoading();
        }

        if (ChatWindow.Instance != null)
        {
            ChatWindow.Instance.AddWindow(_rootVisualContainer);
        }
        if (SystemMenuWindow.Instance != null)
        {
            SystemMenuWindow.Instance.AddWindow(_rootVisualContainer);
        }

        if (StatusWindow.Instance != null)
        {
            StatusWindow.Instance.AddWindow(_rootVisualContainer);
        }
        if (DeadWindow.Instance != null)
        {
            DeadWindow.Instance.AddWindow(_rootVisualContainer);
            DeadWindow.Instance.HideWindow();
        }
        if (BufferPanel.Instance != null)
        {
            BufferPanel.Instance.AddWindow(_rootVisualContainer);
            BufferPanel.Instance.ShowWindow();
        }
        if (SkillbarWindow.Instance != null)
        {
            SkillbarWindow.Instance.AddWindow(_rootVisualContainer);
        }
        if (InventoryWindow.Instance != null)
        {
            InventoryWindow.Instance.AddWindow(_rootVisualContainer);
            InventoryWindow.Instance.HideWindow();
        }
        if (CharacterInfoWindow.Instance != null)
        {
            CharacterInfoWindow.Instance.AddWindow(_rootVisualContainer);
            CharacterInfoWindow.Instance.HideWindow();
        }
        if (ActionWindow.Instance != null)
        {
            ActionWindow.Instance.AddWindow(_rootVisualContainer);
            ActionWindow.Instance.HideWindow();
        }
        if (TargetWindow.Instance != null)
        {
            TargetWindow.Instance.AddWindow(_rootVisualContainer);
        }
        if (ExitWindow.Instance != null)
        {
            ExitWindow.Instance.AddWindow(_rootVisualContainer);
        }
        if (L2ToolTip.Instance != null)
        {
            L2ToolTip.Instance.AddWindow(_tooltipVisualContainer);
            L2ToolTip.Instance.HideWindow();
        }
        if (L2SlotManager.Instance != null)
        {
            L2SlotManager.Instance.AddWindow(_slotVisualContainer);
            L2SlotManager.Instance.HideWindow();
        }
        if (MenuWindow.Instance != null)
        {
            MenuWindow.Instance.AddWindow(_rootVisualContainer);
        }
        if(HtmlWindow.Instance != null)
        {
            HtmlWindow.Instance.AddWindow(_rootVisualContainer);
            HtmlWindow.Instance.HideWindow();
        }
        if (DealerWindow.Instance != null)
        {
            DealerWindow.Instance.AddWindow(_rootVisualContainer);
            DealerWindow.Instance.HideWindow();
        }
        if (ToolTipSimple.Instance != null)
        {
            ToolTipSimple.Instance.AddWindow(_rootVisualContainer);
            ToolTipSimple.Instance.HideWindow();
        }
        if (QuantityInput.Instance != null)
        {
            QuantityInput.Instance.AddWindow(_rootVisualContainer);
            QuantityInput.Instance.HideWindow();
        }
        if (SystemMessageWindow.Instance != null)
        {
            SystemMessageWindow.Instance.AddWindow(_rootVisualContainer);
            SystemMessageWindow.Instance.HideWindow();
        }

        if (EnchantWindow.Instance != null)
        {
            EnchantWindow.Instance.AddWindow(_rootVisualContainer);
            EnchantWindow.Instance.HideWindow();
        }

        if (MultiSellWindow.Instance != null)
        {
            MultiSellWindow.Instance.AddWindow(_rootVisualContainer);
            MultiSellWindow.Instance.HideWindow();
        }

        if (SkillLearnWindow.Instance != null)
        {
            SkillLearnWindow.Instance.AddWindow(_rootVisualContainer);
            SkillLearnWindow.Instance.HideWindow();
        }

        if (SkillListWindow.Instance != null)
        {
            SkillListWindow.Instance.AddWindow(_rootVisualContainer);
            SkillListWindow.Instance.HideWindow();
        }

        if (DescriptionSkillWindow.Instance != null)
        {
            DescriptionSkillWindow.Instance.AddWindow(_rootVisualContainer);
            DescriptionSkillWindow.Instance.HideWindow();
        }
        if (ShowListWindow.Instance != null)
        {
            ShowListWindow.Instance.AddWindow(_rootVisualContainer);
            ShowListWindow.Instance.HideWindow();
        }
        if (QuestListWindow.Instance != null)
        {
            QuestListWindow.Instance.AddWindow(_rootVisualContainer);
            QuestListWindow.Instance.HideWindow();
        }
        if (QuestWindow.Instance != null)
        {
            QuestWindow.Instance.AddWindow(_rootVisualContainer);
            QuestWindow.Instance.HideWindow();
        }
        if (ClanWindow.Instance != null)
        {
            ClanWindow.Instance.AddWindow(_rootVisualContainer);
            ClanWindow.Instance.HideWindow();
        }

        if (SellCropListWindow.Instance != null)
        {
            SellCropListWindow.Instance.AddWindow(_rootVisualContainer);
            SellCropListWindow.Instance.HideWindow();
        }
        if (SeedInfoWindow.Instance != null)
        {
            SeedInfoWindow.Instance.AddWindow(_rootVisualContainer);
            SeedInfoWindow.Instance.HideWindow();
        }

        if (MapWindow.Instance != null)
        {
            MapWindow.Instance.AddWindow(_rootVisualContainer);
            MapWindow.Instance.HideWindow();
        }

        if (RecipeBookWindow.Instance != null)
        {
            RecipeBookWindow.Instance.AddWindow(_rootVisualContainer);
            RecipeBookWindow.Instance.HideWindow();
        }

        if (CraftingItemWindow.Instance != null)
        {
            CraftingItemWindow.Instance.AddWindow(_rootVisualContainer);
            CraftingItemWindow.Instance.HideWindow();
        }
    }

    public void EnableMouse()
    {
        if (!_mouseEnabled)
        {
            _mouseEnabled = true;
            NativeFunctions.SetCursorPos(_lastMousePosition.X, _lastMousePosition.Y);
        }
    }

    public void DisableMouse()
    {
        if (_mouseEnabled)
        {
            NativeFunctions.GetCursorPos(out _lastMousePosition);
            _mouseEnabled = false;
        }
    }

    public void CheckUIInputs()
    {
        if (InputManager.Instance.OpenCharacerStatus)
        {
            if (CharacterInfoWindow.Instance != null)
            {
                CharacterInfoWindow.Instance.ToggleHideWindow();
            }
        }

        if (InputManager.Instance.OpenInventory)
        {
            if (InventoryWindow.Instance != null)
            {
                //InventoryWindow.Instance.ToggleHideWindowManual();
            }
        }

        if (InputManager.Instance.OpenSystemMenu)
        {
            if (SystemMenuWindow.Instance != null)
            {
                SystemMenuWindow.Instance.ToggleHideWindow();
            }
        }

        if (InputManager.Instance.OpenActions)
        {
            if (ActionWindow.Instance != null)
            {
                ActionWindow.Instance.ToggleHideWindow();
            }
        }

        if (InputManager.Instance.CloseWindow)
        {
            if (ChatWindow.Instance != null && ChatWindow.Instance.ChatOpened)
            {
                ChatWindow.Instance.CloseChat(false);
                return;
            }

            if (_openedWindows != null && _openedWindows.Count > 0)
            {
                _openedWindows[_openedWindows.Count - 1].HideWindow();
            }
            else
            {
                SystemMenuWindow.Instance.ToggleHideWindow();
            }
        }
    }

    public void WindowOpened(L2PopupWindow popupWindow)
    {
        _openedWindows.Add(popupWindow);
    }

    public void WindowClosed(L2PopupWindow popupWindow)
    {
        _openedWindows.Remove(popupWindow);
    }

    public void OnGUI()
    {
        if (_mouseEnabled)
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Confined;
        }
    }
}
