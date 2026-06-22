using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;

public class RoomValidator : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el CenterEyeAnchor del OVRCameraRig")]
    public Transform playerHead; 
    
    [Header("Resultados de Validación")]
    public bool isRoomValid = false;
    public MRUKAnchor closestTable;

    private void Start()
    {
        // Nos suscribimos al evento que se dispara cuando MRUK termina de cargar la habitación
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomCreated);
        }
    }

    private void OnDestroy()
    {
        // Es buena práctica desuscribirse para evitar errores de memoria
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.RemoveListener(OnRoomCreated);
        }
    }

    private void OnRoomCreated(MRUKRoom room)
    {
        int doorCount = 0;
        int tableCount = 0;
        int bedOrCouchCount = 0;

        List<MRUKAnchor> tables = new List<MRUKAnchor>();

        // Iteramos sobre todos los anclajes (muebles/paredes) encontrados en la habitación
        foreach (var anchor in room.Anchors)
        {
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.DOOR_FRAME)) doorCount++;
            
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
            {
                tableCount++;
                tables.Add(anchor);
            }
            
            if (anchor.HasAnyLabel(MRUKAnchor.SceneLabels.BED) || anchor.HasAnyLabel(MRUKAnchor.SceneLabels.COUCH)) bedOrCouchCount++;
        }

        // Validación de requisitos mínimos
        if (doorCount >= 1 && tableCount >= 1 && bedOrCouchCount >= 1)
        {
            isRoomValid = true;
            Debug.Log("<color=green>Validación MR exitosa:</color> La habitación cumple los requisitos para el juego.");
            FindClosestTable(tables);
        }
        else
        {
            isRoomValid = false;
            Debug.LogError($"<color=red>Validación MR fallida:</color> Faltan elementos. Puertas: {doorCount}, Mesas: {tableCount}, Camas/Sofás: {bedOrCouchCount}");
        }
    }

    private void FindClosestTable(List<MRUKAnchor> tables)
    {
        if (playerHead == null)
        {
            Debug.LogWarning("Falta asignar el Player Head en el RoomValidator. No se puede calcular la distancia.");
            return;
        }

        float minDistance = float.MaxValue;
        MRUKAnchor closest = null;

        foreach (var table in tables)
        {
            // Calculamos la distancia entre la cabeza del jugador y el centro de la mesa
            float distance = Vector3.Distance(playerHead.position, table.transform.position);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = table;
            }
        }

        closestTable = closest;
        Debug.Log($"La mesa más cercana está a {minDistance:F2} metros. Guardada como referencia.");
    }
}