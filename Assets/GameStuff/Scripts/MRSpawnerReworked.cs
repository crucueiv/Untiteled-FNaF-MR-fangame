using UnityEngine;
using Meta.XR.MRUtilityKit;

public class MRSpawnerReworked : MonoBehaviour
{
    [Header("Configuración Principal")]
    [Tooltip("El prefab del objeto que deseas generar (ej. Linterna).")]
    public GameObject prefabToSpawn;
    
    [Tooltip("La superficie objetivo.")]
    public MRUKAnchor.SceneLabels targetSurface = MRUKAnchor.SceneLabels.TABLE;

    [Header("Posicionamiento Relativo")]
    [Tooltip("-1 es el extremo izquierdo/inferior/trasero. 1 es el derecho/superior/delantero. 0 es el centro exacto.")]
    [Range(-1f, 1f)] public float sliderX = 0f;
    [Range(-1f, 1f)] public float sliderY = 0f;
    [Range(-1f, 1f)] public float sliderZ = 0f;

    [Header("Ajustes Inteligentes de Superficie")]
    [Tooltip("Si está activo, ignora el slider del eje de profundidad/altura y pega el objeto a la superficie automáticamente.")]
    public bool snapToSurface = true;
    
    [Tooltip("Margen para que el objeto no se hunda dentro de la malla.")]
    public float surfacePadding = 0.05f;

    [Header("Ajuste de Rotación")]
    public Vector3 rotationOffset = Vector3.zero;

    private void Start()
    {
        // Esperamos a que la habitación virtual esté completamente cargada
        MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomLoaded);
    }

    private void OnRoomLoaded(MRUKRoom room)
    {
        MRUKAnchor bestAnchor = null;
        float minDistance = float.MaxValue;
        Transform playerHead = Camera.main != null ? Camera.main.transform : null;

        if (playerHead == null) return;

        // Buscar el ancla más cercana al jugador que coincida con la etiqueta
        foreach (var anchor in room.Anchors)
        {
            if ((anchor.Label & targetSurface) != 0)
            {
                float dist = Vector3.Distance(playerHead.position, anchor.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestAnchor = anchor;
                }
            }
        }

        if (bestAnchor != null)
        {
            SpawnObjectExactly(bestAnchor);
        }
        else
        {
            Debug.LogWarning($"MRSpawnerReworked: No se encontró superficie con etiqueta {targetSurface}");
        }
    }

    private void SpawnObjectExactly(MRUKAnchor anchor)
    {
        // 1. Obtenemos la caja de colisión LOCAL del ancla
        Bounds bounds = anchor.VolumeBounds.HasValue ? anchor.VolumeBounds.Value : new Bounds(Vector3.zero, Vector3.zero);

        // 2. Calculamos la posición base usando los sliders. 
        // bounds.center es el centro local, bounds.extents es la mitad de la medida total.
        float localX = bounds.center.x + (bounds.extents.x * sliderX);
        float localY = bounds.center.y + (bounds.extents.y * sliderY);
        float localZ = bounds.center.z + (bounds.extents.z * sliderZ);

        // 3. Sistema inteligente: ¿Pared o Mueble?
        if (snapToSurface)
        {
            bool isVerticalSurface = (anchor.Label & (MRUKAnchor.SceneLabels.WALL_FACE | MRUKAnchor.SceneLabels.DOOR_FRAME | MRUKAnchor.SceneLabels.WINDOW_FRAME)) != 0;

            if (isVerticalSurface)
            {
                // Es una pared: Forzamos la profundidad (Z) para que quede pegado a la cara interior de la pared.
                localZ = bounds.max.z + surfacePadding;
            }
            else
            {
                // Es un mueble o suelo: Forzamos la altura (Y) para que descanse sobre la superficie.
                localY = bounds.max.y + surfacePadding;
            }
        }

        Vector3 finalLocalPosition = new Vector3(localX, localY, localZ);

        // 4. Transformamos la coordenada local perfecta a la coordenada real del mundo
        Vector3 finalWorldPosition = anchor.transform.TransformPoint(finalLocalPosition);

        // 5. Ajustamos la rotación respetando la inclinación del ancla
        Quaternion finalWorldRotation = anchor.transform.rotation * Quaternion.Euler(rotationOffset);

        // 6. Generamos el objeto
        Instantiate(prefabToSpawn, finalWorldPosition, finalWorldRotation);
    }
}