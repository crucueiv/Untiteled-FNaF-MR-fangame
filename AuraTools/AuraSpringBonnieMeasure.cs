using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Paso 6: firma de HasLabel/AnchorLabels de MRUK y medidas del modelo de Springbonnie.
/// Instancia el modelo, mide y lo destruye (no modifica nada).
/// </summary>
public static class AuraSpringBonnieMeasure
{
    private const string FbxPath = "Assets/GameStuff/Models/DriveRigged/SpringBonnieVR/HWSpringBonnieIMPROVED.fbx";

    public static string Execute()
    {
        var log = new StringBuilder();

        Type ancla = null;
        var todas = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < todas.Length && ancla == null; i++)
        {
            ancla = todas[i].GetType("Meta.XR.MRUtilityKit.MRUKAnchor", false);
        }

        if (ancla != null)
        {
            var metodos = ancla.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            for (int i = 0; i < metodos.Length; i++)
            {
                if (metodos[i].Name != "HasLabel" && metodos[i].Name != "HasAnyLabel")
                {
                    continue;
                }

                var par = metodos[i].GetParameters();
                var nombres = new StringBuilder();
                for (int p = 0; p < par.Length; p++)
                {
                    nombres.Append(par[p].ParameterType.Name).Append(' ').Append(par[p].Name).Append(';');
                }

                log.AppendLine("[G] " + metodos[i].Name + "(" + nombres + ")");
            }

            var prop = ancla.GetProperty("AnchorLabels");
            if (prop != null && prop.PropertyType.IsGenericType)
            {
                log.AppendLine("[G] AnchorLabels=" + prop.PropertyType.GetGenericArguments()[0].Name);
            }
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null)
        {
            log.AppendLine("[G] NO EXISTE " + FbxPath);
            return log.ToString();
        }

        var temp = (GameObject)UnityEngine.Object.Instantiate(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        var smrs = temp.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < smrs.Length; i++)
        {
            if (smrs[i].name != "Shell" && smrs[i].name != "Shell.003" && smrs[i].name != "Endo")
            {
                continue;
            }

            var b = smrs[i].bounds;
            log.AppendLine("[G] bounds " + smrs[i].name + " centro=" + b.center.ToString("0.###") + " tam=" + b.size.ToString("0.###"));
        }

        var huesos = new[]
        {
            "bip_pelvis", "bip_spine_0", "bip_neck", "bip_head",
            "bip_hip_l", "bip_knee_l", "bip_foot_l", "bip_hip_r", "bip_knee_r", "bip_foot_r",
            "bip_upperArm_l", "bip_lowerArm_l", "bip_hand_l",
            "bip_upperArm_r", "bip_lowerArm_r", "bip_hand_r"
        };

        var mapa = new Transform[huesos.Length];
        var todos = temp.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < huesos.Length; i++)
        {
            for (int k = 0; k < todos.Length; k++)
            {
                if (todos[k].name == huesos[i])
                {
                    mapa[i] = todos[k];
                    break;
                }
            }

            if (mapa[i] == null)
            {
                log.AppendLine("[G] FALTA hueso " + huesos[i]);
                continue;
            }

            log.AppendLine("[G] " + huesos[i] + " pos=" + mapa[i].position.ToString("0.###")
                + " rot=" + mapa[i].rotation.eulerAngles.ToString("0.#")
                + " escalaGlobal=" + mapa[i].lossyScale.x.ToString("0.###"));
        }

        if (mapa[4] != null && mapa[7] != null)
        {
            var derecha = (mapa[7].position - mapa[4].position).normalized;
            var adelante = Vector3.Cross(derecha, Vector3.up).normalized;
            log.AppendLine("[G] derecha=" + derecha.ToString("0.###") + " adelante=" + adelante.ToString("0.###")
                + " yaw=" + Quaternion.LookRotation(adelante, Vector3.up).eulerAngles.y.ToString("0.#"));
        }

        UnityEngine.Object.DestroyImmediate(temp);
        log.AppendLine("[G] modelo temporal destruido");
        return log.ToString();
    }
}
