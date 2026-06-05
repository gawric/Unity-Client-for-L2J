#ifndef L2_FX_SPRITE_EMITTER_VERTEX_INCLUDED
#define L2_FX_SPRITE_EMITTER_VERTEX_INCLUDED

// Shared helpers for UE-style SpriteEmitter billboards in URP.
// Include after Core.hlsl and L2FxParticleAnim.hlsl.

float L2Fx_SpriteMaterialSeed(float globalSeed)
{
    // ParticleGroup assigns one runtime material per sprite slot and writes _Seed there.
    // Do not derive this from vertex UV: four different seeds per quad shear the sprite.
    return globalSeed;
}

float3 L2Fx_ObjectWorldScale()
{
    return float3(
        length(float3(UNITY_MATRIX_M[0][0], UNITY_MATRIX_M[1][0], UNITY_MATRIX_M[2][0])),
        length(float3(UNITY_MATRIX_M[0][1], UNITY_MATRIX_M[1][1], UNITY_MATRIX_M[2][1])),
        length(float3(UNITY_MATRIX_M[0][2], UNITY_MATRIX_M[1][2], UNITY_MATRIX_M[2][2])));
}

float L2Fx_MotionCompensationForManualBillboardScale(float manualBillboardScale)
{
    if (manualBillboardScale <= 0.0)
    {
        return 1.0;
    }

    float3 objectScale = L2Fx_ObjectWorldScale();
    return 1.0 / max(objectScale.x, 1e-4);
}

// Flat camera basis on the ground plane (screen left/right independent of player facing).
void L2Fx_CameraHorizontalBasis(out float3 camRightH, out float3 camForwardH)
{
    float3 camRightWS = float3(UNITY_MATRIX_V[0].x, UNITY_MATRIX_V[1].x, UNITY_MATRIX_V[2].x);
    float3 camForwardWS = float3(-UNITY_MATRIX_V[0].z, -UNITY_MATRIX_V[1].z, -UNITY_MATRIX_V[2].z);

    camRightH = float3(camRightWS.x, 0.0, camRightWS.z);
    float lenR = length(camRightH);
    camRightH = lenR > 1e-5 ? camRightH / lenR : float3(1.0, 0.0, 0.0);

    camForwardH = float3(camForwardWS.x, 0.0, camForwardWS.z);
    float lenF = length(camForwardH);
    camForwardH = lenF > 1e-5 ? camForwardH / lenF : float3(0.0, 0.0, 1.0);
}

// Azimuth in camera horizontal plane: 90 deg = screen left, 270 deg = screen right.
// Requires L2Fx_DegToRad from L2FxEmitterSpawn.hlsl (include that file before this one).
float3 L2Fx_CameraHorizontalUnitDirection(float azimuthDeg)
{
    float3 camRightH;
    float3 camForwardH;
    L2Fx_CameraHorizontalBasis(camRightH, camForwardH);

    float theta = azimuthDeg * L2Fx_DegToRad;
    return -camRightH * sin(theta) + camForwardH * cos(theta);
}

float3 L2Fx_WorldVelocityToObject(float3 velWS)
{
    return mul((float3x3)GetWorldToObjectMatrix(), velWS);
}

float3 L2Fx_CameraBillboardPositionWS(
    float3 centerWS,
    float3 quadOS,
    float manualBillboardScale,
    float applyUuToStartSize)
{
    float3 toCamera = _WorldSpaceCameraPos.xyz - centerWS;
    toCamera = dot(toCamera, toCamera) > 1e-8 ? normalize(toCamera) : float3(0, 1, 0);

    float3 rightWS = normalize(cross(float3(0, 1, 0), toCamera));
    float3 upWS = normalize(cross(toCamera, rightWS));

    if (applyUuToStartSize > 0.5)
    {
        return centerWS + rightWS * quadOS.x + upWS * quadOS.y;
    }

    float3 objectScale = L2Fx_ObjectWorldScale();
    if (manualBillboardScale > 0.0)
    {
        objectScale = float3(manualBillboardScale, manualBillboardScale, manualBillboardScale);
    }

    return centerWS
        + rightWS * (quadOS.x * objectScale.x)
        + upWS * (quadOS.y * objectScale.y);
}

void L2Fx_BuildColorScaleArrays5(
    uint colorScaleCount,
    float4 colorScale0,
    float colorScaleTime1, float4 colorScale1,
    float colorScaleTime2, float4 colorScale2,
    float colorScaleTime3, float4 colorScale3,
    float colorScaleTime4, float4 colorScale4,
    out float times[8],
    out float4 colors[8])
{
    [unroll]
    for (uint i = 0; i < 8; i++)
    {
        times[i] = 999.0;
        colors[i] = float4(1, 1, 1, 1);
    }

    times[0] = 0.0;
    colors[0] = colorScale0;
    if (colorScaleCount >= 2) { times[1] = colorScaleTime1; colors[1] = colorScale1; }
    if (colorScaleCount >= 3) { times[2] = colorScaleTime2; colors[2] = colorScale2; }
    if (colorScaleCount >= 4) { times[3] = colorScaleTime3; colors[3] = colorScale3; }
    if (colorScaleCount >= 5) { times[4] = colorScaleTime4; colors[4] = colorScale4; }
}

#endif // L2_FX_SPRITE_EMITTER_VERTEX_INCLUDED
