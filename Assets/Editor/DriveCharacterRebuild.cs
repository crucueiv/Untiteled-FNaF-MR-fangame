using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye los prefabs de DriveCharacters: materiales/texturas correctas por submesh,
/// escala normalizada, huesos con nombres legibles y un rig de animación procedural
/// (Animation Rigging) con targets de IK arrastrables.
/// </summary>
public static class DriveCharacterRebuild
{
    private const string ModelRoot = "Assets/GameStuff/Models/DriveRigged";
    private const string PrefabRoot = "Assets/GameStuff/Prefabs/DriveCharacters";
    private const string MaterialFolder = "Assets/GameStuff/Prefabs/DriveCharacters/Materials";
    private const string AnimationFolder = "Assets/GameStuff/Animations/DriveCharacters";
    private const string ScenePath = "Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity";
    private const float TargetHeight = 1.8f;

    // Endoesqueleto compartido: manos y piernas de todos los personajes lo usan.
    private const string SharedEndoDiffuse = ModelRoot + "/Endo/endo_d.jpg";
    private const string SharedEndoNormal = ModelRoot + "/Endo/body_n.jpg";
    private static readonly string[] LimbTokens = { "hand", "arm", "leg", "foot", "wrist", "knee", "elbow" };

    // Nombres legibles de huesos
    private const string HipsName = "Hips";
    private const string SpineName = "Spine";
    private const string ChestName = "Chest";
    private const string NeckName = "Neck";
    private const string HeadName = "Head";

    private sealed class TextureOverride
    {
        public string MaterialToken;   // token en el nombre del material de origen o de su textura
        public string TexturePath;     // difusa que debe usarse
        public string NormalPath;      // normal opcional
    }

    private sealed class CharacterDef
    {
        public string Name;
        public string Fbx;
        public string TextureFolder;
        public TextureOverride[] TexOverrides = System.Array.Empty<TextureOverride>();
    }

    private static readonly CharacterDef[] Characters =
    {
        new CharacterDef { Name = "Bonnie",        Fbx = $"{ModelRoot}/Bonnie/HWBonnieis cool.fbx",        TextureFolder = $"{ModelRoot}/Bonnie" },
        new CharacterDef { Name = "Chica",         Fbx = $"{ModelRoot}/Chica/HWChica.fbx",                 TextureFolder = $"{ModelRoot}/Chica" },
        new CharacterDef { Name = "Endo",          Fbx = $"{ModelRoot}/Endo/HWEndo.fbx",                   TextureFolder = $"{ModelRoot}/Endo" },
        new CharacterDef { Name = "Foxy",          Fbx = $"{ModelRoot}/Foxy/HWFoxy.fbx",                   TextureFolder = $"{ModelRoot}/Foxy",
            TexOverrides = new[]
            {
                // La pieza interior del pecho de Foxy viene mapeada como "battery" en el FBX,
                // pero es el endoesqueleto: usa la textura del Endo.
                new TextureOverride
                {
                    MaterialToken = "battery",
                    TexturePath = $"{ModelRoot}/Endo/endo_d.jpg",
                    NormalPath = $"{ModelRoot}/Endo/body_n.jpg"
                }
            } },
        new CharacterDef { Name = "Freddy",        Fbx = $"{ModelRoot}/Freddy/HWFreddy.fbx",               TextureFolder = $"{ModelRoot}/Freddy" },
        new CharacterDef { Name = "GoldenFreddy",  Fbx = $"{ModelRoot}/Freddy/HWFreddy.fbx",               TextureFolder = $"{ModelRoot}/Freddy" },
        new CharacterDef { Name = "WitheredBonnie", Fbx = $"{ModelRoot}/WitheredBonnie/HWwbonnie.fbx",      TextureFolder = $"{ModelRoot}/WitheredBonnie" },
        new CharacterDef { Name = "WitheredChica", Fbx = $"{ModelRoot}/WitheredChica/HWwchica.fbx",        TextureFolder = $"{ModelRoot}/WitheredChica" },
        new CharacterDef { Name = "WitheredFoxy",  Fbx = $"{ModelRoot}/WitheredFoxy/HWwfoxy.fbx",          TextureFolder = $"{ModelRoot}/WitheredFoxy" },
        new CharacterDef { Name = "WitheredFreddy", Fbx = $"{ModelRoot}/WitheredFreddy/HWwfreddy.fbx",      TextureFolder = $"{ModelRoot}/WitheredFreddy" },
    };

    private sealed class BoneSet
    {
        public Transform Pelvis, Spine, Chest, Neck, Head;
        public Transform UpperArmL, LowerArmL, HandL, UpperArmR, LowerArmR, HandR;
        public Transform ThighL, KneeL, FootL, ThighR, KneeR, FootR;

        public bool HasArmsL => UpperArmL && LowerArmL && HandL;
        public bool HasArmsR => UpperArmR && LowerArmR && HandR;
        public bool HasLegsL => ThighL && KneeL && FootL;
        public bool HasLegsR => ThighR && KneeR && FootR;
    }

    private sealed class TargetSet
    {
        public Vector3 LeftHand, RightHand, LeftFoot, RightFoot, LookAt, Torso;
        public Vector3 LeftElbow, RightElbow, LeftKnee, RightKnee;
        public Quaternion LeftHandRot, RightHandRot, LeftFootRot, RightFootRot, TorsoRot;
    }

    // ------------------------------------------------------------------ AUDIT

