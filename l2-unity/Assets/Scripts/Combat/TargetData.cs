using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class TargetData
{
    [SerializeField] private Status _status;
    [SerializeField] private Stats _stats;
    [SerializeField] private NetworkIdentityInterlude _identity;
    [SerializeField] private ObjectData _data;
    [SerializeField] private float _distance;
    private string hexColor;
    public Status Status { get { return _status; } }
    public Stats Stats { get { return _stats; } }
    public NetworkIdentityInterlude Identity { get { return _identity; } }
    public ObjectData Data { get { return _data; } }
    public float Distance { get { return _distance; } set { _distance = value; } }
    public Entity GetEntity() { return _entity; }

    private Entity _entity;
    public StyleColor GetColorName()
    {
        StyleColor styleColor = new StyleColor();
        Color color;
        ColorUtility.TryParseHtmlString(hexColor, out color);
        styleColor.value = color;
        return styleColor;
    }

    public TargetData(ObjectData target)
    {
        _data = target;

        if(_data != null)
        {
             _entity  = _data.ObjectTransform.GetComponent<Entity>();
             _identity = _entity.IdentityInterlude;
             _status = _entity.Status;
             _stats = _entity.Stats;
        }
    }

    public bool IsDead()
    {
       return _entity.IsDead();
    }

    public void SetColor(string hexColor)
    {
        this.hexColor = hexColor;
    }

    public TargetData() { }
}
