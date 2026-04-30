// Phase 5 / PR 5.C — bullet-impact scorch decal. A flat quad oriented to the
// hit normal. Procedural radial dark gradient + emissive crack lines + alpha
// fade controlled by _Lifetime / _Time. No textures needed.
Shader "VoidSurvivor/BulletImpactDecal"
{
    Properties
    {
        [HDR] _ScorchColor("Scorch Color (HDR)", Color) = (0.05, 0.05, 0.05, 1)
        [HDR] _CrackColor("Crack Color (HDR)", Color) = (1.8, 0.6, 0.15, 1)
        _CrackCount("Crack Count", Range(2, 12)) = 6
        _BirthTime("Birth Time", Float) = 0
        _Lifetime("Lifetime", Float) = 8
        _Radius("Radius (UV space)", Range(0.1, 0.5)) = 0.45
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }

        Pass
        {
            Name "BulletImpact"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            // Push slightly toward the camera so we don't z-fight with the wall.
            Offset -1, -1

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
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ScorchColor;
                float4 _CrackColor;
                float  _CrackCount;
                float  _BirthTime;
                float  _Lifetime;
                float  _Radius;
            CBUFFER_END

            float hash11(float n) { return frac(sin(n) * 43758.5453); }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv - 0.5;
                float r = length(p);

                // Outside the radius — fully transparent.
                if (r > _Radius) return half4(0,0,0,0);

                // Radial dark scorch — strong at center, fades to edge.
                float scorch = smoothstep(_Radius, _Radius * 0.15, r);

                // Cracks: N rays from center. Pick the angle, snap to a sector,
                // and check if we're close to the sector center line.
                float angle = atan2(p.y, p.x);
                float twoPi = 6.28318;
                float sector = twoPi / max(2.0, _CrackCount);
                float sectorIdx = floor((angle + 3.14159) / sector);
                float sectorAngle = sectorIdx * sector - 3.14159 + sector * 0.5;
                // Per-crack length jitter so they don't all reach the rim.
                float crackLen = lerp(0.55, 1.0, hash11(sectorIdx + 1.7));
                float crackBand = abs(angle - sectorAngle);
                crackBand = min(crackBand, twoPi - crackBand);
                // Thinner crack toward the edge.
                float crackThickness = lerp(0.06, 0.012, smoothstep(0.0, _Radius, r));
                float crack = smoothstep(crackThickness, 0.0, crackBand);
                crack *= step(r, _Radius * crackLen);
                // Crack only inside a thin annulus, not at dead center.
                crack *= smoothstep(0.05, 0.12, r);

                // Time-based fade: fully visible for 0..0.6, fade to 0 by lifetime.
                float age = (_Time.y - _BirthTime) / max(0.05, _Lifetime);
                float ageFade = 1.0 - smoothstep(0.6, 1.0, age);

                half3 color = _ScorchColor.rgb * scorch + _CrackColor.rgb * crack;
                float alpha = saturate(scorch * 0.85 + crack * 0.95) * ageFade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
