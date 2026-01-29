Shader "VeilBreakers/VFX/ScrollingSmoke"
{
    Properties
    {
        _MainTex ("Smoke Texture", 2D) = "white" {}
        _Color ("Smoke Color", Color) = (1, 0.5, 0.2, 0.3)
        _ScrollSpeed ("Scroll Speed", Vector) = (0.05, 0.02, 0, 0)
        _Intensity ("Intensity", Range(0, 1)) = 0.5
        _FadeTop ("Fade Top", Range(0, 1)) = 0.7
        _FadeBottom ("Fade Bottom", Range(0, 1)) = 0.1
        _TileScale ("Tile Scale", Range(0.5, 4)) = 2.0

        // Second layer for depth
        _Layer2Speed ("Layer 2 Speed", Vector) = (0.03, 0.015, 0, 0)
        _Layer2Scale ("Layer 2 Scale", Range(0.5, 4)) = 1.5
        _Layer2Intensity ("Layer 2 Intensity", Range(0, 1)) = 0.3
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
                // Layer 1 - Main smoke
                float2 uv1 = i.uv * _TileScale + _ScrollSpeed.xy * _Time.y;
                fixed smoke1 = tex2D(_MainTex, uv1).r;

                // Layer 2 - Background smoke (different speed/scale for parallax)
                float2 uv2 = i.uv * _Layer2Scale + _Layer2Speed.xy * _Time.y;
                fixed smoke2 = tex2D(_MainTex, uv2).r;

                // Combine layers
                fixed smoke = smoke1 * _Intensity + smoke2 * _Layer2Intensity;
                smoke = saturate(smoke);

                // Vertical gradient fade (smoke at bottom fades out at top)
                float fade = smoothstep(_FadeBottom, _FadeTop, i.uv.y);
                fade = 1.0 - fade; // Invert: visible at bottom, fades at top

                // Apply color and fade
                fixed4 result = _Color;
                result.a = smoke * fade * _Color.a;

                return result;
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
