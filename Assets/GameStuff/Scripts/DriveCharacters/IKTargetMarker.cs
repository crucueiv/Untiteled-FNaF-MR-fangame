using UnityEngine;

/// <summary>
/// Dibuja un pequeño handle en la vista de escena para localizar y arrastrar
/// los targets de IK de los personajes.
/// </summary>
[ExecuteAlways]
public class IKTargetMarker : MonoBehaviour
{
    public Color color = Color.cyan;
    public float radius = 0.045f;

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawSphere(transform.position, radius * 0.35f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(color.r, color.g, color.b, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, transform.forward * (radius * 4f));
    }
}
