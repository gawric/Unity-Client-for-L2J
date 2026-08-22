using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StorageNpc
{
    private static StorageNpc instance;
    public static Dictionary<int , NpcInfoDto> npcs;
    public static Dictionary<int, UserInfoDto> users;
    public static Dictionary<int, CharInfoDto> chars;
    private object _sync = new object();
    private StorageNpc()
    { }

    public static StorageNpc getInstance()
    {
        if (instance == null)
        {
            instance = new StorageNpc();
            npcs = new Dictionary<int, NpcInfoDto>();
            users = new Dictionary<int, UserInfoDto>();
            chars = new Dictionary<int, CharInfoDto>();
        }
            
        return instance;
    }

    public void AddNpcInfo(NpcInfoDto npc)
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

    public void AddCharInfo(CharInfoDto info)
    {
        if (info == null || info.Identity == null)
            return;
        lock (_sync)
        {
            if (chars == null)
                chars = new Dictionary<int, CharInfoDto>();
            chars[info.Identity.Id] = info;
        }
    }

    public NpcInfoDto[] CopyNpcs()
    {
        lock (_sync)
        {
            if (npcs == null || npcs.Count == 0)
                return new NpcInfoDto[0];
            NpcInfoDto[] copy = new NpcInfoDto[npcs.Count];
            npcs.Values.CopyTo(copy, 0);
            return copy;
        }
    }

    public CharInfoDto[] CopyChars()
    {
        lock (_sync)
        {
            if (chars == null || chars.Count == 0)
                return new CharInfoDto[0];
            CharInfoDto[] copy = new CharInfoDto[chars.Count];
            chars.Values.CopyTo(copy, 0);
            return copy;
        }
    }

    public void AddUserInfo(UserInfoDto user)
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
    public UserInfoDto GetFirstUser()
    {
        if(users.Count > 0)
        {
            return users.Values.First();
        }
        return null;
    }
    public NpcInfoDto GetNpcInfo(int objId)
    {
        //lock (_sync)
        //{
            return (npcs.ContainsKey(objId)) ? npcs[objId] : null;
        //}
    }

    public UserInfoDto GetUserInfo(int objId)
    {
        //lock (_sync)
        //{
            return (users.ContainsKey(objId)) ? users[objId] : null;
        ////}
    }
}
