// m_u008_b SpriteEmitter1 flash: PTDS_AlphaBlend, fx_m_t0054, PTDU_Normal.

// D3D9 FS parity: out = (1,1,1,tex.a) * vertexColor; premultiplied alpha blend.

Shader "L2/Effects/ShieldTaFlash"

{

    Properties

    {

        _MainTex ("Texture", 2D) = "white" {}



        _StartTime ("Start Time", Float) = 0

        [Toggle] _HasLifetime ("Has Lifetime", Float) = 1

        _InitialDelayRange ("Initial Delay Range (Min,Max) sec", Vector) = (0.5, 0.5, 0, 0)

        _LifetimeRange ("Lifetime Range (Min,Max) sec", Vector) = (2.5, 2.5, 0, 0)

        _Seed ("Seed", Float) = 0



        [Toggle] _FadeIn ("Fade In", Float) = 1

        _FadeInEndTime ("FadeIn End Time (sec)", Float) = 1.025

        [Toggle] _Fadeout ("Fade Out", Float) = 1

        _FadeoutStartTime ("FadeOut Start Time (sec)", Float) = 1.975



        _Opacity ("Opacity", Range(0, 2)) = 0.44

        [Toggle] _bAlphaBlend ("AlphaBlend ColorScale", Float) = 1

        _ColorScaleRepeats ("ColorScale Repeats", Float) = 1

        _ColorScaleCount ("ColorScale Count", Int) = 2

        _ColorScale0 ("ColorScale[0]", Color) = (1, 1, 1, 1)

        _ColorScaleTime1 ("ColorScale Time[1]", Range(0, 1)) = 1

        _ColorScale1 ("ColorScale[1]", Color) = (1, 1, 1, 1)

        _ColorScaleTime2 ("ColorScale Time[2]", Range(0, 1)) = 1

        _ColorScale2 ("ColorScale[2]", Color) = (1, 1, 1, 1)

        _ColorMultMin ("ColorMult Min", Color) = (1, 1, 0.905, 1)

        _ColorMultMax ("ColorMult Max", Color) = (1, 1, 0.905, 1)



        _SizeRange ("Start Size UU (Min,Max)", Vector) = (20, 20, 0, 0)

        _BillboardScale ("Manual Billboard Scale (0 = object scale)", Float) = 0.018125

        _SurfaceNormals ("Surface Normal (0 = object up / UE 0,0,1)", Vector) = (0, 0, 0, 0)

        [Toggle] _UniformSize ("Uniform Size", Float) = 1



        _TextureUSubdivisions ("Texture U Subdivisions", Float) = 4

        _TextureVSubdivisions ("Texture V Subdivisions", Float) = 4

        _SubdivisionStart ("Subdivision Start", Float) = 2

        _SubdivisionEnd ("Subdivision End", Float) = 3

        [Toggle] _BlendBetweenSubdivisions ("Blend Between Subdivisions", Float) = 1



        [Header(Debug)]

        [Toggle] _DebugAtlasPreview ("Debug Atlas Preview (show selected cell)", Float) = 0

        _DebugAtlasPreviewAlpha ("Debug Preview Alpha", Range(0, 1)) = 0.85

        _DebugAtlasPreviewBoost ("Debug Preview RGB Boost", Range(0.25, 8)) = 1

        _DebugAtlasBackground ("Debug Preview Background", Color) = (0.03, 0.04, 0.08, 1)

    }



    SubShader

    {

        Tags

        {

            "RenderPipeline" = "UniversalPipeline"

            "Queue" = "Transparent"

            "RenderType" = "Transparent"

        }



        Blend One OneMinusSrcAlpha

        Cull Off

        ZWrite Off

        ZTest LEqual



        Pass

        {

            Name "ShieldTaFlashAlpha"

            Tags { "LightMode" = "UniversalForward" }



            HLSLPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #pragma target 3.0



            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "../Common/L2FxEmitterSpawn.hlsl"

            #include "../Common/L2FxFlipbook.hlsl"

            #include "../Common/L2FxSpriteEmitterVertex.hlsl"

            #include "../Common/L2FxParticleAnim.hlsl"

            #include "../Common/L2FxAtlasDebug.hlsl"



            TEXTURE2D(_MainTex);

            SAMPLER(sampler_MainTex);



            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float _StartTime;

                float _HasLifetime;

                float4 _InitialDelayRange;

                float4 _LifetimeRange;

                float _Seed;

                float _FadeIn;

                float _FadeInEndTime;

                float _Fadeout;

                float _FadeoutStartTime;

                float _Opacity;

                float _bAlphaBlend;

                float _ColorScaleRepeats;

                uint _ColorScaleCount;

                float4 _ColorScale0;

                float _ColorScaleTime1;

                float4 _ColorScale1;

                float _ColorScaleTime2;

                float4 _ColorScale2;

                float4 _ColorMultMin;

                float4 _ColorMultMax;

                float4 _SizeRange;

                float _BillboardScale;

                float4 _SurfaceNormals;

                float _UniformSize;

                float _TextureUSubdivisions;

                float _TextureVSubdivisions;

                float _SubdivisionStart;

                float _SubdivisionEnd;

                float _BlendBetweenSubdivisions;

                float _DebugAtlasPreview;

                float _DebugAtlasPreviewAlpha;

                float _DebugAtlasPreviewBoost;

                float4 _DebugAtlasBackground;

            CBUFFER_END



            struct Attributes

            {

                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float2 uv : TEXCOORD0;

            };



            struct Varyings

            {

                float4 positionHCS : SV_POSITION;

                float2 uvAtlasA : TEXCOORD0;

                float2 uvAtlasB : TEXCOORD1;

                float4 tint : COLOR;

                nointerpolation float particleSeed : TEXCOORD2;

                float flipbookBlend : TEXCOORD3;

            };



            Varyings vert(Attributes IN)

            {

                Varyings OUT;

                float pSeed = L2Fx_SpriteMaterialSeed(_Seed);

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);

                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);

                float age = L2Fx_AgeSeconds(_Time.y, _StartTime, delay);

                float ageNorm = saturate(age / max(lifetime, 1e-4));

                OUT.particleSeed = pSeed;



                float3 baseSize = L2Fx_StartSize(

                    _SizeRange.xy,

                    _SizeRange.xy,

                    _SizeRange.xy,

                    _UniformSize > 0.5,

                    pSeed,

                    _StartTime);



                float3 quadOS = IN.positionOS.xyz * baseSize;

                float3 centerWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

                float3 posWS = L2Fx_PtduNormalPositionWS(

                    centerWS,

                    quadOS,

                    _BillboardScale,

                    _SurfaceNormals.xyz);



                OUT.positionHCS = TransformWorldToHClip(posWS);



                float2 quadUv = TRANSFORM_TEX(IN.uv, _MainTex);

                int uSub = max(1, (int)_TextureUSubdivisions);

                int vSub = max(1, (int)_TextureVSubdivisions);

                int s0 = (int)_SubdivisionStart;

                int s1 = (int)_SubdivisionEnd;

                float fBlend = 0.0;

                float2 uvA;

                float2 uvB;



                if (_BlendBetweenSubdivisions > 0.5)

                {

                    int fa;

                    int fb;

                    L2Fx_FlipbookBlendFrames(ageNorm, s0, s1, fa, fb, fBlend);

                    uvA = L2Fx_FlipbookAtlasUV(quadUv, fa, uSub, vSub);

                    uvB = L2Fx_FlipbookAtlasUV(quadUv, fb, uSub, vSub);

                }

                else

                {

                    int fi = L2Fx_FlipbookFrameIndex(ageNorm, s0, s1);

                    uvA = L2Fx_FlipbookAtlasUV(quadUv, fi, uSub, vSub);

                    uvB = uvA;

                }



                OUT.uvAtlasA = uvA;

                OUT.uvAtlasB = uvB;

                OUT.flipbookBlend = fBlend;



                float ctimes[8];

                float4 ccols[8];

                L2Fx_BuildColorScaleArrays5(

                    _ColorScaleCount,

                    _ColorScale0,

                    _ColorScaleTime1, _ColorScale1,

                    _ColorScaleTime2, _ColorScale2,

                    1.0, float4(1, 1, 1, 1),

                    1.0, float4(1, 1, 1, 1),

                    ctimes,

                    ccols);



                float csParam = max(_ColorScaleRepeats, 1.0) - 1.0;

                float4 cs = L2Fx_SampleColorScale(

                    ageNorm,

                    csParam,

                    _ColorScaleCount,

                    ctimes,

                    ccols,

                    _bAlphaBlend > 0.5);



                float4 colorMult = lerp(_ColorMultMin, _ColorMultMax, L2Fx_Hash11(pSeed * 19.0 + _StartTime));

                OUT.tint = float4(cs.rgb * colorMult.rgb, cs.a);



                return OUT;

            }



            half4 frag(Varyings IN) : SV_Target

            {

                float pSeed = IN.particleSeed;

                float delay = L2Fx_RandomInitialDelay(_InitialDelayRange.xy, pSeed, _StartTime, 3.0);

                float lifetime = L2Fx_RandomLifetime(_LifetimeRange.xy, pSeed, _StartTime, 7.0);



                half4 texA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasA);

                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlasB);



                texA = half4(1.0, 1.0, 1.0, texA.a);

                texB = half4(1.0, 1.0, 1.0, texB.a);



                half4 finalColor = lerp(texA, texB, (half)IN.flipbookBlend);

                finalColor *= (half4)IN.tint;



                if (_DebugAtlasPreview > 0.5)

                {

                    return L2Fx_AtlasDebugPreviewColor(

                        finalColor,

                        finalColor.a,

                        _DebugAtlasPreviewAlpha,

                        _DebugAtlasPreviewBoost,

                        _DebugAtlasBackground);

                }



                float lifeAlpha = L2Fx_LifetimeAlpha(

                    _Time.y, _HasLifetime, _StartTime, delay, lifetime,

                    _FadeIn, _FadeInEndTime, _Fadeout, _FadeoutStartTime);



                finalColor.a *= (half)(_Opacity * lifeAlpha);

                finalColor.rgb *= finalColor.a;

                return half4(saturate(finalColor.rgb), saturate(finalColor.a));

            }



            ENDHLSL

        }

    }

}


