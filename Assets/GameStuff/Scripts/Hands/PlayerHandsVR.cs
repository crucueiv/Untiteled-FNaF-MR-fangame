using UnityEngine;

/// <summary>
/// Sustituye los visuales del mando por las manos de FNaF Help Wanted.
///
/// Las manos son prefabs normales instanciados como hijos de las anclas del mando
/// (RightControllerAnchor / LeftControllerAnchor), asi que siguen la pose del mando sin
/// hacer ninguna copia manual de transform cada frame.
///
/// Interruptor:
///   usarManosFNaF = false (por defecto) -> no hace absolutamente nada: los visuales del
///   mando y el sistema actual se quedan como estan.
///   usarManosFNaF = true  -> aparecen las manos, se ocultan los visuales del mando y se
///   leen grip / gatillo / pulgar con OVRInput para plegar los dedos.
///
/// No reserva memoria por frame: los arrays de renderers se cachean en Awake.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class PlayerHandsVR : MonoBehaviour
{
    [Header("Interruptor principal")]
    [Tooltip("OFF = sistema actual intacto. ON = manos FNaF visibles y visuales del mando ocultos.")]
    public bool usarManosFNaF = false;

    [Tooltip("Permite cambiar el interruptor en tiempo de ejecucion (util para probar en el visor).")]
    public bool permitirCambioEnEjecucion = true;

    [Header("Manos (prefabs instanciados bajo las anclas del mando)")]
    [Tooltip("Raiz de la mano derecha. Su hijo \"Model\" contiene la malla.")]
    public Transform raizManoDerecha;

    [Tooltip("Raiz de la mano izquierda (misma malla espejada con escala X = -1).")]
    public Transform raizManoIzquierda;

    [Tooltip("Componente que pliega los dedos de la mano derecha.")]
    public FnafHandPose poseDerecha;

    [Tooltip("Componente que pliega los dedos de la mano izquierda.")]
    public FnafHandPose poseIzquierda;

    [Header("Desplazamiento respecto al ancla del mando")]
    [Tooltip("Posicion local (metros). El ancla del mando cae en el centro del mango: la muneca va detras.")]
    public Vector3 offsetPosicionDerecha = new Vector3(0f, 0f, -0.09f);

    [Tooltip("Rotacion local (grados) para ajustar la inclinacion de la mano derecha.")]
    public Vector3 offsetRotacionDerecha = Vector3.zero;

    [Tooltip("Posicion local (metros) de la mano izquierda.")]
    public Vector3 offsetPosicionIzquierda = new Vector3(0f, 0f, -0.09f);

    [Tooltip("Rotacion local (grados) de la mano izquierda.")]
    public Vector3 offsetRotacionIzquierda = Vector3.zero;

    [Header("Visuales del mando que se ocultan con las manos activas")]
    [Tooltip("Objeto [BuildingBlock] Controller Tracking del mando derecho. Se ocultan sus renderers, no el objeto (conserva colliders y tracking).")]
    public GameObject visualMandoDerecho;

    [Tooltip("Objeto [BuildingBlock] Controller Tracking del mando izquierdo.")]
    public GameObject visualMandoIzquierdo;

    [Header("Dedos")]
    [Tooltip("Leer OVRInput. Desactivado: la mano se queda abierta (para probar en el editor).")]
    public bool leerMandos = true;

    private Renderer[] renderersDerecho;
    private Renderer[] renderersIzquierdo;
    private bool[] renderersDerechoOriginal;
    private bool[] renderersIzquierdoOriginal;

    // Estado ya aplicado, para no repetir SetActive / enabled cada frame.
    private bool estadoActivo;

    // ---------------------------------------------------------------- API publica

    /// <summary>Enciende o apaga las manos FNaF en tiempo de ejecucion.</summary>
    public void SetManosFNaF(bool activo)
    {
        if (!permitirCambioEnEjecucion && Time.timeSinceLevelLoad > 0f)
        {
            return;
        }

        usarManosFNaF = activo;
        AplicarEstado(activo);
    }

    /// <summary>Indica si las manos FNaF estan sustituyendo al mando.</summary>
    public bool ManosActivas
    {
        get { return usarManosFNaF; }
    }

    // ---------------------------------------------------------------- Ciclo de vida

    private void Awake()
    {
        Cachear();
        estadoActivo = !usarManosFNaF; // fuerza a aplicar el estado real en el primer Update
        AplicarEstado(usarManosFNaF);
    }

    private void OnEnable()
    {
        Cachear();
        AplicarEstado(usarManosFNaF);
    }

    private void Update()
    {
        // Solo se toca el estado cuando cambia el interruptor.
        if (usarManosFNaF != estadoActivo)
        {
            AplicarEstado(usarManosFNaF);
        }

        if (!usarManosFNaF || !leerMandos)
        {
            return;
        }

        LeerMando(OVRInput.Controller.RTouch, poseDerecha);
        LeerMando(OVRInput.Controller.LTouch, poseIzquierda);
    }

    private void LateUpdate()
    {
        // Se aplica despues de que el rig haya movido las anclas.
        if (usarManosFNaF)
        {
            AplicarOffsets();
        }
    }

    // ---------------------------------------------------------------- Interno

    private void Cachear()
    {
        CachearRenderers(visualMandoDerecho, ref renderersDerecho, ref renderersDerechoOriginal);
        CachearRenderers(visualMandoIzquierdo, ref renderersIzquierdo, ref renderersIzquierdoOriginal);
    }

    private static void CachearRenderers(GameObject objetivo, ref Renderer[] cache, ref bool[] originales)
    {
        if (objetivo == null || cache != null)
        {
            return;
        }

        cache = objetivo.GetComponentsInChildren<Renderer>(true);
        originales = new bool[cache.Length];

        for (int i = 0; i < cache.Length; i++)
        {
            originales[i] = cache[i].enabled;
        }
    }

    private void AplicarEstado(bool activo)
    {
        estadoActivo = activo;

        if (raizManoDerecha != null)
        {
            raizManoDerecha.gameObject.SetActive(activo);
        }

        if (raizManoIzquierda != null)
        {
            raizManoIzquierda.gameObject.SetActive(activo);
        }

        // Se ocultan los renderers del mando, no el objeto: asi el tracking y los
        // colliders del mando siguen funcionando igual.
        RestaurarRenderers(renderersDerecho, renderersDerechoOriginal, !activo);
        RestaurarRenderers(renderersIzquierdo, renderersIzquierdoOriginal, !activo);

        if (activo)
        {
            AplicarOffsets();
        }
        else
        {
            if (poseDerecha != null)
            {
                poseDerecha.ReiniciarPose();
            }

            if (poseIzquierda != null)
            {
                poseIzquierda.ReiniciarPose();
            }
        }
    }

    private static void RestaurarRenderers(Renderer[] renderers, bool[] originales, bool mostrarMando)
    {
        if (renderers == null || originales == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].enabled = mostrarMando && originales[i];
        }
    }

    private void AplicarOffsets()
    {
        AplicarOffset(raizManoDerecha, offsetPosicionDerecha, offsetRotacionDerecha);
        AplicarOffset(raizManoIzquierda, offsetPosicionIzquierda, offsetRotacionIzquierda);
    }

    private static void AplicarOffset(Transform mano, Vector3 posicion, Vector3 rotacion)
    {
        if (mano == null)
        {
            return;
        }

        if (mano.localPosition != posicion)
        {
            mano.localPosition = posicion;
        }

        Quaternion objetivo = Quaternion.Euler(rotacion);
        if (mano.localRotation != objetivo)
        {
            mano.localRotation = objetivo;
        }
    }

    /// <summary>
    /// Traduce el mando a la pose de la mano: grip = puno, gatillo = indice,
    /// tocar un boton o el stick con el pulgar = pulgar recogido.
    /// </summary>
    private static void LeerMando(OVRInput.Controller controlador, FnafHandPose pose)
    {
        if (pose == null)
        {
            return;
        }

        float puno = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controlador);
        float gatillo = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controlador);

        bool pulgarTocando = OVRInput.Get(OVRInput.Touch.One, controlador)
                          || OVRInput.Get(OVRInput.Touch.Two, controlador)
                          || OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, controlador)
                          || OVRInput.Get(OVRInput.Touch.PrimaryThumbRest, controlador)
                          || OVRInput.Get(OVRInput.Button.One, controlador)
                          || OVRInput.Get(OVRInput.Button.Two, controlador);

        pose.SetPuno(puno);
        pose.SetGatillo(gatillo);
        pose.SetPulgar(pulgarTocando ? 1f : 0f);
    }
}
