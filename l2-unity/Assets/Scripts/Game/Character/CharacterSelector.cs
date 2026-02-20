using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

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

    public Camera Camera { get { return _charSelectCamera; } set { _charSelectCamera = value; } }
    public int SelectedSlot { get { return _selectedCharacterSlot; } }


    private static CharacterSelector _instance;
    public static CharacterSelector Instance { get { return _instance; } }

    void Awake() {
        if (_instance == null) {
            _instance = this;
            dict = new Dictionary<int, float>();
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
        _pawnData = LogongrpTable.Instance.LogonGrps;
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
        _pawnData = LogongrpTable.Instance.LogonGrps;
        _characterGameObjects = new List<GameObject>();
        _selectedCharacterSlot = -1;

        for (int i = 0; i < characters.Count; i++)
        {
            SpawnInterludeCharacterSlot(i);
        }
    }

   

    public void SpawnInterludeCharacterSlot(int id)
    {
        GameObject pawnObject = CharacterCreator.Instance.CreatePawnInterlude(_charactersInterlude[id].CharacterRaceAnimation, _charactersInterlude[id].Appreance);
        pawnObject.GetComponent<SelectableCharacterEntity>().CharacterInfoInterlude = _charactersInterlude[id];
        pawnObject.GetComponent<SelectableCharacterEntity>().WeaponAnim = pawnObject.GetComponent<UserGear>().WeaponAnim;
        string name = _charactersInterlude[id].Name;
        CharacterCreator.Instance.PlacePawn(pawnObject, _pawnData[id], name, _container);
        _characterGameObjects.Add(pawnObject);
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
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(CreatorPacketsGameLobby.CharacterSelect(SelectedSlot), enable, enable);
       // GameClient.Instance.ClientPacketHandler.SendRequestSelectCharacter(SelectedSlot);
    }

    public void TryToDeleteCharacter()
    {
          if (SelectedSlot == -1)
          {
                Debug.LogWarning("Please select a character");
                return;
            }


            bool enable = GameClient.Instance.IsCryptEnabled();
            SendGameDataQueue.Instance().AddItem(CreatorPacketsGameLobby.RequestCharacterDelete(SelectedSlot), enable, enable);
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
