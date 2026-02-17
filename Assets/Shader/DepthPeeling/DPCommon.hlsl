#ifndef DP_COMMON_INCLUDED
#define DP_COMMON_INCLUDED

inline float4 EncodeFloatRGBA(float v)
{
    float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
    float kEncodeBit = 1.0 / 255.0;
    float4 enc = kEncodeMul * v;
    enc = frac(enc);
    enc -= enc.yzww * kEncodeBit;
    return enc;
}

inline float DecodeFloatRGBA(float4 v)
{
    float4 kDecodeDot = float4(1.0, 1.0 / 255.0, 1.0 / 65025.0, 1.0 / 16581375.0);
    return dot(v, kDecodeDot);
}
#endif