// ============================================================================
// Lineage 2 Smoke Particle Shader (ported from SPIR-V 1.3)
// Original: FF_FS_e9e1c76173292b7edf514c51e164caffc887a21b
// ============================================================================
Shader "Lineage2/Smoke"
{
    Properties
    {
        [Header(Textures)]
        _MainTex ("Smoke Texture A", 2D) = "white" {}
        _Tex1    ("Smoke Texture B", 2D) = "white" {}

        [Header(Fog)]
        [KeywordEnum(None, Exp, Exp2, Linear)] _FogMode ("Fog Mode", Float) = 2
        _FogColor   ("Fog Color",     Color) = (0.5, 0.5, 0.5, 1)
        _FogDensity ("Fog Density",   Range(0, 1)) = 0.05
        _FogEnd     ("Fog End",       Float) = 80
        _FogScale   ("Fog Scale",     Float) = 0.02

        [Header(Alpha Test)]
        [KeywordEnum(Never, Less, Equal, LEqual, Greater, NotEqual, GEqual, Always)]
        _AlphaTest  ("Alpha Test Func", Float) = 1
        _AlphaRef   ("Alpha Reference",  Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #pragma multi_compile _FOGMODE_NONE _FOGMODE_EXP _FOGMODE_EXP2 _FOGMODE_LINEAR
            #pragma multi_compile _ALPHATEST_NEVER _ALPHATEST_LESS _ALPHATEST_EQUAL _ALPHATEST_LEQUAL _ALPHATEST_GREATER _ALPHATEST_NOTEQUAL _ALPHATEST_GEQUAL _ALPHATEST_ALWAYS

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : TEXCOORD1;
                float4 clipPos  : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _Tex1;
            float4    _MainTex_ST;

            float3 _FogColor;
            float  _FogDensity;
            float  _FogEnd;
            float  _FogScale;
            float  _AlphaRef;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos     = UnityObjectToClipPos(v.vertex);
                o.uv      = TRANSFORM_TEX(v.uv, _MainTex);
                o.color   = v.color;
                o.clipPos = o.pos;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // ── Dual‑texture sample ───────────────────────────────────
                float4 tex0 = tex2D(_MainTex, i.uv);
                float4 tex1 = tex2D(_Tex1,    i.uv);

                // ── Blend textures by vertex alpha ────────────────────────
                float mixFactor = i.color.a;
                float4 blended  = lerp(tex0, tex1, mixFactor);

                // ── Tint by vertex color, preserve texture alpha ──────────
                float4 result;
                result.rgb = i.color.rgb * blended.rgb;
                result.a   = blended.a;

                // ── Per‑pixel fog (D3D9 fixed‑function table) ────────────
                float zOverW = i.clipPos.z / i.clipPos.w;
                float fogFactor;

                #if defined(_FOGMODE_EXP)
                    fogFactor = exp(-zOverW * _FogDensity);
                #elif defined(_FOGMODE_EXP2)
                    float d = zOverW * _FogDensity;
                    fogFactor = exp(-d * d);
                #elif defined(_FOGMODE_LINEAR)
                    fogFactor = saturate((_FogEnd - zOverW) * _FogScale);
                #else
                    fogFactor = 1.0;
                #endif

                result.rgb = lerp(_FogColor, result.rgb, fogFactor);

                // ── Alpha test (D3DCMPFUNC modes 0‑7) ────────────────────
                #if !defined(_ALPHATEST_ALWAYS)
                {
                    bool bDiscard;

                    #if defined(_ALPHATEST_NEVER)
                        bDiscard = true;
                    #elif defined(_ALPHATEST_LESS)
                        bDiscard = !(result.a < _AlphaRef);
                    #elif defined(_ALPHATEST_EQUAL)
                        bDiscard = !(result.a == _AlphaRef);
                    #elif defined(_ALPHATEST_LEQUAL)
                        bDiscard = !(result.a <= _AlphaRef);
                    #elif defined(_ALPHATEST_GREATER)
                        bDiscard = !(result.a > _AlphaRef);
                    #elif defined(_ALPHATEST_NOTEQUAL)
                        bDiscard = !(result.a != _AlphaRef);
                    #elif defined(_ALPHATEST_GEQUAL)
                        bDiscard = !(result.a >= _AlphaRef);
                    #else
                        bDiscard = false;
                    #endif

                    if (bDiscard)
                        discard;
                }
                #endif

                return result;
            }
            ENDHLSL
        }
    }

    Fallback "Mobile/Particles/Alpha Blended"
}