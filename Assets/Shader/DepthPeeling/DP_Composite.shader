Shader "OIT/DP_Composite"
{
   SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "DP_Composite"
            
            ZWrite Off
            ZTest Always
            Cull Off
            
            //SrcColor * 1 + DstColor * SrcAlpha
            Blend One SrcAlpha, Zero One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            
            TEXTURE2D(_LayerColorTex);
            SAMPLER(sampler_LayerColorTex);

            Varyings vert(Attributes i)
            {
                Varyings o;
               
                o.uv = GetFullScreenTriangleTexCoord(i.vertexID);
               
                o.positionCS = GetFullScreenTriangleVertexPosition(i.vertexID);

                return o;
            }

            float4 frag(Varyings i): SV_Target
            {
                
                float4 layerCol = SAMPLE_TEXTURE2D(_LayerColorTex, sampler_LayerColorTex, i.uv);
                return layerCol;
            }
            ENDHLSL
        }
    }
}
