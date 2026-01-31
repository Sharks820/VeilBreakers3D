Shader "VeilBreakers/VFX/FloatingParticles"
{
    Properties
    {
        _MainTex ("Particle Texture (optional)", 2D) = "white" {}
        _Color ("Particle Color", Color) = (1, 0.6, 0.2, 0.6)
        _ScrollSpeed ("Scroll Speed (XY main, ZW secondary)", Vector) = (0.02, -0.03, 0.015, -0.02)
        _Intensity ("Intensity", Range(0, 5)) = 1.0
        _TileScale ("Tile Scale", Range(1, 20)) = 8.0
        _Layer2Scale ("Layer 2 Scale", Range(1, 20)) = 12.0
        _GlowStrength ("Glow Strength", Range(0, 5)) = 1.0
        _FadeEdges ("Fade Edges", Range(0, 0.5)) = 0.1
        _ParticleSize ("Particle Size", Range(0.01, 0.3)) = 0.08
        _ParticleSoftness ("Particle Softness", Range(0.01, 0.5)) = 0.15
        _MaskCenter ("Mask Center (UV)", Vector) = (0.5, 0.45, 0, 0)
        _MaskRadius ("Mask Radius", Range(0, 1)) = 0.85
        _MaskSoftness ("Mask Softness", Range(0.01, 1)) = 0.35
        _MaskPower ("Mask Power", Range(0.1, 4)) = 1.0
        _Density ("Density", Range(0, 1)) = 0.45
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
            "Queue" = "Transparent+75"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One // Additive for glowing embers
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
            float _TileScale;
            float _Layer2Scale;
            float _GlowStrength;
            float _FadeEdges;
            float _ParticleSize;
            float _ParticleSoftness;
            float4 _MaskCenter;
            float _MaskRadius;
            float _MaskSoftness;
            float _MaskPower;
            float _Density;
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

            // Hash function for procedural randomness
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Create a single particle at a grid cell
            float particle(float2 uv, float2 cellId, float time)
            {
                // Random offset within cell
                float2 rand = hash2(cellId);
                float2 particlePos = rand;

                // Animate particle position slightly
                particlePos.x += sin(time * 0.5 + rand.x * 6.28) * 0.1;
                particlePos.y += cos(time * 0.3 + rand.y * 6.28) * 0.1;

                // Distance to particle center
                float dist = length(uv - particlePos);

                // Soft circle with random size variation
                float size = _ParticleSize * (0.5 + rand.x * 0.5);
                float p = 1.0 - smoothstep(size * 0.3, size + _ParticleSoftness, dist);

                // Random brightness
                p *= (0.5 + rand.y * 0.5);

                // Some particles twinkle
                p *= 0.7 + 0.3 * sin(time * (2.0 + rand.x * 3.0));

                return p;
            }

            // Create particle field
            float particleField(float2 uv, float scale, float time)
            {
                float2 scaledUV = uv * scale;
                float2 cellId = floor(scaledUV);
                float2 cellUV = frac(scaledUV);

                float particles = 0.0;

                // Check 3x3 neighborhood for particles
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 neighborId = cellId + neighbor;
                        float2 neighborUV = cellUV - neighbor;

                        particles += particle(neighborUV, neighborId, time);
                    }
                }

                return particles;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = (_UnscaledTime > 0.0) ? _UnscaledTime : _Time.y;

                // Layer 1: Main particles (scrolling)
                float2 uv1 = i.uv + time * _ScrollSpeed.xy;
                float particles1 = particleField(uv1, _TileScale, time);

                // Layer 2: Secondary particles for depth (different scroll)
                float2 uv2 = i.uv + time * _ScrollSpeed.zw;
                float particles2 = particleField(uv2, _Layer2Scale, time * 0.8);

                // Combine layers
                float combined = particles1 + particles2 * 0.6;

                // Normalize-ish so we can control density consistently.
                combined = saturate(combined * 0.35);
                combined = smoothstep(_Density, 1.0, combined);

                // Edge fade
                float edgeFadeX = smoothstep(0.0, _FadeEdges, i.uv.x) * smoothstep(1.0, 1.0 - _FadeEdges, i.uv.x);
                float edgeFadeY = smoothstep(0.0, _FadeEdges, i.uv.y) * smoothstep(1.0, 1.0 - _FadeEdges, i.uv.y);
                float edgeFade = edgeFadeX * edgeFadeY;

                // Radial mask to keep particles focused around the subject (helps readability/AAA composition)
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 centered = (i.uv - _MaskCenter.xy) * float2(aspect, 1.0);
                float dist = length(centered);
                float mask = 1.0 - smoothstep(_MaskRadius, _MaskRadius + _MaskSoftness, dist);
                mask = pow(saturate(mask), _MaskPower);

                // Apply intensity and glow
                float glow = combined * _GlowStrength;
                float alpha = combined * _Intensity * edgeFade * mask * uiClearMask(i.uv) * _Color.a;
                alpha *= readabilityMask(i.uv);

                // Glow brightens the color
                float3 glowColor = _Color.rgb * (1.0 + glow);

                return fixed4(glowColor, saturate(alpha));
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
