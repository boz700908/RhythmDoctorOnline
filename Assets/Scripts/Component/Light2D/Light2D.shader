Shader "Custom/Light2D"
{
    Properties
    {
        _LightColor ("Light Color", Color) = (1,1,1,1)
        _Range ("Range", Float) = 5.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend One One // Additive混合
        ZWrite Off
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

            float4 _LightColor;
            float _Range;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 计算从中心到边缘的距离(0到1)
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center) * 2.0;

                // 径向渐变衰减
                float attenuation = 1.0 - saturate(dist);
                attenuation = pow(attenuation, 2.0); // 平方衰减,更自然

                // 应用颜色和衰减
                fixed4 col = _LightColor * attenuation;

                return col;
            }
            ENDCG
        }
    }
}