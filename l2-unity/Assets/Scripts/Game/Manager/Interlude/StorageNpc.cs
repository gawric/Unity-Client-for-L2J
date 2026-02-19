using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StorageNpc
{
    private static StorageNpc instance;
    public static Dictionary<int , NpcInfo> npcs;
    public static Dictionary<int, UserInfo> users;
    private object _sync = new object();
    private StorageNpc()
    { }

    public static StorageNpc getInstance()
    {
        if (instance == null)
        {
            instance = new StorageNpc();
            npcs = new Dictionary<int, NpcInfo>();
            users = new Dictionary<int, UserInfo>();
        }
            
        return instance;
    }

    public void AddNpcInfo(NpcInfo npc)
    {
        lock (_sync)
        {
            if (!npcs.ContainsKey(npc.Identity.Id))
            {
                npcs.Add(npc.Identity.Id, npc);
            }
            else
            {
                npcs.Remove(npc.Identity.Id);
                npcs.Add(npc.Identity.Id, npc);
            }
           
        }
    }

    public void AddUserInfo(UserInfo user)
    {
        lock (_sync)
        {
            if (!users.ContainsKey(user.PlayerInfoInterlude.Identity.Id))
            {
                users.Add(user.PlayerInfoInterlude.Identity.Id, user);
            }
            else
            {
                users.Remove(user.PlayerInfoInterlude.Identity.Id);
                users.Add(user.PlayerInfoInterlude.Identity.Id, user);
            }

        }
    }
    public UserInfo GetFirstUser()
    {
        if(users.Count > 0)
        {
            return users.Values.First();
        }
        return null;
    }
    public NpcInfo GetNpcInfo(int objId)
    {
        //lock (_sync)
        //{
            return (npcs.ContainsKey(objId)) ? npcs[objId] : null;
        //}
    }

    public UserInfo GetUserInfo(int objId)
    {
        //lock (_sync)
        //{
            return (users.ContainsKey(objId)) ? users[objId] : null;
        ////}
    }
}
