using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Appearance {
    [SerializeField] private float _collisonHeight;
    [SerializeField] private float _collisionRadius;
    [SerializeField] private int _lhand;
    [SerializeField] private int _rhand;
    [SerializeField] private int _serverNameColor;
    [SerializeField] private int _serverTitleColor;

    public float CollisionHeight { get { return _collisonHeight; } set { _collisonHeight = value; } }
    public float CollisionRadius { get { return _collisionRadius; } set { _collisionRadius = value; } }
    public float PhisicalAttackRange { get { return _collisionRadius; } }

    public int ServerTitleColor { get => _serverTitleColor; set => _serverTitleColor = value; }
    public int ServerNameColor { get => _serverNameColor; set => _serverNameColor = value; }

    public int LHand { get { return _lhand; } set { _lhand = value; } }
    public int RHand { get { return _rhand; } set { _rhand = value; } }
}
