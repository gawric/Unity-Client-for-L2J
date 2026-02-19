using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NetworkIdentityInterlude
{
    [SerializeField] private EntityType _entityType;
    [SerializeField] private int _id;
    [SerializeField] private string _name;
    [SerializeField] private string _title;
    [SerializeField] private string _titleColor = "9CE8A9FF"; // default color
    [SerializeField] private int _resetTick = 0; // tick 24h

    [Header("Npc")]
    [SerializeField] private int _npcId;
    [SerializeField] private string _npcClass;

    [Header("Player")]
    [SerializeField] private int _playerClass;
    //[SerializeField] private int _baseClass;

    [Header("Transform")]
    [SerializeField] private Vector3 _position = new Vector3(4724, -68, -1731);
    [SerializeField] private Quaternion _heading;

    private float _origHeading;
    private int _clanId;

    [SerializeField] private bool _owned = false;
    private bool _isHideHpBar = false;
    public bool IsRunning { get; set; }
    public EntityType EntityType { get => _entityType; set => _entityType = value; }
    public int Id { get => _id; set => _id = value; }

    public int ClanId { get => _clanId; set => _clanId = value; }
    public int NpcId { get => _npcId; set => _npcId = value; }
    public string NpcClass { get => _npcClass; set => _npcClass = value; }
    public string Name { get => _name; set => _name = value; }
    public string Title { get => _title; set => _title = value; }
    public string TitleColor { get => _titleColor; set => _titleColor = value; }
    public Vector3 Position { get => _position; set => _position = value; }
    public Quaternion Heading { get => _heading; set => _heading = value; }

    public float OrigHeading { get => _origHeading; set => _origHeading = value; }


    public bool Owned { get => _owned; set => _owned = value; }
    public int PlayerClass { get => _playerClass; set => _playerClass = value; }
    //public int  BaseClass { get => _baseClass; set => _baseClass = value; }
    public int ResetTick { get => _resetTick; set => _resetTick = value; }

    public bool IsHideHpBar { get => _isHideHpBar;  }

    public NetworkIdentityInterlude() { }

    public void SetPosX(float x)
    {
        _position.x = x;
    }

    public void SetPosY(float y)
    {
        _position.y = y;
    }

    public void SetPosZ(float z)
    {
        _position.z = z;
    }

    public void SetL2jPos(float x , float y , float z)
    {
        Vector3 l2jpos = new Vector3(x, y, z);
        _position = VectorUtils.ConvertPosToUnity(l2jpos);
    }

    public Vector3 GetXZPos()
    {
        return _position;
    }

    public void SetHideHp(int npcId)
    {
        _isHideHpBar = ParceHideHpbar(npcId);
    }

    private bool ParceHideHpbar(int npcId)
    {
        Npcgrp npc = NpcgrpTable.Instance.GetNpcgrp(npcId);
        // Debug.Log("TYPE: ++++++++++++++++++++++++>" + npc.Type);
        if (npc != null)
        {
            if (npc.Type.Equals("monster_normal") | npc.Type.Equals("hide"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    public Vector3 GetL2jPos()
    {
        return VectorUtils.ConvertPosUnityToL2j(_position);
    }

}
