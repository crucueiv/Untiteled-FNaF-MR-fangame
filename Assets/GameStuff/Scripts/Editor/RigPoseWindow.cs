using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ventana para posar cualquier personaje con esqueleto sin entrar en Play.
///
/// Detecta el rig a partir de la seleccion (o de un Transform elegido a mano), lista sus huesos y
/// deja girar el hueso seleccionado con tres sliders. Al girar un hueso se mueve toda la cadena que
/// cuelga de el (skinning normal de Unity), igual que en Play.
///
/// La pose se guarda donde vive siempre: en los huesos de la escena o del prefab. Para no perderla
/// al mover la figura, el componente SpringBonnieStaticPose tiene que estar pausado: esta ventana lo
/// pausa mientras la tengas abierta y lo devuelve a su sitio al cerrarla.
/// </summary>
public sealed class RigPoseWindow : EditorWindow
{
    private Transform raiz;
    private SpringBonnieStaticPose poseComponente;

    private string filtro = string.Empty;
    private bool soloHuesosRig = true;
    private bool pausarAutoAplicar = true;
    private Transform huesoSeleccionado;

    private Vector2 scroll;
    private readonly List<Transform> huesos = new List<Transform>(256);

    [MenuItem("FNaF Tools/Rig Pose Editor")]
    private static void Abrir()
    {
        var ventana = GetWindow<RigPoseWindow>("Rig Pose");
        ventana.minSize = new Vector2(380f, 460f);
        ventana.DetectarDesdeSeleccion();
        ventana.Show();
    }

    private void OnEnable()
    {
        DetectarDesdeSeleccion();
    }

    private void OnDisable()
    {
        // Al cerrar la ventana se devuelve el control a la resolucion automatica de piernas.
        if (poseComponente != null && pausarAutoAplicar)
        {
            poseComponente.pausarAutoAplicar = false;
            EditorUtility.SetDirty(poseComponente);
        }
    }

    private void OnSelectionChange()
    {
        if (raiz == null)
        {
            DetectarDesdeSeleccion();
        }

        Repaint();
    }

    // ------------------------------------------------------------------ deteccion del rig

    /// <summary>Busca hacia arriba el primer objeto que tenga un SkinnedMeshRenderer (o un SpringBonnieStaticPose).</summary>
    private void DetectarDesdeSeleccion()
    {
        var origen = Selection.activeGameObject;
        if (origen == null)
        {
            return;
        }

        var actual = origen.transform;
        for (int i = 0; i < 8 && actual != null; i++)
        {
            if (actual.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                || actual.GetComponent<SpringBonnieStaticPose>() != null)
            {
                raiz = actual;
                SincronizarComponente();
                RecolectarHuesos();
                return;
            }

            actual = actual.parent;
        }
    }

    private void SincronizarComponente()
    {
        poseComponente = raiz != null ? raiz.GetComponent<SpringBonnieStaticPose>() : null;

        if (poseComponente != null)
        {
            pausarAutoAplicar = poseComponente.pausarAutoAplicar;
        }
    }

