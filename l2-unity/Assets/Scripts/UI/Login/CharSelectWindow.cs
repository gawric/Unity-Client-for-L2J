using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class CharSelectWindow : L2Window {
    private VisualTreeAsset _arrowInputTemplate;
    private ArrowInputManipulator _charNameManipulator;
    private List<CharSelectionInfoPackage> _characters;
    private List<CharSelectInfoPackage> _charactersInterlude;

    private Label _levelLabel;
    private Label _classLabel;
    private Label _hpLabel;
    private Label _mpLabel;
    private Label _expLabel;
    private Label _spLabel;
    private Label _karmaLabel;

    private VisualElement _HPBar;
    private VisualElement _HPBarBG;
    private VisualElement _MPBar;
    private VisualElement _MPBarBG;
    private VisualElement _ExpBarBG;
    private VisualElement _ExpBar;
    private CharSelectInfoPackage _selectChar;

    private static CharSelectWindow _instance;
    public static CharSelectWindow Instance { get { return _instance; } }

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

    private void Update() {
        if (!_isWindowHidden) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                AudioManager.Instance.PlayUISound("click_01");
                ReLoginPressed();
            } else if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)) {
                AudioManager.Instance.PlayUISound("click_01");
                StartGamePressed();
            }
        }
    }

    protected override void LoadAssets() {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Login/CharSelectWindow"); 
        _arrowInputTemplate = LoadAsset("Data/UI/_Elements/Template/ArrowInput");
    }

    protected override IEnumerator BuildWindow(VisualElement root) {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        Button loginButton = (Button) GetElementById("StartButton");
        loginButton.AddManipulator(new ButtonClickSoundManipulator(loginButton));
        loginButton.RegisterCallback<ClickEvent>(evt => StartGamePressed());

        Button deleteButton = (Button)GetElementById("DeleteButton");
        deleteButton.AddManipulator(new ButtonClickSoundManipulator(deleteButton));
        deleteButton.RegisterCallback<ClickEvent>(evt => DeletePressed());

        Button createButton = (Button)GetElementById("CreateButton");
        createButton.AddManipulator(new ButtonClickSoundManipulator(createButton));
        createButton.RegisterCallback<ClickEvent>(evt => CreatePressed());

        Button reloginButton = (Button)GetElementById("ReloginButton");
        reloginButton.AddManipulator(new ButtonClickSoundManipulator(reloginButton));
        reloginButton.RegisterCallback<ClickEvent>(evt => ReLoginPressed());

        VisualElement userNameInputContainer = GetElementById("UserSelectContainer");
        VisualElement userNameInput = _arrowInputTemplate.Instantiate()[0];
        _charNameManipulator = new ArrowInputManipulator(userNameInput, "Name", new string[] { }, -1, (index, value) => {
            CharacterSelector.Instance.SelectInterludeCharacter(index);
        });

        _levelLabel = (Label)GetElementById("LevelLabel");
        _classLabel = (Label)GetElementById("ClassLabel");
        _hpLabel = (Label)GetElementById("HPText");
        _mpLabel = (Label)GetElementById("MPText");
        _expLabel = (Label)GetElementById("ExpText");
        _spLabel = (Label)GetElementById("SPLabel");
        _karmaLabel = (Label)GetElementById("KarmaLabel");

        _HPBar = GetElementById("HPBar");
        _HPBarBG = GetElementById("HPBarBG");
        _MPBarBG = GetElementById("MPBarBG");
        _MPBar = GetElementById("MPBar");
        _ExpBar = GetElementById("ExpBar");
        _ExpBarBG = GetElementById("ExpBarBG");

        userNameInput.AddManipulator(_charNameManipulator);
        userNameInputContainer.Add(userNameInput);
        SetEmptyData();
    }

    public void SetCharacterList(List<CharSelectionInfoPackage> characters) {
        _characters = characters;
        string[] charArray = characters.Select(character => character.Name).ToArray();
        _charNameManipulator.UpdateValues(charArray);
    }

    public void SetInterludeCharacterList(List<CharSelectInfoPackage> characters)
    {
        _charactersInterlude = characters;
        string[] charArray = characters.Select(character => character.Name).ToArray();
        _charNameManipulator.UpdateValues(charArray);
    }

    public void SelectSlot(int slot) {
        if (slot == _charNameManipulator.Index) {
            return;
        }

        if (slot == -1) {
            _levelLabel.text = "0";
            _classLabel.text = "??";
            _hpLabel.text = "0/0";
            _mpLabel.text = "0/0";
            _expLabel.text = "0.00%";
            _spLabel.text = "0";
            _karmaLabel.text = "0";
            _charNameManipulator.ClearInput();
        } else if(slot < _charactersInterlude.Count) {
            _levelLabel.text = _charactersInterlude[slot].Lvl.ToString();
            _classLabel.text =  ((CharacterClass)(_characters[slot].ClassId)).ToString();
            _hpLabel.text = $"{_charactersInterlude[slot].Hp}/{_charactersInterlude[slot].MaxHp}";
            _mpLabel.text = $"{_charactersInterlude[slot].Mp}/{_charactersInterlude[slot].MaxMp}";
            _expLabel.text = _charactersInterlude[slot].ExpProcent.ToString("0.00") + "%";
            _spLabel.text = _charactersInterlude[slot].Sp.ToString();
            _karmaLabel.text = _charactersInterlude[slot].Karma.ToString();

            StartCoroutine(UpdateBarsInterlude(slot));

            _charNameManipulator.SelectIndex(slot);
        }
    }

    private void SetEmptyData()
    {
        _levelLabel.text = "0";
        _classLabel.text = "";
        _hpLabel.text = "0/0";
        _mpLabel.text = "0/0";
        _expLabel.text = "0.00%";
        _spLabel.text = "0";
        _karmaLabel.text = "0";
        _charNameManipulator.ClearInput();
    }

    public void SelectInterludeSlot(int slot)
    {
        
        if (slot == _charNameManipulator.Index)
        {
            return;
        }
        
        if (slot == -1)
        {
            _levelLabel.text = "0";
            _classLabel.text = "??";
            _hpLabel.text = "0/0";
            _mpLabel.text = "0/0";
            _expLabel.text = "0.00%";
            _spLabel.text = "0";
            _karmaLabel.text = "0";
            _charNameManipulator.ClearInput();
        }
        else if (slot < _charactersInterlude.Count)
        {
            _selectChar = _charactersInterlude[slot];
            int s = _charactersInterlude[slot].Lvl;
            int classId = _charactersInterlude[slot].ClassId;
            _levelLabel.text = _charactersInterlude[slot].Lvl.ToString();
            _classLabel.text = ((CharacterClass)(_charactersInterlude[slot].ClassId)).ToString();
            _hpLabel.text = $"{_charactersInterlude[slot].Hp}/{_charactersInterlude[slot].MaxHp}";
            _mpLabel.text = $"{_charactersInterlude[slot].Mp}/{_charactersInterlude[slot].MaxMp}";
            _expLabel.text = _charactersInterlude[slot].ExpProcent.ToString("0.00") + "%";
            _spLabel.text = _charactersInterlude[slot].Sp.ToString();
            _karmaLabel.text = _charactersInterlude[slot].Karma.ToString();

            StartCoroutine(UpdateBarsInterlude(slot));

            _charNameManipulator.SelectIndex(slot);
        }
    }


   // public void SelectInterludeSlot(int slot)
   // {
       // if (slot == _charNameManipulator.Index)
       // {
        //    return;
        //}

       // if (slot == -1)
       // {
           // _levelLabel.text = "0";
           // _classLabel.text = "??";
           // _hpLabel.text = "0/0";
           // _mpLabel.text = "0/0";
         //   _expLabel.text = "0.00%";
          // //// _spLabel.text = "0";
           // _karmaLabel.text = "0";
            //_charNameManipulator.ClearInput();
       // }
      //  else if (slot < _characters.Count)
      //  {
           // _levelLabel.text = _characters[slot].PlayerStats.Level.ToString();
           // _classLabel.text = ((CharacterClass)(_characters[slot].ClassId)).ToString();
           // _hpLabel.text = $"{_characters[slot].PlayerStatus.Hp}/{_characters[slot].PlayerStats.MaxHp}";
           // _mpLabel.text = $"{_characters[slot].PlayerStatus.Mp}/{_characters[slot].PlayerStats.MaxMp}";
           // _expLabel.text = _characters[slot].ExpPercent.ToString("0.00") + "%";
          //  _spLabel.text = _characters[slot].Sp.ToString();
          //  _karmaLabel.text = _characters[slot].Karma.ToString();

         //   StartCoroutine(UpdateBars(slot));

        //    _charNameManipulator.SelectIndex(slot);
       // }
    //}

  

    IEnumerator UpdateBarsInterlude(int slot)
    {

        yield return new WaitForEndOfFrame();
        if (_HPBarBG != null && _HPBar != null)
        {
            float hpRatio = (float)_charactersInterlude[slot].Hp / (float) _charactersInterlude[slot].MaxHp;
            float bgWidth = _HPBarBG.resolvedStyle.width;
            float barWidth = bgWidth * hpRatio;
            _HPBar.style.width = barWidth;
        }

        if (_MPBarBG != null && _MPBar != null)
        {
            float mpRatio = (float)_charactersInterlude[slot].Mp / (float) _charactersInterlude[slot].MaxMp;
            float bgWidth = _MPBarBG.resolvedStyle.width;
            float barWidth = bgWidth * mpRatio;
            _MPBar.style.width = barWidth;
        }

        if (_ExpBarBG != null && _ExpBar != null)
        {
            float bgWidth = _ExpBarBG.resolvedStyle.width;
            //  float procent = (float)_charactersInterlude[slot].ExpProcent;
            float expRatio = (float)_charactersInterlude[slot].Exp / (float)_charactersInterlude[slot].MaxExp();
            float barWidth = bgWidth * expRatio;
            _ExpBar.style.width = barWidth;
        }
    }

    public CharSelectInfoPackage GetSelectChar()
    {
        return _selectChar;
    }
    private void StartGamePressed() {
        CharacterSelector.Instance.ConfirmSelection();
        
    }

    private void ReLoginPressed() {
        GameManager.Instance.OnRelogin();
        GameClient.Instance.Disconnect();
    }

    private void CreatePressed() {
        // GameManager.Instance.OnCreateUser();
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(CreatorPacketsGameLobby.CreateNewCharacter(), enable, enable);
    }

    private void DeletePressed() {

    }
}