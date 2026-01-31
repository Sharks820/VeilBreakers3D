Shader "VeilBreakers/VFX/LightRays"
{
    Properties
    {
        _Color ("Ray Color", Color) = (1, 0.6, 0.25, 0.5)
        _Intensity ("Intensity", Range(0, 5)) = 0.6
        _RayCount ("Ray Count", Range(5, 60)) = 24
        _RaySharpness ("Ray Sharpness", Range(1, 10)) = 4
        _RotationSpeed ("Rotation Speed", Range(-1, 1)) = 0.05
        _RaySpeed ("Ray Flicker Speed", Range(0, 3)) = 0.4
        _Radius ("Radius", Range(0, 1)) = 0.9
        _Softness ("Softness", Range(0.01, 1)) = 0.4
        _Center ("Center (UV)", Vector) = (0.5, 0.85, 0, 0)
        _InnerFade ("Inner Fade", Range(0, 0.5)) = 0.12
        _LogoProtect ("Logo Protect", Range(0, 1)) = 0.55
        _UnscaledTime ("Unscaled Time", Float) = 0
        _BottomClearStart ("Bottom Clear Start", Range(0, 1)) = 0.10
        _BottomClearEnd ("Bottom Clear End", Range(0, 1)) = 0.28
        _TopClearStart ("Top Clear Start", Range(0, 1)) = 0.70
        _TopClearEnd ("Top Clear End", Range(0, 1)) = 0.92
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
            "Queue" = "Transparent+25"
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
            float _RayCount;
            float _RaySharpness;
            float _RotationSpeed;
            float _RaySpeed;
            float _Radius;
            float _Softness;
            float4 _Center;
            float _InnerFade;
            float _LogoProtect;
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

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
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
                float2 uv = i.uv - _Center.xy;
                uv.x *= aspect;

                float r = length(uv);
                float angle = atan2(uv.y, uv.x);
                angle += t * _RotationSpeed;

                float rays = abs(sin(angle * _RayCount + t * _RaySpeed));
                rays = pow(rays, _RaySharpness);

                float radial = 1.0 - smoothstep(_Radius, _Radius + _Softness, r);
                float vertical = smoothstep(0.0, 0.5, i.uv.y);

                float noise = hash(uv * 10.0 + t);
                rays *= lerp(0.7, 1.2, noise);

                // Fade rays near the source so they don't "blow out" the logo/center.
                float inner = smoothstep(_InnerFade, _InnerFade + 0.08, r);

                // Protect the logo region (top-center) from getting washed out.
                float2 logoUV = i.uv - float2(0.5, 0.89);
                logoUV.x *= aspect;
                float logoDist = length(logoUV);
                float logoMask = 1.0 - smoothstep(0.0, 0.18, logoDist);
                float logoProtect = lerp(1.0, 1.0 - _LogoProtect, logoMask);

                float alpha = rays * radial * vertical * inner * logoProtect * uiClearMask(i.uv) * _Intensity * _Color.a;
                alpha *= readabilityMask(i.uv);
                float3 rgb = _Color.rgb * alpha;

                return float4(rgb, alpha);
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
