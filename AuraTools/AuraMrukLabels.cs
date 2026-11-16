using System;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Paso 6: valores de las etiquetas de MRUK y forma de acceder a la instancia.
/// </summary>
public static class AuraMrukLabels
{
    public static string Execute()
    {
        var log = new StringBuilder();
        Type ancla = null;
        Type mruk = null;
        var todas = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < todas.Length; i++)
        {
            if (ancla == null) ancla = todas[i].GetType("Meta.XR.MRUtilityKit.MRUKAnchor", false);
            if (mruk == null) mruk = todas[i].GetType("Meta.XR.MRUtilityKit.MRUK", false);
        }

        if (ancla != null)
        {
            var tipoEtiquetas = ancla.GetNestedType("SceneLabels", BindingFlags.Public);
            log.AppendLine("[F] SceneLabels enum=" + (tipoEtiquetas != null));
            if (tipoEtiquetas != null)
            {
                var valores = Enum.GetNames(tipoEtiquetas);
                log.AppendLine("[F] labels(" + valores.Length + ")=" + string.Join(",", valores));
            }

            var hasLabel = ancla.GetMethod("HasLabel", new[] { tipoEtiquetas });
            log.AppendLine("[F] HasLabel(SceneLabels)=" + (hasLabel != null));
        }

        if (mruk != null)
        {
            var inst = mruk.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (inst == null)
            {
                var campo = mruk.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                log.AppendLine("[F] MRUK.Instance campo=" + (campo != null) + " tipo=" + (campo != null ? campo.FieldType.Name : "-"));
            }
            else
            {
                log.AppendLine("[F] MRUK.Instance prop=" + inst.PropertyType.Name);
            }

            var cond = mruk.GetMethod("GetCurrentRoom", Type.EmptyTypes);
            log.AppendLine("[F] GetCurrentRoom=" + (cond != null ? cond.ReturnType.Name : "no"));
        }

        var passthrough = UnityEngine.Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsSortMode.None);
        log.AppendLine("[F] OVRPassthroughLayer en escena=" + passthrough.Length);

        return log.ToString();
    }
}
