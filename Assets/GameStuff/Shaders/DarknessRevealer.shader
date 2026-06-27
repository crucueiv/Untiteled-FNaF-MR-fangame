Shader "Custom/MR_UltimateDarknessRevealer"
{
    Properties
    {
        _DarknessColor ("Color de la Oscuridad", Color) = (0, 0, 0, 1)
        _LightSensitivity ("Sensibilidad a la Luz", Range(0.1, 10)) = 2.0
        _TintStrength ("Fuerza del Tinte de Linterna", Range(0, 1)) = 0.5
        _CookieStrength ("Intensidad de la Cookie", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+100" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend One OneMinusSrcAlpha 
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2; // ← NUEVO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _DarknessColor;
                half _LightSensitivity;
                half _TintStrength;
                half _CookieStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // FIX 1: GetVertexPositionInputs + GetShadowCoord
                // Maneja correctamente cascade shadows Y screen-space shadows de Unity 6
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS  = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.normalWS    = normInputs.normalWS;
                output.shadowCoord = GetShadowCoord(posInputs); // ← calcula aquí, no en fragment
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                // FIX 1: shadowCoord viene del vertex shader
                // La versión de 3 args también aplica la cookie de la luz principal
                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half NdotL_main = saturate(dot(normalWS, mainLight.direction));
                half3 totalLightColor = mainLight.color
                    * (NdotL_main * mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    // La versión con shadowMask activa sombras Y cookies correctamente
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half NdotL_add = saturate(dot(normalWS, addLight.direction));
                    // addLight.color ya lleva la cookie aplicada cuando _LIGHT_COOKIES está activo
                    half3 addLightColor = addLight.color
                        * (NdotL_add * addLight.shadowAttenuation * addLight.distanceAttenuation)
                        * _CookieStrength;
                    totalLightColor += addLightColor;
                LIGHT_LOOP_END

                half lightIntensity  = Luminance(totalLightColor) * _LightSensitivity;
                half darknessFactor  = saturate(1.0 - lightIntensity);

                // FIX 2: Alpha premultiplicado correcto — sin color aditivo en zonas iluminadas
                //
                // ANTES: addedColor = lerp(totalLightColor * _TintStrength, _DarknessColor, darknessFactor)
                //   → En zonas iluminadas (darknessFactor≈0, alpha≈0), Blend One OneMinusSrcAlpha
                //     sumaba 'totalLightColor * _TintStrength' encima del mundo real → zona blanca.
                //   → La cookie quedaba enmascarada por ese glow aditivo.
                //
                // AHORA: en zonas iluminadas el fragmento es 100% transparente (rgb=0, alpha=0).
                //   La variación espacial de la cookie se convierte en variación de darknessFactor:
                //   cookie abierta → luz → darknessFactor bajo → transparente (mundo real visible)
                //   cookie cerrada → sin luz → darknessFactor alto → opaco negro
                //   Eso ES el patrón de la cookie proyectado sobre la oscuridad.
                half alpha = darknessFactor;

                // Tinte opcional: sutil variación en la oscuridad según el color de la linterna,
                // sin añadir brillo aditivo al mundo real
                half3 finalRGB = _DarknessColor.rgb + totalLightColor * (_TintStrength * darknessFactor);
                finalRGB = saturate(finalRGB);

                // Alpha premultiplicado: rgb *= alpha  para que Blend One OneMinusSrcAlpha sea correcto
                return half4(finalRGB * alpha, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}