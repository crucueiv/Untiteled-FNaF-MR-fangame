using Meta.XR.EnvironmentDepth;
using UnityEngine;

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

    [Tooltip("Margen en metros que se resta a la profundidad del entorno. 0.06 es el valor que ya usa HighlightsAndShadows.")]
    [Range(0f, 0.3f)]
    public float depthBias = 0.06f;

    [Tooltip("Si la escena no tiene passthrough (escenas VR), desactiva la Depth API por completo.")]
    public bool disableWithoutPassthrough = true;

    [Header("Diagnostico")]
    public bool logState = true;

    private void Awake()
    {
        if (depthManager == null) depthManager = GetComponent<EnvironmentDepthManager>();
        if (depthManager == null) depthManager = FindAnyObjectByType<EnvironmentDepthManager>(FindObjectsInactive.Include);

        // El bias es un uniforme de shader: se fija como global para que lo lean todos los shaders.
        Shader.SetGlobalFloat(DepthBiasProperty, depthBias);
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
            Debug.Log($"[MRDepthOcclusion] Oclusion activada en modo {occlusionMode} (bias {depthBias:0.###}).");
        }

        SetOcclusionActive(true);
    }

    private void OnDisable() => ClearOcclusion();

    private void OnDestroy() => ClearOcclusion();

    private void OnValidate()
    {
        depthBias = Mathf.Clamp(depthBias, 0f, 0.3f);
        Shader.SetGlobalFloat(DepthBiasProperty, depthBias);
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
