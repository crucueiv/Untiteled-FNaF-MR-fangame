using UnityEngine;

/// <summary>
/// Animación procedural para los personajes de DriveCharacterShowcase.
/// Aporta vida al esqueleto sin necesidad de clips: respiración (sube/baja la pelvis),
/// balanceo de cabeza y "foot planting" ajustando los targets de IK al suelo.
///
/// Todo se escribe en Update, ANTES de que Animation Rigging evalúe el rig (que corre entre
/// Update y LateUpdate). Así el rig siempre tiene la última palabra: manos/pies acaban en su
/// target y la cabeza mira al LookAtTarget aunque se gire el torso.
/// Además se restaura la pose de reposo de Spine/Chest/Neck/Head cada frame para que las
/// constraints con peso parcial (cuello 40 %, spine 50 %) no se acumulen frame a frame.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ProceduralIdle : MonoBehaviour
{
    [Header("Referencias del esqueleto")]
    public Transform hips;
    public Transform spine;
    public Transform chest;
    public Transform neck;
    public Transform head;

    [Header("Targets de IK")]
    public Transform leftFootTarget;
    public Transform rightFootTarget;

    [Header("Respiración")]
    public bool enableBreathing = true;
    [Tooltip("Metros que sube y baja la pelvis al respirar")]
    public float breathOffset = 0.012f;
    [Tooltip("Ciclos por segundo")]
    public float breathFrequency = 0.22f;

    [Header("Balanceo de cabeza")]
    public bool enableHeadSway = true;
    public float headSwayAngle = 3f;
    public float headSwayFrequency = 0.11f;

    [Header("Foot planting")]
    public bool enableFootPlanting = true;
    public LayerMask groundMask = ~0;
    [Tooltip("Altura desde la que se lanza el rayo hacia el suelo")]
    public float castHeight = 1.2f;
    [Tooltip("Margen extra por encima de la altura de reposo del tobillo")]
    public float footOffset = 0f;
    [Tooltip("Solo se pega al suelo si el pie está a menos de esta distancia")]
    public float maxSnapDistance = 0.35f;

    private Quaternion spineRest, chestRest, neckRest, headRest;
    private float leftFootRestHeight = float.NaN, rightFootRestHeight = float.NaN;
    private float lastBreath;
    private float phase;
    private bool ready;

    private void Awake()
    {
        spineRest = spine ? spine.localRotation : Quaternion.identity;
        chestRest = chest ? chest.localRotation : Quaternion.identity;
        neckRest = neck ? neck.localRotation : Quaternion.identity;
        headRest = head ? head.localRotation : Quaternion.identity;
        ready = hips != null || neck != null;
        phase = Random.value * 10f;
    }

    private void Start()
    {
        if (hips == null) Debug.LogWarning($"[ProceduralIdle] {name}: falta la referencia 'hips'.", this);
        if (neck == null) Debug.LogWarning($"[ProceduralIdle] {name}: falta la referencia 'neck'.", this);
        if (leftFootTarget == null) Debug.LogWarning($"[ProceduralIdle] {name}: falta 'leftFootTarget'.", this);
        if (rightFootTarget == null) Debug.LogWarning($"[ProceduralIdle] {name}: falta 'rightFootTarget'.", this);

        Debug.Log($"[ProceduralIdle] {name} activo (Update, antes del rig). breathing={enableBreathing} " +
                  $"sway={enableHeadSway} footPlanting={enableFootPlanting} " +
                  $"spine={(spine ? spine.name : "-")} chest={(chest ? chest.name : "-")} head={(head ? head.name : "-")}");
    }

    private void Update()
    {
        if (!ready) return;

        phase += Time.deltaTime;

        // Pose de reposo de los huesos que orienta el rig (evita acumulación de constraints parciales).
        if (spine) spine.localRotation = spineRest;
        if (chest) chest.localRotation = chestRest;
        if (head) head.localRotation = headRest;

        if (neck)
        {
            var sway = Quaternion.identity;
            if (enableHeadSway)
            {
                var yaw = Mathf.Sin(phase * headSwayFrequency * Mathf.PI * 2f) * headSwayAngle;
                var pitch = Mathf.Cos(phase * headSwayFrequency * Mathf.PI * 2f * 1.7f) * headSwayAngle * 0.4f;
                sway = Quaternion.Euler(pitch, yaw, 0f);
            }
            neck.localRotation = neckRest * sway;
        }

        if (enableBreathing && hips)
        {
            // Delta relativo: se puede mover la pelvis/personaje sin que la respiración lo devuelva.
            var breath = Mathf.Sin(phase * breathFrequency * Mathf.PI * 2f) * breathOffset;
            hips.position += Vector3.up * (breath - lastBreath);
            lastBreath = breath;
        }

        if (enableFootPlanting)
        {
            PlantFoot(leftFootTarget, ref leftFootRestHeight);
            PlantFoot(rightFootTarget, ref rightFootRestHeight);
        }
    }

    private void PlantFoot(Transform foot, ref float restHeight)
    {
        if (!foot) return;

        var origin = foot.position + Vector3.up * castHeight;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, castHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            return;

        var delta = foot.position.y - hit.point.y;

        // El target está en el tobillo, no en la suela: se guarda su altura de reposo sobre el
        // suelo la primera vez y se mantiene, en vez de hundirlo hasta el suelo (inalcanzable).
        if (float.IsNaN(restHeight))
        {
            restHeight = delta;
            Debug.Log($"[ProceduralIdle] {name}: {foot.name} altura de reposo sobre el suelo = {restHeight:F3} m", this);
        }

        if (Mathf.Abs(delta - restHeight) > maxSnapDistance) return;

        var p = foot.position;
        p.y = hit.point.y + restHeight + footOffset;
        foot.position = p;
    }
}
