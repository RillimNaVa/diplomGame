// Phase 5 / PR 5.A — inverted-hull outline shader for staggered enemies.
// Renders the back face of a slightly inflated mesh in a flat HDR color, so a
// halo appears around the silhouette. Cull Front + ZWrite On + ZTest LEqual
// makes the outline draw behind the regular enemy material from the camera's
// side, leaving only the rim pixels visible.
//
// Activated by StaggerOutline.cs — adds this material as a SECOND entry on
// each renderer's materials array when EnemyStagger.OnStaggerChanged(true)
// fires. Removed when the stagger ends or the enemy returns to the pool.
Shader "VoidSurvivor/StaggerOutline"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Color (HDR)", Color) = (3.0, 0.3, 0.1, 1)
        _OutlineWidth("Outline Width (m)", Range(0, 0.15)) = 0.025
        _PulseSpeed("Pulse Speed", Float) = 5.0
        _PulseAmount("Pulse Amount", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 100

        Pass
        {
            Name "OutlineHull"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _PulseSpeed;
                float  _PulseAmount;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Inflate along normal in object space.
                float3 inflated = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                VertexPositionInputs vpi = GetVertexPositionInputs(inflated);
                OUT.positionHCS = vpi.positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pulse = 1.0 - _PulseAmount + _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));
                return half4(_OutlineColor.rgb * pulse, _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
