#ifndef MR_LIGHTING_INCLUDED
#define MR_LIGHTING_INCLUDED

// ---------------------------------------------------------------------------------------------
// Utilidades compartidas por los shaders propios del modo oscuridad MR.
// Incluir DESPUES de Core.hlsl, Lighting.hlsl y EnvironmentOcclusionURP.hlsl.
//
// Todo lo de aqui son uniformes GLOBALES (fuera del CBUFFER UnityPerMaterial) a proposito:
// el layout de UnityPerMaterial tiene que seguir siendo identico byte a byte en todos los
// pases del mismo shader para que el SRP Batcher siga siendo compatible.
// ---------------------------------------------------------------------------------------------

// Bias de profundidad global. Lo fija MRDepthOcclusionController.
float _EnvironmentDepthBias;

// Modo de oclusion: 0 = dither (por defecto), 1 = premultiplicado.
float _MR_OcclusionMode;
// Mezcla del umbral con ruido IGN: 0 = corte duro en el umbral, 1 = borde tramado.
float _MR_OcclusionDither;
// Umbral base del modo dither.
float _MR_OcclusionDitherThreshold;
// Ruido animado: 0 = patron estatico (defecto, sin parpadeo), 1 = el patron cambia cada frame
// (se ve mas fino en movimiento, pero centellea cuando la cabeza esta quieta).
float _MR_OcclusionNoiseAnim;

// Ruido IGN (interleaved gradient noise) barato y estable por pixel.
float MR_IgnNoise(float2 pixelCoord)
{
    return frac(52.9829189 * frac(dot(pixelCoord, float2(0.06711056, 0.00583715))));
}

// Grano multiplicativo (global). 0 = sin grano; 0.12 = +/-12 % de ruido por pixel.
float _MR_GrainAmount;

// Vista de depuracion de la oclusion: 0 = normal, 1 = ensena la oclusion en escala de grises.
float _MR_DebugOcclusionView;

// Escala del rebote (1 = el valor del material). Un 0 sin fijar se interpreta como 1.
float _MR_BounceScale;

// ---------------------------------------------------------------------------------------------
// Mascara de cercania de la capa de oscuridad (DarknessRevealer).
//
// Anula SOLO el termino de SOMBRA de la capa alli donde el mundo real esta claramente delante de
// la superficie de la habitacion (brazo, mano, mandos, objetos reales), para que la sombra de un
// objeto virtual no se pinte encima del brazo. La oscuridad NO se ocluye: el brazo se sigue
// oscureciendo, que es el aspecto del modo oscuridad.
//
// Son tres globales porque el margen tiene que ser ABSOLUTO + PROPORCIONAL: la Depth API tiene un
// error que crece con la distancia, asi que una tolerancia fija se queda corta en paredes lejanas
// (aparecerian huecos de sombra falsos) y sobra de cerca. No se reutiliza _EnvironmentDepthBias
// porque esta pensado para objetos virtuales sueltos, no para una capa pegada a la superficie real.
// ---------------------------------------------------------------------------------------------

// Tolerancia fija en metros: cuanto tiene que estar el mundo real por delante de la superficie
// para considerarlo un oclusor y no la propia superficie (ruido del sensor / desajuste de la malla).
float _MR_ShadowOcclusionMargin;

// Parte proporcional a la distancia del fragmento (0.02 = 2 % de la profundidad lineal).
float _MR_ShadowOcclusionRelative;

// Ancho en metros de la transicion entre "sombra intacta" y "sombra anulada".
float _MR_ShadowOcclusionFade;

// Grano de sensor: multiplica el color, asi que el negro puro sigue siendo 0 y las zonas
// oscuras (pero no nulas) son donde mas se nota.
half3 MR_ApplyGrain(half3 color, float2 pixelCoord)
{
    if (_MR_GrainAmount <= 0.0001)
    {
        return color;
    }

    // Coordenada desplazada y escalada para que el grano no se correlacione con el dither de
    // la oclusion (que usa la coordenada de pixel tal cual).
    float noise = MR_IgnNoise(pixelCoord * 1.618 + 0.5);
    return color * (half)(1.0 + (noise - 0.5) * 2.0 * _MR_GrainAmount);
}

// Aplica la oclusion de entorno de la Depth API de Meta.
//   color : solo se oscurece en modo premultiplicado (en dither se deja intacto).
//   alpha : solo se multiplica en modo premultiplicado, para que el compositor mezcle el
//           passthrough con el alfa del eye buffer.
//   depthOcclusionMask : el _DepthOcclusion del material (1 = ocluible, 0 = nunca, p. ej. lo
//           que el jugador esta sujetando).
void MR_ApplyOcclusion(float3 positionWS, float2 pixelCoord, half depthOcclusionMask,
                       inout half3 color, inout half alpha)
{
#if defined(HARD_OCCLUSION) || defined(SOFT_OCCLUSION)
    half occlusion = (half)META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(positionWS, _EnvironmentDepthBias);
    occlusion = lerp(1.0h, occlusion, saturate(depthOcclusionMask));

    // Vista de depuracion: la oclusion en escala de grises (1 = visible, 0 = tapado).
    if (_MR_DebugOcclusionView > 0.5)
    {
        color = (half3)occlusion;
        alpha = 1.0h;
        return;
    }

    if (_MR_OcclusionMode > 0.5)
    {
        // Premultiplicado: el pixel conserva su color oscurecido y su alfa, y el compositor
        // decide. Solo se descarta lo practicamente invisible.
        color *= occlusion;
        alpha *= occlusion;
        clip(occlusion - 0.01h);
    }
    else
    {
        // Dither: el color no se toca (asi no aparece el fleco oscuro de la transicion suave)
        // y el borde se resuelve descartando pixeles contra un umbral con ruido IGN.
        float2 noiseCoord = pixelCoord;
        if (_MR_OcclusionNoiseAnim > 0.5)
        {
            // Salto entero por frame (mod 64 para no perder precision): el patron cambia sin
            // desplazarse, que es lo que se aprecia como grano fino en movimiento.
            float step = fmod(floor(_Time.y * 60.0), 64.0);
            noiseCoord += step * float2(17.0, 23.0);
        }

        half threshold = (half)lerp(_MR_OcclusionDitherThreshold, MR_IgnNoise(noiseCoord),
                                    saturate(_MR_OcclusionDither));
        // Suelo de seguridad: si nadie ha fijado los globales (todo a 0) el umbral vale 0.01,
        // que es exactamente el recorte que hacia el codigo anterior.
        threshold = max(threshold, 0.01h);
        clip(occlusion - threshold);
    }
#endif
    // Sin HARD_OCCLUSION / SOFT_OCCLUSION el shader queda exactamente como estaba.
}

#endif // MR_LIGHTING_INCLUDED
