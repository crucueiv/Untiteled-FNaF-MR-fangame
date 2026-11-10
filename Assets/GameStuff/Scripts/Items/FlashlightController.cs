using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto Light (Spotlight) hijo de la linterna")]
    public Light spotlight;

    private bool isFlashlightOn;

    private void Start()
    {
        isFlashlightOn = false;

        if (spotlight != null)
        {
            spotlight.enabled = isFlashlightOn;
        }
        else
        {
            Debug.LogWarning("FlashlightController: No has asignado el Spotlight en el inspector.");
        }
    }

    /// <summary>
    /// Se invoca desde GrabbedObjectButton al pulsar el botón configurado.
    /// </summary>
    public void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;

        if (spotlight != null)
        {
            spotlight.enabled = isFlashlightOn;
        }
    }
}
