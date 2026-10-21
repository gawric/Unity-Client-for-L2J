using UnityEngine;

public sealed class EntityActionStop : IEntityActionProcess
{
    static readonly int BlockMask = LayerMask.GetMask(
        "Default", "Terrain", "StaticMesh", "Brush", "AllowWalk", "Deco", "Obstacle");

    public L2PawnRange Range { get; }

    public EntityActionStop(L2PawnRange pawnRange)
    {
        Range = pawnRange;
    }

    public void Enter(Entity entity, object payload)
    {
        if (entity == null || entity.IsDead())
            return;

        EntityActionVisual.PlayStandWait(entity);
    }

    public void Tick(Entity entity)
    {
    }

    public static void StopLocomotion(Entity entity)
    {
        if (entity == null)
            return;

        bool wasMoving = entity.Identity != null && MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(entity.Identity.Id);
        Vector3 destBefore = entity.ActionSlot != null
            ? entity.ActionSlot.Destination
            : Vector3.zero;
        EntityActionVisual.CancelMove(entity);
        L2PawnRange.ClearIgnoredPawn(entity);
        if (entity.ActionSlot != null)
        {
            entity.ActionSlot.PawnDist = 0f;
            entity.ActionSlot.Destination = entity.transform.position;
        }

        // EntityActionCombatLog.LogStep(entity,
            // "STOP LOCO nick=" + EntityActionCombatLog.NameOf(entity) +
            // " wasMoving=" + wasMoving +
            // " nowL2=" + EntityActionCombatLog.L2(entity.transform.position) +
            // " destL2=" + EntityActionCombatLog.L2(destBefore));
    }

    public static void TryCorrectPosition(Entity entity, StopMoveDto dto, L2PawnRange pawnRange, bool skipCorrection)
    {
        if (entity == null || entity.Identity == null || dto == null)
            return;

        Vector3 stop = GroundSnapHelper.SnapToGroundOrKeep(dto.StopPos);
        float snapD = Vector3.Distance(entity.transform.position, stop);
        float skipM = pawnRange != null
            ? pawnRange.AdjustSkipMeters()
            : VectorUtils.ConvertL2UuToMeters(L2PawnRange.AdjustSkipUu);
        Entity pawn = EntityPawnLookup.PawnOf(entity);
        bool skipSnap = skipCorrection || snapD <= skipM;
        string reason = skipCorrection ? " SKIP_WEIRD" : (skipSnap ? " SKIP_SNAP" : " SNAP");
        // EntityActionCombatLog.LogCiPawn(entity,
            // "Stop.Pos nick=" + EntityActionCombatLog.NameOf(entity) +
            // " snapD=" + snapD.ToString("F2") +
            // " skipM=" + skipM.ToString("F2") +
            // " stop=" + EntityActionCombatLog.Vec(stop) +
            // " now=" + EntityActionCombatLog.Vec(entity.transform.position) +
            // " pawn=" + EntityActionCombatLog.Describe(pawn) +
            // reason);
        if (skipSnap)
        {
            if (snapD >= 0.4f)
            {
                Vector3 nowL2 = VectorUtils.ConvertPosUnityToL2j(entity.transform.position);
                Vector3 stopL2 = VectorUtils.ConvertPosUnityToL2j(stop);
                // Debug.Log("[MOVE_SNAP] STOP SKIP nick=" + EntityActionCombatLog.NameOf(entity) +
                    // " snapM=" + snapD.ToString("F2") +
                    // " skipM=" + skipM.ToString("F2") +
                    // " nowL2=" + nowL2.x.ToString("F0") + "," + nowL2.y.ToString("F0") + "," + nowL2.z.ToString("F0") +
                    // " stopL2=" + stopL2.x.ToString("F0") + "," + stopL2.y.ToString("F0") + "," + stopL2.z.ToString("F0"));
            }
            return;
        }

        if (WouldOverlap(entity, stop))
        {
            // EntityActionCombatLog.LogGap(entity, "Stop SKIP_OVERLAP", pawn,
                // " snapD=" + snapD.ToString("F2") +
                // " stop=" + EntityActionCombatLog.Vec(stop));
            return;
        }

        Vector3 dest = entity.ActionSlot.Destination;
        float stopToDest = Vector3.Distance(stop, dest);
        if (snapD >= 0.4f || stopToDest >= 0.4f)
        {
            Vector3 nowL2 = VectorUtils.ConvertPosUnityToL2j(entity.transform.position);
            Vector3 stopL2 = VectorUtils.ConvertPosUnityToL2j(stop);
            Vector3 destL2 = VectorUtils.ConvertPosUnityToL2j(dest);
            // Debug.Log("[MOVE_SNAP] STOP nick=" + EntityActionCombatLog.NameOf(entity) +
                // " snapM=" + snapD.ToString("F2") +
                // " stopToDestM=" + stopToDest.ToString("F2") +
                // " nowL2=" + nowL2.x.ToString("F0") + "," + nowL2.y.ToString("F0") + "," + nowL2.z.ToString("F0") +
                // " stopL2=" + stopL2.x.ToString("F0") + "," + stopL2.y.ToString("F0") + "," + stopL2.z.ToString("F0") +
                // " destL2=" + destL2.x.ToString("F0") + "," + destL2.y.ToString("F0") + "," + destL2.z.ToString("F0"));
        }

        entity.Identity.Position = stop;
        entity.ActionSlot.Destination = stop;
        EntitySpawnShared.ApplyGroundedTransform(entity.gameObject, stop, entity.transform.rotation);
    }

    static bool WouldOverlap(Entity entity, Vector3 worldPos)
    {
        CharacterController cc = entity.GetComponent<CharacterController>();
        if (cc == null)
            cc = entity.GetComponentInChildren<CharacterController>();
        if (cc == null)
            return false;

        Vector3 delta = cc.transform.TransformPoint(cc.center) - entity.transform.position;
        Vector3 worldCenter = worldPos + delta + entity.transform.up * 0.05f;
        float radius = Mathf.Max(0.01f, cc.radius * 0.85f);
        float half = Mathf.Max(0f, cc.height * 0.5f - cc.radius - 0.05f);
        Vector3 up = cc.transform.up;
        return Physics.CheckCapsule(
            worldCenter + up * half,
            worldCenter - up * half,
            radius,
            BlockMask,
            QueryTriggerInteraction.Ignore);
    }
}
