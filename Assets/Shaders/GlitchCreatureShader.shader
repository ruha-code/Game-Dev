Shader "GlitchCreature/NoiseEntity"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.1, 0.1, 0.1, 0.8)
        _NoiseScale ("Noise Scale", Float) = 5.0
        _NoiseSpeed ("Noise Speed", Float) = 2.0
        _FlickerIntensity ("Flicker Intensity", Range(0, 1)) = 0.5
        _Distortion ("Distortion", Range(0, 1)) = 0.3
        _EdgeGlow ("Edge Glow", Color) = (0.5, 0.7, 1.0, 1.0)
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
            };

            float4 _MainColor;
            float _NoiseScale;
            float _NoiseSpeed;
            float _FlickerIntensity;
            float _Distortion;
            float4 _EdgeGlow;

            // Simple 3D noise function
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
                float n = fbm(v.vertex.xyz * _NoiseScale + time) - 0.5;
                v.vertex.xyz += v.normal * n * _Distortion;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _NoiseSpeed;
                float n = fbm(i.worldPos * _NoiseScale + time);
                
                // Flicker effect
                float flicker = sin(time * 15.0) * 0.5 + 0.5;
                flicker = smoothstep(0.3, 0.7, flicker);
                float alpha = _MainColor.a * (1.0 - _FlickerIntensity + _FlickerIntensity * flicker);
                
                // Edge glow (Fresnel)
                float fresnel = 1.0 - saturate(dot(i.viewDir, i.normal));
                fresnel = pow(fresnel, 2.0);
                
                // Noise-based transparency
                float noiseAlpha = smoothstep(0.2, 0.8, n);
                alpha *= noiseAlpha;
                
                float3 color = lerp(_MainColor.rgb, _EdgeGlow.rgb, fresnel * 0.5);
                
                return fixed4(color, alpha * 0.8);
            }
            ENDCG
        }
    }
}
