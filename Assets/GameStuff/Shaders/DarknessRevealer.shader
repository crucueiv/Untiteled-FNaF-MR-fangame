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

        // Margen de profundidad: empuja la capa de oscuridad hacia atras (alejandola de la camara)
        // a lo largo del vector de vista. Un objeto virtual que quede unos centimetros por debajo
        // de la malla de MRUK deja de quedar cubierto por ella. 0 = comportamiento anterior.
        [Header(Margen de profundidad)]
        _DepthPushBack ("Margen de profundidad (m)", Range(0, 0.1)) = 0.02
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

            // VR: Single Pass Instanced necesita instancing en el shader.
            #pragma multi_compile_instancing
            // Depth API de Meta: variante sin keyword = capa de oscuridad como estaba (Editor / VR).
            #pragma multi_compile_fragment _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // Meta Depth API: META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS (1 = visible, 0 = tapado).
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"
            // Globales compartidos (_MR_ShadowOcclusionMargin, _MR_DebugOcclusionView...).
            #include "MR_Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _DarknessColor;
                half _LightSensitivity;
                half _TintStrength;
                half _CookieStrength;
                half _NdotLInfluence;
                half _DepthPushBack;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS);

                // La posicion que se usa para ILUMINAR es la original (no se toca la logica de luz ni de
                // sombras). Solo la profundidad de la capa se empuja hacia atras, alejandola de la camara
                // a lo largo del vector de vista, para que no tape objetos virtuales que quedan unos
                // milimetros o centimetros por debajo de la malla de MRUK.
                float3 viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                float3 pushedPositionWS = posInputs.positionWS - viewDirWS * _DepthPushBack;

                output.positionCS  = TransformWorldToHClip(pushedPositionWS);
                output.positionWS  = posInputs.positionWS;
                output.normalWS    = normInputs.normalWS;
                output.shadowCoord = GetShadowCoord(posInputs);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);

                // ---------- Mascara de cercania (Depth API de Meta) ----------
                // El brazo/mano real NO debe tapar la oscuridad (para eso esta la capa), pero tampoco
                // debe recibir la sombra de un objeto virtual: se anula SOLO el termino de sombra en
                // los pixeles cuyo mundo real esta claramente por delante de la superficie de la
                // habitacion.
                //
                // Se comparan profundidades LINEALES en metros (la del fragmento, sacada de la matriz
                // de reproyeccion del Depth API, contra la de la textura de profundidad). NO se usa
                // META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS a proposito: eso devuelve un valor de
                // VISIBILIDAD (0/1 ruidoso en HARD, campo suavizado y dilatado en SOFT) y esta capa
                // esta pegada a la superficie real, o sea justo en su transicion: el 1 - valor salia
                // > 0 en toda la habitacion y borraba las sombras del passthrough entero.
                half nearField = 0.0h;
#if defined(HARD_OCCLUSION) || defined(SOFT_OCCLUSION)
                const float4 depthSpace = mul(_EnvironmentDepthReprojectionMatrices[unity_StereoEyeIndex],
                                              float4(input.positionWS, 1.0));
                const float2 depthUv = saturate((depthSpace.xy / depthSpace.w + 1.0) * 0.5);
                // Profundidad lineal del fragmento en el espacio de la camara de profundidad.
                const float sceneLinear = (1.0 / ((depthSpace.z / depthSpace.w) + _EnvironmentDepthZBufferParams.y))
                                        * _EnvironmentDepthZBufferParams.x;
                // Profundidad lineal del mundo real en ese pixel (10000 = sin datos).
                const float envLinear = SampleEnvironmentDepthLinear(depthUv);

                // Validacion: si la Depth API aun no ha publicado sus globales o el pixel no tiene
                // profundidad, no se anula NADA. Un fallo aqui dejaria sin sombras toda la habitacion.
                const bool depthUsable = (_EnvironmentDepthZBufferParams.x != 0.0)
                                      && (abs(depthSpace.w) > 1e-5)
                                      && (sceneLinear > 0.02)
                                      && (envLinear > 0.02) && (envLinear < 1000.0);

                // Metros que el mundo real esta POR DELANTE de la superficie (> 0 = hay un oclusor).
                const float inFront = depthUsable ? (sceneLinear - envLinear) : -1.0;
                // Tolerancia: fija + proporcional a la distancia (el error del sensor crece con ella).
                const float margin = max(_MR_ShadowOcclusionMargin, 0.0)
                                   + max(_MR_ShadowOcclusionRelative, 0.0) * max(sceneLinear, 0.0);
                const float fade = max(_MR_ShadowOcclusionFade, 0.005);
                nearField = (half)smoothstep(margin, margin + fade, inFront);
#endif

                // Vista de depuracion: 1 = geometria real delante (sin sombra), 0 = superficie real.
                if (_MR_DebugOcclusionView > 0.5)
                {
                    return half4(nearField, nearField, nearField, 1.0h);
                }

                // En una pared de MRUK nos interesa "esta zona esta dentro del cono de luz",
                // no "cuanto de perpendicular llega la luz". Por eso NdotL es opcional:
                // con _NdotLInfluence = 0 el factor vale 1 y las esquinas de 90 grados
                // ya no se oscurecen al apuntar desde arriba.
                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                // Sin sombra sobre el mundo real cercano (brazo, mandos, objetos apoyados delante).
                half mainShadow = lerp(mainLight.shadowAttenuation, 1.0h, nearField);
                half NdotL_main = lerp(1.0h, saturate(dot(normalWS, mainLight.direction)), _NdotLInfluence);
                half3 totalLightColor = mainLight.color
                    * (NdotL_main * mainShadow * mainLight.distanceAttenuation);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    half NdotL_add = lerp(1.0h, saturate(dot(normalWS, addLight.direction)), _NdotLInfluence);
                    half addShadow = lerp(addLight.shadowAttenuation, 1.0h, nearField);
                    // En URP, distanceAttenuation de un foco ya incluye la atenuacion del cono
                    half3 addLightColor = addLight.color
                        * (NdotL_add * addShadow * addLight.distanceAttenuation)
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
