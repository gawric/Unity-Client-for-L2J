using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reconciles a visible actor's cubic list with the list carried by
/// CharInfo/UserInfo. Mirrors APawn::CheckCubicStatus semantics exactly,
/// including the order of operations (adds first, then removals, then slots).
/// </summary>
public sealed class CubicCollection
{
    private readonly List<CubicInstance> _cubics = new List<CubicInstance>();

    public IReadOnlyList<CubicInstance> Cubics => _cubics;

    /// <summary>
    /// Native equivalent: APawn::CheckCubicStatus(short count, short* ids).
    /// </summary>
    /// <param name="ids">Cubic types from the packet (may be null/empty).</param>
    /// <param name="owner">Owner transform, used for the +100 Z spawn offset.</param>
    public void CheckCubicStatus(int[] ids, Transform owner)
    {
        int count = ids != null ? ids.Length : 0;

        // Native: count == 0 short-circuits straight to DeleteAllNCubic.
        if (count == 0)
        {
            DeleteAll();
            return;
        }

        // Pass 1 — every id in the packet that we do not have yet gets added.
        // Native compares `cmp word ptr [ebx + edi*2], dx` (indexed read) and
        // only calls AddNCubic when the type is absent.
        for (int i = 0; i < count; i++)
        {
            int type = ids[i];
            // Pass 1 has no cap check of its own in the native code
            // (loc_1B81CA9 -> call AddNCubic straight away); AddNCubic rejects
            // Num > 3 internally, so the guard lives there only.
            if (!ContainsType(type))
                AddNCubic(type, owner);
        }

        // Pass 2 — every cubic we have that is absent from the packet goes away.
        // The loop runs UPWARDS in the native code:
        //   loc_1B81CC0: cmp edi,[esi+1678h] / jge done ; ... / add edi,1
        // so a removal shifts the next element left under the cursor and that
        // element is skipped. This is a real native quirk, not a bug to "fix":
        // reproducing it keeps the Unity client in step with Interlude when a
        // packet drops several cubics at once.
        for (int i = 0; i < _cubics.Count; i++)
        {
            if (!ContainsId(ids, count, _cubics[i].CubicType))
                RemoveAt(i);
        }

        // Pass 3 — remaining slots are renumbered 0..Num-1 (CubicIndex, +0x4EC).
        for (int i = 0; i < _cubics.Count; i++)
            _cubics[i].CubicIndex = i;
    }

    /// <summary>
    /// Native equivalent: APawn::AddNCubic(int CubicType).
    /// </summary>
    public CubicInstance AddNCubic(int cubicType, Transform owner)
    {
        // Guard order matters: Num check happens before the type range check.
        if (_cubics.Count > CubicConstants.MaxCubics) return null;
        if (cubicType > CubicConstants.MaxCubicType) return null;

        string emitter = CubicConstants.EmitterName(cubicType);
        if (emitter == null) return null; // native: NAME_None -> return 0

        Vector3 spawn = owner != null ? owner.position : Vector3.zero;
        spawn.z += CubicConstants.SpawnZOffset;

        var cubic = new CubicInstance
        {
            CubicType = cubicType,
            MovementState = CubicConstants.StateSpawn,
            Location = spawn,
            DestLocation = Vector3.zero,
            Velocity = Vector3.zero,
            SkillId = -1,
            Target = null,
            EmitterName = emitter,
            // Pass 3 of CheckCubicStatus overwrites this; native AddNCubic leaves
            // the freshly spawned actor's index at its constructor value.
            CubicIndex = _cubics.Count
        };

        // Native: FArray::Insert(&NCubicArray, 0, 1, 4) then Data[0] = Cubic.
        // Insert-at-front means the array is newest-first: after adding 1, 2, 3
        // the live array is ["3","2","1"] (observed in del_partial). Appending
        // instead would put CubicIndex in the wrong order before Pass 3 runs.
        _cubics.Insert(0, cubic);
        return cubic;
    }

    public bool ContainsType(int cubicType)
    {
        for (int i = 0; i < _cubics.Count; i++)
            if (_cubics[i].CubicType == cubicType) return true;
        return false;
    }

    private static bool ContainsId(int[] ids, int count, int type)
    {
        for (int i = 0; i < count; i++)
            if (ids[i] == type) return true;
        return false;
    }

    /// <summary>
    /// Native equivalent: APawn::DeleteNCubic(int CubicType).
    /// Live body 0x1E1B30, returned in <c>del_partial</c> with Num 3-&gt;2.
    ///
    /// The native loop (loc_1B81B63) has NO early exit: `esi = Num-1`, then it
    /// walks the WHOLE array downwards and removes every element whose
    /// CubicType(+0x4DC) matches, calling TArray::Remove + XLevel->vtbl[0xAC]
    /// (DestroyActor) for each. The first C# version returned after the first
    /// match, which would leave a duplicate type alive.
    /// </summary>
    public void DeleteNCubic(int cubicType)
    {
        for (int i = _cubics.Count - 1; i >= 0; i--)
        {
            if (_cubics[i].CubicType == cubicType)
                RemoveAt(i);
        }
    }

    /// <summary>
    /// Native equivalent: APawn::DeleteAllNCubic() — Live 0x1E1A30.
    /// Live verified in <c>del_all</c> (Num 1-&gt;0) and <c>del_partial</c>
    /// (Num 2-&gt;0 after CUBIC_STOP).
    ///
    /// The native tail loop walks backwards, calling TArray::Remove(i,1) plus
    /// XLevel->vtbl[0xAC] (DestroyActor) for each element, and only then runs
    /// FArray::Empty. The backward walk matters: destroying an actor while the
    /// array still references it is exactly what the native order does.
    /// </summary>
    public void DeleteAll()
    {
        for (int i = _cubics.Count - 1; i >= 0; i--)
            RemoveAt(i);
        // Mirrors the trailing FArray::Empty(&NCubicArray, 4, 0).
        _cubics.Clear();
    }

    private void RemoveAt(int index)
    {
        CubicInstance cubic = _cubics[index];
        if (cubic.Visual != null) UnityEngine.Object.Destroy(cubic.Visual);
        _cubics.RemoveAt(index);
    }

    internal List<CubicInstance> Items
    {
        get { return _cubics; }
    }
}