    [MenuItem("Tools/Drive Characters/1 - Audit")]
    public static void Audit()
    {
        Debug.Log("[DriveRebuild] ===== AUDIT START =====");

        foreach (var def in Characters)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(def.Fbx);
            if (source == null)
            {
                Debug.LogError($"[DriveRebuild] AUDIT {def.Name}: no se pudo cargar {def.Fbx}");
                continue;
            }

            var textures = LoadTextures(def.TextureFolder);
            Debug.Log($"[DriveRebuild] AUDIT {def.Name} | fbx={def.Fbx} | texturas={textures.Length} " +
                      $"({string.Join(", ", textures.Select(t => t.name))})");

            var renderers = source.GetComponentsInChildren<Renderer>(true);
            var union = new Bounds();
            var first = true;
            foreach (var r in renderers)
            {
                if (first) { union = r.bounds; first = false; } else { union.Encapsulate(r.bounds); }

                var slots = r.sharedMaterials;
                for (var i = 0; i < slots.Length; i++)
                {
                    var src = slots[i];
                    var baseTex = GetBaseTexture(src);
                    var normal = PickNormal(textures, baseTex);
                    Debug.Log($"[DriveRebuild]   SLOT renderer={r.name} idx={i} origen={(src == null ? "<null>" : src.name)} " +
                              $"shader={(src == null || src.shader == null ? "<null>" : src.shader.name)} " +
                              $"base={(baseTex == null ? "<NONE>" : baseTex.name)} " +
                              $"normal={(normal == null ? "<none>" : normal.name)}");
                }
            }

            Debug.Log($"[DriveRebuild]   BOUNDS size={union.size} min={union.min} max={union.max} " +
                      $"escalaNormalizada={(union.size.y > 0.001f ? TargetHeight / union.size.y : 0f):F5}");

            var bones = FindBones(source.transform);
            Debug.Log($"[DriveRebuild]   HUESOS Pelvis={(bones.Pelvis ? bones.Pelvis.name : "<none>")} " +
                      $"Spine={(bones.Spine ? bones.Spine.name : "<none>")} Chest={(bones.Chest ? bones.Chest.name : "<none>")} " +
                      $"Neck={(bones.Neck ? bones.Neck.name : "<none>")} Head={(bones.Head ? bones.Head.name : "<none>")}");
            Debug.Log($"[DriveRebuild]   HUESOS BrazoL={bones.HasArmsL} ({N(bones.UpperArmL)}/{N(bones.LowerArmL)}/{N(bones.HandL)}) " +
                      $"BrazoR={bones.HasArmsR} ({N(bones.UpperArmR)}/{N(bones.LowerArmR)}/{N(bones.HandR)}) " +
                      $"PiernaL={bones.HasLegsL} ({N(bones.ThighL)}/{N(bones.KneeL)}/{N(bones.FootL)}) " +
                      $"PiernaR={bones.HasLegsR} ({N(bones.ThighR)}/{N(bones.KneeR)}/{N(bones.FootR)})");
        }

