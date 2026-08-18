Shader "F89/ProceduralFlightGround"
{
    Properties
    {
        _LandMask ("Land Mask Map", 2D) = "white" {}
        _MapHalfSizeWorld ("Map Half Size (world units)", Float) = 30000
        _MapAspectWidthOverHeight ("Map Aspect (width ÷ height)", Float) = 1.223

        _OceanDeep ("Ocean Deep", Color) = (0.239, 0.486, 0.800, 1)
        _OceanShallow ("Ocean Shallow", Color) = (0.10, 0.34, 0.52, 1)
        _OceanHighlight ("Ocean Highlight", Color) = (0.22, 0.48, 0.62, 1)
        _WaveScale ("Wave Scale", Float) = 0.012
        _WaveSpeed ("Wave Speed", Float) = 0.4

        _IceBright ("Ice Bright", Color) = (0.90, 0.93, 0.96, 1)
        _IceMid ("Ice Mid", Color) = (0.78, 0.83, 0.88, 1)
        _IceShadow ("Ice Shadow", Color) = (0.62, 0.69, 0.78, 1)
        _IceCrack ("Ice Crack", Color) = (0.52, 0.60, 0.72, 1)
        _IceBlue ("Blue Ice", Color) = (0.72, 0.82, 0.92, 1)
        _RockTint ("Rock Outcrop", Color) = (0.58, 0.56, 0.54, 1)
        _LandNoiseScale ("Land Noise Scale", Float) = 0.09
        _SatelliteBlend ("Satellite Imagery Blend", Range(0, 1)) = 0
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
                float _MapAspectWidthOverHeight;
                float4 _OceanDeep;
                float4 _OceanShallow;
                float4 _OceanHighlight;
                float _WaveScale;
                float _WaveSpeed;
                float4 _IceBright;
                float4 _IceMid;
                float4 _IceShadow;
                float4 _IceCrack;
                float4 _IceBlue;
                float4 _RockTint;
                float _LandNoiseScale;
                float _SatelliteBlend;
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
                for (int i = 0; i < 5; i++)
                {
                    value += amplitude * ValueNoise(p);
                    p *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 WorldToMaskUv(float3 worldPos)
            {
                float mapWidthWorld = max(_MapHalfSizeWorld * 2.0, 1.0);
                float aspect = max(_MapAspectWidthOverHeight, 0.0001);
                float mapHeightWorld = mapWidthWorld / aspect;
                float halfHeightWorld = mapHeightWorld * 0.5;
                float u = (worldPos.x + _MapHalfSizeWorld) / mapWidthWorld;
                float mileV = (-worldPos.z + halfHeightWorld) / mapHeightWorld;
                return float2(u, 1.0 - mileV);
            }

            float LandFactor(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return 0.0;
                }

                float4 sampleColor = SAMPLE_TEXTURE2D(_LandMask, sampler_LandMask, uv);
                return sampleColor.a;
            }

            float3 SampleSatellite(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return float3(1.0, 1.0, 1.0);
                }

                float4 sampleColor = SAMPLE_TEXTURE2D(_LandMask, sampler_LandMask, uv);
                if (sampleColor.a < 0.5)
                {
                    return float3(1.0, 1.0, 1.0);
                }

                return sampleColor.rgb;
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
                float scale = max(_LandNoiseScale, 0.0001);
                float2 p = worldPos.xz * scale;

                float drift = Fbm(p);
                float detail = Fbm(p * 3.7 + float2(17.0, 43.0));
                float crevice = Fbm(p * 7.5 + float2(91.0, 12.0));
                float sastrugi = ValueNoise(float2(p.x * 0.35, p.y * 2.8));
                float windPack = ValueNoise(float2(p.x * 1.8, p.y * 0.22) + float2(33.0, 7.0));
                float rock = Fbm(p * 0.65 + float2(200.0, 50.0));
                float blueIce = smoothstep(0.58, 0.74, Fbm(p * 1.4 + float2(-40.0, 120.0)));
                float dune = Fbm(p * 0.28 + float2(8.0, -19.0));
                float frost = ValueNoise(p * 11.0 + float2(4.0, 9.0));

                float3 ice = lerp(_IceMid.rgb, _IceBright.rgb, drift);
                ice = lerp(ice, _IceShadow.rgb, saturate(1.0 - detail) * 0.55);
                ice = lerp(ice, _IceCrack.rgb, smoothstep(0.50, 0.68, crevice) * 0.34);
                ice = lerp(ice, _IceBright.rgb, sastrugi * 0.20);
                ice = lerp(ice, _IceShadow.rgb, (1.0 - windPack) * 0.18);
                ice = lerp(ice, _IceBlue.rgb, blueIce * 0.32);
                ice = lerp(ice, _RockTint.rgb, smoothstep(0.64, 0.76, rock) * 0.20);
                ice = lerp(ice, _IceMid.rgb, dune * 0.12);
                ice = lerp(ice, _IceBright.rgb, frost * 0.08);
                return ice;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = WorldToMaskUv(input.worldPos);
                float land = LandFactor(uv);
                float time = _Time.y;

                float3 ocean = SampleOcean(input.worldPos, time);
                float3 ice = SampleIce(input.worldPos);
                float3 satellite = SampleSatellite(uv);
                float3 landColor = lerp(ice, satellite, _SatelliteBlend);
                float3 color = lerp(ocean, landColor, land);
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
            float _MapAspectWidthOverHeight;
            fixed4 _OceanDeep;
            fixed4 _OceanShallow;
            fixed4 _OceanHighlight;
            float _WaveScale;
            float _WaveSpeed;
            fixed4 _IceBright;
            fixed4 _IceMid;
            fixed4 _IceShadow;
            fixed4 _IceCrack;
            fixed4 _IceBlue;
            fixed4 _RockTint;
            float _LandNoiseScale;
            float _SatelliteBlend;

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
                for (int i = 0; i < 5; i++)
                {
                    value += amplitude * ValueNoise(p);
                    p *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 WorldToMaskUv(float3 worldPos)
            {
                float mapWidthWorld = max(_MapHalfSizeWorld * 2.0, 1.0);
                float aspect = max(_MapAspectWidthOverHeight, 0.0001);
                float mapHeightWorld = mapWidthWorld / aspect;
                float halfHeightWorld = mapHeightWorld * 0.5;
                float u = (worldPos.x + _MapHalfSizeWorld) / mapWidthWorld;
                float mileV = (-worldPos.z + halfHeightWorld) / mapHeightWorld;
                return float2(u, 1.0 - mileV);
            }

            float LandFactor(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return 0.0;
                }

                fixed4 sampleColor = tex2D(_LandMask, uv);
                return sampleColor.a;
            }

            fixed3 SampleSatellite(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return fixed3(1.0, 1.0, 1.0);
                }

                fixed4 sampleColor = tex2D(_LandMask, uv);
                if (sampleColor.a < 0.5)
                {
                    return fixed3(1.0, 1.0, 1.0);
                }

                return sampleColor.rgb;
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
                float scale = max(_LandNoiseScale, 0.0001);
                float2 p = worldPos.xz * scale;

                float drift = Fbm(p);
                float detail = Fbm(p * 3.7 + float2(17.0, 43.0));
                float crevice = Fbm(p * 7.5 + float2(91.0, 12.0));
                float sastrugi = ValueNoise(float2(p.x * 0.35, p.y * 2.8));
                float windPack = ValueNoise(float2(p.x * 1.8, p.y * 0.22) + float2(33.0, 7.0));
                float rock = Fbm(p * 0.65 + float2(200.0, 50.0));
                float blueIce = smoothstep(0.58, 0.74, Fbm(p * 1.4 + float2(-40.0, 120.0)));
                float dune = Fbm(p * 0.28 + float2(8.0, -19.0));
                float frost = ValueNoise(p * 11.0 + float2(4.0, 9.0));

                fixed3 ice = lerp(_IceMid.rgb, _IceBright.rgb, drift);
                ice = lerp(ice, _IceShadow.rgb, saturate(1.0 - detail) * 0.55);
                ice = lerp(ice, _IceCrack.rgb, smoothstep(0.50, 0.68, crevice) * 0.34);
                ice = lerp(ice, _IceBright.rgb, sastrugi * 0.20);
                ice = lerp(ice, _IceShadow.rgb, (1.0 - windPack) * 0.18);
                ice = lerp(ice, _IceBlue.rgb, blueIce * 0.32);
                ice = lerp(ice, _RockTint.rgb, smoothstep(0.64, 0.76, rock) * 0.20);
                ice = lerp(ice, _IceMid.rgb, dune * 0.12);
                ice = lerp(ice, _IceBright.rgb, frost * 0.08);
                return ice;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = WorldToMaskUv(i.worldPos);
                float land = LandFactor(uv);
                fixed3 ocean = SampleOcean(i.worldPos, _Time.y);
                fixed3 ice = SampleIce(i.worldPos);
                fixed3 satellite = SampleSatellite(uv);
                fixed3 landColor = lerp(ice, satellite, _SatelliteBlend);
                return fixed4(lerp(ocean, landColor, land), 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
