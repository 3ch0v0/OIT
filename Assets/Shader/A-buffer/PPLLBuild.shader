Shader "OIT/PPLL_Build"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        // 关键：不写入深度，不输出颜色 (ColorMask 0)
        ZWrite Off
        Cull Off
        ColorMask 0

        Pass
        {
            Name "PerPixelLinkedList"
            Tags { "LightMode" = "PerPixelLinkedList" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma require randomwrite

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "PPLLCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END
            
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            // --- Buffer 定义 (对应截图变量名) ---
            RWStructuredBuffer<FragmentAndLinkBuffer_STRUCT> fragLinkedBuffer : register(u1); 
            RWByteAddressBuffer startOffetBuffer : register(u2);

            // ==========================================
            // 对应截图 Image 12: 写入链表函数
            // ==========================================
            void createFragmentEntry(float4 col, float3 pos, uint uSampleIdx)
            {
                // 1. 申请新节点索引
                uint uPixelCount = fragLinkedBuffer.IncrementCounter();

                // 2. 计算地址 (截图逻辑：减去 0.5 对齐像素中心)
                // _ScreenParams.x 是 Width
                uint uStartOffsetAddress = 4 * (_ScreenParams.x * (pos.y - 0.5) + (pos.x - 0.5));
                
                // 3. 原子交换：把当前 uPixelCount 设为头，取回旧头 uOldStartOffset
                uint uOldStartOffset;
                startOffetBuffer.InterlockedExchange(uStartOffsetAddress, uPixelCount, uOldStartOffset);

                // 4. 组装数据
                FragmentAndLinkBuffer_STRUCT Element;
                Element.pixelColor = PackRGBA(col);
                // 截图用了 OitLinear01Depth
                Element.uDepthSampleIdx = PackDepthSampleIdx(OitLinear01Depth(pos.z), uSampleIdx);
                Element.next = uOldStartOffset;

                // 5. 写入 Buffer
                fragLinkedBuffer[uPixelCount] = Element;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            void frag(Varyings input) // void return
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                if (color.a < 0.01) discard;

                // 调用写入函数 (无 MSAA，SampleIdx = 0)
                createFragmentEntry(color, input.positionCS.xyz, 0);
            }
            ENDHLSL
        }
    }
}