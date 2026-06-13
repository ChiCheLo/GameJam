Shader "FullScreen/Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Thickness", Range(0, 5)) = 1
        _DepthThreshold("Depth Threshold", Range(0, 10)) = 1
        _NormalThreshold("Normal Threshold", Range(0, 10)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off Cull Off

        Pass
        {
//            Stencil
//            {
//                Ref 1
//                Comp Equal // 只有相等時才渲染
//            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineThickness;
                float _DepthThreshold;
                float _NormalThreshold;
            CBUFFER_END

            void GetDepthAndNormal(float2 uv, out float depth, out float3 normal)
            {
                float rawDepth = SampleSceneDepth(uv);
                depth = LinearEyeDepth(rawDepth, _ZBufferParams);
                normal = SampleSceneNormals(uv);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 texelSize = (1.0 / _ScreenParams.xy) * _OutlineThickness;

                float centerRawDepth = SampleSceneDepth(uv);

                #if UNITY_REVERSED_Z
                if (centerRawDepth <= 0.00001) return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                #else
                if (centerRawDepth >= 0.99999) return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                #endif

                float d0, d1, d2, d3;
                float3 n0, n1, n2, n3;

                GetDepthAndNormal(uv + float2(-1, -1) * texelSize, d0, n0);
                GetDepthAndNormal(uv + float2( 1,  1) * texelSize, d1, n1);
                GetDepthAndNormal(uv + float2( 1, -1) * texelSize, d2, n2);
                GetDepthAndNormal(uv + float2(-1,  1) * texelSize, d3, n3);

                // --- 深度邊緣偵測 ---
                float depthEdge = sqrt(pow(d0 - d1, 2) + pow(d2 - d3, 2));
                depthEdge /= d0;
                depthEdge = step(_DepthThreshold * 0.01, depthEdge);

                // --- 法線邊緣偵測 ---
                float3 normalEdgeVec = sqrt(pow(n0 - n1, 2) + pow(n2 - n3, 2));
                float normalEdge = length(normalEdgeVec);
                normalEdge = step(_NormalThreshold, normalEdge);

                float edge = max(depthEdge, normalEdge);

                half4 sceneCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                return lerp(sceneCol, _OutlineColor, edge);
            }
            ENDHLSL
        }
    }
}
