using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Paso 6 del plan: monta Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity.
///
///  - Copia InitialTestRoom para heredar rig, passthrough y MRUK tal cual.
///  - Quita la parte de oscuridad (spawner de cubos y material MR_DirectLightOnly).
///  - Anade una luz de sala (la que espera RoomLightManager).
///  - Crea el prefab Springbonnie_Suit (sin mallas del endoesqueleto) con la pose sentada.
///  - Lo coloca al borde de un proxy de cama y le engancha MRSpringBonniePlacement.
///  - Registra la escena en Build Settings y guarda un render de comprobacion.
///
/// Es idempotente: se puede volver a ejecutar; reconstruye el prefab del personaje.
/// </summary>
public static class AuraMRSceneBuild
{
    private const string EscenaBase = "Assets/GameStuff/TestScenes/InitialTestRoom.unity";
    private const string EscenaMR = "Assets/GameStuff/Scenes/MR_SpringBonnieRoom.unity";
    private const string RutaFbx = "Assets/GameStuff/Models/DriveRigged/SpringBonnieVR/HWSpringBonnieIMPROVED.fbx";
    private const string CarpetaPersonajes = "Assets/GameStuff/Prefabs/Characters";
    private const string RutaPrefabPersonaje = CarpetaPersonajes + "/SpringBonnie_Suit.prefab";
    private const string RutaMatBloque = "Assets/GameStuff/Materials/BlockMaterial_Room.mat";
    private const string RutaMatCama = "Assets/GameStuff/Materials/BedProxy.mat";
    private const string RutaRender = "AuraTools/mr_scene_check.png";

    // Medidas de referencia (metros)
    // 1,90 m de estatura: con la cadera a 0,61 m (cama de 0,55 + 0,06) las plantas de los pies
    // quedan justo en el suelo con las rodillas a 90 grados, que es lo que pide la tarea.
    private const float AlturaDePieObjetivo = 1.90f;
    private const float AltoCama = 0.55f;
    private const float AnchoCama = 1.6f;
    private const float LargoCama = 2.0f;

    // Nombre de las mallas del endoesqueleto que se ocultan (se conservan ojos y dientes)
    private static readonly string[] MallasEndo = { "Endo", "Endo.002", "Endo.005", "Endo.027" };

