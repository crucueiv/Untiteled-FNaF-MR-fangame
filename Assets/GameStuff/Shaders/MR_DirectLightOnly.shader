Shader "Custom/MR_DirectLightOnly"
{
    // Para objetos virtuales en la oscuridad MR:
    // solo recibe luz DIRECTA (principal + adicionales, con sombras y cookies).
    // Sin luz ambiente, sin SH, sin reflejos de skybox -> sin luz = negro puro.
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

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
            // LitInput declara _BaseMap, _BaseColor, _Cutoff... y el CBUFFER UnityPerMaterial,
            // el mismo que usa el pase ShadowCaster (asi ambos pases son coherentes)
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS  = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.normalWS    = normInputs.normalWS;
                output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);
                output.shadowCoord = GetShadowCoord(posInputs);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * _BaseColor;
                float3 normalWS = normalize(input.normalWS);

                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half3 lighting = mainLight.color
                    * (saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation * mainLight.distanceAttenuation);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                    lighting += addLight.color
                        * (saturate(dot(normalWS, addLight.direction)) * addLight.shadowAttenuation * addLight.distanceAttenuation);
                LIGHT_LOOP_END

                // NOTA: aqui NO se suma ningun termino ambiente (SampleSH, reflection probes, etc.)
                half3 color = albedo.rgb * lighting;
                return half4(color, 1.0h);
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
