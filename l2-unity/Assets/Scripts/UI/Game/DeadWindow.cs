using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class DeadWindow : L2Window
{
    private static DeadWindow _instance;
    public static DeadWindow Instance { get { return _instance; } }

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
        _instance = null;
    }


    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/DeadWindow");
    }


    protected override IEnumerator BuildWindow(VisualElement root)
    {
   

        InitWindow(root);

 
        yield return new WaitForEndOfFrame();

        Button toVillage = _windowEle.Q<Button>("VillageButton");
        
        //default window dead - 152/203
        //_windowEle.style.left = (root.resolvedStyle.width - 152) / 2;
        //float top = (root.resolvedStyle.height - 130) / 2;
        //_windowEle.style.top = top;
        OnCenterScreen(root);
        RegisterClick(toVillage);

    }



    private void RegisterClick(Button toVillage)
    {
        toVillage.RegisterCallback<MouseUpEvent>(evt => {

            RequestRestartPoint sendPaket = CreatorPacketsUser.CreateRestartPoint();
            bool enable = GameClient.Instance.IsCryptEnabled();
            SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);

            HideWindow();
        }, TrickleDown.TrickleDown);
    }


}
