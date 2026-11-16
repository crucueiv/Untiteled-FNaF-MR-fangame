using UnityEngine;

/// <summary>
/// Posa los dedos de una mano FNaF (rig de 3 falanges por dedo) a partir de tres valores
/// normalizados: puno (grip), indice (gatillo) y pulgar.
///
/// El eje de flexion de cada dedo NO esta escrito a mano: se calcula una sola vez en Awake
/// con el producto vectorial entre la direccion del dedo y el eje que cruza la mano
/// (indice -> menique). Asi funciona igual con la mano derecha y con la copia espejada.
/// Si aun asi un dedo doblase hacia el dorso, se marca "invertirFlexion" en el Inspector.
///
/// Los angulos maximos de flexion son campos serializados: se ajustan sin tocar codigo.
/// </summary>
[DisallowMultipleComponent]
public sealed class FnafHandPose : MonoBehaviour
{
    public enum LadoMano
    {
        Derecha,
        Izquierda
    }

    [Header("Rig de la mano")]
    [Tooltip("Mano a la que pertenece este rig. Solo informativo: el espejado se hace con la escala del prefab.")]
    public LadoMano lado = LadoMano.Derecha;

    [Tooltip("Sufijo completo con el que acaba el nombre del hueso. Los nombres reales del FBX son index_01_r_jnt, thumb_02_r_jnt...")]
    public string sufijoHuesos = "_r_jnt";

    [Header("Angulos maximos por falange (grados)")]
    [Tooltip("Flexion de la falange proximal de indice, corazon, anular y menique.")]
    public float flexionProximal = 34f;

    [Tooltip("Flexion de la falange media de indice, corazon, anular y menique.")]
    public float flexionMedia = 62f;

    [Tooltip("Flexion de la falange distal de indice, corazon, anular y menique.")]
    public float flexionDistal = 44f;

    [Header("Pulgar")]
    public float pulgarProximal = 22f;
    public float pulgarMedia = 34f;
    public float pulgarDistal = 26f;

    [Tooltip("Cuanto cierra el indice con el puno (0 = solo lo mueve el gatillo, 1 = cierra como los demas).")]
    [Range(0f, 1f)]
    public float pesoGripEnIndice = 0.35f;

    [Tooltip("Abduccion de los dedos al abrir la mano (grados).")]
    public float aperturaDedos = 6f;

    [Header("Correccion de ejes")]
    [Tooltip("Invierte el sentido de la flexion si los dedos doblan hacia el dorso en vez de hacia la palma.")]
    public bool invertirFlexion = false;

    [Tooltip("Invierte el pulgar respecto al resto de dedos.")]
    public bool invertirPulgar = false;

    [Header("Suavizado")]
    [Tooltip("Velocidad con la que los dedos alcanzan la pose objetivo (normalizado por segundo).")]
    public float velocidadDedos = 12f;

    private const int Falanges = 3;

    private struct Dedo
    {
        public Transform[] huesos;
        public Quaternion[] reposo;
        public Vector3 ejeFlexion;
        public Vector3 ejeAbduccion;
        public float escalaFlexion;
    }

    private readonly Dedo[] dedos = new Dedo[5];
    private bool listo;

    private float punoActual;
    private float indiceActual;
    private float pulgarActual;

    private float punoObjetivo;
    private float indiceObjetivo;
    private float pulgarObjetivo;

    // Nombres de los huesos en el FBX (sin el sufijo comun).
    private static readonly string[] NombresDedos = { "index", "middle", "ring", "pinky", "thumb" };

    private void Awake()
    {
        ConstruirDedos();
    }

    // ---------------------------------------------------------------- API publica

    /// <summary>
    /// Fuerza la construccion de la tabla de huesos. Awake ya lo hace en ejecucion;
    /// este metodo existe para poder posar la mano desde un script de editor
    /// (verificacion sin visor), donde Awake no se ejecuta.
    /// </summary>
    public void Inicializar()
    {
        ConstruirDedos();
    }

    /// <summary>Cierra la mano (0 = abierta, 1 = puno cerrado).</summary>
    public void SetPuno(float valor)
    {
        punoObjetivo = Mathf.Clamp01(valor);
    }

    /// <summary>Cierra el indice como si apretase el gatillo (0 = estirado, 1 = recogido).</summary>
    public void SetGatillo(float valor)
    {
        indiceObjetivo = Mathf.Clamp01(valor);
    }

    /// <summary>Recoge el pulgar, por ejemplo al tocar los botones del mando.</summary>
    public void SetPulgar(float valor)
    {
        pulgarObjetivo = Mathf.Clamp01(valor);
    }

    /// <summary>Fuerza la pose final sin suavizado (util para probar en el editor).</summary>
    public void ForzarPose(float puno, float gatillo, float pulgar)
    {
        punoObjetivo = punoActual = Mathf.Clamp01(puno);
        indiceObjetivo = indiceActual = Mathf.Clamp01(gatillo);
        pulgarObjetivo = pulgarActual = Mathf.Clamp01(pulgar);
        Aplicar();
    }

    /// <summary>Numero de dedos con huesos encontrados (5 significa que el rig esta bien).</summary>
    public int DedosEncontrados()
    {
        if (!listo)
        {
            return 0;
        }

        int encontrados = 0;
        for (int d = 0; d < dedos.Length; d++)
        {
            if (dedos[d].huesos != null && dedos[d].huesos[0] != null)
            {
                encontrados++;
            }
        }

        return encontrados;
    }

