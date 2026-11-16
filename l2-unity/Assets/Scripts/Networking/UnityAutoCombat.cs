using System.Collections;
using System.IO;
using UnityEngine;

// Opt-in check started by UnityAutoLogin when the request has attack=nearest.
public sealed class UnityAutoCombat : MonoBehaviour
{
    public float StartDelay = 5f;
    public float Timeout = 60f;
    public bool QuitAfter;
    public int SkillId;

    public static string ResultPath
    {
        get { return Path.Combine(UnityAutoLogin.ResultDirectory, "autologin.combat"); }
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(StartDelay);

        ClickManager click = ClickManager.Instance;
        MonsterEntity target = FindNearestMonster();
        if (click == null || target == null)
        {
            yield return Finish("FAIL no ClickManager or no monster nearby");
            yield break;
        }

        int targetId = target.Identity != null ? target.Identity.Id : 0;
        string targetName = target.name;
        Debug.Log("[AutoCombat] target " + targetName + " id=" + targetId + " dist=" + DistanceToPlayer(target).ToString("0.0") + " playerHp=" + PlayerHp());

        click.ClickEntity(target.gameObject);
        yield return new WaitForSecondsRealtime(1.5f);
        if (SkillId == 0)
            click.ClickEntity(target.gameObject);
        else
            PlayerActions.Instance.UseSkill(SkillId);
        Debug.Log("[AutoCombat] attack sent skill=" + SkillId);

        float startedAt = Time.realtimeSinceStartup;
        float nextCastAt = startedAt + 4f;
        double lastHp = -1;
        while (Time.realtimeSinceStartup < startedAt + Timeout)
        {
            if (target == null)
            {
                yield return Finish("OK target despawned after " + Elapsed(startedAt) + "s, id=" + targetId);
                yield break;
            }

            if (target.IsDead())
            {
                yield return Finish("OK target dead after " + Elapsed(startedAt) + "s, id=" + targetId + " playerHp=" + PlayerHp());
                yield break;
            }

            double hp = target.Status != null ? target.Hp() : -1;
            if (SkillId > 0 && Time.realtimeSinceStartup >= nextCastAt && PlayerActions.Instance != null)
            {
                PlayerActions.Instance.UseSkill(SkillId);
                nextCastAt = Time.realtimeSinceStartup + 4f;
            }
            if (hp != lastHp)
            {
                Debug.Log("[AutoCombat] target hp=" + hp.ToString("0") + " playerHp=" + PlayerHp());
                lastHp = hp;
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }

        yield return Finish("FAIL timeout, target alive, hp=" + lastHp.ToString("0"));
    }

    MonsterEntity FindNearestMonster()
    {
        if (World.Instance == null || World.Instance.MonstersContainer == null || PlayerEntity.Instance == null)
            return null;

        MonsterEntity best = null;
        float bestDistance = float.MaxValue;
        foreach (MonsterEntity monster in World.Instance.MonstersContainer.GetComponentsInChildren<MonsterEntity>(false))
        {
            if (monster.IsDead() || monster.Identity == null)
                continue;
            float distance = DistanceToPlayer(monster);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = monster;
            }
        }
        return best;
    }

    static float DistanceToPlayer(Component other)
    {
        return Vector3.Distance(PlayerEntity.Instance.transform.position, other.transform.position);
    }

    static string PlayerHp()
    {
        PlayerEntity player = PlayerEntity.Instance;
        return player != null && player.Status != null ? player.Hp().ToString("0") : "?";
    }

    static string Elapsed(float since)
    {
        return (Time.realtimeSinceStartup - since).ToString("0.0");
    }

    IEnumerator Finish(string result)
    {
        Debug.Log("[AutoCombat] " + result);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            File.WriteAllText(ResultPath, result + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AutoCombat] could not write result: " + e.Message);
        }

        if (!QuitAfter)
            yield break;

        yield return new WaitForSecondsRealtime(3f);
        Debug.Log("[AutoCombat] quitting");
        Application.Quit();
    }
}
