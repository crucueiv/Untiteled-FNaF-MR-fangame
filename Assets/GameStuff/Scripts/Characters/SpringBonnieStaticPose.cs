using UnityEngine;

/// <summary>
/// Springbonnie sentado: la pose (torso, brazos, cabeza, separacion de las piernas) se posa a mano
/// en el editor y vive en los huesos del prefab/escena. Este componente solo se encarga de que las
/// piernas lleguen al suelo:
///
///   1. Coge el tobillo tal como esta posado a mano y le conserva la X y la Z.
///   2. Le fija la altura para que la planta quede sobre el suelo (tobillo = suelo + desfase).
///   3. Re-resuelve cadera y rodilla con IK de dos huesos y deja el pie plano.
///
/// Se ejecuta al activarse y cada vez que cambia el transform de la figura (tambien en el editor),
/// asi que moviendo el modelo con la herramienta Move las piernas se recolocan solas.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SpringBonnieStaticPose : MonoBehaviour
{
    [Header("Pies en el suelo")]
    [Tooltip("Altura (Y de mundo) del suelo donde tienen que apoyar las plantas. 0 = suelo de la habitacion.")]
    public float alturaSuelo = 0f;

    [Tooltip("Metros del tobillo a la planta del pie. Medido en este rig a escala 0.469: 0.201.")]
    public float desfaseTobilloPlanta = 0.201f;

    [Tooltip("Hacia donde apunta la rodilla al resolver el IK: al frente y algo hacia fuera. (1, 0.7, 0.15) abre las rodillas en pose de rana.")]
    public Vector3 poloRodilla = new Vector3(0.30f, 0.55f, 1.00f);

    [Header("Nombres de huesos del modelo")]
    [Tooltip("Nombre exacto del hueso de la pelvis.")]
    public string huesoPelvis = "bip_pelvis";

    [Tooltip("Sufijo de los huesos del lado derecho del personaje.")]
    public string sufijoDerecha = "_r";

    [Tooltip("Sufijo de los huesos del lado izquierdo del personaje.")]
    public string sufijoIzquierda = "_l";

    public string baseCadera = "bip_hip";
    public string baseRodilla = "bip_knee";
    public string baseTobillo = "bip_foot";

    [Tooltip("Sufijo del hueso terminal del pie (la punta). En este modelo es '_end'.")]
    public string sufijoPuntaPie = "_end";

    [Header("Opciones")]
    [Tooltip("Resuelve las piernas tambien en el editor, para ver el resultado sin entrar en Play.")]
    public bool aplicarEnEditor = true;

    [Tooltip("Lo activa la ventana de pose mientras estas editando, para que no se pisen tus retoques.")]
    public bool pausarAutoAplicar = false;

    [Tooltip("Imprime en consola los huesos que no ha encontrado.")]
    public bool avisarHuesosPerdidos = true;

    [Header("Huesos resueltos (automatico)")]
    public Transform pelvis;

    public Transform caderaDer;
    public Transform rodillaDer;
    public Transform tobilloDer;
    public Transform puntaDer;

    public Transform caderaIzq;
    public Transform rodillaIzq;
    public Transform tobilloIzq;
    public Transform puntaIzq;

    private bool resuelto;
    private bool cacheValida;
    private Vector3 ultimaPosicion;
    private Quaternion ultimaRotacion;
    private Vector3 ultimaEscala;

    private void OnEnable()
    {
        if (!aplicarEnEditor && !Application.isPlaying)
        {
            return;
        }

        if (pausarAutoAplicar)
        {
            return;
        }

        PiernasAlSuelo();
    }

    private void Update()
    {
        // Solo se recalcula cuando la figura se mueve, gira o escala: quieta no cuesta nada.
        if (!aplicarEnEditor && !Application.isPlaying)
        {
            return;
        }

        if (pausarAutoAplicar)
        {
            return;
        }

        if (cacheValida
            && transform.position == ultimaPosicion
            && transform.rotation == ultimaRotacion
            && transform.lossyScale == ultimaEscala)
        {
            return;
        }

        PiernasAlSuelo();
    }

    /// <summary>
    /// Baja (o sube) las piernas hasta que las plantas tocan el suelo, conservando la separacion y
    /// el alcance hacia delante que tenga la pose. No toca ningun otro hueso de la figura.
    /// </summary>
    [ContextMenu("Piernas al suelo")]
    public void PiernasAlSuelo()
    {
        if (!ResolverHuesos())
        {
            return;
        }

        Cachear();

        float objetivoY = alturaSuelo + desfaseTobilloPlanta;

        ResolverPierna(caderaDer, rodillaDer, tobilloDer, puntaDer, true, objetivoY);
        ResolverPierna(caderaIzq, rodillaIzq, tobilloIzq, puntaIzq, false, objetivoY);

        Debug.Log(string.Format(
            "[SpringBonnieStaticPose] piernas al suelo {0}: suelo.y={1:F3} tobillo.y={2:F3} "
            + "tobilloDer={3} tobilloIzq={4} pelota.y={5:F3} pelvis.y={6:F3}",
            name, alturaSuelo, objetivoY,
            tobilloDer != null ? tobilloDer.position.ToString("F3") : "null",
            tobilloIzq != null ? tobilloIzq.position.ToString("F3") : "null",
            puntaDer != null ? puntaDer.position.y : float.NaN,
            pelvis != null ? pelvis.position.y : float.NaN), this);
    }

    // ------------------------------------------------------------------ piernas

    /// <summary>Resuelve una pierna: IK de cadera-rodilla para que el tobillo quede a la altura pedida.</summary>
    private void ResolverPierna(Transform cadera, Transform rodilla, Transform tobillo, Transform punta,
        bool derecha, float objetivoY)
    {
        if (cadera == null || rodilla == null || tobillo == null)
        {
            return;
        }

        Vector3 actual = tobillo.position;

        // La X y la Z son las que has posado a mano: aqui solo se corrige la altura.
        var objetivo = new Vector3(actual.x, objetivoY, actual.z);

        Vector3 polo = transform.TransformDirection(
            derecha ? poloRodilla : new Vector3(-poloRodilla.x, poloRodilla.y, poloRodilla.z));

        ResolverDosHuesos(cadera, rodilla, tobillo, objetivo, polo);
        PlanearPie(tobillo, punta);
    }

    /// <summary>
    /// IK analitico de dos huesos: coloca la rodilla para que el tobillo llegue al objetivo.
    /// Si el objetivo esta fuera de alcance se estira la pierna todo lo posible.
    /// </summary>
    private void ResolverDosHuesos(Transform cadera, Transform rodilla, Transform tobillo,
        Vector3 objetivoMundo, Vector3 direccionRodillaMundo)
    {
        if (cadera == null || rodilla == null || tobillo == null)
        {
            return;
        }

        Vector3 origen = cadera.position;
        float l1 = Vector3.Distance(origen, rodilla.position);
        float l2 = Vector3.Distance(rodilla.position, tobillo.position);
        if (l1 < 1e-4f || l2 < 1e-4f)
        {
            return;
        }

        Vector3 hacia = objetivoMundo - origen;
        float distancia = hacia.magnitude;
        if (distancia < 1e-4f)
        {
            return;
        }

        Vector3 eje = hacia / distancia;
        distancia = Mathf.Clamp(distancia, Mathf.Abs(l1 - l2) + 1e-3f, l1 + l2 - 1e-3f);

        // Proyeccion de la rodilla sobre el eje cadera-tobillo y altura del triangulo.
        float a = (l1 * l1 - l2 * l2 + distancia * distancia) / (2f * distancia);
        float h = Mathf.Sqrt(Mathf.Max(l1 * l1 - a * a, 0f));

        Vector3 perpendicular = direccionRodillaMundo - Vector3.Dot(direccionRodillaMundo, eje) * eje;
        if (perpendicular.sqrMagnitude < 1e-8f)
        {
            perpendicular = Vector3.Cross(eje, transform.forward);
            if (perpendicular.sqrMagnitude < 1e-8f)
            {
                perpendicular = Vector3.Cross(eje, Vector3.up);
            }
        }

        perpendicular.Normalize();

        Vector3 posicionRodilla = origen + eje * a + perpendicular * h;

        ApuntarMundo(cadera, rodilla, posicionRodilla - origen);
        ApuntarMundo(rodilla, tobillo, objetivoMundo - rodilla.position);
    }

    /// <summary>
    /// Deja el pie plano: gira el tobillo lo justo para que el hueso de la punta quede a su misma
    /// altura, conservando la direccion horizontal que tenga la pose.
    /// </summary>
    private static void PlanearPie(Transform tobillo, Transform punta)
    {
        if (tobillo == null || punta == null)
        {
            return;
        }

        Vector3 dirPie = punta.position - tobillo.position;
        if (dirPie.sqrMagnitude < 1e-10f)
        {
            return;
        }

        float longitud = dirPie.magnitude;
        var horizontal = new Vector3(dirPie.x, 0f, dirPie.z);
        if (horizontal.sqrMagnitude < 1e-8f)
        {
            return;
        }

        Vector3 objetivo = horizontal.normalized * longitud;
        tobillo.rotation = Quaternion.FromToRotation(dirPie.normalized, objetivo.normalized) * tobillo.rotation;
    }

    /// <summary>Gira el hueso para que el vector hueso-&gt;hijo apunte en la direccion de mundo pedida.</summary>
    private static void ApuntarMundo(Transform hueso, Transform hijo, Vector3 direccionMundo)
    {
        if (hueso == null || hijo == null || direccionMundo.sqrMagnitude < 1e-10f)
        {
            return;
        }

        Vector3 actual = hijo.position - hueso.position;
        if (actual.sqrMagnitude < 1e-10f)
        {
            return;
        }

        hueso.rotation = Quaternion.FromToRotation(actual.normalized, direccionMundo.normalized) * hueso.rotation;
    }

    // ------------------------------------------------------------------ utilidades

    private void Cachear()
    {
        ultimaPosicion = transform.position;
        ultimaRotacion = transform.rotation;
        ultimaEscala = transform.lossyScale;
        cacheValida = true;
    }

    /// <summary>Saca por consola donde quedan las plantas y la cadera, para ajustar el desfase con datos.</summary>
    [ContextMenu("Medir plantas y estatura")]
    public void MedirPlanta()
    {
        if (!ResolverHuesos())
        {
            return;
        }

        float tobilloDerY = tobilloDer != null ? tobilloDer.position.y : float.NaN;
        float tobilloIzqY = tobilloIzq != null ? tobilloIzq.position.y : float.NaN;
        float pelvisY = pelvis != null ? pelvis.position.y : float.NaN;

        // La planta real se mide con BakeMesh: Renderer.bounds viene inflado por la pose de bind.
        float planta = float.NaN;
        var mallas = GetComponentsInChildren<SkinnedMeshRenderer>(false);
        var temporal = new Mesh();
        for (int i = 0; i < mallas.Length; i++)
        {
            if (mallas[i] == null || mallas[i].sharedMesh == null)
            {
                continue;
            }

            mallas[i].BakeMesh(temporal);
            var vertices = temporal.vertices;
            for (int v = 0; v < vertices.Length; v++)
            {
                float y = mallas[i].transform.TransformPoint(vertices[v]).y;
                if (float.IsNaN(planta) || y < planta)
                {
                    planta = y;
                }
            }
        }

        DestroyImmediate(temporal);

        Debug.Log(string.Format(
            "[SpringBonnieStaticPose] pelota/pelvis: pelvis.y={0:F3} tobilloDer.y={1:F3} tobilloIzq.y={2:F3} "
            + "plantaReal={3:F3} suelo={4:F3} desfaseActual={5:F3} desfaseMedido={6:F3}",
            pelvisY, tobilloDerY, tobilloIzqY, planta, alturaSuelo, desfaseTobilloPlanta,
            float.IsNaN(planta) ? float.NaN : Mathf.Min(tobilloDerY, tobilloIzqY) - planta), this);
    }

    /// <summary>Busca los huesos por nombre una sola vez y los cachea en los campos publicos.</summary>
    public bool ResolverHuesos()
    {
        if (resuelto)
        {
            return true;
        }

        pelvis = Buscar(huesoPelvis);

        caderaDer = Buscar(baseCadera + sufijoDerecha);
        rodillaDer = Buscar(baseRodilla + sufijoDerecha);
        tobilloDer = Buscar(baseTobillo + sufijoDerecha);
        puntaDer = Buscar(baseTobillo + sufijoDerecha + sufijoPuntaPie);

        caderaIzq = Buscar(baseCadera + sufijoIzquierda);
        rodillaIzq = Buscar(baseRodilla + sufijoIzquierda);
        tobilloIzq = Buscar(baseTobillo + sufijoIzquierda);
        puntaIzq = Buscar(baseTobillo + sufijoIzquierda + sufijoPuntaPie);

        // El esqueleto solo es util si las dos cadenas de pierna existen.
        resuelto = caderaDer != null && rodillaDer != null && tobilloDer != null
            && caderaIzq != null && rodillaIzq != null && tobilloIzq != null;

        if (!resuelto && avisarHuesosPerdidos && (Application.isPlaying || !Application.isEditor))
        {
            Debug.LogWarning("[SpringBonnieStaticPose] No se encontraron los huesos de las piernas en " + name
                + ". Revisa los nombres en el Inspector.", this);
        }

        return resuelto;
    }

    private Transform Buscar(string nombre)
    {
        if (string.IsNullOrEmpty(nombre))
        {
            return null;
        }

        var todos = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre)
            {
                return todos[i];
            }
        }

        return null;
    }
}
