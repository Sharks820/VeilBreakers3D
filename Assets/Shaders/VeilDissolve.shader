Shader "VeilBreakers/VeilDissolve"
{
    Properties
    {
        // Standard URP Lit surface properties
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0

        // Dissolve properties
        _DissolveThreshold ("Dissolve Threshold", Range(0, 1)) = 0
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1.0
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.15)) = 0.05
        [HDR] _DissolveEdgeColor ("Edge Color (HDR)", Color) = (1, 0.5, 0, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        LOD 300

        // =====================================================================
        // FORWARD LIT PASS
        // =====================================================================

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // SRP Batcher compatibility
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _BumpScale;
                float4 _NoiseTexture_ST;
                half _DissolveThreshold;
                float _NoiseScale;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                half _EmissionIntensity;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // =========================================================
                // DISSOLVE CLIP
                // =========================================================
                float2 noiseUV = input.uv * _NoiseScale;
                half noise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, noiseUV).r;

                // Hard alpha clip: binary discard, no transparency (prevents z-fighting)
                clip(noise - _DissolveThreshold);

                // =========================================================
                // DISSOLVE EDGE EMISSION
                // =========================================================
                half edge = 1.0 - smoothstep(_DissolveThreshold, _DissolveThreshold + _DissolveEdgeWidth, noise);
                half3 dissolveEmission = _DissolveEdgeColor.rgb * _EmissionIntensity * edge;

                // =========================================================
                // SURFACE DATA
                // =========================================================
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 albedo = baseMap * _BaseColor;

                // Normal mapping
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv),
                    _BumpScale
                );
                half3x3 tangentToWorld = half3x3(
                    input.tangentWS,
                    input.bitangentWS,
                    input.normalWS
                );
                half3 normalWS = normalize(mul(normalTS, tangentToWorld));

                // =========================================================
                // LIGHTING
                // =========================================================
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.fogCoord = input.fogFactor;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = dissolveEmission;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = albedo.a;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }

        // =====================================================================
        // SHADOW CASTER PASS
        // =====================================================================

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // SRP Batcher compatibility (must match ForwardLit CBUFFER exactly)
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _BumpScale;
                float4 _NoiseTexture_ST;
                half _DissolveThreshold;
                float _NoiseScale;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                half _EmissionIntensity;
            CBUFFER_END

            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);

            struct AttributesShadow
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsShadow
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 _LightDirection;

            VaryingsShadow vertShadow(AttributesShadow input)
            {
                VaryingsShadow output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                return output;
            }

            half4 fragShadow(VaryingsShadow input) : SV_Target
            {
                // Dissolve clip in shadow pass so shadows dissolve in sync with geometry
                float2 noiseUV = input.uv * _NoiseScale;
                half noise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, noiseUV).r;
                clip(noise - _DissolveThreshold);

                return 0;
            }
            ENDHLSL
        }

        // =====================================================================
        // DEPTH ONLY PASS
        // =====================================================================

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;
                half _BumpScale;
                float4 _NoiseTexture_ST;
                half _DissolveThreshold;
                float _NoiseScale;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                half _EmissionIntensity;
            CBUFFER_END

            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VaryingsDepth vertDepth(AttributesDepth input)
            {
                VaryingsDepth output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return output;
            }

            half4 fragDepth(VaryingsDepth input) : SV_Target
            {
                float2 noiseUV = input.uv * _NoiseScale;
                half noise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, noiseUV).r;
                clip(noise - _DissolveThreshold);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