    /// <summary>Devuelve la mano a la pose abierta original del FBX.</summary>
    public void ReiniciarPose()
    {
        if (!listo)
        {
            return;
        }

        for (int d = 0; d < dedos.Length; d++)
        {
            for (int f = 0; f < Falanges; f++)
            {
                if (dedos[d].huesos[f] != null)
                {
                    dedos[d].huesos[f].localRotation = dedos[d].reposo[f];
                }
            }
        }
    }

    // ---------------------------------------------------------------- Construccion

    private void ConstruirDedos()
    {
        if (listo)
        {
            return;
        }

        // Direccion "a lo ancho de la mano" tomada de los huesos raiz de indice y menique.
        Transform indiceBase = BuscarHueso("index", 1);
        Transform meniqueBase = BuscarHueso("pinky", 1);

        Vector3 ejeAncho = Vector3.forward;
        if (indiceBase != null && meniqueBase != null)
        {
            Vector3 delta = meniqueBase.position - indiceBase.position;
            if (delta.sqrMagnitude > 1e-12f)
            {
                ejeAncho = delta.normalized;
            }
        }

        for (int d = 0; d < dedos.Length; d++)
        {
            string nombre = NombresDedos[d];
            var dedo = new Dedo
            {
                huesos = new Transform[Falanges],
                reposo = new Quaternion[Falanges],
                ejeFlexion = Vector3.right,
                ejeAbduccion = Vector3.up,
                escalaFlexion = 1f
            };

            for (int f = 0; f < Falanges; f++)
            {
                Transform hueso = BuscarHueso(nombre, f + 1);
                dedo.huesos[f] = hueso;
                if (hueso != null)
                {
                    dedo.reposo[f] = hueso.localRotation;
                }
            }

            Transform raiz = dedo.huesos[0];
            Transform siguiente = dedo.huesos[1];

            if (raiz != null && siguiente != null)
            {
                // Eje de flexion = dedo x ancho de la mano, expresado en el espacio propio del hueso.
                Vector3 direccionDedo = (siguiente.position - raiz.position).normalized;
                Vector3 ejeMundo = Vector3.Cross(direccionDedo, ejeAncho);

                if (ejeMundo.sqrMagnitude < 1e-8f)
                {
                    ejeMundo = Vector3.Cross(direccionDedo, transform.right);
                }

                if (ejeMundo.sqrMagnitude > 1e-8f)
                {
                    dedo.ejeFlexion = raiz.InverseTransformDirection(ejeMundo.normalized);
                }

                dedo.ejeAbduccion = raiz.InverseTransformDirection(ejeAncho);
            }

            bool esPulgar = nombre == "thumb";
            bool invertir = esPulgar ? invertirPulgar : invertirFlexion;
            dedo.escalaFlexion = invertir ? -1f : 1f;

            dedos[d] = dedo;
        }

        listo = true;
    }

    private Transform BuscarHueso(string dedo, int falange)
    {
        // El sufijo ya incluye el _jnt final (los huesos se llaman index_01_r_jnt).
        string nombre = dedo + "_0" + falange + sufijoHuesos;
        Transform[] todos = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre)
            {
                return todos[i];
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- Bucle

    private void Update()
    {
        if (!listo)
        {
            return;
        }

        // Interpolacion exponencial independiente del framerate y sin reservar memoria.
        float paso = 1f - Mathf.Exp(-Mathf.Max(velocidadDedos, 0.01f) * Time.deltaTime);

        punoActual = Mathf.Lerp(punoActual, punoObjetivo, paso);
        indiceActual = Mathf.Lerp(indiceActual, indiceObjetivo, paso);
        pulgarActual = Mathf.Lerp(pulgarActual, pulgarObjetivo, paso);

        Aplicar();
    }

    private void Aplicar()
    {
        for (int d = 0; d < dedos.Length; d++)
        {
            Dedo dedo = dedos[d];
            if (dedo.huesos == null || dedo.huesos[0] == null)
            {
                continue;
            }

            bool esPulgar = d == 4;
            float cierre;

            if (esPulgar)
            {
                cierre = pulgarActual;
            }
            else if (d == 0)
            {
                // El indice responde al gatillo y, en menor medida, al puno.
                cierre = Mathf.Max(indiceActual, punoActual * pesoGripEnIndice);
            }
            else
            {
                cierre = punoActual;
            }

            float proximal = esPulgar ? pulgarProximal : flexionProximal;
            float media = esPulgar ? pulgarMedia : flexionMedia;
            float distal = esPulgar ? pulgarDistal : flexionDistal;

            // Apertura: los dedos se separan un poco cuando la mano esta abierta.
            float abduccion = 0f;
            if (!esPulgar)
            {
                abduccion = (d - 1.5f) * aperturaDedos * (1f - cierre);
            }

            for (int f = 0; f < Falanges; f++)
            {
                Transform hueso = dedo.huesos[f];
                if (hueso == null)
                {
                    continue;
                }

                Quaternion rotacion = dedo.reposo[f];

                if (f == 0)
                {
                    rotacion *= Quaternion.AngleAxis(abduccion * dedo.escalaFlexion, dedo.ejeAbduccion);
                }

                // Sin arrays temporales: cero asignaciones por frame.
                float baseFlexion = f == 0 ? proximal : (f == 1 ? media : distal);
                float angulo = baseFlexion * cierre * dedo.escalaFlexion;
                rotacion *= Quaternion.AngleAxis(angulo, dedo.ejeFlexion);

                hueso.localRotation = rotacion;
            }
        }
    }
}
