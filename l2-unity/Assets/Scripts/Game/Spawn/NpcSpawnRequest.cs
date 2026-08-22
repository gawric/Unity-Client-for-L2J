using UnityEngine;

public sealed class NpcSpawnRequest
{
    public EntityIdentity Identity;
    public NpcStatusInterlude Status;
    public Stats Stats;
    public Npcgrp Npcgrp;
    public NpcName NpcName;
    public GameObject Prefab;
}
