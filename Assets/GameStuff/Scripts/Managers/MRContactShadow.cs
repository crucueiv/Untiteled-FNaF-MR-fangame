using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sombra de contacto blanda bajo objetos virtuales (pies, cadera, cuerpo) apoyados en una superficie
/// real: busca la superficie mas cercana por debajo de cada objetivo en las anclas de MRUK (cama,
/// suelo) y coloca ahi un quad con <c>Custom/MR_ContactBlob</c>.
///
/// Detalles:
///  - Se desvanece con la altura (falloffDistance): pegado a la superficie es opaco, y a partir de
///    esa distancia desaparece. Asi el objeto no lleva una mancha pegada cuando se levanta.
///  - El blob es geometria transparente ocluida por la Depth API: mi brazo real lo tapa.
///  - No usa Physics ni colliders (la escena no tiene ninguno sobre el mundo real) y no toca
///    renderer.material: usa MaterialPropertyBlock para la opacidad.
///  - Coste: 3 quads (6 triangulos) y una lectura de profundidad por pixel. Sin luces ni sombras.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class MRContactShadow : MonoBehaviour
{
    [Header("Objetivos")]
    [Tooltip("Transforms bajo los que se dibuja la sombra (pies, cadera...). Uno por blob.")]
    public Transform[] targets;

    [Header("Aspecto")]
    [Tooltip("Material con el shader Custom/MR_ContactBlob.")]
    public Material blobMaterial;

    [Tooltip("Radio del blob en metros.")]
    [Range(0.02f, 0.6f)]
    public float radius = 0.12f;

    [Tooltip("Opacidad maxima, justo sobre la superficie.")]
    [Range(0f, 1f)]
    public float opacity = 0.55f;

    [Tooltip("Altura (m) a la que la sombra desaparece.")]
    [Range(0.02f, 1f)]
    public float falloffDistance = 0.25f;

    [Tooltip("Separacion de la superficie para que no haya z-fighting (m).")]
    [Range(0f, 0.02f)]
    public float surfaceOffset = 0.002f;

    [Header("Superficies (MRUK)")]
    [Tooltip("Etiquetas de ancla que valen como superficie de contacto.")]
    public MRUKAnchor.SceneLabels labelFilter = MRUKAnchor.SceneLabels.BED | MRUKAnchor.SceneLabels.FLOOR;

    [Tooltip("Log de por que no aparece la sombra (dejarlo apagado en el visor).")]
    public bool logState = false;

    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static Mesh s_quadMesh;

    private Transform[] blobs;
    private MeshRenderer[] blobRenderers;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        CrearBlobs();
    }

    private void OnDisable()
    {
        OcultarTodos();
    }

    private void LateUpdate()
    {
        if (blobs == null || blobs.Length == 0)
        {
            return;
        }

        MRUKRoom room = MRUK.Instance != null ? MRUK.Instance.GetCurrentRoom() : null;
        if (room == null || room.Anchors == null)
        {
            OcultarTodos();
            return;
        }

        var anclas = room.Anchors;

        for (int i = 0; i < blobs.Length; i++)
        {
            Transform objetivo = targets[i];
            if (objetivo == null)
            {
                Activar(i, false);
                continue;
            }

            Vector3 posicion = objetivo.position;
            float mejorDistancia = float.MaxValue;
            Vector3 mejorSuperficie = Vector3.zero;

            for (int a = 0; a < anclas.Count; a++)
            {
                MRUKAnchor ancla = anclas[a];
                if (ancla == null || (ancla.Label & labelFilter) == 0)
                {
                    continue;
                }

                Vector3 superficie;
                float distancia = ancla.GetClosestSurfacePosition(posicion, out superficie);
                if (distancia <= 0.001f || distancia > falloffDistance)
                {
                    continue;
                }

                // La superficie tiene que estar POR DEBAJO y ser sobre todo vertical: asi no se cuela
                // la cara lateral de la cama cuando la cadera esta justo en el borde.
                float caida = posicion.y - superficie.y;
                if (caida <= 0f || caida < distancia * 0.5f)
                {
                    continue;
                }

                if (distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejorSuperficie = superficie;
                }
            }

            if (mejorDistancia == float.MaxValue)
            {
                Activar(i, false);
                continue;
            }

            float desvanecido = 1f - Mathf.Clamp01(mejorDistancia / Mathf.Max(falloffDistance, 0.001f));

            blobs[i].SetPositionAndRotation(mejorSuperficie + Vector3.up * surfaceOffset, Quaternion.identity);
            blobs[i].localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            block.SetFloat(OpacityId, opacity * desvanecido);
            blobRenderers[i].SetPropertyBlock(block);

            Activar(i, true);
        }
    }

    // ------------------------------------------------------------------ construccion

    private void CrearBlobs()
    {
        int cantidad = targets != null ? targets.Length : 0;
        blobs = new Transform[cantidad];
        blobRenderers = new MeshRenderer[cantidad];
        block = new MaterialPropertyBlock();

        if (cantidad == 0)
        {
            Debug.LogWarning("[MRContactShadow] '" + name + "' no tiene objetivos: no se crea ninguna sombra.", this);
            return;
        }

        if (blobMaterial == null)
        {
            Debug.LogWarning("[MRContactShadow] '" + name + "' no tiene material de blob (Custom/MR_ContactBlob).", this);
            return;
        }

        Mesh quad = ObtenerQuad();

        for (int i = 0; i < cantidad; i++)
        {
            var go = new GameObject(name + "_ContactBlob_" + i);
            go.layer = gameObject.layer;
            // Sin padre: el blob vive en coordenadas de mundo y no hereda la escala del personaje.
            go.transform.SetParent(null, false);

            var filtro = go.AddComponent<MeshFilter>();
            filtro.sharedMesh = quad;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = blobMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            blobs[i] = go.transform;
            blobRenderers[i] = renderer;
            Activar(i, false);
        }
    }

    /// <summary>Quad de 1x1 centrado (el mismo que el primitivo Quad de Unity, generado aqui para no depender de Resources).</summary>
    private static Mesh ObtenerQuad()
    {
        if (s_quadMesh != null)
        {
            return s_quadMesh;
        }

        var mesh = new Mesh { name = "MR_ContactBlob_Quad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        s_quadMesh = mesh;
        return s_quadMesh;
    }

    private void Activar(int indice, bool activo)
    {
        if (blobRenderers == null || indice < 0 || indice >= blobRenderers.Length)
        {
            return;
        }

        MeshRenderer renderer = blobRenderers[indice];
        if (renderer == null || renderer.enabled == activo)
        {
            return;
        }

        renderer.enabled = activo;

        if (logState)
        {
            Debug.Log("[MRContactShadow] blob " + indice + " activo = " + activo + " en '" + name + "'.", this);
        }
    }

    private void OcultarTodos()
    {
        if (blobRenderers == null)
        {
            return;
        }

        for (int i = 0; i < blobRenderers.Length; i++)
        {
            Activar(i, false);
        }
    }
}
