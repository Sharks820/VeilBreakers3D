Shader "VeilBreakers/VFX/HeatDistortion"
{
    Properties
    {
        _DistortTex ("Distortion Noise", 2D) = "gray" {}
        _MaskTex ("Distortion Mask", 2D) = "white" {}
        _DistortStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _ScrollSpeed ("Scroll Speed", Vector) = (0.1, 0.15, 0, 0)
        _TileScale ("Tile Scale", Range(0.5, 4)) = 1.0
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
                // Sample mask (controls where distortion appears)
                fixed mask = tex2D(_MaskTex, i.uv).r;

                // Sample distortion noise with animated scroll
                float2 distortUV = i.uv * _TileScale + _ScrollSpeed.xy * _Time.y;
                fixed2 distort = tex2D(_DistortTex, distortUV).rg;

                // Convert from 0-1 to -0.5 to 0.5 range for proper offset
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
    FallBack "Diffuse"
}
