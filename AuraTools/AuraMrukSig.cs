using System;
using System.Reflection;
using System.Text;

/// <summary>Firmas exactas de HasLabel y tipos de PlaneRect/VolumeBounds.</summary>
public static class AuraMrukSig
{
    public static string Execute()
    {
        var log = new StringBuilder();
        Type tipo = null;
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length && tipo == null; i++)
        {
            tipo = asms[i].GetType("Meta.XR.MRUtilityKit.MRUKAnchor", false);
        }

        if (tipo == null)
        {
            return "[S] no encuentro MRUKAnchor";
        }

        var metodos = tipo.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        for (int i = 0; i < metodos.Length; i++)
        {
            if (metodos[i].Name != "HasLabel" && metodos[i].Name != "HasAnyLabel")
            {
                continue;
            }

            var pars = metodos[i].GetParameters();
            var nombres = new string[pars.Length];
            for (int p = 0; p < pars.Length; p++)
            {
                nombres[p] = pars[p].ParameterType.Name + " " + pars[p].Name;
            }

            log.AppendLine("[S] " + metodos[i].Name + "(" + string.Join(", ", nombres) + ")");
        }

        var props = tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        for (int i = 0; i < props.Length; i++)
        {
            if (props[i].Name == "PlaneRect" || props[i].Name == "VolumeBounds" || props[i].Name == "Label")
            {
                log.AppendLine("[S] prop " + props[i].Name + " : " + props[i].PropertyType.FullName);
            }
        }

        return log.ToString();
    }
}
