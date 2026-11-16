using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Paso 4 del plan: materiales, prefabs de mano, enganche al rig y render de comprobacion.
/// Script de automatizacion: vive en AuraTools/ (fuera de Assets) para que Unity no lo compile.
/// Se ejecuta desde el editor con execute_script; es idempotente (borra y reconstruye los prefabs).
/// </summary>
public static class AuraHandsBuild
{
    private const string FbxPath = "Assets/GameStuff/Models/Player Hands/RIG_Right_PlayerHands_10k.fbx";
    private const string MatDir = "Assets/GameStuff/Materials";
    private const string RightMatPath = MatDir + "/Hands_FNaF_Right.mat";
    private const string LeftMatPath = MatDir + "/Hands_FNaF_Left.mat";
    private const string PrefabDir = "Assets/GameStuff/Prefabs/Hands";
    private const string RightPrefabPath = PrefabDir + "/PlayerHand_Right.prefab";
    private const string LeftPrefabPath = PrefabDir + "/PlayerHand_Left.prefab";
    private const string RigPrefabPath = "Assets/GameStuff/Prefabs/MRThings.prefab";
    private const string MenuScenePath = "Assets/GameStuff/TestScenes/TestMenu.unity";

    // Largo de referencia de una mano adulta: muneca -> punta del corazon.
    private const float LargoManoMetros = 0.19f;

    // Por encima de la UI del canvas (queue 3000) para que las manos queden delante del panel.
    private const int ColaManos = 4000;

    // El ancla del mando cae en el centro del mango; la muneca queda 9 cm detras.
    private static readonly Vector3 OffsetMuneca = new Vector3(0f, 0f, -0.09f);

    public static void Execute()
    {
        var log = new StringBuilder();
        log.AppendLine("[AuraHands] ===== inicio =====");

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx == null)
        {
            Debug.LogError("[AuraHands] no existe " + FbxPath);
            return;
        }

