using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Contorno reutilizable para objetos interactuables. Se ve en la oscuridad porque el
/// material del contorno es unlit y aditivo (independiente de cualquier luz).
///
/// - Al activarse el objeto (OnEnable) muestra el contorno si "mostrarAlActivar" esta marcado.
/// - Show() / Hide() publicos.
/// - Al primer agarre (Oculus GrabInteractable, estado Select) el contorno se oculta para
///   siempre, salvo que se vuelva a llamar a Show() o a ResetHighlight().
///
/// El objeto original NO se toca: cada malla con MeshFilter recibe una cascara hija con el
/// material COMPARTIDO de Resources/HighlightOutline.mat y un MaterialPropertyBlock para el
/// color/grosor/pulso (cero instancias de material por objeto).
/// </summary>
[DefaultExecutionOrder(-20)]
public sealed class InteractableHighlight : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Color del borde. Puede pasarse de 1 para que brille (HDR).")]
    [ColorUsage(true, true)]
    public Color outlineColor = new Color(0.25f, 0.85f, 1f, 1f);

    [Tooltip("Grosor del borde como fraccion de la pantalla (0.012 = 1.2% de la altura).")]
    [Range(0.001f, 0.05f)]
    public float outlineWidth = 0.012f;

    [Header("Pulso")]
    [Tooltip("Pulso suave para llamar la atencion. Se calcula en el shader (coste cero en CPU).")]
    public bool pulsar = true;

    [Range(0f, 10f)]
    public float pulseSpeed = 2.5f;

    [Tooltip("Brillo minimo del pulso respecto al color del contorno.")]
    [Range(0f, 1f)]
    public float pulseMin = 0.35f;

    [Header("Comportamiento")]
    [Tooltip("Muestra el contorno automaticamente al activarse el objeto.")]
    public bool mostrarAlActivar = true;

    [Tooltip("Oculta el contorno para siempre en el primer agarre.")]
    public bool ocultarAlPrimerAgarre = true;

    [Tooltip("Material del contorno. Si se deja vacio se carga Resources/HighlightOutline.mat.")]
    public Material outlineMaterial;

    private const string NombreCascara = "__HighlightOutline";
    private const string RutaMaterialPorDefecto = "HighlightOutline";

    private readonly List<GameObject> cascaras = new List<GameObject>();
    private readonly List<GrabInteractable> agarrables = new List<GrabInteractable>();
    private MaterialPropertyBlock propiedades;
    private Material materialCompartido;
    private bool construido;
    private bool visible;
    private bool hasBeenGrabbed;

    /// <summary>True si el objeto ya se ha agarrado al menos una vez.</summary>
    public bool HasBeenGrabbed => hasBeenGrabbed;

    /// <summary>True si el contorno esta construido y visible.</summary>
    public bool IsVisible => visible;

    private void OnEnable()
    {
        if (mostrarAlActivar && !hasBeenGrabbed)
        {
            Show();
        }
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        SoltarEventos();
    }

    // ---------------------------------------------------------------- API publica

    /// <summary>Muestra el contorno (lo reconstruye si hizo falta).</summary>
    public void Show()
    {
        Construir();
        SetVisible(true);
    }

    /// <summary>Oculta el contorno sin destruirlo.</summary>
    public void Hide()
    {
        SetVisible(false);
    }

    /// <summary>Vuelve a permitir el contorno despues de un agarre y lo muestra.</summary>
    public void ResetHighlight()
    {
        hasBeenGrabbed = false;
        Show();
    }

    /// <summary>Cambia el color en caliente (todos los objetos que compartan este componente).</summary>
    public void SetColor(Color color)
    {
        outlineColor = color;
        AplicarPropiedades();
    }

    /// <summary>Helper para objetos que se crean por codigo.</summary>
    public static InteractableHighlight Attach(GameObject objetivo)
    {
        if (objetivo == null)
        {
            return null;
        }

        var existente = objetivo.GetComponent<InteractableHighlight>();
        if (existente != null)
        {
            existente.Show();
            return existente;
        }

        var nuevo = objetivo.AddComponent<InteractableHighlight>();
        nuevo.Show();
        return nuevo;
    }

    // ---------------------------------------------------------------- Construccion

    private void Construir()
    {
        if (construido)
        {
            return;
        }

        if (materialCompartido == null)
        {
            materialCompartido = outlineMaterial != null
                ? outlineMaterial
                : Resources.Load<Material>(RutaMaterialPorDefecto);

            if (materialCompartido == null)
            {
                Debug.LogError("[InteractableHighlight] No se encontro Resources/" + RutaMaterialPorDefecto +
                               ".mat; el contorno no se puede dibujar.");
                return;
            }
        }

        if (propiedades == null)
        {
            propiedades = new MaterialPropertyBlock();
        }

        // Se recogen primero las mallas para no crear cascaras sobre las cascaras.
        var objetivos = new List<MeshFilter>();
        foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null)
            {
                continue;
            }

            if (mf.gameObject.name == NombreCascara || mf.transform.IsChildOf(transform) == false)
            {
                continue;
            }

            if (!EsLodVisible(mf))
            {
                continue;
            }

            objetivos.Add(mf);
        }

        for (int i = 0; i < objetivos.Count; i++)
        {
            MeshFilter origen = objetivos[i];

            var go = new GameObject(NombreCascara);
            go.layer = origen.gameObject.layer;
            go.transform.SetParent(origen.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var filtro = go.AddComponent<MeshFilter>();
            filtro.sharedMesh = origen.sharedMesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materialCompartido;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            cascaras.Add(go);
        }

        construido = true;
        AplicarPropiedades();
        EngancharAgarres();

        Debug.Log("[InteractableHighlight] Contorno creado en '" + name + "' (" + cascaras.Count + " malla(s)).");
    }

    private static bool EsLodVisible(MeshFilter filtro)
    {
        LODGroup grupo = filtro.GetComponentInParent<LODGroup>();
        if (grupo == null)
        {
            return true;
        }

        LOD[] lods = grupo.GetLODs();
        if (lods == null || lods.Length == 0)
        {
            return true;
        }

        // Solo el LOD0: los demas niveles son mallas de menor detalle.
        Renderer[] lod0 = lods[0].renderers;
        for (int i = 0; i < lod0.Length; i++)
        {
            if (lod0[i] == null)
            {
                continue;
            }

            if (lod0[i].GetComponent<MeshFilter>() == filtro)
            {
                return true;
            }
        }

        return false;
    }

    private void AplicarPropiedades()
    {
        if (propiedades == null)
        {
            return;
        }

        propiedades.SetColor("_OutlineColor", outlineColor);
        propiedades.SetFloat("_OutlineWidth", outlineWidth);
        propiedades.SetFloat("_PulseEnabled", pulsar ? 1f : 0f);
        propiedades.SetFloat("_PulseSpeed", pulseSpeed);
        propiedades.SetFloat("_PulseMin", pulseMin);

        for (int i = 0; i < cascaras.Count; i++)
        {
            GameObject cascara = cascaras[i];
            if (cascara == null)
            {
                continue;
            }

            var renderer = cascara.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(propiedades);
            }
        }
    }

    private void SetVisible(bool nuevoEstado)
    {
        visible = nuevoEstado;

        for (int i = 0; i < cascaras.Count; i++)
        {
            if (cascaras[i] != null)
            {
                cascaras[i].SetActive(nuevoEstado);
            }
        }
    }

    // ---------------------------------------------------------------- Agarre

    private void EngancharAgarres()
    {
        if (!ocultarAlPrimerAgarre)
        {
            return;
        }

        SoltarEventos();
        GetComponentsInChildren(true, agarrables);

        for (int i = 0; i < agarrables.Count; i++)
        {
            if (agarrables[i] == null)
            {
                continue;
            }

            agarrables[i].WhenStateChanged += AlCambiarEstado;
            Debug.Log("[InteractableHighlight] Escuchando el agarre de '" + agarrables[i].name + "'.");
        }

        if (agarrables.Count == 0)
        {
            Debug.LogWarning("[InteractableHighlight] '" + name +
                             "' no tiene ningun GrabInteractable; el contorno no se ocultara al agarrar.");
        }
    }

    private void SoltarEventos()
    {
        for (int i = 0; i < agarrables.Count; i++)
        {
            if (agarrables[i] != null)
            {
                agarrables[i].WhenStateChanged -= AlCambiarEstado;
            }
        }

        agarrables.Clear();
    }

    private void AlCambiarEstado(InteractableStateChangeArgs args)
    {
        if (args.NewState != InteractableState.Select)
        {
            return;
        }

        hasBeenGrabbed = true;
        SetVisible(false);

        Debug.Log("[InteractableHighlight] '" + name + "' agarrado: contorno oculto definitivamente.");

        if (ocultarAlPrimerAgarre)
        {
            DestruirCascaras();
        }
    }

    private void DestruirCascaras()
    {
        for (int i = 0; i < cascaras.Count; i++)
        {
            if (cascaras[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(cascaras[i]);
                }
                else
                {
                    DestroyImmediate(cascaras[i]);
                }
            }
        }

        cascaras.Clear();
        construido = false;
    }
}
