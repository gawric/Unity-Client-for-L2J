using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PackageToListDto : IWireDto
{

    private  Dictionary<string , int> _players;

    public Dictionary<string, int> Players { get => _players; }
    public List<string> GetListName()
    {
        if(_players == null) return new List<string>();

        return _players.Keys.ToList();
    }

    public PackageToListDto()
    {
        _players = new Dictionary<string, int>();
    }

    public void ReadFrom(PacketReader reader)
    {
        int size = reader.ReadI();

        for(int i =0; i < size; i++)
        {
            int objectId = reader.ReadI();
            string name = reader.ReadOtherS();

            if (!_players.ContainsKey(name))
            {
                _players.Add(name , objectId);
            }
        }

    }
}
