Shader "Custom/CRT_UI_Text_Fixed"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Jitter ("Jitter", Range(0,0.005)) = 0.0005
        _Flicker ("Flicker", Range(0,0.2)) = 0.03
        _ColorShift ("Color Shift", Range(0,0.005)) = 0.001
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            float _Jitter;
            float _Flicker;
            float _ColorShift;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata_t v)
            {
                v2f o;

                // subtle jitter
                float jitter = sin(_Time.y * 20.0) * _Jitter;
                v.vertex.x += jitter;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float shift = _ColorShift;

                // sample alpha only
                float a = tex2D(_MainTex, uv).a;

                // RGB shift using alpha (fake color separation)
                float r = tex2D(_MainTex, uv + float2(shift, 0)).a;
                float g = a;
                float b = tex2D(_MainTex, uv - float2(shift, 0)).a;

                float flicker = 1.0 + (rand(float2(_Time.y, uv.y)) - 0.5) * _Flicker;

                // apply color PROPERLY
                fixed4 col = float4(r, g, b, a) * i.color;

                col.rgb *= flicker;

                return col;
            }
            ENDCG
        }
    }
}