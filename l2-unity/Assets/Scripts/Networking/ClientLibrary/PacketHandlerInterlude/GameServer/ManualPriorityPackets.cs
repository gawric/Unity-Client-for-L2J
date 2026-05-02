using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ManualPriorityPackets
{
    private static ManualPriorityPackets instance;

    private static List<ServerPacket> listPackets;

    private ManualPriorityPackets()
    { }

    public static ManualPriorityPackets getInstance()
    {
        if (instance == null)
        {
            instance = new ManualPriorityPackets();
            listPackets = new List<ServerPacket>();
        }

        return instance;
    }

    public bool IsClosePacketArrived { get; set; }

    public void AddPacketsDelay(ServerPacket packet)
    {
        listPackets.Add(packet);
    }

   
    public void UseDelayPackets()
    {

    }



}
