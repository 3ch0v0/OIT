#ifndef PPLL_COMMON_INCLUDED
#define PPLL_COMMON_INCLUDED

// 【修复点】显式包含 Core.hlsl，这样编译器就知道 _ZBufferParams 是什么了
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 对应截图 Image 8: 结构体定义 (3个 uint = 12 bytes)
struct FragmentAndLinkBuffer_STRUCT
{
    uint pixelColor;      // Packed RGBA
    uint uDepthSampleIdx; // Packed Depth(24) + SampleIdx(8)
    uint next;            // Next Pointer
};

// --- 对应截图 Image 10/12/13: 打包工具 ---

// 1. 颜色打包
uint PackRGBA(float4 unpackedInput)
{
    uint4 u = (uint4)(saturate(unpackedInput) * 255 + 0.5);
    return (u.w << 24UL) | (u.z << 16UL) | (u.y << 8UL) | u.x;
}

float4 UnpackRGBA(uint packedInput)
{
    float4 unpackedOutput;
    uint4 p = uint4((packedInput & 0xFFUL),
                    (packedInput >> 8UL) & 0xFFUL,
                    (packedInput >> 16UL) & 0xFFUL,
                    (packedInput >> 24UL));
    return ((float4)p) / 255.0;
}

// 2. 深度+SampleID 打包
uint PackDepthSampleIdx(float depth, uint uSampleIdx)
{
    // 注意：这里需要防止 depth 超出 0-1 范围，否则位移会出错
    uint d = (uint)(saturate(depth) * (pow(2, 24) - 1));
    return (d << 8UL) | uSampleIdx;
}

float UnpackDepth(uint uDepthSampleIdx)
{
    return (float)(uDepthSampleIdx >> 8UL) / (pow(2, 24) - 1);
}

uint UnpackSampleIdx(uint uDepthSampleIdx)
{
    return uDepthSampleIdx & 0xFFUL;
}

// 3. 线性深度转换 (Image 12 中用到)
// 这个函数依赖 _ZBufferParams
inline float OitLinear01Depth(float z)
{
    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
}

#endif