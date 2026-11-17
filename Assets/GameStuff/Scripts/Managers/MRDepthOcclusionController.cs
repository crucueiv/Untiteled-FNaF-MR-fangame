using Meta.XR.EnvironmentDepth;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Como se resuelve el borde de la oclusion en los shaders propios (global <c>_MR_OcclusionMode</c>).
/// </summary>
public enum MROcclusionBlendMode
{
    /// <summary>Descarta pixeles con ruido IGN: quita el fleco oscuro de la transicion suave.</summary>
    Dither = 0,

    /// <summary>Multiplica color y alfa por la oclusion (comportamiento anterior del SDK).</summary>
    Premultiplied = 1,
}

/// <summary>
/// Oclusion de entorno (Meta Depth API) para las escenas con passthrough.
///
/// No usa hand tracking ni cambia el modo de input: la profundidad del entorno incluye las manos y
/// los mandos reales, y los shaders del proyecto (MR_DirectLightOnly, MR_HighlightOutline y
/// MR_LitOcclusion) usan <c>META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS</c> para recortarse donde el
/// mundo real esta delante.
///
/// Detalles importantes:
///  - Solo actua si <see cref="EnvironmentDepthManager.IsSupported"/> y la escena tiene passthrough.
///  - El bias NO es una propiedad del EnvironmentDepthManager: es un uniforme de shader, asi que se
///    fija con <c>Shader.SetGlobalFloat("_EnvironmentDepthBias", valor)</c>.
///  - Al desactivarse deja <see cref="OcclusionShadersMode.None"/> para no dejar las keywords
///    HARD_OCCLUSION/SOFT_OCCLUSION activas.
///  - Nunca toca <c>EnvironmentDepthManager.RemoveHands</c>.
/// </summary>
[DisallowMultipleComponent]
public class MRDepthOcclusionController : MonoBehaviour
{
    private const string DepthBiasProperty = "_EnvironmentDepthBias";
    private const string ShadowMarginProperty = "_MR_ShadowOcclusionMargin";
    private const string ShadowRelativeProperty = "_MR_ShadowOcclusionRelative";
    private const string ShadowFadeProperty = "_MR_ShadowOcclusionFade";
    private const string BlendModeProperty = "_MR_OcclusionMode";
    private const string DitherEdgeProperty = "_MR_OcclusionDither";
    private const string DitherThresholdProperty = "_MR_OcclusionDitherThreshold";
    private const string NoiseAnimProperty = "_MR_OcclusionNoiseAnim";
    private const string GrainProperty = "_MR_GrainAmount";
    private const string BounceScaleProperty = "_MR_BounceScale";
    private const string DebugViewProperty = "_MR_DebugOcclusionView";

    // --- Estado estatico (lo consultan los objetos interactuables) ---

    private static bool s_occlusionActive;

    /// <summary>
    /// True solo si la oclusion de entorno esta realmente funcionando en la escena cargada
    /// (Depth API soportada, passthrough presente y EnvironmentDepthManager habilitado).
    /// En el Editor y en las escenas VR es false.
    /// </summary>
    public static bool IsOcclusionActive => s_occlusionActive;

