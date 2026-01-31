Shader "VeilBreakers/VFX/HeatDistortion"
{
    Properties
    {
        _DistortTex ("Distortion Noise (optional)", 2D) = "gray" {}
        _MaskTex ("Distortion Mask (optional)", 2D) = "white" {}
        _DistortStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _ScrollSpeed ("Scroll Speed", Vector) = (0.1, 0.15, 0, 0)
        _TileScale ("Tile Scale", Range(0.5, 8)) = 2.0
        _MaskFalloff ("Mask Falloff", Range(0.1, 2)) = 0.8
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
            "Queue" = "Transparent+200"
            "RenderType" = "Transparent"
        }

        // Grab the screen behind the object
        GrabPass { "_GrabTexture" }

        Pass
        {
            ZWrite Off
            Blend Off

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
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _DistortTex;
            sampler2D _MaskTex;
            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float _DistortStrength;
            float4 _ScrollSpeed;
            float _TileScale;
            float _MaskFalloff;
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

            // 2D smooth noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Procedural distortion noise
            float2 distortionNoise(float2 uv, float time)
            {
                float2 n1 = float2(
                    noise(uv * _TileScale + time * _ScrollSpeed.xy),
                    noise(uv * _TileScale + float2(37.0, 17.0) + time * _ScrollSpeed.xy)
                );

                float2 n2 = float2(
                    noise(uv * _TileScale * 2.0 + time * _ScrollSpeed.xy * 1.5),
                    noise(uv * _TileScale * 2.0 + float2(53.0, 29.0) + time * _ScrollSpeed.xy * 1.5)
                );

                return (n1 + n2 * 0.5) / 1.5;
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
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Create procedural radial mask (heat rises from bottom center)
                float2 centered = i.uv - float2(0.5, 0.3);
                centered.y *= 0.6; // Elongate vertically
                float dist = length(centered);
                float mask = 1.0 - smoothstep(0.0, _MaskFalloff, dist);

                // Bottom fade (heat is stronger at bottom)
                mask *= smoothstep(0.0, 0.4, 1.0 - i.uv.y);

                // Optional mask texture to focus distortion (e.g., chest glow)
                float maskTex = tex2D(_MaskTex, i.uv).r;
                mask *= maskTex;
                mask *= readabilityMask(i.uv);

                // Get procedural distortion
                float2 distort = distortionNoise(i.uv, _Time.y);

                // Convert from 0-1 to -0.5 to 0.5 range
                distort = (distort - 0.5) * 2.0;

                // Apply distortion strength and mask
                float2 offset = distort * _DistortStrength * mask;

                // Sample grabbed screen texture with offset
                float2 grabUV = i.grabPos.xy / i.grabPos.w + offset;
                fixed4 col = tex2D(_GrabTexture, grabUV);

                return col;
            }
            ENDCG
        }
    }

    // Fallback for platforms that don't support GrabPass
    FallBack "Transparent/VertexLit"
}
