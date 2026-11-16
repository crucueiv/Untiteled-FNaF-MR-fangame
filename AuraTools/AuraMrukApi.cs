using System;
using System.Linq;
using System.Reflection;
using System.Text;

/// <summary>Lista la API disponible en MRUKAnchor para quitar los avisos de obsoleto.</summary>
public static class AuraMrukApi
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
            return "[A] no encuentro MRUKAnchor";
        }

        log.AppendLine("[A] tipo=" + tipo.FullName);
        var miembros = tipo.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        for (int i = 0; i < miembros.Length; i++)
        {
            string n = miembros[i].Name;
            if (n.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("PlaneRect", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("VolumeBounds", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Center", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Size", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                log.AppendLine("[A] " + miembros[i].MemberType + " " + n);
            }
        }

        var anidados = tipo.GetNestedTypes(BindingFlags.Public);
        for (int i = 0; i < anidados.Length; i++)
        {
            if (!anidados[i].IsEnum)
            {
                continue;
            }

            log.AppendLine("[A] enum " + anidados[i].Name + " = "
                + string.Join(", ", Enum.GetNames(anidados[i]).Take(12)) + " ...");
        }

        return log.ToString();
    }
}