    /// <summary>Rellena la lista de huesos segun los filtros actuales.</summary>
    private void RecolectarHuesos()
    {
        huesos.Clear();
        if (raiz == null)
        {
            return;
        }

        var todos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            var hueso = todos[i];
            if (hueso == raiz)
            {
                continue;
            }

            if (soloHuesosRig && !PareceHueso(hueso.name))
            {
                continue;
            }

            if (filtro.Length > 0 && hueso.name.IndexOf(filtro, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            huesos.Add(hueso);
        }
    }

    /// <summary>Descarta los nodos que no son huesos (mallas, targets de IK, ayudas).</summary>
    private static bool PareceHueso(string nombre)
    {
        return nombre.StartsWith("bip_")
            || nombre.StartsWith("Jiggle_")
            || nombre.EndsWith("_jnt")
            || nombre.EndsWith("_jnt_end")
            || nombre == "C_World_jnt";
    }

    // ------------------------------------------------------------------ GUI

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Personaje a posar", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        raiz = (Transform)EditorGUILayout.ObjectField("Raiz", raiz, typeof(Transform), true);
        if (EditorGUI.EndChangeCheck())
        {
            SincronizarComponente();
            RecolectarHuesos();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Detectar desde la seleccion"))
            {
                DetectarDesdeSeleccion();
                Repaint();
            }

            using (new EditorGUI.DisabledScope(raiz == null))
            {
                if (GUILayout.Button("Refrescar huesos"))
                {
                    RecolectarHuesos();
                    Repaint();
                }
            }
        }

        if (raiz == null)
        {
            EditorGUILayout.HelpBox(
                "Selecciona en la jerarquia el objeto del personaje (el que tiene el SkinnedMeshRenderer o el SpringBonnieStaticPose).",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        DibujarControlesPose();

        EditorGUILayout.Space();
        DibujarListaHuesos();

        if (huesoSeleccionado != null)
        {
            DibujarHuesoSeleccionado();
        }
    }

    private void DibujarControlesPose()
    {
        EditorGUILayout.LabelField("Pose", EditorStyles.boldLabel);

        if (poseComponente != null)
        {
            EditorGUI.BeginChangeCheck();
            pausarAutoAplicar = EditorGUILayout.ToggleLeft(
                "Pausar la resolucion automatica mientras edito", pausarAutoAplicar);
            if (EditorGUI.EndChangeCheck())
            {
                poseComponente.pausarAutoAplicar = pausarAutoAplicar;
                EditorUtility.SetDirty(poseComponente);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(poseComponente == null))
            {
                if (GUILayout.Button("Piernas al suelo"))
                {
                    poseComponente.PiernasAlSuelo();
                    MarcarEscenaSucio();
                    RefrescarVista();
                }
            }

            if (GUILayout.Button("Medir plantas"))
            {
                if (poseComponente != null)
                {
                    poseComponente.MedirPlanta();
                }
            }
        }

        EditorGUILayout.HelpBox(
            "La pose vive en los huesos: lo que muevas aqui se guarda con la escena o el prefab. "
            + "Solo las piernas se recolocan solas cuando mueves la figura.",
            MessageType.None);
    }

    private void DibujarListaHuesos()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            filtro = EditorGUILayout.TextField("Filtrar", filtro);

            bool antes = soloHuesosRig;
            soloHuesosRig = EditorGUILayout.ToggleLeft("Solo huesos", soloHuesosRig, GUILayout.Width(100f));
            if (antes != soloHuesosRig)
            {
                RecolectarHuesos();
            }
        }

        EditorGUILayout.LabelField("Huesos (" + huesos.Count + ")", EditorStyles.boldLabel);

        float alto = Mathf.Max(140f, position.height - 330f);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(alto));

        for (int i = 0; i < huesos.Count; i++)
        {
            var hueso = huesos[i];
            if (hueso == null)
            {
                continue;
            }

            bool seleccionado = hueso == huesoSeleccionado;
            GUI.backgroundColor = seleccionado ? new Color(0.35f, 0.7f, 1f) : Color.white;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(Mathf.Min(80f, 12f * Profundidad(hueso)));

                if (GUILayout.Button(hueso.name, EditorStyles.miniButton))
                {
                    huesoSeleccionado = hueso;
                    Selection.activeTransform = hueso;
                    GUI.FocusControl(null);
                }
            }

            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DibujarHuesoSeleccionado()
    {
        EditorGUILayout.LabelField("Hueso: " + huesoSeleccionado.name, EditorStyles.boldLabel);

        var euler = Normalizar(huesoSeleccionado.localEulerAngles);
        var nuevo = new Vector3(
            EditorGUILayout.Slider("X", euler.x, -180f, 180f),
            EditorGUILayout.Slider("Y", euler.y, -180f, 180f),
            EditorGUILayout.Slider("Z", euler.z, -180f, 180f));

        if (nuevo != euler)
        {
            Undo.RecordObject(huesoSeleccionado, "Pose " + huesoSeleccionado.name);
            huesoSeleccionado.localRotation = Quaternion.Euler(nuevo);
            MarcarEscenaSucio();
            RefrescarVista();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Resetear este hueso"))
            {
                Undo.RecordObject(huesoSeleccionado, "Reset hueso");
                huesoSeleccionado.localRotation = Quaternion.identity;
                MarcarEscenaSucio();
                RefrescarVista();
            }

            if (poseComponente != null && GUILayout.Button("Medir plantas"))
            {
                poseComponente.MedirPlanta();
            }
        }
    }

    // ------------------------------------------------------------------ utilidades

    private void MarcarEscenaSucio()
    {
        if (raiz == null)
        {
            return;
        }

        var escena = raiz.gameObject.scene;
        if (escena.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(escena);
        }
        else
        {
            var etapa = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (etapa != null)
            {
                EditorSceneManager.MarkSceneDirty(etapa.scene);
            }
        }
    }

    private static void RefrescarVista()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private static Vector3 Normalizar(Vector3 euler)
    {
        return new Vector3(
            Mathf.DeltaAngle(0f, euler.x),
            Mathf.DeltaAngle(0f, euler.y),
            Mathf.DeltaAngle(0f, euler.z));
    }

    private static int Profundidad(Transform hueso)
    {
        int nivel = 0;
        var actual = hueso.parent;
        while (actual != null)
        {
            nivel++;
            actual = actual.parent;
        }

        return nivel;
    }
}
