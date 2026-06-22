using UnityEngine;

public class InventoryBodyFollower : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí la cámara principal del jugador (CenterEyeAnchor).")]
    public Transform headTransform;

    [Header("Configuración")]
    [Tooltip("Distancia hacia abajo desde la cabeza (ej. -0.4 para la cintura).")]
    public float heightOffset = -0.4f;
    [Tooltip("Velocidad a la que el inventario sigue al jugador.")]
    public float smoothSpeed = 8f;

    private void Update()
    {
        if (headTransform == null) return;

        // Calculamos la posición: justo debajo de la cabeza
        Vector3 targetPosition = headTransform.position + Vector3.up * heightOffset;
        
        // Calculamos la rotación: solo nos interesa la rotación en el eje Y (giro del cuerpo)
        Vector3 euler = headTransform.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(0, euler.y, 0);

        // Movemos el inventario de forma suave para evitar mareos
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}
