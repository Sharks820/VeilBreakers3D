Shader "VeilBreakers/VFX/FloatingParticles"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Particle Color", Color) = (1, 0.6, 0.2, 0.6)
        _ScrollSpeed ("Scroll Speed (XY main, ZW secondary)", Vector) = (0.02, -0.03, 0.015, -0.02)
        _Intensity ("Intensity", Range(0, 2)) = 1.0
        _TileScale ("Tile Scale", Range(1, 8)) = 4.0
        _Layer2Scale ("Layer 2 Scale", Range(1, 8)) = 6.0
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.5
        _FadeEdges ("Fade Edges", Range(0, 0.5)) = 0.1
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Layer 1 - Large particles (foreground)
                float2 uv1 = i.uv * _TileScale + _ScrollSpeed.xy * _Time.y;
                fixed particles1 = tex2D(_MainTex, uv1).r;

                // Layer 2 - Small particles (background, different speed)
                float2 uv2 = i.uv * _Layer2Scale + _ScrollSpeed.zw * _Time.y;
                uv2.x += 0.5; // Offset to prevent overlap
                fixed particles2 = tex2D(_MainTex, uv2).r;

                // Combine layers with different weights
                fixed particles = particles1 * 0.7 + particles2 * 0.5;

                // Threshold to create distinct particles
                particles = smoothstep(0.4, 0.6, particles);

                // Edge fade (optional, for softer edges)
                float edgeFade = 1.0;
                if (_FadeEdges > 0)
                {
                    float2 centered = abs(i.uv - 0.5) * 2.0;
                    float maxDist = max(centered.x, centered.y);
                    edgeFade = 1.0 - smoothstep(1.0 - _FadeEdges * 2, 1.0, maxDist);
                }

                // Apply color, intensity, and glow
                fixed4 result = _Color;
                result.rgb *= (1.0 + particles * _GlowStrength);
                result.a = particles * _Intensity * edgeFade * _Color.a;

                return result;
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
