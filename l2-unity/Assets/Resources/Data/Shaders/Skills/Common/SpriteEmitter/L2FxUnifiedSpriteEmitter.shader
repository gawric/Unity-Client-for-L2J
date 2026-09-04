Shader "L2/Effects/SpriteEmitter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _StartTime ("Start Time", Float) = 0
        _Seed ("Seed", Float) = 0
        _SpriteMotionRandStateBits ("Sprite Motion appRand TLS", Float) = 0
        _SpriteSpinRandStateBits ("Sprite Spin appRand TLS", Float) = 0
        _OwnerWorldPos ("Owner World Position", Vector) = (0, 0, 0, 0)
        _LifetimeRange ("Lifetime Min Max", Vector) = (1, 1, 0, 0)
        _InitialDelayRange ("Initial Delay Min Max", Vector) = (0, 0, 0, 0)
        [Toggle] _UseManualAge ("Use Manual Age", Float) = 0
        _ManualAge ("Manual Age", Float) = 0

        [Enum(None,0,Box,1,Polar,2,FullTLS,3)] _SpawnMode ("Spawn Mode", Float) = 0
        [Enum(Shape0Box,0,Polar,1)] _FullTlsShape ("Full TLS Shape", Float) = 0
        [Enum(Box,0,Sphere,1,Polar,2)] _HeLocationShape ("HE Location Shape", Float) = 0
        [Enum(None,0,Ballistic,1,Drag,2)] _MotionMode ("Motion Mode", Float) = 0
        [Enum(CameraBillboard,0,PTDU_Up,1,PTDU_Normal,2,PTDU_Forward,3)] _OrientationMode ("Orientation Mode", Float) = 0
        _SurfaceNormals ("Surface Normals", Vector) = (0, 0, 0, 0)
        [Enum(None,0,StartAndOwner,1,OwnerAndStart,2)] _PtvdMode ("PTVD Mode", Float) = 0
        [Enum(Uniform,0,XYZ,1)] _SizeMode ("Size Mode", Float) = 0
        [Enum(None,0,AppRand,1)] _SpinMode ("Spin Mode", Float) = 0
        [Enum(Static,0,Timed,1,Random,2,BlendBetween,3)] _FlipbookMode ("Flipbook Mode", Float) = 0

        _L2FxWorldCalibration ("World Calibration K", Float) = 1.1
        _StartLocationOffsetUc ("StartLocationOffset UC XYZ", Vector) = (0, 0, 0, 0)
        _StartLocationRangeUU ("StartLocation Range UU", Vector) = (0, 0, 0, 0)
        _StartLocationRangeXUc ("StartLocation X Min Max", Vector) = (0, 0, 0, 0)
        _StartLocationRangeYUc ("StartLocation Y Min Max", Vector) = (0, 0, 0, 0)
        _StartLocationRangeZUc ("StartLocation Z Min Max", Vector) = (0, 0, 0, 0)
        _PolarThetaRangeUc ("Polar Theta Min Max", Vector) = (0, 360, 0, 0)
        _PolarPhiRangeUc ("Polar Phi Min Max", Vector) = (0, 0, 0, 0)
        _PolarRadiusRangeUc ("Polar Radius Min Max", Vector) = (0, 0, 0, 0)
        _SphereRadiusRangeUc ("Sphere Radius Min Max", Vector) = (0, 0, 0, 0)
        [Toggle] _UseRevolution ("UseRevolution", Float) = 0
        _RevolutionCenterOffsetRangeXUc ("Revolution Center X Min Max", Vector) = (0, 0, 0, 0)
        _RevolutionCenterOffsetRangeYUc ("Revolution Center Y Min Max", Vector) = (0, 0, 0, 0)
        _RevolutionCenterOffsetRangeZUc ("Revolution Center Z Min Max", Vector) = (0, 0, 0, 0)
        _RevolutionsPerSecondRangeXUc ("Revolution RPS X Min Max", Vector) = (0, 0, 0, 0)
        _RevolutionsPerSecondRangeYUc ("Revolution RPS Y Min Max", Vector) = (0, 0, 0, 0)
        _RevolutionsPerSecondRangeZUc ("Revolution RPS Z Min Max", Vector) = (0, 0, 0, 0)
        [Toggle] _UseRevolutionScale ("Use RevolutionScale", Float) = 0
        _RevolutionScaleRepeats ("RevolutionScale Repeats", Float) = 0
        _RevolutionScaleCount ("RevolutionScale Key Count", Float) = 1
        _RevolutionScaleKey0 ("RevolutionScale Key 0", Vector) = (0, 1, 1, 1)
        _RevolutionScaleKey1 ("RevolutionScale Key 1", Vector) = (1, 1, 1, 1)
        _RevolutionScaleKey2 ("RevolutionScale Key 2", Vector) = (1, 1, 1, 1)
        _RevolutionScaleKey3 ("RevolutionScale Key 3", Vector) = (1, 1, 1, 1)
        _RevolutionScaleKey4 ("RevolutionScale Key 4", Vector) = (1, 1, 1, 1)
        _RevolutionScaleKey5 ("RevolutionScale Key 5", Vector) = (1, 1, 1, 1)
        _RevolutionScaleKey6 ("RevolutionScale Key 6", Vector) = (1, 1, 1, 1)
        _StartVelocityRangeXUc ("StartVelocity X Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeYUc ("StartVelocity Y Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRangeZUc ("StartVelocity Z Min Max", Vector) = (0, 0, 0, 0)
        _StartVelocityRadialRangeUc ("StartVelocity Radial", Vector) = (1, 1, 0, 0)
        _AccelerationUc ("Acceleration UC XYZ", Vector) = (0, 0, 0, 0)
        _VelocityLossRangeUc ("Velocity Loss XYZ", Vector) = (0, 0, 0, 0)
        [Enum(Independent,0,Relative,1,Absolute,2,RelativeRotation,3,Spray,4,RelativePosition,5,ScreenAbsolute,6,ScreenRelative,7)] _CoordinateSystem ("CoordinateSystem", Float) = 1
        [Toggle] _IndependentSprayAccel ("IndependentSprayAccel", Float) = 0
        _MaxAbsVelocityUc ("MaxAbsVelocity XYZ", Vector) = (10000, 10000, 10000, 0)
        [Toggle] _UseVelocityScale ("Use VelocityScale", Float) = 0
        _VelocityScaleRepeats ("VelocityScale Repeats", Float) = 0
        _VelocityScaleCount ("VelocityScale Key Count", Float) = 1
        _VelocityScaleKey0 ("VelocityScale Key 0", Vector) = (0, 1, 1, 1)
        _VelocityScaleKey1 ("VelocityScale Key 1", Vector) = (1, 1, 1, 1)
        _VelocityScaleKey2 ("VelocityScale Key 2", Vector) = (1, 1, 1, 1)
        _VelocityScaleKey3 ("VelocityScale Key 3", Vector) = (1, 1, 1, 1)
        _VelocityScaleKey4 ("VelocityScale Key 4", Vector) = (1, 1, 1, 1)
        _VelocityScaleKey5 ("VelocityScale Key 5", Vector) = (1, 1, 1, 1)
        _VelocityScaleKey6 ("VelocityScale Key 6", Vector) = (1, 1, 1, 1)
        _SpawnDeltaTime ("Spawn Delta Time", Float) = 0.012

        _SizeRange ("Uniform Size Min Max", Vector) = (1, 1, 0, 0)
        _SizeRangeXUc ("Size X Min Max", Vector) = (1, 1, 0, 0)
        _SizeRangeYUc ("Size Y Min Max", Vector) = (1, 1, 0, 0)
        _SizeRangeZUc ("Size Z Min Max", Vector) = (1, 1, 0, 0)
        [Toggle] _UseSizeScale ("Use SizeScale", Float) = 0
        _SizeScaleRepeats ("SizeScale Repeats", Float) = 0
        _SizeScaleCount ("SizeScale Key Count", Float) = 2
        _SizeKey0 ("Size Key 0", Vector) = (0, 1, 0, 0)
        _SizeKey1 ("Size Key 1", Vector) = (1, 1, 0, 0)
        _SizeKey2 ("Size Key 2", Vector) = (1, 1, 0, 0)
        _SizeKey3 ("Size Key 3", Vector) = (1, 1, 0, 0)
        _SizeKey4 ("Size Key 4", Vector) = (1, 1, 0, 0)

        _SpriteSpinStartRangeUc ("Start Spin Min Max", Vector) = (0, 0, 0, 0)
        _SpriteSpinSpsRangeUc ("Spin Per Sec Min Max", Vector) = (0, 0, 0, 0)
        _SpriteSpinCcwOrCw ("Spin Direction", Vector) = (0.5, 0.5, 0.5, 0)

        _ColorScaleCount ("Color Scale Key Count", Float) = 2
        _ColorScaleParam ("Color Scale Repeats", Float) = 0
        _ColorKey0 ("Color Key 0", Color) = (1, 1, 1, 1)
        _ColorKey1Time ("Color Key 1 Time", Float) = 1
        _ColorKey1 ("Color Key 1", Color) = (1, 1, 1, 1)
        _ColorKey2Time ("Color Key 2 Time", Float) = 1
        _ColorKey2 ("Color Key 2", Color) = (1, 1, 1, 1)
        _ColorKey3Time ("Color Key 3 Time", Float) = 1
        _ColorKey3 ("Color Key 3", Color) = (1, 1, 1, 1)
        _ColorMulMin ("ColorMultiplier Min RGB", Vector) = (1, 1, 1, 0)
        _ColorMulMax ("ColorMultiplier Max RGB", Vector) = (1, 1, 1, 0)
        _ColorFadeAlphaBlend ("ColorFade AlphaBlend", Float) = 0
        [Toggle] _FadeIn ("Fade In", Float) = 0
        _FadeInEndTime ("Fade In End", Float) = 0
        [Toggle] _Fadeout ("Fade Out", Float) = 0
        _FadeoutStartTime ("Fade Out Start", Float) = 0
        _Opacity ("Opacity", Range(0, 2)) = 1
        _RgbBoost ("RGB Boost", Range(0, 16)) = 1
        [Toggle] _L2SpriteColorGammaToLinear ("Color Gamma To Linear", Float) = 0

        _TextureUSubdivisions ("Atlas U Cells", Float) = 1
        _TextureVSubdivisions ("Atlas V Cells", Float) = 1
        _SubdivisionStart ("Subdivision Start", Float) = 0
        _SubdivisionEnd ("Subdivision End", Float) = 0
        _StaticSubdivision ("Static Subdivision", Float) = 0

        [Toggle] _IgnoreMainTexAlpha ("Ignore Texture Alpha", Float) = 0
        [Toggle] _AlphaFromLuma ("Alpha From Luma", Float) = 0
        _LumaAlphaFloor ("Luma Alpha Floor", Range(0, 1)) = 0
        [Toggle] _UseSoftLumaAlpha ("Soft Luma Alpha", Float) = 0
        _LumaAlphaPower ("Luma Alpha Power", Float) = 1
        _AlphaClipThreshold ("Alpha Clip Threshold (-1 off)", Float) = -1
        [Enum(Off,0,TexAlpha,1,Luma,2,RgbOpaque,3,DarkOpaqueHoles,4)] _DebugSpriteOut ("Debug Sprite Output", Float) = 0

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        [Toggle] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    // Same URP Deferred rule as MeshEmitter: Transparent + UniversalForwardOnly.
    // Geometry + UniversalForward does not draw. Missing mesh texture is not a reason
    // to replace MeshEmitter with this SpriteEmitter shader.
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
            Name "UnifiedSpriteForwardOnly"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "L2FxUnifiedSpriteEmitter.Pass.hlsl"
            ENDHLSL
        }
    }
    FallBack Off
}
