using UnityEngine;

/// <summary>
/// Colocacion de objetos virtuales sobre la superficie real (MRUK).
///
/// La capa de oscuridad (DarknessRevealer) es opaca alli donde no llega luz y escribe profundidad,
/// asi que un objeto que quede unos centimetros por debajo de la malla del cuarto aparece tapado
/// (negro puro y sin contorno). Para evitarlo, el objeto se apoya sobre la superficie detectada
/// dejando un margen configurable.
/// </summary>
public static class MRSurfacePlacement
{
    /// <summary>
    /// Margen por defecto entre la base del objeto y la superficie real.
    /// </summary>
    public const float DefaultClearance = 0.02f;

    /// <summary>
    /// Coloca la base del objeto (segun los bounds de sus renderers) a <paramref name="clearance"/>
    /// metros sobre la superficie que haya justo debajo de <paramref name="origin"/>.
    /// Devuelve true si se coloco y la altura de la superficie golpeada.
    /// </summary>
    public static bool TrySnapToSurfaceBelow(GameObject instance, Vector3 origin, float clearance,
        float maxDistance, out float surfaceHeight)
    {
        surfaceHeight = 0f;

        if (instance == null)
        {
            return false;
        }

        // Los colliders del EffectMesh de MRUK son los que representan el cuarto real.
        RaycastHit hit;
        if (!Physics.Raycast(origin, Vector3.down, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Bounds bounds;
        if (!TryGetBounds(instance, out bounds))
        {
            return false;
        }

        float targetBaseY = hit.point.y + Mathf.Max(0f, clearance);
        Vector3 position = instance.transform.position;
        position.y += targetBaseY - bounds.min.y;
        instance.transform.position = position;

        surfaceHeight = hit.point.y;
        return true;
    }

    /// <summary>Bounds en mundo que envuelven todos los renderers activos del objeto.</summary>
    public static bool TryGetBounds(GameObject instance, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
            {
                continue;
            }

            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
            {
                continue;
            }

            if (!any)
            {
                bounds = r.bounds;
                any = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return any;
    }
}
