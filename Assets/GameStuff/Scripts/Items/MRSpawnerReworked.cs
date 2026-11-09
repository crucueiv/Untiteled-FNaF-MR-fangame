using UnityEngine;
using Meta.XR.MRUtilityKit;

public class MRSpawnerReworked : MonoBehaviour
{
    [Header("Configuración Principal")]
    [Tooltip("El prefab del objeto que deseas generar.")]
    public GameObject prefabToSpawn;

    [Tooltip("La superficie objetivo.")]
    public MRUKAnchor.SceneLabels targetSurface = MRUKAnchor.SceneLabels.TABLE;

    [Header("Posicionamiento Relativo")]
    [Tooltip("-1 es el extremo izquierdo/inferior/trasero. 1 es el derecho/superior/delantero. 0 es el centro.")]
    [Range(-1f, 1f)] public float sliderX = 0f;
    [Range(-1f, 1f)] public float sliderY = 0f;
    [Range(-1f, 1f)] public float sliderZ = 0f;

    [Header("Ajustes de Superficie")]
    [Tooltip("Si activo, ignora sliderZ y pega el objeto a la cara correcta automáticamente.")]
    public bool snapToSurface = true;

    [Tooltip("Distancia mínima que el objeto mantiene de la superficie para no hundirse.")]
    public float surfacePadding = 0.05f;

    [Tooltip("Margen que el objeto mantiene respecto a los bordes para no caerse. 0.1m recomendado.")]
    public float borderMargin = 0.1f;

    [Header("Rotación")]
    [Tooltip("Rotación adicional en grados aplicada después de alinear con la superficie.")]
    public Vector3 rotationOffset = Vector3.zero;

    // FIX 1: labels que representan "superficie superior" (spawn encima, cara top del volumen)
    private static readonly MRUKAnchor.SceneLabels TopSurfaceLabels =
        MRUKAnchor.SceneLabels.FLOOR   |
        MRUKAnchor.SceneLabels.TABLE   |
        MRUKAnchor.SceneLabels.COUCH   |
        MRUKAnchor.SceneLabels.BED     |
        MRUKAnchor.SceneLabels.STORAGE;

    // labels que representan "superficie inferior" (spawn colgando debajo, cara bottom)
    private static readonly MRUKAnchor.SceneLabels BottomSurfaceLabels =
        MRUKAnchor.SceneLabels.CEILING;

