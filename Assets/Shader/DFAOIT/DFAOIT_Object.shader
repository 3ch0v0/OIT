Shader "OIT/DFAOIT_Object"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.0, 0.33, 1.0, 1.0)
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0,1)) = 0.5
        _Glossiness ("Glossiness", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "LightMode"="DFA_Peeling"}

        
        Pass
        {
            Name "DFA TailAccum"
            Tags { "LightMode"="DFA_Peeling" }
            
            ZWrite Off      
            ZTest Less
            Cull Back
            Blend 0 One One   
            Blend 1 One One, zero OneMinusSrcAlpha
            Blend 2 One One
            
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "DFAOITCommon.hlsl"
            
            struct FragOut
            {
                float4 tailSum : SV_Target0; // rgb: sum(color * alpha), a: sum(alpha)
                float4 tailAuccm : SV_Target1; // rgb: sum(col),a:prod(1-alpha) 
                float fragmenCount: SV_Target2; // count of fragments
            };
            
            Varyings vert(Attributes v) { return DFAOITVertex(v); }

            FragOut frag(Varyings i)
            {
                FragOut o;
                //o.tailSum = 0;
                //o.tailAuccm = 0;

                float4 s = EvaluateSurface(i);
                //if (s.a <= 0.0001) return o;

                float2 uv = ScreenUV(i.positionCS);
                float2 ssUV = i.positionSS.xy/i.positionSS.w;
                float secondZ = SAMPLE_TEXTURE2D(_DFASecondDepthTex, sampler_DFASecondDepthTex, ssUV).r;
                float currZ = i.positionCS.z;

                
                #if UNITY_REVERSED_Z
                    if (currZ >= secondZ - 0.00001) discard; 
                #else
                    if (currZ <= secondZ + 0.00001) discard;
                #endif

                float alpha = saturate(s.a);

                o.tailSum = float4(s.rgb* alpha, alpha);
                o.tailAuccm = float4(s.rgb, alpha);
                o.fragmenCount = 0.001f;
                return o;
            }
            ENDHLSL
        }
    }
}