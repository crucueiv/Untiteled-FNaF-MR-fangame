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
    [Tooltip("Rotación adicional en grados (aplicada después de alinear con la superficie).")]
    public Vector3 rotationOffset = Vector3.zero;

    private const float InvSqrt2 = 0.707107f;

    private void Start()
    {
        MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomLoaded);
    }

    private void OnRoomLoaded(MRUKRoom room)
    {
        MRUKAnchor bestAnchor = null;
        float minDistance = float.MaxValue;
        Transform playerHead = Camera.main != null ? Camera.main.transform : null;

        if (playerHead == null) return;

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
        Vector3 surfaceNormal = anchor.transform.forward;

        if (anchor.VolumeBounds.HasValue)
        {
            SpawnOnVolume(anchor);
        }
        else if (anchor.PlaneRect.HasValue)
        {
            SpawnOnPlane(anchor);
        }
        else
        {
            Vector3 fallbackPos = anchor.transform.position + surfaceNormal * surfacePadding;
            Quaternion fallbackRot = Quaternion.LookRotation(surfaceNormal) * Quaternion.Euler(rotationOffset);
            Instantiate(prefabToSpawn, fallbackPos, fallbackRot);
        }
    }

    private void SpawnOnVolume(MRUKAnchor anchor)
    {
        Bounds bounds = anchor.VolumeBounds.Value;
        Vector3 surfaceNormal = anchor.transform.forward;

        int surfaceFaceIndex = GetSurfaceFaceIndex(anchor);

        Vector3 localPos;
        Quaternion localFaceRotation;

        switch (surfaceFaceIndex)
        {
            case 0:
            {
                localPos = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, (sliderX + 1f) * 0.5f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (sliderY + 1f) * 0.5f),
                    bounds.max.z + surfacePadding);
                localFaceRotation = Quaternion.identity;
                break;
            }
            case 1:
            {
                localPos = new Vector3(
                    Mathf.Lerp(-bounds.max.x, -bounds.min.x, (sliderX + 1f) * 0.5f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (sliderY + 1f) * 0.5f),
                    bounds.min.z - surfacePadding);
                localFaceRotation = Quaternion.Euler(0f, 180f, 0f);
                break;
            }
            case 2:
            {
                localPos = new Vector3(
                    bounds.max.x + surfacePadding,
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (sliderY + 1f) * 0.5f),
                    Mathf.Lerp(-bounds.max.z, -bounds.min.z, (sliderX + 1f) * 0.5f));
                localFaceRotation = Quaternion.Euler(0f, 90f, 0f);
                break;
            }
            case 3:
            {
                localPos = new Vector3(
                    bounds.min.x - surfacePadding,
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (sliderY + 1f) * 0.5f),
                    Mathf.Lerp(bounds.min.z, bounds.max.z, (sliderX + 1f) * 0.5f));
                localFaceRotation = Quaternion.Euler(0f, -90f, 0f);
                break;
            }
            case 4:
            {
                localPos = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, (sliderX + 1f) * 0.5f),
                    bounds.max.y + surfacePadding,
                    Mathf.Lerp(-bounds.max.z, -bounds.min.z, (sliderY + 1f) * 0.5f));
                localFaceRotation = Quaternion.Euler(-90f, 0f, 0f);
                break;
            }
            case 5:
            {
                localPos = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, (sliderX + 1f) * 0.5f),
                    bounds.min.y - surfacePadding,
                    Mathf.Lerp(bounds.min.z, bounds.max.z, (sliderY + 1f) * 0.5f));
                localFaceRotation = Quaternion.Euler(90f, 0f, 0f);
                break;
            }
            default:
            {
                localPos = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, (sliderX + 1f) * 0.5f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (sliderY + 1f) * 0.5f),
                    bounds.max.z + surfacePadding);
                localFaceRotation = Quaternion.identity;
                break;
            }
        }

        if (!snapToSurface)
        {
            localPos = new Vector3(
                bounds.center.x + bounds.extents.x * sliderX,
                bounds.center.y + bounds.extents.y * sliderY,
                bounds.center.z + bounds.extents.z * sliderZ);
            localFaceRotation = Quaternion.identity;
        }

        Vector3 worldPos = anchor.transform.TransformPoint(localPos);
        Quaternion worldRot = anchor.transform.rotation * localFaceRotation * Quaternion.Euler(rotationOffset);
        Instantiate(prefabToSpawn, worldPos, worldRot);
    }

    private int GetSurfaceFaceIndex(MRUKAnchor anchor)
    {
        Vector3 normal = anchor.transform.forward;
        bool isVertical = (anchor.Label & (MRUKAnchor.SceneLabels.WALL_FACE | MRUKAnchor.SceneLabels.DOOR_FRAME | MRUKAnchor.SceneLabels.WINDOW_FRAME)) != 0;

        if (isVertical)
        {
            return 0;
        }

        if (normal.y >= InvSqrt2)
        {
            return 0;
        }
        else if (normal.y <= -InvSqrt2)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    private void SpawnOnPlane(MRUKAnchor anchor)
    {
        Rect rect = anchor.PlaneRect.Value;

        float localX = Mathf.Lerp(-rect.width * 0.5f, rect.width * 0.5f, (sliderX + 1f) * 0.5f);
        float localY = Mathf.Lerp(-rect.height * 0.5f, rect.height * 0.5f, (sliderY + 1f) * 0.5f);
        float localZ = surfacePadding;

        if (!snapToSurface)
        {
            localZ = sliderZ * 0.5f;
        }

        Vector3 localPos = new Vector3(localX, localY, localZ);
        Vector3 worldPos = anchor.transform.TransformPoint(localPos);
        Vector3 surfaceNormal = anchor.transform.forward;

        Quaternion worldRot = Quaternion.LookRotation(surfaceNormal) * Quaternion.Euler(rotationOffset);
        Instantiate(prefabToSpawn, worldPos, worldRot);
    }
}
