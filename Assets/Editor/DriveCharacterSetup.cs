using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DriveCharacterSetup
{
    private const string ModelRoot = "Assets/GameStuff/Models/DriveRigged";
    private const string PrefabRoot = "Assets/GameStuff/Prefabs/DriveCharacters";
    private const string AnimationRoot = "Assets/GameStuff/Animations/DriveCharacters";
    private const string ScenePath = "Assets/GameStuff/TestScenes/DriveCharacterShowcase.unity";

    private sealed class CharacterDefinition
    {
        public string Name;
        public string Fbx;
        public string TextureFolder;
        public bool Golden;
    }

    public static void GenerateAll()
    {
        EnsureFolder("Assets/GameStuff");
        EnsureFolder("Assets/GameStuff/Prefabs");
        EnsureFolder("Assets/GameStuff/Animations");
        EnsureFolder("Assets/GameStuff/TestScenes");
        EnsureFolder(PrefabRoot);
        EnsureFolder(AnimationRoot);
        CleanGeneratedMaterials();

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var definitions = new List<CharacterDefinition>
        {
            new CharacterDefinition { Name = "Bonnie", Fbx = $"{ModelRoot}/Bonnie/HWBonnieis cool.fbx", TextureFolder = $"{ModelRoot}/Bonnie" },
            new CharacterDefinition { Name = "Chica", Fbx = $"{ModelRoot}/Chica/HWChica.fbx", TextureFolder = $"{ModelRoot}/Chica" },
            new CharacterDefinition { Name = "Endo", Fbx = $"{ModelRoot}/Endo/HWEndo.fbx", TextureFolder = $"{ModelRoot}/Endo" },
            new CharacterDefinition { Name = "Foxy", Fbx = $"{ModelRoot}/Foxy/HWFoxy.fbx", TextureFolder = $"{ModelRoot}/Foxy" },
            new CharacterDefinition { Name = "Freddy", Fbx = $"{ModelRoot}/Freddy/HWFreddy.fbx", TextureFolder = $"{ModelRoot}/Freddy" },
            new CharacterDefinition { Name = "GoldenFreddy", Fbx = $"{ModelRoot}/Freddy/HWFreddy.fbx", TextureFolder = $"{ModelRoot}/Freddy", Golden = true },
            new CharacterDefinition { Name = "WitheredBonnie", Fbx = $"{ModelRoot}/WitheredBonnie/HWwbonnie.fbx", TextureFolder = $"{ModelRoot}/WitheredBonnie" },
            new CharacterDefinition { Name = "WitheredChica", Fbx = $"{ModelRoot}/WitheredChica/HWwchica.fbx", TextureFolder = $"{ModelRoot}/WitheredChica" },
            new CharacterDefinition { Name = "WitheredFoxy", Fbx = $"{ModelRoot}/WitheredFoxy/HWwfoxy.fbx", TextureFolder = $"{ModelRoot}/WitheredFoxy" },
            new CharacterDefinition { Name = "WitheredFreddy", Fbx = $"{ModelRoot}/WitheredFreddy/HWwfreddy.fbx", TextureFolder = $"{ModelRoot}/WitheredFreddy" }
        };

        var prefabs = new List<GameObject>();
        foreach (var definition in definitions)
        {
            if (!File.Exists(ToAbsolute(definition.Fbx)))
            {
                Debug.LogWarning($"Skipping {definition.Name}: missing {definition.Fbx}");
                continue;
            }

            AssetDatabase.ImportAsset(definition.Fbx, ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(definition.Fbx);
            if (source == null)
            {
                Debug.LogWarning($"Skipping {definition.Name}: Unity could not load {definition.Fbx}");
                continue;
            }

            var instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                Debug.LogWarning($"Skipping {definition.Name}: prefab instantiation failed");
                continue;
            }

            instance.name = definition.Name;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            PrepareInstance(instance, definition);

            var prefabPath = $"{PrefabRoot}/{definition.Name}.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            if (saved != null)
                prefabs.Add(saved);

            UnityEngine.Object.DestroyImmediate(instance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CreateShowcase(prefabs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Debug.Log($"DriveCharacterSetup complete: generated {prefabs.Count} prefabs and {ScenePath}.");
    }

    public static void ValidateAll()
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
        var valid = 0;
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;
            var animator = prefab.GetComponent<Animator>();
            var skinned = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var hooks = prefab.transform.Find("DynamicRigHooks");
            var texturedSlots = prefab.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Count(material => material != null && (material.GetTexture("_BaseMap") != null || material.GetTexture("_MainTex") != null));
            if (animator == null || skinned.Length == 0 || hooks == null)
                throw new InvalidOperationException($"{path}: missing Animator, skinned mesh, or DynamicRigHooks.");
            Debug.Log($"VALID {path}: skinned={skinned.Length}, texturedSlots={texturedSlots}, hooks={hooks.childCount}");
            valid++;
        }

        if (!File.Exists(ToAbsolute(ScenePath)))
            throw new FileNotFoundException("Showcase scene was not generated.", ScenePath);
        if (EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath))
            throw new InvalidOperationException("Showcase scene must remain outside Build Settings.");
        Debug.Log($"DriveCharacterSetup validation complete: {valid} prefabs validated; showcase excluded from Build Settings.");
    }

    public static void AuditMaterials()
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            Debug.Log($"MATERIAL AUDIT {path}");
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (var i = 0; i < slots.Length; i++)
                {
                    var material = slots[i];
                    var baseTexture = material == null ? null : material.GetTexture("_BaseMap") ?? material.GetTexture("_MainTex");
                    var normalTexture = material == null ? null : material.GetTexture("_BumpMap");
                    Debug.Log(
                        $"MATERIAL SLOT renderer={renderer.name} index={i} material={(material == null ? "<null>" : material.name)} " +
                        $"base={(baseTexture == null ? "<none>" : baseTexture.name)} normal={(normalTexture == null ? "<none>" : normalTexture.name)}");
                }
            }

        }
    }

    public static void AuditSourceMaterials()
    {
        var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { ModelRoot });
        foreach (var guid in fbxGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                continue;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
                continue;
            Debug.Log($"SOURCE MATERIAL AUDIT {path}");
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                var names = renderer.sharedMaterials.Select(material => material == null ? "<null>" : material.name);
                Debug.Log($"SOURCE SLOTS renderer={renderer.name} materials={string.Join("|", names)}");
            }
        }
    }

    private static void PrepareInstance(GameObject instance, CharacterDefinition definition)
    {
        var animator = instance.GetComponent<Animator>();
        if (!animator)
            animator = instance.AddComponent<Animator>();
        if (!animator)
        {
            Debug.LogWarning($"{definition.Name}: could not create an Animator on the prefab root.");
            return;
        }
        var controllerPath = $"{AnimationRoot}/{definition.Name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        animator.runtimeAnimatorController = controller;

        var clips = CreatePoseClips(definition.Name);
        ConfigureController(controller, clips);

        var hooks = new GameObject("DynamicRigHooks");
        hooks.transform.SetParent(instance.transform, false);
        AddHook(hooks.transform, "RootTarget", FindBone(instance.transform, "root", "armature"));
        AddHook(hooks.transform, "PelvisTarget", FindBone(instance.transform, "pelvis", "hip"));
        AddHook(hooks.transform, "HeadTarget", FindBone(instance.transform, "head", "neck"));
        AddHook(hooks.transform, "LeftHandTarget", FindBone(instance.transform, "lefthand", "left hand", "leftwrist"));
        AddHook(hooks.transform, "RightHandTarget", FindBone(instance.transform, "righthand", "right hand", "rightwrist"));
        AddHook(hooks.transform, "LeftFootTarget", FindBone(instance.transform, "leftfoot", "left ankle"));
        AddHook(hooks.transform, "RightFootTarget", FindBone(instance.transform, "rightfoot", "right ankle"));
        AddHook(hooks.transform, "WallContactNormal", hooks.transform);

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                    continue;
                var sourceMaterialName = materials[i].name;
                var material = new Material(materials[i]) { name = $"{definition.Name}_{renderer.name}_{i}" };
                ApplyTextures(material, definition, sourceMaterialName);
                var materialPath = $"{PrefabRoot}/Materials/{Sanitize(material.name)}.mat";
                EnsureFolder($"{PrefabRoot}/Materials");
                AssetDatabase.CreateAsset(material, UniqueAssetPath(materialPath));
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }

        foreach (var mesh in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (mesh.sharedMesh == null)
                Debug.LogWarning($"{definition.Name}: renderer {mesh.name} has no skinned mesh.");
        }
    }

    private static AnimationClip[] CreatePoseClips(string characterName)
    {
        var result = new AnimationClip[3];
        var names = new[] { "Pose_Neutral", "Pose_ArmsExtended", "Pose_WallContact" };
        for (var i = 0; i < names.Length; i++)
        {
            var path = $"{AnimationRoot}/{characterName}_{names[i]}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = $"{characterName}_{names[i]}", legacy = false, frameRate = 30f };
                AssetDatabase.CreateAsset(clip, path);
            }
            result[i] = clip;
        }
        return result;
    }

    private static void ConfigureController(AnimatorController controller, AnimationClip[] clips)
    {
        var layer = controller.layers[0];
        var machine = layer.stateMachine;
        foreach (var state in machine.states)
            machine.RemoveState(state.state);

        var neutral = machine.AddState(clips[0].name);
        neutral.motion = clips[0];
        machine.defaultState = neutral;
        var arms = machine.AddState(clips[1].name);
        arms.motion = clips[1];
        var wall = machine.AddState(clips[2].name);
        wall.motion = clips[2];
        controller.parameters = new[]
        {
            new AnimatorControllerParameter { name = "Pose", type = AnimatorControllerParameterType.Int, defaultInt = 0 }
        };
        var any = machine.AddAnyStateTransition(arms);
        any.AddCondition(AnimatorConditionMode.Equals, 1, "Pose");
        var anyWall = machine.AddAnyStateTransition(wall);
        anyWall.AddCondition(AnimatorConditionMode.Equals, 2, "Pose");
        var anyNeutral = machine.AddAnyStateTransition(neutral);
        anyNeutral.AddCondition(AnimatorConditionMode.Equals, 0, "Pose");
        EditorUtility.SetDirty(controller);
    }

    private static void CreateShowcase(List<GameObject> prefabs)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("DriveCharacterShowcase");
        var cameraObject = new GameObject("ShowcaseCamera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.transform.position = new Vector3(0f, 4f, -16f);
        cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        camera.fieldOfView = 45f;

        var lightObject = new GameObject("ShowcaseLight");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "ReviewFloor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = Vector3.one * 8f;

        for (var i = 0; i < prefabs.Count; i++)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefabs[i]) as GameObject;
            if (instance == null)
                continue;
            instance.transform.SetParent(root.transform, false);
            instance.transform.position = new Vector3((i % 5) * 3.2f - 6.4f, 0f, (i / 5) * 4f);
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var label = new GameObject($"{instance.name}_Label");
            label.transform.SetParent(root.transform, false);
            label.transform.position = instance.transform.position + Vector3.up * 2.8f;
            var text = label.AddComponent<TextMesh>();
            text.text = instance.name;
            text.characterSize = 0.18f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);
    }

    private static void AddHook(Transform parent, string name, Transform bone)
    {
        var hook = new GameObject(name).transform;
        hook.SetParent(bone != null ? bone : parent, false);
        hook.localPosition = Vector3.zero;
        hook.localRotation = Quaternion.identity;
        hook.localScale = Vector3.one;
    }

    private static Transform FindBone(Transform root, params string[] tokens)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            var normalized = child.name.Replace("_", "").Replace(" ", "").ToLowerInvariant();
            if (tokens.Any(token => normalized.Contains(token.Replace("_", "").Replace(" ", "").ToLowerInvariant())))
                return child;
        }
        return null;
    }

    private static void ApplyTextures(Material material, CharacterDefinition definition, string sourceMaterialName)
    {
        var folder = definition.TextureFolder;
        var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(texture => texture != null)
            .ToArray();
        ConfigureNormalMaps(textures);
        var materialName = sourceMaterialName.ToLowerInvariant();
        var texture = PickTexture(textures, definition, materialName, false);

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader != null)
            material.shader = shader;
        if (texture != null && material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (texture != null && material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        var normal = PickTexture(textures, definition, materialName, true);
        if (normal != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }
    }

    private static Texture2D PickTexture(Texture2D[] textures, CharacterDefinition definition, string materialName, bool normal)
    {
        var part = materialName.Contains("eye") ? "eye" :
            materialName.Contains("endo") || materialName.Contains("skeleton") ? "endo" :
            materialName.Contains("mic") ? "mic" :
            materialName.Contains("battery") ? "battery" :
            materialName.Contains("shell") || materialName.Contains("outer") ? "shell" : "body";
        var keys = new List<string>();

        if (normal)
        {
            if (part == "body" || part == "shell")
                keys.AddRange(new[] { "normal", "_n" });
            else if (part == "endo")
                keys.Add("endonormal");
            else
                return null;
        }
        else if (definition.Name == "GoldenFreddy")
        {
            keys.Add(part == "eye" ? "gfreddyeye" : "gfreddydiffuse");
        }
        else if (definition.Name == "Bonnie")
        {
            if (part == "body") keys.Add("bonniediffuse");
            if (part == "eye") keys.Add("beyediffuse");
        }
        else if (definition.Name == "Chica")
        {
            if (part == "body") keys.Add("chicabodyd");
            if (part == "eye") keys.Add("chicaeyed");
        }
        else if (definition.Name == "Endo")
        {
            if (part == "eye") keys.Add("endoeyediffuse");
            else keys.AddRange(new[] { "endo2", "bodye" });
        }
        else if (definition.Name == "Foxy")
        {
            if (part == "body") keys.Add("foxydiffuse");
            if (part == "eye") keys.Add("foxeyed");
            if (part == "battery") keys.Add("batteryd");
        }
        else if (definition.Name == "Freddy")
        {
            if (part == "body") keys.Add("freddydiffuse");
            if (part == "eye") keys.Add("freddyeyediffuse");
            if (part == "mic") keys.Add("micd");
        }
        else if (definition.Name == "WitheredBonnie")
        {
            if (part == "body" || part == "shell") keys.Add("wbndiffuse");
            if (part == "endo") keys.Add("wbnendodiffuse");
            if (part == "eye") keys.Add("wbnendodiffuseeye");
        }
        else if (definition.Name == "WitheredChica")
        {
            if (part == "body" || part == "shell") keys.Add("wchdiffuse");
            if (part == "endo") keys.Add("wchendodiffuse");
            if (part == "eye") keys.Add("wcheyediffuse");
        }
        else if (definition.Name == "WitheredFoxy")
        {
            if (part == "body" || part == "shell") keys.Add("wfxdiffuse");
            if (part == "endo") keys.Add("wfxendodiffuse");
            if (part == "eye") keys.Add("wfxeyediffuse");
        }
        else if (definition.Name == "WitheredFreddy")
        {
            if (part == "body" || part == "shell") keys.Add("wfddiffuse");
            if (part == "endo") keys.Add("wfdendodiffuse");
            if (part == "eye") keys.Add("wfdeyediffuse");
        }

        return textures.FirstOrDefault(texture =>
            keys.Any(key => Normalize(texture.name).Contains(Normalize(key))));
    }

    private static void ConfigureNormalMaps(IEnumerable<Texture2D> textures)
    {
        foreach (var texture in textures)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            var name = Normalize(texture.name);
            if (!name.Contains("normal") && !name.EndsWith("n"))
                continue;
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }
    }

    private static string Normalize(string value)
    {
        return new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

    private static void CleanGeneratedMaterials()
    {
        var folder = $"{PrefabRoot}/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EnsureFolder(folder);
            return;
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static string UniqueAssetPath(string path)
    {
        if (!File.Exists(ToAbsolute(path)))
            return path;
        return AssetDatabase.GenerateUniqueAssetPath(path);
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string Sanitize(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }

    private static string ToAbsolute(string projectPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), projectPath);
    }
}
