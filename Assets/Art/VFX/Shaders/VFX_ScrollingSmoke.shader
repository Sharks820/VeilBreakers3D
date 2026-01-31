Shader "VeilBreakers/VFX/ScrollingSmoke"
{
    Properties
    {
        _MainTex ("Smoke Texture (optional)", 2D) = "white" {}
        _Color ("Smoke Color", Color) = (1, 0.5, 0.2, 0.3)
        _ScrollSpeed ("Scroll Speed", Vector) = (0.05, 0.02, 0, 0)
        _Intensity ("Intensity", Range(0, 5)) = 1.0
        _FadeTop ("Fade Top", Range(0, 1)) = 0.7
        _FadeBottom ("Fade Bottom", Range(0, 1)) = 0.1
        _TileScale ("Tile Scale", Range(0.5, 8)) = 2.0
        _Layer2Speed ("Layer 2 Speed", Vector) = (0.03, 0.015, 0, 0)
        _Layer2Scale ("Layer 2 Scale", Range(0.5, 8)) = 1.5
        _Layer2Intensity ("Layer 2 Intensity", Range(0, 5)) = 0.5
        _NoiseDetail ("Noise Detail", Range(1, 8)) = 4.0
        _SmokeThickness ("Smoke Thickness", Range(0.1, 2)) = 0.8
        _UnscaledTime ("Unscaled Time", Float) = 0
        _BottomClearStart ("Bottom Clear Start", Range(0, 1)) = 0.10
        _BottomClearEnd ("Bottom Clear End", Range(0, 1)) = 0.28
        _TopClearStart ("Top Clear Start", Range(0, 1)) = 0.70
        _TopClearEnd ("Top Clear End", Range(0, 1)) = 0.90
        _SubjectCenter ("Subject Center (UV)", Vector) = (0.5, 0.42, 0, 0)
        _SubjectRadius ("Subject Radius", Range(0, 1)) = 0.32
        _SubjectSoftness ("Subject Softness", Range(0.01, 1)) = 0.18
        _SubjectStrength ("Subject Strength", Range(0, 1)) = 0.9
        _LogoCenter ("Logo Center (UV)", Vector) = (0.5, 0.88, 0, 0)
        _LogoRadius ("Logo Radius", Range(0, 1)) = 0.2
        _LogoSoftness ("Logo Softness", Range(0.01, 1)) = 0.12
        _LogoStrength ("Logo Strength", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+50"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha // Alpha blend
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ScrollSpeed;
            float _Intensity;
            float _FadeTop;
            float _FadeBottom;
            float _TileScale;
            float4 _Layer2Speed;
            float _Layer2Scale;
            float _Layer2Intensity;
            float _NoiseDetail;
            float _SmokeThickness;
            float _UnscaledTime;
            float _BottomClearStart;
            float _BottomClearEnd;
            float _TopClearStart;
            float _TopClearEnd;
            float4 _SubjectCenter;
            float _SubjectRadius;
            float _SubjectSoftness;
            float _SubjectStrength;
            float4 _LogoCenter;
            float _LogoRadius;
            float _LogoSoftness;
            float _LogoStrength;

            // Hash function for procedural noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // 2D value noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Smooth interpolation
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal Brownian Motion for smoke-like noise
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }

                return value;
            }

            float uiClearMask(float2 uv)
            {
                float bottom = smoothstep(_BottomClearStart, _BottomClearEnd, uv.y);
                float top = 1.0 - smoothstep(_TopClearStart, _TopClearEnd, uv.y);
                return saturate(bottom * top);
            }

            float readabilityMask(float2 uv)
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);

                float2 subj = (uv - _SubjectCenter.xy) * float2(aspect, 1.0);
                float subjDist = length(subj);
                float subjMask = smoothstep(_SubjectRadius, _SubjectRadius + _SubjectSoftness, subjDist);

                float2 logo = (uv - _LogoCenter.xy) * float2(aspect, 1.0);
                float logoDist = length(logo);
                float logoMask = smoothstep(_LogoRadius, _LogoRadius + _LogoSoftness, logoDist);

                float mask = lerp(1.0, subjMask, _SubjectStrength);
                mask *= lerp(1.0, logoMask, _LogoStrength);
                return mask;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = (_UnscaledTime > 0.0) ? _UnscaledTime : _Time.y;

                // Layer 1: Main smoke (scrolling)
                float2 uv1 = i.uv * _TileScale + time * _ScrollSpeed.xy;
                float smoke1 = fbm(uv1 * _NoiseDetail, 4);

                // Layer 2: Secondary smoke for depth
                float2 uv2 = i.uv * _Layer2Scale + time * _Layer2Speed.xy;
                float smoke2 = fbm(uv2 * _NoiseDetail * 0.8, 3);

                // Add some swirling motion
                float2 swirl = float2(
                    sin(i.uv.y * 3.14159 + time * 0.2) * 0.02,
                    cos(i.uv.x * 3.14159 + time * 0.15) * 0.02
                );
                float smoke3 = fbm((i.uv + swirl) * _TileScale * 1.5 + time * _ScrollSpeed.xy * 0.5, 3);

                // Combine layers
                float combined = smoke1 * _Intensity + smoke2 * _Layer2Intensity + smoke3 * 0.3;

                // Apply thickness threshold for more smoke-like appearance
                combined = smoothstep(0.3, 0.3 + _SmokeThickness, combined);

                // Vertical fade (stronger at bottom, fades at top)
                float vertFade = smoothstep(_FadeBottom, _FadeTop, i.uv.y);
                vertFade = 1.0 - vertFade; // Invert so smoke is at bottom

                // Edge fade (horizontal)
                float edgeFade = smoothstep(0.0, 0.2, i.uv.x) * smoothstep(1.0, 0.8, i.uv.x);

                // Final alpha
                float alpha = combined * vertFade * edgeFade * uiClearMask(i.uv) * _Color.a;
                alpha *= readabilityMask(i.uv);

                return fixed4(_Color.rgb, saturate(alpha));
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
