Shader "Custom/MR_HighlightOutline"
{
    // Contorno "inverted hull" para objetos interactuables que deben verse en la oscuridad.
    //  - Unlit: no depende de ninguna luz (se ve aunque el objeto este negro).
    //  - Extrusion por normal en espacio de vista y compensada por distancia -> grosor
    //    constante en pantalla a cualquier distancia.
    //  - Aditivo con color HDR: solo ilumina el borde, nunca oscurece el objeto original.
    //  - Pulso suave opcional calculado en el shader con _Time (coste cero en CPU).
    //  - Preparado para VR con Single Pass Instanced.
    Properties
    {
        [HDR] _OutlineColor ("Color del contorno", Color) = (0.25, 0.85, 1, 1)
        _OutlineWidth ("Grosor (fraccion de pantalla)", Range(0.001, 0.05)) = 0.012
        _PulseEnabled ("Pulso activo (1 si / 0 no)", Range(0, 1)) = 1
        _PulseSpeed ("Velocidad del pulso", Range(0, 10)) = 2.5
        _PulseMin ("Brillo minimo del pulso", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front          // solo las caras traseras del hull -> contorno en el silueta
            ZWrite Off
            ZTest LEqual
            Blend One One       // aditivo: aclarar el borde, nunca tapar nada

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half _OutlineWidth;
                half _PulseEnabled;
                half _PulseSpeed;
                half _PulseMin;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                // Trabajamos en espacio de vista para que el grosor no dependa de la distancia.
                float3 positionVS = TransformWorldToView(positionWS);
                float3 normalVS    = TransformWorldToViewDir(normalWS, true);

                float distancia = max(-positionVS.z, 0.001);
                positionVS.xy += normalVS.xy * (_OutlineWidth * distancia);

                output.positionCS = TransformWViewToHClip(positionVS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half pulse = 1.0h;
                if (_PulseEnabled > 0.5h)
                {
                    pulse = lerp(_PulseMin, 1.0h, sin(_Time.y * _PulseSpeed) * 0.5h + 0.5h);
                }

                // Blend One One: el alfa no se usa, el color HDR marca la intensidad del borde.
                return half4(_OutlineColor.rgb * pulse, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
