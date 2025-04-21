﻿
using UnityEngine;

[System.Serializable]
public class Npcgrp {
    [SerializeField] private int _npcId;
    [SerializeField] private string _className;
    [SerializeField] private string _mesh;
    [SerializeField] private string[] _materials;
    [SerializeField] private float _speed;
    [SerializeField] private string[] _attackSounds;
    [SerializeField] private string[] _defenseSounds;
    [SerializeField] private string[] _damageSounds;
    [SerializeField] private string _attackEffect;
    [SerializeField] private bool _friendly;
    [SerializeField] private bool _hpVisible;
    [SerializeField] private string[] _dialogSounds;
    [SerializeField] private float _collisionRadius;
    [SerializeField] private float _collisionHeight;
    [SerializeField] private int _rhand;
    [SerializeField] private int _lhand;
    [SerializeField] private float _maxHp;
    [SerializeField] private float _maxMp;
    [SerializeField] private string _npcType;

    public int NpcId { get { return _npcId; } set { _npcId = value; } }
    public string ClassName { get => _className; set => _className = value; }
    public string Mesh { get => _mesh; set => _mesh = value; }
    public string[] Materials { get => _materials; set => _materials = value; }
    public float Speed { get => _speed; set => _speed = value; }
    public string[] AttackSounds { get => _attackSounds; set => _attackSounds = value; }
    public string[] DefenseSounds { get => _defenseSounds; set => _defenseSounds = value; }
    public string[] DamageSounds { get => _damageSounds; set => _damageSounds = value; }
    public string AttackEffect { get => _attackEffect; set => _attackEffect = value; }
    public bool Friendly { get => _friendly; set => _friendly = value; }
    public bool HpVisible { get => _hpVisible; set => _hpVisible = value; }
    public string[] DialogSounds { get => _dialogSounds; set => _dialogSounds = value; }
    public float CollisionRadius { get => _collisionRadius; set => _collisionRadius = value; }
    public float CollisionHeight { get => _collisionHeight; set => _collisionHeight = value; }
    public int Rhand { get => _rhand; set => _rhand = value; }
    public int Lhand { get => _lhand; set => _lhand = value; }
    public float MaxHp { get => _maxHp; set => _maxHp = value; }
    public float MaxMp { get => _maxMp; set => _maxMp = value; }

    public string Type { get => _npcType; set => _npcType = value; }
}