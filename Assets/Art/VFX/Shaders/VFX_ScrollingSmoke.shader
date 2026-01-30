Shader "VeilBreakers/VFX/ScrollingSmoke"
{
    Properties
    {
        _MainTex ("Smoke Texture", 2D) = "white" {}
        _Color ("Smoke Color", Color) = (1, 0.5, 0.2, 0.3)
        _ScrollSpeed ("Scroll Speed", Vector) = (0.05, 0.02, 0, 0)
        _Intensity ("Intensity", Range(0, 10)) = 1.0
        _FadeTop ("Fade Top", Range(0, 1)) = 0.7
        _FadeBottom ("Fade Bottom", Range(0, 1)) = 0.1
        _TileScale ("Tile Scale", Range(0.5, 4)) = 2.0

        // Second layer for depth
        _Layer2Speed ("Layer 2 Speed", Vector) = (0.03, 0.015, 0, 0)
        _Layer2Scale ("Layer 2 Scale", Range(0.5, 4)) = 1.5
        _Layer2Intensity ("Layer 2 Intensity", Range(0, 10)) = 0.5
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Layer 1: Main smoke
                float2 uv1 = i.uv * _TileScale + _Time.y * _ScrollSpeed.xy;
                fixed4 smoke1 = tex2D(_MainTex, uv1);

                // Layer 2: Secondary smoke for depth
                float2 uv2 = i.uv * _Layer2Scale + _Time.y * _Layer2Speed.xy;
                fixed4 smoke2 = tex2D(_MainTex, uv2);

                // Combine layers
                float combined = smoke1.r * _Intensity + smoke2.r * _Layer2Intensity;

                // Vertical fade (stronger at bottom, fades at top)
                float vertFade = smoothstep(_FadeBottom, _FadeTop, i.uv.y);
                vertFade = 1.0 - vertFade; // Invert so smoke is at bottom

                // Edge fade (horizontal)
                float edgeFade = smoothstep(0.0, 0.2, i.uv.x) * smoothstep(1.0, 0.8, i.uv.x);

                // Final alpha
                float alpha = combined * vertFade * edgeFade * _Color.a;

                return fixed4(_Color.rgb, saturate(alpha));
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
