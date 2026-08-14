

Shader "Custom/SimpleElectricArcLit"
{
    Properties
    {
        [Header(Visuals)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 2, 5, 1)

        [HideInInspector] _ArcDataTex ("Data Tex", 2D) = "grey" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                
                
                float4 _StartPos;
                float4 _DirAndInvLen; // xyz = dir, w = 1 / lenSq
                float3 _U;
                float3 _V;
                float3 _Z;
                float3 _TimeXYZ;       // x = t_x, y = t_y, z = t_z
                float3 _Amplitudes;   // x = amplitudeX, y = amplitudeY, z = amplitudeZ
            CBUFFER_END

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ArcDataTex);  SAMPLER(sampler_ArcDataTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float3 dir = _DirAndInvLen.xyz;
                float invLenSq = _DirAndInvLen.w;
                
                float h = dot(worldPos - _StartPos.xyz, dir) * invLenSq;
                
                float4 p = SAMPLE_TEXTURE2D_LOD(_ArcDataTex, sampler_ArcDataTex, float2(_TimeXYZ.x, h), 0);
                
                float offsetX = (p.x - 0.5) * _Amplitudes.x;
                float offsetY = (p.y - 0.5) * _Amplitudes.y;
                float offsetZ = (p.z - 0.5) * _Amplitudes.z;

                worldPos += (_U * offsetX + _V * offsetY + _Z * offsetZ);

                output.positionCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                SurfaceData surfaceData;
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.emission = _EmissionColor.rgb + albedo.rgb;

                InputData inputData;
                ZERO_INITIALIZE(InputData, inputData);
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}