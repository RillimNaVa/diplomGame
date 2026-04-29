// Phase 5 / PR 5.B — Brute slam warning rune.
// Replaces the simple runtime emissive cylinder used by BruteSlamDecal during
// the 0.9s wind-up. Layers:
//   1. Outer ring  — always visible, defines the slam radius.
//   2. Inner cross — pulsing, breathes in/out so the rune reads as "alive".
//   3. Sweep arc   — clockwise countdown that fills the ring as _Progress
//                    goes 0 → 1 (slam impact when it hits 1.0).
//   4. Rotating tick marks — small rotating notches inside the outer ring.
//
// Polar coordinates derived from world-space distance to the GameObject pivot,
// so the pattern is geometry-agnostic — works on a flattened cylinder or any
// other flat-on-floor mesh.
//
// Animated by BruteSlamDecal.cs which sets _Progress over telegraphTime.
Shader "VoidSurvivor/SlamWarning"
{
    Properties
    {
        [HDR] _BaseColor("Base Color (HDR)", Color) = (3.0, 1.0, 0.15, 1)
        _Progress("Progress 0..1", Range(0,1)) = 0
        _PulseSpeed("Pulse Speed", Float) = 6.0
        _RotationSpeed("Rotation Speed", Float) = 1.4
        _RingThickness("Outer Ring Thickness", Range(0.005, 0.1)) = 0.04
        _SweepThickness("Sweep Thickness", Range(0.05, 0.4)) = 0.18
        _Alpha("Master Alpha", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "SlamWarning"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One   // additive-ish — pops on dark floors
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
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
                float  _Progress;
                float  _PulseSpeed;
                float  _RotationSpeed;
                float  _RingThickness;
                float  _SweepThickness;
                float  _Alpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                // Pivot world position (object origin in world space).
                OUT.pivotWS = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                // Use object-X and object-Z axes in world space so polar
                // coordinates rotate with the GameObject.
                OUT.axisX = normalize(mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0)));
                OUT.axisZ = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1)));
                // Approximate horizontal scale = max of x/z lossy scale.
                float3 sx = mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0));
                float3 sz = mul((float3x3)unity_ObjectToWorld, float3(0, 0, 1));
                OUT.scaleHorizontal = max(length(sx), length(sz));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Local-space offset from pivot in the XZ plane.
                float3 d = IN.worldPos - IN.pivotWS;
                float dx = dot(d, IN.axisX);
                float dz = dot(d, IN.axisZ);
                // Normalize to unit disc (the cylinder primitive has radius 0.5
                // in object-space, so scaleHorizontal is half-the-world-radius).
                float r = sqrt(dx * dx + dz * dz) / max(0.001, IN.scaleHorizontal * 0.5);

                // Discard outside disc and the central hole.
                clip(0.99 - r);

                float angle = atan2(dz, dx);                 // -PI..PI
                float angleNorm = (angle + 3.14159265) / 6.2831853;  // 0..1

                // 1. Outer ring — solid line right at the boundary.
                float outerRing = smoothstep(0.96 - _RingThickness, 0.97, r)
                                * smoothstep(1.0,  0.96, r);

                // 2. Inner "X" cross with pulse breathing.
                float pulse = 0.65 + 0.35 * sin(_Time.y * _PulseSpeed);
                float crossX = smoothstep(0.04, 0.0, abs(dx) / max(0.001, IN.scaleHorizontal * 0.5));
                float crossZ = smoothstep(0.04, 0.0, abs(dz) / max(0.001, IN.scaleHorizontal * 0.5));
                float innerCross = max(crossX, crossZ);
                innerCross *= smoothstep(0.45, 0.05, r); // limit to inner area
                innerCross *= pulse;

                // 3. Sweep arc — clockwise countdown that fills the band as
                // _Progress 0 → 1. Renders only between 0.65 and 0.94 radius.
                float bandMask = smoothstep(0.65, 0.7, r) * smoothstep(0.94, 0.90, r);
                float sweepEdge = smoothstep(_Progress + 0.015, _Progress - 0.015, angleNorm);
                float sweep = sweepEdge * bandMask;

                // 4. Rotating tick marks (12 short notches around the ring).
                float rotAngleNorm = frac(angleNorm + _Time.y * _RotationSpeed * 0.1);
                float ticks = step(0.45, frac(rotAngleNorm * 12.0));
                ticks *= step(frac(rotAngleNorm * 12.0), 0.55);
                ticks *= smoothstep(0.85, 0.88, r) * smoothstep(0.93, 0.90, r);

                // Combine layers
                float intensity = outerRing * 1.4
                                + innerCross * 2.5
                                + sweep      * 2.8
                                + ticks      * 1.2;

                // Brightness ramp toward impact frame.
                intensity *= lerp(0.7, 1.5, _Progress);

                half3 color = _BaseColor.rgb * intensity;
                float alpha = saturate(intensity) * _Alpha;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
