Shader "GlitchCreature/EnhancedEntity"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.1, 0.1, 0.1, 0.8)
        _NoiseScale ("Noise Scale", Float) = 5.0
        _NoiseSpeed ("Noise Speed", Float) = 2.0
        _FlickerIntensity ("Flicker Intensity", Range(0, 1)) = 0.5
        _Distortion ("Distortion", Range(0, 1)) = 0.3
        _EdgeGlow ("Edge Glow", Color) = (0.5, 0.7, 1.0, 1.0)
        _ParticleDensity ("Particle Density", Range(0, 1)) = 0.3
        _ParticleSpeed ("Particle Speed", Float) = 1.0
        _GlitchBlocks ("Glitch Blocks", Range(0, 1)) = 0.2
        _DissolveAmount ("Dissolve", Range(0, 1)) = 0
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            float4 _MainColor;
            float _NoiseScale;
            float _NoiseSpeed;
            float _FlickerIntensity;
            float _Distortion;
            float4 _EdgeGlow;
            float _ParticleDensity;
            float _ParticleSpeed;
            float _GlitchBlocks;
            float _DissolveAmount;

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float fbm(float3 p)
            {
                float f = 0.0;
                f += 0.5000 * noise(p); p *= 2.02;
                f += 0.2500 * noise(p); p *= 2.03;
                f += 0.1250 * noise(p); p *= 2.01;
                f += 0.0625 * noise(p);
                return f / 0.9375;
            }

            v2f vert (appdata v)
            {
                v2f o;
                float time = _Time.y * _NoiseSpeed;
                
                // Vertex distortion
                float n = fbm(v.vertex.xyz * _NoiseScale + time) - 0.5;
                v.vertex.xyz += v.normal * n * _Distortion;
                
                // Glitch block displacement
                if (_GlitchBlocks > 0.01)
                {
                    float blockY = floor(v.vertex.y * 10.0) / 10.0;
                    float blockNoise = rand(float2(blockY, floor(time * 8.0)));
                    if (blockNoise > (1.0 - _GlitchBlocks))
                    {
                        v.vertex.x += (rand(float2(blockNoise, time)) - 0.5) * _GlitchBlocks * 0.5;
                    }
                }
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                o.uv = v.vertex.xy * 0.5 + 0.5;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _NoiseSpeed;
                float n = fbm(i.worldPos * _NoiseScale + time);
                
                // Dissolve effect
                float dissolveNoise = fbm(i.worldPos * _NoiseScale * 2.0 + time * 0.5);
                if (dissolveNoise < _DissolveAmount)
                    discard;
                
                // Flicker effect
                float flicker = sin(time * 15.0) * 0.5 + 0.5;
                flicker = smoothstep(0.3, 0.7, flicker);
                float alpha = _MainColor.a * (1.0 - _FlickerIntensity + _FlickerIntensity * flicker);
                
                // Particle breakup effect
                float particleNoise = rand(floor(i.worldPos.xy * _ParticleDensity * 20.0) + floor(time * _ParticleSpeed));
                float particleAlpha = particleNoise > (1.0 - _ParticleDensity) ? 1.0 : 0.0;
                alpha = lerp(alpha, alpha * 0.3, particleAlpha * 0.5);
                
                // Edge glow (Fresnel)
                float fresnel = 1.0 - saturate(dot(i.viewDir, i.normal));
                fresnel = pow(fresnel, 2.0);
                
                // Noise-based transparency
                float noiseAlpha = smoothstep(0.2, 0.8, n);
                alpha *= noiseAlpha;
                
                // Chromatic aberration on edges
                float3 color = _MainColor.rgb;
                float aberration = fresnel * 0.3;
                color.r += aberration;
                color.b -= aberration;
                
                color = lerp(color, _EdgeGlow.rgb, fresnel * 0.5);
                
                // Random color inversion during heavy flicker
                if (_FlickerIntensity > 0.7)
                {
                    float invertNoise = rand(floor(i.worldPos.xy * 30.0) + floor(time * 20.0));
                    if (invertNoise > 0.95)
                        color = 1.0 - color;
                }
                
                return fixed4(color, alpha * 0.8);
            }
            ENDCG
        }
    }
}
