using System.Collections.Generic;
using UnityEngine;

public class GravityNpc : MonoBehaviour, IGravity
{

    private Dictionary<int, GravityData> _dict;
    public List<int> _remove;


    private Vector3 _direction;
    private float _speed;
    private float _gravity = 28f;
    private float _moveSpeedMultiplier = 1f;


    private static IGravity _instance;
    public static IGravity Instance { get { return _instance; } }





    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _dict = new Dictionary<int, GravityData>();
            _remove = new List<int>();
            _direction = Vector3.zero;
        }
        else
        {
            Destroy(this);
        }
    }


    private void FixedUpdate()
    {
        if (_dict.Count == 0) return;
        //Debug.Log("GravityData size " + _dict.Count);
        foreach (KeyValuePair<int, GravityData> kvp in _dict)
        {
            GravityData data = kvp.Value;
            CharacterController controller = data.GetControllerToTypeEntity();

            if (!data.IsDead() && controller != null)
            {
                Vector3 ajustedDirection = _direction * _speed * _moveSpeedMultiplier + Vector3.down * _gravity;
                controller.Move(ajustedDirection * Time.deltaTime);
            }
        }

        Delete(_dict, _remove);
        _remove.Clear();
    }

    public void AddGravity(int id, GravityData data)
    {
        if (!_dict.ContainsKey(id))
        {
            _dict.Add(id, data);
        }
    }

    private void Delete(Dictionary<int, GravityData> dict , List<int> remove)
    {
        if(remove.Count > 0)
        {
            foreach (int id in remove)
            {
                if (dict.ContainsKey(id)) dict.Remove(id);
            }
        }
    }

    public void DeleteGravity(int id)
    {
        _remove.Add(id);
    }


    //public void Sync()
    //{
    //    _isSync = true;
    //}

}
