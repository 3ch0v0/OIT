#ifndef _LIGHTING_INCLUDED
#define _LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float3 CalculateLighting(float3 texColor,float3 viewDirWS, float3 normalWS, float _Glossiness, float4 _SpecularColor)
{
    Light mainLight = GetMainLight();
    float3 lightColor = mainLight.color * mainLight.distanceAttenuation;
    float3 lightDirWS = normalize(mainLight.direction);
    float3 halfDirWS = normalize(lightDirWS + viewDirWS);
    //-ambient
    float3 ambient = SampleSH(normalWS);
    //-diffuse
    float NdotL = max(0, dot(normalWS, lightDirWS));
    float3 diffuse = NdotL * lightColor;
    //-specular
    float NdotH = max(0, dot(normalWS, halfDirWS));
    float3 specular = lightColor * _SpecularColor.rgb * pow(NdotH, _Glossiness * 128.0);

    float3 finalColor = texColor.rgb * (ambient + diffuse) + specular;
    return finalColor;
}
#endif
