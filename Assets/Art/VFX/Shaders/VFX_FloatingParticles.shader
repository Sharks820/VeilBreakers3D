Shader "VeilBreakers/VFX/FloatingParticles"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Color ("Particle Color", Color) = (1, 0.6, 0.2, 0.6)
        _ScrollSpeed ("Scroll Speed (XY main, ZW secondary)", Vector) = (0.02, -0.03, 0.015, -0.02)
        _Intensity ("Intensity", Range(0, 20)) = 1.0
        _TileScale ("Tile Scale", Range(1, 8)) = 4.0
        _Layer2Scale ("Layer 2 Scale", Range(1, 8)) = 6.0
        _GlowStrength ("Glow Strength", Range(0, 20)) = 0.5
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
                // Layer 1: Main particles
                float2 uv1 = i.uv * _TileScale + _Time.y * _ScrollSpeed.xy;
                fixed4 particles1 = tex2D(_MainTex, uv1);

                // Layer 2: Secondary particles for depth/variation
                float2 uv2 = i.uv * _Layer2Scale + _Time.y * _ScrollSpeed.zw;
                fixed4 particles2 = tex2D(_MainTex, uv2);

                // Combine particles (additive style)
                float combined = particles1.r + particles2.r * 0.6;

                // Edge fade
                float edgeFadeX = smoothstep(0.0, _FadeEdges, i.uv.x) * smoothstep(1.0, 1.0 - _FadeEdges, i.uv.x);
                float edgeFadeY = smoothstep(0.0, _FadeEdges, i.uv.y) * smoothstep(1.0, 1.0 - _FadeEdges, i.uv.y);
                float edgeFade = edgeFadeX * edgeFadeY;

                // Apply intensity and glow
                float glow = combined * _Intensity * _GlowStrength;
                float alpha = combined * _Intensity * edgeFade * _Color.a;

                // Glow brightens the color
                float3 glowColor = _Color.rgb * (1.0 + glow * 0.5);

                return fixed4(glowColor, saturate(alpha));
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
