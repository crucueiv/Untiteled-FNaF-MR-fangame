using System.Collections;
using UnityEngine;

public class RoomLightManager : MonoBehaviour
{
    [Header("Configuración de Passthrough")]
    [Tooltip("El nivel de oscuridad cuando la luz está apagada. -1 es negro total, 0 es normal.")]
    [Range(-1f, 0f)]
    public float darkBrightness = -0.8f;
    
    private OVRPassthroughLayer passthroughLayer;
    private bool isRoomLightOn = true;

    void Start()
    {
        // Busca automáticamente el componente generado por el Building Block de Passthrough
        passthroughLayer = FindAnyObjectByType<OVRPassthroughLayer>();
        
        if (passthroughLayer == null)
        {
            Debug.LogError("RoomLightManager: No se encontró un OVRPassthroughLayer en la escena.");
        }
    }

    // Llama a esta función con 'false' para apagar la habitación o 'true' para encenderla
    public void SetRoomLight(bool isOn)
    {
        isRoomLightOn = isOn;
        ApplyLightState(isRoomLightOn ? 0f : darkBrightness);
    }

    // Llama a esta función para hacer un parpadeo de terror
    public void TriggerFlicker(float totalDuration, int numberOfFlickers)
    {
        StartCoroutine(FlickerRoutine(totalDuration, numberOfFlickers));
    }

    private IEnumerator FlickerRoutine(float duration, int flickers)
    {
        float interval = duration / (flickers * 2);
        
        for (int i = 0; i < flickers; i++)
        {
            ApplyLightState(darkBrightness); // Apaga
            yield return new WaitForSeconds(interval);
            ApplyLightState(0f); // Enciende
            yield return new WaitForSeconds(interval);
        }
        
        // Devuelve la luz a su estado original después del parpadeo
        ApplyLightState(isRoomLightOn ? 0f : darkBrightness);
    }

    private void ApplyLightState(float brightnessValue)
    {
        if (passthroughLayer != null)
        {
            // Ajusta el brillo directamente en la cámara del jugador
            passthroughLayer.colorMapEditorBrightness = brightnessValue;
            
            // Si oscurecemos, también reducimos el contraste para evitar ruidos de color extraños
            passthroughLayer.colorMapEditorContrast = brightnessValue < 0 ? -0.3f : 0f;
        }
    }
}