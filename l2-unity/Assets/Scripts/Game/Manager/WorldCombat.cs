using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using static StatusUpdatePacket;
using static UnityEngine.EventSystems.EventTrigger;

public class WorldCombat : MonoBehaviour {
    [SerializeField] private GameObject _impactParticle;

    private static WorldCombat _instance;
    public static WorldCombat Instance { get { return _instance; } }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
        } else {
            Destroy(this);
        }
    }

    void OnDestroy() {
        _instance = null;
    }


    public void InflictAttack(Transform target, int damage, bool criticalHit) {
        ApplyDamage(target, damage, criticalHit);
    }

    public void InflictAttack(Vector3 hitPoint, Vector3 impactDirection) {
        //ApplyDamage(target, damage, criticalHit);

        // Instantiate hit particle
        ParticleImpact(hitPoint, impactDirection);
    }

    private void ApplyDamage(Transform target, int damage, bool criticalHit) {
        Entity entity = target.GetComponent<Entity>();
        if (entity != null) {
            // Apply damage to target
            entity.ApplyDamage(damage, criticalHit);
        }
    }



    private void ParticleImpact(Vector3 hitPoint, Vector3 impactDirection)
    {
        // Use the actual impact point instead of calculating a new position
        GameObject go = (GameObject)Instantiate(
            _impactParticle,
            hitPoint,  // Use the exact impact point from collision detection
            Quaternion.identity);

        // Use the provided impact direction to orient the particle
        go.transform.rotation = Quaternion.LookRotation(impactDirection);
    }


    public void StatusUpdate(Entity entity,  PlayerInterludeStats pis , PlayerStatusInterlude psi , int id)
    {
        PlayerStatusInterlude status = (PlayerStatusInterlude)entity.Status;
        PlayerInterludeStats stats = (PlayerInterludeStats) entity.Stats;

        stats.Exp = pis.Exp;
        stats.Str = pis.Str;
        stats.Dex = pis.Dex;
        stats.Con = pis.Con;
        stats.Int = pis.Int;
        stats.Wit = pis.Wit;
        stats.Men = pis.Men;


        status.SetHp(psi.GetHp());
        stats.MaxHp = pis.MaxHp;
        status.SetMp(psi.GetMp());
        stats.MaxMp = pis.MaxMp;
        stats.Sp = pis.Sp;

        stats.CurrWeight = pis.CurrWeight;
        stats.MaxWeight = pis.MaxWeight;

        stats.PAtk = pis.PAtk;
        //Debug.Log(" StatusUpdate " + pis.PAtkSpd);
        stats.PAtkSpd = pis.PAtkSpd;
    }
    public void StatusUpdate(Entity entity, List<StatusUpdatePacket.Attribute> attributes , int id) {
       // Debug.Log("Word combat: Status update");
        Status status = entity.Status;
        Stats stats = entity.Stats;

        foreach (StatusUpdatePacket.Attribute attribute in attributes) {
            //Debug.Log("World Combat Update Status UPDATE +++++++ Iteration ID " + attribute.id + " VALUE " + attribute.value + " NPC " + id);
            switch((AttributeType) attribute.id) {
                case AttributeType.LEVEL:
                    stats.Level = attribute.value;
                    break;
                case AttributeType.EXP:
                    ((PlayerInterludeStats) stats).Exp = attribute.value;
                    break;
                case AttributeType.STR:
                    ((PlayerInterludeStats) stats).Str = (byte)attribute.value;
                    break;
                case AttributeType.DEX:
                    ((PlayerInterludeStats) stats).Dex = (byte)attribute.value;
                    break;
                case AttributeType.CON:
                    ((PlayerInterludeStats) stats).Con = (byte)attribute.value;
                    break;
                case AttributeType.INT:
                    ((PlayerInterludeStats) stats).Int = (byte)attribute.value;
                    break;
                case AttributeType.WIT:
                    ((PlayerInterludeStats) stats).Wit = (byte)attribute.value;
                    break;
                case AttributeType.MEN:
                    ((PlayerInterludeStats) stats).Men = (byte)attribute.value;
                    break;
                case AttributeType.CUR_HP:
                    //Debug.Log("World Combat select CUR_HP ID " + attribute.id + " VALUE " + attribute.value + " NPC " + id);

                    status.SetHp(attribute.value);
                    break;
                case AttributeType.MAX_HP:
                    //Debug.Log("World Combat select MAX_HP ID " + attribute.id + " VALUE " + attribute.value + " NPC " + id);
                    stats.MaxHp = attribute.value;
                    break;
                case AttributeType.CUR_MP:
                    status.SetMp(attribute.value);
                    break;
                case AttributeType.MAX_MP:
                    stats.MaxMp = attribute.value;
                    break;
                case AttributeType.SP:
                    ((PlayerInterludeStats) stats).Sp = attribute.value;
                    break;
                case AttributeType.CUR_LOAD:
                    ((PlayerInterludeStats) stats).CurrWeight = attribute.value;
                    break;
                case AttributeType.MAX_LOAD:
                    ((PlayerInterludeStats) stats).MaxWeight = attribute.value;
                    break;
                case AttributeType.P_ATK:
                    ((PlayerInterludeStats) stats).PAtk = attribute.value;
                    break;
                case AttributeType.ATK_SPD:
                    stats.PAtkSpd = attribute.value;
                    break;
                case AttributeType.P_DEF:
                    ((PlayerInterludeStats) stats).PDef = attribute.value;
                    break;
                case AttributeType.P_EVASION:
                    ((PlayerInterludeStats)stats).PEvasion = attribute.value;
                    break;
                case AttributeType.P_ACCURACY:
                    ((PlayerInterludeStats)stats).PAccuracy = attribute.value;
                    break;
                case AttributeType.P_CRITICAL:
                    ((PlayerInterludeStats)stats).PCritical = attribute.value;
                    break;
                case AttributeType.M_EVASION:
                    ((PlayerInterludeStats)stats).MEvasion = attribute.value;
                    break;
                case AttributeType.M_ACCURACY:
                    ((PlayerInterludeStats)stats).MAccuracy = attribute.value;
                    break;
                case AttributeType.M_CRITICAL:
                    ((PlayerInterludeStats)stats).MCritical = attribute.value;
                    break;
                case AttributeType.M_ATK:
                    ((PlayerInterludeStats) stats).MAtk = attribute.value;
                    break;
                case AttributeType.CAST_SPD:
                    stats.MAtkSpd = attribute.value;
                    break;
                case AttributeType.M_DEF:
                    ((PlayerInterludeStats) stats).MDef = attribute.value;
                    break;
                case AttributeType.PVP_FLAG:
                    break;
                case AttributeType.KARMA:
                    break;
                case AttributeType.CUR_CP:
                    PlayerStatusInterlude test = (PlayerStatusInterlude)status;
                    test.Cp = attribute.value;
                    break;
                case AttributeType.MAX_CP:
                    stats.MaxCp = attribute.value;
                    break;
            }
        }
    }
}
