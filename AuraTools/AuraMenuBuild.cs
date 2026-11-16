using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paso 5 del plan: crea la pantalla "Scene Select Screen" dentro de Menu World Canvas
/// (duplicando Main Menu Screen para heredar el estilo) y la cablea con FnafMenuController.
/// Es idempotente: si la pantalla ya existe, la borra y la vuelve a crear.
/// </summary>
public static class AuraMenuBuild
{
    private const string MenuScenePath = "Assets/GameStuff/TestScenes/TestMenu.unity";
    private const string NombrePantalla = "Scene Select Screen";

    public static void Execute()
    {
        var log = new StringBuilder();
        var escena = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);

        try
        {
            var raices = escena.GetRootGameObjects();
            GameObject canvas = null;
            for (int i = 0; i < raices.Length && canvas == null; i++)
            {
                canvas = Buscar(raices[i].transform, "Menu World Canvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[AuraMenu] no encuentro Menu World Canvas");
                return;
            }

            var principal = Buscar(canvas.transform, "Main Menu Screen");
            if (principal == null)
            {
                Debug.LogError("[AuraMenu] no encuentro Main Menu Screen");
                return;
            }

            FnafMenuController controlador = null;
            for (int i = 0; i < raices.Length && controlador == null; i++)
            {
                controlador = BuscarComponente<FnafMenuController>(raices[i]);
            }

            if (controlador == null)
            {
                Debug.LogError("[AuraMenu] no encuentro FnafMenuController en la escena");
                return;
            }

            // Se borra la version anterior para poder repetir el proceso sin duplicar.
            var anterior = Buscar(canvas.transform, NombrePantalla);
            if (anterior != null)
            {
                Object.DestroyImmediate(anterior);
                log.AppendLine("[AuraMenu] borrada la pantalla anterior");
            }

            var copia = Object.Instantiate(principal, principal.transform.parent);
            copia.name = NombrePantalla;
            copia.transform.SetSiblingIndex(principal.transform.GetSiblingIndex() + 1);
            copia.transform.localPosition = principal.transform.localPosition;
            copia.transform.localRotation = principal.transform.localRotation;
            copia.transform.localScale = principal.transform.localScale;

            var grupo = copia.GetComponent<CanvasGroup>();
            if (grupo != null)
            {
                grupo.alpha = 1f;
                grupo.interactable = true;
                grupo.blocksRaycasts = true;
            }

            CambiarTexto(copia.transform, "Main Menu Heading", "SELECCIONAR ESCENA", 66f, log);
            CambiarTexto(copia.transform, "Menu Input Hint", "ELIGE LA ESCENA EN LA QUE QUIERES JUGAR", 30f, log);

            var botonPruebas = ConfigurarBoton(copia.transform, "Start Game Button", "Test Room Button",
                "SALA DE PRUEBAS (OSCURIDAD)", 40f, log);
            var botonSpring = ConfigurarBoton(copia.transform, "Settings Button", "Springbonnie Room Button",
                "HABITACION MR - SPRINGBONNIE", 40f, log);
            var botonVolver = ConfigurarBoton(copia.transform, "Exit Button", "Scene Select Back Button",
                "VOLVER", 60f, log);

            controlador.sceneSelectPanel = grupo;
            controlador.testRoomButton = botonPruebas;
            controlador.springBonnieButton = botonSpring;
            controlador.sceneSelectBackButton = botonVolver;
            EditorUtility.SetDirty(controlador);

            // Igual que las demas pantallas: se guarda desactivada y el controlador la enciende.
            copia.SetActive(false);

            log.AppendLine("[AuraMenu] creada " + NombrePantalla
                + " | botones=" + (botonPruebas != null) + "," + (botonSpring != null) + "," + (botonVolver != null)
                + " | CanvasGroup=" + (grupo != null));

            SincronizarBuildSettings(log);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            log.AppendLine("[AuraMenu] guardada " + MenuScenePath);
        }
        finally
        {
            if (escena.IsValid())
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log(log.ToString());
    }

    /// <summary>Cambia el texto de un TMP hijo con ese nombre.</summary>
    private static void CambiarTexto(Transform raiz, string nombre, string texto, float tamano, StringBuilder log)
    {
        var objeto = Buscar(raiz, nombre);
        if (objeto == null)
        {
            log.AppendLine("[AuraMenu] AVISO: no encuentro el texto " + nombre);
            return;
        }

        var tmp = objeto.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp == null)
        {
            log.AppendLine("[AuraMenu] AVISO: " + nombre + " no es un texto TMP");
            return;
        }

        tmp.text = texto;
        tmp.fontSize = tamano;
        EditorUtility.SetDirty(tmp);
    }

    /// <summary>Renombra un boton, le cambia la etiqueta y devuelve su Button.</summary>
    private static Button ConfigurarBoton(Transform raiz, string nombre, string nuevoNombre, string etiqueta, float tamano, StringBuilder log)
    {
        var objeto = Buscar(raiz, nombre);
        if (objeto == null)
        {
            log.AppendLine("[AuraMenu] AVISO: no encuentro el boton " + nombre);
            return null;
        }

        objeto.name = nuevoNombre;

        var etiquetaObjeto = Buscar(objeto.transform, "Label");
        if (etiquetaObjeto == null)
        {
            log.AppendLine("[AuraMenu] AVISO: " + nuevoNombre + " no tiene hijo Label");
        }
        else
        {
            var tmp = etiquetaObjeto.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = etiqueta;
                tmp.fontSize = tamano;
                tmp.enableAutoSizing = false;
                EditorUtility.SetDirty(tmp);
            }
        }

        var boton = objeto.GetComponent<Button>();
        if (boton != null)
        {
            // Las llamadas se registran en Awake; se limpia cualquier resto duplicado.
            boton.onClick = new Button.ButtonClickedEvent();
            EditorUtility.SetDirty(boton);
        }

        return boton;
    }

    /// <summary>Deja en Build Settings las escenas del juego en el orden correcto.</summary>
    private static void SincronizarBuildSettings(StringBuilder log)
    {
        string[] deseadas =
        {
            "Assets/GameStuff/TestScenes/TestMenu.unity",
            "Assets/GameStuff/TestScenes/InitialTestRoom.unity",
            "Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity"
        };

        var lista = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        var yaEsta = new System.Collections.Generic.List<string>();

        for (int i = 0; i < deseadas.Length; i++)
        {
            if (!System.IO.File.Exists(deseadas[i]))
            {
                log.AppendLine("[AuraMenu] (aun no existe) " + deseadas[i]);
                continue;
            }

            lista.Add(new EditorBuildSettingsScene(deseadas[i], true));
            yaEsta.Add(deseadas[i]);
        }

        // Se conservan las escenas que ya estuvieran y no esten en la lista.
        var actuales = EditorBuildSettings.scenes;
        for (int i = 0; i < actuales.Length; i++)
        {
            if (yaEsta.Contains(actuales[i].path))
            {
                continue;
            }

            lista.Add(actuales[i]);
            log.AppendLine("[AuraMenu] Build Settings conserva " + actuales[i].path);
        }

        EditorBuildSettings.scenes = lista.ToArray();

        for (int i = 0; i < lista.Count; i++)
        {
            log.AppendLine("[AuraMenu] Build Settings [" + i + "] " + lista[i].path);
        }
    }

    private static T BuscarComponente<T>(GameObject raiz) where T : Component
    {
        var encontrados = raiz.GetComponentsInChildren<T>(true);
        return encontrados.Length > 0 ? encontrados[0] : null;
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
