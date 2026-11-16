using System;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Paso 6: iluminacion global de la escena y API disponible de MRUK (anclas de cama).
/// </summary>
public static class AuraAmbientAndMruk
{
    public static string Execute()
    {
        var log = new StringBuilder();

        log.AppendLine("[E] ambientMode=" + RenderSettings.ambientMode
            + " ambientIntensity=" + RenderSettings.ambientIntensity.ToString("0.###")
            + " ambientLight=" + RenderSettings.ambientLight
            + " ambientSky=" + RenderSettings.ambientSkyColor);
        log.AppendLine("[E] fog=" + RenderSettings.fog + " skybox=" + (RenderSettings.skybox != null ? RenderSettings.skybox.name : "null"));

        var luces = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        log.AppendLine("[E] luces en escena=" + luces.Length);
        for (int i = 0; i < luces.Length; i++)
        {
            log.AppendLine("[E] luz " + luces[i].name + " tipo=" + luces[i].type + " intensidad=" + luces[i].intensity
                + " activa=" + luces[i].gameObject.activeInHierarchy);
        }

        // API de MRUK por reflexion (el paquete puede no estar referenciado por este ensamblado dinamico)
        var tipos = new[] { "Meta.XR.MRUtilityKit.MRUKRoom", "Meta.XR.MRUtilityKit.MRUKAnchor", "Meta.XR.MRUtilityKit.MRUK" };
        var todas = AppDomain.CurrentDomain.GetAssemblies();
        for (int t = 0; t < tipos.Length; t++)
        {
            Type tipo = null;
            for (int a = 0; a < todas.Length && tipo == null; a++)
            {
                tipo = todas[a].GetType(tipos[t], false);
            }

            if (tipo == null)
            {
                log.AppendLine("[E] tipo no encontrado: " + tipos[t]);
                continue;
            }

            log.AppendLine("[E] ==== " + tipo.FullName + " ====");
            var props = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int p = 0; p < props.Length; p++)
            {
                log.AppendLine("[E] prop " + props[p].PropertyType.Name + " " + props[p].Name);
            }

            var metodos = tipo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            for (int m = 0; m < metodos.Length; m++)
            {
                log.AppendLine("[E] metodo " + metodos[m].ReturnType.Name + " " + metodos[m].Name);
            }
        }

        return log.ToString();
    }
}
