using UnityEngine;
using UnityEngine.UI;

public class MRDebuggerMenu : MonoBehaviour
{
    [Header("Referencias de Iluminación")]
    [SerializeField] private RoomLightManager roomLightManager;

    [Header("Sistema de Spawner")]
    [SerializeField] private GameObject[] spawnablePrefabs;
    [SerializeField] private Transform spawnPoint; // Opcional: origen del spawn (si queda vacío, usará la cámara)

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
        Debug.Log($"[Debugger] Spawned: {spawnedObject.name} en la posición {position}");
    }
}