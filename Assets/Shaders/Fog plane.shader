Shader "Custom/FogPlane"
{
   Properties
    {
        _BaseColor("Fog Color", Color) = (0.1, 0.8, 0.4, 1.0)
        _NoiseScale("Noise Scale", Float) = 0.5
        _TimeScale("Boiling Speed", Float) = 0.3
        
        [Header(Noise Math)]
        _NoiseMult("Noise Multiply", Float) = 2.0
        _NoiseAdd("Noise Add", Float) = -0.2

        [Header(Depth Math)]
        _DepthMult("Depth Multiply", Float) = 0.5
        _DepthAdd("Depth Add", Float) = 0.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _NoiseScale;
                float _TimeScale;
                
                float _NoiseMult;
                float _NoiseAdd;
                
                float _DepthMult;
                float _DepthAdd;
            CBUFFER_END

            float hash(float3 p) 
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3D(float3 x) 
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                         lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                         lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            float fbm(float3 p) 
            {
                float f = 0.0;
                f += 0.5000 * noise3D(p); p *= 2.01;
                f += 0.2500 * noise3D(p); p *= 2.02;
                f += 0.1250 * noise3D(p);
                return f;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.screenPos = ComputeScreenPos(vertexInput.positionCS);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                
                #if UNITY_REVERSED_Z
                    float rawDepth = SampleSceneDepth(screenUV);
                #else
                    float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                
                float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float fragmentLinearDepth = IN.screenPos.w;
                
                float rawDepthDiff = sceneLinearDepth - fragmentLinearDepth;
                
                float depthAlpha = saturate(rawDepthDiff * _DepthMult + _DepthAdd);

                float3 noiseCoords = float3(IN.positionWS.x * _NoiseScale, IN.positionWS.z * _NoiseScale, _Time.y * _TimeScale);
                float rawNoise = fbm(noiseCoords);
                
                float noiseAlpha = saturate(rawNoise * _NoiseMult + _NoiseAdd);

                float finalAlpha = noiseAlpha * depthAlpha;

                return half4(_BaseColor.rgb, _BaseColor.a * finalAlpha);
            }
            ENDHLSL
        }
    }
}
