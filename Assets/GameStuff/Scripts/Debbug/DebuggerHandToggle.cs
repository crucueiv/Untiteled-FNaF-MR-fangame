using UnityEngine;

public class DebuggerHandToggle : MonoBehaviour
{
    [Header("Referencia al Menú")]
    [Tooltip("Arrastra aquí el Canvas de tu menú Debugger")]
    [SerializeField] private GameObject debuggerCanvas;

    private void Start()
    {
        // Ocultamos el menú al inicio del juego para que no estorbe
        if (debuggerCanvas != null)
        {
            debuggerCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        // OVRInput.Button.Start corresponde al botón de 3 rayas.
        // Especificamos LTouch para asegurarnos de que solo lea el mando izquierdo.
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
        {
            if (debuggerCanvas != null)
            {
                // Alternamos el estado actual (si está apagado, se enciende; y viceversa)
                debuggerCanvas.SetActive(!debuggerCanvas.activeSelf);
            }
        }
    }
}