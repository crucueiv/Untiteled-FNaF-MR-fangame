using UnityEngine;

/// <summary>
/// Coloca los targets de Animation Rigging de un animatrónico para conseguir una pose
/// imponente (hombros adelantados, manos bajas y cerradas, pies abiertos, torso inclinado).
/// Se aplica una sola vez en Awake: el rig siempre conserva la última palabra.
/// Los desplazamientos se expresan en metros para poder ajustarlos desde el inspector.
/// </summary>
[DefaultExecutionOrder(-60)]
public sealed class FnafMenuCharacterPose : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftElbowHint;
    public Transform rightElbowHint;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform torsoRotateTarget;

    [Header("Animator")]
    [Tooltip("Valor del parámetro entero 'Pose' (0 Neutral, 1 ArmsExtended, 2 WallContact)")]
    public int poseIndex = 1;

    [Header("Pose (metros)")]
    [Tooltip("Cuánto bajan las manos desde su posición de reposo")]
    public float handsDrop = 0.55f;
    [Tooltip("Cuánto se adelantan las manos hacia el jugador")]
    public float handsForward = 0.30f;
    [Tooltip("Cuánto se cierran las manos hacia el eje del cuerpo")]
    public float handsInward = 0.42f;
    [Tooltip("Cuánto bajan los codos (hombros encogidos)")]
    public float elbowsDrop = 0.22f;
    [Tooltip("Cuánto se abren los pies")]
    public float feetSpread = 0.10f;
    [Tooltip("Cuánto se adelanta el torso (inclinarse hacia el jugador)")]
    public float torsoLean = 0.10f;

    private bool applied;

    private void Awake()
    {
        ApplyPose();
    }

    [ContextMenu("Aplicar pose")]
    public void ApplyPose()
    {
        if (applied)
            return;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
            animator.SetInteger("Pose", poseIndex);

        // eje local del personaje (el modelo mira a +Z)
        Vector3 forward = transform.forward;
        Vector3 up = transform.up;
        Vector3 right = transform.right;

        ApplyWorldOffset(leftHandTarget, (right * handsInward) + (-up * handsDrop) + (forward * handsForward));
        ApplyWorldOffset(rightHandTarget, (-right * handsInward) + (-up * handsDrop) + (forward * handsForward));
        ApplyWorldOffset(leftElbowHint, -up * elbowsDrop);
        ApplyWorldOffset(rightElbowHint, -up * elbowsDrop);
        ApplyWorldOffset(leftFootTarget, -right * feetSpread);
        ApplyWorldOffset(rightFootTarget, right * feetSpread);
        ApplyWorldOffset(torsoRotateTarget, forward * torsoLean);

        applied = true;

        Debug.Log($"[FnafMenuCharacterPose] {name}: pose aplicada (pose={poseIndex}, manos -{handsDrop:F2}m, " +
                  $"pies ±{feetSpread:F2}m, torso +{torsoLean:F2}m).", this);
    }

    private static void ApplyWorldOffset(Transform target, Vector3 worldOffset)
    {
        if (target == null || target.parent == null)
            return;

        // localPosition vive en el espacio del padre, así que el desplazamiento se convierte
        // de metros del mundo a unidades locales del padre (el rig está escalado ~0.02).
        target.localPosition += target.parent.InverseTransformVector(worldOffset);
    }
}
