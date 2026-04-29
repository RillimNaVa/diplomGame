// Phase 5 / PR 5.A — enemy dissolve shader.
// Stylized burn-edge dissolve: clip pixels where 3D world-space noise is below
// _DissolveAmount, plus an emissive halo at the leading edge. Lit by main
// directional light only (Lambert + ambient) so we can keep the shader simple
// — full PBR is overkill for a 1-second death animation.
//
// Animated by EnemyDissolve.cs which sets _DissolveAmount over death duration.
Shader "VoidSurvivor/EnemyDissolve"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _BaseMap("Base Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,0)
        _DissolveAmount("Dissolve Amount", Range(0,1.05)) = 0
        _DissolveEdgeWidth("Edge Width", Range(0.01,0.3)) = 0.08
        [HDR] _DissolveEdgeColor("Edge Color (HDR)", Color) = (3.0, 1.2, 0.2, 1)
        _NoiseScale("Noise Scale", Float) = 4.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _EmissionColor;
                float  _DissolveAmount;
                float  _DissolveEdgeWidth;
                float4 _DissolveEdgeColor;
                float  _NoiseScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // Hash-based 3D noise — cheap, no texture lookup.
            float hash13(float3 p)
            {
                p = frac(p * float3(443.8975, 397.2973, 491.1871));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash13(i + float3(0,0,0));
                float n100 = hash13(i + float3(1,0,0));
                float n010 = hash13(i + float3(0,1,0));
                float n110 = hash13(i + float3(1,1,0));
                float n001 = hash13(i + float3(0,0,1));
                float n101 = hash13(i + float3(1,0,1));
                float n011 = hash13(i + float3(0,1,1));
                float n111 = hash13(i + float3(1,1,1));

                return lerp(
                    lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                    lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y),
                    f.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.uv          = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float n = noise3(IN.worldPos * _NoiseScale);
                float diff = n - _DissolveAmount;
                clip(diff);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo  = baseTex.rgb * _BaseColor.rgb;

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                half3 lit  = albedo * (ndotl * mainLight.color.rgb + half3(0.32, 0.32, 0.36));
                half3 emi  = _EmissionColor.rgb;

                // Edge glow: brighter as we approach the clip threshold.
                float edge = 1.0 - smoothstep(0.0, _DissolveEdgeWidth, diff);
                emi += _DissolveEdgeColor.rgb * edge * 3.5;

                return half4(lit + emi, 1);
            }
            ENDHLSL
        }
    }
}