    public static string Execute()
    {
        var log = new StringBuilder();

        AsegurarCarpetas(log);
        if (!File.Exists(EscenaMR))
        {
            AssetDatabase.CopyAsset(EscenaBase, EscenaMR);
            AssetDatabase.Refresh();
            log.AppendLine("[MR] escena copiada de InitialTestRoom");
        }
        else
        {
            log.AppendLine("[MR] la escena ya existia, se reutiliza");
        }

        CrearMateriales(log);

        var escena = EditorSceneManager.OpenScene(EscenaMR, OpenSceneMode.Single);
        log.AppendLine("[MR] escena abierta " + escena.path);

        QuitarOscuridad(log);
        AnadirLuzDeSala(log);

        // ---- Personaje
        float alturaDePie = MedirAlturaDePie(log);
        float escala = 1f;
        if (alturaDePie > 0.1f)
        {
            escala = AlturaDePieObjetivo / alturaDePie;
        }

        log.AppendLine("[MR] altura de pie medida=" + alturaDePie.ToString("0.###") + " m -> escala=" + escala.ToString("0.####"));
        CrearPrefabPersonaje(escala, log);

        // ---- Colocacion en la escena
        ColocarPersonaje(log);
        ColocarProxyCama(log);
        DiagnosticoPersonaje(log);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        log.AppendLine("[MR] escena guardada " + escena.path);

        RegistrarEnBuildSettings(log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RenderDeComprobacion(log);
        return log.ToString();
    }

    // ------------------------------------------------------------------ escena

    private static void AsegurarCarpetas(StringBuilder log)
    {
        if (!AssetDatabase.IsValidFolder("Assets/GameStuff/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets/GameStuff", "Scenes");
            log.AppendLine("[MR] creada carpeta Assets/GameStuff/Scenes");
        }

        if (!AssetDatabase.IsValidFolder(CarpetaPersonajes))
        {
            AssetDatabase.CreateFolder("Assets/GameStuff/Prefabs", "Characters");
            log.AppendLine("[MR] creada carpeta " + CarpetaPersonajes);
        }
    }

    private static void CrearMateriales(StringBuilder log)
    {
        var shaderLit = Shader.Find("Universal Render Pipeline/Lit");

        var matBloque = AssetDatabase.LoadAssetAtPath<Material>(RutaMatBloque);
        if (matBloque == null)
        {
            matBloque = new Material(shaderLit);
            AssetDatabase.CreateAsset(matBloque, RutaMatBloque);
            log.AppendLine("[MR] material creado " + RutaMatBloque);
        }

        matBloque.SetColor("_BaseColor", new Color(0.16f, 0.33f, 0.85f, 1f));
        matBloque.SetFloat("_Smoothness", 0.35f);
        matBloque.SetFloat("_Metallic", 0f);
        matBloque.renderQueue = 2000;
        EditorUtility.SetDirty(matBloque);

        var matCama = AssetDatabase.LoadAssetAtPath<Material>(RutaMatCama);
        if (matCama == null)
        {
            matCama = new Material(shaderLit);
            AssetDatabase.CreateAsset(matCama, RutaMatCama);
            log.AppendLine("[MR] material creado " + RutaMatCama);
        }

        matCama.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.58f, 1f));
        matCama.SetFloat("_Smoothness", 0.1f);
        matCama.SetFloat("_Metallic", 0f);
        matCama.renderQueue = 2000;
        EditorUtility.SetDirty(matCama);
    }

    /// <summary>Quita el spawner de cubos y cambia el material del bloque interactuable.</summary>
    private static void QuitarOscuridad(StringBuilder log)
    {
        var escena = EditorSceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();

        GameObject managers = null;
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "Managers")
            {
                managers = raices[i];
                break;
            }
        }

        if (managers != null)
        {
            var spawner = managers.transform.Find("SpawnerObjectManager");
            if (spawner != null)
            {
                Object.DestroyImmediate(spawner.gameObject);
                log.AppendLine("[MR] quitado Managers/SpawnerObjectManager (generaba cubos con el material de oscuridad)");
            }
        }

        // El bloque interactuable se conserva (hace falta para probar el agarre) con material normal.
        var matBloque = AssetDatabase.LoadAssetAtPath<Material>(RutaMatBloque);
        var bloques = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int cambiados = 0;
        for (int i = 0; i < bloques.Length; i++)
        {
            var mats = bloques[i].sharedMaterials;
            bool tocado = false;
            for (int k = 0; k < mats.Length; k++)
            {
                if (mats[k] != null && mats[k].name == "BlockMaterial_DirectLight")
                {
                    mats[k] = matBloque;
                    tocado = true;
                }
            }

            if (tocado)
            {
                bloques[i].sharedMaterials = mats;
                cambiados++;
            }
        }

        log.AppendLine("[MR] renderers con material de oscuridad cambiados=" + cambiados);
    }

    private static void AnadirLuzDeSala(StringBuilder log)
    {
        var escena = EditorSceneManager.GetActiveScene();
        GameObject luzGo = null;
        var raices = escena.GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "MR Room Light")
            {
                luzGo = raices[i];
                break;
            }
        }

        if (luzGo == null)
        {
            luzGo = new GameObject("MR Room Light");
        }

        var luz = luzGo.GetComponent<Light>();
        if (luz == null)
        {
            luz = luzGo.AddComponent<Light>();
        }

        luz.type = LightType.Directional;
        luz.color = new Color(1f, 0.96f, 0.90f, 1f);
        luz.intensity = 0.9f;
        luz.shadows = LightShadows.Soft;
        luzGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        // RoomLightManager ya esta en la escena y espera una luz principal para atenuar el mundo virtual.
        var managers = Object.FindObjectsByType<RoomLightManager>(FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            var so = new SerializedObject(managers[i]);
            var prop = so.FindProperty("mainVirtualLight");
            if (prop != null)
            {
                prop.objectReferenceValue = luz;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        log.AppendLine("[MR] luz de sala (directional " + luz.intensity.ToString("0.##") + ") enlazada a RoomLightManager");
    }

    // ------------------------------------------------------------------ personaje

    /// <summary>
    /// Estatura del modelo a escala 1, medida con los huesos (la silueta renderizada incluye la
    /// pose de bind y da un valor inflado, por eso no se usa). Del tobillo a la base del craneo
    /// hay aproximadamente el 87 % de la estatura de un humano de proporciones normales.
    /// </summary>
    private static float MedirAlturaDePie(StringBuilder log)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx);
        if (fbx == null)
        {
            log.AppendLine("[MR] ERROR: no existe " + RutaFbx);
            return 0f;
        }

        var temp = (GameObject)Object.Instantiate(fbx);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        // La estatura de pie se mide buscando los huesos directamente en el modelo recien
        // instanciado (sin componentes: la pose los moveria).
        Transform huesoCabeza = BuscarPorNombre(temp.transform, "bip_head");
        Transform huesoTobillo = BuscarPorNombre(temp.transform, "bip_foot_r");
        float cabeza = huesoCabeza != null ? huesoCabeza.position.y : 0f;
        float tobillo = huesoTobillo != null ? huesoTobillo.position.y : 0f;
        float estatura = 0f;
        if (cabeza - tobillo > 0.01f)
        {
            estatura = (cabeza - tobillo) / 0.87f;
            log.AppendLine("[MR] huesos a escala 1: tobillo y=" + tobillo.ToString("0.###")
                + " cabeza y=" + cabeza.ToString("0.###")
                + " -> estatura " + estatura.ToString("0.###") + " m");
        }
        else
        {
            log.AppendLine("[MR] no se han podido medir los huesos del modelo");
        }

        Object.DestroyImmediate(temp);
        return estatura;
    }

    private static Transform BuscarPorNombre(Transform raiz, string nombre)
    {
        var todos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre)
            {
                return todos[i];
            }
        }

        return null;
    }

    /// <summary>Altura en metros de la silueta del objeto visto con una camara ortografica frontal.</summary>
    private static float MedirSilueta(GameObject objetivo, float tamanoOrto, float centroY, StringBuilder log, string etiqueta)
    {
        const int lado = 512;
        var goCam = new GameObject("__AuraCamMedir");
        var cam = goCam.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = tamanoOrto;
        cam.transform.position = new Vector3(0f, centroY, -14f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(1f, 0f, 1f, 1f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 60f;
        cam.allowHDR = false;

        var rt = new RenderTexture(lado, lado, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var previo = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0f, 0f, lado, lado), 0, 0);
        tex.Apply();
        RenderTexture.active = previo;

        var pixeles = tex.GetPixels();
        int minY = lado;
        int maxY = -1;
        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                var c = pixeles[y * lado + x];
                bool magenta = c.r > 0.8f && c.g < 0.25f && c.b > 0.8f;
                if (!magenta)
                {
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        float mpp = (tamanoOrto * 2f) / lado;
        float alto = 0f;
        if (maxY >= 0)
        {
            alto = (maxY - minY + 1) * mpp;
            float yInferior = centroY - tamanoOrto + minY * mpp;
            log.AppendLine("[MR] silueta " + etiqueta + ": alto=" + alto.ToString("0.###")
                + " m | y inferior=" + yInferior.ToString("0.###"));
        }
        else
        {
            log.AppendLine("[MR] silueta " + etiqueta + ": vacia");
        }

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(goCam);
        return alto;
    }

    private static void CrearPrefabPersonaje(float escala, StringBuilder log)
    {
        if (File.Exists(RutaPrefabPersonaje))
        {
            AssetDatabase.DeleteAsset(RutaPrefabPersonaje);
        }

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx);
        var raiz = new GameObject("SpringBonnie_Suit");
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        modelo.name = "Model";
        modelo.transform.SetParent(raiz.transform, false);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;

        // Sin endoesqueleto: se apagan las mallas del esqueleto visibles entre las costuras.
        var smrs = raiz.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < smrs.Length; i++)
        {
            for (int k = 0; k < MallasEndo.Length; k++)
            {
                if (smrs[i].name == MallasEndo[k])
                {
                    smrs[i].enabled = false;
                    log.AppendLine("[MR] malla del endoesqueleto desactivada: " + smrs[i].name);
                    break;
                }
            }
        }

        raiz.transform.localScale = Vector3.one * escala;

        var pose = raiz.AddComponent<SpringBonnieStaticPose>();
        pose.aplicarEnEditor = true;
        pose.ResolverHuesos();
        pose.AplicarPose();

        log.AppendLine("[MR] huesos: pelvis=" + (pose.pelvis != null ? pose.pelvis.name : "-")
            + " caderaDer=" + (pose.caderaDer != null ? pose.caderaDer.name : "-")
            + " manoDer=" + (pose.manoDer != null ? pose.manoDer.name : "-"));

        if (pose.pelvis != null && pose.tobilloDer != null)
        {
            log.AppendLine("[MR] cadera y=" + pose.pelvis.position.y.ToString("0.###")
                + " | tobillo der y=" + pose.tobilloDer.position.y.ToString("0.###")
                + " | cabeza y=" + (pose.cabeza != null ? pose.cabeza.position.y.ToString("0.###") : "-")
                + " | muslo=" + Vector3.Distance(pose.caderaDer.position, pose.rodillaDer.position).ToString("0.###")
                + " | espinilla=" + Vector3.Distance(pose.rodillaDer.position, pose.tobilloDer.position).ToString("0.###"));
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPersonaje);
        Object.DestroyImmediate(raiz);
        log.AppendLine("[MR] prefab guardado " + RutaPrefabPersonaje);
    }

    private static void ColocarPersonaje(StringBuilder log)
    {
        var escena = EditorSceneManager.GetActiveScene();

        // Se borra una colocacion anterior para que el script sea repetible.
        var raices = escena.GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "Springbonnie_MR")
            {
                Object.DestroyImmediate(raices[i]);
                break;
            }
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPersonaje);
        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instancia.name = "Springbonnie_MR";

        var pose = instancia.GetComponent<SpringBonnieStaticPose>();
        if (pose != null)
        {
            pose.ResolverHuesos();
            pose.AplicarPose();
        }

        var proxyCama = GameObject.Find("BedProxy_MR");
        var colocacion = instancia.GetComponent<MRSpringBonniePlacement>();
        if (colocacion == null)
        {
            colocacion = instancia.AddComponent<MRSpringBonniePlacement>();
        }

        colocacion.pose = pose;
        colocacion.cadera = pose != null ? pose.pelvis : null;
        colocacion.proxyCama = proxyCama;
        colocacion.usarAnclaMRUK = true;
        colocacion.etiquetaCama = "BED";
        colocacion.separacionDelBorde = 0.10f;
        colocacion.alturaCaderaSobreCama = 0.06f;
        colocacion.rotacionManualY = 0f;

        log.AppendLine("[MR] personaje colocado en la escena (Springbonnie_MR)");
    }

    private static void ColocarProxyCama(StringBuilder log)
    {
        var escena = EditorSceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();
        GameObject cama = null;
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "BedProxy_MR")
            {
                cama = raices[i];
                break;
            }
        }

        if (cama == null)
        {
            cama = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cama.name = "BedProxy_MR";
            var colisionador = cama.GetComponent<Collider>();
            if (colisionador != null)
            {
                Object.DestroyImmediate(colisionador);
            }
        }

        // Cama centrada en z = -LargoCama/2: el borde que mira al jugador queda en z = 0.
        cama.transform.position = new Vector3(0f, AltoCama * 0.5f, -LargoCama * 0.5f);
        cama.transform.rotation = Quaternion.identity;
        cama.transform.localScale = new Vector3(AnchoCama, AltoCama, LargoCama);

        var renderer = cama.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(RutaMatCama);
        }

        // Punto manual de la cadera: sobre el borde delantero, 10 cm hacia dentro de la cama.
        var colocaciones = Object.FindObjectsByType<MRSpringBonniePlacement>(FindObjectsSortMode.None);
        for (int i = 0; i < colocaciones.Length; i++)
        {
            colocaciones[i].proxyCama = cama;
            colocaciones[i].puntoCaderaManual = new Vector3(0f, AltoCama + colocaciones[i].alturaCaderaSobreCama, -0.10f);
            colocaciones[i].Colocar();
        }

        log.AppendLine("[MR] proxy de cama " + AnchoCama + "x" + AltoCama + "x" + LargoCama + " colocado");
    }

    private static void DiagnosticoPersonaje(StringBuilder log)
    {
        var instancias = Object.FindObjectsByType<SpringBonnieStaticPose>(FindObjectsSortMode.None);
        if (instancias.Length == 0)
        {
            log.AppendLine("[MR] no hay personaje en la escena");
            return;
        }

        var pose = instancias[0];
        float escala = pose.transform.lossyScale.x;
        float tobillo = pose.tobilloDer != null ? pose.tobilloDer.position.y : 0f;
        float cadera = pose.pelvis != null ? pose.pelvis.position.y : 0f;
        float cabeza = pose.cabeza != null ? pose.cabeza.position.y : 0f;
        // A escala 1 el tobillo esta a 0,386 m del suelo, asi que la planta queda esa distancia por debajo.
        float planta = tobillo - 0.386f * escala;

        log.AppendLine("[MR] figura: escala=" + escala.ToString("0.####")
            + " | cadera y=" + cadera.ToString("0.###")
            + " | tobillo y=" + tobillo.ToString("0.###")
            + " | planta aprox y=" + planta.ToString("0.###")
            + " | cabeza y=" + cabeza.ToString("0.###"));
    }

    private static void RegistrarEnBuildSettings(StringBuilder log)
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool existe = false;
        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i].path == EscenaMR)
            {
                existe = true;
                break;
            }
        }

        if (!existe)
        {
            lista.Add(new EditorBuildSettingsScene(EscenaMR, true));
            EditorBuildSettings.scenes = lista.ToArray();
            log.AppendLine("[MR] escena anadida a Build Settings");
        }
        else
        {
            log.AppendLine("[MR] la escena ya estaba en Build Settings");
        }

        var finales = EditorBuildSettings.scenes;
        for (int i = 0; i < finales.Length; i++)
        {
            log.AppendLine("[MR] Build Settings [" + i + "] " + finales[i].path + " activa=" + finales[i].enabled);
        }
    }

    private static void RenderDeComprobacion(StringBuilder log)
    {
        var escena = EditorSceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();
        GameObject rig = null;
        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == "MRThings")
            {
                rig = raices[i];
                break;
            }
        }

        if (rig != null)
        {
            rig.SetActive(false);
        }

        const int lado = 640;
        var goCam = new GameObject("__AuraCamCheck");
        var cam = goCam.AddComponent<Camera>();
        cam.orthographic = false;
        cam.fieldOfView = 55f;
        cam.transform.position = new Vector3(1.35f, 1.35f, -2.2f);
        cam.transform.LookAt(new Vector3(0f, 0.85f, -0.35f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 40f;

        var rt = new RenderTexture(lado, lado, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var previo = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0f, 0f, lado, lado), 0, 0);
        tex.Apply();
        RenderTexture.active = previo;
        File.WriteAllBytes(RutaRender, tex.EncodeToPNG());

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(goCam);

        if (rig != null)
        {
            rig.SetActive(true);
        }

        log.AppendLine("[MR] render de comprobacion: " + RutaRender);
    }
}
