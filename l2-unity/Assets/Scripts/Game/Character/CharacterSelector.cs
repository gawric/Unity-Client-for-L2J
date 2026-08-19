using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class CharacterSelector : MonoBehaviour
{
    [SerializeField] private int _selectedCharacterSlot;
    [SerializeField] private CharSelectionInfoPackage _selectedCharacter;
    [SerializeField] private CharSelectInfoPackage _selectedCharacterInterlude;
    [SerializeField] private List<CharSelectionInfoPackage> _characters;
    [SerializeField] private List<CharSelectInfoPackage> _charactersInterlude;
    [SerializeField] private LayerMask _characterMask;
    [SerializeField] private Camera _charSelectCamera;
    private Dictionary<int, float> dict;
    private GameObject _container;
    private List<Logongrp> _pawnData;
    private List<GameObject> _characterGameObjects;

    [Inject] private LogongrpTable _logonGrps;

    private List<Logongrp> LogonGrps
    {
        get
        {
            LogongrpTable table = _logonGrps != null ? _logonGrps : LogongrpTable.Instance;
            return table.LogonGrps;
        }
    }

    public Camera Camera { get { return _charSelectCamera; } set { _charSelectCamera = value; } }
    public int SelectedSlot { get { return _selectedCharacterSlot; } }

    /// <summary>Spawned lobby pawns (Interlude / legacy select row).</summary>
    public IReadOnlyList<GameObject> CharacterPawns => _characterGameObjects;


    private static CharacterSelector _instance;
    public static CharacterSelector Instance { get { return _instance; } }

    void Awake() {
        if (_instance == null) {
            _instance = this;
            dict = new Dictionary<int, float>();
            App.InjectGameObject(gameObject);
        } else if (_instance != this) {
            Destroy(this);
        }
    }

    public void SetCharacterList(List<CharSelectionInfoPackage> characters) {
        if(_container == null) {
            _container = new GameObject("Characters");
        }

        if (_characterGameObjects != null) {
            _characterGameObjects.ForEach((go) => {
                Destroy(go);
            });
        }

        _characters = characters;
        _pawnData = LogonGrps;
        _characterGameObjects = new List<GameObject>();
        _selectedCharacterSlot = -1;

        for (int i = 0; i < characters.Count; i++) {
            SpawnCharacterSlot(i);
        }
    }

    public void SetCharacterInterludeList(List<CharSelectInfoPackage> characters)
    {
        if (_container == null)
        {
            _container = new GameObject("Characters");
        }

        if (_characterGameObjects != null)
        {
            _characterGameObjects.ForEach((go) => {
                Destroy(go);
            });
        }

        _charactersInterlude = characters;
        _pawnData = LogonGrps;
        _characterGameObjects = new List<GameObject>();
        _selectedCharacterSlot = -1;

        for (int i = 0; i < characters.Count; i++)
        {
            SpawnInterludeCharacterSlot(i);
        }
    }

   

    public void SpawnInterludeCharacterSlot(int id)
    {
        CharSelectInfoPackage info = _charactersInterlude[id];
        GameObject pawnObject = CharacterCreator.Instance.CreatePawnInterlude(info.CharacterRaceAnimation, info.Appreance);
        if (pawnObject == null)
        {
            LobbyFlowLog.Error(
                "lobby pawn null slot=" + id + " name=" + info.Name +
                " raceAnim=" + info.CharacterRaceAnimation + " — skip (would NRE and freeze char select)");
            return;
        }

        pawnObject.GetComponent<SelectableCharacterEntity>().CharacterInfoInterlude = info;
        pawnObject.GetComponent<SelectableCharacterEntity>().WeaponAnim = pawnObject.GetComponent<UserGear>().WeaponAnim;
        string name = info.Name;
        CharacterCreator.Instance.PlacePawn(pawnObject, _pawnData[id], name, _container);
        _characterGameObjects.Add(pawnObject);
        LobbyFlowLog.Info("lobby pawn spawned slot=" + id + " name=" + name);
    }

    Bounds GetMaxBounds(GameObject parent)
    {
        var total = new Bounds(parent.transform.position, Vector3.zero);
        foreach (var child in parent.GetComponentsInChildren<Collider>())
        {
            total.Encapsulate(child.bounds);
        }
        return total;
    }

    public void SpawnCharacterSlot(int id) {
        GameObject pawnObject = CharacterCreator.Instance.CreatePawn(_characters[id].CharacterRaceAnimation, _characters[id].PlayerAppearance);
        pawnObject.GetComponent<SelectableCharacterEntity>().CharacterInfo = _characters[id];
        pawnObject.GetComponent<SelectableCharacterEntity>().WeaponAnim = pawnObject.GetComponent<UserGear>().WeaponAnim;
        CharacterCreator.Instance.PlacePawn(pawnObject, _pawnData[id], _characters[id].Name, _container);
        _characterGameObjects.Add(pawnObject);
    }

    public void SelectCharacter(int slot) {
        if (slot >= 0 && slot < _characters.Count) {
            if (_selectedCharacterSlot == slot) {
                return;
            }

            if(_selectedCharacterSlot != -1) {
                _characterGameObjects[_selectedCharacterSlot].GetComponent<SelectableCharacterEntity>().SetDestination(_pawnData[_selectedCharacterSlot]);
            }

            _characterGameObjects[slot].GetComponent<SelectableCharacterEntity>().SetDestination(_pawnData[7]);

            _selectedCharacterSlot = slot;
            _selectedCharacter = _characters[slot];

            CharSelectWindow.Instance.SelectSlot(slot);
        }
    }

    public void SelectInterludeCharacter(int slot)
    {
        if (slot >= 0 && slot < _charactersInterlude.Count)
        {
            if (_selectedCharacterSlot == slot)
            {
                return;
            }

            if (_selectedCharacterSlot != -1)
            {
                _characterGameObjects[_selectedCharacterSlot].GetComponent<SelectableCharacterEntity>().SetDestination(_pawnData[_selectedCharacterSlot]);
            }

            _characterGameObjects[slot].GetComponent<SelectableCharacterEntity>().SetDestination(_pawnData[7]);

            _selectedCharacterSlot = slot;
            _selectedCharacterInterlude = _charactersInterlude[slot];

            CharSelectWindow.Instance.SelectInterludeSlot(slot);
        }
    }

    public void ConfirmSelection()
     {
        if (SelectedSlot == -1) {
            Debug.LogWarning("Please select a character");
            return;
        }
        IncomingPacketActions.Game.Send(new CharacterSelectCommand(SelectedSlot));
       // GameClient.Instance.ClientPacketHandler.SendRequestSelectCharacter(SelectedSlot);
    }

    public void TryToDeleteCharacter()
    {
          if (SelectedSlot == -1)
          {
                Debug.LogWarning("Please select a character");
                return;
            }
            IncomingPacketActions.Game.Send(new CharacterDeleteCommand(SelectedSlot));
            Debug.LogWarning("Requesting delete character , slot: " + SelectedSlot);
    }


    void Update() {
        if(_charSelectCamera == null) {
            return;
        }


        if(Input.GetMouseButtonDown(0)) {
            Ray ray = _charSelectCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000f)) {
                int hitLayer = hit.collider.gameObject.layer;
                if (_characterMask == (_characterMask | (1 << hitLayer))) {
                    CharSelectInfoPackage hitInfo = hit.transform.parent.GetComponent<SelectableCharacterEntity>().CharacterInfoInterlude;
                    SelectInterludeCharacter(hitInfo.Slot);
                }
            }
        }
    }
}
