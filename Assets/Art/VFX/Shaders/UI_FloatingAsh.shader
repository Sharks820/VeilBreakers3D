Shader "VeilBreakers/UI/FloatingAsh"
{
    Properties
    {
        _MainTex ("Ash Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0.4, 0.35, 0.3, 0.5)
        _RiseSpeed ("Rise Speed", Range(0.01, 0.2)) = 0.03
        _DriftSpeed ("Drift Speed", Range(0.01, 0.1)) = 0.02
        _DriftAmount ("Drift Amount", Range(0, 0.2)) = 0.05
        _Intensity ("Intensity", Range(0, 2)) = 0.6
        _FadeBottom ("Fade Bottom", Range(0, 0.3)) = 0.05
        _FadeTop ("Fade Top", Range(0.7, 1)) = 0.95
        _TileX ("Tile X", Range(1, 6)) = 3
        _TileY ("Tile Y", Range(1, 6)) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
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
            float _RiseSpeed;
            float _DriftSpeed;
            float _DriftAmount;
            float _Intensity;
            float _FadeBottom;
            float _FadeTop;
            float _TileX;
            float _TileY;

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
                // Layer 1: Main ash floating up
                float2 uv1 = i.uv * float2(_TileX, _TileY);
                uv1.y -= _Time.y * _RiseSpeed;
                uv1.x += sin(_Time.y * _DriftSpeed * 2.0 + i.uv.y * 5.0) * _DriftAmount;
                uv1.x += cos(_Time.y * _DriftSpeed + i.uv.y * 3.0) * _DriftAmount * 0.5;
                fixed4 ash1 = tex2D(_MainTex, uv1);

                // Layer 2: Secondary ash (slower, different pattern)
                float2 uv2 = i.uv * float2(_TileX * 0.8, _TileY * 0.9) + float2(0.4, 0.3);
                uv2.y -= _Time.y * _RiseSpeed * 0.6;
                uv2.x += sin(_Time.y * _DriftSpeed * 1.5 + i.uv.y * 4.0 + 2.0) * _DriftAmount * 1.3;
                uv2.x += cos(_Time.y * _DriftSpeed * 0.8 + i.uv.y * 2.5 + 1.0) * _DriftAmount * 0.7;
                fixed4 ash2 = tex2D(_MainTex, uv2);

                // Layer 3: Distant/slow ash
                float2 uv3 = i.uv * float2(_TileX * 0.6, _TileY * 0.7) + float2(0.7, 0.6);
                uv3.y -= _Time.y * _RiseSpeed * 0.35;
                uv3.x += sin(_Time.y * _DriftSpeed + i.uv.y * 2.0 + 4.0) * _DriftAmount * 0.6;
                fixed4 ash3 = tex2D(_MainTex, uv3);

                // Combine layers
                float combined = ash1.a * 0.5 + ash2.a * 0.35 + ash3.a * 0.15;

                // Vertical fade
                float vertFade = smoothstep(_FadeBottom, _FadeBottom + 0.15, i.uv.y);
                vertFade *= smoothstep(_FadeTop + 0.05, _FadeTop, i.uv.y);

                // Edge fade
                float edgeFade = smoothstep(0.0, 0.1, i.uv.x) * smoothstep(1.0, 0.9, i.uv.x);

                // Final alpha
                float alpha = combined * _Intensity * vertFade * edgeFade * i.color.a * _Color.a;

                return fixed4(_Color.rgb, saturate(alpha));
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
