Shader "Hidden/Occluder"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "Occluder"
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = _Color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // RGB = mask color (optional)
                // A = occluder mask

                float4 col = i.color;  
                //float4 col = float4(0.5, 0.5, 0.5, 1.0);

                // ako želiš hard occluder masku:
                col.a = 1.0;

                return col;
            }

            ENDHLSL
        }
    }
}