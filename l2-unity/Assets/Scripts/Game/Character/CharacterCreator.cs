using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class CharacterCreator : MonoBehaviour
{
    [SerializeField] private GameObject[] pawns = new GameObject[26];
    [SerializeField] private int currentPawnIndex = -1;
    [SerializeField] private GameObject currentPawn = null;

    [Inject] private ModelTable _models;
    [Inject] private CharacterBuilder _characterBuilder;
    [Inject] private LogongrpTable _logonGrps;

    private ModelTable Models
    {
        get { return _models != null ? _models : ModelTable.Instance; }
    }

    private CharacterBuilder Builder
    {
        get { return _characterBuilder != null ? _characterBuilder : CharacterBuilder.Instance; }
    }

    private List<Logongrp> LogonGrps
    {
        get
        {
            LogongrpTable table = _logonGrps != null ? _logonGrps : LogongrpTable.Instance;
            return table.LogonGrps;
        }
    }

    private bool _pawnRotating = false;
    private bool _pawnRotatingRight = true;

    private GameObject _pawnContainer;
    public int PawnIndex { get { return currentPawnIndex; } }

    private static CharacterCreator _instance;
    public static CharacterCreator Instance { get { return _instance; } }

    void Awake()
    {
        if (_instance == null) {
            _instance = this;
        } else if(_instance != this) {
            Destroy(gameObject);
        }
    }

    private void Update() {
        if (currentPawn == null) {
            _pawnRotating = false;
            return;
        }

        if(_pawnRotating) {
            if (_pawnRotatingRight) {
                currentPawn.transform.eulerAngles = new Vector3(0, currentPawn.transform.eulerAngles.y + Time.deltaTime * 69f, 0);
            } else {
                currentPawn.transform.eulerAngles = new Vector3(0, currentPawn.transform.eulerAngles.y - Time.deltaTime * 69f, 0);
            }
        }
    }

    public void SpawnAllPawns() {
        List<Logongrp> pawnData = LogonGrps;

        _pawnContainer = new GameObject("Pawns");

        for (var i = 8; i < pawnData.Count; i++) {
             GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FDarkElf, new PlayerAppearance());
             pawns[i] = pawnObject;
             PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
        }
    }

    public void SpawnAllCharCreatePawns()
    {
        _pawnContainer = new GameObject("Pawns");

        List<Logongrp> pawnData = LogonGrps;
        for (var i = 8; i < pawnData.Count; i++)
        {
            Logongrp logonGrp = pawnData[i];
            GameObject pawnObject = CreatePawn(logonGrp.RaceId, new PlayerAppearance());
            if (pawnObject == null) {
                pawnObject = FallbackPawn();
            }
            
            pawns[i] = pawnObject;
            PlacePawn(pawnObject, logonGrp, "Pawn" + i, _pawnContainer);
        }
    }

    public GameObject SpawnPawnWithAppearance(CharacterRaceAnimation raceId , int id , PlayerAppearance appearance) {
        List<Logongrp> pawnData = LogonGrps;

        GameObject pawnObject = CreatePawn(raceId, appearance);
        PlacePawn(pawnObject, pawnData[id], "Pawn" + id, _pawnContainer);
        pawnObject.SetActive(false);
        
        return pawnObject;
    }

    public void SpawnPawnWithId(int id) {
        Logongrp pawnData = LogonGrps[id];
        GameObject pawnObject = CreatePawn(pawnData.RaceId, new PlayerAppearance());
        if (pawnObject == null) {
            pawnObject = FallbackPawn();
        }

        PlacePawn(pawnObject, pawnData, "Pawn" + id, _pawnContainer);
    }

    public CharacterRaceAnimation GetRaceAnimator(int id) {
        return LogonGrps[id].RaceId;
    }

    public void SelectPawn(string race, string pawnClass, string gender) {
        int index = 0;
        switch(race) {
            case "Human":
                index = 8;
                break;
            case "Elf":
                index = 12;
                break;
            case "Dark Elf":
                index = 16;
                break;
            case "Orc":
                index = 20;
                break;
            case "Dwarf":
                index = 24;
                break;
        }

        if(pawnClass == "Mystic") {
            index += 2;
        }

        if(gender == "Female") {
            index += 1;
        }

        currentPawnIndex = index;
        currentPawn = pawns[index];
    }

    public void ResetPawnSelection() {
        if (currentPawn != null) {
            // Restore pawn appearance and rotation
            Destroy(currentPawn);

            SpawnPawnWithId(currentPawnIndex);
        }

        currentPawn = null;
        currentPawnIndex = -1;
    }
    
    public GameObject CreatePawn(CharacterRaceAnimation raceId, PlayerAppearance appearance) {
        GameObject pawnObject = Builder.BuildCharacterBase(raceId, appearance, EntityType.Pawn);
        if (pawnObject == null) {
            // m0nster: временная заглушка, пока не реализованы все персонажи
            return null;
        }

        UserGear gear = pawnObject.GetComponent<UserGear>();

        gear.Initialize(-1, raceId);
        gear.SyncEquippedArmor(appearance);

        if (appearance.LHand != 0) {
            gear.EquipWeapon(appearance.LHand, true);
        }
        if (appearance.RHand != 0) {
            gear.EquipWeapon(appearance.RHand, false);
        }

        return pawnObject;
    }

    public GameObject CreatePawnInterlude(CharacterRaceAnimation raceId, PlayerAppearance appearance)
    {
        GameObject pawnObject = Builder.BuildCharacterBaseInterlude(raceId, appearance, EntityType.Pawn);
        if (pawnObject == null) {
            // m0nster: временная заглушка, пока не реализованы все персонажи
            return null;
        }
        
        UserGear gear = pawnObject.GetComponent<UserGear>();
        gear.Initialize(-1, raceId);
        CharacterDefaultEquipment.EquipStarterGear(gear, appearance);
        
        return pawnObject;
    }
    
    public void PlacePawn(GameObject pawnObject, Logongrp pawnData, string name, GameObject container) {
        UpdatePawnPosAndRot(pawnObject, pawnData);
        pawnObject.transform.name = name;
        pawnObject.transform.parent = container.transform;
        pawnObject.SetActive(true);

        UserGear gear = pawnObject.GetComponent<UserGear>();
        BaseAnimationController animController = pawnObject.GetComponent<BaseAnimationController>();
        animController.Initialize();
        // Unarmed starter → wait_hand; armed → wait_1HS / wait_bow / …
        string waitState = AnimationNames.WAIT.ToString() + gear.WeaponAnim;
        IncomingPacketActions.Animations.PlayLobbyLocomotion(animController, waitState);
    }

    public void UpdatePawnPosAndRot(GameObject pawnObject, Logongrp pawnData) {
        Vector3 pawnPosition = new Vector3(pawnData.X, pawnData.Y, pawnData.Z);
        pawnPosition = VectorUtils.ConvertPosToUnity(pawnPosition);
        pawnObject.transform.position = pawnPosition;
        pawnObject.transform.eulerAngles = new Vector3(0, 360.00f * pawnData.Yaw / 65536, 0);
    }

    public void RotatePawn(bool right) {
        _pawnRotating = true;
        _pawnRotatingRight = right;
    }

    public void StopRotatingPawn() {
        _pawnRotating = false;
    }

    public void ReBuildFace(CharacterRaceAnimation raceId, byte _face)
    {
        GameObject pawnObject = pawns[currentPawnIndex];
        if (pawnObject != null)
        {
            UserGear gear = pawnObject.GetComponent<UserGear>();
            GameObject face = Instantiate(Models.GetFace(raceId, _face));
            gear.EquipFace(face);
        }
    }

    public void ReBuildHair(CharacterRaceAnimation raceId, byte hairColor, byte hairStyle)
    {
        GameObject pawnObject = pawns[currentPawnIndex];

        if (pawnObject != null)
        {
            var hair1M = Models.GetHair(raceId, hairStyle, hairColor, false);
            var hair2M = Models.GetHair(raceId, hairStyle, hairColor, true);
            if(hair1M != null & hair2M != null)
            {
                GameObject hair1 = Instantiate(hair1M);
                GameObject hair2 = Instantiate(hair2M);
                UserGear gear = pawnObject.GetComponent<UserGear>();
                //GameObject face = Instantiate(ModelTable.Instance.GetFace(raceId, hair1));
                // gear.EquipHair1(hair1);
                gear.EquipHair(hair1, hair2);
            }

        }
    }

    public GameObject FallbackPawn() {
        return CreatePawn(CharacterRaceAnimation.FFighter, new PlayerAppearance());
    }


}
