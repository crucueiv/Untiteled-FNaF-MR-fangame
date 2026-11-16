using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inspeccion del menu VR: vuelca la estructura de "Menu World Canvas" para poder
/// duplicar la pantalla principal con el mismo estilo. Solo lee.
/// </summary>
public static class AuraMenuInspect
{
    private const string MenuScenePath = "Assets/GameStuff/TestScenes/TestMenu.unity";

    public static void Execute()
    {
        var log = new StringBuilder();
        var escena = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject canvas = null;
            var raices = escena.GetRootGameObjects();
            for (int i = 0; i < raices.Length && canvas == null; i++)
            {
                canvas = Buscar(raices[i].transform, "Menu World Canvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[AuraMenu] no encuentro Menu World Canvas");
                return;
            }

            var canvasComp = canvas.GetComponent<Canvas>();
            log.AppendLine("[AuraMenu] canvas pos=" + canvas.transform.localPosition.ToString("F3")
                + " escala=" + canvas.transform.localScale.ToString("F5")
                + " orden=" + (canvasComp != null ? canvasComp.sortingOrder : -1));

            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var hijo = canvas.transform.GetChild(i);
                log.AppendLine("[AuraMenu] hijo[" + i + "]=" + hijo.name + " activo=" + hijo.gameObject.activeSelf);
            }

            var principal = Buscar(canvas.transform, "Main Menu Screen");
            if (principal == null)
            {
                Debug.LogError("[AuraMenu] no encuentro Main Menu Screen");
                return;
            }

            log.AppendLine("[AuraMenu] ---- Main Menu Screen ----");
            Volcar(principal.transform, 0, log);

            var fondo = Buscar(canvas.transform, "Transition Fade");
            if (fondo != null)
            {
                log.AppendLine("[AuraMenu] Transition Fade pos=" + fondo.transform.localPosition.ToString("F3")
                    + " escala=" + fondo.transform.localScale.ToString("F3"));
            }
        }
        finally
        {
            if (escena.IsValid())
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        Debug.Log(log.ToString());
    }

    private static void Volcar(Transform t, int nivel, StringBuilder log)
    {
        string sangria = new string(' ', nivel * 2);
        var tmp = t.GetComponent<TMPro.TextMeshProUGUI>();
        var boton = t.GetComponent<Button>();
        var grupo = t.GetComponent<CanvasGroup>();
        var rect = t.GetComponent<RectTransform>();

        var sb = new StringBuilder();
        sb.Append(sangria).Append(t.name);
        if (rect != null)
        {
            sb.Append(" [rect ").Append(rect.sizeDelta.ToString("F0")).Append(" pos ").Append(rect.anchoredPosition.ToString("F0")).Append("]");
        }

        if (tmp != null) sb.Append(" TEXTO=\"").Append(tmp.text).Append("\" size=").Append(tmp.fontSize);
        if (boton != null) sb.Append(" BOTON persistentes=").Append(boton.onClick.GetPersistentEventCount());
        if (grupo != null) sb.Append(" CanvasGroup");
        if (t.GetComponent<Image>() != null) sb.Append(" Image");
        if (t.GetComponent<BoxCollider>() != null) sb.Append(" BoxCollider");
        if (t.gameObject.layer != 0) sb.Append(" layer=").Append(t.gameObject.layer);
        if (!t.gameObject.activeSelf) sb.Append(" (inactivo)");

        log.AppendLine(sb.ToString());

        for (int i = 0; i < t.childCount; i++)
        {
            Volcar(t.GetChild(i), nivel + 1, log);
        }
    }

    private static GameObject Buscar(Transform raiz, string nombre)
    {
        var todos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre)
            {
                return todos[i].gameObject;
            }
        }

        return null;
    }
}
