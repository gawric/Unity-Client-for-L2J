using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public interface IData
{
    byte[] DecodeData();
    ClientPacket GetPacket();
}
