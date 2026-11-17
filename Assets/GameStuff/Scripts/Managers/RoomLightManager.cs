using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Orquestador de iluminación global para la experiencia de realidad mixta.
/// Modula el brillo/contraste de la capa de passthrough del mundo real y la intensidad de la luz virtual principal de Unity,
/// además de coordinar parpadeos estroboscópicos y emitir el evento estático <see cref="OnRoomLightStateChanged"/>.
/// </summary>
public class RoomLightManager : MonoBehaviour
{
    [Header("Configuración de Passthrough")]
    [Range(-1f, 0f)]
    public float darkBrightness = -0.8f;

    [Header("Configuración de Luz Virtual (Unity)")]
    [Tooltip("Arrastra aquí la Directional Light de tu escena.")]
    public Light mainVirtualLight;
    public float virtualLightOnIntensity = 1f;
    public float virtualLightOffIntensity = 0.05f;

    [Header("Ajustes de Prueba (Inspector)")]
    public float testFlickerDuration = 2f;
    public int testFlickerCount = 5;
    
    private OVRPassthroughLayer passthroughLayer;
    private bool isRoomLightOn = true;

    /// <summary>
    /// Evento estático que notifica a todos los componentes suscriptores cuando el estado de iluminación de la habitación cambia.
    /// Parámetro: true si la luz está encendida, false si está apagada.
    /// </summary>
    public static event Action<bool> OnRoomLightStateChanged;

    void Start()
    {
        passthroughLayer = FindAnyObjectByType<OVRPassthroughLayer>();
        
        if (passthroughLayer == null)
            Debug.LogError("RoomLightManager: No se encontró un OVRPassthroughLayer.");
    }

    /// <summary>
    /// Establece el estado de iluminación de la habitación (mundo real y virtual).
    /// </summary>
    /// <param name="isOn">True para encender, false para apagar y oscurecer el passthrough.</param>
    public void SetRoomLight(bool isOn)
    {
        isRoomLightOn = isOn;
        ApplyLightState(isRoomLightOn ? 0f : darkBrightness, isRoomLightOn);
    }

    /// <summary>
    /// Dispara una rutina de parpadeo que alterna rápidamente entre encendido y apagado.
    /// </summary>
    /// <param name="totalDuration">Duración total del efecto de parpadeo en segundos.</param>
    /// <param name="numberOfFlickers">Cantidad de ciclos de parpadeo.</param>
    public void TriggerFlicker(float totalDuration, int numberOfFlickers)
    {
        StartCoroutine(FlickerRoutine(totalDuration, numberOfFlickers));
    }

    private IEnumerator FlickerRoutine(float duration, int flickers)
    {
        float interval = duration / (flickers * 2);
        
        for (int i = 0; i < flickers; i++)
        {
            ApplyLightState(darkBrightness, false); // Apaga
            yield return new WaitForSeconds(interval);
            ApplyLightState(0f, true);              // Enciende
            yield return new WaitForSeconds(interval);
        }
        
        // Devuelve al estado original
        ApplyLightState(isRoomLightOn ? 0f : darkBrightness, isRoomLightOn);
    }

    private void ApplyLightState(float brightnessValue, bool isVirtualOn)
    {
        // 1. Oscurecer el mundo real (Passthrough)
        if (passthroughLayer != null)
        {
            passthroughLayer.colorMapEditorBrightness = brightnessValue;
            passthroughLayer.colorMapEditorContrast = brightnessValue < 0 ? -0.3f : 0f;
        }

        // 2. Oscurecer el mundo virtual (Unity Light)
        if (mainVirtualLight != null)
        {
            mainVirtualLight.intensity = isVirtualOn ? virtualLightOnIntensity : virtualLightOffIntensity;
        }

        // 3. Avisar a todos los objetos específicos que la luz ha cambiado
        OnRoomLightStateChanged?.Invoke(isVirtualOn);
    }

    // --- CONTROLES DESDE EL INSPECTOR ---

    /// <summary>Acción de prueba desde el menú contextual para apagar la habitación.</summary>
    [ContextMenu("💡 Apagar Habitación")]
    public void TestTurnOff() { SetRoomLight(false); }

    /// <summary>Acción de prueba desde el menú contextual para encender la habitación.</summary>
    [ContextMenu("💡 Encender Habitación")]
    public void TestTurnOn() { SetRoomLight(true); }

    /// <summary>Acción de prueba desde el menú contextual para ejecutar parpadeo.</summary>
    [ContextMenu("⚡ Probar Parpadeo (Flickering)")]
    public void TestFlickerAction()
    {
        if (Application.isPlaying) TriggerFlicker(testFlickerDuration, testFlickerCount);
    }
}