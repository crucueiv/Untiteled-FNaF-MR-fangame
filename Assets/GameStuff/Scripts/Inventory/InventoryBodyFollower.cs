using UnityEngine;

/// <summary>
/// Mantiene el inventario accesible alrededor de la cintura y el torso,
/// siguiendo el giro horizontal de la cabeza.
/// </summary>
public class InventoryBodyFollower : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara o ancla de la cabeza del jugador. Si se deja vacío, se usa MainCamera.")]
    public Transform headTransform;

    [Header("Posición del cuerpo")]
    [Tooltip("Altura del rig de inventario respecto a la cabeza. La posición de los slots hijos define los bolsillos y el hombro.")]
    public float heightOffset = -0.7f;

    [Tooltip("Velocidad a la que sigue el inventario al torso. Valores mayores reducen el retraso al girar.")]
    public float smoothSpeed = 12f;

    private void Awake()
    {
        if (headTransform == null && Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (headTransform == null)
        {
            return;
        }

        Vector3 targetPosition = headTransform.position + Vector3.up * heightOffset;
        Quaternion targetRotation = Quaternion.Euler(0f, headTransform.eulerAngles.y, 0f);
        float followAmount = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPosition, followAmount);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followAmount);
    }
}
