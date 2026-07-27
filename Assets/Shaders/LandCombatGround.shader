Shader "F89/LandCombatGround"
{
    Properties
    {
        _LandMask ("Antarctica Map", 2D) = "white" {}
        _ArenaCenterMiles ("Arena Center (miles XY)", Vector) = (0, 0, 0, 0)
        _MapSizeMiles ("Map Width (miles east-west)", Float) = 3000
        _MapAspectWidthOverHeight ("Map Aspect (width ÷ height)", Float) = 1.223
        _WorldUnitsPerMile ("World Units Per Mile", Float) = 20
        _ArenaHalfSizeWorld ("Arena Half Size (world units)", Float) = 10

        _IceBright ("Snow Bright", Color) = (0.93, 0.95, 0.98, 1)
        _IceMid ("Snow Mid", Color) = (0.82, 0.86, 0.91, 1)
        _IceShadow ("Snow Shadow", Color) = (0.68, 0.74, 0.82, 1)
        _IceCrack ("Ice Crevice", Color) = (0.52, 0.60, 0.72, 1)
        _IceBlue ("Blue Ice", Color) = (0.72, 0.82, 0.92, 1)
        _RockTint ("Rock Outcrop", Color) = (0.58, 0.56, 0.54, 1)
        _LandNoiseScale ("Terrain Noise Scale", Float) = 0.11
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
                float4 _ArenaCenterMiles;
                float _MapSizeMiles;
                float _MapAspectWidthOverHeight;
                float _WorldUnitsPerMile;
                float _ArenaHalfSizeWorld;
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

            float3 SampleSnowTerrain(float2 worldXY)
            {
                // Infinite procedural Antarctica ice field — continuous in world space.
                float scale = max(_LandNoiseScale, 0.0001);
                float2 p = worldXY * scale;

                float drift = Fbm(p);
                float detail = Fbm(p * 3.7 + float2(17.0, 43.0));
                float crevice = Fbm(p * 7.5 + float2(91.0, 12.0));
                float sastrugi = ValueNoise(float2(p.x * 0.35, p.y * 2.8));
                float windPack = ValueNoise(float2(p.x * 1.8, p.y * 0.22) + float2(33.0, 7.0));
                float rock = Fbm(p * 0.65 + float2(200.0, 50.0));
                float blueIce = smoothstep(0.58, 0.74, Fbm(p * 1.4 + float2(-40.0, 120.0)));
                float dune = Fbm(p * 0.28 + float2(8.0, -19.0));
                float frost = ValueNoise(p * 11.0 + float2(4.0, 9.0));

                float3 snow = lerp(_IceMid.rgb, _IceBright.rgb, drift);
                snow = lerp(snow, _IceShadow.rgb, saturate(1.0 - detail) * 0.55);
                snow = lerp(snow, _IceCrack.rgb, smoothstep(0.50, 0.68, crevice) * 0.34);
                snow = lerp(snow, _IceBright.rgb, sastrugi * 0.20);
                snow = lerp(snow, _IceShadow.rgb, (1.0 - windPack) * 0.18);
                snow = lerp(snow, _IceBlue.rgb, blueIce * 0.32);
                snow = lerp(snow, _RockTint.rgb, smoothstep(0.64, 0.76, rock) * 0.20);
                snow = lerp(snow, _IceMid.rgb, dune * 0.12);
                snow = lerp(snow, _IceBright.rgb, frost * 0.08);
                return snow;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 worldXY = input.worldPos.xy;
                float3 color = SampleSnowTerrain(worldXY);
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

            float _LandNoiseScale;
            fixed4 _IceBright;
            fixed4 _IceMid;
            fixed4 _IceShadow;
            fixed4 _IceCrack;
            fixed4 _IceBlue;
            fixed4 _RockTint;

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

            fixed3 SampleSnowTerrain(float2 worldXY)
            {
                float scale = max(_LandNoiseScale, 0.0001);
                float2 p = worldXY * scale;

                float drift = Fbm(p);
                float detail = Fbm(p * 3.7 + float2(17.0, 43.0));
                float crevice = Fbm(p * 7.5 + float2(91.0, 12.0));
                float sastrugi = ValueNoise(float2(p.x * 0.35, p.y * 2.8));
                float windPack = ValueNoise(float2(p.x * 1.8, p.y * 0.22) + float2(33.0, 7.0));
                float rock = Fbm(p * 0.65 + float2(200.0, 50.0));
                float blueIce = smoothstep(0.58, 0.74, Fbm(p * 1.4 + float2(-40.0, 120.0)));
                float dune = Fbm(p * 0.28 + float2(8.0, -19.0));
                float frost = ValueNoise(p * 11.0 + float2(4.0, 9.0));

                fixed3 snow = lerp(_IceMid.rgb, _IceBright.rgb, drift);
                snow = lerp(snow, _IceShadow.rgb, saturate(1.0 - detail) * 0.55);
                snow = lerp(snow, _IceCrack.rgb, smoothstep(0.50, 0.68, crevice) * 0.34);
                snow = lerp(snow, _IceBright.rgb, sastrugi * 0.20);
                snow = lerp(snow, _IceShadow.rgb, (1.0 - windPack) * 0.18);
                snow = lerp(snow, _IceBlue.rgb, blueIce * 0.32);
                snow = lerp(snow, _RockTint.rgb, smoothstep(0.64, 0.76, rock) * 0.20);
                snow = lerp(snow, _IceMid.rgb, dune * 0.12);
                snow = lerp(snow, _IceBright.rgb, frost * 0.08);
                return snow;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 worldXY = i.worldPos.xy;
                fixed3 color = SampleSnowTerrain(worldXY);
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
