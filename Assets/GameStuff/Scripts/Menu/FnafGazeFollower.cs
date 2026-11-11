using UnityEngine;
using UnityEngine.Serialization;

public sealed class FnafGazeFollower : MonoBehaviour
{
    [FormerlySerializedAs("gazeTarget")]
    public Transform playerTarget;
    public Transform headJoint;
    public Transform[] eyeJoints;
    public bool rotateBodyToPlayer = true;
    public float bodyYawLimit = 42f;
    public float headYawLimit = 10f;
    public float headPitchLimit = 6f;
    public float eyeYawLimit = 22f;
    public float eyePitchLimit = 16f;
    public float responseSpeed = 7f;

    private Quaternion bodyRestRotation;
    private Quaternion headRestRotation;
    private Quaternion[] eyeRestRotations;

    private void Awake()
    {
        bodyRestRotation = transform.localRotation;

        if (headJoint != null)
            headRestRotation = headJoint.localRotation;

        if (eyeJoints == null)
            eyeJoints = new Transform[0];

        eyeRestRotations = new Quaternion[eyeJoints.Length];
        for (int i = 0; i < eyeJoints.Length; i++)
        {
            if (eyeJoints[i] != null)
                eyeRestRotations[i] = eyeJoints[i].localRotation;
        }
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                playerTarget = mainCamera.transform;
        }

        if (playerTarget == null)
            return;

        float blend = 1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime);

        if (rotateBodyToPlayer)
        {
            Vector3 bodyDirection = DirectionInParentSpace(transform, playerTarget.position);
            Quaternion bodyTargetRotation = bodyRestRotation * GazeOffset(bodyDirection, bodyYawLimit, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, bodyTargetRotation, blend);
        }

        if (headJoint != null)
        {
            Vector3 direction = DirectionInParentSpace(headJoint, playerTarget.position);
            Quaternion targetRotation = headRestRotation * GazeOffset(direction, headYawLimit, headPitchLimit);
            headJoint.localRotation = Quaternion.Slerp(headJoint.localRotation, targetRotation, blend);
        }

        for (int i = 0; i < eyeJoints.Length; i++)
        {
            Transform eye = eyeJoints[i];
            if (eye == null)
                continue;

            Vector3 direction = DirectionInParentSpace(eye, playerTarget.position);
            Quaternion targetRotation = eyeRestRotations[i] * GazeOffset(direction, eyeYawLimit, eyePitchLimit);
            eye.localRotation = Quaternion.Slerp(eye.localRotation, targetRotation, blend);
        }
    }

    private static Vector3 DirectionInParentSpace(Transform joint, Vector3 worldTarget)
    {
        Vector3 worldDirection = worldTarget - joint.position;
        return joint.parent != null ? joint.parent.InverseTransformDirection(worldDirection) : worldDirection;
    }

    private static Quaternion GazeOffset(Vector3 direction, float yawLimit, float pitchLimit)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return Quaternion.identity;

        float horizontal = Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z);
        float yaw = Mathf.Clamp(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -yawLimit, yawLimit);
        float pitch = Mathf.Clamp(-Mathf.Atan2(direction.y, horizontal) * Mathf.Rad2Deg, -pitchLimit, pitchLimit);
        return Quaternion.Euler(pitch, yaw, 0f);
    }
}
