Shader "Custom/CylinderRings"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 0.5, 1, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0.8, 1, 1)
        
        [HideInInspector] _RingsCount ("Rings Count", Float) = 7.0
        [HideInInspector] _SpeedUV ("Speed in UV space", Float) = 1.0
        [HideInInspector] _CurveTex ("Curve Texture", 2D) = "grey" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
        LOD 100

        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _BaseColor;
            float4 _EmissionColor;
            
            float _RingsCount;
            float _SpeedUV;
            sampler2D _CurveTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float phase = (i.uv.y * _RingsCount) - (_Time.y * _SpeedUV);
                
                float wave = frac(phase);

                float threshold = tex2D(_CurveTex, float2(i.uv.y, 0.5)).r;

                clip(threshold - wave - 0.001);

                return _BaseColor + _EmissionColor;
            }
            ENDCG
        }
    }
}
