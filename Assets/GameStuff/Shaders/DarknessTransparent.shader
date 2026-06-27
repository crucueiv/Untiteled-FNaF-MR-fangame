Shader "Custom/MR_DarknessRevealer"
{
    Properties
    {
        [HDR] _BaseColor("Color de Oscuridad", Color) = (0, 0, 0, 1)
        _LightSensitivity("Sensibilidad a la Luz (Transparencia)", Float) = 2.0
        _LightTint("Intensidad de Cookie/Luz sobre Passthrough", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // NUEVO BLEND: Premultiplied Alpha. 
            // Nos permite sumar la luz sobre el passthrough mientras restamos la oscuridad
            Blend One OneMinusSrcAlpha
            ZWrite On 
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex LitVertex
            #pragma fragment LitFragment

            // Pragmas de URP para Luces
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            
            // Pragmas CLAVE para que las sombras de la linterna funcionen sobre este material
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            // Pragma CLAVE para leer la textura de la Cookie
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _LightSensitivity;
                half _LightTint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings LitVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                half3 totalLightColor = half3(0, 0, 0);

                // 1. LUZ PRINCIPAL
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL_main = saturate(dot(input.normalWS, mainLight.direction));
                totalLightColor += mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation * NdotL_main;

                // 2. LUCES ADICIONALES (Tu Linterna)
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                    
                    half NdotL_add = saturate(dot(input.normalWS, additionalLight.direction));
                    
                    // Aquí se integra la sombra (shadowAttenuation) y la Cookie (incluida en el color/distance)
                    half3 addContribution = additionalLight.color * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation * NdotL_add;
                    
                    totalLightColor += addContribution;
                LIGHT_LOOP_END

                // 3. CÁLCULO DE TRANSPARENCIA Y COLOR PREMULTIPLICADO
                half totalLightIntensity = Luminance(totalLightColor);

                // Alpha representa la oscuridad (1 = negro sólido, 0 = deja ver la vida real)
                half alpha = saturate(1.0 - (totalLightIntensity * _LightSensitivity));

                // Magia del Alpha Premultiplicado:
                // Sumamos la luz de la linterna multiplicada por el Tinte (para dibujar la cookie encima del mundo real)
                // Y a eso le sumamos la oscuridad base, multiplicada por su propio alpha para que se desvanezca al recibir luz
                half3 finalColor = (totalLightColor * _LightTint) + (_BaseColor.rgb * alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // Pase de sombras para que esta pared pueda proyectar sombras a otros objetos si es necesario
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}