        Debug.Log("[DriveRebuild] ===== AUDIT END =====");
    }

    private static string N(Transform t) => t ? t.name : "<none>";

    // -------------------------------------------------------------- MATERIALS

    [MenuItem("Tools/Drive Characters/2 - Rebuild Materials")]
    public static void RebuildMaterials()
    {
        EnsureFolder(MaterialFolder);

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var missing = 0;

        foreach (var def in Characters)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(def.Fbx);
            if (source == null)
            {
                Debug.LogError($"[DriveRebuild] MATERIALS {def.Name}: no se pudo cargar {def.Fbx}");
                continue;
            }

            var textures = LoadTextures(def.TextureFolder);
            ConfigureNormalMaps(textures);

            foreach (var r in source.GetComponentsInChildren<Renderer>(true))
            {
                var slots = r.sharedMaterials;
                for (var i = 0; i < slots.Length; i++)
                {
                    var src = slots[i];
                    var baseTex = ResolveBaseTexture(def, src, textures);
                    var normal = PickNormal(textures, baseTex);
                    if (TryTextureOverride(def, src, baseTex, out var ovBase, out var ovNormal))
                    {
                        baseTex = ovBase;
                        if (ovNormal != null) normal = ovNormal;
                    }
                    else if (TryLimbEndoTexture(r.name, baseTex, out var limbEndo, out var limbNormal))
                    {
                        baseTex = limbEndo;
                        if (limbNormal != null) normal = limbNormal;
                    }
                    var mat = BuildMaterial(def.Name, r.name, i, baseTex, normal);
                    if (baseTex == null) missing++;

                    var assetName = $"{def.Name}_{Sanitize(r.name)}_{i}";
                    if (!used.Add(assetName))
                    {
                        assetName = $"{assetName}_{used.Count}";
                        used.Add(assetName);
                        Debug.LogWarning($"[DriveRebuild] MATERIALS nombre duplicado, renombrado a {assetName}");
                    }

                    var path = $"{MaterialFolder}/{assetName}.mat";
                    AssetDatabase.CreateAsset(mat, path);
                    Debug.Log($"[DriveRebuild] MAT {path} base={(baseTex == null ? "<none>" : baseTex.name)} " +
                              $"normal={(normal == null ? "<none>" : normal.name)}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[DriveRebuild] MATERIALS listo. Slots sin textura de origen: {missing}");
    }

    private static Material BuildMaterial(string character, string rendererName, int index, Texture baseTex, Texture normal)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = $"{character}_{Sanitize(rendererName)}_{index}" };

        if (baseTex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
            mat.mainTexture = baseTex;
        }

        if (normal != null)
        {
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        return mat;
    }

    private static Texture GetBaseTexture(Material mat)
    {
        if (mat == null) return null;

        if (mat.HasProperty("_BaseMap"))
        {
            var t = mat.GetTexture("_BaseMap");
            if (t != null) return t;
        }

        if (mat.HasProperty("_MainTex"))
        {
            var t = mat.GetTexture("_MainTex");
            if (t != null) return t;
        }

        return mat.mainTexture;
    }

    private static Texture2D[] LoadTextures(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return Array.Empty<Texture2D>();

        return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(t => t != null)
            .ToArray();
    }

    /// <summary>
    /// Resuelve la textura base de un slot. El material embebido del FBX manda cuando tiene textura;
    /// si no (los materiales "Endo"/"Endo2" vienen vacíos), se deduce la intención por el nombre del
    /// material de origen: Body / Shell / Eyes / Endo / Battery / Mic.
    /// </summary>
    private static Texture2D ResolveBaseTexture(CharacterDef def, Material src, Texture2D[] textures)
    {
        var name = src == null ? string.Empty : Normalize(src.name);
        var diffuses = textures.Where(t => !IsNormalCandidate(t.name)).ToArray();

        Texture2D First(params string[] tokens) => diffuses.FirstOrDefault(t =>
            tokens.Any(k => Normalize(t.name).Contains(k)));

        // Golden Freddy comparte FBX con Freddy: hay que forzar sus texturas propias
        // (el material embebido apunta a las de Freddy y no sirve como fuente fiable)
        if (def.Name == "GoldenFreddy")
        {
            if (name.Contains("mic")) return First("mic");
            if (name.Contains("eye")) return First("gfreddyeye");
            return First("gfreddydiffuse");
        }

        var direct = GetBaseTexture(src);
        if (direct is Texture2D t2d) return t2d;

        if (name.Contains("eye"))
        {
            var eye = First("eye");
            if (eye != null) return eye;
        }

        if (name.Contains("endo"))
        {
            var endo = First("endo");
            if (endo != null) return endo;
        }

        if (name.Contains("battery"))
        {
            var battery = First("battery");
            if (battery != null) return battery;
        }

        if (name.Contains("mic"))
        {
            var mic = First("mic");
            if (mic != null) return mic;
        }

        return MainDiffuse(textures);
    }

    /// <summary>Difusa principal del personaje (ni normal, ni ojos, ni endosqueleto).</summary>
    private static Texture2D MainDiffuse(Texture2D[] textures)
    {
        var candidates = textures.Where(t => !IsNormalCandidate(t.name)).ToArray();

        var main = candidates.FirstOrDefault(t =>
        {
            var n = Normalize(t.name);
            return !n.Contains("eye") && !n.Contains("endo");
        });

        return main ?? candidates.FirstOrDefault() ?? textures.FirstOrDefault();
    }

    private static Texture2D PickNormal(Texture2D[] textures, Texture baseTex)
    {
        if (baseTex == null) return null;

        var baseName = Normalize(baseTex.name);
        if (baseName.Contains("eye")) return null;

        var normals = textures.Where(t => IsNormalCandidate(t.name)).ToArray();
        if (normals.Length == 0) return null;

        Texture2D NonEndo() => normals.FirstOrDefault(t => !Normalize(t.name).Contains("endo"));

        if (baseName.Contains("endo"))
            return normals.FirstOrDefault(t => Normalize(t.name).Contains("endo")) ?? NonEndo();
        if (baseName.Contains("battery"))
            return normals.FirstOrDefault(t => Normalize(t.name).Contains("battery")) ?? NonEndo();
        if (baseName.Contains("mic"))
            return normals.FirstOrDefault(t => Normalize(t.name).Contains("mic")) ?? NonEndo();

        var body = normals.FirstOrDefault(t =>
        {
            var n = Normalize(t.name);
            return n.Contains("body") && !n.Contains("endo");
        });
        if (body != null) return body;

        var generic = normals.FirstOrDefault(t =>
        {
            var n = Normalize(t.name);
            return n.Contains("normal") && !n.Contains("endo");
        });
        return generic ?? NonEndo();
    }

    private static bool IsNormalCandidate(string textureName)
    {
        var n = Normalize(textureName);
        return n.Contains("normal") || n.EndsWith("n");
    }

    private static Texture2D PickFallbackDiffuse(Texture2D[] textures) => MainDiffuse(textures);

    private static void ConfigureNormalMaps(IEnumerable<Texture2D> textures)
    {
        foreach (var tex in textures)
        {
            if (!IsNormalCandidate(tex.name)) continue;

            var path = AssetDatabase.GetAssetPath(tex);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
            if (importer.textureType == TextureImporterType.NormalMap) continue;

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
            Debug.Log($"[DriveRebuild] Textura {tex.name} reimportada como NormalMap.");
        }
    }

    // ---------------------------------------------------------------- PREFABS

    [MenuItem("Tools/Drive Characters/3 - Rebuild Prefabs")]
    public static void RebuildPrefabs()
    {
        EnsureFolder(PrefabRoot);

        foreach (var def in Characters)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(def.Fbx);
            if (source == null)
            {
                Debug.LogError($"[DriveRebuild] PREFAB {def.Name}: no se pudo cargar {def.Fbx}");
                continue;
            }

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[DriveRebuild] PREFAB {def.Name}: no se pudo instanciar el FBX");
                continue;
            }

            instance.name = def.Name;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // 1. Radios/huesos
            var bones = FindBones(instance.transform);

            // 2. Bounds y factor de escala
            var bounds = ComputeBounds(instance);
            var scale = bounds.size.y > 0.001f ? TargetHeight / bounds.size.y : 1f;

            // 3. Escalar y dejar los pies a y=0
            instance.transform.localScale = Vector3.one * scale;
            foreach (var child in instance.transform.Cast<Transform>().ToArray())
                child.localPosition += new Vector3(0f, -bounds.min.y, 0f);

            // 4. Posiciones locales de los targets: DESPUÉS de escalar/desplazar, para que
            //    coincidan exactamente con los huesos (antes quedaban desplazados en Y).
            var targets = ComputeTargetPositions(instance.transform, ComputeBounds(instance), bones);

            // 5. Materiales por slot
            var assigned = 0;
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                var slots = r.sharedMaterials;
                for (var i = 0; i < slots.Length; i++)
                {
                    var path = $"{MaterialFolder}/{def.Name}_{Sanitize(r.name)}_{i}.mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        Debug.LogWarning($"[DriveRebuild] PREFAB {def.Name}/{r.name}[{i}]: falta {path}");
                        continue;
                    }
                    slots[i] = mat;
                    assigned++;
                }

                r.sharedMaterials = slots;
            }

            // 6. Animator
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                $"{AnimationFolder}/{def.Name}.controller");
            if (controller != null) animator.runtimeAnimatorController = controller;
            else Debug.LogWarning($"[DriveRebuild] PREFAB {def.Name}: no existe {AnimationFolder}/{def.Name}.controller");
            animator.applyRootMotion = false;

            // 7. Nombres legibles
            RenameBones(bones);

            // 8. Targets de IK
            CreateTargets(instance.transform, targets);

            // 9. Guardar
            var prefabPath = $"{PrefabRoot}/{def.Name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);

            Debug.Log($"[DriveRebuild] PREFAB {prefabPath} | escala={scale:F5} alto={bounds.size.y:F2} " +
                      $"ancho={bounds.size.x:F2} materiales={assigned} " +
                      $"brazos={(bones.HasArmsL ? "L" : "-")}{(bones.HasArmsR ? "R" : "-")} " +
                      $"piernas={(bones.HasLegsL ? "L" : "-")}{(bones.HasLegsR ? "R" : "-")}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[DriveRebuild] PREFABS listo.");
    }

    /// <summary>
    /// El endoesqueleto de manos y piernas usa siempre la textura del Endo (endo_d.jpg)
    /// en el Base Map, en lugar de la difusa del personaje o de su endo teñido.
    /// </summary>
    private static bool TryLimbEndoTexture(string rendererName, Texture resolved,
        out Texture2D baseTex, out Texture2D normalTex)
    {
        baseTex = null;
        normalTex = null;
        if (resolved == null) return false;

        var rn = Normalize(rendererName);
        if (!LimbTokens.Any(t => rn.Contains(t))) return false;
        if (!Normalize(resolved.name).Contains("endo")) return false;

        baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SharedEndoDiffuse);
        normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SharedEndoNormal);
        if (baseTex == null)
        {
            Debug.LogWarning($"[DriveRebuild] No se pudo cargar {SharedEndoDiffuse}");
            return false;
        }

        Debug.Log($"[DriveRebuild] MAT endo-limb {rendererName}: {resolved.name} -> {baseTex.name}");
        return true;
    }

    /// <summary>
    /// Sustituye la difusa/normal de un slot cuando el FBX la trae mal mapeada
    /// (el interior del pecho de Foxy viene como "battery" y es el endoesqueleto).
    /// </summary>
    private static bool TryTextureOverride(CharacterDef def, Material src, Texture2D resolved, out Texture2D baseTex, out Texture2D normalTex)
    {
        baseTex = null;
        normalTex = null;
        if (def.TexOverrides == null || def.TexOverrides.Length == 0) return false;

        var materialName = src == null ? string.Empty : Normalize(src.name);
        var sourceTexture = GetBaseTexture(src);
        var sourceName = sourceTexture == null ? string.Empty : Normalize(sourceTexture.name);
        var resolvedName = resolved == null ? string.Empty : Normalize(resolved.name);

        foreach (var o in def.TexOverrides)
        {
            var token = Normalize(o.MaterialToken);
            if (token.Length == 0) continue;
            if (!materialName.Contains(token) && !sourceName.Contains(token) && !resolvedName.Contains(token)) continue;

            baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(o.TexturePath);
            if (!string.IsNullOrEmpty(o.NormalPath))
                normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(o.NormalPath);

            if (baseTex == null)
            {
                Debug.LogError($"[DriveRebuild] MAT override {def.Name}: no se pudo cargar {o.TexturePath}");
                return false;
            }

            Debug.Log($"[DriveRebuild] MAT override {def.Name}/{materialName}: {resolvedName} -> {baseTex.name}");
            return true;
        }

        return false;
    }

    private static Bounds ComputeBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);

        var b = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static TargetSet ComputeTargetPositions(Transform root, Bounds bounds, BoneSet bones)
    {
        var set = new TargetSet();

        Vector3 Local(Transform t) => t ? root.InverseTransformPoint(t.position) : Vector3.zero;

        set.LeftHand = Local(bones.HandL);
        set.RightHand = Local(bones.HandR);
        set.LeftFoot = Local(bones.FootL);
        set.RightFoot = Local(bones.FootR);

        // Rotación de reposo de manos/pies: el handle hereda esta orientación, de modo que
        // al entrar en Play la pose no cambia y al girar el handle la mano gira con él.
        set.LeftHandRot = bones.HandL ? bones.HandL.rotation : Quaternion.identity;
        set.RightHandRot = bones.HandR ? bones.HandR.rotation : Quaternion.identity;
        set.LeftFootRot = bones.FootL ? bones.FootL.rotation : Quaternion.identity;
        set.RightFootRot = bones.FootR ? bones.FootR.rotation : Quaternion.identity;

        // Handle para orientar el torso (rotación del pecho)
        var chestBone = bones.Chest ? bones.Chest : bones.Spine;
        set.Torso = Local(chestBone);
        set.TorsoRot = chestBone ? chestBone.rotation : Quaternion.identity;

        // Hints ligeramente por delante del codo/rodilla
        Vector3 ElbowHint(Transform rootBone, Transform mid, Transform tip)
        {
            if (!rootBone || !mid || !tip) return Local(mid);
            var dir = (mid.position - tip.position).normalized;
            return root.InverseTransformPoint(mid.position + dir * (Vector3.Distance(rootBone.position, mid.position) * 0.5f));
        }

        set.LeftElbow = ElbowHint(bones.UpperArmL, bones.LowerArmL, bones.HandL);
        set.RightElbow = ElbowHint(bones.UpperArmR, bones.LowerArmR, bones.HandR);
        set.LeftKnee = ElbowHint(bones.ThighL, bones.KneeL, bones.FootL);
        set.RightKnee = ElbowHint(bones.ThighR, bones.KneeR, bones.FootR);

        // LookAt en ESPACIO MUNDO: delante de la cabeza a media altura del personaje.
        // (bounds está en metros de mundo y el root está escalado, por eso no se suma en local.)
        var headWorld = bones.Head ? bones.Head.position : new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        set.LookAt = root.InverseTransformPoint(headWorld + root.forward * LookAtDistance(bounds));
        return set;
    }

    private static float LookAtDistance(Bounds worldBounds) => Mathf.Max(0.6f, worldBounds.size.y * 0.5f);

    private static void CreateTargets(Transform root, TargetSet set)
    {
        var old = root.Find("IKTargets");
        if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

        var group = new GameObject("IKTargets");
        group.transform.SetParent(root, false);

        Transform Make(string name, Vector3 localPos, Quaternion worldRot, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(group.transform, false);
            go.transform.localPosition = localPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = Vector3.one;

            var marker = go.AddComponent<IKTargetMarker>();
            marker.color = color;
            marker.radius = 0.045f;
            return go.transform;
        }

        Make("LeftHandTarget", set.LeftHand, set.LeftHandRot, new Color(0.1f, 0.9f, 1f));
        Make("RightHandTarget", set.RightHand, set.RightHandRot, new Color(0.1f, 0.6f, 1f));
        Make("LeftFootTarget", set.LeftFoot, set.LeftFootRot, new Color(0.5f, 1f, 0.2f));
        Make("RightFootTarget", set.RightFoot, set.RightFootRot, new Color(0.2f, 0.8f, 0.3f));
        Make("LookAtTarget", set.LookAt, Quaternion.identity, new Color(1f, 0.85f, 0.1f));
        Make("LeftElbowHint", set.LeftElbow, Quaternion.identity, new Color(0.6f, 0.6f, 0.6f));
        Make("RightElbowHint", set.RightElbow, Quaternion.identity, new Color(0.6f, 0.6f, 0.6f));
        Make("LeftKneeHint", set.LeftKnee, Quaternion.identity, new Color(0.6f, 0.6f, 0.6f));
        Make("RightKneeHint", set.RightKnee, Quaternion.identity, new Color(0.6f, 0.6f, 0.6f));
        Make("TorsoRotateTarget", set.Torso, set.TorsoRot, new Color(0.9f, 0.4f, 1f));
    }

    // -------------------------------------------------------------------- RIG

    [MenuItem("Tools/Drive Characters/4 - Build Procedural Rig")]
    public static void BuildProceduralRig()
    {
        var sphere = SafeBuiltinSphere();

        foreach (var def in Characters)
        {
            var prefabPath = $"{PrefabRoot}/{def.Name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[DriveRebuild] RIG {def.Name}: no se pudo abrir {prefabPath}");
                continue;
            }

            try
            {
                var oldRig = root.transform.Find("Rig");
                if (oldRig) UnityEngine.Object.DestroyImmediate(oldRig.gameObject);

                var rigGO = new GameObject("Rig");
                rigGO.transform.SetParent(root.transform, false);
                var rig = rigGO.AddComponent<Rig>();

                var builder = root.GetComponent<RigBuilder>();
                if (builder == null) builder = root.AddComponent<RigBuilder>();
                builder.layers.Clear();
                builder.layers.Add(new RigLayer(rig, true));

                var targets = root.transform.Find("IKTargets");
                Transform T(string n) => targets ? targets.Find(n) : null;
                Transform B(string n)
                {
                    // Los huesos renombrados viven en la jerarquía, no en IKTargets.
                    // Se excluyen objetos de malla (hay piezas llamadas "Head", "HandR"...).
                    return root.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(t => t.name == n && t.GetComponent<Renderer>() == null);
                }

                var lh = T("LeftHandTarget"); var rh = T("RightHandTarget");
                var lf = T("LeftFootTarget"); var rf = T("RightFootTarget");
                var le = T("LeftElbowHint"); var re = T("RightElbowHint");
                var lk = T("LeftKneeHint"); var rk = T("RightKneeHint");
                var lookAt = T("LookAtTarget");
                var torsoTarget = T("TorsoRotateTarget");
                FixLookAtDistance(def.Name, root, B(HeadName), lookAt);

                var spineBone = B(SpineName);
                var chestBone = B(ChestName);

                // ORDEN IMPORTANTE: Animation Rigging evalúa las constraints en orden de jerarquía.
                // Primero se orienta el cuerpo (torso, cuello, cabeza) y AL FINAL se resuelve el IK
                // de brazos y piernas, para que manos/pies acaben siempre sobre su target.
                var count = 0;
                count += AddTorsoRotation(rigGO.transform, spineBone, chestBone, torsoTarget);
                count += AddNeckAim(rigGO.transform, B(NeckName), lookAt, root.transform, B(HeadName));
                count += AddHeadAim(rigGO.transform, B(HeadName), lookAt, root.transform);
                count += AddTwoBone(rigGO.transform, "ArmIK_L", B("UpperArm_L"), B("LowerArm_L"), B("Hand_L"), lh, le, 1f);
                count += AddTwoBone(rigGO.transform, "ArmIK_R", B("UpperArm_R"), B("LowerArm_R"), B("Hand_R"), rh, re, 1f);
                count += AddTwoBone(rigGO.transform, "LegIK_L", B("Thigh_L"), B("Knee_L"), B("Foot_L"), lf, lk, 1f);
                count += AddTwoBone(rigGO.transform, "LegIK_R", B("Thigh_R"), B("Knee_R"), B("Foot_R"), rf, rk, 1f);
                Debug.Log($"[DriveRebuild] RIG {def.Name} orden: " +
                          string.Join(", ", rigGO.transform.Cast<Transform>().Select(t => t.name)));

                // Effectors visuales (manejables en la vista de escena)
                if (sphere != null)
                {
                    var style = new RigEffectorData.Style
                    {
                        shape = sphere,
                        color = new Color(0.1f, 0.9f, 1f),
                        size = 0.04f,
                        position = Vector3.zero,
                        rotation = Vector3.zero
                    };
                    foreach (var t in new[] { lh, rh, lf, rf, lookAt, torsoTarget })
                        if (t) builder.AddEffector(t, style);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[DriveRebuild] RIG {prefabPath}: constraints={count} " +
                          $"targets={(targets ? targets.childCount : 0)} effectors={(sphere != null ? 5 : 0)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DriveRebuild] RIG {def.Name} falló: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[DriveRebuild] RIG listo.");
    }

    /// <summary>
    /// Un LookAt pegado al pivote de la cabeza hace el aim inestable (la cabeza se va hacia atrás).
    /// Si está demasiado cerca, se recoloca delante de la cabeza a una distancia segura.
    /// </summary>
    private static void FixLookAtDistance(string label, GameObject root, Transform head, Transform lookAt)
    {
        if (!head || !lookAt) return;
        var bounds = ComputeBounds(root);
        var minDist = LookAtDistance(bounds) * 0.5f;
        var dist = Vector3.Distance(head.position, lookAt.position);
        if (dist >= minDist) return;

        lookAt.position = head.position + root.transform.forward * LookAtDistance(bounds);
        Debug.Log($"[DriveRebuild] RIG {label}: LookAtTarget estaba a {dist:F3} m de la cabeza, recolocado a " +
                  $"{Vector3.Distance(head.position, lookAt.position):F3} m delante.");
    }

    private static int AddTwoBone(Transform rigRoot, string name, Transform rootBone, Transform mid, Transform tip,
        Transform target, Transform hint, float targetRotationWeight)
    {
        if (!rootBone || !mid || !tip || !target)
        {
            Debug.LogWarning($"[DriveRebuild] RIG {name}: cadena incompleta, se omite.");
            return 0;
        }

        var go = new GameObject(name);
        go.transform.SetParent(rigRoot, false);
        var c = go.AddComponent<TwoBoneIKConstraint>();
        c.data.root = rootBone;
        c.data.mid = mid;
        c.data.tip = tip;
        c.data.target = target;
        c.data.hint = hint;
        c.data.targetPositionWeight = 1f;
        c.data.targetRotationWeight = targetRotationWeight;
        c.data.hintWeight = 1f;
        c.weight = 1f;
        return 1;
    }

    /// <summary>
    /// Orienta el torso con el handle TorsoRotateTarget. Si existen Spine y Chest, la rotación
    /// se reparte (Spine 50 %, Chest 100 % encima); si solo hay uno, ese hueso la toma entera.
    /// </summary>
    private static int AddTorsoRotation(Transform rigRoot, Transform spine, Transform chest, Transform target)
    {
        if (!target || (!spine && !chest))
        {
            Debug.LogWarning("[DriveRebuild] RIG TorsoRotate: falta hueso de torso o handle, se omite.");
            return 0;
        }

        var bones = new List<(Transform bone, float weight)>();
        if (spine && chest && spine != chest)
        {
            bones.Add((spine, 0.5f));
            bones.Add((chest, 1f));
        }
        else
        {
            bones.Add((chest ? chest : spine, 1f));
        }

        var count = 0;
        foreach (var (bone, weight) in bones)
        {
            var go = new GameObject(bones.Count > 1 ? $"TorsoRotate_{bone.name}" : "TorsoRotate");
            go.transform.SetParent(rigRoot, false);
            var c = go.AddComponent<MultiRotationConstraint>();
            c.data.constrainedObject = bone;

            var sources = c.data.sourceObjects;
            sources.Clear();
            sources.Add(new WeightedTransform(target, 1f));
            c.data.sourceObjects = sources;
            c.data.maintainOffset = true;
            c.weight = weight;
            count++;
        }

        Debug.Log($"[DriveRebuild] TorsoRotate huesos={string.Join(", ", bones.Select(b => $"{b.bone.name}({b.weight:F1})"))}");
        return count;
    }

    /// <summary>
    /// El cuello aporta parte de la mirada (40 %, ±30°); la cabeza completa el resto.
    /// El cuello NO apunta al LookAtTarget directamente: éste está a la altura de la cabeza, y desde
    /// el cuello quedaría ~10° hacia arriba (el cuello se echaba hacia atrás al dar a Play). Se usa un
    /// ayudante hijo del LookAtTarget, a la altura del cuello, que en reposo coincide con su eje de mira
    /// y se desplaza junto al LookAtTarget cuando éste se mueve.
    /// </summary>
    private static int AddNeckAim(Transform rigRoot, Transform neck, Transform lookAt, Transform modelRoot, Transform head)
    {
        if (!neck || !lookAt)
        {
            Debug.LogWarning("[DriveRebuild] RIG NeckAim: falta cuello o LookAtTarget, se omite.");
            return 0;
        }

        var old = lookAt.Find(NeckAimHelperName);
        if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);

        var helper = new GameObject(NeckAimHelperName).transform;
        helper.SetParent(lookAt, false);
        var reference = head ? head.position : neck.position;
        helper.position = neck.position + (lookAt.position - reference);
        Debug.Log($"[DriveRebuild] NeckAim ayudante a {Vector3.Distance(neck.position, helper.position):F3} m del cuello " +
                  $"(offset vertical respecto al LookAt {helper.position.y - lookAt.position.y:F3} m)");

        return AddAim(rigRoot, "NeckAim", neck, helper, modelRoot, 0.4f, 30f);
    }

    private const string NeckAimHelperName = "NeckAimHelper";

    private static int AddHeadAim(Transform rigRoot, Transform head, Transform lookAt, Transform modelRoot)
    {
        if (!head || !lookAt) return 0;
        return AddAim(rigRoot, "HeadAim", head, lookAt, modelRoot, 1f, 50f);
    }

    private static int AddAim(Transform rigRoot, string name, Transform head, Transform lookAt, Transform modelRoot,
        float weight, float limit)
    {
        var go = new GameObject(name);
        go.transform.SetParent(rigRoot, false);
        var c = go.AddComponent<MultiAimConstraint>();

        c.data.constrainedObject = head;

        var sources = c.data.sourceObjects;
        sources.Clear();
        sources.Add(new WeightedTransform(lookAt, 1f));
        c.data.sourceObjects = sources;

        // Detectar qué eje local de la cabeza apunta "hacia delante" en el modelo
        var forward = modelRoot.forward;
        var best = MultiAimConstraintData.Axis.Z;
        var bestDot = float.NegativeInfinity;
        void Consider(Vector3 dir, MultiAimConstraintData.Axis axis)
        {
            var d = Vector3.Dot(dir, forward);
            if (d > bestDot) { bestDot = d; best = axis; }
        }
        Consider(head.forward, MultiAimConstraintData.Axis.Z);
        Consider(-head.forward, MultiAimConstraintData.Axis.Z_NEG);
        Consider(head.right, MultiAimConstraintData.Axis.X);
        Consider(-head.right, MultiAimConstraintData.Axis.X_NEG);
        Consider(head.up, MultiAimConstraintData.Axis.Y);
        Consider(-head.up, MultiAimConstraintData.Axis.Y_NEG);

        c.data.aimAxis = best;
        c.data.upAxis = MultiAimConstraintData.Axis.Y;
        c.data.worldUpType = MultiAimConstraintData.WorldUpType.None;
        c.data.limits = new Vector2(-limit, limit);
        c.data.maintainOffset = true;
        c.weight = weight;

        Debug.Log($"[DriveRebuild] {name} hueso={head.name} eje detectado = {best} (dot={bestDot:F2}) weight={weight:F1} limites=±{limit:F0}");
        return 1;
    }

    private static Mesh SafeBuiltinSphere()
    {
        try { return Resources.GetBuiltinResource<Mesh>("Sphere.fbx"); }
        catch (Exception e)
        {
            Debug.LogWarning($"[DriveRebuild] No se pudo obtener la esfera builtin para effectors: {e.Message}");
            return null;
        }
    }

    // ---------------------------------------------------------------- SHOWCASE

    [MenuItem("Tools/Drive Characters/5 - Rebuild Showcase Scene")]
    public static void RebuildShowcase()
    {
        var active = SceneManager.GetActiveScene();
        if (active.path != ScenePath)
        {
            active = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var root = GameObject.Find("DriveCharacterShowcase");
        if (root == null)
        {
            root = new GameObject("DriveCharacterShowcase");
            Undo.RegisterCreatedObjectUndo(root, "Create showcase root");
        }

        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        foreach (var child in root.transform.Cast<Transform>().ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);

        // Medidas de cada prefab
        var prefabs = Characters
            .Select(d => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{d.Name}.prefab"))
            .Where(p => p != null)
            .ToArray();

        if (prefabs.Length == 0)
        {
            Debug.LogError("[DriveRebuild] SCENE: no hay prefabs que colocar.");
            return;
        }

        var sizes = prefabs.Select(p =>
        {
            var rs = p.GetComponentsInChildren<Renderer>(true);
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds();
            for (var i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b.size;
        }).ToArray();

        var colWidth = sizes.Max(s => s.x) + 0.55f;
        var rowDepth = Mathf.Max(sizes.Max(s => s.z) + 1.6f, 3.6f);
        const int columns = 5;
        var rows = Mathf.CeilToInt(prefabs.Length / (float)columns);
        var gridWidth = columns * colWidth;
        var gridDepth = rows * rowDepth;

        for (var i = 0; i < prefabs.Length; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var pos = new Vector3(
                (col - (columns - 1) * 0.5f) * colWidth,
                0f,
                row * rowDepth);

            var instance = PrefabUtility.InstantiatePrefab(prefabs[i], root.transform) as GameObject;
            if (instance == null) continue;

            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var headHeight = sizes[i].y > 0.1f ? sizes[i].y : TargetHeight;

            var labelGO = new GameObject($"{prefabs[i].name}_Label");
            labelGO.transform.SetParent(root.transform, false);
            labelGO.transform.position = pos + Vector3.up * (headHeight + 0.22f);
            labelGO.transform.rotation = Quaternion.identity;

            var text = labelGO.AddComponent<TextMesh>();
            text.text = prefabs[i].name;
            text.fontSize = 64;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(0.06f, 0.06f, 0.09f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) text.font = font;
            else Debug.LogWarning("[DriveRebuild] SCENE: no se encontró LegacyRuntime.ttf para las etiquetas.");
        }

        // Suelo
        var floor = GameObject.Find("ReviewFloor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ReviewFloor";
        }
        floor.transform.position = new Vector3(0f, 0f, gridDepth * 0.25f);
        floor.transform.localScale = new Vector3(gridWidth * 0.16f, 1f, (gridDepth + 6f) * 0.2f);

        // Cámara
        var camGO = GameObject.Find("ShowcaseCamera");
        if (camGO == null)
        {
            camGO = new GameObject("ShowcaseCamera");
            camGO.AddComponent<Camera>();
        }
        var cam = camGO.GetComponent<Camera>();
        cam.fieldOfView = 40f;
        camGO.transform.position = new Vector3(0f, 1.5f + gridDepth * 0.35f, -(gridWidth * 0.62f));
        camGO.transform.rotation = Quaternion.Euler(9f, 0f, 0f);

        // Luz
        var lightGO = GameObject.Find("ShowcaseLight");
        if (lightGO == null)
        {
            lightGO = new GameObject("ShowcaseLight");
            lightGO.AddComponent<Light>();
        }
        var light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active);

        Debug.Log($"[DriveRebuild] SCENE {ScenePath} reconstruida: {prefabs.Length} personajes, " +
                  $"columnas={columns} separación={colWidth:F2} profundidad={rowDepth:F2}");
    }

    // ------------------------------------------------------------- IDLE SCRIPT

    [MenuItem("Tools/Drive Characters/6 - Attach Procedural Idle")]
    public static void AttachProceduralIdle()
    {
        foreach (var def in Characters)
        {
            var prefabPath = $"{PrefabRoot}/{def.Name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) continue;

            try
            {
                var idle = root.GetComponent<ProceduralIdle>();
                if (idle == null) idle = root.AddComponent<ProceduralIdle>();

                Transform Find(string n) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);

                idle.hips = Find(HipsName);
                idle.neck = Find(NeckName);
                // Solo huesos: hay piezas de malla con nombres como "Head".
                Transform FindBone(string n) => root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == n && t.GetComponent<Renderer>() == null);
                idle.spine = FindBone(SpineName);
                idle.chest = FindBone(ChestName);
                idle.head = FindBone(HeadName);
                var targets = root.transform.Find("IKTargets");
                if (targets)
                {
                    idle.leftFootTarget = targets.Find("LeftFootTarget");
                    idle.rightFootTarget = targets.Find("RightFootTarget");
                }

                if (idle.hips == null || idle.neck == null)
                    Debug.LogWarning($"[DriveRebuild] IDLE {def.Name}: hips={N(idle.hips)} neck={N(idle.neck)}");

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[DriveRebuild] IDLE {prefabPath}: hips={N(idle.hips)} neck={N(idle.neck)} " +
                          $"pieL={(idle.leftFootTarget ? idle.leftFootTarget.name : "<none>")} " +
                          $"pieR={(idle.rightFootTarget ? idle.rightFootTarget.name : "<none>")}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DriveRebuild] IDLE {def.Name} falló: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[DriveRebuild] IDLE listo.");
    }

    [MenuItem("Tools/Drive Characters/Rebuild All")]
    public static void RebuildAll()
    {
        RebuildMaterials();
        RebuildPrefabs();
        BuildProceduralRig();
        RebuildShowcase();
        AttachProceduralIdle();
        Debug.Log("[DriveRebuild] TODO LISTO.");
    }

    // --------------------------------------------------------------- BONE FIND

    private static BoneSet FindBones(Transform root)
    {
        // Solo huesos reales: fuera objetos de malla (hay personajes con piezas llamadas "Head", "HandR"...)
        var all = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t != root)
            .Where(t => t.GetComponent<Renderer>() == null)
            .Where(t => !IsIgnoredBone(t.name))
            .ToArray();

        var set = new BoneSet();
        set.Pelvis = Pick(all, "pelvis", null);
        set.Spine = Pick(all, "spine", null);
        set.Chest = all.FirstOrDefault(t => Norm(t.name).Contains("chest") ||
                                            Norm(t.name).Contains("spine1") ||
                                            Norm(t.name).Contains("spine2"));
        set.Neck = Pick(all, "neck", null);
        set.Head = Pick(all, "head", null);

        set.UpperArmL = Pick(all, "upperarm", "l");
        set.LowerArmL = Pick(all, "lowerarm", "l");
        set.HandL = Pick(all, "hand", "l");
        set.UpperArmR = Pick(all, "upperarm", "r");
        set.LowerArmR = Pick(all, "lowerarm", "r");
        set.HandR = Pick(all, "hand", "r");

        set.ThighL = Pick(all, "hip", "l");
        set.KneeL = Pick(all, "knee", "l");
        set.FootL = Pick(all, "foot", "l");
        set.ThighR = Pick(all, "hip", "r");
        set.KneeR = Pick(all, "knee", "r");
        set.FootR = Pick(all, "foot", "r");

        // Cadena de IK: si falta la punta (mano/pie) se usa el hijo del hueso medio
        set.HandL = EnsureTip(set.UpperArmL, set.LowerArmL, set.HandL, "Hand_L");
        set.HandR = EnsureTip(set.UpperArmR, set.LowerArmR, set.HandR, "Hand_R");
        set.FootL = EnsureTip(set.ThighL, set.KneeL, set.FootL, "Foot_L");
        set.FootR = EnsureTip(set.ThighR, set.KneeR, set.FootR, "Foot_R");

        return set;
    }

    private static Transform EnsureTip(Transform rootBone, Transform mid, Transform tip, string label)
    {
        if (rootBone == null || mid == null) return null;
        if (tip != null) return tip;

        // Primer hijo válido del hueso medio
        for (var i = 0; i < mid.childCount; i++)
        {
            var child = mid.GetChild(i);
            if (child.GetComponent<Renderer>() != null) continue;
            if (Norm(child.name).Contains("jiggle")) continue;
            Debug.Log($"[DriveRebuild] {label}: sin hueso terminal, se usa el hijo {child.name}");
            return child;
        }

        // Ayudante sintético al final del hueso medio
        var dir = (mid.position - rootBone.position).normalized;
        var len = Mathf.Max(Vector3.Distance(rootBone.position, mid.position) * 0.8f, 0.001f);
        var helper = new GameObject($"{label}_IKTip").transform;
        helper.SetParent(mid, false);
        helper.position = mid.position + dir * len;
        Debug.LogWarning($"[DriveRebuild] {label}: no existe hueso terminal, se crea {helper.name} sintético");
        return helper;
    }

    private static bool IsIgnoredBone(string name)
    {
        var n = Norm(name);
        return n.Contains("jiggle") || n.Contains("target") || n.Contains("end") ||
               n.Contains("pad") || n.Contains("eye") || n.Contains("jaw") || n.Contains("ear") ||
               n.Contains("bowtie") || n.Contains("tie") || n.Contains("hair") || n.Contains("shoulderpad");
    }

    private static Transform Pick(Transform[] bones, string token, string side)
    {
        foreach (var b in bones)
        {
            var n = Norm(b.name);
            if (!n.Contains(token)) continue;
            if (side != null)
            {
                if (!n.EndsWith(side)) continue;
            }
            return b;
        }
        return null;
    }

    private static void RenameBones(BoneSet bones)
    {
        void R(Transform t, string name)
        {
            if (t && t.name != name) t.name = name;
        }

        R(bones.Pelvis, HipsName);
        R(bones.Spine, SpineName);
        R(bones.Chest, ChestName);
        R(bones.Neck, NeckName);
        R(bones.Head, HeadName);

        R(bones.UpperArmL, "UpperArm_L");
        R(bones.LowerArmL, "LowerArm_L");
        R(bones.HandL, "Hand_L");
        R(bones.UpperArmR, "UpperArm_R");
        R(bones.LowerArmR, "LowerArm_R");
        R(bones.HandR, "Hand_R");

        R(bones.ThighL, "Thigh_L");
        R(bones.KneeL, "Knee_L");
        R(bones.FootL, "Foot_L");
        R(bones.ThighR, "Thigh_R");
        R(bones.KneeR, "Knee_R");
        R(bones.FootR, "Foot_R");
    }

    // ----------------------------------------------------------------- HELPERS

    private static string Norm(string value) =>
        new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string Normalize(string value) => Norm(value);

    private static string Sanitize(string value)
    {
        var chars = value.Select(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' ? c : '_').ToArray();
        return new string(chars);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
