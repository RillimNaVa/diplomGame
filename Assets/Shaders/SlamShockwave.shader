// Phase 5 / PR 5.B — Brute slam impact shockwave.
// Replaces the simple expanding emissive cylinder used by SlamImpactRing.
// Layers:
//   1. Hot core            — bright burst at the impact origin, fades fast.
//   2. Primary shockwave   — thick ring expanding outward (radius = _Progress).
//   3. Secondary ring      — thinner ring trailing the primary at half offset.
//   4. Radial cracks       — N "lightning fingers" radiating outward from
//                            the impact point with per-finger jitter; their
//                            length is gated by _Progress (cracks "grow").
//   5. Outer falloff       — overall intensity fades after _Progress > 0.7.
//
// Polar coordinates derived from world-space distance to the GameObject
// pivot, identical pattern to SlamWarning.shader. Animated by
// SlamImpactRing.cs via _Progress 0 → 1 over the ring's lifetime.
Shader "VoidSurvivor/SlamShockwave"
{
    Properties
    {
        [HDR] _BaseColor("Base Color (HDR)", Color) = (3.5, 1.5, 0.3, 1)
        [HDR] _CrackColor("Crack Color (HDR)", Color) = (5.0, 2.5, 0.6, 1)
        _Progress("Progress 0..1", Range(0,1.05)) = 0
        _RingThickness("Primary Ring Thickness", Range(0.01, 0.3)) = 0.10
        _SecondaryThickness("Secondary Ring Thickness", Range(0.01, 0.2)) = 0.05
        _CrackCount("Crack Count", Float) = 9
        _CrackWidth("Crack Width (rad fraction)", Range(0.005, 0.06)) = 0.02
        _Alpha("Master Alpha", Range(0,1)) = 0.95
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "SlamShockwave"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One   // additive — punches through floor materials
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 pivotWS     : TEXCOORD1;
                float3 axisX       : TEXCOORD2;
                float3 axisZ       : TEXCOORD3;
                float  scaleHorizontal : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _CrackColor;
                float  _Progress;
                float  _RingThickness;
                float  _SecondaryThickness;
                float  _CrackCount;
                float  _CrackWidth;
                float  _Alpha;
            CBUFFER_END

            float hash11(float n) { return frac(sin(n) * 43758.5453); }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.pivotWS = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                OUT.axisX = normalize(mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0)));
                OUT.axisZ = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1)));
                float3 sx = mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0));
                float3 sz = mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1));
                OUT.scaleHorizontal = max(length(sx), length(sz));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 d = IN.worldPos - IN.pivotWS;
                float dx = dot(d, IN.axisX);
                float dz = dot(d, IN.axisZ);
                float halfRadius = max(0.001, IN.scaleHorizontal * 0.5);
                float r = sqrt(dx * dx + dz * dz) / halfRadius;

                clip(1.05 - r);   // discard outside the disc

                float angle = atan2(dz, dx);
                float angleNorm = (angle + 3.14159265) / 6.2831853;  // 0..1

                // 1. Hot core — large at the start, shrinks and dims as ring expands.
                float coreRadius = lerp(0.45, 0.05, _Progress);
                float core = smoothstep(coreRadius, 0.0, r) * (1.0 - _Progress * 0.85);

                // 2. Primary shockwave ring — radius = _Progress.
                float primDist = abs(r - _Progress);
                float primary  = smoothstep(_RingThickness, 0.0, primDist);

                // 3. Secondary trailing ring — slightly behind the primary.
                float secRadius = max(0.0, _Progress - 0.18);
                float secDist   = abs(r - secRadius);
                float secondary = smoothstep(_SecondaryThickness, 0.0, secDist) * 0.6;

                // 4. Radial cracks — N fingers with per-finger angle jitter.
                // Quantize angleNorm into _CrackCount slots; each slot has its
                // own jitter and length factor so the cracks look organic.
                float slot = floor(angleNorm * _CrackCount);
                float angleInSlot = frac(angleNorm * _CrackCount);
                float jitter = (hash11(slot) - 0.5) * 0.08; // small per-finger angular offset
                float distFromCenterAngle = abs((angleInSlot - 0.5) - jitter);
                float crackBand = smoothstep(_CrackWidth, 0.0, distFromCenterAngle);

                // Length per finger (0.6..1.0 of ring radius), gated by _Progress.
                float fingerLen = lerp(0.65, 1.05, hash11(slot + 17.31));
                float crackProgress = saturate(_Progress * 1.25);
                float crackRadialMask = smoothstep(crackProgress * fingerLen + 0.03,
                                                   crackProgress * fingerLen - 0.03, r);
                // Center-side gating — cracks emanate from a small radius outward.
                crackRadialMask *= smoothstep(0.05, 0.15, r);
                float crack = crackBand * crackRadialMask;

                // 5. Overall fade after the ring has traveled most of the disc.
                float fade = 1.0 - smoothstep(0.7, 1.05, _Progress);

                // Combine
                float baseIntensity = (core * 3.5 + primary * 2.2 + secondary * 1.4) * fade;
                float crackIntensity = crack * 3.0 * fade;

                half3 color = _BaseColor.rgb * baseIntensity + _CrackColor.rgb * crackIntensity;
                float alpha = saturate(baseIntensity + crackIntensity * 0.6) * _Alpha;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