        // ---- 0. Textura y material de origen del FBX
        Texture textura = null;
        var smrOrigen = fbx.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smrOrigen != null && smrOrigen.sharedMaterial != null)
        {
            var mOrigen = smrOrigen.sharedMaterial;
            log.AppendLine("[AuraHands] material origen=" + mOrigen.name + " shader=" + mOrigen.shader.name + " queue=" + mOrigen.renderQueue);
            if (mOrigen.HasProperty("_BaseMap")) textura = mOrigen.GetTexture("_BaseMap");
            if (mOrigen.HasProperty("_BaseColor")) log.AppendLine("[AuraHands] color origen=" + mOrigen.GetColor("_BaseColor"));
            if (textura != null) log.AppendLine("[AuraHands] textura=" + AssetDatabase.GetAssetPath(textura));
            else log.AppendLine("[AuraHands] AVISO: el material de origen no tiene _BaseMap");
        }
        else
        {
            log.AppendLine("[AuraHands] AVISO: el FBX no tiene SkinnedMeshRenderer con material");
        }

        CrearCarpetas(log);

        var matDerecha = CrearMaterialMano(RightMatPath, textura, 2f, log); // Cull Back
        var matIzquierda = CrearMaterialMano(LeftMatPath, textura, 1f, log); // Cull Front (escala -X)

        if (matDerecha == null || matIzquierda == null)
        {
            Debug.LogError("[AuraHands] no se pudieron crear los materiales");
            return;
        }

        var prefabDerecha = ConstruirManoDerecha(matDerecha, log);
        if (prefabDerecha == null)
        {
            Debug.LogError("[AuraHands] no se pudo construir el prefab de la mano derecha");
            return;
        }

        var prefabIzquierda = ConstruirManoIzquierda(prefabDerecha, matIzquierda, log);

        EngancharEnRigPrefab(prefabDerecha, prefabIzquierda, log);
        EngancharEnEscenaMenu(prefabDerecha, prefabIzquierda, log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ---- Render de comprobacion (sin visor): mano cerrada vista en 3/4 y de perfil
        var instDer = RenderComprobacion(prefabDerecha, "hand_right", false, log);
        var instIzq = RenderComprobacion(prefabIzquierda, "hand_left", true, log);

        if (instDer != null) Object.DestroyImmediate(instDer);
        if (instIzq != null) Object.DestroyImmediate(instIzq);

        log.AppendLine("[AuraHands] ===== fin =====");
        Debug.Log(log.ToString());
    }

    // ------------------------------------------------------------------ materiales

    private static void CrearCarpetas(StringBuilder log)
    {
        if (!AssetDatabase.IsValidFolder("Assets/GameStuff/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/GameStuff", "Materials");
        }

        if (!AssetDatabase.IsValidFolder("Assets/GameStuff/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/GameStuff", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            AssetDatabase.CreateFolder("Assets/GameStuff/Prefabs", "Hands");
            log.AppendLine("[AuraHands] creada carpeta " + PrefabDir);
        }
    }

    /// <summary>
    /// Material de mano: URP/Lit en modo Transparente (alpha blending, ZWrite Off),
    /// con emision para que la mano se vea tambien en la sala a oscuras, y cola 4000
    /// para dibujarse por delante del panel del menu.
    /// </summary>
    private static Material CrearMaterialMano(string ruta, Texture textura, float cull, StringBuilder log)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[AuraHands] no encuentro Universal Render Pipeline/Lit");
            return null;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = mat == null;
        if (nuevo)
        {
            mat = new Material(shader);
        }
        else
        {
            mat.shader = shader;
            mat.shaderKeywords = new string[0];
        }

        if (textura != null)
        {
            mat.SetTexture("_BaseMap", textura);
            mat.SetTexture("_EmissionMap", textura);
        }

        // Color semitransparente frio, como el fantasma de Help Wanted.
        mat.SetColor("_BaseColor", new Color(0.86f, 0.93f, 1f, 0.62f));
        mat.SetFloat("_Smoothness", 0.3f);
        mat.SetFloat("_Metallic", 0f);

        // Superficie transparente: mismos valores que escribe el Inspector de URP.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", cull);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.SetOverrideTag("RenderType", "Transparent");

        // Brillo propio: en la sala a oscuras la mano tiene que seguir viendose.
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.16f, 0.30f, 0.45f, 1f));
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        // Una mano transparente no debe proyectar sombra.
        mat.SetShaderPassEnabled("ShadowCaster", false);

        // La cola se fija AL FINAL: los cambios de propiedad anteriores la devuelven a 3000.
        mat.renderQueue = ColaManos;

        if (nuevo)
        {
            AssetDatabase.CreateAsset(mat, ruta);
        }

        EditorUtility.SetDirty(mat);
        log.AppendLine("[AuraHands] material=" + ruta + " cull=" + cull + " queue=" + mat.renderQueue);
        return mat;
    }

    // ------------------------------------------------------------------ prefabs

    private static GameObject ConstruirManoDerecha(Material mat, StringBuilder log)
    {
        var raiz = new GameObject("PlayerHand_Right");

        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath), raiz.transform);
        instancia.name = "Model";
        // Se desvincula del FBX para que el prefab sea autonomo.
        PrefabUtility.UnpackPrefabInstance(instancia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // +X del modelo (direccion de los dedos) -> +Z del ancla (adelante);
        // -Y del modelo (palma) -> -Y del ancla (palma hacia abajo, agarre del mando).
        instancia.transform.localPosition = Vector3.zero;
        instancia.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        instancia.transform.localScale = Vector3.one;

        PrepararModelo(instancia, mat, true, log);

        var pose = raiz.AddComponent<FnafHandPose>();
        pose.lado = FnafHandPose.LadoMano.Derecha;
        pose.sufijoHuesos = "_r_jnt";
        pose.invertirFlexion = false;
        pose.invertirPulgar = false;
        pose.Inicializar();

        var asset = PrefabUtility.SaveAsPrefabAsset(raiz, RightPrefabPath);
        Object.DestroyImmediate(raiz);
        log.AppendLine("[AuraHands] prefab=" + RightPrefabPath + (asset != null ? " OK" : " FALLO"));
        return asset;
    }

    private static GameObject ConstruirManoIzquierda(GameObject prefabDerecha, Material mat, StringBuilder log)
    {
        var raiz = (GameObject)PrefabUtility.InstantiatePrefab(prefabDerecha);
        raiz.name = "PlayerHand_Left";
        raiz.transform.position = Vector3.zero;
        raiz.transform.rotation = Quaternion.identity;
        raiz.transform.localScale = Vector3.one;
        PrefabUtility.UnpackPrefabInstance(raiz, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // Misma malla espejada en X (como se hacia con las manos holograficas).
        raiz.transform.localScale = new Vector3(-1f, 1f, 1f);

        var modelo = BuscarHijo(raiz.transform, "Model");
        if (modelo == null)
        {
            Debug.LogError("[AuraHands] la mano derecha no tiene hijo Model");
            Object.DestroyImmediate(raiz);
            return null;
        }

        PrepararModelo(modelo.gameObject, mat, false, log);

        var pose = raiz.GetComponent<FnafHandPose>();
        if (pose != null)
        {
            pose.lado = FnafHandPose.LadoMano.Izquierda;
            // En la copia espejada el eje de flexion calculado sale invertido (el signo del
            // producto vectorial cambia con la reflexion), asi que hay que negarlo.
            pose.invertirFlexion = true;
            pose.invertirPulgar = true;
            pose.Inicializar();
        }

        var asset = PrefabUtility.SaveAsPrefabAsset(raiz, LeftPrefabPath);
        Object.DestroyImmediate(raiz);
        log.AppendLine("[AuraHands] prefab=" + LeftPrefabPath + (asset != null ? " OK" : " FALLO"));
        return asset;
    }

    /// <summary>Ajusta material, sombras y (si toca) escala y posicion de la muneca.</summary>
    private static void PrepararModelo(GameObject modelo, Material mat, bool ajustarGeometria, StringBuilder log)
    {
        var animadores = modelo.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animadores.Length; i++)
        {
            log.AppendLine("[AuraHands] Animator eliminado en " + animadores[i].name + " (sobrescribiria la pose de dedos)");
            Object.DestroyImmediate(animadores[i]);
        }

        var smr = modelo.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr != null)
        {
            smr.sharedMaterial = mat;
            smr.shadowCastingMode = ShadowCastingMode.Off;
            smr.receiveShadows = false;
            // Los bounds del FBX son diminutos: sin esto la malla se descarta al posarla.
            smr.updateWhenOffscreen = true;
            log.AppendLine("[AuraHands] SMR verts=" + (smr.sharedMesh != null ? smr.sharedMesh.vertexCount : 0));
        }

        if (!ajustarGeometria)
        {
            return;
        }

        // Se parte de una transformacion limpia para poder medir en el espacio del prefab.
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;
        modelo.transform.localScale = Vector3.one;

        Transform muneca = BuscarHijo(modelo.transform, "hand_r_jnt");
        Transform indiceBase = BuscarHijo(modelo.transform, "index_01_r_jnt");
        Transform meniqueBase = BuscarHijo(modelo.transform, "pinky_01_r_jnt");
        Transform punta = BuscarHijo(modelo.transform, "middle_03_r_jnt_end");
        if (punta == null)
        {
            punta = BuscarHijo(modelo.transform, "index_03_r_jnt_end");
        }

        if (muneca == null || punta == null || indiceBase == null || meniqueBase == null)
        {
            log.AppendLine("[AuraHands] AVISO: faltan huesos para medir o alinear la mano");
            return;
        }

        // 1) Escala: el FBX viene en unidades raras (el nodo .ao tiene escala 100).
        float largo = Vector3.Distance(muneca.position, punta.position);
        if (largo > 1e-5f)
        {
            float factor = LargoManoMetros / largo;
            modelo.transform.localScale = Vector3.one * factor;
            log.AppendLine("[AuraHands] largo medido(m)=" + largo.ToString("F4") + " escala=" + factor.ToString("F4"));
        }

        // 2) Orientacion: se lleva la direccion de los dedos al +Z de la raiz y la palma al -Y,
        //    que es la pose con la que se agarra el mando. Se deduce de los huesos, sin numeros fijos.
        Vector3 dedos = (punta.position - muneca.position).normalized;
        Vector3 ancho = meniqueBase.position - indiceBase.position;
        ancho -= dedos * Vector3.Dot(ancho, dedos);
        ancho.Normalize();
        Vector3 palma = Vector3.Cross(ancho, dedos).normalized;

        Matrix4x4 baseOrigen = Matrix4x4.identity;
        baseOrigen.SetColumn(0, dedos);
        baseOrigen.SetColumn(1, ancho);
        baseOrigen.SetColumn(2, palma);

        Matrix4x4 baseDestino = Matrix4x4.identity;
        baseDestino.SetColumn(0, Vector3.forward); // dedos -> +Z
        baseDestino.SetColumn(1, Vector3.right);   // indice -> menique -> +X
        baseDestino.SetColumn(2, Vector3.down);    // palma -> -Y

        modelo.transform.localRotation = (baseDestino * baseOrigen.transpose).rotation;
        log.AppendLine("[AuraHands] dedos(modelo)=" + dedos.ToString("F3") + " palma=" + palma.ToString("F3")
            + " rotacion=" + modelo.transform.localRotation.eulerAngles.ToString("F1"));

        // 3) La muneca queda en el origen de la raiz del prefab.
        Vector3 origenRaiz = modelo.transform.parent != null ? modelo.transform.parent.position : Vector3.zero;
        Vector3 desplazamiento = muneca.position - origenRaiz;
        modelo.transform.position -= desplazamiento;

        log.AppendLine("[AuraHands] desplazamiento muneca=" + desplazamiento.ToString("F4")
            + " alto final(m)=" + Vector3.Distance(muneca.position, punta.position).ToString("F4"));
    }

    // ------------------------------------------------------------------ rig

    private static void EngancharEnRigPrefab(GameObject prefabDerecha, GameObject prefabIzquierda, StringBuilder log)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath) == null)
        {
            log.AppendLine("[AuraHands] AVISO: no existe " + RigPrefabPath);
            return;
        }

        var contenido = PrefabUtility.LoadPrefabContents(RigPrefabPath);
        try
        {
            var rig = BuscarComponente<OVRCameraRig>(contenido);
            if (rig == null)
            {
                log.AppendLine("[AuraHands] AVISO: el prefab del rig no tiene OVRCameraRig");
                return;
            }

            EngancharManos(rig.transform, prefabDerecha, prefabIzquierda, log, "MRThings.prefab");
            PrefabUtility.SaveAsPrefabAsset(contenido, RigPrefabPath);
            log.AppendLine("[AuraHands] guardado " + RigPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contenido);
        }
    }

    private static void EngancharEnEscenaMenu(GameObject prefabDerecha, GameObject prefabIzquierda, StringBuilder log)
    {
        if (!System.IO.File.Exists(MenuScenePath))
        {
            log.AppendLine("[AuraHands] AVISO: no existe " + MenuScenePath);
            return;
        }

        var escena = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);
        try
        {
            OVRCameraRig rig = null;
            var raices = escena.GetRootGameObjects();
            for (int i = 0; i < raices.Length && rig == null; i++)
            {
                rig = BuscarComponente<OVRCameraRig>(raices[i]);
            }

            if (rig == null)
            {
                log.AppendLine("[AuraHands] AVISO: TestMenu no tiene OVRCameraRig");
                return;
            }

            if (PrefabUtility.GetOutermostPrefabInstanceRoot(rig.gameObject) != null)
            {
                log.AppendLine("[AuraHands] TestMenu: el rig es una instancia de prefab; hereda los cambios de " + RigPrefabPath);
                return;
            }

            EngancharManos(rig.transform, prefabDerecha, prefabIzquierda, log, "TestMenu.unity");
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            log.AppendLine("[AuraHands] guardada " + MenuScenePath + " (rig propio de la escena)");
        }
        finally
        {
            if (escena.IsValid())
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }

    /// <summary>
    /// Cuelga las dos manos de las anclas del mando y coloca PlayerHandsVR en el objeto Player.
    /// </summary>
    private static void EngancharManos(Transform rig, GameObject prefabDerecha, GameObject prefabIzquierda, StringBuilder log, string etiqueta)
    {
        Transform tracking = rig.Find("TrackingSpace");
        if (tracking == null)
        {
            tracking = BuscarHijo(rig, "TrackingSpace");
        }

        if (tracking == null)
        {
            log.AppendLine("[AuraHands] AVISO (" + etiqueta + "): no encuentro TrackingSpace");
            return;
        }

        var anclaDerecha = BuscarHijo(tracking, "RightControllerAnchor");
        var anclaIzquierda = BuscarHijo(tracking, "LeftControllerAnchor");

        var manoDerecha = ColocarMano(anclaDerecha, prefabDerecha, "PlayerHand_Right", Vector3.one, log, etiqueta);
        var manoIzquierda = ColocarMano(anclaIzquierda, prefabIzquierda, "PlayerHand_Left", new Vector3(-1f, 1f, 1f), log, etiqueta);

        if (manoDerecha == null && manoIzquierda == null)
        {
            log.AppendLine("[AuraHands] AVISO (" + etiqueta + "): no se coloco ninguna mano");
            return;
        }

        GameObject objetivo = rig.gameObject;
        if (rig.parent != null && rig.parent.name == "Player")
        {
            objetivo = rig.parent.gameObject;
        }

        var hands = objetivo.GetComponent<PlayerHandsVR>();
        if (hands == null)
        {
            hands = objetivo.AddComponent<PlayerHandsVR>();
        }

        hands.usarManosFNaF = false;
        hands.raizManoDerecha = manoDerecha != null ? manoDerecha.transform : null;
        hands.raizManoIzquierda = manoIzquierda != null ? manoIzquierda.transform : null;
        hands.poseDerecha = manoDerecha != null ? manoDerecha.GetComponent<FnafHandPose>() : null;
        hands.poseIzquierda = manoIzquierda != null ? manoIzquierda.GetComponent<FnafHandPose>() : null;
        hands.offsetPosicionDerecha = OffsetMuneca;
        hands.offsetPosicionIzquierda = OffsetMuneca;

        var visualDerecho = BuscarHijo(tracking, "[BuildingBlock] Controller Tracking Right");
        var visualIzquierdo = BuscarHijo(tracking, "[BuildingBlock] Controller Tracking Left");
        hands.visualMandoDerecho = visualDerecho != null ? visualDerecho.gameObject : null;
        hands.visualMandoIzquierdo = visualIzquierdo != null ? visualIzquierdo.gameObject : null;

        log.AppendLine("[AuraHands] " + etiqueta + ": PlayerHandsVR en " + objetivo.name
            + " | visualMandoDerecho=" + (visualDerecho != null ? visualDerecho.name : "null")
            + " | visualMandoIzquierdo=" + (visualIzquierdo != null ? visualIzquierdo.name : "null"));
    }

    private static GameObject ColocarMano(Transform ancla, GameObject prefab, string nombre, Vector3 escala, StringBuilder log, string etiqueta)
    {
        if (ancla == null)
        {
            log.AppendLine("[AuraHands] AVISO (" + etiqueta + "): falta el ancla para " + nombre);
            return null;
        }

        var previa = ancla.Find(nombre);
        if (previa != null)
        {
            Object.DestroyImmediate(previa.gameObject);
        }

        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ancla);
        instancia.name = nombre;
        instancia.transform.localPosition = OffsetMuneca;
        instancia.transform.localRotation = Quaternion.identity;
        instancia.transform.localScale = escala;
        instancia.SetActive(false); // el interruptor esta OFF por defecto

        log.AppendLine("[AuraHands] " + etiqueta + ": " + nombre + " colgada de " + ancla.name);
        return instancia;
    }

    // ------------------------------------------------------------------ comprobacion visual

    /// <summary>
    /// Instancia el prefab, cierra la mano y la renderiza a PNG para poder revisarla sin visor.
    /// Devuelve la instancia temporal para que el llamador la destruya.
    /// </summary>
    private static GameObject RenderComprobacion(GameObject prefab, string nombre, bool espejada, StringBuilder log)
    {
        if (prefab == null)
        {
            return null;
        }

        var temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        temp.transform.position = new Vector3(0f, 2000f, 0f);
        temp.transform.rotation = Quaternion.identity;
        if (espejada)
        {
            temp.transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        var pose = temp.GetComponent<FnafHandPose>();
        if (pose != null)
        {
            pose.Inicializar();
            log.AppendLine("[AuraHands] " + nombre + ": dedos encontrados=" + pose.DedosEncontrados());
            pose.ForzarPose(1f, 0f, 1f); // puno cerrado y pulgar recogido
        }

        var luzGo = new GameObject("__AuraHandLight");
        var luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.4f;
        luz.color = Color.white;
        luzGo.transform.rotation = Quaternion.Euler(30f, 160f, 0f);

        // Encuadre automatico a partir de la posicion de los huesos (los bounds del FBX son diminutos).
        var huesos = temp.GetComponentsInChildren<Transform>(true);
        Bounds caja = new Bounds(temp.transform.position, Vector3.zero);
        for (int i = 0; i < huesos.Length; i++)
        {
            caja.Encapsulate(huesos[i].position);
        }

        caja.Expand(0.04f);
        Vector3 centro = caja.center;
        float radio = Mathf.Max(caja.extents.magnitude, 0.06f);
        float distancia = radio / Mathf.Sin(40f * Mathf.Deg2Rad * 0.5f) * 1.05f;
        log.AppendLine("[AuraHands] bounds " + nombre + " centro=" + centro.ToString("F3") + " tam=" + caja.size.ToString("F3"));

        int pintados = 0;
        pintados += RenderVista(centro, centro + new Vector3(0.8f, 0.55f, -0.9f).normalized * distancia, "AuraTools/" + nombre + "_a.png");
        pintados += RenderVista(centro, centro + new Vector3(1f, 0.05f, 0f).normalized * distancia, "AuraTools/" + nombre + "_b.png");
        pintados += RenderVista(centro, centro + new Vector3(-0.12f, 1f, 0.3f).normalized * distancia, "AuraTools/" + nombre + "_c.png");

        Object.DestroyImmediate(luzGo);
        log.AppendLine("[AuraHands] render " + nombre + ": pixeles no-fondo total=" + pintados);
        return temp;
    }

    private static int RenderVista(Vector3 objetivo, Vector3 posicionCamara, string ruta)
    {
        const int Lado = 640;

        var camGo = new GameObject("__AuraHandCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 1f);
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.005f;
        cam.farClipPlane = 20f;
        cam.allowHDR = false;
        cam.transform.position = posicionCamara;
        cam.transform.LookAt(objetivo);

        var rt = new RenderTexture(Lado, Lado, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var previo = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(Lado, Lado, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0f, 0f, Lado, Lado), 0, 0);
        tex.Apply();

        RenderTexture.active = previo;

        var pixeles = tex.GetPixels32();
        int noFondo = 0;
        for (int i = 0; i < pixeles.Length; i++)
        {
            Color32 p = pixeles[i];
            if (p.r > 26 || p.g > 26 || p.b > 30)
            {
                noFondo++;
            }
        }

        System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());

        Object.DestroyImmediate(tex);
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);

        return noFondo;
    }

    // ------------------------------------------------------------------ utilidades

    private static T BuscarComponente<T>(GameObject raiz) where T : Component
    {
        var encontrados = raiz.GetComponentsInChildren<T>(true);
        return encontrados.Length > 0 ? encontrados[0] : null;
    }

    private static Transform BuscarHijo(Transform raiz, string nombre)
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
}
