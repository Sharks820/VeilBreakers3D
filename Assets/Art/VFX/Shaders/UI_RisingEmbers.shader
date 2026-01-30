Shader "VeilBreakers/UI/RisingEmbers"
{
    Properties
    {
        _MainTex ("Ember Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 0.5, 0.1, 1)
        _Speed ("Rise Speed", Range(0.01, 0.5)) = 0.1
        _Sway ("Horizontal Sway", Range(0, 0.1)) = 0.02
        _SwaySpeed ("Sway Speed", Range(0.5, 3)) = 1.0
        _Intensity ("Intensity", Range(0, 3)) = 1.0
        _FadeBottom ("Fade Bottom", Range(0, 0.5)) = 0.1
        _FadeTop ("Fade Top", Range(0.5, 1)) = 0.9
        _TileX ("Tile X", Range(1, 5)) = 2
        _TileY ("Tile Y", Range(1, 5)) = 3
        _Glow ("Glow Amount", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend One One // Additive blending for glow
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Speed;
            float _Sway;
            float _SwaySpeed;
            float _Intensity;
            float _FadeBottom;
            float _FadeTop;
            float _TileX;
            float _TileY;
            float _Glow;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Layer 1: Main embers rising
                float2 uv1 = i.uv * float2(_TileX, _TileY);
                uv1.y -= _Time.y * _Speed;
                uv1.x += sin(_Time.y * _SwaySpeed + i.uv.y * 6.28) * _Sway;
                fixed4 embers1 = tex2D(_MainTex, uv1);

                // Layer 2: Secondary embers (different speed/offset)
                float2 uv2 = i.uv * float2(_TileX * 0.7, _TileY * 0.8) + float2(0.33, 0.5);
                uv2.y -= _Time.y * _Speed * 0.7;
                uv2.x += sin(_Time.y * _SwaySpeed * 1.3 + i.uv.y * 4.5 + 1.5) * _Sway * 1.2;
                fixed4 embers2 = tex2D(_MainTex, uv2);

                // Layer 3: Slow distant embers
                float2 uv3 = i.uv * float2(_TileX * 0.5, _TileY * 0.6) + float2(0.66, 0.25);
                uv3.y -= _Time.y * _Speed * 0.4;
                uv3.x += sin(_Time.y * _SwaySpeed * 0.8 + i.uv.y * 3.14 + 3.0) * _Sway * 0.8;
                fixed4 embers3 = tex2D(_MainTex, uv3);

                // Combine layers
                float combined = embers1.a + embers2.a * 0.6 + embers3.a * 0.3;

                // Vertical fade (spawn at bottom, fade at top)
                float vertFade = smoothstep(_FadeBottom, _FadeBottom + 0.2, i.uv.y);
                vertFade *= smoothstep(_FadeTop + 0.1, _FadeTop, i.uv.y);

                // Edge fade
                float edgeFade = smoothstep(0.0, 0.15, i.uv.x) * smoothstep(1.0, 0.85, i.uv.x);

                // Final alpha
                float alpha = combined * _Intensity * vertFade * edgeFade * i.color.a;

                // Glow effect - brighter center
                float3 glowColor = _Color.rgb * (1.0 + combined * _Glow);

                return fixed4(glowColor * alpha, alpha);
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
