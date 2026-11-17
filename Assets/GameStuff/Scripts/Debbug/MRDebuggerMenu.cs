using UnityEngine;
using UnityEngine.UI;

public class MRDebuggerMenu : MonoBehaviour
{
    [Header("Referencias de Iluminación")]
    [SerializeField] private RoomLightManager roomLightManager;

    [Header("Sistema de Spawner")]
    [SerializeField] private GameObject[] spawnablePrefabs;
    [SerializeField] private Transform spawnPoint; // Opcional: origen del spawn (si queda vacío, usará la cámara)

    [Header("Colocación sobre la superficie real")]
    [Tooltip("Margen entre la base del objeto y la superficie detectada. La capa de oscuridad (DarknessRevealer) escribe profundidad y es negra donde no llega luz: un objeto hundido en la malla de MRUK sale negro puro y sin contorno.")]
    [SerializeField] private float surfaceClearance = MRSurfacePlacement.DefaultClearance;

    [Tooltip("Si está activo, el objeto se apoya en la superficie que haya debajo (mesa, suelo) en vez de caer desde la altura de la cabeza.")]
    [SerializeField] private bool placeOnSurface = true;

    [Tooltip("Distancia máxima hacia abajo para buscar la superficie real (m).")]
    [SerializeField] private float surfaceRayDistance = 3f;

    private void Start()
    {
        // En Unity 6, FindFirstObjectByType es la forma optimizada de buscar objetos únicos
        if (roomLightManager == null)
        {
            roomLightManager = Object.FindFirstObjectByType<RoomLightManager>();
        }

        if (spawnPoint == null && Camera.main != null)
        {
            spawnPoint = Camera.main.transform;
        }
    }

    // --- FUNCIONES DE LUZ ---
    public void DebugTurnLightsOn()
    {
        if (roomLightManager != null)
        {
            roomLightManager.SetRoomLight(true);
            Debug.Log("[Debugger] Luces Encendidas");
        }
    }

    public void DebugTurnLightsOff()
    {
        if (roomLightManager != null)
        {
            roomLightManager.SetRoomLight(false);
            Debug.Log("[Debugger] Luces Apagadas");
        }
    }

    public void DebugTriggerFlicker()
    {
        if (roomLightManager != null)
        {
            roomLightManager.TriggerFlicker(roomLightManager.testFlickerDuration,roomLightManager.testFlickerCount);
            Debug.Log("[Debugger] Parpadeo Activado");
        }
    }

    // --- FUNCIONES DE SPAWN ---
    public void SpawnPrefab(int index)
    {
        if (spawnablePrefabs == null || index < 0 || index >= spawnablePrefabs.Length)
        {
            Debug.LogError("[Debugger] Índice de prefab inválido o la lista está vacía.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[Debugger] No se encontró un punto de origen (Cámara) para spawnear.");
            return;
        }

        // Calculamos una posición a 1.5 metros enfrente de donde mira el jugador
        Vector3 position = spawnPoint.position + spawnPoint.forward * 1.5f;
        
        // Mantenemos el objeto derecho (evitamos que se incline si el jugador mira hacia arriba/abajo)
        Quaternion rotation = Quaternion.LookRotation(spawnPoint.forward);
        rotation.x = 0;
        rotation.z = 0;

        GameObject spawnedObject = Instantiate(spawnablePrefabs[index], position, rotation);

        // La capa de oscuridad del cuarto tapa (en negro) lo que quede por debajo de la malla de MRUK,
        // asi que el objeto se apoya en la superficie detectada con un margen en vez de caer desde
        // la altura de la cabeza.
        if (placeOnSurface)
        {
            float surfaceHeight;
            if (MRSurfacePlacement.TrySnapToSurfaceBelow(spawnedObject, position, surfaceClearance, surfaceRayDistance, out surfaceHeight))
            {
                Debug.Log($"[Debugger] {spawnedObject.name} apoyado en la superficie y={surfaceHeight:0.###} con margen {surfaceClearance:0.###} m.");
            }
        }

        Debug.Log($"[Debugger] Spawned: {spawnedObject.name} en la posición {spawnedObject.transform.position}");
    }
}