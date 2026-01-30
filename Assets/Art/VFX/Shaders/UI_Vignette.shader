Shader "VeilBreakers/UI/Vignette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Vignette Color", Color) = (0, 0, 0, 0.8)
        _Intensity ("Intensity", Range(0, 5)) = 1.5
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.5
        _PulseSpeed ("Pulse Speed", Range(0, 3)) = 0.5
        _PulseAmount ("Pulse Amount", Range(0, 0.3)) = 0.1

        // UI Stencil support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Intensity;
            float _Softness;
            float _PulseSpeed;
            float _PulseAmount;

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
                // Calculate distance from center (0,0 at center, 1 at corners)
                float2 centered = (i.uv - 0.5) * 2.0;
                float dist = length(centered);

                // Pulse effect
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // Create smooth vignette (0 at center, 1 at edges)
                float vignette = smoothstep(0.2, 0.2 + _Softness, dist) * _Intensity * pulse;

                // Output vignette color with calculated alpha
                float4 result = _Color;
                result.a = vignette * _Color.a * i.color.a;

                return result;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
