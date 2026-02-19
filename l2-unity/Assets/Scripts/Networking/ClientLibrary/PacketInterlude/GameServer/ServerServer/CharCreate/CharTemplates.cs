using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharTemplates : ServerPacket
{
    private List<PlayerTemplates> _listTemplates;

    public List<PlayerTemplates> PlayerTemplates { get { return _listTemplates; } }
    public CharTemplates(byte[] d) : base(d)
    {
        _listTemplates = new List<PlayerTemplates>();
        Parse();
    }

    public override void Parse()
    {
       int size =  ReadI();
        for(int i = 0; i < size; i++)
        {
            PlayerTemplates _playerTemplates = new PlayerTemplates();
            _playerTemplates.Race = ReadI();
            _playerTemplates.SetClassId(ReadI());
            int empty1 = ReadI(); //0x46
            _playerTemplates.Base_str = ReadI();
            int empty2 = ReadI(); //0x0A
            int empty3 = ReadI(); //0x46
            _playerTemplates.Base_dex = ReadI();
            int empty4 = ReadI(); //0x0A
            int empty5 = ReadI(); //0x46
            _playerTemplates.Base_con = ReadI();
            int empty6 = ReadI(); //0x0A
            int empty7 = ReadI(); //0x46
            _playerTemplates.Base_int = ReadI();
            int empty8 = ReadI(); //0x0A
            int empty9 = ReadI(); //0x46
            _playerTemplates.Base_wit = ReadI();
            int empty10 = ReadI(); //0x0A
            int empty11 = ReadI(); //0x46
            _playerTemplates.Base_men = ReadI();
            int empty12 = ReadI(); //0x0A
            _listTemplates.Add(_playerTemplates);
        }
        
    }
}
