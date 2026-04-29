// Phase 5 / PR 5.A — animated force-field shader for soft-lock barriers.
// Layers:
//   1. Hex-grid pattern in world XY → reads as "energy mesh"
//   2. Vertical scrolling waves → motion / "containment field" feel
//   3. Fresnel rim → bright silhouette edge so the barrier reads from
//      shallow grazing angles, not just dead-on
//   4. Slow pulse on the whole color
// Transparent additive-ish blend, ZWrite Off, two-sided.
Shader "VoidSurvivor/ForceField"
{
    Properties
    {
        [HDR] _BaseColor("Base Color (HDR)", Color) = (1.2, 0.45, 0.1, 0.55)
        [HDR] _RimColor("Rim Color (HDR)", Color) = (3.0, 1.5, 0.4, 1)
        _HexScale("Hex Scale", Float) = 6.0
        _ScrollSpeed("Scroll Speed", Float) = 0.4
        _RimPower("Rim Power", Float) = 2.5
        _PulseSpeed("Pulse Speed", Float) = 1.5
        _WaveFrequency("Wave Frequency", Float) = 4.0
        _WaveSpeed("Wave Speed", Float) = 3.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "ForceField"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 viewDir     : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float  _HexScale;
                float  _ScrollSpeed;
                float  _RimPower;
                float  _PulseSpeed;
                float  _WaveFrequency;
                float  _WaveSpeed;
            CBUFFER_END

            // Distance to nearest hex edge — produces a flat-topped hex grid.
            float hexDistance(float2 p)
            {
                p = abs(p);
                return max(dot(p, normalize(float2(1.0, 1.732))), p.x);
            }

            float hexGrid(float2 uv)
            {
                uv.x *= 0.866025;
                float2 r = float2(1.0, 1.732);
                float2 h = r * 0.5;
                float2 a = fmod(uv, r) - h;
                float2 b = fmod(uv + h, r) - h;
                float da = hexDistance(a);
                float db = hexDistance(b);
                return min(da, db);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.uv          = IN.uv;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDir     = normalize(_WorldSpaceCameraPos - vpi.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // World-space hex pattern so the grid scale stays consistent.
                float2 hexUV = IN.worldPos.xz * _HexScale;
                hexUV.y += _Time.y * _ScrollSpeed;
                float h = hexGrid(hexUV);
                // Edge-emphasis: smooth ring near hex border.
                float hex = smoothstep(0.50, 0.46, h);

                // Vertical scrolling waves on world Y.
                float wave = sin(IN.worldPos.y * _WaveFrequency - _Time.y * _WaveSpeed);
                wave = saturate((wave + 1.0) * 0.5);
                wave = smoothstep(0.55, 1.0, wave);

                // Fresnel rim — bright edges from shallow camera angles.
                float fresnel = 1.0 - saturate(dot(normalize(IN.normalWS), IN.viewDir));
                fresnel = pow(fresnel, _RimPower);

                // Slow color pulse.
                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed);

                half3 baseColor = _BaseColor.rgb * (hex * 0.55 + wave * 0.35 + 0.15);
                half3 rim       = _RimColor.rgb  * fresnel;
                half3 color     = (baseColor + rim) * pulse;

                float alpha = saturate(_BaseColor.a * (hex * 0.55 + wave * 0.45 + fresnel * 0.7));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
