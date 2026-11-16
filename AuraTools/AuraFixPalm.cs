using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Corrige los valores serializados de SpringBonnieStaticPose en el prefab y en la
/// instancia de la escena MR: palma hacia arriba (invertirNormalPalma=false) y manos
/// apoyadas sobre el muslo (objetivo alcanzable). Reaplica la pose y guarda todo.
/// </summary>
public static class AuraFixPalm
{
    private const string RutaPrefab = "Assets/GameStuff/Prefabs/Characters/SpringBonnie_Suit.prefab";
    private static readonly Vector3 ObjetivoMano = new Vector3(0.20f, 0.13f, 0.30f);

    public static string Execute()
    {
        var log = new StringBuilder();

        // ---- 1. Prefab: se abre como contenido, se edita y se guarda como asset.
        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        if (raiz == null)
        {
            return "[P] no puedo abrir el prefab " + RutaPrefab;
        }

        var posePrefab = raiz.GetComponent<SpringBonnieStaticPose>();
        if (posePrefab == null)
        {
            PrefabUtility.UnloadPrefabContents(raiz);
            return "[P] el prefab no tiene SpringBonnieStaticPose";
        }

        posePrefab.invertirNormalPalma = false;
        posePrefab.objetivoManoLocal = ObjetivoMano;
        posePrefab.AplicarPose();
        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        PrefabUtility.UnloadPrefabContents(raiz);
        log.AppendLine("[P] prefab actualizado: invertirNormalPalma=false objetivoManoLocal=" + ObjetivoMano.ToString("F2"));

        // ---- 2. Instancia de la escena abierta (por si tenia overrides propios).
        var escena = EditorSceneManager.GetActiveScene();
        var todas = Object.FindObjectsOfType<SpringBonnieStaticPose>(true);
        int tocadas = 0;
        for (int i = 0; i < todas.Length; i++)
        {
            var pose = todas[i];
            if (pose == null || EditorUtility.IsPersistent(pose))
            {
                continue;
            }

            var so = new SerializedObject(pose);
            so.FindProperty("invertirNormalPalma").boolValue = false;
            so.FindProperty("objetivoManoLocal").vector3Value = ObjetivoMano;
            so.ApplyModifiedPropertiesWithoutUndo();
            pose.AplicarPose();
            EditorUtility.SetDirty(pose);
            tocadas++;
            log.AppendLine("[P] instancia en escena: " + pose.gameObject.name);
        }

        log.AppendLine("[P] instancias corregidas=" + tocadas);
        AssetDatabase.SaveAssets();
        if (escena.IsValid() && tocadas > 0)
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            log.AppendLine("[P] escena guardada: " + escena.name);
        }

        return log.ToString();
    }
}
