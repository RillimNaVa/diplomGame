// Phase 5 / PR 5.C — exit-door portal swirl. Polar-UV swirl pattern + Fresnel
// rim + scrolling color so exit doors read as "portal to next arena" instead
// of a flat emissive panel.
//
// Used by ArenaBuildMaterials.MakeExitPortal for the per-arena exit marker.
Shader "VoidSurvivor/ExitPortal"
{
    Properties
    {
        [HDR] _CoreColor("Core Color (HDR)", Color) = (2.0, 0.4, 0.4, 1)
        [HDR] _OuterColor("Outer Color (HDR)", Color) = (1.0, 0.15, 0.4, 1)
        [HDR] _RimColor("Rim Color (HDR)", Color) = (3.0, 1.0, 1.0, 1)
        _SwirlSpeed("Swirl Speed", Float) = 1.4
        _SwirlTwist("Swirl Twist", Float) = 5.0
        _SwirlBands("Swirl Bands", Float) = 5.0
        _RimPower("Rim Power", Float) = 2.2
        _PulseSpeed("Pulse Speed", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ExitPortal"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On

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
                float4 _CoreColor;
                float4 _OuterColor;
                float4 _RimColor;
                float  _SwirlSpeed;
                float  _SwirlTwist;
                float  _SwirlBands;
                float  _RimPower;
                float  _PulseSpeed;
            CBUFFER_END

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
                // Polar UVs around the door panel. The door visual is a thin
                // box, so UVs run 0..1 across the face; recenter to -0.5..0.5.
                float2 p = IN.uv - 0.5;
                float r = length(p) * 2.0;            // 0..1 from center
                float angle = atan2(p.y, p.x);

                // Swirl: combine angle + twisted radius so spiral arms appear.
                float swirl = frac(angle / 6.28318 + r * _SwirlTwist - _Time.y * _SwirlSpeed);
                // _SwirlBands distinct stripes; smoothed band falloff.
                float band = abs(frac(swirl * _SwirlBands) - 0.5) * 2.0;
                band = smoothstep(0.7, 0.0, band);

                // Color gradient core->outer.
                half3 baseCol = lerp(_CoreColor.rgb, _OuterColor.rgb, smoothstep(0.0, 1.0, r));
                half3 swirlCol = baseCol * (0.4 + band * 0.9);

                // Fresnel rim so the door reads from grazing angles.
                float fresnel = 1.0 - saturate(dot(normalize(IN.normalWS), IN.viewDir));
                fresnel = pow(fresnel, _RimPower);
                half3 rim = _RimColor.rgb * fresnel * 0.55;

                // Slow pulse.
                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed);

                half3 color = (swirlCol + rim) * pulse;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
