Shader "VeilBreakers/VFX/Vignette"
{
    Properties
    {
        _MainTex ("Vignette Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0, 0, 0, 1)
        _Intensity ("Intensity", Range(0, 2)) = 1.0
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

        Blend DstColor Zero // Multiply blend
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
                // Sample vignette texture (white center, black edges)
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Pulse effect
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // Invert: black edges become darker when multiplied
                // tex.r = 1 (center) stays white, tex.r = 0 (edge) becomes dark
                float vignette = lerp(1.0, tex.r, _Intensity * pulse);

                // Apply color tint to darkened areas
                fixed3 result = lerp(_Color.rgb, fixed3(1,1,1), vignette);

                return fixed4(result, 1.0);
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
