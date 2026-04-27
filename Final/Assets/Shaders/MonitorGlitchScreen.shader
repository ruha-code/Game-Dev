Shader "Monitor/GlitchScreen"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _GlitchTex ("Glitch Texture", 2D) = "white" {}
        _FaceTex ("Creature Face", 2D) = "white" {}
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0
        _FaceAlpha ("Face Opacity", Range(0, 1)) = 0
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.3
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Contrast ("Contrast", Range(0, 2)) = 1
        _ChromaticOffset ("Chromatic Offset", Range(0, 0.1)) = 0
        _ScreenTear ("Screen Tear", Range(0, 1)) = 0
        _VignetteStrength ("Vignette", Range(0, 1)) = 0.3
        _NoiseScale ("Noise Scale", Float) = 10
        _NoiseSpeed ("Noise Speed", Float) = 5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvOffset : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            sampler2D _GlitchTex;
            sampler2D _FaceTex;
            float4 _MainTex_ST;
            float _GlitchIntensity;
            float _FaceAlpha;
            float _ScanlineStrength;
            float _Brightness;
            float _Contrast;
            float _ChromaticOffset;
            float _ScreenTear;
            float _VignetteStrength;
            float _NoiseScale;
            float _NoiseSpeed;
            
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = rand(i);
                float b = rand(i + float2(1.0, 0.0));
                float c = rand(i + float2(0.0, 1.0));
                float d = rand(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvOffset = float2(0, 0);
                
                // Enhanced screen tear effect
                if (_ScreenTear > 0.01)
                {
                    float tearLine = frac(_Time.y * 3.0);
                    float tearWidth = 0.02;
                    if (abs(v.uv.y - tearLine) < tearWidth)
                    {
                        o.uvOffset.x = _ScreenTear * (rand(float2(_Time.y, v.uv.y)) - 0.5) * 0.3;
                    }
                    
                    // Multiple tear lines
                    for (int i = 0; i < 3; i++)
                    {
                        float multiTear = frac(_Time.y * (2.0 + i) + i * 0.3);
                        if (abs(v.uv.y - multiTear) < tearWidth * 0.5)
                        {
                            o.uvOffset.x += _ScreenTear * 0.5 * (rand(float2(_Time.y + i, v.uv.y)) - 0.5) * 0.2;
                        }
                    }
                }
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv + i.uvOffset;
                float time = _Time.y;
                
                // Enhanced glitch block displacement
                float glitchBlock = 0;
                if (_GlitchIntensity > 0.01)
                {
                    // Multiple block sizes
                    for (int blockSize = 0; blockSize < 3; blockSize++)
                    {
                        float scale = 10.0 + blockSize * 10.0;
                        float blockY = floor(uv.y * scale) / scale;
                        float blockNoise = rand(float2(blockY, floor(time * (8.0 + blockSize * 2.0))));
                        if (blockNoise > (1.0 - _GlitchIntensity * (0.3 + blockSize * 0.1)))
                        {
                            glitchBlock += (rand(float2(blockNoise, time + blockSize)) - 0.5) * _GlitchIntensity * 0.1 * (3.0 - blockSize);
                        }
                    }
                }
                
                // Chromatic aberration
                float2 chromaUV = uv + float2(glitchBlock, 0);
                float r = tex2D(_MainTex, chromaUV + float2(_ChromaticOffset, 0)).r;
                float g = tex2D(_MainTex, chromaUV).g;
                float b = tex2D(_MainTex, chromaUV - float2(_ChromaticOffset, 0)).b;
                float3 color = float3(r, g, b);
                
                // Apply brightness and contrast
                color = (color - 0.5) * _Contrast + 0.5;
                color *= _Brightness;
                
                // Enhanced scanlines
                float scanline = sin(uv.y * 800.0) * 0.5 + 0.5;
                float scanline2 = sin(uv.y * 400.0 + time * 2.0) * 0.5 + 0.5;
                color *= 1.0 - _ScanlineStrength * (1.0 - scanline) * 0.7 - _ScanlineStrength * 0.3 * (1.0 - scanline2);
                
                // Noise overlay
                float n = noise(uv * _NoiseScale + time * _NoiseSpeed);
                color += (n - 0.5) * _GlitchIntensity * 0.3;
                
                // Vignette
                float2 vigUV = uv - 0.5;
                float vigDist = length(vigUV);
                float vignette = 1.0 - vigDist * _VignetteStrength * 2.0;
                color *= vignette;
                
                // Creature face overlay with enhanced glitch
                if (_FaceAlpha > 0.01)
                {
                    float2 faceUV = (uv - 0.5) * 0.8 + 0.5;
                    
                    // Face glitch effects
                    float faceGlitch = rand(float2(floor(uv.y * 30.0), floor(time * 15.0)));
                    if (faceGlitch > (1.0 - _GlitchIntensity * 0.3))
                    {
                        faceUV.x += (rand(float2(faceGlitch, time)) - 0.5) * _GlitchIntensity * 0.1;
                        faceUV.y += (rand(float2(faceGlitch + 1.0, time)) - 0.5) * _GlitchIntensity * 0.05;
                    }
                    
                    float4 faceColor = tex2D(_FaceTex, faceUV);
                    
                    // Face color distortion
                    if (_GlitchIntensity > 0.5)
                    {
                        faceColor.r = tex2D(_FaceTex, faceUV + float2(_ChromaticOffset * 2.0, 0)).r;
                        faceColor.b = tex2D(_FaceTex, faceUV - float2(_ChromaticOffset * 2.0, 0)).b;
                    }
                    
                    color = lerp(color, faceColor.rgb, _FaceAlpha * faceColor.a);
                }
                
                // Random color inversion during heavy glitch
                if (_GlitchIntensity > 0.7)
                {
                    float invertNoise = rand(float2(floor(uv.x * 50.0), floor(time * 20.0)));
                    if (invertNoise > 0.95)
                    {
                        color = 1.0 - color;
                    }
                    
                    // Horizontal glitch lines
                    float hLine = floor(uv.y * 100.0) / 100.0;
                    float hNoise = rand(float2(hLine, floor(time * 10.0)));
                    if (hNoise > 0.98)
                    {
                        color = float3(rand(float2(time, hLine)), rand(float2(time + 1.0, hLine)), rand(float2(time + 2.0, hLine)));
                    }
                }
                
                // Random brightness spikes
                if (_GlitchIntensity > 0.3)
                {
                    float spikeNoise = rand(float2(floor(uv.x * 20.0), floor(time * 15.0)));
                    if (spikeNoise > 0.99)
                    {
                        color *= 2.0;
                    }
                }
                
                // Clamp and return
                color = saturate(color);
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
}
