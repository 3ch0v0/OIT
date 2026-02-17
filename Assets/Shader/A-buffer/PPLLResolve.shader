Shader "OIT/PPLL_Resolve"
{
    Properties {}
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "PPLLCommon.hlsl"

            // Buffer
            ByteAddressBuffer startOffetBuffer;
            StructuredBuffer<FragmentAndLinkBuffer_STRUCT> fragLinkedBuffer;
            
            // 【关键】声明 C# 传过来的背景纹理
            TEXTURE2D_X(_PPLL_BlitRT);
            SAMPLER(sampler_PPLL_BlitRT);

            #define MAX_SORTED_PIXELS 16

            // 使用 OITVaryings 避免与 Blit.hlsl 冲突
            struct OITVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            OITVaryings vert(uint vertexID : SV_VertexID)
            {
                OITVaryings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            float4 renderLinkedList(float4 bgCol, float2 pos, uint uSampleIndex)
            {
                // 获取头指针
                uint uStartOffsetAddress = 4 * (_ScreenParams.x * (pos.y - 0.5) + (pos.x - 0.5));
                uint uOffset = startOffetBuffer.Load(uStartOffsetAddress);

                FragmentAndLinkBuffer_STRUCT SortedPixels[MAX_SORTED_PIXELS];
                int nNumPixels = 0;

                // 遍历链表
                while (uOffset != 0)
                {
                    if (nNumPixels >= MAX_SORTED_PIXELS) break;
                    FragmentAndLinkBuffer_STRUCT Element = fragLinkedBuffer[uOffset];
                    SortedPixels[nNumPixels] = Element;
                    nNumPixels++;
                    uOffset = Element.next;
                }

                // 排序 (Bubble Sort)
                for (int i = 0; i < nNumPixels - 1; i++) {
                    for (int j = i + 1; j > 0; j--) {
                        float depth = UnpackDepth(SortedPixels[j].uDepthSampleIdx);
                        float prevDepth = UnpackDepth(SortedPixels[j-1].uDepthSampleIdx);
                        if (prevDepth < depth) {
                            FragmentAndLinkBuffer_STRUCT temp = SortedPixels[j-1];
                            SortedPixels[j-1] = SortedPixels[j];
                            SortedPixels[j] = temp;
                        }
                    }
                }

                // 混合
                float4 res = bgCol;
                for (int k = 0; k < nNumPixels; k++)
                {
                    float4 vPixColor = UnpackRGBA(SortedPixels[k].pixelColor);
                    res.rgb = lerp(res.rgb, vPixColor.rgb, vPixColor.a);
                }
                return res;
            }

            half4 frag(OITVaryings input) : SV_Target
            {
                // 采样背景
                float4 col = SAMPLE_TEXTURE2D_X(_PPLL_BlitRT, sampler_PPLL_BlitRT, input.texcoord);
                return renderLinkedList(col, input.positionCS.xy, 0);
            }
            ENDHLSL
        }
    }
}