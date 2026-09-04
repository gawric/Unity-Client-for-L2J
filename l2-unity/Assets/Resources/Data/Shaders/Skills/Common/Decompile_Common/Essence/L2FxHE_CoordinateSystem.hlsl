#ifndef L2_FX_HE_COORDINATE_SYSTEM_INCLUDED
#define L2_FX_HE_COORDINATE_SYSTEM_INCLUDED

// Engine_essens_high_elves.dll, byte UParticleEmitter+0x110
#define L2FX_HE_PTCS_INDEPENDENT 0.0
#define L2FX_HE_PTCS_RELATIVE 1.0
#define L2FX_HE_PTCS_ABSOLUTE 2.0
#define L2FX_HE_PTCS_RELATIVE_ROTATION 3.0
#define L2FX_HE_PTCS_SPRAY 4.0
#define L2FX_HE_PTCS_RELATIVE_POSITION 5.0
#define L2FX_HE_PTCS_SCREEN_ABSOLUTE 6.0
#define L2FX_HE_PTCS_SCREEN_RELATIVE 7.0

bool L2FxHE_CoordinateSystem_IsSpray(float coordinateSystem)
{
    return abs(coordinateSystem - L2FX_HE_PTCS_SPRAY) < 0.5;
}

float3 L2FxHE_CoordinateSystem_UeToUnityDir(float3 vectorUe)
{
    return float3(vectorUe.x, vectorUe.z, vectorUe.y);
}

float3 L2FxHE_CoordinateSystem_UnityToUeDir(float3 vectorUnity)
{
    return float3(vectorUnity.x, vectorUnity.z, vectorUnity.y);
}

float3 L2FxHE_IndependentSprayAccel_WorldAxesToObjectUe(float3 vectorUe)
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();
    float3 axisX = float3(objectToWorld._m00, objectToWorld._m10, objectToWorld._m20);
    float3 axisY = float3(objectToWorld._m01, objectToWorld._m11, objectToWorld._m21);
    float3 axisZ = float3(objectToWorld._m02, objectToWorld._m12, objectToWorld._m22);
    float scaleX = max(length(axisX), 1e-6);
    float scaleY = max(length(axisY), 1e-6);
    float scaleZ = max(length(axisZ), 1e-6);
    float3x3 worldToObjectRotation = float3x3(
        axisX / scaleX,
        axisY / scaleY,
        axisZ / scaleZ);

    float3 unityWorld = L2FxHE_CoordinateSystem_UeToUnityDir(vectorUe);
    float3 unityObject = mul(worldToObjectRotation, unityWorld);
    return L2FxHE_CoordinateSystem_UnityToUeDir(unityObject);
}

// Native IndependentSprayAccel (emitter+0x40 bit0): Acceleration and VelocityLoss
// stay on world axes while Spray already rotated Location/Velocity at spawn.
void L2FxHE_IndependentSprayAccel_Resolve(
    float coordinateSystem,
    float independentSprayAccel,
    float3 accelerationUe,
    float3 velocityLossPerSecond,
    out float3 resolvedAccelerationUe,
    out float3 resolvedVelocityLossPerSecond)
{
    resolvedAccelerationUe = accelerationUe;
    resolvedVelocityLossPerSecond = velocityLossPerSecond;
    if (independentSprayAccel < 0.5 ||
        !L2FxHE_CoordinateSystem_IsSpray(coordinateSystem))
    {
        return;
    }

    resolvedAccelerationUe =
        L2FxHE_IndependentSprayAccel_WorldAxesToObjectUe(accelerationUe);
    resolvedVelocityLossPerSecond =
        L2FxHE_IndependentSprayAccel_WorldAxesToObjectUe(velocityLossPerSecond);
}

#endif