    /// <summary>Se dispara solo cuando <see cref="IsOcclusionActive"/> cambia de valor.</summary>
    public static event System.Action<bool> OnOcclusionActiveChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_occlusionActive = false;
        OnOcclusionActiveChanged = null;
    }

    private static void SetOcclusionActive(bool value)
    {
        if (s_occlusionActive == value)
        {
            return;
        }

        s_occlusionActive = value;
        Debug.Log($"[MRDepthOcclusion] IsOcclusionActive = {value}.");
        OnOcclusionActiveChanged?.Invoke(value);
    }

    [Header("Referencias")]
    [Tooltip("EnvironmentDepthManager de la escena (se busca solo si se deja vacio).")]
    public EnvironmentDepthManager depthManager;

    [Header("Configuracion de oclusion")]
    [Tooltip("SoftOcclusion suaviza el borde (recomendado). HardOcclusion es mas barato pero recorta duro.")]
    public OcclusionShadersMode occlusionMode = OcclusionShadersMode.SoftOcclusion;

    [Tooltip("Margen en metros que se resta a la profundidad del entorno. Subirlo empuja a los objetos virtuales hacia la camara: mas margen perdona el error de la Depth API, pero tapa antes al mundo real/mi mano. 0.12 por defecto.")]
    [Range(0f, 0.3f)]
    public float depthBias = 0.12f;

    [Header("Sombras sobre el brazo real")]
    [Tooltip("Tolerancia fija (m) de la mascara que anula el TERMINO DE SOMBRA de la capa de oscuridad cuando el mundo real esta por delante de la superficie de la habitacion. Es una tolerancia, no una distancia de deteccion: subirla conserva mas sombras sobre el passthrough (util si desaparecen sombras en paredes lejanas por ruido del sensor). 0 = cualquier cosa delante anula la sombra. La oscuridad nunca se ocluye.")]
    [Range(0f, 0.3f)]
    public float shadowOcclusionMargin = 0.04f;

    [Tooltip("Parte de la tolerancia que escala con la distancia (0.02 = 2 % de la profundidad lineal). Es el error que la Depth API acumula al alejarse.")]
    [Range(0f, 0.1f)]
    public float shadowOcclusionRelative = 0.02f;

    [Tooltip("Ancho (m) de la transicion entre conservar la sombra y anularla. Mas ancho = borde mas suave alrededor del brazo.")]
    [Range(0.01f, 0.3f)]
    public float shadowOcclusionFade = 0.06f;

    [Header("Borde de la oclusion (shaders propios)")]
    [Tooltip("Dither quita el fleco de la transicion; Premultiplicado es el modo antiguo del SDK (oscurece el color y baja el alfa).")]
    public MROcclusionBlendMode blendMode = MROcclusionBlendMode.Dither;

    [Tooltip("Dither: true = borde tramado con ruido IGN (recomendado). false = corte duro en el umbral.")]
    public bool ditherEdge = true;

    [Tooltip("Dither: umbral base cuando el ruido pesa 0 (y umbral medio cuando pesa 1).")]
    [Range(0f, 1f)]
    public float ditherThreshold = 0.5f;

    [Tooltip("Cambia el patron de ruido cada frame. Se ve mas fino en movimiento pero centellea con la cabeza quieta: dejarlo apagado.")]
    public bool animatedNoise = false;

    [Header("Realismo (globales de los shaders)")]
    [Tooltip("Grano de sensor del material de los bloques (global _MR_GrainAmount). Multiplicativo: el negro puro sigue negro. 0 = sin grano.")]
    [Range(0f, 0.4f)]
    public float grainAmount = 0.12f;

    [Tooltip("Escala del rebote de los bloques (global _MR_BounceScale). 1 = el valor del material; 0.25 lo deja casi apagado.")]
    [Range(0.25f, 2f)]
    public float bounceScale = 1f;

    [Header("Ambiente de la sala")]
    [Tooltip("Ambiente calido. Solo para escenas donde la sala real debe iluminar un poco los objetos virtuales (MR_SpringBonnieRoom). Apagado = la escena queda sin ambiente, como hasta ahora.")]
    public bool warmAmbient = false;

    [Tooltip("Color del ambiente (calido).")]
    public Color ambientColor = new Color(1f, 0.86f, 0.72f, 1f);

    [Tooltip("Intensidad del ambiente (0.10 = un 10 % de relleno).")]
    [Range(0f, 0.5f)]
    public float ambientIntensity = 0.10f;

    [Tooltip("Si la escena no tiene passthrough (escenas VR), desactiva la Depth API por completo.")]
    public bool disableWithoutPassthrough = true;

    [Header("Diagnostico")]
    public bool logState = true;

    [Tooltip("Ensenya la oclusion de entorno en escala de grises sobre los objetos virtuales (global _MR_DebugOcclusionView). Solo para depurar.")]
    public bool showOcclusionAsGrayscale = false;

    // Copia del ambiente de la escena para devolverlo al desactivar.
    private bool ambientApplied;
    private AmbientMode previousAmbientMode;
    private Color previousAmbientLight;
    private float previousAmbientIntensity;

    private void Awake()
    {
        if (depthManager == null) depthManager = GetComponent<EnvironmentDepthManager>();
        if (depthManager == null) depthManager = FindAnyObjectByType<EnvironmentDepthManager>(FindObjectsInactive.Include);

        // Los ajustes de oclusion son uniformes de shader, no propiedades de material: se fijan como
        // globales para que los lean todos los shaders (MR_DirectLightOnly y MR_LitOcclusion).
        ApplyShaderGlobals();
        ApplyAmbient();
    }

    private void Start()
    {
        if (!EnvironmentDepthManager.IsSupported)
        {
            Deactivate("la Depth API no esta soportada (Editor o visor sin soporte)");
            return;
        }

        if (disableWithoutPassthrough && !HasPassthrough())
        {
            Deactivate("la escena no tiene passthrough");
            return;
        }

        if (depthManager == null)
        {
            Deactivate("falta el componente EnvironmentDepthManager");
            return;
        }

        // El modo se puede fijar antes de recibir la textura: el manager guarda el valor y activa la
        // keyword en cuanto la profundidad esta disponible.
        depthManager.enabled = true;
        depthManager.OcclusionShadersMode = occlusionMode;

        if (logState)
        {
            Debug.Log($"[MRDepthOcclusion] Oclusion activada en modo {occlusionMode} (bias {depthBias:0.###}, " +
                      $"mascara de sombra sobre el brazo: tolerancia {shadowOcclusionMargin:0.###} m + " +
                      $"{shadowOcclusionRelative:0.###} x distancia, transicion {shadowOcclusionFade:0.###} m).");
        }

        SetOcclusionActive(true);
    }

    private void OnDisable()
    {
        ClearOcclusion();
        RestoreAmbient();
    }

    private void OnDestroy()
    {
        ClearOcclusion();
        RestoreAmbient();
    }

    private void OnValidate()
    {
        depthBias = Mathf.Clamp(depthBias, 0f, 0.3f);
        shadowOcclusionMargin = Mathf.Clamp(shadowOcclusionMargin, 0f, 0.3f);
        shadowOcclusionRelative = Mathf.Clamp(shadowOcclusionRelative, 0f, 0.1f);
        shadowOcclusionFade = Mathf.Clamp(shadowOcclusionFade, 0.01f, 0.3f);
        ditherThreshold = Mathf.Clamp01(ditherThreshold);
        grainAmount = Mathf.Clamp(grainAmount, 0f, 0.4f);
        ambientIntensity = Mathf.Clamp(ambientIntensity, 0f, 0.5f);
        ApplyShaderGlobals();
    }

    /// <summary>
    /// Publica los ajustes como uniformes GLOBALES (los lee MR_Lighting.hlsl). Los valores viven en
    /// el Editor mientras el componente este seleccionado o la escena en Play, para poder ajustarlos
    /// desde el Inspector y desde MRDebugOverlay.
    /// </summary>
    public void ApplyShaderGlobals()
    {
        Shader.SetGlobalFloat(DepthBiasProperty, depthBias);
        Shader.SetGlobalFloat(ShadowMarginProperty, shadowOcclusionMargin);
        Shader.SetGlobalFloat(ShadowRelativeProperty, shadowOcclusionRelative);
        Shader.SetGlobalFloat(ShadowFadeProperty, shadowOcclusionFade);
        Shader.SetGlobalFloat(BlendModeProperty, (float)blendMode);
        Shader.SetGlobalFloat(DitherEdgeProperty, ditherEdge ? 1f : 0f);
        Shader.SetGlobalFloat(DitherThresholdProperty, ditherThreshold);
        Shader.SetGlobalFloat(NoiseAnimProperty, animatedNoise ? 1f : 0f);
        Shader.SetGlobalFloat(GrainProperty, grainAmount);
        Shader.SetGlobalFloat(BounceScaleProperty, bounceScale);
        Shader.SetGlobalFloat(DebugViewProperty, showOcclusionAsGrayscale ? 1f : 0f);
    }

    /// <summary>
    /// Aplica el ambiente calido de la sala con <see cref="RenderSettings"/> (solo la escena cargada).
    /// La intensidad va DENTRO del color: asi el resultado no depende de si el pipeline multiplica o
    /// no <c>ambientIntensity</c> en modo Flat.
    /// </summary>
    public void ApplyAmbient()
    {
        if (!warmAmbient || ambientApplied)
        {
            return;
        }

        previousAmbientMode = RenderSettings.ambientMode;
        previousAmbientLight = RenderSettings.ambientLight;
        previousAmbientIntensity = RenderSettings.ambientIntensity;
        ambientApplied = true;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor * ambientIntensity;
        RenderSettings.ambientIntensity = 1f;

        if (logState)
        {
            Debug.Log($"[MRDepthOcclusion] Ambiente calido activado: {ambientColor} x {ambientIntensity:0.##} " +
                      $"= {RenderSettings.ambientLight}.");
        }
    }

    /// <summary>Devuelve el ambiente que tenia la escena antes de <see cref="ApplyAmbient"/>.</summary>
    public void RestoreAmbient()
    {
        if (!ambientApplied)
        {
            return;
        }

        RenderSettings.ambientMode = previousAmbientMode;
        RenderSettings.ambientLight = previousAmbientLight;
        RenderSettings.ambientIntensity = previousAmbientIntensity;
        ambientApplied = false;

        if (logState)
        {
            Debug.Log("[MRDepthOcclusion] Ambiente de la escena restaurado.");
        }
    }

    // --- API publica (Inspector, menus de debug, futuras opciones) ---

    public void SetOcclusionMode(OcclusionShadersMode mode)
    {
        occlusionMode = mode;

        if (depthManager == null || !EnvironmentDepthManager.IsSupported) return;

        depthManager.enabled = true;
        depthManager.OcclusionShadersMode = occlusionMode;
    }

    public void SetDepthBias(float bias)
    {
        depthBias = Mathf.Clamp(bias, 0f, 0.3f);
        Shader.SetGlobalFloat(DepthBiasProperty, depthBias);
    }

    /// <summary>
    /// Tolerancia (m) de la mascara de cercania de la capa de oscuridad: cuanto tiene que estar el
    /// mundo real por delante de la superficie para que deje de aplicar la sombra (la oscuridad se
    /// sigue aplicando). Subirla conserva mas sombras sobre el passthrough.
    /// </summary>
    public void SetShadowOcclusionMargin(float margin)
    {
        shadowOcclusionMargin = Mathf.Clamp(margin, 0f, 0.3f);
        Shader.SetGlobalFloat(ShadowMarginProperty, shadowOcclusionMargin);
    }

    public void SetBlendMode(MROcclusionBlendMode mode)
    {
        blendMode = mode;
        Shader.SetGlobalFloat(BlendModeProperty, (float)blendMode);
    }

    public void SetDitherEdge(bool enabled)
    {
        ditherEdge = enabled;
        Shader.SetGlobalFloat(DitherEdgeProperty, ditherEdge ? 1f : 0f);
    }

    public void SetDitherThreshold(float threshold)
    {
        ditherThreshold = Mathf.Clamp01(threshold);
        Shader.SetGlobalFloat(DitherThresholdProperty, ditherThreshold);
    }

    public void SetAnimatedNoise(bool enabled)
    {
        animatedNoise = enabled;
        Shader.SetGlobalFloat(NoiseAnimProperty, animatedNoise ? 1f : 0f);
    }

    public void SetGrainAmount(float amount)
    {
        grainAmount = Mathf.Clamp(amount, 0f, 0.4f);
        Shader.SetGlobalFloat(GrainProperty, grainAmount);
    }

    public void SetBounceScale(float scale)
    {
        bounceScale = Mathf.Clamp(scale, 0.25f, 2f);
        Shader.SetGlobalFloat(BounceScaleProperty, bounceScale);
    }

    public void SetOcclusionGrayscale(bool enabled)
    {
        showOcclusionAsGrayscale = enabled;
        Shader.SetGlobalFloat(DebugViewProperty, showOcclusionAsGrayscale ? 1f : 0f);
    }

    public void SetWarmAmbient(bool enabled)
    {
        warmAmbient = enabled;

        if (warmAmbient)
        {
            ApplyAmbient();
        }
        else
        {
            RestoreAmbient();
        }
    }

    public void SetAmbientIntensity(float intensity)
    {
        ambientIntensity = Mathf.Clamp(intensity, 0f, 0.5f);

        if (!ambientApplied)
        {
            return;
        }

        RenderSettings.ambientLight = ambientColor * ambientIntensity;
    }

    // --- Interno ---

    private void Deactivate(string reason)
    {
        if (depthManager != null)
        {
            depthManager.OcclusionShadersMode = OcclusionShadersMode.None;
            depthManager.enabled = false;
        }

        SetOcclusionActive(false);

        if (logState)
        {
            Debug.Log($"[MRDepthOcclusion] Oclusion desactivada: {reason}.");
        }
    }

    private void ClearOcclusion()
    {
        if (depthManager != null)
        {
            depthManager.OcclusionShadersMode = OcclusionShadersMode.None;
        }

        SetOcclusionActive(false);
    }

    /// <summary>
    /// Hay passthrough si la escena tiene un OVRPassthroughLayer o el OVRManager lo pide al arrancar.
    /// </summary>
    private bool HasPassthrough()
    {
        if (FindAnyObjectByType<OVRPassthroughLayer>(FindObjectsInactive.Include) != null) return true;

        OVRManager manager = FindAnyObjectByType<OVRManager>(FindObjectsInactive.Include);
        return manager != null && manager.isInsightPassthroughEnabled;
    }
}
