Shader "Hidden/Outline"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D_X(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            float4 _CameraDepthTexture_TexelSize;

            float _Scale;
            float4 _Color;
            float _DepthThreshold;
            float _DepthNormalThreshold;
            float _DepthNormalThresholdScale;
            float _NormalThreshold;
            float4x4 _ClipToView;

            TEXTURE2D_X(_BlitTexture);

            struct Attributes { uint vertexID : SV_VertexID; };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 viewDir    : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.positionCS = pos;
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                output.viewDir = mul(_ClipToView, pos).xyz;
                return output;
            }

            float4 alphaBlend(float4 top, float4 bottom)
            {
                float3 col = top.rgb * top.a + bottom.rgb * (1 - top.a);
                float  a   = top.a + bottom.a * (1 - top.a);
                return float4(col, a);
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float2 ts = _CameraDepthTexture_TexelSize.xy;
                float hf = floor(_Scale * 0.5);
                float hc = ceil(_Scale * 0.5);

                float2 blUV = i.uv + float2(-ts.x, -ts.y) * hf;
                float2 trUV = i.uv + float2( ts.x,  ts.y) * hc;
                float2 brUV = i.uv + float2( ts.x * hc, -ts.y * hf);
                float2 tlUV = i.uv + float2(-ts.x * hf,  ts.y * hc);

                float d0 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, blUV).r;
                float d1 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, trUV).r;
                float d2 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, brUV).r;
                float d3 = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, tlUV).r;

                float3 n0 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, blUV).rgb;
                float3 n1 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, trUV).rgb;
                float3 n2 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, brUV).rgb;
                float3 n3 = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, tlUV).rgb;

                float3 viewNormal = n0 * 2 - 1;
                float NdotV = 1 - dot(viewNormal, -i.viewDir);
                float nt01 = saturate((NdotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold));
                float nt   = nt01 * _DepthNormalThresholdScale + 1;

                float depthThreshold = _DepthThreshold * d0 * nt;

                float dfd0 = d1 - d0;
                float dfd1 = d3 - d2;
                float edgeDepth = sqrt(pow(dfd0, 2) + pow(dfd1, 2)) * 100;
                edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

                float3 nfd0 = n1 - n0;
                float3 nfd1 = n3 - n2;
                float edgeNormal = sqrt(dot(nfd0, nfd0) + dot(nfd1, nfd1));
                edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;

                float edge = max(edgeDepth, edgeNormal);

                float4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.uv);
                float4 edgeColor = float4(_Color.rgb, _Color.a * edge);
                return alphaBlend(edgeColor, src);
            }
            ENDHLSL
        }
    }
}