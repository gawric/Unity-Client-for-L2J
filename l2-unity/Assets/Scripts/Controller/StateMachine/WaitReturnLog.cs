using UnityEngine;

/// <summary>
/// Local-player WAIT_RETURN / atkwait leak probe. Filter: [WAIT_RETURN]
/// </summary>
public static class WaitReturnLog
{
    public const string Tag = "[WAIT_RETURN]";

    public static void Dump(string where, Entity dead = null, DieDto dto = null)
    {
        PlayerEntity player = PlayerEntity.Instance;
        PlayerStateMachine sm = PlayerStateMachine.Instance;
        int deadId = dto != null
            ? dto.ObjectId
            : (dead != null && dead.Identity != null ? dead.Identity.Id : 0);
        int targetId = player != null ? player.TargetId : 0;
        int localId = sm != null ? sm.GetObjectId() : -1;
        string anim = "-";
        if (IncomingPacketActions.Animations != null && localId > 0)
            anim = IncomingPacketActions.Animations.GetCurrentAnimationName(localId);

        Debug.Log(Tag + " " + where +
            " dead=" + NameOf(dead) +
            " deadId=" + deadId +
            " localId=" + localId +
            " localTargetId=" + targetId +
            " targetMatch=" + (targetId != 0 && targetId == deadId) +
            " autoAtk=" + (player != null && player.isAutoAttack) +
            " isAttack=" + (player != null && player.IsAttack) +
            " state=" + (sm != null ? sm.State.ToString() : "-") +
            " intention=" + (sm != null ? sm.Intention.ToString() : "-") +
            " anim=" + anim);
    }

    public static void Handle(
        string where,
        bool fromCombatSmb,
        bool targetDead,
        bool useAtkWait,
        string animation,
        bool skipSwing,
        string swingState)
    {
        Dump(where);
        Debug.Log(Tag + " HANDLE " + where +
            " fromCombatSmb=" + fromCombatSmb +
            " targetDead=" + targetDead +
            " useAtkWait=" + useAtkWait +
            " play=" + animation +
            " skipSwing=" + skipSwing +
            " swing=" + swingState);
    }

    public static void Play(int objectId, string animName)
    {
        PlayerStateMachine sm = PlayerStateMachine.Instance;
        int localId = sm != null ? sm.GetObjectId() : -1;
        if (localId > 0 && objectId != localId)
            return;
        Debug.Log(Tag + " PLAY objectId=" + objectId +
            " localId=" + localId +
            " anim=" + animName +
            " state=" + (sm != null ? sm.State.ToString() : "-"));
    }

    static string NameOf(Entity entity)
    {
        if (entity == null)
            return "-";
        if (entity is UserEntity user)
            return user.Nick;
        if (entity.Identity != null && !string.IsNullOrEmpty(entity.Identity.Name))
            return entity.Identity.Name;
        return entity.name;
    }
}
