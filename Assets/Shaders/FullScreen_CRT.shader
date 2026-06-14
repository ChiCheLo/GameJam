Shader "Custom/FullScreen_CRT"
{
    Properties
    {
        _MainColor ("Color", Color) = (1 ,1 ,1 ,1)

        _FisheyeStrength ("Fisheye Strength", Range(0, 1)) = 0.5
        _AberrationAmount ("Aberration Amount", Vector) = (0.2, 0.2, 0, 0)

        _LineWidth("LineWidth", Range(0, 1)) = 0.1
        _LineGap ("LineGap", Range(0, 1)) = 0.1
        _LineAlpha ("LineAlpha", Range(0, 1)) = 0.5

        _NoiseIntensity ("NoiseIntensity", Float) = 1
        _NoiseSpeed ("NoiseSpeed", Float) = 0.2

        _VignetteBrightness ("VignetteBrightness", Float) = 0.1
        _VignetteIntensity ("VignetteIntensity", Range(0, 2)) = 0.1

        _ScanBarTex ("_ScanBarTex", 2D) = "white"{}
        _ScanBarAlpha ("ScanBarAlpha", Range(0, 1)) = 0.8
        _ScanBarSpeed ("ScanBarSpeed", Float) = 3
        _Brightness ("Brightness", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            TEXTURE2D(_ScanBarTex);
            SAMPLER(sampler_ScanBarTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _MainColor;
                float _FisheyeStrength;
                float2 _AberrationAmount;
                float _LineWidth;
                float _LineGap;
                float _LineAlpha;
                float _NoiseIntensity;
                float _NoiseSpeed;
                float _VignetteBrightness;
                float _VignetteIntensity;
                float _ScanBarAlpha;
                float _ScanBarSpeed;
                float _Brightness;
            CBUFFER_END

            float randomUV(float2 uv, float speed)
            {
                return frac(sin(dot(uv + (_Time.y * speed), float2(12.9898, 78.233))) * 43758.5453);
            }

            float ScanEffect(float v, float lineWidth, float gap)
            {
                float p = frac(v / (lineWidth + gap)) * (lineWidth + gap);
                p = step(gap, p) * _LineAlpha;

                return p;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // 魚眼效果 (Fisheye)
                half2 center = half2(0.5, 0.5);
                half2 distVec = uv - center;
                half distSq = dot(distVec, distVec);

                uv = center + distVec * (1.0 + distSq * _FisheyeStrength);

                // 外框
                float2 isInsideV2 = step(0.0, uv) * step(uv, 1.0);
                float isInside = isInsideV2.x * isInsideV2.y;

                // 色彩偏移 (Chromatic Aberration)
                float2 offset = _AberrationAmount;
                half r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset).r;
                half g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).g;
                half b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset).b;

                // 掃描線
                r *= 1 - (ScanEffect(uv.y + offset.y, _LineWidth, _LineGap));
                g *= 1 - (ScanEffect(uv.y, _LineWidth, _LineGap));
                b *= 1 - (ScanEffect(uv.y - offset.y, _LineWidth, _LineGap));

                half3 col = half3(r, g, b);

                // 雜訊
                float noise = randomUV(uv, _NoiseSpeed) * _NoiseIntensity;
                col += noise;

                // 暗角
                float vignette = uv.x * uv.y;
                vignette *= (1 - uv.x) * (1 - uv.y);
                vignette *= _VignetteBrightness;
                vignette = pow(vignette, _VignetteIntensity);

                col *= vignette;

                // 滾動條
                float2 scanBarUV = uv.xy;
                scanBarUV.y += _Time * _ScanBarSpeed;
                half barIntensity = SAMPLE_TEXTURE2D(_ScanBarTex, sampler_ScanBarTex, scanBarUV).r;
                col *= min(barIntensity + (1 - _ScanBarAlpha), 1);

                // 整體亮度
                col *= _Brightness;

                return half4(col * _MainColor * isInside, 1);
            }
            ENDHLSL
        }
    }
}