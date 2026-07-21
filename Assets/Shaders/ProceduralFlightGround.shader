Shader "F89/ProceduralFlightGround"
{
    Properties
    {
        _LandMask ("Land Mask Map", 2D) = "white" {}
        _MapHalfSizeWorld ("Map Half Size (world units)", Float) = 30000

        _OceanDeep ("Ocean Deep", Color) = (0.04, 0.18, 0.36, 1)
        _OceanShallow ("Ocean Shallow", Color) = (0.10, 0.34, 0.52, 1)
        _OceanHighlight ("Ocean Highlight", Color) = (0.22, 0.48, 0.62, 1)
        _WaveScale ("Wave Scale", Float) = 0.012
        _WaveSpeed ("Wave Speed", Float) = 0.4

        _IceBright ("Ice Bright", Color) = (0.90, 0.93, 0.96, 1)
        _IceMid ("Ice Mid", Color) = (0.78, 0.83, 0.88, 1)
        _IceShadow ("Ice Shadow", Color) = (0.62, 0.69, 0.78, 1)
        _IceCrack ("Ice Crack", Color) = (0.48, 0.56, 0.66, 1)
        _LandNoiseScale ("Land Noise Scale", Float) = 0.0035
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_LandMask);
            SAMPLER(sampler_LandMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _LandMask_ST;
                float _MapHalfSizeWorld;
                float4 _OceanDeep;
                float4 _OceanShallow;
                float4 _OceanHighlight;
                float _WaveScale;
                float _WaveSpeed;
                float4 _IceBright;
                float4 _IceMid;
                float4 _IceShadow;
                float4 _IceCrack;
                float _LandNoiseScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.worldPos);
                return output;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * ValueNoise(p);
                    p *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 WorldToMaskUv(float3 worldPos)
            {
                float mapSize = max(_MapHalfSizeWorld * 2.0, 1.0);
                float u = (worldPos.x + _MapHalfSizeWorld) / mapSize;
                float mileV = (-worldPos.z + _MapHalfSizeWorld) / mapSize;
                return float2(u, 1.0 - mileV);
            }

            float LandFactor(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return 0.0;
                }

                float4 sampleColor = SAMPLE_TEXTURE2D(_LandMask, sampler_LandMask, uv);
                float brightness = (sampleColor.r + sampleColor.g + sampleColor.b) / 3.0;
                float blueDominance = sampleColor.b - max(sampleColor.r, sampleColor.g);
                // Match AntarcticaLandMask display-land / solid-ice thresholds used for map bases.
                float landScore = smoothstep(0.60, 0.78, brightness)
                    * (1.0 - smoothstep(0.0, 0.04, blueDominance));
                return saturate(landScore);
            }

            float3 SampleOcean(float3 worldPos, float time)
            {
                float scale = max(_WaveScale, 0.0001);
                float2 p = worldPos.xz * scale;

                float waveA = sin(p.x * 6.0 + time * _WaveSpeed * 2.5) * sin(p.y * 5.2 - time * _WaveSpeed * 1.8);
                float waveB = sin(dot(p, float2(0.78, 0.62)) * 8.0 + time * _WaveSpeed);
                float ripple = ValueNoise(p * 3.5 + float2(time * _WaveSpeed * 0.35, -time * _WaveSpeed * 0.25));
                float waves = waveA * 0.45 + waveB * 0.35 + (ripple - 0.5) * 0.35;

                float3 ocean = lerp(_OceanDeep.rgb, _OceanShallow.rgb, waves * 0.5 + 0.5);
                float highlight = saturate(sin(p.x * 11.0 - time * 1.6) * sin(p.y * 9.0 + time * 1.2) * 0.5 + 0.5);
                ocean = lerp(ocean, _OceanHighlight.rgb, highlight * 0.18);
                return ocean;
            }

            float3 SampleIce(float3 worldPos)
            {
                float2 p = worldPos.xz * max(_LandNoiseScale, 0.0001);
                float drift = Fbm(p);
                float detail = Fbm(p * 3.7 + float2(17.0, 43.0));
                float crevice = Fbm(p * 7.5 + float2(91.0, 12.0));

                float3 ice = lerp(_IceMid.rgb, _IceBright.rgb, drift);
                ice = lerp(ice, _IceShadow.rgb, saturate(1.0 - detail) * 0.55);
                ice = lerp(ice, _IceCrack.rgb, smoothstep(0.52, 0.68, crevice) * 0.42);

                float windStreak = ValueNoise(float2(p.x * 0.35, p.y * 2.8));
                ice = lerp(ice, _IceBright.rgb, windStreak * 0.12);
                return ice;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = WorldToMaskUv(input.worldPos);
                float land = LandFactor(uv);
                float time = _Time.y;

                float3 ocean = SampleOcean(input.worldPos, time);
                float3 ice = SampleIce(input.worldPos);
                float3 color = lerp(ocean, ice, land);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _LandMask;
            float4 _LandMask_ST;
            float _MapHalfSizeWorld;
            fixed4 _OceanDeep;
            fixed4 _OceanShallow;
            fixed4 _OceanHighlight;
            float _WaveScale;
            float _WaveSpeed;
            fixed4 _IceBright;
            fixed4 _IceMid;
            fixed4 _IceShadow;
            fixed4 _IceCrack;
            float _LandNoiseScale;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * ValueNoise(p);
                    p *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 WorldToMaskUv(float3 worldPos)
            {
                float mapSize = max(_MapHalfSizeWorld * 2.0, 1.0);
                float u = (worldPos.x + _MapHalfSizeWorld) / mapSize;
                float mileV = (-worldPos.z + _MapHalfSizeWorld) / mapSize;
                return float2(u, 1.0 - mileV);
            }

            float LandFactor(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return 0.0;
                }

                fixed4 sampleColor = tex2D(_LandMask, uv);
                float brightness = (sampleColor.r + sampleColor.g + sampleColor.b) / 3.0;
                float blueDominance = sampleColor.b - max(sampleColor.r, sampleColor.g);
                return saturate(
                    smoothstep(0.60, 0.78, brightness)
                    * (1.0 - smoothstep(0.0, 0.04, blueDominance)));
            }

            fixed3 SampleOcean(float3 worldPos, float time)
            {
                float scale = max(_WaveScale, 0.0001);
                float2 p = worldPos.xz * scale;
                float waveA = sin(p.x * 6.0 + time * _WaveSpeed * 2.5) * sin(p.y * 5.2 - time * _WaveSpeed * 1.8);
                float waveB = sin(dot(p, float2(0.78, 0.62)) * 8.0 + time * _WaveSpeed);
                float ripple = ValueNoise(p * 3.5 + float2(time * _WaveSpeed * 0.35, -time * _WaveSpeed * 0.25));
                float waves = waveA * 0.45 + waveB * 0.35 + (ripple - 0.5) * 0.35;
                fixed3 ocean = lerp(_OceanDeep.rgb, _OceanShallow.rgb, waves * 0.5 + 0.5);
                float highlight = saturate(sin(p.x * 11.0 - time * 1.6) * sin(p.y * 9.0 + time * 1.2) * 0.5 + 0.5);
                ocean = lerp(ocean, _OceanHighlight.rgb, highlight * 0.18);
                return ocean;
            }

            fixed3 SampleIce(float3 worldPos)
            {
                float2 p = worldPos.xz * max(_LandNoiseScale, 0.0001);
                float drift = Fbm(p);
                float detail = Fbm(p * 3.7 + float2(17.0, 43.0));
                float crevice = Fbm(p * 7.5 + float2(91.0, 12.0));
                fixed3 ice = lerp(_IceMid.rgb, _IceBright.rgb, drift);
                ice = lerp(ice, _IceShadow.rgb, saturate(1.0 - detail) * 0.55);
                ice = lerp(ice, _IceCrack.rgb, smoothstep(0.52, 0.68, crevice) * 0.42);
                float windStreak = ValueNoise(float2(p.x * 0.35, p.y * 2.8));
                ice = lerp(ice, _IceBright.rgb, windStreak * 0.12);
                return ice;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = WorldToMaskUv(i.worldPos);
                float land = LandFactor(uv);
                fixed3 ocean = SampleOcean(i.worldPos, _Time.y);
                fixed3 ice = SampleIce(i.worldPos);
                return fixed4(lerp(ocean, ice, land), 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
