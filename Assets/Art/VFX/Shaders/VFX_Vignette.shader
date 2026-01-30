Shader "VeilBreakers/VFX/Vignette"
{
    Properties
    {
        _MainTex ("Vignette Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0, 0, 0, 1)
        _Intensity ("Intensity", Range(0, 10)) = 1.0
        _PulseSpeed ("Pulse Speed", Range(0, 2)) = 0.0
        _PulseAmount ("Pulse Amount", Range(0, 0.3)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha // Alpha blend (works on black backgrounds)
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
            float _Intensity;
            float _PulseSpeed;
            float _PulseAmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture for vignette shape
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Calculate radial distance from center
                float2 centered = (i.uv - 0.5) * 2.0;
                float dist = length(centered);

                // Pulse effect
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // Create vignette (procedural if no texture)
                float vignette = smoothstep(0.3, 1.0, dist) * _Intensity * pulse;

                // Combine with texture (use texture alpha if available)
                vignette = lerp(vignette, tex.a * _Intensity * pulse, tex.a > 0.01 ? 0.5 : 0.0);

                // Output
                return fixed4(_Color.rgb, saturate(vignette * _Color.a));
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
