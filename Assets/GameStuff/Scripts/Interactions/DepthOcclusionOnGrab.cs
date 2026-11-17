using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Desactiva la oclusion por profundidad de un objeto interactuable SOLO mientras el jugador lo
/// esta sujetando, y la restaura (con un pequeno margen) al soltarlo.
///
/// - El sistema de agarre es el Interaction SDK de Meta: <see cref="GrabInteractable"/> (WhenStateChanged).
///   Un objeto cuenta como agarrado mientras al menos uno de sus interactores lo este seleccionando.
/// - Es completamente inerte si <see cref="MRDepthOcclusionController.IsOcclusionActive"/> es false
///   (Editor, escenas VR): no toca ningun material ni ningun MaterialPropertyBlock.
///
/// Dos estrategias, aplicadas por renderer y por slot de material:
///  A) Materiales con la propiedad "_DepthOcclusion" (MR_DirectLightOnly, MR_HighlightOutline):
///     MaterialPropertyBlock con _DepthOcclusion = 0 mientras esta agarrado y el valor original al
///     soltarlo. Nunca se tocan los materiales compartidos.
///  B) Materiales sin "_DepthOcclusion" (p. ej. la linterna en URP Lit): se guardan pares
///     original -> variante con MR_LitOcclusion. Con oclusion activa y el objeto suelto se asignan
///     las variantes (renderer.sharedMaterials); agarrado o sin oclusion, los originales.
///
/// Los renderers se re-escanean en cada cambio de estado porque InteractableHighlight crea y destruye
/// en runtime las cascaras "__HighlightOutline".
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class DepthOcclusionOnGrab : MonoBehaviour
{
    /// <summary>Par material original -> mismo material con el shader de oclusion (MR_LitOcclusion).</summary>
    [Serializable]
    public struct MaterialPair
    {
        [Tooltip("Material tal y como esta en el prefab (sin oclusion).")]
        public Material original;

        [Tooltip("Variante del mismo material usando Custom/MR_LitOcclusion (siempre ocluye).")]
        public Material conOclusion;
    }

    [Header("Renderers afectados")]
    [Tooltip("Si se deja vacio se rellena solo con los hijos que tengan materiales compatibles.")]
    [SerializeField] private List<Renderer> renderers = new List<Renderer>();

    [Header("Pares de materiales (Estrategia B)")]
    [Tooltip("Materiales sin _DepthOcclusion: original -> variante con oclusion. Solo materiales opacos.")]
    [SerializeField] private List<MaterialPair> pares = new List<MaterialPair>();

    [Header("Tiempos")]
    [Tooltip("Margen (segundos) tras soltar antes de volver a activar la oclusion, para que la mano real no recorte el objeto.")]
    [SerializeField] private float releaseDelay = 0.25f;

    [Header("Ajustes")]
    [Tooltip("Marca esto para desactivar el comportamiento en este objeto concreto.")]
    [SerializeField] private bool desactivado = false;

    [Tooltip("Logs de cada cambio de estado (dejalo apagado en el visor).")]
    [SerializeField] private bool logsDetallados = false;

#if UNITY_EDITOR
    [Tooltip("SOLO EDITOR: simula la oclusion activa para poder probar sin Depth API. No afecta a builds.")]
    [SerializeField] private bool forzarOclusionEnEditor = false;
#endif

    private const string NombreCascaraContorno = "__HighlightOutline";
    private static readonly int DepthOcclusionId = Shader.PropertyToID("_DepthOcclusion");

    private sealed class RendererState
    {
        public Renderer renderer;
        public Material[] originales;
        public Material[] conOclusion;
        public float[] depthOriginal;
        public bool esCascara;
    }

    private readonly Dictionary<Renderer, RendererState> estados = new Dictionary<Renderer, RendererState>();
    private readonly List<GrabInteractable> agarrables = new List<GrabInteractable>();
    private readonly List<Renderer> encontrados = new List<Renderer>();
    private MaterialPropertyBlock propiedades;

    private Coroutine reactivacion;
    private bool agarrado;
    private bool agarradoSimulado;
    private bool inicializado;
    private bool suscritoControlador;

    // ------------------------------------------------------------------ ciclo de vida

    private void Awake()
    {
        propiedades = new MaterialPropertyBlock();
        RecogerAgarrables();
        AutoRellenarRenderers();
        ValidarPares();
        RefrescarRenderers();
        agarrado = EstaAgarrado() || agarradoSimulado;
        inicializado = true;
    }

    private void OnEnable()
    {
        RecogerAgarrables();

        if (!suscritoControlador)
        {
            MRDepthOcclusionController.OnOcclusionActiveChanged += AlCambiarOclusion;
            suscritoControlador = true;
        }

        CancelarReactivacion();
        agarrado = EstaAgarrado() || agarradoSimulado;
        RefrescarRenderers();
        AplicarEstado();
    }

    private void OnDisable()
    {
        if (suscritoControlador)
        {
            MRDepthOcclusionController.OnOcclusionActiveChanged -= AlCambiarOclusion;
            suscritoControlador = false;
        }

        SoltarAgarrables();
        CancelarReactivacion();
        RestaurarTodo();
    }

    private void OnDestroy()
    {
        if (suscritoControlador)
        {
            MRDepthOcclusionController.OnOcclusionActiveChanged -= AlCambiarOclusion;
            suscritoControlador = false;
        }

        SoltarAgarrables();
        CancelarReactivacion();
        RestaurarTodo();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && inicializado)
        {
            CancelarReactivacion();
            RefrescarRenderers();
            AplicarEstado();
        }
    }
