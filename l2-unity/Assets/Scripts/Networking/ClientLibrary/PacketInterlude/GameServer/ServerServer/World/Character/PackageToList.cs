using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PackageToList : ServerPacket
{

    private  Dictionary<string , int> _players;

    public Dictionary<string, int> Players { get => _players; }
    public List<string> GetListName()
    {
        if(_players == null) return new List<string>();

        return _players.Keys.ToList();
    }

    public PackageToList(byte[] d) : base(d)
    {
        _players = new Dictionary<string, int>();

        Parse();
    }

    public override void Parse()
    {
        int size = ReadI();

        for(int i =0; i < size; i++)
        {
            int objectId = ReadI();
            string name = ReadOtherS();

            if (!_players.ContainsKey(name))
            {
                _players.Add(name , objectId);
            }
        }

    }
}