    private void Start()
    {
        MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomLoaded);
    }

    private void OnRoomLoaded(MRUKRoom room)
    {
        MRUKAnchor bestAnchor = null;
        float minDistance = float.MaxValue;
        Transform playerHead = Camera.main?.transform;
        if (playerHead == null) return;

        foreach (var anchor in room.Anchors)
        {
            if ((anchor.Label & targetSurface) == 0) continue;
            float dist = Vector3.Distance(playerHead.position, anchor.transform.position);
            if (dist < minDistance) { minDistance = dist; bestAnchor = anchor; }
        }

        if (bestAnchor != null)
            SpawnObjectOn(bestAnchor);
        else
            Debug.LogWarning($"MRSpawnerReworked: No se encontró superficie '{targetSurface}'.");
    }

    private void SpawnObjectOn(MRUKAnchor anchor)
    {
        if      (anchor.VolumeBounds.HasValue) SpawnOnVolume(anchor);
        else if (anchor.PlaneRect.HasValue)    SpawnOnPlane(anchor);
        else
        {
            Vector3 pos = anchor.transform.position + anchor.transform.forward * surfacePadding;
            Instantiate(prefabToSpawn, pos, anchor.transform.rotation * Quaternion.Euler(rotationOffset));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  VOLUMEN (mesas, camas, armarios, sofás…)
    // ─────────────────────────────────────────────────────────────
    private void SpawnOnVolume(MRUKAnchor anchor)
    {
        Bounds b = anchor.VolumeBounds.Value;

        // FIX 3: rangos seguros con margen de borde para evitar que el objeto se caiga
        float xMin = Mathf.Min(b.min.x + borderMargin, b.center.x);
        float xMax = Mathf.Max(b.max.x - borderMargin, b.center.x);
        float yMin = Mathf.Min(b.min.y + borderMargin, b.center.y);
        float yMax = Mathf.Max(b.max.y - borderMargin, b.center.y);
        float zMin = Mathf.Min(b.min.z + borderMargin, b.center.z);
        float zMax = Mathf.Max(b.max.z - borderMargin, b.center.z);

        float tx = (sliderX + 1f) * 0.5f; // [0..1]
        float ty = (sliderY + 1f) * 0.5f;

        // FIX 1 + 2: GetFaceIndex ahora devuelve la cara correcta para cada label
        int face = GetFaceIndex(anchor);

        Vector3 localPos;
        Quaternion localFaceRot;

        switch (face)
        {
            case 0: // Cara delantera (local +Z) — paredes frontales, marcos
                localPos    = new Vector3(Mathf.Lerp(xMin, xMax, tx), Mathf.Lerp(yMin, yMax, ty), b.max.z + surfacePadding);
                localFaceRot = Quaternion.identity;
                break;

            case 1: // Cara trasera (local -Z)
                localPos    = new Vector3(Mathf.Lerp(xMax, xMin, tx), Mathf.Lerp(yMin, yMax, ty), b.min.z - surfacePadding);
                localFaceRot = Quaternion.Euler(0f, 180f, 0f);
                break;

            case 2: // Cara derecha (local +X) — FIX 2: ahora alcanzable
                localPos    = new Vector3(b.max.x + surfacePadding, Mathf.Lerp(yMin, yMax, ty), Mathf.Lerp(zMax, zMin, tx));
                localFaceRot = Quaternion.Euler(0f, 90f, 0f);
                break;

            case 3: // Cara izquierda (local -X) — FIX 2: ahora alcanzable
                localPos    = new Vector3(b.min.x - surfacePadding, Mathf.Lerp(yMin, yMax, ty), Mathf.Lerp(zMin, zMax, tx));
                localFaceRot = Quaternion.Euler(0f, -90f, 0f);
                break;

            case 4: // Cara superior (local +Y) — MESA, SUELO, CAMA, SOFÁ
                localPos    = new Vector3(Mathf.Lerp(xMin, xMax, tx), b.max.y + surfacePadding, Mathf.Lerp(zMin, zMax, ty));
                // FIX: Quaternion.identity mantiene el objeto erguido (Y=arriba, Z=frente del anchor).
                // El Euler(-90,0,0) original tumbaba el objeto de lado.
                localFaceRot = Quaternion.identity;
                break;

            case 5: // Cara inferior (local -Y) — TECHO
                localPos    = new Vector3(Mathf.Lerp(xMin, xMax, tx), b.min.y - surfacePadding, Mathf.Lerp(zMin, zMax, ty));
                localFaceRot = Quaternion.Euler(180f, 0f, 0f); // objeto cuelga hacia abajo
                break;

            default:
                localPos    = b.center;
                localFaceRot = Quaternion.identity;
                break;
        }

        if (!snapToSurface)
        {
            // Modo libre: los 3 sliders mueven el objeto libremente dentro del volumen
            localPos = new Vector3(
                Mathf.Lerp(b.center.x - b.extents.x, b.center.x + b.extents.x, (sliderX + 1f) * 0.5f),
                Mathf.Lerp(b.center.y - b.extents.y, b.center.y + b.extents.y, (sliderY + 1f) * 0.5f),
                Mathf.Lerp(b.center.z - b.extents.z, b.center.z + b.extents.z, (sliderZ + 1f) * 0.5f));
            localFaceRot = Quaternion.identity;
        }

        Vector3 worldPos = anchor.transform.TransformPoint(localPos);
        Quaternion worldRot = anchor.transform.rotation * localFaceRot * Quaternion.Euler(rotationOffset);
        Instantiate(prefabToSpawn, worldPos, worldRot);
    }

    // ─────────────────────────────────────────────────────────────
    //  PLANO (paredes, suelo/techo como plano puro, marcos)
    // ─────────────────────────────────────────────────────────────
    private void SpawnOnPlane(MRUKAnchor anchor)
    {
        Rect rect = anchor.PlaneRect.Value;
        Vector3 surfaceNormal = anchor.transform.forward;

        // FIX 3: margen de borde en los extremos del plano
        float halfW = Mathf.Max(0f, rect.width  * 0.5f - borderMargin);
        float halfH = Mathf.Max(0f, rect.height * 0.5f - borderMargin);

        float localX = Mathf.Lerp(-halfW, halfW, (sliderX + 1f) * 0.5f);
        float localY = Mathf.Lerp(-halfH, halfH, (sliderY + 1f) * 0.5f);
        float localZ = snapToSurface ? surfacePadding : sliderZ * 0.5f;

        Vector3 worldPos = anchor.transform.TransformPoint(new Vector3(localX, localY, localZ));

        // Rotación inteligente según si el plano es horizontal o vertical
        bool isHorizontal = Mathf.Abs(surfaceNormal.y) > 0.707f;
        Quaternion baseRot;

        if (isHorizontal)
        {
            // Suelo / techo plano: objeto erguido mirando hacia la cámara
            Vector3 camForward = Camera.main != null
                ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (camForward.sqrMagnitude < 0.001f) camForward = Vector3.forward;
            // En techo la normal apunta abajo, invertimos el up del objeto
            Vector3 upDir = surfaceNormal.y > 0f ? Vector3.up : Vector3.down;
            baseRot = Quaternion.LookRotation(camForward, upDir);
        }
        else
        {
            // Pared / marco: el objeto mira hacia la habitación (normal = hacia el jugador en MRUK)
            Vector3 projUp = Vector3.ProjectOnPlane(Vector3.up, surfaceNormal).normalized;
            baseRot = Quaternion.LookRotation(surfaceNormal, projUp);
        }

        Instantiate(prefabToSpawn, worldPos, baseRot * Quaternion.Euler(rotationOffset));
    }

    // ─────────────────────────────────────────────────────────────
    //  DETECCIÓN DE CARA CORRECTA (FIX 1 + 2)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Determina qué cara del volumen usar basándose en la etiqueta del anchor,
    /// convirtiendo la dirección mundo deseada al índice de cara local.
    /// 0=front(+Z)  1=back(-Z)  2=right(+X)  3=left(-X)  4=top(+Y)  5=bottom(-Y)
    /// </summary>
    private int GetFaceIndex(MRUKAnchor anchor)
    {
        if ((anchor.Label & TopSurfaceLabels) != 0)
            // Mesa, suelo, cama, sofá → queremos la cara que mira hacia arriba en el mundo
            return WorldDirectionToFaceIndex(anchor, Vector3.up);

        if ((anchor.Label & BottomSurfaceLabels) != 0)
            // Techo → cara que mira hacia abajo
            return WorldDirectionToFaceIndex(anchor, Vector3.down);

        // Paredes, marcos, cualquier otro → forward del anchor (apunta a la habitación en MRUK)
        return WorldDirectionToFaceIndex(anchor, anchor.transform.forward);
    }

    /// <summary>
    /// Proyecta una dirección mundo al espacio local del anchor y devuelve
    /// el índice de la cara cuyo eje local mejor se alinea con ella.
    /// </summary>
    private int WorldDirectionToFaceIndex(MRUKAnchor anchor, Vector3 worldDir)
    {
        Vector3 local = anchor.transform.InverseTransformDirection(worldDir.normalized);
        float ax = Mathf.Abs(local.x);
        float ay = Mathf.Abs(local.y);
        float az = Mathf.Abs(local.z);

        if (az >= ax && az >= ay) return local.z > 0f ? 0 : 1; // front / back
        if (ax >= ay)             return local.x > 0f ? 2 : 3; // right / left
        return                           local.y > 0f ? 4 : 5; // top  / bottom
    }
}