using System;
using Meta.XR.MRUtilityKit;
using UnityEngine;

/// <summary>
/// Coloca a Springbonnie sentado en el borde de la cama real detectada por el visor.
///
/// La colocacion por defecto la decides tu en el editor: mueves el objeto Springbonnie_MR sobre el
/// proxy de la cama y ahi se queda. Este componente solo interviene en Play y solo cuando MRUK
/// termina de cargar la habitacion: entonces aparca la cadera en el borde del ancla BED mas cercana
/// al jugador y ajusta la altura del suelo de las piernas. Si no hay cama real, no toca nada.
///
/// Toda la geometria se calcula en el espacio LOCAL del ancla (reutilizando las utilidades de
/// MRSpawnerReworked), porque las anclas de MRUK no tienen por que estar alineadas con el mundo:
/// el ancla BED tipica lleva el forward apuntando hacia arriba.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class MRSpringBonniePlacement : MonoBehaviour
{
    [Header("Componentes")]
    [Tooltip("Componente que resuelve las piernas contra el suelo.")]
    public SpringBonnieStaticPose pose;

    [Tooltip("Hueso de la pelvis: la figura se coloca haciendo que este hueso caiga sobre el borde de la cama.")]
    public Transform cadera;

    [Tooltip("Camara que se usa para saber por que lado de la cama esta el jugador. Vacio = Camera.main.")]
    public Camera camara;

    [Header("Ancla de cama (MRUK)")]
    [Tooltip("Si esta activo, se busca una cama real detectada por el visor.")]
    public bool usarAnclaMRUK = true;

    [Tooltip("Etiqueta del ancla que se busca (MRUK SceneLabels). La cama es 'BED'.")]
    public string etiquetaCama = "BED";

    [Tooltip("Metros que la cadera se mete hacia dentro de la cama desde el borde (para no quedar colgando).")]
    [Range(0f, 0.5f)]
    public float separacionDelBorde = 0.10f;

    [Tooltip("Altura de la cadera por encima de la superficie de la cama (compensa el grosor del muslo).")]
    [Range(0f, 0.3f)]
    public float alturaCaderaSobreCama = 0.06f;

    [Tooltip("Espacio libre que se exige por delante de un lado para considerarlo despejado. Si todos los lados estan cerca de una pared se elige igualmente el que mira al jugador.")]
    [Range(0f, 2f)]
    public float margenPared = 0.60f;

    [Header("Proxy de cama")]
    [Tooltip("Cubo con medidas de cama para poder colocar la figura en el editor.")]
    public GameObject proxyCama;

    [Tooltip("Muestra el proxy aunque se haya encontrado una cama real (solo para depurar).")]
    public bool mostrarProxySiempre = false;

    /// <summary>Anclas que cuentan como obstaculo al elegir por que lado sale la figura.</summary>
    private static readonly MRUKAnchor.SceneLabels EtiquetasPared =
        MRUKAnchor.SceneLabels.WALL_FACE |
        MRUKAnchor.SceneLabels.INNER_WALL_FACE |
        MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE |
        MRUKAnchor.SceneLabels.STORAGE;

    /// <summary>Eje local de cada cara del ancla, indexado como en MRSpawnerReworked.</summary>
    private static readonly Vector3[] EjesLocales =
    {
        Vector3.forward,    // 0 cara +Z
        Vector3.back,       // 1 cara -Z
        Vector3.right,      // 2 cara +X
        Vector3.left,       // 3 cara -X
        Vector3.up,         // 4 cara +Y
        Vector3.down        // 5 cara -Y
    };

    private bool camaRealEncontrada;
    private bool suscrito;
    private bool colocando;

    private void Awake()
    {
        if (pose == null)
        {
            pose = GetComponent<SpringBonnieStaticPose>();
        }

        if (camara == null)
        {
            camara = Camera.main;
        }
    }

    private void OnEnable()
    {
        IntentarSuscribir();

        if (!camaRealEncontrada)
        {
            Colocar();
        }
    }

    private void Start()
    {
        // MRUK.Instance se crea en su propio Awake: en OnEnable puede no existir todavia.
        IntentarSuscribir();

        if (!camaRealEncontrada)
        {
            Colocar();
        }
    }

    private void OnDisable()
    {
        if (!suscrito)
        {
            return;
        }

        var mruk = MRUK.Instance;
        if (mruk != null)
        {
            mruk.SceneLoadedEvent.RemoveListener(AlCargarHabitacion);
            mruk.RoomCreatedEvent.RemoveListener(AlCrearHabitacion);
        }

        suscrito = false;
    }

    /// <summary>
    /// Se engancha a los eventos con los que MRUK avisa de que la habitacion ya esta cargada.
    /// SceneLoadedEvent es el aviso final (la habitacion ya se puede consultar); RoomCreatedEvent
    /// llega antes y, durante la carga, varias veces seguidas. Sin estos enganches, colocar en
    /// Awake llega siempre antes que la habitacion y la figura se queda donde estaba en el editor.
    /// </summary>
    private void IntentarSuscribir()
    {
        var mruk = MRUK.Instance;
        if (mruk == null)
        {
            return;
        }

        mruk.SceneLoadedEvent.RemoveListener(AlCargarHabitacion);
        mruk.SceneLoadedEvent.AddListener(AlCargarHabitacion);
        mruk.RoomCreatedEvent.RemoveListener(AlCrearHabitacion);
        mruk.RoomCreatedEvent.AddListener(AlCrearHabitacion);
        suscrito = true;

        // Si la habitacion ya estaba cargada (recarga de escena, objeto reactivado...), se coloca ya.
        if (mruk.IsInitialized && !camaRealEncontrada)
        {
            Colocar();
        }
    }

    /// <summary>MRUK avisa de que la escena esta lista: es el momento de consultar las anclas.</summary>
    private void AlCargarHabitacion()
    {
        Debug.Log("[MRSpringBonniePlacement] Escena MRUK cargada: se coloca la figura en la cama.", this);
        Colocar();
    }

    /// <summary>
    /// Habitacion nueva. Durante la carga MRUK avisa varias veces y aun con IsInitialized en false:
    /// esos avisos se ignoran (la colocacion buena llega con SceneLoadedEvent).
    /// </summary>
    private void AlCrearHabitacion(MRUKRoom habitacion)
    {
        if (MRUK.Instance == null || !MRUK.Instance.IsInitialized)
        {
            return;
        }

        Colocar();
    }

    /// <summary>
    /// En Play: si hay cama real, aparca la figura en su borde. Si no, respeta la colocacion que
    /// dejaste en el editor. Despues baja las piernas hasta el suelo.
    /// </summary>
    [ContextMenu("Colocar figura")]
    public void Colocar()
    {
        if (colocando)
        {
            return;
        }

        colocando = true;
        camaRealEncontrada = false;

        Vector3 puntoCadera;
        float yaw;
        float alturaSuelo;
        if (usarAnclaMRUK && Application.isPlaying && IntentarAnclaCama(out puntoCadera, out yaw, out alturaSuelo))
        {
            camaRealEncontrada = true;

            if (pose != null)
            {
                pose.alturaSuelo = alturaSuelo;
            }

            Aplicar(puntoCadera, yaw);
        }
        else
        {
            Debug.Log("[MRSpringBonniePlacement] Sin cama real: se respeta la colocacion del editor ("
                + transform.position.ToString("F3") + ").", this);
        }

        if (pose != null)
        {
            pose.PiernasAlSuelo();
        }

        if (proxyCama != null)
        {
            proxyCama.SetActive(!camaRealEncontrada || mostrarProxySiempre);
        }

        colocando = false;
    }

    /// <summary>Aplica giro y posicion para que el hueso de la cadera caiga sobre el punto pedido.</summary>
    private void Aplicar(Vector3 puntoCaderaDeseado, float yaw)
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cadera != null)
        {
            // Se desplaza toda la figura lo que le falte a la cadera para llegar al punto.
            transform.position += puntoCaderaDeseado - cadera.position;
        }
        else
        {
            transform.position = puntoCaderaDeseado;
        }
    }

    /// <summary>
    /// Busca la cama real mas cercana al jugador y devuelve el punto donde aparcar la cadera, el
    /// giro para que la figura mire hacia fuera de la cama y la altura del suelo de la habitacion.
    /// Todo se calcula en el espacio local del ancla, que es la unica forma de acertar cuando el
    /// ancla esta girada (en la cama el forward suele apuntar hacia arriba).
    /// </summary>
    private bool IntentarAnclaCama(out Vector3 puntoCadera, out float yaw, out float alturaSuelo)
    {
        puntoCadera = cadera != null ? cadera.position : transform.position;
        yaw = transform.eulerAngles.y;
        alturaSuelo = pose != null ? pose.alturaSuelo : 0f;

        if (!Application.isPlaying)
        {
            return false;
        }

        var mruk = MRUK.Instance;
        if (mruk == null)
        {
            Debug.Log("[MRSpringBonniePlacement] No hay MRUK en la escena: se mantiene la colocacion del editor.", this);
            return false;
        }

        // Ojo: SceneLoadedEvent llega justo antes de que MRUK ponga IsInitialized a true, asi que
        // aqui solo se exige que haya habitaciones cargadas, no el flag.
        bool hayHabitaciones = mruk.Rooms != null && mruk.Rooms.Count > 0;
        if (!mruk.IsInitialized && !hayHabitaciones)
        {
            Debug.Log("[MRSpringBonniePlacement] MRUK todavia no ha cargado ninguna habitacion: se mantiene la colocacion del editor.", this);
            return false;
        }

        Vector3 posicionJugador = camara != null ? camara.transform.position : transform.position;

        MRUKRoom habitacion = mruk.GetCurrentRoom();
        MRUKAnchor cama = habitacion != null ? BuscarCama(habitacion, posicionJugador) : null;

        // Si la habitacion actual no tiene cama (o no hay "habitacion actual": al cargar la escena
        // desde un prefab de pruebas, GetCurrentRoom() puede devolver null), se busca en el resto de
        // habitaciones cargadas por MRUK.
        if (cama == null && mruk.Rooms != null)
        {
            float mejorDistancia = float.MaxValue;
            for (int i = 0; i < mruk.Rooms.Count; i++)
            {
                var sala = mruk.Rooms[i];
                if (sala == null || sala == habitacion)
                {
                    continue;
                }

                var candidata = BuscarCama(sala, posicionJugador);
                if (candidata == null)
                {
                    continue;
                }

                float distancia = (candidata.transform.position - posicionJugador).sqrMagnitude;
                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    cama = candidata;
                    habitacion = sala;
                }
            }
        }

        if (cama == null)
        {
            Debug.Log("[MRSpringBonniePlacement] No hay ancla '" + etiquetaCama
                + "' en ninguna de las " + (mruk.Rooms != null ? mruk.Rooms.Count : 0)
                + " habitaciones cargadas: se mantiene la colocacion del editor.", this);
            return false;
        }

        Bounds local;
        if (!MRSpawnerReworked.TryGetLocalBounds(cama, out local))
        {
            Debug.LogWarning("[MRSpringBonniePlacement] El ancla BED no tiene volumen ni plano: no se puede colocar.", this);
            return false;
        }

        // 1) Cara de arriba del ancla, buscada en su espacio local (su "Y" no tiene por que ser el arriba del mundo).
        int caraArriba = MRSpawnerReworked.WorldDirectionToFaceIndex(cama, Vector3.up);
        Vector3 ejeArriba = EjesLocales[caraArriba];
        int indiceArriba = EjeDominante(ejeArriba);
        float signoArriba = Signo(ejeArriba[indiceArriba]);
        float techoLocal = local.center[indiceArriba] + signoArriba * local.extents[indiceArriba];

        // 2) Borde lateral: el lado que mira al jugador y con sitio libre por delante.
        Vector3 centroMundo = cama.transform.TransformPoint(local.center);
        Vector3 haciaJugador = posicionJugador - centroMundo;
        haciaJugador.y = 0f;

        int ejeLado;
        float signoLado;
        float libreDelante;
        ElegirLado(cama, local, indiceArriba, haciaJugador, out ejeLado, out signoLado, out libreDelante);

        // 3) La cadera se mete hacia dentro de la cama lo que pida separacionDelBorde, sin salirse de la huella.
        float mitadLado = Mathf.Max(0f, local.extents[ejeLado] - separacionDelBorde);

        var puntoLocal = local.center;
        puntoLocal[ejeLado] = local.center[ejeLado] + signoLado * mitadLado;
        puntoLocal[indiceArriba] = techoLocal + signoArriba * Mathf.Max(0f, alturaCaderaSobreCama);

        Vector3 puntoMundo = cama.transform.TransformPoint(puntoLocal);

        // 4) Giro: solo yaw, mirando hacia fuera de la cama por el lado elegido.
        var salidaLocal = Vector3.zero;
        salidaLocal[ejeLado] = signoLado;
        Vector3 salida = cama.transform.TransformDirection(salidaLocal);
        salida = Vector3.ProjectOnPlane(salida, Vector3.up);
        if (salida.sqrMagnitude < 1e-4f)
        {
            salida = haciaJugador;
        }

        if (salida.sqrMagnitude < 1e-4f)
        {
            salida = transform.forward;
        }

        salida.Normalize();

        puntoCadera = puntoMundo;
        yaw = Quaternion.LookRotation(salida, Vector3.up).eulerAngles.y;
        alturaSuelo = AlturaSueloReal(cama.Room != null ? cama.Room : habitacion, local, cama);

        Debug.Log("[MRSpringBonniePlacement] Cama encontrada en " + centroMundo.ToString("0.###")
            + " bounds locales min " + local.min.ToString("0.###") + " max " + local.max.ToString("0.###")
            + " cara arriba " + caraArriba
            + " -> cadera local " + puntoLocal.ToString("0.###") + " mundo " + puntoMundo.ToString("0.###")
            + " yaw " + yaw.ToString("0.#") + " lado " + ejeLado + (signoLado >= 0f ? "+" : "-")
            + " espacio libre delante " + libreDelante.ToString("0.###") + " m"
            + " suelo.y " + alturaSuelo.ToString("0.###"), this);

        return true;
    }

    /// <summary>Ancla con la etiqueta pedida mas cercana al jugador dentro de la habitacion.</summary>
    private MRUKAnchor BuscarCama(MRUKRoom habitacion, Vector3 posicionJugador)
    {
        MRUKAnchor mejor = null;
        float mejorDistancia = float.MaxValue;
        MRUKAnchor.SceneLabels etiqueta = EtiquetaDesdeTexto(etiquetaCama);

        var anclas = habitacion.Anchors;
        for (int i = 0; i < anclas.Count; i++)
        {
            var ancla = anclas[i];
            if (ancla == null || (ancla.Label & etiqueta) == 0)
            {
                continue;
            }

            Bounds local;
            if (!MRSpawnerReworked.TryGetLocalBounds(ancla, out local))
            {
                continue;
            }

            float distancia = (ancla.transform.TransformPoint(local.center) - posicionJugador).sqrMagnitude;
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = ancla;
            }
        }

        return mejor;
    }

    /// <summary>
    /// Elige, de las cuatro caras laterales del ancla, la que mira al jugador y tiene sitio libre
    /// por delante. Asi el cuerpo no se mete en una pared cuando la cama casi toca el muro.
    /// </summary>
    private void ElegirLado(MRUKAnchor cama, Bounds local, int indiceArriba, Vector3 haciaJugador,
        out int ejeElegido, out float signoElegido, out float libreElegido)
    {
        ejeElegido = (indiceArriba + 1) % 3;
        signoElegido = 1f;
        libreElegido = 0f;

        float mejorPuntuacion = float.MinValue;

        for (int eje = 0; eje < 3; eje++)
        {
            if (eje == indiceArriba)
            {
                continue;
            }

            for (int s = 0; s < 2; s++)
            {
                float signo = s == 0 ? 1f : -1f;

                var dirLocal = Vector3.zero;
                dirLocal[eje] = signo;

                Vector3 dirMundo = cama.transform.TransformDirection(dirLocal);
                dirMundo = Vector3.ProjectOnPlane(dirMundo, Vector3.up);
                if (dirMundo.sqrMagnitude < 1e-4f)
                {
                    continue;
                }

                dirMundo.Normalize();

                // Punto del borde de ese lado, a la altura del centro del ancla.
                var bordeLocal = local.center;
                bordeLocal[eje] = local.center[eje] + signo * local.extents[eje];
                Vector3 bordeMundo = cama.transform.TransformPoint(bordeLocal);

                float libre = EspacioLibreDelante(cama.Room, bordeMundo, dirMundo);
                float puntuacion = Vector3.Dot(haciaJugador, dirMundo);
                if (libre < margenPared)
                {
                    puntuacion -= 100f;
                }

                if (puntuacion > mejorPuntuacion)
                {
                    mejorPuntuacion = puntuacion;
                    ejeElegido = eje;
                    signoElegido = signo;
                    libreElegido = libre;
                }
            }
        }
    }

    /// <summary>
    /// Metros hasta la pared mas cercana que haya por delante del punto. Solo cuenta las anclas que
    /// esten en la direccion pedida: una pared detras no penaliza ese lado.
    /// </summary>
    private static float EspacioLibreDelante(MRUKRoom habitacion, Vector3 punto, Vector3 direccion)
    {
        float mejor = float.MaxValue;
        if (habitacion == null)
        {
            return mejor;
        }

        var anclas = habitacion.Anchors;
        for (int i = 0; i < anclas.Count; i++)
        {
            var ancla = anclas[i];
            if (ancla == null || (ancla.Label & EtiquetasPared) == 0)
            {
                continue;
            }

            Vector3 cercana;
            float distancia = ancla.GetClosestSurfacePosition(punto, out cercana);
            if (distancia < 0f)
            {
                distancia = 0f;
            }

            // Si la pared queda a la espalda (dot negativo), no limita este lado.
            if (Vector3.Dot(cercana - punto, direccion) <= 0f)
            {
                continue;
            }

            if (distancia < mejor)
            {
                mejor = distancia;
            }
        }

        return mejor;
    }

    /// <summary>
    /// Altura del suelo de la habitacion: el ancla FLOOR si existe y, si no, el fondo del ancla de la cama.
    /// </summary>
    private static float AlturaSueloReal(MRUKRoom habitacion, Bounds localCama, MRUKAnchor cama)
    {
        if (habitacion != null)
        {
            var anclas = habitacion.Anchors;
            for (int i = 0; i < anclas.Count; i++)
            {
                var ancla = anclas[i];
                if (ancla != null && (ancla.Label & MRUKAnchor.SceneLabels.FLOOR) != 0)
                {
                    return ancla.transform.position.y;
                }
            }
        }

        // Sin suelo: se usa la cara de abajo de la propia cama.
        if (cama != null)
        {
            int caraAbajo = MRSpawnerReworked.WorldDirectionToFaceIndex(cama, Vector3.down);
            Vector3 ejeAbajo = EjesLocales[caraAbajo];
            int indice = EjeDominante(ejeAbajo);
            float signo = Signo(ejeAbajo[indice]);
            var fondoLocal = localCama.center;
            fondoLocal[indice] = localCama.center[indice] + signo * localCama.extents[indice];
            return cama.transform.TransformPoint(fondoLocal).y;
        }

        return 0f;
    }

    /// <summary>Indice (0=X, 1=Y, 2=Z) del componente dominante de un eje local.</summary>
    private static int EjeDominante(Vector3 eje)
    {
        float ax = Mathf.Abs(eje.x);
        float ay = Mathf.Abs(eje.y);
        float az = Mathf.Abs(eje.z);

        if (ax >= ay && ax >= az)
        {
            return 0;
        }

        return ay >= az ? 1 : 2;
    }

    private static float Signo(float valor)
    {
        return valor >= 0f ? 1f : -1f;
    }

    /// <summary>
    /// Convierte el texto del Inspector ('BED') a la etiqueta de MRUK. Si no se reconoce
    /// se usa BED. Se hace una sola vez por busqueda, no por frame.
    /// </summary>
    private static MRUKAnchor.SceneLabels EtiquetaDesdeTexto(string texto)
    {
        MRUKAnchor.SceneLabels etiqueta;
        if (!string.IsNullOrEmpty(texto) && Enum.TryParse(texto, true, out etiqueta))
        {
            return etiqueta;
        }

        return MRUKAnchor.SceneLabels.BED;
    }
}
