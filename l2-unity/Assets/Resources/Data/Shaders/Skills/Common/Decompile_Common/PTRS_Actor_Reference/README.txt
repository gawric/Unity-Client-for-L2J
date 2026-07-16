PTRS_Actor / UMeshEmitter WorldMat reference
==============================================

Purpose
-------
This directory preserves the reverse-engineering contract for future Unity
validation. The implementation is:

  ../L2FxPTRS_Actor.hlsl

It is deliberately separate from existing production shaders. A future shader
or CPU test can include/call it without needing to reinterpret the old client
again.

Client branch
-------------
UMeshEmitter::RenderParticles:

  UseRotationFrom == 1  -> PTRS_Actor

Relevant runtime fields:

  UMeshEmitter +0x108  UseRotationFrom
  UMeshEmitter +0x244  SpinParticles flag

  Particle +0x00       Location                         FVector
  Particle +0x30       SpinsPerSecond components c0,c1,c2 (float)
  Particle +0x3C       StartSpin components c0,c1,c2      (float)
  Particle +0x6C       FinalSize                        FVector
  Particle +0xC8       particle rotation for branch == 4 (not PTRS_Actor)
  Particle slot stride 0xE0 (224)

  Owner +0x1BC         Location                          FVector
  Owner +0x1C8         Rotation                          FRotator (Pitch,Yaw,Roll), URU ints
  Owner +0x28C         pivot used by renderer            FVector

Formula
-------
UE2 row-vector convention:

  world =
      T(-OwnerPivot)
    * S(FinalSize)
    * R(Spin)
    * T(ParticleLocation)
    * R(OwnerRotation)
    * T(OwnerLocation)

  transformedPosition = float4(localPosition, 1) * world

FMatrix is row-major and its translation is m[12..14] (the fourth row).
HLSL's mul(rowVector, matrix) is appropriate for this UE-side matrix.

Spin
----
The client computes each source component:

  ci = trunc(SpinsPerSecond.ci * ParticleTime + StartSpin.ci)

Then calls:

  RotationURU(c1, c0, c2)

Thus the FRotationMatrix input is:

  (Pitch, Yaw, Roll) = (c1, c0, c2)

URU -> radians:

  radians = URU * (2*pi / 65536)

The FRotationMatrix coefficients are implemented exactly in
L2FxPTRS_Actor.hlsl from client helper sub_1BC63F0.

Unity basis
-----------
Do the exact calculation in UE coordinates first. Then convert the resulting
position using this project's position bridge:

  Unity(X,Y,Z) = UE(X,Z,Y) * UU_TO_UNITY

Do not directly feed the UE row-vector matrix to a Unity column-vector
mul(matrix, vector) call. Either use L2FxPTRSActor_TransformPositionUnity or
transpose/conjugate the matrix deliberately.

Live validation evidence
------------------------
Capture method:

  UMeshEmitter::RenderParticles active slot:
    EDI = current particle slot
    EBX = UMeshEmitter

  Final WorldMat:
    stack local v162 = [EBP-0x108]
    hook selected as "lea-ebp-108-last".

The hook runs after v190 advances by 0xE0, so the associated slot is normally:

  particleSlot = v190 - 0xE0

For the final active slot v190 can be outside the active range; the capture
tool uses the active-entry FIFO fallback.

Verified run
------------
Effect:

  it_healing_potion_ta
  MeshEmitter3 (needlelight)
  MeshEmitter4 (Wave)

RenderParticle.log, 2026-07-16 15:50:

  MeshEmitter3, slot 0x18936000:
    ownerLocation = (-85618, 240974, -3730.27393)
    ownerRotation = (0, 0, 0)
    ownerPivot = (0, 0, 0)
    finalSize = (0.0266, 0.0266, 0.0266)
    startSpin c0,c1,c2 = (57460.878906, 60672.925781, 0)
    spinRate c0,c1,c2 = (0, 0, 0)
    particleTime = 0.0079

    live WorldMat:
      [ 0.01702 -0.01664 -0.01198  0
        0.01863  0.01905  0        0
        0.00857 -0.00838  0.02380  0
       -85618    240974   -3730.27393 1 ]

    L2FxPTRSActor (c1,c0,c2): match=1, maxAbsDiff=0.000000
    Unswapped (c0,c1,c2): match=0, maxAbsDiff=0.008077

  MeshEmitter4, slot 0x136F7800:
    finalSize = (0.0331, 0.0331, -0.0078)
    particleLocation = (0, 0, 0.1901)
    spinRate c0,c1,c2 = (-19660.5, 0, 0)
    startSpin c0,c1,c2 = (54276.828125, 104.721603, 0)
    particleTime = 0.0079

    live WorldMat:
      [ 0.01518 -0.02942  0.00033  0
        0.02942  0.01518  0        0
        0.00004 -0.00007 -0.00776  0
       -85618    240974   -3730.08374 1 ]

    L2FxPTRSActor (c1,c0,c2): match=1, maxAbsDiff=0.000006
    Unswapped (c0,c1,c2): match=0, maxAbsDiff=0.029752

What is live-proven vs statically proven
-----------------------------------------
Live-proven on the above effect:

  - final size, particle location, owner location, exact FRotationMatrix,
    spin component swap (c1,c0,c2), and the complete resulting WorldMat.

Proven in the RenderParticles decompile, but not exercised with non-zero data
by this specific potion:

  - R(OwnerRotation)
  - T(-OwnerPivot)

For a complete non-identity regression, capture an effect whose owner has
non-zero Rotation and/or non-zero Owner+0x28C. Then compare its live matrix
with L2FxPTRSActor_BuildWorldUeRowMatrix using max absolute element error
<= 1e-3 (normal float variance is expected around 1e-5).

Non-PTRS_Actor branches
-----------------------
UseRotationFrom == 4 uses a rotation stored in Particle+0xC8/+0xCC/+0xD0;
it is not the live actor-rotation branch. Other values use the no-owner
branch. This library intentionally covers only runtime value 1.
