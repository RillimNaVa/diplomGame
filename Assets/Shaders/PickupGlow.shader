// Phase 5 / PR 5.C — animated glow shader for world-space pickups (HP orbs).
// Layers:
//   1. Internal rotating beam visible through Fresnel (front faces dim, edges
//      bright) — "core energy"
//   2. Horizontal scrolling scanline so the orb reads as energetic, not static
//   3. Fresnel rim — bright silhouette for readability across distance
//   4. Slow color pulse so the orb breathes
// Transparent additive-leaning blend, ZWrite off, two-sided so the back of
// the sphere shows the rotating beam through the front.
Shader "VoidSurvivor/PickupGlow"
{
    Properties
    {
        [HDR] _BaseColor("Base Color (HDR)", Color) = (0.2, 1.4, 0.6, 0.85)
        [HDR] _RimColor("Rim Color (HDR)", Color) = (0.4, 3.0, 1.2, 1)
        [HDR] _BeamColor("Beam Color (HDR)", Color) = (1.0, 4.5, 2.0, 1)
        _RimPower("Rim Power", Float) = 2.4
        _BeamSpeed("Beam Rotation Speed", Float) = 2.5
        _BeamWidth("Beam Width", Float) = 0.18
        _ScanFrequency("Scanline Frequency", Float) = 8.0
        _ScanSpeed("Scanline Speed", Float) = 1.8
        _PulseSpeed("Pulse Speed", Float) = 1.3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "PickupGlow"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
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
                float3 positionOS  : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float4 _BeamColor;
                float  _RimPower;
                float  _BeamSpeed;
                float  _BeamWidth;
                float  _ScanFrequency;
                float  _ScanSpeed;
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
                OUT.positionOS  = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Rotating internal beam: derive an angle from object-space XZ and
                // animate it with _Time; thin bright stripe at angle 0.
                float3 op = normalize(IN.positionOS);
                float angle = atan2(op.z, op.x);
                float beamPhase = frac((angle / 6.28318) + _Time.y * _BeamSpeed * 0.16);
                // Two beams 180° apart for a dual-vane spinner look.
                float beamA = smoothstep(_BeamWidth, 0.0, abs(beamPhase - 0.25));
                float beamB = smoothstep(_BeamWidth, 0.0, abs(beamPhase - 0.75));
                float beam = max(beamA, beamB);

                // Horizontal scanline on object-space Y so it stays oriented
                // with the orb regardless of camera rotation.
                float scan = sin(IN.positionOS.y * _ScanFrequency - _Time.y * _ScanSpeed);
                scan = saturate(0.5 + 0.5 * scan);
                scan = smoothstep(0.55, 1.0, scan);

                // Fresnel rim — bright outline.
                float fresnel = 1.0 - saturate(dot(normalize(IN.normalWS), IN.viewDir));
                float rim = pow(fresnel, _RimPower);

                // Slow color pulse on the whole result.
                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed);

                half3 baseLayer = _BaseColor.rgb * (0.4 + scan * 0.7);
                half3 beamLayer = _BeamColor.rgb * beam * fresnel;
                half3 rimLayer  = _RimColor.rgb  * rim;
                half3 color     = (baseLayer + beamLayer + rimLayer) * pulse;

                float alpha = saturate(_BaseColor.a * (0.35 + scan * 0.4 + rim * 0.7 + beam * 0.5));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
