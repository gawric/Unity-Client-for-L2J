Shader "L2/Effects/MeshEmitter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SecondTex ("Second Texture (D3D9 t1)", 2D) = "white" {}
        [Toggle] _UseSecondTex ("Use Second Tex MODULATE2X", Float) = 0
        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (1, 1, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age sec", Float) = 0

        [Enum(None,0,ZOnly,1,XYZ,2,LocationRange,3)] _SpawnMode ("Spawn Mode", Float) = 0
        [Toggle] _FullTlsShape ("PTLS Polar Spawn", Float) = 0
        [Enum(None,0,StartPositionAndOwner,1,OwnerAndStartPosition,2)] _PtvdMode ("GetVelocityDirectionFrom", Float) = 0
        [Enum(None,0,Ballistic,1,Drag,2)] _MotionMode ("Motion Mode", Float) = 0
        [Enum(Regular,0,PTRS_Actor,1)] _TransformMode ("Transform Mode", Float) = 0
        [Enum(Uniform,0,XYPlusZ,1,XYZ,2,UniformRange,3)] _SizeMode ("Size Mode", Float) = 0
        [Enum(Uc,0,Ue,1)] _OffsetSource ("Offset Property Source", Float) = 0
        [Enum(Vector,0,Ranges,1)] _SpinSpsMode ("Spin SPS Source", Float) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.8
        _StartSize ("StartSize Uniform", Float) = 1
        _StartSizeRange ("StartSize Uniform Min Max", Vector) = (1, 1, 0, 0)
        _StartSizeXY ("StartSize XY", Float) = 1
        _StartSizeZRange ("StartSize Z Min Max", Vector) = (1, 1, 0, 0)
        _StartSizeRangeXUc ("StartSize X Min Max", Vector) = (1, 1, 0, 0)
        _StartSizeRangeYUc ("StartSize Y Min Max", Vector) = (1, 1, 0, 0)
        _StartSizeRangeZUc ("StartSize Z Min Max", Vector) = (1, 1, 0, 0)
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 0
        _SizeKeyCount ("Size Key Count", Float) = 2
        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0, 1, 0, 0)
        _SizeKey1 ("Size Key 1 Time Size", Vector) = (1, 1, 0, 0)
        _SizeKey2 ("Size Key 2 Time Size", Vector) = (1, 1, 0, 0)
        _SizeKey3 ("Size Key 3 Time Size", Vector) = (1, 1, 0, 0)
        _SizeKey4 ("Size Key 4 Time Size", Vector) = (1, 1, 0, 0)

        _StartLocationOffsetUc ("StartLocationOffset UC XYZ", Vector) = (0, 0, 0, 0)
        _StartLocationOffsetUe ("StartLocationOffset UE XYZ", Vector) = (0, 0, 0, 0)
        _StartLocationZRangeUU ("StartLocation Z Min Max", Vector) = (0, 0, 0, 0)
        _StartLocationRangeXUc ("StartLocation X Min Max", Vector) = (0, 0, 0, 0)
        _StartLocationRangeYUc ("StartLocation Y Min Max", Vector) = (0, 0, 0, 0)
        _StartLocationRangeZUc ("StartLocation Z Min Max", Vector) = (0, 0, 0, 0)
        _PolarThetaRangeUc ("Polar Theta Min Max", Vector) = (0, 360, 0, 0)
        _PolarPhiRangeUc ("Polar Phi Min Max", Vector) = (0, 180, 0, 0)
        _PolarRadiusRangeUc ("Polar Radius UU Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityZRangeUU ("StartVelocity Z Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (0, 0, 0, 0)
        _AccelerationUc ("Acceleration UC XYZ", Vector) = (0, 0, 0, 0)
        _VelocityLossRangeUc ("VelocityLoss UC XYZ", Vector) = (0, 0, 0, 0)
        _MeshSpawnRandStateBits ("Mesh Spawn appRand TLS", Float) = 0

        [Toggle] _SpinParticles ("SpinParticles", Float) = 0
        _StartSpinYawRangeUc ("StartSpin Yaw Min Max", Vector) = (0, 0, 0, 0)
        _StartSpinPitchRangeUc ("StartSpin Pitch Min Max", Vector) = (0, 0, 0, 0)
        _StartSpinRollRangeUc ("StartSpin Roll Min Max", Vector) = (0, 0, 0, 0)
        _SpsYawPitchRollUc ("SPS Yaw Pitch Roll", Vector) = (0, 0, 0, 0)
        _SpsYawRangeUc ("SPS Yaw Min Max", Vector) = (0, 0, 0, 0)
        _SpsPitchRangeUc ("SPS Pitch Min Max", Vector) = (0, 0, 0, 0)
        _SpsRollRangeUc ("SPS Roll Min Max", Vector) = (0, 0, 0, 0)
        _SpinCCWorCW ("Spin Direction XYZ", Vector) = (0.5, 0.5, 0.5, 0)
        _StartSpinRandStateBits ("StartSpin appRand TLS", Float) = 0

        _ColorMultiplier ("ColorMultiplier RGB", Vector) = (1, 1, 1, 0)
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (1, 1, 1, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (1, 1, 1, 0)
        _ColorScaleRepeats ("ColorScale Repeats", Float) = 0
        _ColorKey0 ("Color Key 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("Color Key 1 Time", Float) = 1
        _ColorKey1 ("Color Key 1", Color) = (1, 1, 1, 1)
        _ColorKey2Time ("Color Key 2 Time", Float) = 1
        _ColorKey2 ("Color Key 2", Color) = (1, 1, 1, 1)
        _ColorKey3Time ("Color Key 3 Time", Float) = 1
        _ColorKey3 ("Color Key 3", Color) = (1, 1, 1, 1)
        _ColorKey4Time ("Color Key 4 Time", Float) = 1
        _ColorKey4 ("Color Key 4", Color) = (1, 1, 1, 1)
        _ColorKey5Time ("Color Key 5 Time", Float) = 1
        _ColorKey5 ("Color Key 5", Color) = (1, 1, 1, 1)
        [Toggle] _FadeIn ("FadeIn", Float) = 0
        _FadeInEndTime ("FadeIn End Time", Float) = 0
        [Toggle] _FadeOut ("FadeOut", Float) = 0
        _FadeOutStartTime ("FadeOut Start Time", Float) = 0
        _Opacity ("Opacity", Range(0, 2)) = 1
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("Color Gamma To Linear", Float) = 0
        _AlphaClipThreshold ("Alpha Clip Threshold (-1 off)", Float) = -1
        [Toggle] _DebugMeshOut ("Debug Mesh Output", Float) = 0
        [Toggle] _ExpandShaderBounds ("Expand CPU Bounds For Vertex Motion", Float) = 0

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    // URP Deferred (URP_Renderer RenderingMode=2, DepthPriming=On):
    // Unlit Queue=Geometry + LightMode=UniversalForward is skipped (no GBuffer/DepthOnly).
    // Mesh + texture look missing. Do not convert MeshEmitter to SpriteEmitter as a workaround.
    // Required tags: Queue/RenderType=Transparent, UniversalMaterialType=Unlit,
    // LightMode=UniversalForwardOnly. Same rule for item DropMesh (L2DropMeshMasked).
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "Unlit"
            "L2FxGpuInstancing" = "On"
        }

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        ZTest LEqual
        Cull [_Cull]

        Pass
        {
            Name "UnifiedMeshForwardOnly"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "L2FxUnifiedMeshEmitter.Pass.hlsl"
            ENDHLSL
        }
    }
    FallBack Off
}
