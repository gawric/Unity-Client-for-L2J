using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharTemplatesDto : IWireDto
{
    private List<PlayerTemplates> _listTemplates;

    public List<PlayerTemplates> PlayerTemplates { get { return _listTemplates; } }
    public CharTemplatesDto()
    {
        _listTemplates = new List<PlayerTemplates>();
    }

    public void ReadFrom(PacketReader reader)
    {
       int size =  reader.ReadI();
        for(int i = 0; i < size; i++)
        {
            PlayerTemplates _playerTemplates = new PlayerTemplates();
            _playerTemplates.Race = reader.ReadI();
            _playerTemplates.SetClassId(reader.ReadI());
            int empty1 = reader.ReadI(); //0x46
            _playerTemplates.Base_str = reader.ReadI();
            int empty2 = reader.ReadI(); //0x0A
            int empty3 = reader.ReadI(); //0x46
            _playerTemplates.Base_dex = reader.ReadI();
            int empty4 = reader.ReadI(); //0x0A
            int empty5 = reader.ReadI(); //0x46
            _playerTemplates.Base_con = reader.ReadI();
            int empty6 = reader.ReadI(); //0x0A
            int empty7 = reader.ReadI(); //0x46
            _playerTemplates.Base_int = reader.ReadI();
            int empty8 = reader.ReadI(); //0x0A
            int empty9 = reader.ReadI(); //0x46
            _playerTemplates.Base_wit = reader.ReadI();
            int empty10 = reader.ReadI(); //0x0A
            int empty11 = reader.ReadI(); //0x46
            _playerTemplates.Base_men = reader.ReadI();
            int empty12 = reader.ReadI(); //0x0A
            _listTemplates.Add(_playerTemplates);
        }
        
    }
}
