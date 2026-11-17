Shader "Custom/MR_ContactBlob"
{
    // Sombra de contacto blanda bajo un objeto virtual (manos, pies, cuerpo) sobre la superficie
    // real mas cercana (anclas de MRUK). Es un quad transparente en premultiplicado:
    //  - Blend One OneMinusSrcAlpha: el alfa hace de cobertura, sin fleco.
    //  - ZWrite Off y Cola Transparent: no tapa nada, se mezcla sobre el passthrough.
    //  - Se ocluye con la Depth API de Meta, asi que mi brazo real tapa el blob.
    // Coste: un quad (2 triangulos) + una lectura de la textura de profundidad por pixel.
    Properties
    {
        _Color ("Color (RGB) / opacidad (A)", Color) = (0, 0, 0, 1)
        _Opacity ("Opacidad", Range(0, 1)) = 0.55
        _Softness ("Blandura del borde", Range(0.05, 1)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Blob"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // VR: Single Pass Instanced necesita instancing en el shader.
            #pragma multi_compile_instancing
            // Depth API: la variante sin keyword deja el blob sin ocluir (Editor o visor sin soporte).
            #pragma multi_compile_fragment _ HARD_OCCLUSION SOFT_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Meta Depth API: META_DEPTH_* y _EnvironmentDepthBias (global, lo fija MRDepthOcclusionController).
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"

            // Bias de profundidad global (fuera del CBUFFER: lo fija MRDepthOcclusionController).
            float _EnvironmentDepthBias;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacity;
                half _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv         : TEXCOORD1;
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
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Degradado radial: 1 en el centro, 0 en el borde del quad.
                half distanceFromCenter = (half)length(input.uv * 2.0 - 1.0);
                half edge = 1.0h - smoothstep(0.0h, 1.0h, distanceFromCenter);
                // Exponente controlado por _Softness: mas blando = caida mas tendida.
                half falloff = lerp(2.5h, 1.0h, saturate(_Softness));
                half alpha = pow(edge, falloff) * _Opacity * _Color.a;

                half4 color = half4(_Color.rgb * alpha, alpha);

                // Oclusion de entorno: premultiplicado, para que el brazo real tape el blob.
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY_WORLDPOS(input.positionWS, color, _EnvironmentDepthBias);

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
