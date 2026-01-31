Shader "VeilBreakers/VFX/BackGlow"
{
    Properties
    {
        _Color ("Glow Color", Color) = (1, 0.5, 0.2, 0.6)
        _Intensity ("Intensity", Range(0, 5)) = 1.0
        _Radius ("Radius", Range(0, 1)) = 0.6
        _Softness ("Softness", Range(0.01, 1)) = 0.4
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.1
        _FlickerSpeed ("Flicker Speed", Range(0, 3)) = 0.6
        _Center ("Center (UV)", Vector) = (0.5, 0.45, 0, 0)
        _UnscaledTime ("Unscaled Time", Float) = 0
        _BottomClearStart ("Bottom Clear Start", Range(0, 1)) = 0.10
        _BottomClearEnd ("Bottom Clear End", Range(0, 1)) = 0.28
        _TopClearStart ("Top Clear Start", Range(0, 1)) = 0.70
        _TopClearEnd ("Top Clear End", Range(0, 1)) = 0.90
        _SubjectCenter ("Subject Center (UV)", Vector) = (0.5, 0.42, 0, 0)
        _SubjectRadius ("Subject Radius", Range(0, 1)) = 0.32
        _SubjectSoftness ("Subject Softness", Range(0.01, 1)) = 0.18
        _SubjectStrength ("Subject Strength", Range(0, 1)) = 0.9
        _LogoCenter ("Logo Center (UV)", Vector) = (0.5, 0.88, 0, 0)
        _LogoRadius ("Logo Radius", Range(0, 1)) = 0.2
        _LogoSoftness ("Logo Softness", Range(0.01, 1)) = 0.12
        _LogoStrength ("Logo Strength", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend One One
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

            float4 _Color;
            float _Intensity;
            float _Radius;
            float _Softness;
            float _FlickerAmount;
            float _FlickerSpeed;
            float4 _Center;
            float _UnscaledTime;
            float _BottomClearStart;
            float _BottomClearEnd;
            float _TopClearStart;
            float _TopClearEnd;
            float4 _SubjectCenter;
            float _SubjectRadius;
            float _SubjectSoftness;
            float _SubjectStrength;
            float4 _LogoCenter;
            float _LogoRadius;
            float _LogoSoftness;
            float _LogoStrength;

            float uiClearMask(float2 uv)
            {
                float bottom = smoothstep(_BottomClearStart, _BottomClearEnd, uv.y);
                float top = 1.0 - smoothstep(_TopClearStart, _TopClearEnd, uv.y);
                return saturate(bottom * top);
            }

            float readabilityMask(float2 uv)
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);

                float2 subj = (uv - _SubjectCenter.xy) * float2(aspect, 1.0);
                float subjDist = length(subj);
                float subjMask = smoothstep(_SubjectRadius, _SubjectRadius + _SubjectSoftness, subjDist);

                float2 logo = (uv - _LogoCenter.xy) * float2(aspect, 1.0);
                float logoDist = length(logo);
                float logoMask = smoothstep(_LogoRadius, _LogoRadius + _LogoSoftness, logoDist);

                float mask = lerp(1.0, subjMask, _SubjectStrength);
                mask *= lerp(1.0, logoMask, _LogoStrength);
                return mask;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = (_UnscaledTime > 0.0) ? _UnscaledTime : _Time.y;
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 centered = (i.uv - _Center.xy) * float2(aspect, 1.0);
                float dist = length(centered);

                float glow = 1.0 - smoothstep(_Radius, _Radius + _Softness, dist);
                float flicker = 1.0 + (sin(t * _FlickerSpeed + (i.uv.x + i.uv.y) * 6.28318) * 0.5 + 0.5) * _FlickerAmount;

                float alpha = glow * _Intensity * _Color.a * flicker * uiClearMask(i.uv);
                alpha *= readabilityMask(i.uv);
                float3 rgb = _Color.rgb * alpha;

                return float4(rgb, alpha);
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
