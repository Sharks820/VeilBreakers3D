Shader "VeilBreakers/VeilCrack"
{
    Properties
    {
        _CrackProgress ("Crack Progress", Range(0, 1)) = 0
        _CrackColor ("Crack Color (HDR)", Color) = (1, 0.5, 0, 1)
        _CrackWidth ("Crack Width", Range(0.001, 0.05)) = 0.01
        _CrackGlowIntensity ("Glow Intensity", Float) = 5.0
        _ShatterProgress ("Shatter Progress", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 8.0
        _CellScale ("Cell Scale", Float) = 4.0
        _BackgroundAlpha ("Background Alpha", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
        }

        Pass
        {
            Name "VeilCrack"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _CrackProgress;
                float4 _CrackColor;
                float _CrackWidth;
                float _CrackGlowIntensity;
                float _ShatterProgress;
                float _NoiseScale;
                float _CellScale;
                float _BackgroundAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Voronoi hash function
            float2 VoronoiHash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Returns (minDist, edgeDist) where edgeDist = secondDist - minDist
            float2 Voronoi(float2 uv, float scale)
            {
                float2 g = floor(uv * scale);
                float2 f = frac(uv * scale);
                float minDist = 1.0;
                float secondDist = 1.0;
                float2 closestCell = float2(0, 0);

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 cell = VoronoiHash(g + offset);
                        float2 diff = offset + cell - f;
                        float dist = length(diff);
                        if (dist < minDist)
                        {
                            secondDist = minDist;
                            minDist = dist;
                            closestCell = g + offset;
                        }
                        else if (dist < secondDist)
                        {
                            secondDist = dist;
                        }
                    }
                }

                return float2(minDist, secondDist - minDist);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Apply shatter displacement: cells shift outward from center
                if (_ShatterProgress > 0.001)
                {
                    float2 center = float2(0.5, 0.5);
                    float2 dir = normalize(uv - center);
                    float dist = length(uv - center);
                    uv += dir * dist * _ShatterProgress * 0.5;
                }

                // Voronoi edge distance
                float2 voronoi = Voronoi(uv, _CellScale);
                float edgeDist = voronoi.y;

                // Crack mask: cracks become visible as _CrackProgress increases
                float crackThreshold = _CrackWidth * _CrackProgress;
                float crackMask = 1.0 - smoothstep(0, crackThreshold, edgeDist);

                // Only show cracks where progress has reached
                crackMask *= step(0.01, _CrackProgress);

                // Crack glow with HDR intensity
                float3 crackGlow = _CrackColor.rgb * _CrackGlowIntensity * crackMask;

                // Crack alpha
                float crackAlpha = crackMask * _CrackColor.a;

                // Background white-out
                float3 bgColor = float3(1, 1, 1) * _BackgroundAlpha;
                float bgAlpha = _BackgroundAlpha;

                // Composite: crack glow on top of background
                float3 finalColor = crackGlow + bgColor * (1.0 - crackAlpha);
                float finalAlpha = saturate(crackAlpha + bgAlpha);

                // Shatter fragment fade
                if (_ShatterProgress > 0.5)
                {
                    float shatterFade = 1.0 - saturate((_ShatterProgress - 0.5) * 2.0);
                    finalAlpha *= shatterFade;
                }

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
