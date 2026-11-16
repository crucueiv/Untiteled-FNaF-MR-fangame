using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Reimporta el shader y comprueba si compila, y aplica los valores al material del bloque.
public static class AuraShaderCheck
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        AssetDatabase.ImportAsset("Assets/GameStuff/Shaders/MR_DirectLightOnly.shader", ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        Shader sh = Shader.Find("Custom/MR_DirectLightOnly");
        if (sh == null)
        {
            sb.AppendLine("[Aura][ERROR] Shader no encontrado");
            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        sb.AppendLine("[Aura] shader isSupported=" + sh.isSupported + " passes=" + sh.passCount);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/GameStuff/Materials/BlockMaterial_DirectLight.mat");
        if (mat == null)
        {
            sb.AppendLine("[Aura][ERROR] Material no encontrado");
            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        mat.SetFloat("_LightWrap", 0.4f);
        mat.SetFloat("_LightContrast", 0.8f);
        mat.SetFloat("_LightBoost", 1.6f);
        mat.SetFloat("_ShadowsOn", 1f);
        mat.SetFloat("_ShadowNormalBias", 0.02f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        sb.AppendLine("[Aura] Material: _BaseColor=" + mat.GetColor("_BaseColor") +
                      " wrap=" + mat.GetFloat("_LightWrap") +
                      " contrast=" + mat.GetFloat("_LightContrast") +
                      " boost=" + mat.GetFloat("_LightBoost") +
                      " shadows=" + mat.GetFloat("_ShadowsOn") +
                      " bias=" + mat.GetFloat("_ShadowNormalBias") +
                      " queue=" + mat.renderQueue);

        sb.AppendLine("[Aura] ShadowCasterEnabled=" + sh.FindPassTagValue(0, new ShaderTagId("LightMode")));

        Debug.Log(sb.ToString());
        return sb.ToString();
    }
}
