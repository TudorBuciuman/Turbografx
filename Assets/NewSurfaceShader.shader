Shader "Custom/CRT_Full"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.4
        _Curvature ("Screen Curvature", Range(0,0.5)) = 0.08
        _Vignette ("Vignette", Range(0,2)) = 1.2
        _ColorBleed ("Color Bleed", Range(0,0.005)) = 0.002
        _NoiseIntensity ("Noise", Range(0,0.1)) = 0.02
        _Flicker ("Flicker", Range(0,0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;

            float _ScanlineIntensity;
            float _Curvature;
            float _Vignette;
            float _ColorBleed;
            float _NoiseIntensity;
            float _Flicker;

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

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float2 uv = v.uv * 2.0 - 1.0;

                uv += uv * abs(uv) * _Curvature;

                o.uv = uv * 0.5 + 0.5;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float offset = _ColorBleed;

                float r = tex2D(_MainTex, uv + float2(offset, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(offset, 0)).b;

                fixed4 col = float4(r, g, b, 1);

                float scan = sin(uv.y * 800.0) * 0.5 + 0.5;
                col.rgb *= lerp(1.0, scan, _ScanlineIntensity);

                float2 dist = uv - 0.5;
                float vignette = 1.0 - dot(dist, dist) * _Vignette;
                col.rgb *= vignette;

                float noise = rand(uv * _Time.y) * _NoiseIntensity;
                col.rgb += noise;

                float flicker = 1.0 + (rand(float2(_Time.y, 0)) - 0.5) * _Flicker;
                col.rgb *= flicker;

                return col;
            }
            ENDCG
        }
    }
}