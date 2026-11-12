Shader "Custom/MR_UltimateDarknessRevealer"
{
    Properties
    {
        _DarknessColor ("Color de la Oscuridad", Color) = (0, 0, 0, 1)
        _LightSensitivity ("Sensibilidad a la Luz", Range(0.1, 10)) = 2.0
        _TintStrength ("Fuerza del Tinte de Linterna", Range(0, 1)) = 0.5
        _CookieStrength ("Intensidad de la Cookie", Range(0, 5)) = 1.0
        // NUEVO: 0 = ignora el angulo de la superficie (el cono se ve igual en suelo, techo y paredes)
        //        1 = comportamiento anterior (NdotL completo)
        _NdotLInfluence ("Influencia del angulo (NdotL)", Range(0, 1)) = 0.0
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
                float4 shadowCoord : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _DarknessColor;
                half _LightSensitivity;
                half _TintStrength;
                half _CookieStrength;
                half _NdotLInfluence;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS  = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.normalWS    = normInputs.normalWS;
                output.shadowCoord = GetShadowCoord(posInputs);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                // En una pared de MRUK nos interesa "esta zona esta dentro del cono de luz",
                // no "cuanto de perpendicular llega la luz". Por eso NdotL es opcional:
                // con _NdotLInfluence = 0 el factor vale 1 y las esquinas de 90 grados
                // ya no se oscurecen al apuntar desde arriba.
                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half NdotL_main = lerp(1.0h, saturate(dot(normalWS, mainLight.direction)), _NdotLInfluence);
                half3 totalLightColor = mainLight.color
                    * (NdotL_main * mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half NdotL_add = lerp(1.0h, saturate(dot(normalWS, addLight.direction)), _NdotLInfluence);
                    // En URP, distanceAttenuation de un foco ya incluye la atenuacion del cono
                    half3 addLightColor = addLight.color
                        * (NdotL_add * addLight.shadowAttenuation * addLight.distanceAttenuation)
                        * _CookieStrength;
                    totalLightColor += addLightColor;
                LIGHT_LOOP_END

                half lightIntensity  = Luminance(totalLightColor) * _LightSensitivity;
                half darknessFactor  = saturate(1.0 - lightIntensity);

                half alpha = darknessFactor;

                half3 finalRGB = _DarknessColor.rgb + totalLightColor * (_TintStrength * darknessFactor);
                finalRGB = saturate(finalRGB);

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
