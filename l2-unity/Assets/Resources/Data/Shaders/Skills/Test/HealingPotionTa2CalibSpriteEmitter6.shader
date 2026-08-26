// it_healing_potion_ta_2 / m_u008_b SpriteEmitter6 — IDA-verified calib shader.

// sizeUU = StartSize(UU) * SizeScale(t)  [diameter in UU]

// sizeInMeters = sizeUU / 52.5

// sizeM via L2Fx_GetFinalVertexSizeMeters (global SpriteEmitter K = 1.1).

Shader "L2/Effects/Calib/HealingPotionTa2SpriteEmitter6"

{

    Properties

    {

        _MainTex ("Texture", 2D) = "white" {}



        _L2FxWorldCalibration ("World Calibration K", Float) = 1.1

        _SizeRange ("Start Size UU Min Max", Vector) = (2, 2, 0, 0)



        _TestSizeScaleAge ("SizeScale Age 0-1", Range(0, 1)) = 0.25

        _SizeScaleRepeats ("SizeScale Repeats", Float) = 1

        _SizeKey0 ("Size Key 0 Time Size", Vector) = (0.1, 2, 0, 0)

        _SizeKey1 ("Size Key 1 Time Size", Vector) = (0.18, 1, 0, 0)

        _SizeKey2 ("Size Key 2 Time Size", Vector) = (0.41, 0.7, 0, 0)

        _SizeKey3 ("Size Key 3 Time Size", Vector) = (1, 0.01, 0, 0)

        _SizeKey4 ("Size Key 4 Time Size", Vector) = (1, 0.01, 0, 0)



        _TextureUSubdivisions ("Atlas U Cells", Float) = 4

        _TextureVSubdivisions ("Atlas V Cells", Float) = 4

        _ManualFrameIndex ("Manual Frame Index", Float) = 15

        _SubdivisionStart ("Subdivision Start", Float) = 14

        _SubdivisionEnd ("Subdivision End", Float) = 16



        _RgbBoost ("RGB Boost", Range(0, 16)) = 7

        _LumaAlphaFloor ("Luma Alpha Floor", Range(0, 0.25)) = 0.003

    }



    SubShader

    {

        Tags

        {

            "RenderPipeline" = "UniversalPipeline"

            "Queue" = "Transparent"

            "RenderType" = "Transparent"

                    "L2FxGpuInstancing" = "On"
        }



        Blend One One

        Cull Off

        ZWrite Off

        ZTest LEqual



        Pass

        {

            Name "HealingPotionTa2CalibSE6"

            Tags { "LightMode" = "UniversalForward" }



            HLSLPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma target 3.0



            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "../Common/L2FxCoreGeometryTest.hlsl"

            #include "../Common/L2FxFlipbook.hlsl"



            TEXTURE2D(_MainTex);

            SAMPLER(sampler_MainTex);



            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float _L2FxWorldCalibration;

                float4 _SizeRange;

                float _TestSizeScaleAge;

                float _SizeScaleRepeats;

                float4 _SizeKey0;

                float4 _SizeKey1;

                float4 _SizeKey2;

                float4 _SizeKey3;

                float4 _SizeKey4;

                float _TextureUSubdivisions;

                float _TextureVSubdivisions;

                float _ManualFrameIndex;

                float _SubdivisionStart;

                float _SubdivisionEnd;

                float _RgbBoost;

                float _LumaAlphaFloor;

            CBUFFER_END

            #include "../Common/L2FxInstancing.hlsl"



            float EvaluateDynamicSizeScale(float progress)

            {

                float phase = frac(progress * _SizeScaleRepeats);



                float4 keys[5] = { _SizeKey0, _SizeKey1, _SizeKey2, _SizeKey3, _SizeKey4 };



                if (keys[0].x > 0.0 && phase < keys[0].x)

                {

                    return lerp(1.0, keys[0].y, phase / max(keys[0].x, 1e-6));

                }



                int idx = 0;

                while (idx < 4 && phase > keys[idx + 1].x)

                {

                    idx++;

                }



                float t0 = keys[idx].x;

                float s0 = keys[idx].y;

                float t1 = keys[idx + 1].x;

                float s1 = keys[idx + 1].y;



                if (abs(t1 - t0) < 1e-6)

                {

                    return s0;

                }



                float u = (phase - t0) / (t1 - t0);

                return lerp(s0, s1, saturate(u));

            }



            float ResolveStartSizeUU()

            {

                float minUU = _SizeRange.x;

                float maxUU = _SizeRange.y;

                if (maxUU < minUU)

                {

                    float t = minUU;

                    minUU = maxUU;

                    maxUU = t;

                }

                return (minUU + maxUU) * 0.5;

            }



            struct Attributes

            {

                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID

            };



            struct Varyings

            {

                float4 positionHCS : SV_POSITION;

                float2 uvAtlas : TEXCOORD0;

            };



            Varyings vert(Attributes IN)

            {
                UNITY_SETUP_INSTANCE_ID(IN);

                Varyings OUT;



                float sizeUU = ResolveStartSizeUU() * EvaluateDynamicSizeScale(_TestSizeScaleAge);

                float sizeM = L2Fx_GetFinalVertexSizeMeters(sizeUU, _L2FxWorldCalibration);

                float3 quadOS = IN.positionOS.xyz * sizeM;

                OUT.positionHCS = TransformObjectToHClip(float4(quadOS, 1.0));



                int uSub = max(1, (int)_TextureUSubdivisions);

                int vSub = max(1, (int)_TextureVSubdivisions);

                int s0 = (int)_SubdivisionStart;

                int s1 = (int)_SubdivisionEnd;

                int lo = min(s0, s1);

                int hi = max(s0, s1);

                int frame = clamp((int)round(_ManualFrameIndex), lo, hi);

                OUT.uvAtlas = L2Fx_FlipbookAtlasUV(IN.uv, frame, uSub, vSub);

                return OUT;

            }



            half4 frag(Varyings IN) : SV_Target

            {

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvAtlas);

                float lum = dot(tex.rgb, float3(0.299, 0.587, 0.114));

                float mask = saturate((lum - _LumaAlphaFloor) / max(1.0 - _LumaAlphaFloor, 1e-4));

                half3 rgb = tex.rgb * (half)_RgbBoost * (half)mask;

                return half4(saturate(rgb), 1.0);

            }

            ENDHLSL

        }

    }



    FallBack Off

}

