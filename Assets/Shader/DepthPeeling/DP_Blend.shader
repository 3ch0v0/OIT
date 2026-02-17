Shader "OIT/DP_Blend"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            //Name "DP_Blend"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGBA
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../Lighting.hlsl"
            #include "DPCommon.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
               
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float4 positionSS : TEXCOORD1;
            };
            
            TEXTURE2D(_LayerColorTex); SAMPLER(sampler_LayerColorTex);
            TEXTURE2D(_DPAccumTex); SAMPLER(sampler_DPAccumTex);
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            
            
            CBUFFER_START(UnityperMaterial)
            float _Alpha;
            float4  _BaseColor;
            float _Glossiness;
            float4 _SpecularColor;
            CBUFFER_END
            
            
            Varyings vert (Attributes i)
            {
                Varyings o;
                o.uv = GetFullScreenTriangleTexCoord(i.vertexID);
                o.positionCS=GetFullScreenTriangleVertexPosition(i.vertexID);
                o.positionSS = ComputeScreenPos(o.positionCS);
                
                return o;
            }

            float4 frag (Varyings i): SV_Target
            {
                float2 ssUV = i.positionSS.xy/i.positionSS.w;
               
                float4 bgCol=SAMPLE_TEXTURE2D(_DPAccumTex,sampler_DPAccumTex, i.uv);
                //float4 bgCol=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, ssUV);
                float4 layerCol=SAMPLE_TEXTURE2D(_LayerColorTex,sampler_LayerColorTex, i.uv);
                float4 premultLayerCol=float4(layerCol.rgb*layerCol.a,layerCol.a);
               
                
                //return premultLayerCol+(1-premultLayerCol.a)*bgCol;
                return layerCol;
                                                                  
                
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
