using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterInfoWindow : L2PopupWindow
{
    // player
    private Label _nameLabel;
    private Label _levelLabel;
    private Label _classLabel;

    //bars
    private Label _hpLabel;
    private Label _mpLabel;
    private Label _spLabel;
    private Label _expLabel;
    private Label _weightLabel;
    private Label _cpLabel;
    private VisualElement _hpBar;
    private VisualElement _hpBarBg;
    private VisualElement _mpBar;
    private VisualElement _mpBarBg;
    private VisualElement _cpBar;
    private VisualElement _cpBarBg;
    private VisualElement _weightBar;
    private VisualElement _weightBarBg;
    private VisualElement _expBar;
    private VisualElement _expBarBg;

    //combat
    private Label _patkLabel;
    private Label _pdefLabel;
    private Label _paccLabel;
    private Label _pevaLabel;
    private Label _pcritLabel;
    private Label _patkspdLabel;
    private Label _speedLabel;
    private Label _matkLabel;
    private Label _mdefLabel;
    private Label _maccuracyLabel;
    private Label _mevasionLabel;
    private Label _mcritLabel;
    private Label _castspeedLabel;

    //stats
    private Label _strLabel;
    private Label _intLabel;
    private Label _dexLabel;
    private Label _witLabel;
    private Label _conLabel;
    private Label _menLabel;

    //social
    private Label _repLabel;
    private Label _pvpLabel;
    private Label _recLabel;
    private Label _raidLabel;


    private static CharacterInfoWindow _instance;
    public static CharacterInfoWindow Instance {
        get { return _instance; }
    }
    private void Awake() {
        if (_instance == null) {
            _instance = this;
        } else {
            Destroy(this);
        }
    }

    private void OnDestroy() {
        _instance = null;
    }

    protected override void LoadAssets() {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/CharacterInfoWindow");
    }

    protected override void InitWindow(VisualElement root) {
        base.InitWindow(root);

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
    }
    protected override IEnumerator BuildWindow(VisualElement root) {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        var dragArea = GetElementByClass("drag-area");
        // player
        _nameLabel = GetLabelById("CharacterNameLabel");
        _levelLabel = GetLabelById("LvlLabelName");
        _classLabel = GetLabelById("ClassLabelName");

        //bars
        _hpLabel = GetLabelById("HpLabel");
        _mpLabel = GetLabelById("MpLabel");
        _spLabel = GetLabelById("SpLabel");
        _expLabel = GetLabelById("ExpLabel");
        _weightLabel = GetLabelById("WeightLabel");
        _cpLabel = GetLabelById("CpLabelB");
        _hpBar = GetElementById("HpGauge");
        _hpBarBg = GetElementById("HpBg");
        _mpBar = GetElementById("MpGauge");
        _mpBarBg = GetElementById("MpBg");
        _cpBar = GetElementById("CpGaugeB");
        _cpBarBg = GetElementById("CpBgB");
        _weightBar = GetElementById("WeightGauge");
        _weightBarBg = GetElementById("WeightBg");
        _expBar = GetElementById("ExpGauge");
        _expBarBg = GetElementById("ExpBg");

        //combat
        _patkLabel = GetLabelById("PAtkLabel");
        _pdefLabel = GetLabelById("PDefLabel");
        _paccLabel = GetLabelById("PAccuracyLabel");
        _pevaLabel = GetLabelById("PEvasionLabel");
        _pcritLabel = GetLabelById("PCriticalLabel");
        _patkspdLabel = GetLabelById("PAtkSpdLabel");
        _speedLabel = GetLabelById("PSpeedLabel");
        _matkLabel = GetLabelById("MAtkLabel");
        _mdefLabel = GetLabelById("MDefLabel");
        _maccuracyLabel = GetLabelById("MAccuracyLabel");
        _mevasionLabel = GetLabelById("MEvasionLabel");
        _mcritLabel = GetLabelById("MCriticalLabel");
        _castspeedLabel = GetLabelById("MCastingLabel");

        //stats
        _strLabel = GetLabelById("StrLabel");
        _intLabel = GetLabelById("IntLabel");
        _dexLabel = GetLabelById("DEXLabel");
        _witLabel = GetLabelById("WITLabel");
        _conLabel = GetLabelById("CONLabel");
        _menLabel = GetLabelById("MENLabel");

        //social
        _repLabel = GetLabelById("RepLabel");
        _pvpLabel = GetLabelById("PvpLabel");
        _recLabel = GetLabelById("RecLabel");
        _raidLabel = GetLabelById("RaidLabel");
    }

    public void UpdateValues() {
        PlayerEntity player = PlayerEntity.Instance;

        if (player == null) {
            Debug.LogWarning("Player entity is null, can't update character window");
            return;
        }

       // StartCoroutine(UpdateWithDelay(player));
        StartCoroutine(UpdateWithDelayInterlude(player));
    }


    private IEnumerator UpdateWithDelayInterlude(PlayerEntity player)
    {
        yield return new WaitForEndOfFrame();

        var stats = (PlayerInterludeStats)player.Stats;
        var status = (PlayerStatusInterlude)player.Status;

        UpdatePlayerInterlude(player.IdentityInterlude, stats);

        UpdateStatsInterlude(stats);

        UpdateCombatValuesInterlude(stats);
       // Debug.Log("++++++++ PLAYER UPDATE BARS EVENT FINISH  +++++++++");
        UpdateBarsInterlude(status, stats);

        UpdateSocialInterlude(stats.PvpKills, stats.PkKills);
    }

    private void UpdatePlayerInterlude(NetworkIdentityInterlude identity, PlayerInterludeStats stats)
    {
        _nameLabel.text = identity.Name;
        _classLabel.text = ((CharacterClass)(identity.PlayerClass)).ToString();
        _levelLabel.text = stats.Level.ToString();

    }


    private void UpdateStatsInterlude(PlayerInterludeStats stats)
    {
        _strLabel.text = stats.Str.ToString();
        _intLabel.text = stats.Int.ToString();
        _dexLabel.text = stats.Dex.ToString();
        _witLabel.text = stats.Wit.ToString();
        _conLabel.text = stats.Con.ToString();
        _menLabel.text = stats.Men.ToString();
    }


    private void UpdateCombatValuesInterlude(PlayerInterludeStats stats)
    {
        _patkLabel.text = stats.PAtk.ToString();
        _pdefLabel.text = stats.PDef.ToString();
        _paccLabel.text = stats.PAccuracy.ToString();
        _pevaLabel.text = stats.PEvasion.ToString();
        _pcritLabel.text = stats.PCritical.ToString();
        _patkspdLabel.text = stats.PAtkSpd.ToString();
        _speedLabel.text = stats.Speed.ToString();

        _matkLabel.text = stats.MAtk.ToString();
        _mdefLabel.text = stats.MDef.ToString();
        _maccuracyLabel.text = stats.MAccuracy.ToString();
        _mevasionLabel.text = stats.MEvasion.ToString();
        _mcritLabel.text = stats.MCritical.ToString();
        _castspeedLabel.text = stats.MAtkSpd.ToString();
    }


    private void UpdateSocialInterlude(int pk , int pvp)
    {
        _repLabel.text = "0";
        _pvpLabel.text = pvp + " / "+pk;
        _recLabel.text = "0 / 0";
        _raidLabel.text = "0";
    }

    

    private void UpdateBarsInterlude(PlayerStatusInterlude status, PlayerInterludeStats stats)
    {
        _hpLabel.text = $"{status.GetHp()}/{stats.MaxHp}";
        _mpLabel.text = $"{status.GetMp()}/{stats.MaxMp}";
        _cpLabel.text = $"{status.Cp}/{stats.MaxCp}";
        _spLabel.text = stats.Sp.ToString();

        if (stats.MaxExp > 0)
        {
             float procent = (float)stats.ExpPercent(stats.Level + 1);
            _expLabel.text =  procent + "%";
        }
        else
        {
            _expLabel.text = $"00.00%";
        }

        if (stats.MaxWeight > 0)
        {
            float procent = (float)stats.WeightPercent();
            _weightLabel.text = procent + "%"; 
        }
        else
        {
            _weightLabel.text = $"00.00%";
        }

        if (_hpBarBg != null && _hpBar != null)
        {
            double hpRatio = (float)status.GetHp() / stats.MaxHp;
            // float bgWidth = _hpBarBg.resolvedStyle.width;
            //resolvedStyle.width no calc set static
            float bgWidth = 141;
            double barWidth = bgWidth * hpRatio;
            if (stats.MaxHp == 0)
            {
                barWidth = 0;
            }
            _hpBar.style.width = Convert.ToSingle(barWidth);
        }

        if (_mpBarBg != null && _mpBar != null)
        {
            double mpRatio = (float)status.GetMp() / stats.MaxMp;
            //float bgWidth = _mpBarBg.resolvedStyle.width;
            float bgWidth = 141;
            double barWidth = bgWidth * mpRatio;
            if (stats.MaxMp == 0)
            {
                barWidth = 0;
            }
            _mpBar.style.width = Convert.ToSingle(barWidth);
        }

        if (_cpBarBg != null && _cpBar != null)
        {
            //float bgWidth = _cpBarBg.resolvedStyle.width;
            float bgWidth = 128;
            double cpRatio = (float)status.Cp / stats.MaxCp;
            double barWidth = bgWidth * cpRatio;
            if (stats.MaxCp == 0)
            {
                barWidth = 0;
            }
            _cpBar.style.width = Convert.ToSingle(barWidth);
        }

        if (_expBarBg != null && _expBar != null)
        {
            float bgWidth = 315;
            //float bgWidth = _expBarBg.resolvedStyle.width;
            double expRatio = stats.Exp / stats.MaxExp;
            double barWidth = bgWidth * expRatio;
            if (stats.MaxExp == 0)
            {
                barWidth = 0;
            }
            _expBar.style.width = Convert.ToSingle(barWidth);
        }

        if (_weightBarBg != null && _weightBar != null)
        {
            
            float bgWidth = 128;
            //float bgWidth = _expBarBg.resolvedStyle.width;
            double expRatio = (float)stats.CurrWeight / stats.MaxWeight;
            double barWidth = (double)bgWidth * expRatio;

            if (stats.MaxExp == 0)
            {
                barWidth = 0;
            }
            _weightBar.style.width = Convert.ToSingle(barWidth);
        }
    }
}
