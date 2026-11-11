using UnityEngine;

/// <summary>
/// Hace que el animatrónico "siga" al jugador sin moverse del sitio: gira el cuerpo
/// (yaw limitado y suavizado) y coloca el LookAtTarget del rig en la cabeza del jugador
/// para que la cabeza (MultiAim del cuello/cabeza) y los ojos se claven en él.
/// Se ejecuta en Update con orden -50: después de ProceduralIdle (-100) y antes del rig.
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class FnafPlayerTracker : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Ancla de la cabeza del jugador (CenterEyeAnchor del OVR rig)")]
    public Transform playerHead;
    public Transform lookAtTarget;
    public Transform torsoRotateTarget;

    [Header("Seguimiento")]
    [Tooltip("Grados máximos que el cuerpo puede girar desde su orientación de reposo")]
    public float bodyYawLimit = 70f;
    [Tooltip("Fracción del giro total que hace el cuerpo (el resto lo hacen cuello y ojos)")]
    [Range(0f, 1f)] public float bodyShare = 0.75f;
    [Tooltip("Cuánto se mezcla el target del torso hacia el jugador")]
    [Range(0f, 1f)] public float torsoFollow = 0.35f;
    [Tooltip("Velocidad de respuesta del giro")]
    public float responseSpeed = 3.5f;
    [Tooltip("Altura añadida al punto al que miran (negativo = mirar un poco por debajo de los ojos)")]
    public float lookHeightOffset = -0.06f;

    private float restLocalYaw;
    private Vector3 torsoRestLocalOffset;
    private bool warnedMissingPlayer;

    private void Awake()
    {
        restLocalYaw = transform.localEulerAngles.y;

        if (torsoRotateTarget != null)
            torsoRestLocalOffset = transform.InverseTransformPoint(torsoRotateTarget.position);

        ResolvePlayerHead();
    }

    private void Start()
    {
        Debug.Log($"[FnafPlayerTracker] {name} activo. playerHead={(playerHead != null ? playerHead.name : "NULL")} " +
                  $"lookAtTarget={(lookAtTarget != null ? lookAtTarget.name : "NULL")} yawLimit={bodyYawLimit}", this);
    }

    private void Update()
    {
        ResolvePlayerHead();
        if (playerHead == null)
            return;

        float blend = 1f - Mathf.Exp(-responseSpeed * Time.deltaTime);

        // --- Cuerpo: gira hacia el jugador con límite y suavizado ---
        Vector3 toPlayer = playerHead.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            float desiredYaw = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
            // la orientación de reposo del personaje miraba a -Z (rotada 180 en escena)
            float restWorldYaw = transform.parent != null
                ? transform.parent.eulerAngles.y + restLocalYaw
                : restLocalYaw;
            float delta = Mathf.DeltaAngle(restWorldYaw, desiredYaw) * bodyShare;
            delta = Mathf.Clamp(delta, -bodyYawLimit, bodyYawLimit);

            Quaternion targetLocal = Quaternion.Euler(0f, restLocalYaw + delta, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocal, blend);
        }

        // --- Cabeza y ojos: el LookAtTarget se coloca en la cabeza del jugador ---
        if (lookAtTarget != null)
            lookAtTarget.position = playerHead.position + Vector3.up * lookHeightOffset;

        // --- Torso: mezcla entre su reposo y el jugador para acompañar el giro ---
        if (torsoRotateTarget != null)
        {
            Vector3 torsoRestWorld = transform.TransformPoint(torsoRestLocalOffset);
            torsoRotateTarget.position = Vector3.Lerp(torsoRestWorld, playerHead.position, torsoFollow);
        }
    }

    private void ResolvePlayerHead()
    {
        if (playerHead != null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerHead = mainCamera.transform;
            return;
        }

        GameObject centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            playerHead = centerEye.transform;
            return;
        }

        if (!warnedMissingPlayer)
        {
            warnedMissingPlayer = true;
            Debug.LogError($"[FnafPlayerTracker] {name}: no se encontró la cabeza del jugador (CenterEyeAnchor).", this);
        }
    }
}
