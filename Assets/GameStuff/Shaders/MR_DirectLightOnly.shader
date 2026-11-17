Shader "Custom/MR_DirectLightOnly"
{
    // Objetos virtuales en la oscuridad MR:
    //  - SOLO luz directa (principal + adicionales, con sombras y cookies).
    //  - Sin luz ambiente, sin SH, sin reflejos de skybox -> sin luz = negro puro y OPACO.
    //  - El corte N.L se suaviza con _LightWrap/_LightContrast/_LightBoost (nunca con ambiente).
    //  - Bias de sombra propio (_ShadowNormalBias) para evitar acne de auto-sombra.
    //  - Preparado para VR con Single Pass Instanced (multi_compile_instancing + macros stereo).
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Sombreado directo)]
        _LightWrap ("Wrap (suavizado del corte N.L)", Range(0, 1)) = 0.4
        _LightContrast ("Contraste de la luz", Range(0.2, 3)) = 0.8
        _LightBoost ("Multiplicador de luz", Range(0, 6)) = 1.6
        // Relleno por luz que llega: NO depende de N.L ni de la sombra, asi que un objeto
        // tapado por otro deja de ser negro puro. 0 = comportamiento anterior.
        _LightBounce ("Rebote / relleno", Range(0, 1)) = 0.15

        [Header(Sombras)]
        _ShadowsOn ("Recibir sombras (1 si / 0 no)", Range(0, 1)) = 1
        _ShadowNormalBias ("Bias normal de la sombra", Range(0, 0.2)) = 0.02

        [Header(Oclusion por profundidad)]
        // 1 = la mano/mando real tapa al objeto. 0 = nunca se ocluye (objetos que el jugador sujeta).
        _DepthOcclusion ("Oclusion por profundidad (1 si / 0 no)", Range(0, 1)) = 1
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
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // VR: Single Pass Instanced necesita instancing en el shader.
            #pragma multi_compile_instancing
            // Oclusion de entorno (Meta Depth API): solo fragment para no multiplicar variantes de vertice.
            // El valor '_' (sin keyword) deja el shader exactamente como estaba.
            #pragma multi_compile_fragment _ HARD_OCCLUSION SOFT_OCCLUSION
            // Forward+ (por si el renderer cambia de Forward a Forward+ en el futuro).
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            // Todas las variantes de sombra suave de URP 17 (si falta la que usa el pipeline,
            // SampleShadowmap devuelve atenuacion indefinida y el objeto sale negro por zonas).
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // Meta Depth API: META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS (1 = visible, 0 = tapado).
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"
            // MR_ApplyOcclusion + _EnvironmentDepthBias + _MR_OcclusionMode (globales).
            #include "MR_Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _LightWrap;
                half _LightContrast;
                half _LightBoost;
                half _LightBounce;
                half _ShadowsOn;
                half _ShadowNormalBias;
                half _DepthOcclusion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = normInputs.normalWS;
                output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);

                // La posicion con la que se calcula la sombra se desplaza por la normal:
                // sin esto el propio cubo se auto-sombrea (acne) y aparecen zonas negras.
                float3 shadowPositionWS = posInputs.positionWS + normInputs.normalWS * _ShadowNormalBias;
                output.shadowCoord = TransformWorldToShadowCoord(shadowPositionWS);

                return output;
            }

            // Reparte la energia de una luz con el corte suave (wrap) y devuelve solo la parte difusa.
            half DiffuseTerm(half3 normalWS, half3 lightDirection, half attenuation)
            {
                half wrap = max(_LightWrap, 0.0h);
                half ndl = saturate((dot(normalWS, lightDirection) + wrap) / (1.0h + wrap));
                return ndl * attenuation;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedoSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = albedoSample.rgb * _BaseColor.rgb;
                float3 normalWS = normalize(input.normalWS);
                float3 shadowPositionWS = input.positionWS + normalWS * _ShadowNormalBias;

                // ---------- Luz principal ----------
                Light mainLight = GetMainLight(input.shadowCoord, shadowPositionWS, half4(1, 1, 1, 1));
                half mainAttenuation = lerp(1.0h, mainLight.shadowAttenuation, saturate(_ShadowsOn))
                                     * mainLight.distanceAttenuation;
                half3 lighting = mainLight.color * DiffuseTerm(normalWS, mainLight.direction, mainAttenuation);

                // Rebote: la misma luz ya consultada, sin N.L y sin sombra (solo la atenuacion del
                // foco). No hace ninguna llamada extra a GetAdditionalLight: coste 0 de muestreo.
                half3 bounce = mainLight.color * mainLight.distanceAttenuation;

                // ---------- Luces adicionales (point / spot), con sombras y cookies ----------
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, shadowPositionWS, half4(1, 1, 1, 1));
                    half addAttenuation = lerp(1.0h, addLight.shadowAttenuation, saturate(_ShadowsOn))
                                        * addLight.distanceAttenuation;
                    lighting += addLight.color * DiffuseTerm(normalWS, addLight.direction, addAttenuation);
                    bounce += addLight.color * addLight.distanceAttenuation;
                LIGHT_LOOP_END

                // ---------- SIN termino ambiente (aqui no hay SampleSH ni reflection probes) ----------
                // Contraste < 1 levanta los medios tonos; 0 ^ x sigue siendo 0 -> negro puro sin luz.
                half contrast = max(_LightContrast, 0.01h);
                lighting = pow(max(lighting, (half3)0), contrast) * _LightBoost;

                // ---------- Rebote: relleno que NO depende de N.L ni de la sombra ----------
                // Sin luz que llegue el rebote es 0 y el objeto sigue siendo negro puro; tapado por
                // otro objeto sigue recibiendo esta parte, asi que deja de salir negro.
                lighting += bounce * _LightBounce * (_MR_BounceScale > 0.0001h ? (half)_MR_BounceScale : 1.0h);

                half3 color = albedo * lighting;

                // ---------- Grano de sensor (global _MR_GrainAmount) ----------
                // Multiplicativo: el negro puro sigue siendo 0 y se nota sobre todo en las zonas
                // oscuras pero iluminadas. Coste: un frac/dot por pixel, sin muestras extra.
                color = MR_ApplyGrain(color, input.positionCS.xy);

                half4 result = half4(color, 1.0h);

                // ---------- Oclusion por profundidad (manos y mandos reales) ----------
                // Dos modos conmutables con el global _MR_OcclusionMode (ver MR_Lighting.hlsl).
                MR_ApplyOcclusion(input.positionWS, input.positionCS.xy, _DepthOcclusion,
                                  result.rgb, result.a);

                return result;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // Mismo CBUFFER que el pase ForwardLit para que el SRP Batcher sea compatible.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _LightWrap;
                half _LightContrast;
                half _LightBoost;
                half _LightBounce;
                half _ShadowsOn;
                half _ShadowNormalBias;
                half _DepthOcclusion;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
