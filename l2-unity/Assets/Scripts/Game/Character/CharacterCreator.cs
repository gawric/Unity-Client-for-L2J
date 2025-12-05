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
        List<Logongrp> pawnData = LogongrpTable.Instance.Logongrps;

        _pawnContainer = new GameObject("Pawns");

        for (var i = 8; i < pawnData.Count; i++) {
             GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FDarkElf, new PlayerAppearance());
             pawns[i] = pawnObject;
             PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
        }
    }

    public void SpawnAllCharCreatePawns()
    {
        List<Logongrp> pawnData = LogongrpTable.Instance.Logongrps;

        _pawnContainer = new GameObject("Pawns");

        for (var i = 8; i < pawnData.Count; i++)
        {
            //MFigther
            if (i == 8)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.MFighter, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }else if (i == 9)
                //FFigther
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FFighter, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 10)
            //MMagic
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.MMagic, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 11)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FMagic, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 12)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.MElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 13)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 14)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.MElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 15)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 16)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.MDarkElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 18)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.MDarkElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 19)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FDarkElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 24)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.MDwarf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else if (i == 25)
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FDwarf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
                //SetSpeed(pawnObject);
            }
            else
            {
                GameObject pawnObject = CreatePawn(CharacterRaceAnimation.FDarkElf, new PlayerAppearance());
                pawns[i] = pawnObject;
                PlacePawn(pawnObject, pawnData[i], "Pawn" + i, _pawnContainer);
            }

        }
    }

    public GameObject SpawnPawnWithAppearance(CharacterRaceAnimation race_id , int id , PlayerAppearance appearance)
    {
        List<Logongrp> pawnData = LogongrpTable.Instance.Logongrps;

        GameObject pawnObject = CreatePawn(race_id, appearance);

        // pawnObject.SetActive(false);
        PlacePawn(pawnObject, pawnData[id], "Pawn" + id, _pawnContainer);
        pawnObject.SetActive(false);
        return pawnObject;
    }

    public void SpawnPawnWithId(int id) {
        List<Logongrp> pawnData = LogongrpTable.Instance.Logongrps;

        GameObject pawnObject = CreatePawn(GetRaceAnimator(id), new PlayerAppearance());

        PlacePawn(pawnObject, pawnData[id], "Pawn" + id, _pawnContainer);
    }

    public CharacterRaceAnimation GetRaceAnimator(int id)
    {
        if (id == 8)
        {
            return CharacterRaceAnimation.MFighter;
        }
        else if (id == 9)
        {
            return CharacterRaceAnimation.FFighter;
        }
        else if (id == 10)
        {
            return CharacterRaceAnimation.MMagic;
        }
        else if (id == 11)
        {
            return CharacterRaceAnimation.FMagic;
        }
        else if (id == 12)
        {
            return CharacterRaceAnimation.MElf;
        }
        else if (id == 13)
        {
            return CharacterRaceAnimation.FElf;
        }
        else if (id == 14)
        {
            return CharacterRaceAnimation.MElf;
        }
        else if (id == 15)
        {
            return CharacterRaceAnimation.FElf;
        }
        else if (id == 16)
        {
            return CharacterRaceAnimation.MDarkElf;
        }
        else if (id == 18)
        {
            return CharacterRaceAnimation.MDarkElf;
        }
        else if (id == 19)
        {
            return CharacterRaceAnimation.FDarkElf;
        }
        else if (id == 24)
        {
            return CharacterRaceAnimation.MDwarf;
        }
        else if (id == 25)
        {

            return CharacterRaceAnimation.FDwarf;
        }
        else
        {
            return CharacterRaceAnimation.FDarkElf;
        }
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


}
