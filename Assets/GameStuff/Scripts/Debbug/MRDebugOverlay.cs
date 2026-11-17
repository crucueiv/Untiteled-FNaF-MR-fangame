#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Text;
using Meta.XR.EnvironmentDepth;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Panel de diagnostico para Development Build: muestra el estado de la oclusion (keywords, bias,
/// mezcla) y los toggles de realismo, y deja cambiarlos con los mandos sin salir del visor.
///
/// Solo existe si <c>Debug.isDebugBuild</c> es true y la escena tiene un
/// <see cref="MRDepthOcclusionController"/> (escenas MR con passthrough). TestMenu y las escenas VR
/// no lo crean, asi que se ven exactamente igual que antes.
///
/// Combinacion para mostrarlo u ocultarlo: mantener pulsados los dos clics de stick (L3+R3) durante
/// <see cref="tiempoCombinacion"/> segundos. No choca con nada del juego: el boton de menu izquierdo
/// (Start) ya lo usa DebuggerHandToggle, A/X y B/Y los usa PlayerHandsVR para la pose del pulgar y los
/// disparos/gatillos el agarre.
///
/// Controles con el panel visible:
///   A (derecho)          cicla el modo de oclusion (Soft -> Hard -> None)
///   B (derecho)          cicla la mezcla del borde (Dither <-> Premultiplicado)
///   X (izquierdo)        dither edge on/off
///   Y (izquierdo)        sombras de contacto (MRContactShadow) on/off
///   L3                   vista de oclusion en escala de grises
///   R3                   endoesqueleto: como estaba / todos / solo ojos
///   Stick izq. arriba/abajo   bias de profundidad +/- 0.02 m
///   Stick der. izq./der.      elige parametro (Grano / Ambiente / Rebote / Sombra)
///   Stick der. arriba/abajo   ajusta el parametro elegido
///                             (en Sombra: tolerancia +/- 0.02 m; subir = conservar mas sombras
///                              sobre el passthrough y anularlas solo sobre el brazo real)
///
/// En el Editor (sin mandos) hay atajos de teclado: F8 mostrar/ocultar, F9 modo de oclusion,
/// F10 mezcla, flechas izquierda/derecha bias y arriba/abajo ajustan el parametro elegido.
///
/// Coste: un TextMesh de una linea de texto por cuadro y una actualizacion de posicion. Nada en el
/// camino de render de los shaders del juego; invisible y sin coste mientras esta oculto.
/// </summary>
[DisallowMultipleComponent]
public sealed class MRDebugOverlay : MonoBehaviour
{
    private const float UmbralStick = 0.55f;
    private const float PasoBias = 0.02f;
    private const float IntervaloRefresco = 0.15f;

    private static readonly string[] NombresParametro = { "Grano", "Ambiente", "Rebote", "Sombra" };

    private static MRDebugOverlay s_instancia;

    [Header("Presentacion")]
    [Tooltip("Distancia del panel a la camara (m).")]
    public float distancia = 0.55f;

    [Tooltip("Desplazamiento vertical respecto al centro de la vista (m).")]
    public float altura = -0.06f;

    [Tooltip("Tamano del texto en el mundo (m por em, aproximadamente).")]
    public float tamanoTexto = 0.0030f;

    [Header("Combinacion")]
    [Tooltip("Segundos que hay que mantener L3+R3 para mostrar u ocultar el panel.")]
    public float tiempoCombinacion = 0.9f;

    private MRDepthOcclusionController controlador;
    private MRContactShadow[] sombrasContacto;
    private TextMesh texto;
    private Material materialTexto;
    private Camera camara;

    private readonly StringBuilder sb = new StringBuilder(768);

    private bool visible;
    private float combinacion;
    private bool bloqueoSticks;
    private float siguienteRefresco;

    private int parametro;
    private int stickDerX;
    private int stickDerY;
    private int stickIzqY;

    private int estadoEndo;
    private Renderer[] endos;
    private bool[] endosInicial;

    // ------------------------------------------------------------------ arranque

    /// <summary>
    /// Coloca el panel en cada escena que tenga oclusion, sin tocar las escenas en el Editor.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }

        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        CrearSiHaceFalta();
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        s_instancia = null;
        CrearSiHaceFalta();
    }

    private static void CrearSiHaceFalta()
    {
        if (s_instancia != null)
        {
            return;
        }

        // Solo en escenas MR (passthrough con oclusion): asi TestMenu y las escenas VR no cambian.
        if (UnityEngine.Object.FindAnyObjectByType<MRDepthOcclusionController>() == null)
        {
            return;
        }

        var go = new GameObject("MRDebugOverlay");
        s_instancia = go.AddComponent<MRDebugOverlay>();
    }

    private void Awake()
    {
        s_instancia = this;

        camara = Camera.main;
        controlador = FindAnyObjectByType<MRDepthOcclusionController>();
        sombrasContacto = FindObjectsByType<MRContactShadow>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (!CrearTexto())
        {
            enabled = false;
            return;
        }

#if UNITY_EDITOR
        // En el Editor no hay mandos: se abre solo para poder ver el estado sin combinaciones.
        AplicarVisibilidad(true);
#else
        AplicarVisibilidad(false);
#endif
    }

    private void OnDestroy()
    {
        if (s_instancia == this)
        {
            s_instancia = null;
        }

        if (materialTexto != null)
        {
            Destroy(materialTexto);
            materialTexto = null;
        }
    }

    private bool CrearTexto()
    {
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null)
        {
            fuente = Font.CreateDynamicFontFromOSFont("Arial", 32);
        }

        if (fuente == null)
        {
            Debug.LogWarning("[MRDebugOverlay] No hay fuente disponible: el panel no se crea.");
            return false;
        }

        var go = new GameObject("Texto");
        go.transform.SetParent(transform, false);

        texto = go.AddComponent<TextMesh>();
        texto.font = fuente;
        texto.fontSize = 64;
        texto.characterSize = tamanoTexto;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.alignment = TextAlignment.Left;
        texto.richText = false;
        texto.color = new Color(0.92f, 1f, 0.95f, 1f);
        texto.text = "MR DEBUG";

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Copia propia del material de la fuente para poder subirlo por encima de la capa de
            // oscuridad (Custom/MR_UltimateDarknessRevealer, Transparent + 100 = 3100).
            materialTexto = new Material(fuente.material)
            {
                name = "MRDebugOverlay_Font",
                hideFlags = HideFlags.DontSave,
                renderQueue = 4000,
            };

            renderer.sharedMaterial = materialTexto;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.sortingOrder = 100;
        }

        return true;
    }

    // ------------------------------------------------------------------ cada cuadro

    private void Update()
    {
        if (LeerCombinacion())
        {
            return;
        }

        LeerTeclado();

        if (!visible)
        {
            return;
        }

        LeerMando();

        if (Time.unscaledTime >= siguienteRefresco)
        {
            siguienteRefresco = Time.unscaledTime + IntervaloRefresco;
            RefrescarTexto();
        }
    }

    private void LateUpdate()
    {
        if (!visible)
        {
            return;
        }

        if (camara == null)
        {
            camara = Camera.main;
        }

        if (camara == null)
        {
            return;
        }

        Transform vista = camara.transform;
        transform.SetPositionAndRotation(
            vista.position + vista.forward * distancia + vista.up * altura,
            vista.rotation);
    }

    /// <summary>Devuelve true si este cuadro se ha consumido en la combinacion de mostrado/ocultado.</summary>
    private bool LeerCombinacion()
    {
        bool izquierdo = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
        bool derecho = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);

        if (izquierdo && derecho)
        {
            bloqueoSticks = true;
            combinacion += Time.unscaledDeltaTime;

            if (combinacion >= tiempoCombinacion)
            {
                combinacion = 0f;
                AplicarVisibilidad(!visible);
                return true;
            }

            return false;
        }

        combinacion = 0f;

        if (!izquierdo && !derecho)
        {
            bloqueoSticks = false;
        }

        return false;
    }

    private void LeerMando()
    {
        if (controlador == null)
        {
            return;
        }

        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch)) CiclarModoOclusion();
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)) CiclarMezcla();
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch)) controlador.SetDitherEdge(!controlador.ditherEdge);
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)) AlternarSombrasContacto();

        if (!bloqueoSticks && OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch))
        {
            controlador.SetOcclusionGrayscale(!controlador.showOcclusionAsGrayscale);
        }

        if (!bloqueoSticks && OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
        {
            CiclarEndo();
        }

        int x = Snap(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x);
        if (x != 0 && x != stickDerX)
        {
            parametro = (parametro + x + NombresParametro.Length) % NombresParametro.Length;
        }
        stickDerX = x;

        int y = Snap(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y);
        if (y != 0 && y != stickDerY)
        {
            AjustarParametro(parametro, y);
        }
        stickDerY = y;

        int bias = Snap(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).y);
        if (bias != 0 && bias != stickIzqY)
        {
            controlador.SetDepthBias(controlador.depthBias + bias * PasoBias);
        }
        stickIzqY = bias;
    }

    /// <summary>Atajos de teclado para probar el panel en el Editor (sin mandos conectados).</summary>
    private void LeerTeclado()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null)
        {
            return;
        }

        if (teclado[Key.F8].wasPressedThisFrame)
        {
            AplicarVisibilidad(!visible);
            return;
        }

        if (!visible || controlador == null)
        {
            return;
        }

        if (teclado[Key.F9].wasPressedThisFrame) CiclarModoOclusion();
        if (teclado[Key.F10].wasPressedThisFrame) CiclarMezcla();

        if (teclado[Key.LeftArrow].wasPressedThisFrame) controlador.SetDepthBias(controlador.depthBias - PasoBias * 5f);
        if (teclado[Key.RightArrow].wasPressedThisFrame) controlador.SetDepthBias(controlador.depthBias + PasoBias * 5f);
        if (teclado[Key.UpArrow].wasPressedThisFrame) AjustarParametro(parametro, 1);
        if (teclado[Key.DownArrow].wasPressedThisFrame) AjustarParametro(parametro, -1);
    }

    // ------------------------------------------------------------------ acciones

    private void AplicarVisibilidad(bool activo)
    {
        visible = activo;

        if (texto != null)
        {
            texto.gameObject.SetActive(activo);
        }

        if (activo)
        {
            if (camara == null)
            {
                camara = Camera.main;
            }

            RefrescarAhora();
        }

        Debug.Log("[MRDebugOverlay] Panel " + (activo ? "visible" : "oculto") + ".");
    }

    private void RefrescarAhora()
    {
        siguienteRefresco = 0f;
        RefrescarTexto();
    }

    private void CiclarModoOclusion()
    {
        if (controlador == null)
        {
            return;
        }

        OcclusionShadersMode siguiente;
        switch (controlador.occlusionMode)
        {
            case OcclusionShadersMode.SoftOcclusion:
                siguiente = OcclusionShadersMode.HardOcclusion;
                break;
            case OcclusionShadersMode.HardOcclusion:
                siguiente = OcclusionShadersMode.None;
                break;
            default:
                siguiente = OcclusionShadersMode.SoftOcclusion;
                break;
        }

        controlador.SetOcclusionMode(siguiente);
    }

    private void CiclarMezcla()
    {
        if (controlador == null)
        {
            return;
        }

        controlador.SetBlendMode(controlador.blendMode == MROcclusionBlendMode.Dither
            ? MROcclusionBlendMode.Premultiplied
            : MROcclusionBlendMode.Dither);
    }

    private void AlternarSombrasContacto()
    {
        if (sombrasContacto == null || sombrasContacto.Length == 0)
        {
            return;
        }

        bool activar = !sombrasContacto[0].enabled;
        for (int i = 0; i < sombrasContacto.Length; i++)
        {
            if (sombrasContacto[i] != null)
            {
                sombrasContacto[i].enabled = activar;
            }
        }
    }

    private void AjustarParametro(int indice, int signo)
    {
        if (controlador == null)
        {
            return;
        }

        switch (indice)
        {
            case 0:
                controlador.SetGrainAmount(controlador.grainAmount + signo * 0.02f);
                break;

            case 1:
                if (signo > 0 && !controlador.warmAmbient)
                {
                    controlador.SetWarmAmbient(true);
                }
                else if (signo < 0 && controlador.ambientIntensity <= 0.001f)
                {
                    controlador.SetWarmAmbient(false);
                }
                else
                {
                    controlador.SetAmbientIntensity(controlador.ambientIntensity + signo * 0.02f);
                }
                break;

            case 2:
                controlador.SetBounceScale(controlador.bounceScale + signo * 0.05f);
                break;

            default:
                controlador.SetShadowOcclusionMargin(controlador.shadowOcclusionMargin + signo * PasoBias);
                break;
        }
    }

    /// <summary>Endoesqueleto: como estaba / todos / solo los de material de ojos.</summary>
    private void CiclarEndo()
    {
        if (endos == null)
        {
            Renderer[] todos = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var encontrados = new System.Collections.Generic.List<Renderer>();

            for (int i = 0; i < todos.Length; i++)
            {
                if (todos[i] != null && todos[i].name.StartsWith("Endo", System.StringComparison.OrdinalIgnoreCase))
                {
                    encontrados.Add(todos[i]);
                }
            }

            endos = encontrados.ToArray();
            endosInicial = new bool[endos.Length];

            for (int i = 0; i < endos.Length; i++)
            {
                endosInicial[i] = endos[i].enabled;
            }
        }

        if (endos.Length == 0)
        {
            return;
        }

        estadoEndo = (estadoEndo + 1) % 3;

        for (int i = 0; i < endos.Length; i++)
        {
            switch (estadoEndo)
            {
                case 0:
                    endos[i].enabled = endosInicial[i];
                    break;
                case 1:
                    endos[i].enabled = true;
                    break;
                default:
                    endos[i].enabled = TieneMaterialDeOjos(endos[i]);
                    break;
            }
        }
    }

    private static bool TieneMaterialDeOjos(Renderer renderer)
    {
        Material material = renderer.sharedMaterial;
        return material != null && material.name.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int Snap(float valor)
    {
        return valor > UmbralStick ? 1 : (valor < -UmbralStick ? -1 : 0);
    }

    // ------------------------------------------------------------------ texto

    private void RefrescarTexto()
    {
        if (texto == null)
        {
            return;
        }

        sb.Clear();
        sb.AppendLine("MR DEBUG   L3+R3 ocultar");

        sb.Append("Depth activa: ").Append(MRDepthOcclusionController.IsOcclusionActive ? "SI" : "NO");
        sb.Append("   SOFT=").Append(Shader.IsKeywordEnabled("SOFT_OCCLUSION") ? 1 : 0);
        sb.Append(" HARD=").Append(Shader.IsKeywordEnabled("HARD_OCCLUSION") ? 1 : 0);
        sb.AppendLine();

        if (controlador == null)
        {
            sb.AppendLine("Sin MRDepthOcclusionController en la escena.");
            texto.text = sb.ToString();
            return;
        }

        sb.Append("Modo: ").Append(controlador.occlusionMode);
        sb.Append("   Bias ").Append(controlador.depthBias.ToString("0.00")).Append(" m");
        sb.AppendLine();

        sb.Append("Mezcla: ").Append(controlador.blendMode);
        sb.Append("   Edge ").Append(controlador.ditherEdge ? "ON" : "OFF");
        sb.Append("   Umbral ").Append(controlador.ditherThreshold.ToString("0.00"));
        sb.AppendLine();

        sb.Append("Grano ").Append(controlador.grainAmount.ToString("0.00"));
        sb.Append("   Ambiente ").Append(controlador.warmAmbient ? "ON " : "OFF ");
        sb.Append(controlador.ambientIntensity.ToString("0.00"));
        sb.Append("   Rebote ").Append(controlador.bounceScale.ToString("0.00"));
        sb.AppendLine();

        sb.Append("Blobs ").Append(EstadoSombras());
        sb.Append("   Gris ").Append(controlador.showOcclusionAsGrayscale ? "ON" : "OFF");
        sb.Append("   Endo ").Append(EstadoEndo());
        sb.Append("   Sombra ").Append(controlador.shadowOcclusionMargin.ToString("0.00"))
          .Append("+").Append((controlador.shadowOcclusionRelative * 100f).ToString("0.#")).Append("%");
        sb.AppendLine();

        sb.Append("A modo  B mezcla  X edge  Y blobs  L3 gris  R3 endo");
        sb.AppendLine();

        sb.Append("Stick izq Y bias  |  Stick der X: ").Append(NombresParametro[parametro]);
        sb.Append("  Stick der Y: valor");

        texto.text = sb.ToString();
    }

    private string EstadoSombras()
    {
        if (sombrasContacto == null || sombrasContacto.Length == 0)
        {
            return "n/a";
        }

        return sombrasContacto[0] != null && sombrasContacto[0].enabled ? "ON" : "OFF";
    }

    private string EstadoEndo()
    {
        switch (estadoEndo)
        {
            case 1:
                return "todos";
            case 2:
                return "solo ojos";
            default:
                return "como estaba";
        }
    }
}
#endif
