// URP sneeuw-shader: duwt de vertices omlaag waar voetafdrukken zitten,
// maakt een klein randje sneeuw rond de afdruk, en berekent per pixel de
// normals zodat de afdrukken mooi belicht worden.
Shader "Custom/DeformableSnow"
{
    Properties
    {
        _BaseColor      ("Snow Color", Color) = (0.95, 0.97, 1.0, 1)
        _DeepColor      ("Footprint Color", Color) = (0.72, 0.80, 0.90, 1)
        _BaseMap        ("Snow Texture (optioneel)", 2D) = "white" {}
        _Smoothness     ("Smoothness", Range(0, 1)) = 0.35
        _RimHeight      ("Rim Height (randje rond afdruk)", Range(0, 0.1)) = 0.02
        _NormalStrength ("Footprint Normal Strength", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _DeepColor;
            float4 _BaseMap_ST;
            float  _Smoothness;
            float  _RimHeight;
            float  _NormalStrength;
        CBUFFER_END

        TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);

        // Globals, gezet door SnowSurface.cs
        TEXTURE2D(_SnowTrailTex);  SAMPLER(sampler_SnowTrailTex);
        float4 _SnowArea;    // xy = minimum wereld XZ, zw = grootte in meters
        float4 _SnowParams;  // x = 1 texel in uv, y = max diepte (sneeuwdikte)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };

        float2 SnowUV(float3 positionWS)
        {
            return (positionWS.xz - _SnowArea.xy) / max(_SnowArea.zw, 0.0001);
        }

        float SampleTrail(float2 uv)
        {
            float2 m = step(0.0, uv) * step(uv, 1.0); // buiten het gebied = 0
            return SAMPLE_TEXTURE2D_LOD(_SnowTrailTex, sampler_SnowTrailTex, uv, 0).r * m.x * m.y;
        }

        // Hoogte-offset in meters (negatief = ingedeukt, positief = randje)
        float SnowOffset(float2 uv)
        {
            float t = SampleTrail(uv);
            float o = _SnowParams.x * 3.0;
            float n = (SampleTrail(uv + float2(o, 0)) + SampleTrail(uv - float2(o, 0)) +
                       SampleTrail(uv + float2(0, o)) + SampleTrail(uv - float2(0, o))) * 0.25;
            float rim = saturate(n - t);
            return -t * _SnowParams.y + rim * _RimHeight;
        }

        float3 DisplaceWS(float3 positionWS)
        {
            positionWS.y += SnowOffset(SnowUV(positionWS));
            return positionWS;
        }

        // Normal uit de hoogteverschillen (per pixel, dus scherp ook op een grover mesh)
        float3 SnowNormalWS(float3 positionWS, float3 meshNormalWS)
        {
            float2 uv = SnowUV(positionWS);
            float e = _SnowParams.x;
            float hL = SnowOffset(uv - float2(e, 0));
            float hR = SnowOffset(uv + float2(e, 0));
            float hD = SnowOffset(uv - float2(0, e));
            float hU = SnowOffset(uv + float2(0, e));
            float2 stepWS = max(e * _SnowArea.zw, 0.0001);
            float dhdx = (hR - hL) / (2.0 * stepWS.x);
            float dhdz = (hU - hD) / (2.0 * stepWS.y);
            return normalize(meshNormalWS + float3(-dhdx, 0, -dhdz) * _NormalStrength);
        }
        ENDHLSL

        // ---------------------------------------------------------------- Forward
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posWS = DisplaceWS(TransformObjectToWorld(IN.positionOS.xyz));
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            float3 ShadeLight(Light light, float3 N, float3 V, float3 albedo)
            {
                float3 radiance = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                float  NdotL = saturate(dot(N, light.direction));
                float  wrap  = saturate((dot(N, light.direction) + 0.2) / 1.2); // sneeuw = zacht licht
                float3 H = SafeNormalize(light.direction + V);
                float  spec = pow(saturate(dot(N, H)), exp2(10.0 * _Smoothness + 1.0)) * _Smoothness;
                return radiance * (albedo * wrap + spec * NdotL);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = SnowNormalWS(IN.positionWS, normalize(IN.normalWS));
                float  t = SampleTrail(SnowUV(IN.positionWS));

                float3 albedo = lerp(_BaseColor.rgb, _DeepColor.rgb, saturate(t))
                              * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;

                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));

                float3 color = SampleSH(N) * albedo;
                color += ShadeLight(mainLight, N, V, albedo);

                #if defined(_ADDITIONAL_LIGHTS)
                // Forward+ (jouw project) en gewone Forward, net als URP's eigen shaders
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = N;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                half4 shadowMask = half4(1, 1, 1, 1);
                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light dl = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                    color += ShadeLight(dl, N, V, albedo);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light l = GetAdditionalLight(lightIndex, IN.positionWS, shadowMask);
                    color += ShadeLight(l, N, V, albedo);
                LIGHT_LOOP_END
                #endif

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- Shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragEmpty
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            float4 vertShadow (Attributes IN) : SV_POSITION
            {
                float3 posWS = DisplaceWS(TransformObjectToWorld(IN.positionOS.xyz));
                float3 nWS   = TransformObjectToWorldNormal(IN.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirWS = normalize(_LightPosition - posWS);
                #else
                    float3 lightDirWS = _LightDirection;
                #endif
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDirWS));
                return ApplyShadowClamping(posCS);
            }

            half4 fragEmpty () : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- Depth
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            float4 vertDepth (Attributes IN) : SV_POSITION
            {
                return TransformWorldToHClip(DisplaceWS(TransformObjectToWorld(IN.positionOS.xyz)));
            }

            half4 fragDepth () : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- DepthNormals (SSAO e.d.)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct VaryingsDN
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            VaryingsDN vertDN (Attributes IN)
            {
                VaryingsDN OUT;
                float3 posWS = DisplaceWS(TransformObjectToWorld(IN.positionOS.xyz));
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 fragDN (VaryingsDN IN) : SV_Target
            {
                float3 n = SnowNormalWS(IN.positionWS, normalize(IN.normalWS));
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0);
                #else
                    return half4(NormalizeNormalPerPixel(n), 0);
                #endif
            }
            ENDHLSL
        }
    }
}
