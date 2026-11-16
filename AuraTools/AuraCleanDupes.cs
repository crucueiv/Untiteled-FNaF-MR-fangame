using System.Collections.Generic;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Borra las instancias sobrantes de SpringBonnie_Suit que quedaron en la raiz de la escena
/// (una por cada ejecucion del script de montaje) y deja solo Springbonnie_MR, que es la que
/// lleva MRSpringBonniePlacement. Guarda la escena al terminar.
/// </summary>
public static class AuraCleanDupes
{
    public static string Execute()
    {
        var log = new StringBuilder();
        var escena = EditorSceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();
        var borrar = new List<GameObject>();

        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "SpringBonnie_Suit")
            {
                borrar.Add(raices[i]);
            }
        }

        log.AppendLine("[L] copias sobrantes encontradas=" + borrar.Count);
        for (int i = 0; i < borrar.Count; i++)
        {
            log.AppendLine("[L] borro " + borrar[i].name + " en " + V(borrar[i].transform.position));
            Object.DestroyImmediate(borrar[i]);
        }

        // Comprobacion: cuantas figuras quedan.
        var todas = Object.FindObjectsOfType<Transform>(true);
        int quedan = 0;
        for (int i = 0; i < todas.Length; i++)
        {
            if (todas[i].name == "Springbonnie_MR")
            {
                quedan++;
                log.AppendLine("[L] queda " + todas[i].name + " pos=" + V(todas[i].position)
                    + " escala=" + V(todas[i].lossyScale));
            }
        }

        log.AppendLine("[L] figuras restantes=" + quedan);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        log.AppendLine("[L] escena guardada");
        return log.ToString();
    }

    private static string V(Vector3 v)
    {
        return "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";
    }
}
