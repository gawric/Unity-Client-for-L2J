using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder;
using static ModelTable;

public class CharacterCreator : MonoBehaviour
{
    [SerializeField] private GameObject[] pawns = new GameObject[26];
    [SerializeField] private int currentPawnIndex = -1;
    [SerializeField] private GameObject currentPawn = null;

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
        List<Logongrp> pawnData = LogongrpTable.Instance.LogonGrps;

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

        List<Logongrp> pawnData = LogongrpTable.Instance.LogonGrps;
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
        List<Logongrp> pawnData = LogongrpTable.Instance.LogonGrps;

        GameObject pawnObject = CreatePawn(raceId, appearance);
        PlacePawn(pawnObject, pawnData[id], "Pawn" + id, _pawnContainer);
        pawnObject.SetActive(false);
        
        return pawnObject;
    }

    public void SpawnPawnWithId(int id) {
        Logongrp pawnData = LogongrpTable.Instance.LogonGrps[id];
        GameObject pawnObject = CreatePawn(pawnData.RaceId, new PlayerAppearance());
        if (pawnObject == null) {
            pawnObject = FallbackPawn();
        }

        PlacePawn(pawnObject, pawnData, "Pawn" + id, _pawnContainer);
    }

    public CharacterRaceAnimation GetRaceAnimator(int id) {
        return LogongrpTable.Instance.LogonGrps[id].RaceId;
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
        GameObject pawnObject = CharacterBuilder.Instance.BuildCharacterBase(raceId, appearance, EntityType.Pawn);
        if (pawnObject == null) {
            // m0nster: временная заглушка, пока не реализованы все персонажи
            return null;
        }

        UserGear gear = pawnObject.GetComponent<UserGear>();

        gear.Initialize(-1, raceId);

        if (appearance.Chest != 0) {
            gear.EquipArmor(appearance.Chest, ItemSlot.chest);
        } else {
            gear.EquipArmor(ItemTable.NAKED_CHEST, ItemSlot.chest);
        }

        if (appearance.Legs != 0) {
            gear.EquipArmor(appearance.Legs, ItemSlot.legs);
        } else {
            gear.EquipArmor(ItemTable.NAKED_LEGS, ItemSlot.legs);
        }

        if (appearance.Gloves != 0) {
            gear.EquipArmor(appearance.Gloves, ItemSlot.gloves);
        } else {
            gear.EquipArmor(ItemTable.NAKED_GLOVES, ItemSlot.gloves);
        }

        if (appearance.Feet != 0) {
            gear.EquipArmor(appearance.Feet, ItemSlot.feet);
        } else {
            gear.EquipArmor(ItemTable.NAKED_BOOTS, ItemSlot.feet);
        }

        if (appearance.LHand != 0) {
            gear.EquipWeapon(appearance.LHand, true);
        }
        if (appearance.RHand != 0) {
            gear.EquipWeapon(appearance.RHand, false);
        }

        return pawnObject;
    }

    public GameObject CreatePawnInterlude(CharacterRaceAnimation raceId, PlayerInterludeAppearance appearance)
    {
        GameObject pawnObject = CharacterBuilder.Instance.BuildCharacterBaseInterlude(raceId, appearance, EntityType.Pawn);
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
        string nameAnim = "wait_" + gear.WeaponAnim;
        animController.SetBool("wait_" + gear.WeaponAnim, true);
        //animController.SetWaitSpeedLobby("wait_" + gear.WeaponAnim, 0.1f);
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
            GameObject face = Instantiate(ModelTable.Instance.GetFace(raceId, _face));
            gear.EquipFace(face);
        }
    }

    public void ReBuildHair(CharacterRaceAnimation raceId, byte hairColor, byte hairStyle)
    {
        GameObject pawnObject = pawns[currentPawnIndex];

        if (pawnObject != null)
        {
            var hair1M = ModelTable.Instance.GetHair(raceId, hairStyle, hairColor, false);
            var hair2M = ModelTable.Instance.GetHair(raceId, hairStyle, hairColor, true);
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
