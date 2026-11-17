using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Centra al jugador frente al menú VR al entrar en TestMenu, una vez montado y siguiendo el HMD.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuPlayerAlignment : MonoBehaviour
{
    [Header("Alineación del jugador")]
    [Tooltip("Activa la alineación automática del rig al iniciar esta escena.")]
    public bool alinearAlInicio = true;

    [Tooltip("Distancia horizontal entre el centro del panel y la cabeza del jugador, en metros.")]
    [Min(0.25f)]
    public float distanciaAlPanel = 1.6f;

    [Tooltip("Desplazamiento lateral respecto al centro del panel, en metros. Positivo = derecha del panel.")]
    public float desplazamientoLateral = 0f;

    [Header("Referencias (se resuelven desde el menú si quedan vacías)")]
    [Tooltip("Centro de los ojos del jugador (CenterEyeAnchor del OVRCameraRig).")]
    public Transform cabeza;

    [Tooltip("Transform raíz del OVRCameraRig que se va a posicionar y rotar.")]
    public Transform rigRaiz;

    [Tooltip("Panel principal del menú, utilizado para calcular el punto frontal.")]
    public Transform panelMenu;

    [Header("Seguimiento")]
    [Tooltip("Tiempo entre comprobaciones mientras el visor todavía no está montado o siguiendo la cabeza.")]
    [Min(0.1f)]
    public float intervaloReintento = 0.5f;

    private Coroutine alineacionPendiente;
    private Vector3 posicionOriginalRig;
    private Quaternion rotacionOriginalRig;
    private bool poseOriginalGuardada;
    private bool alineacionAplicada;

    private void Awake()
    {
        ResolverReferencias();
    }

    private void OnEnable()
    {
        if (alinearAlInicio)
            alineacionPendiente = StartCoroutine(EsperarSeguimientoYAplicar());
    }

    private void OnDisable()
    {
        if (alineacionPendiente != null)
        {
            StopCoroutine(alineacionPendiente);
            alineacionPendiente = null;
        }

        RestaurarRig();
    }

    private IEnumerator EsperarSeguimientoYAplicar()
    {
        ResolverReferencias();

        while (enabled && alinearAlInicio)
        {
            ResolverReferencias();

            if (ReferenciasValidas() && HayPoseDeCabezaValida())
            {
                // Deja que OVRCameraRig publique la pose de este frame antes de medirla.
                yield return null;
                AplicarAlineacion();
                alineacionPendiente = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, intervaloReintento));
        }

        alineacionPendiente = null;
    }

    private void ResolverReferencias()
    {
        FnafMenuInput entrada = GetComponent<FnafMenuInput>();
        FnafMenuController menu = GetComponent<FnafMenuController>();

        if (cabeza == null && entrada != null)
            cabeza = entrada.playerHead;

        if (panelMenu == null && menu != null && menu.mainMenuPanel != null)
            panelMenu = menu.mainMenuPanel.transform;

        if (rigRaiz == null && cabeza != null)
        {
            OVRCameraRig rig = cabeza.GetComponentInParent<OVRCameraRig>();
            if (rig != null)
                rigRaiz = rig.transform;
        }
    }

    private bool ReferenciasValidas()
    {
        return cabeza != null && rigRaiz != null && panelMenu != null;
    }

    private static bool HayPoseDeCabezaValida()
    {
        if (!OVRManager.isHmdPresent || !OVRPlugin.userPresent)
            return false;

        InputDevice dispositivoCabeza = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        return dispositivoCabeza.isValid &&
               dispositivoCabeza.TryGetFeatureValue(CommonUsages.isTracked, out bool siguiendo) &&
               siguiendo;
    }

    private void AplicarAlineacion()
    {
        if (!alinearAlInicio || !ReferenciasValidas() || alineacionAplicada)
            return;

        posicionOriginalRig = rigRaiz.position;
        rotacionOriginalRig = rigRaiz.rotation;
        poseOriginalGuardada = true;

        Vector3 normalPanel = Vector3.ProjectOnPlane(panelMenu.forward, Vector3.up).normalized;
        Vector3 derechaPanel = Vector3.ProjectOnPlane(panelMenu.right, Vector3.up).normalized;
        if (normalPanel.sqrMagnitude < 0.0001f || derechaPanel.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[MenuPlayerAlignment] El panel del menú no tiene una orientación horizontal válida.", this);
            poseOriginalGuardada = false;
            return;
        }

        Vector3 puntoIdealCabeza = panelMenu.position + normalPanel * distanciaAlPanel + derechaPanel * desplazamientoLateral;
        puntoIdealCabeza.y = cabeza.position.y; // Conserva la altura real: el rig sigue en Floor Level.

        Vector3 direccionAlPanel = Vector3.ProjectOnPlane(panelMenu.position - puntoIdealCabeza, Vector3.up).normalized;
        Vector3 forwardCabeza = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
        if (direccionAlPanel.sqrMagnitude < 0.0001f || forwardCabeza.sqrMagnitude < 0.0001f)
        {
            poseOriginalGuardada = false;
            return;
        }

        float giroYaw = Vector3.SignedAngle(forwardCabeza, direccionAlPanel, Vector3.up);
        rigRaiz.rotation = Quaternion.AngleAxis(giroYaw, Vector3.up) * rigRaiz.rotation;

        Vector3 diferencia = puntoIdealCabeza - cabeza.position;
        diferencia.y = 0f;
        rigRaiz.position += diferencia;

        alineacionAplicada = true;
        Debug.Log($"[MenuPlayerAlignment] Rig alineado frente al menú: offset aplicado {diferencia} m, " +
                  $"yaw {giroYaw:0.0}°, distancia {distanciaAlPanel:0.00} m, lateral {desplazamientoLateral:0.00} m. " +
                  "Altura conservada.", this);
    }

    private void RestaurarRig()
    {
        if (!alineacionAplicada || !poseOriginalGuardada || rigRaiz == null)
            return;

        rigRaiz.SetPositionAndRotation(posicionOriginalRig, rotacionOriginalRig);
        alineacionAplicada = false;
        poseOriginalGuardada = false;
    }

    private void OnDrawGizmosSelected()
    {
        Transform panel = panelMenu;
        if (panel == null)
        {
            FnafMenuController menu = GetComponent<FnafMenuController>();
            if (menu != null && menu.mainMenuPanel != null)
                panel = menu.mainMenuPanel.transform;
        }

        if (panel == null)
            return;

        Vector3 normalPanel = Vector3.ProjectOnPlane(panel.forward, Vector3.up).normalized;
        Vector3 derechaPanel = Vector3.ProjectOnPlane(panel.right, Vector3.up).normalized;
        if (normalPanel.sqrMagnitude < 0.0001f || derechaPanel.sqrMagnitude < 0.0001f)
            return;

        Vector3 punto = panel.position + normalPanel * distanciaAlPanel + derechaPanel * desplazamientoLateral;
        if (cabeza != null)
            punto.y = cabeza.position.y;

        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.9f);
        Gizmos.DrawWireSphere(punto, 0.12f);
        Gizmos.DrawLine(panel.position, punto);
    }
}
