Shader "Custom/CRT_UI_Text_Full"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(CRT Effects)]
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.4
        _ScanlineCount ("Scanline Count", float) = 600
        _Curvature ("Screen Curvature", Range(0,0.5)) = 0.05
        _Vignette ("Vignette Intensity", Range(0,2)) = 1.0
        _ColorBleed ("Color Bleed (RGB Shift)", Range(0,0.01)) = 0.002
        _NoiseIntensity ("Noise Intensity", Range(0,0.1)) = 0.02
        _Flicker ("Flicker Intensity", Range(0,0.2)) = 0.03
        _Jitter ("Horizontal Jitter", Range(0,0.005)) = 0.0005
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
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

            float _ScanlineIntensity;
            float _ScanlineCount;
            float _Curvature;
            float _Vignette;
            float _ColorBleed;
            float _NoiseIntensity;
            float _Flicker;
            float _Jitter;

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
                
                // Horizontal line jitter from your text shader experiment
                float jitter = sin(_Time.y * 20.0) * _Jitter;
                v.vertex.x += jitter;

                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Screen Curvature (applied to UV space)
                float2 uv = v.uv * 2.0 - 1.0;
                uv += uv * abs(uv) * _Curvature;
                o.uv = uv * 0.5 + 0.5;

                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Soft clipping for curved UVs out of bounds
                if (i.uv.x < 0.0 || i.uv.x > 1.0 || i.uv.y < 0.0 || i.uv.y > 1.0)
                {
                    discard;
                }

                float2 uv = i.uv;

                // 1. Color Bleed / Chromatic Aberration (Tuned for RED prominence)
                float r = tex2D(_MainTex, uv + float2(_ColorBleed, 0)).a;
                float g = tex2D(_MainTex, uv).a;
                float b = tex2D(_MainTex, uv - float2(_ColorBleed, 0)).a;
                
                // --- THE RED BOOST TWEAK ---
                // We boost the red offset and slightly pull back the central green channel 
                // when rendering the fringing edges. This kills the yellow and forces a red/magenta bleed.
                r *= 1.1; 
                g *= 0.8; 
                
                float finalAlpha = tex2D(_MainTex, uv).a;
                
                // Combine sampled channels into text alpha structure
                fixed4 col = float4(r, g, b, finalAlpha) * i.color;

                // 2. Scanlines
                float scan = sin(uv.y * _ScanlineCount) * 0.5 + 0.5;
                col.rgb *= lerp(1.0, scan, _ScanlineIntensity);

                // 3. Vignette
                float2 dist = uv - 0.5;
                float vignette = 1.0 - dot(dist, dist) * _Vignette;
                col.rgb *= max(0.0, vignette);

                // 4. Noise
                float noise = rand(uv * _Time.y) * _NoiseIntensity;
                col.rgb += noise * finalAlpha; 

                // 5. Screen Flicker
                float flicker = 1.0 + (rand(float2(_Time.y, 0)) - 0.5) * _Flicker;
                col.rgb *= flicker;

                return col;
            }
            ENDCG
        }
    }
}