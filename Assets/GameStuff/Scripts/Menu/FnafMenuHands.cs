using UnityEngine;

/// <summary>
/// Orienta las manos de un animatrónico del menú.
///
/// El rig de Animation Rigging solo posiciona la mano con TwoBoneIK: la rotación de la muñeca
/// viene del clip del Animator (Pose=1) y por eso salía girada de forma extraña. Este script
/// escribe el hueso Hand_* en LateUpdate, que corre después de la fase de animación (donde el
/// Animator y el RigBuilder evalúan), así que su resultado es el que se ve.
///
/// La dirección de los dedos se mide en el mundo (bip_middle_1 - Hand), sin asumir ejes locales
/// ni las unidades del rig (el esqueleto vive bajo un root escalado ~0.02).
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class FnafMenuHands : MonoBehaviour
{
    [Header("Manos")]
    public Transform leftHand;
    public Transform leftMiddleFinger;
    public Transform rightHand;
    public Transform rightMiddleFinger;

    [Header("Dirección")]
    [Tooltip("Si está activo, la mano apunta a aimTarget en vez de colgar hacia abajo")]
    public bool leftAimsAtTarget;
    [Tooltip("Si está activo, la mano apunta a aimTarget en vez de colgar hacia abajo")]
    public bool rightAimsAtTarget;
    [Tooltip("Punto al que apuntan las manos marcadas con 'Aims At Target' (cabeza del jugador)")]
    public Transform aimTarget;
    [Tooltip("Cuánto se aplica la orientación calculada (1 = dedos exactamente en la dirección objetivo)")]
    [Range(0f, 1f)] public float strength = 1f;

    private bool ready;

    private void Start()
    {
        ready = leftHand != null || rightHand != null;

        if (leftHand == null) Debug.LogWarning($"[FnafMenuHands] {name}: falta 'leftHand'.", this);
        if (rightHand == null) Debug.LogWarning($"[FnafMenuHands] {name}: falta 'rightHand'.", this);

        Debug.Log($"[FnafMenuHands] {name}: izquierda={DescribeHand(leftHand, leftMiddleFinger, leftAimsAtTarget)} " +
                  $"derecha={DescribeHand(rightHand, rightMiddleFinger, rightAimsAtTarget)}", this);
    }

    private void LateUpdate()
    {
        if (!ready)
            return;

        ApplyHand(leftHand, leftMiddleFinger, leftAimsAtTarget);
        ApplyHand(rightHand, rightMiddleFinger, rightAimsAtTarget);
    }

    private void ApplyHand(Transform hand, Transform middleFinger, bool aimsAtTarget)
    {
        if (hand == null || middleFinger == null)
            return;

        Vector3 fingerDirection = middleFinger.position - hand.position;
        if (fingerDirection.sqrMagnitude < 0.0000001f)
            return;

        Vector3 desiredDirection = Vector3.down;
        if (aimsAtTarget && aimTarget != null)
        {
            desiredDirection = aimTarget.position - hand.position;
            if (desiredDirection.sqrMagnitude < 0.0000001f)
                return;
        }

        fingerDirection.Normalize();
        desiredDirection.Normalize();

        Quaternion targetRotation = Quaternion.FromToRotation(fingerDirection, desiredDirection) * hand.rotation;
        hand.rotation = strength >= 0.999f
            ? targetRotation
            : Quaternion.Slerp(hand.rotation, targetRotation, strength);
    }

    private string DescribeHand(Transform hand, Transform middleFinger, bool aimsAtTarget)
    {
        if (hand == null || middleFinger == null)
            return "SIN HUESOS";

        Vector3 fingerDirection = (middleFinger.position - hand.position);
        if (fingerDirection.sqrMagnitude < 0.0000001f)
            return "dirección degenerada";

        string mode = aimsAtTarget && aimTarget != null ? "-> jugador" : "-> abajo";
        return $"{hand.name} dedos={fingerDirection.normalized.ToString("F2")} {mode}";
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        DrawHandGizmo(leftHand, leftMiddleFinger, leftAimsAtTarget);
        DrawHandGizmo(rightHand, rightMiddleFinger, rightAimsAtTarget);
    }

    private void DrawHandGizmo(Transform hand, Transform middleFinger, bool aimsAtTarget)
    {
        if (hand == null || middleFinger == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(hand.position, middleFinger.position);

        Vector3 target = aimsAtTarget && aimTarget != null
            ? (aimTarget.position - hand.position).normalized
            : Vector3.down;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(hand.position, hand.position + target * 0.25f);
    }
#endif
}
