using UnityEngine;

/// <summary>
/// Hace que un brazo del animatrónico "intente alcanzar" al jugador sin moverse del sitio:
/// empuja el target de la mano hacia la cabeza del jugador cada frame, limitado a la longitud
/// real de la cadena de huesos para que el brazo quede casi recto apuntándole en vez de
/// deformarse por pedir un punto imposible.
///
/// Se ejecuta en Update con orden -45: después de ProceduralIdle (-100), de
/// FnafMenuCharacterPose (-60) y de FnafPlayerTracker (-50), y antes de que el rig evalúe.
/// </summary>
[DefaultExecutionOrder(-45)]
public sealed class FnafMenuReach : MonoBehaviour
{
    [Header("Brazo")]
    public Transform upperArm;
    public Transform lowerArm;
    public Transform hand;
    public Transform handTarget;

    [Header("Objetivo")]
    [Tooltip("Cabeza del jugador (CenterEyeAnchor)")]
    public Transform playerHead;
    [Tooltip("Altura añadida al punto al que se estira el brazo")]
    public float heightOffset = 0f;

    [Header("Alcance")]
    [Tooltip("Fracción de la longitud del brazo que se extiende (1 = recto del todo)")]
    [Range(0.2f, 1f)] public float extension = 0.92f;
    [Tooltip("Velocidad de respuesta del estiramiento")]
    public float responseSpeed = 4f;

    private float chainLength;
    private bool ready;
    private bool warnedMissingHead;
    private float nextLogTime;

    private void Awake()
    {
        ready = upperArm != null && lowerArm != null && hand != null && handTarget != null;

        if (!ready)
        {
            Debug.LogError($"[FnafMenuReach] {name}: faltan referencias (upperArm/lowerArm/hand/handTarget).", this);
            return;
        }

        chainLength = Vector3.Distance(upperArm.position, lowerArm.position) +
                      Vector3.Distance(lowerArm.position, hand.position);
    }

    private void Start()
    {
        ResolvePlayerHead();

        Debug.Log($"[FnafMenuReach] {name}: alcanzando con {upperArm.name}. cadena={chainLength:F2}m " +
                  $"extension={extension:F2} -> alcance={chainLength * extension:F2}m " +
                  $"playerHead={(playerHead != null ? playerHead.name : "NULL")}", this);
    }

    private void Update()
    {
        if (!ready)
            return;

        ResolvePlayerHead();
        if (playerHead == null)
            return;

        Vector3 shoulder = upperArm.position;
        Vector3 aimPoint = playerHead.position + Vector3.up * heightOffset;
        Vector3 direction = aimPoint - shoulder;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        Vector3 desired = shoulder + direction * (chainLength * extension);
        float blend = 1f - Mathf.Exp(-responseSpeed * Time.deltaTime);
        handTarget.position = Vector3.Lerp(handTarget.position, desired, blend);

        if (Time.unscaledTime >= nextLogTime)
        {
            nextLogTime = Time.unscaledTime + 5f;
            Debug.Log($"[FnafMenuReach] {name}: hombro={shoulder.ToString("F2")} " +
                      $"mano={handTarget.position.ToString("F2")} dir={direction.ToString("F2")}", this);
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

        if (!warnedMissingHead)
        {
            warnedMissingHead = true;
            Debug.LogError($"[FnafMenuReach] {name}: no se encontró la cabeza del jugador (CenterEyeAnchor).", this);
        }
    }
}