#endif

    // ------------------------------------------------------------------ API de debug

    [ContextMenu("Debug/Simular agarrar")]
    public void DebugSimularAgarre()
    {
        agarradoSimulado = true;
        AlEmpezarAgarre();
        Debug.Log($"[DepthOcclusionOnGrab] '{name}': agarre simulado (agarrado={agarrado}, oclusion={OclusionActiva}).");
    }

    [ContextMenu("Debug/Simular soltar")]
    public void DebugSimularSoltar()
    {
        agarradoSimulado = false;
        AlTerminarAgarre();
        Debug.Log($"[DepthOcclusionOnGrab] '{name}': suelte simulado (agarrado={agarrado}, oclusion={OclusionActiva}).");
    }

    // ------------------------------------------------------------------ agarre

    private void RecogerAgarrables()
    {
        SoltarAgarrables();

        GrabInteractable[] encontradosAgarrables = GetComponentsInChildren<GrabInteractable>(true);
        for (int i = 0; i < encontradosAgarrables.Length; i++)
        {
            GrabInteractable agarrable = encontradosAgarrables[i];
            if (agarrable == null)
            {
                continue;
            }

            agarrables.Add(agarrable);
            agarrable.WhenStateChanged += AlCambiarEstadoAgarrable;
        }

        if (agarrables.Count == 0 && logsDetallados)
        {
            Debug.LogWarning($"[DepthOcclusionOnGrab] '{name}' no tiene ningun GrabInteractable; el objeto nunca contara como agarrado.");
        }
    }

    private void SoltarAgarrables()
    {
        for (int i = 0; i < agarrables.Count; i++)
        {
            if (agarrables[i] != null)
            {
                agarrables[i].WhenStateChanged -= AlCambiarEstadoAgarrable;
            }
        }

        agarrables.Clear();
    }

    private bool EstaAgarrado()
    {
        for (int i = 0; i < agarrables.Count; i++)
        {
            GrabInteractable agarrable = agarrables[i];
            if (agarrable == null)
            {
                continue;
            }

            if (agarrable.SelectingInteractors != null && agarrable.SelectingInteractors.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void AlCambiarEstadoAgarrable(InteractableStateChangeArgs args)
    {
        bool nuevoEstado = EstaAgarrado();

        if (nuevoEstado == agarrado)
        {
            return;
        }

        if (nuevoEstado)
        {
            AlEmpezarAgarre();
        }
        else
        {
            AlTerminarAgarre();
        }
    }

    private void AlEmpezarAgarre()
    {
        agarrado = true;
        CancelarReactivacion();

        if (!OclusionActiva)
        {
            return;
        }

        // Al agarrar, la oclusion se desactiva inmediatamente.
        RefrescarRenderers();
        AplicarEstado();
    }

    private void AlTerminarAgarre()
    {
        agarrado = false;

        if (!OclusionActiva)
        {
            return;
        }

        CancelarReactivacion();

        if (releaseDelay <= 0f)
        {
            RefrescarRenderers();
            AplicarEstado();
            return;
        }

        reactivacion = StartCoroutine(ReactivarTrasMargen());
    }

    private IEnumerator ReactivarTrasMargen()
    {
        yield return new WaitForSeconds(releaseDelay);
        reactivacion = null;

        if (!agarrado && OclusionActiva)
        {
            RefrescarRenderers();
            AplicarEstado();
        }
    }

    private void CancelarReactivacion()
    {
        if (reactivacion != null)
        {
            StopCoroutine(reactivacion);
            reactivacion = null;
        }
    }

    // ------------------------------------------------------------------ estado de oclusion

    private bool OclusionActiva
    {
        get
        {
#if UNITY_EDITOR
            if (forzarOclusionEnEditor)
            {
                return true;
            }
#endif
            return MRDepthOcclusionController.IsOcclusionActive;
        }
    }

    private void AlCambiarOclusion(bool activa)
    {
        if (!inicializado)
        {
            return;
        }

        CancelarReactivacion();
        RefrescarRenderers();
        AplicarEstado();

        if (logsDetallados)
        {
            Debug.Log($"[DepthOcclusionOnGrab] '{name}': oclusion global = {activa}.");
        }
    }

    private void AplicarEstado()
    {
        if (desactivado || !OclusionActiva)
        {
            RestaurarTodo();
            return;
        }

        bool conOclusion = !agarrado && !agarradoSimulado;

        foreach (KeyValuePair<Renderer, RendererState> entrada in estados)
        {
            RendererState estado = entrada.Value;
            if (estado == null || estado.renderer == null)
            {
                continue;
            }

            if (conOclusion)
            {
                AplicarConOclusion(estado);
            }
            else
            {
                AplicarAgarrado(estado);
            }
        }

        if (logsDetallados)
        {
            Debug.Log($"[DepthOcclusionOnGrab] '{name}': estado aplicado (con oclusion = {conOclusion}, renderers = {estados.Count}).");
        }
    }

    /// <summary>Objeto suelto y con oclusion disponible: variantes + _DepthOcclusion en su valor original.</summary>
    private void AplicarConOclusion(RendererState estado)
    {
        if (!MismoArray(estado.renderer.sharedMaterials, estado.conOclusion))
        {
            estado.renderer.sharedMaterials = estado.conOclusion;
        }

        for (int i = 0; i < estado.depthOriginal.Length; i++)
        {
            if (float.IsNaN(estado.depthOriginal[i]))
            {
                continue;
            }

            EscribirDepthOcclusion(estado.renderer, i, estado.depthOriginal[i]);
        }
    }

    /// <summary>Agarrado: materiales originales y _DepthOcclusion = 0 para que la mano real no recorte.</summary>
    private void AplicarAgarrado(RendererState estado)
    {
        if (!MismoArray(estado.renderer.sharedMaterials, estado.originales))
        {
            estado.renderer.sharedMaterials = estado.originales;
        }

        for (int i = 0; i < estado.depthOriginal.Length; i++)
        {
            if (float.IsNaN(estado.depthOriginal[i]))
            {
                continue;
            }

            EscribirDepthOcclusion(estado.renderer, i, 0f);
        }
    }

    /// <summary>Sin oclusion activa (o componente desactivado): materiales originales y sin valores nuestros.</summary>
    private void RestaurarRenderer(RendererState estado)
    {
        if (!MismoArray(estado.renderer.sharedMaterials, estado.originales))
        {
            estado.renderer.sharedMaterials = estado.originales;
        }

        for (int i = 0; i < estado.depthOriginal.Length; i++)
        {
            if (float.IsNaN(estado.depthOriginal[i]))
            {
                continue;
            }

            if (estado.esCascara)
            {
                // Las cascaras de contorno guardan color/grosor/pulso en su propio bloque: solo devolvemos
                // nuestro valor para no borrar los suyos.
                EscribirDepthOcclusion(estado.renderer, i, estado.depthOriginal[i]);
            }
            else
            {
                estado.renderer.SetPropertyBlock(null, i);
            }
        }
    }

    private void RestaurarTodo()
    {
        foreach (KeyValuePair<Renderer, RendererState> entrada in estados)
        {
            RendererState estado = entrada.Value;
            if (estado == null || estado.renderer == null)
            {
                continue;
            }

            RestaurarRenderer(estado);
        }
    }

    private void EscribirDepthOcclusion(Renderer renderer, int slot, float valor)
    {
        if (renderer == null)
        {
            return;
        }

        if (slot < 0 || slot >= renderer.sharedMaterials.Length)
        {
            return;
        }

        renderer.GetPropertyBlock(propiedades, slot);
        propiedades.SetFloat(DepthOcclusionId, valor);
        renderer.SetPropertyBlock(propiedades, slot);
    }

    // ------------------------------------------------------------------ renderers

    private void AutoRellenarRenderers()
    {
        if (renderers != null && renderers.Count > 0)
        {
            return;
        }

        if (renderers == null)
        {
            renderers = new List<Renderer>();
        }

        Renderer[] todos = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            Renderer candidato = todos[i];
            if (candidato == null || !EsRendererDeMalla(candidato))
            {
                continue;
            }

            if (TieneMaterialCompatible(candidato))
            {
                renderers.Add(candidato);
            }
        }
    }

    private static bool EsRendererDeMalla(Renderer renderer)
    {
        return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
    }

    private bool TieneMaterialCompatible(Renderer renderer)
    {
        Material[] materiales = renderer.sharedMaterials;
        for (int i = 0; i < materiales.Length; i++)
        {
            Material material = materiales[i];
            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(DepthOcclusionId) || BuscarPar(material) != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Re-escanea los hijos (incluidas las cascaras de contorno creadas en runtime).</summary>
    private void RefrescarRenderers()
    {
        // Limpia entradas cuyo renderer ya no existe (cascaras destruidas por InteractableHighlight).
        if (estados.Count > 0)
        {
            encontrados.Clear();
            encontrados.AddRange(estados.Keys);
            for (int i = 0; i < encontrados.Count; i++)
            {
                if (encontrados[i] == null)
                {
                    estados.Remove(encontrados[i]);
                }
            }
            encontrados.Clear();
        }

        Renderer[] todos = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            RegistrarRenderer(todos[i]);
        }

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                RegistrarRenderer(renderers[i]);
            }
        }
    }

    private void RegistrarRenderer(Renderer renderer)
    {
        if (renderer == null || estados.ContainsKey(renderer) || !EsRendererDeMalla(renderer))
        {
            return;
        }

        Material[] materiales = renderer.sharedMaterials;
        RendererState estado = new RendererState
        {
            renderer = renderer,
            originales = materiales,
            conOclusion = (Material[])materiales.Clone(),
            depthOriginal = new float[materiales.Length],
            esCascara = renderer.gameObject.name == NombreCascaraContorno
        };

        bool tieneAlgo = false;

        for (int i = 0; i < materiales.Length; i++)
        {
            Material material = materiales[i];
            estado.depthOriginal[i] = float.NaN;

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(DepthOcclusionId))
            {
                estado.depthOriginal[i] = material.GetFloat(DepthOcclusionId);
                tieneAlgo = true;
                continue;
            }

            MaterialPair? par = BuscarPar(material);
            if (par.HasValue && par.Value.conOclusion != null)
            {
                estado.conOclusion[i] = par.Value.conOclusion;
                tieneAlgo = true;
            }
        }

        if (tieneAlgo)
        {
            estados[renderer] = estado;
        }
    }

    private MaterialPair? BuscarPar(Material material)
    {
        if (material == null || pares == null)
        {
            return null;
        }

        for (int i = 0; i < pares.Count; i++)
        {
            if (pares[i].original == material)
            {
                return pares[i];
            }
        }

        return null;
    }

    private void ValidarPares()
    {
        if (pares == null)
        {
            pares = new List<MaterialPair>();
            return;
        }

        for (int i = 0; i < pares.Count; i++)
        {
            MaterialPair par = pares[i];
            if (par.original == null || par.conOclusion == null)
            {
                Debug.LogWarning($"[DepthOcclusionOnGrab] '{name}': el par {i} esta incompleto (original=" +
                                 $"{(par.original != null ? par.original.name : "NULL")}, conOclusion=" +
                                 $"{(par.conOclusion != null ? par.conOclusion.name : "NULL")}); se ignorara.");
                continue;
            }

            bool usado = UsaMaterial(par.original);
            if (!usado)
            {
                Debug.LogWarning($"[DepthOcclusionOnGrab] '{name}': ningun renderer de la lista usa el material '" +
                                 par.original.name + "'; el par se queda sin efecto.");
            }
        }
    }

    private bool UsaMaterial(Material material)
    {
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materiales = renderer.sharedMaterials;
                for (int j = 0; j < materiales.Length; j++)
                {
                    if (materiales[j] == material)
                    {
                        return true;
                    }
                }
            }
        }

        Renderer[] todos = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i] == null)
            {
                continue;
            }

            Material[] materiales = todos[i].sharedMaterials;
            for (int j = 0; j < materiales.Length; j++)
            {
                if (materiales[j] == material)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MismoArray(Material[] a, Material[] b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }
}
