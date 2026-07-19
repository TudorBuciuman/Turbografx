Shader "Custom/CRT_AnalogNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _ColorBleed ("Color Bleed", Range(0,0.05)) = 0.002

        _NoiseAmount   ("Grain Amount", Range(0,0.3)) = 0.08
        _NoiseSpeed    ("Grain Crawl Speed", Range(0,50)) = 12
        _NoiseScale    ("Grain Scale (px)", Range(0.5,4)) = 1.0

        _JitterAmount  ("Line Jitter", Range(0,0.05)) = 0.004
        _JitterFreq    ("Line Jitter Refresh Rate (Hz)", Range(1,60)) = 24
        _GlitchChance  ("Dropout Line Chance", Range(0,1)) = 0.02
        _GlitchStrength("Dropout Line Strength", Range(0,0.3)) = 0.08

        _RollSpeed     ("Roll Bar Speed", Range(0,2)) = 0.15
        _RollWidth     ("Roll Bar Width", Range(0.01,0.5)) = 0.08
        _RollNoiseBoost("Roll Bar Noise Boost", Range(0,1)) = 0.4

        _DotCrawlAmount("Dot Crawl (Chroma Shimmer)", Range(0,0.02)) = 0.004
        _DotCrawlSpeed ("Dot Crawl Speed", Range(0,20)) = 6

        _VertRes ("Vertical Resolution (lines)", Float) = 224
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

            float _ColorBleed;

            float _NoiseAmount;
            float _NoiseSpeed;
            float _NoiseScale;

            float _JitterAmount;
            float _JitterFreq;
            float _GlitchChance;
            float _GlitchStrength;

            float _RollSpeed;
            float _RollWidth;
            float _RollNoiseBoost;

            float _DotCrawlAmount;
            float _DotCrawlSpeed;

            float _VertRes;

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

            // Standard cheap hash. We feed it different "seeds" (some tied to
            // continuous time, some tied to stepped/quantized time) to get
            // both smooth crawl and the jumpy per-refresh jitter real CRTs have.
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // ---- Per-scanline horizontal jitter ----
                // Quantize time so the jitter "refreshes" at a fixed rate
                // instead of smoothly sliding — that's what makes it read
                // as analog instability rather than a wobble shader.
                float refreshStep = floor(_Time.y * _JitterFreq);
                float lineIndex = floor(uv.y * _VertRes);
                float lineSeed = lineIndex * 0.618 + refreshStep;

                float jitter = (rand(float2(lineSeed, 0.0)) - 0.5) * _JitterAmount;

                // Occasional dropout/glitch line: a rarer, much stronger kick
                float glitchRoll = rand(float2(lineSeed, 7.31));
                float isGlitch = step(1.0 - _GlitchChance, glitchRoll);
                float glitchJitter = (rand(float2(lineSeed, 3.14)) - 0.5) * 0.5;
                jitter += isGlitch * glitchJitter;

                uv.x += jitter;

                // ---- Rolling interference band ----
                // A soft horizontal band that slowly travels down (and wraps),
                // like a CRT losing vertical hold for a moment.
                float rollPos = frac(uv.y - _Time.y * _RollSpeed);
                float band = 1.0 - smoothstep(0.0, _RollWidth, abs(rollPos - 0.5) * 2.0);
                // band is 1 at the center of the roll, 0 away from it

                // ---- Color bleed + dot crawl / chroma shimmer ----
                // _ColorBleed is a constant horizontal RGB split (classic
                // composite-signal fringing). _DotCrawlAmount rides on top
                // of it as a fast oscillation, so the fringe itself wobbles
                // instead of sitting in a fixed spot.
                float crawl = sin(uv.y * 400.0 + _Time.y * _DotCrawlSpeed) * _DotCrawlAmount;
                float rOffset = _ColorBleed + crawl;
                float bOffset = _ColorBleed - crawl;

                float r = tex2D(_MainTex, uv + float2(rOffset, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(bOffset, 0)).b;

                fixed4 col = float4(r, g, b, 1);

                // ---- Moving grain ----
                // Offset the noise lookup by time so the grain pattern itself
                // crawls/flickers frame to frame instead of sitting static.
                float2 noiseUV = uv * (_VertRes * _NoiseScale) + _Time.y * _NoiseSpeed * float2(37.0, 91.0);
                float grain = rand(noiseUV) - 0.5;

                float noiseBoost = 1.0 + band * _RollNoiseBoost;
                col.rgb += grain * _NoiseAmount * noiseBoost;

                // Dropout lines also flash brighter/darker, not just displaced
                col.rgb += isGlitch * (rand(float2(lineSeed, 9.9)) - 0.5) * _GlitchStrength;

                return col;
            }
            ENDCG
        }
    }
}
