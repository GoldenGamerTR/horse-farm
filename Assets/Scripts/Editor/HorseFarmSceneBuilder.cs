using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HorseFarm.Core;
using HorseFarm.Player;
using HorseFarm.World;
using HorseFarm.Events;
using HorseFarm.UI;
using HorseFarm.Audio;

namespace HorseFarm.EditorTools
{
    [InitializeOnLoad]
    public static class HorseFarmSceneBuilder
    {
        static HorseFarmSceneBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                    (GameObject.Find("_HorseFarmSystems") == null || GameObject.Find("FenceWallCollider") == null))
                {
                    BuildCompleteScene();
                }
            };
        }

        [MenuItem("HorseFarm/Build Complete Game Scene")]
        public static void BuildCompleteScene()
        {
            UpgradeAllMaterialsToURP();
            AnimatorController horseAc = CreateHorseAnimatorController();
            GameObject wolfConfiguredPrefab = CreateConfiguredWolfPrefab();

            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            GameObject[] roots = scene.GetRootGameObjects();
            Camera mainCam = null;
            Light dirLight = null;

            foreach (GameObject r in roots)
            {
                if (r.GetComponent<Camera>() != null && mainCam == null)
                {
                    mainCam = r.GetComponent<Camera>();
                    continue;
                }
                if (r.GetComponent<Light>() != null && r.GetComponent<Light>().type == LightType.Directional && dirLight == null)
                {
                    dirLight = r.GetComponent<Light>();
                    continue;
                }
                Object.DestroyImmediate(r);
            }

            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.53f, 0.81f, 0.92f);
            mainCam.farClipPlane = 300f;

            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            dirLight.color = new Color(1f, 0.96f, 0.88f);
            dirLight.intensity = 1.25f;
            dirLight.shadows = LightShadows.Soft;

            // 1. Build Farm Environment using POLYGON_Farm assets
            Renderer groundRenderer = BuildFarmWorld();

            // 2. Build Interactive Stations (Barn, Merchant, Market, Region Gate)
            BuildInteractiveStations();

            // 3. Build Player Character using Malbers Animations/Cowboy
            PlayerController playerCtrl = BuildPlayerCharacter(mainCam.transform);

            // 4. Build Core Systems Manager Object
            GameObject sysRoot = new GameObject("_HorseFarmSystems");
            sysRoot.AddComponent<DataManager>();
            sysRoot.AddComponent<AudioManager>();

            RegionEnvironmentController regEnv = sysRoot.AddComponent<RegionEnvironmentController>();
            regEnv.groundRenderer = groundRenderer;
            regEnv.directionalLight = dirLight;

            GameManager gm = sysRoot.AddComponent<GameManager>();
            gm.horseAnimatorController = horseAc;
            gm.pastureCenter = new Vector3(0f, 0f, 2f);
            gm.pastureExtents = new Vector2(11.5f, 7.2f);
            PopulateHorseCoatPrefabs(gm);

            DangerEventManager dangerMgr = sysRoot.AddComponent<DangerEventManager>();
            dangerMgr.wolfPrefab = wolfConfiguredPrefab;
            dangerMgr.barnTargetPosition = new Vector3(0f, 0f, 17f);

            sysRoot.AddComponent<GameHUDAndMenus>();

            if (playerCtrl != null)
            {
                mainCam.transform.position = playerCtrl.transform.position + new Vector3(0f, 3.0f, -5.4f);
                mainCam.transform.LookAt(playerCtrl.transform.position + Vector3.up * 1.8f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[HorseFarmSceneBuilder] Complete Horse Breeding Simulator scene built and saved successfully!");
        }

        [MenuItem("HorseFarm/Upgrade Materials to URP")]
        public static void UpgradeAllMaterialsToURP()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            string[] searchFolders =
            {
                "Assets/PolygonFarm",
                "Assets/LPHorse_Version_2_9",
                "Assets/Malbers Animations",
                "Assets/Synty"
            };

            string[] guids = AssetDatabase.FindAssets("t:Material", searchFolders);
            int converted = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                bool needsUpgrade = mat.shader == null
                    || mat.shader.name == "Standard"
                    || mat.shader.name == "Standard (Specular setup)"
                    || mat.shader.name.Contains("InternalErrorShader")
                    || mat.shader.name.StartsWith("Legacy Shaders/")
                    || mat.shader.name.StartsWith("Unlit/")
                    || mat.shader.name.Contains("SyntyStudios");

                if (needsUpgrade)
                {
                    Texture mainTex = null;
                    if (mat.HasProperty("_MainTex")) mainTex = mat.GetTexture("_MainTex");
                    if (mainTex == null && mat.HasProperty("_BaseMap")) mainTex = mat.GetTexture("_BaseMap");
                    if (mainTex == null && mat.HasProperty("_Texture")) mainTex = mat.GetTexture("_Texture");
                    if (mainTex == null && mat.HasProperty("_Albedo")) mainTex = mat.GetTexture("_Albedo");

                    Color col = Color.white;
                    if (mat.HasProperty("_Color")) col = mat.GetColor("_Color");
                    else if (mat.HasProperty("_BaseColor")) col = mat.GetColor("_BaseColor");

                    Texture bump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                    mat.shader = urpLit;
                    if (mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                        mat.mainTexture = mainTex;
                    }
                    mat.SetColor("_BaseColor", col);
                    mat.color = col;
                    mat.SetFloat("_Smoothness", 0.15f);
                    if (bump != null)
                        mat.SetTexture("_BumpMap", bump);

                    EditorUtility.SetDirty(mat);
                    converted++;
                }
            }

            FixSyntyMaterialsIfMissingTexture("Assets/PolygonFarm", "Assets/PolygonFarm/Textures/PolygonFarm_Texture_01_A.png");
            FixSyntyMaterialsIfMissingTexture("Assets/Synty/PolygonDog", "Assets/Synty/PolygonDog/Textures/POLYGON_Dog_Texture_01.png");

            AssetDatabase.SaveAssets();
            Debug.Log($"[HorseFarmSceneBuilder] Upgraded {converted} materials to URP Lit.");
        }

        private static void FixSyntyMaterialsIfMissingTexture(string folder, string defaultTexPath)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            Texture2D defaultTex = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultTexPath);
            if (defaultTex == null)
            {
                string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                if (texGuids.Length > 0)
                    defaultTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(texGuids[0]));
            }
            if (defaultTex == null) return;

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            foreach (string g in matGuids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.ToLower().Contains("glass") || p.ToLower().Contains("water")) continue;
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null)
                {
                    m.SetTexture("_BaseMap", defaultTex);
                    m.mainTexture = defaultTex;
                    m.SetColor("_BaseColor", Color.white);
                    EditorUtility.SetDirty(m);
                }
            }
        }

        private static AnimatorController CreateHorseAnimatorController()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");

            string controllerPath = "Assets/Animations/HorseAnimator.controller";
            AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("IsGrazing", AnimatorControllerParameterType.Bool);
            ac.AddParameter("RearUp", AnimatorControllerParameterType.Trigger);

            AnimationClip clipIdle = ConfigureHorseClip("Assets/LPHorse_Version_2_9/Version_2_9/Animations/Primary_Cycles/Rig_Idle_Center.anim", true, true);
            AnimationClip clipWalk = ConfigureHorseClip("Assets/LPHorse_Version_2_9/Version_2_9/Animations/Primary_Cycles/Rig_Collected_Walk_RM.anim", true, true);
            AnimationClip clipTrot = ConfigureHorseClip("Assets/LPHorse_Version_2_9/Version_2_9/Animations/Primary_Cycles/Rig_Extended_Trot_RM.anim", true, true);
            AnimationClip clipGallop = ConfigureHorseClip("Assets/LPHorse_Version_2_9/Version_2_9/Animations/Primary_Cycles/Rig_Gallop_Steady_RootMotion.anim", true, true);

            // Ground Grazing clips: non-looping (loop = false) so they play once and finish!
            AnimationClip clipGrazeEnter = ConfigureHorseClip("Assets/LPHorse_Version_2_9/Version_2_9/Animations/Transitions/Rig_Graze_Transition.anim", false, true);
            AnimationClip clipGrazeMotion = ConfigureHorseClip("Assets/LPHorse_Version_2_9/Version_2_9/Animations/Additive_Animations/Full Body/Rig_Graze_Motion.anim", false, true);
            AnimationClip clipRear = ConfigureHorseClip("Assets/LPHorse_Version_2_9/Version_2_9/Animations/Primary_Actions/Rig_Rear_Up_Light_Left.anim", false, true);

            AnimatorStateMachine sm = ac.layers[0].stateMachine;

            AnimatorState stIdle = sm.AddState("Idle");
            stIdle.motion = clipIdle;
            sm.defaultState = stIdle;

            AnimatorState stWalk = sm.AddState("Walk");
            stWalk.motion = clipWalk;

            AnimatorState stTrot = sm.AddState("Trot");
            stTrot.motion = clipTrot;

            AnimatorState stGallop = sm.AddState("Gallop");
            stGallop.motion = clipGallop;

            // GrazeEnter (lowers head to ground) -> Graze (chews grass on ground): both slowed down by 10% (speed = 0.9f)
            AnimatorState stGrazeEnter = sm.AddState("GrazeEnter");
            stGrazeEnter.motion = clipGrazeEnter;
            stGrazeEnter.speed = 0.9f;

            AnimatorState stGraze = sm.AddState("Graze");
            stGraze.motion = clipGrazeMotion != null ? clipGrazeMotion : clipGrazeEnter;
            stGraze.speed = 0.9f;

            AnimatorState stRear = sm.AddState("RearUp");
            stRear.motion = clipRear;

            // Locomotion Transitions
            AddFloatTransition(stIdle, stWalk, "Speed", AnimatorConditionMode.Greater, 0.3f);
            AddFloatTransition(stWalk, stIdle, "Speed", AnimatorConditionMode.Less, 0.25f);

            AddFloatTransition(stWalk, stTrot, "Speed", AnimatorConditionMode.Greater, 1.5f);
            AddFloatTransition(stTrot, stWalk, "Speed", AnimatorConditionMode.Less, 1.4f);

            AddFloatTransition(stTrot, stGallop, "Speed", AnimatorConditionMode.Greater, 2.4f);
            AddFloatTransition(stGallop, stTrot, "Speed", AnimatorConditionMode.Less, 2.3f);

            // Grazing Flow:
            // 1) Idle -> GrazeEnter when IsGrazing == true
            var tToGrazeEnter = stIdle.AddTransition(stGrazeEnter);
            tToGrazeEnter.hasExitTime = false;
            tToGrazeEnter.duration = 0.25f;
            tToGrazeEnter.AddCondition(AnimatorConditionMode.If, 0f, "IsGrazing");

            // 2) GrazeEnter -> Graze automatically when head reaches the ground
            var tEnterToGraze = stGrazeEnter.AddTransition(stGraze);
            tEnterToGraze.hasExitTime = true;
            tEnterToGraze.exitTime = 0.95f;
            tEnterToGraze.duration = 0.15f;

            // 3) Graze -> Idle automatically when the ground grazing animation finishes (or if IsGrazing turns false)
            var tGrazeFinish = stGraze.AddTransition(stIdle);
            tGrazeFinish.hasExitTime = true;
            tGrazeFinish.exitTime = 0.98f;
            tGrazeFinish.duration = 0.45f;

            var tGrazeCancel = stGraze.AddTransition(stIdle);
            tGrazeCancel.hasExitTime = false;
            tGrazeCancel.duration = 0.35f;
            tGrazeCancel.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrazing");

            var tGrazeEnterCancel = stGrazeEnter.AddTransition(stIdle);
            tGrazeEnterCancel.hasExitTime = false;
            tGrazeEnterCancel.duration = 0.3f;
            tGrazeEnterCancel.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrazing");

            var tGrazeToWalk = stGraze.AddTransition(stWalk);
            tGrazeToWalk.hasExitTime = false;
            tGrazeToWalk.duration = 0.3f;
            tGrazeToWalk.AddCondition(AnimatorConditionMode.Greater, 0.3f, "Speed");

            var tGrazeEnterToWalk = stGrazeEnter.AddTransition(stWalk);
            tGrazeEnterToWalk.hasExitTime = false;
            tGrazeEnterToWalk.duration = 0.25f;
            tGrazeEnterToWalk.AddCondition(AnimatorConditionMode.Greater, 0.3f, "Speed");

            var tAnyRear = sm.AddAnyStateTransition(stRear);
            tAnyRear.hasExitTime = false;
            tAnyRear.canTransitionToSelf = false;
            tAnyRear.duration = 0.15f;
            tAnyRear.AddCondition(AnimatorConditionMode.If, 0f, "RearUp");

            var tRearExit = stRear.AddTransition(stIdle);
            tRearExit.hasExitTime = true;
            tRearExit.exitTime = 0.9f;
            tRearExit.duration = 0.2f;

            EditorUtility.SetDirty(ac);
            return ac;
        }

        private static void AddFloatTransition(AnimatorState from, AnimatorState to, string param, AnimatorConditionMode mode, float threshold)
        {
            var tr = from.AddTransition(to);
            tr.hasExitTime = false;
            tr.duration = 0.2f;
            tr.AddCondition(mode, threshold, param);
        }

        private static AnimationClip ConfigureHorseClip(string path, bool loop, bool zeroOutRootMotionDisplacement)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) return null;

            bool dirty = false;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime != loop)
            {
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                dirty = true;
            }

            if (zeroOutRootMotionDisplacement)
            {
                // Flatten Rig/Root_Motion position displacement so _RM animations play in-place without snapping back 6.8m on loop
                string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };
                foreach (string prop in props)
                {
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve("Rig/Root_Motion", typeof(Transform), prop);
                    AnimationCurve existing = AnimationUtility.GetEditorCurve(clip, binding);
                    if (existing != null && existing.length > 0)
                    {
                        float endTime = clip.length > 0f ? clip.length : 1f;
                        AnimationCurve flat = AnimationCurve.Linear(0f, 0f, endTime, 0f);
                        AnimationUtility.SetEditorCurve(clip, binding, flat);
                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(clip);
            }
            return clip;
        }

        private static GameObject CreateConfiguredWolfPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");

            string wolfControllerPath = "Assets/Animations/WolfAnimator.controller";
            AnimatorController wolfAc = AnimatorController.CreateAnimatorControllerAtPath(wolfControllerPath);
            wolfAc.AddParameter("Speed", AnimatorControllerParameterType.Float);
            wolfAc.AddParameter("Flinch", AnimatorControllerParameterType.Trigger);

            AnimationClip runClip = LoadClipFromFbx("Assets/Synty/PolygonDog/Animations/Locomotion/_POLYGON_Dog_Locomotion_Running.fbx");
            AnimationClip cowerClip = LoadClipFromFbx("Assets/Synty/PolygonDog/Animations/Actions_Standing/_POLYGON_Dog_Action_Standing_Cower.fbx");

            AnimatorStateMachine sm = wolfAc.layers[0].stateMachine;
            AnimatorState stRun = sm.AddState("Run");
            stRun.motion = runClip;
            sm.defaultState = stRun;

            if (cowerClip != null)
            {
                AnimatorState stFlinch = sm.AddState("Flinch");
                stFlinch.motion = cowerClip;

                var tFlinch = sm.AddAnyStateTransition(stFlinch);
                tFlinch.hasExitTime = false;
                tFlinch.canTransitionToSelf = false;
                tFlinch.duration = 0.1f;
                tFlinch.AddCondition(AnimatorConditionMode.If, 0f, "Flinch");

                var tBack = stFlinch.AddTransition(stRun);
                tBack.hasExitTime = true;
                tBack.exitTime = 0.85f;
                tBack.duration = 0.15f;
            }

            EditorUtility.SetDirty(wolfAc);

            GameObject sourceWolf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonDog/Prefabs/Dogs/SK_Dog_Wolf_01.prefab");
            if (sourceWolf == null)
                return null;

            GameObject tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourceWolf);
            Animator anim = tempInstance.GetComponentInChildren<Animator>();
            if (anim == null) anim = tempInstance.AddComponent<Animator>();
            anim.runtimeAnimatorController = wolfAc;
            anim.applyRootMotion = false;

            if (tempInstance.GetComponent<WolfController>() == null)
                tempInstance.AddComponent<WolfController>();

            string configuredPath = "Assets/Animations/ConfiguredWolf.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempInstance, configuredPath);
            Object.DestroyImmediate(tempInstance);
            return savedPrefab;
        }

        private static AnimationClip LoadClipFromFbx(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object o in assets)
            {
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            return null;
        }

        private static Renderer BuildFarmWorld()
        {
            GameObject worldRoot = new GameObject("FarmWorld");

            // Main Ground Plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FarmGround";
            ground.transform.SetParent(worldRoot.transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(26f, 1f, 26f);

            Renderer groundRend = ground.GetComponent<Renderer>();
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Animations/FarmGroundMat.mat");
            if (groundMat == null)
            {
                groundMat = new Material(urpLit);
                Color grassCol = new Color(0.37f, 0.55f, 0.26f);
                groundMat.color = grassCol;
                if (groundMat.HasProperty("_BaseColor")) groundMat.SetColor("_BaseColor", grassCol);
                if (groundMat.HasProperty("_Smoothness")) groundMat.SetFloat("_Smoothness", 0.08f);
                AssetDatabase.CreateAsset(groundMat, "Assets/Animations/FarmGroundMat.mat");
            }
            groundRend.sharedMaterial = groundMat;

            // Background Mountains Ring (POLYGON_Farm)
            PlacePrefab("Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Mountains_Grass_01.prefab", worldRoot.transform, new Vector3(0f, -1f, 95f), new Vector3(0f, 0f, 0f), new Vector3(1.5f, 1.4f, 1.5f), false);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Mountains_Grass_02.prefab", worldRoot.transform, new Vector3(-85f, -1f, 65f), new Vector3(0f, -55f, 0f), new Vector3(1.4f, 1.3f, 1.4f), false);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Mountains_Grass_02.prefab", worldRoot.transform, new Vector3(85f, -1f, 65f), new Vector3(0f, 55f, 0f), new Vector3(1.4f, 1.3f, 1.4f), false);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Mountains_Snow_01.prefab", worldRoot.transform, new Vector3(0f, -2f, -110f), new Vector3(0f, 180f, 0f), new Vector3(1.4f, 1.2f, 1.4f), false);

            // Clouds
            PlacePrefab("Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Cloud_01.prefab", worldRoot.transform, new Vector3(-35f, 32f, 45f), Vector3.zero, Vector3.one * 1.4f, false);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Cloud_02.prefab", worldRoot.transform, new Vector3(40f, 36f, 55f), Vector3.zero, Vector3.one * 1.5f, false);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Cloud_03.prefab", worldRoot.transform, new Vector3(10f, 30f, -50f), Vector3.zero, Vector3.one * 1.3f, false);

            // Secondary Farm Buildings (Farmhouse, Silos, Windmill, WaterTower, Shelter)
            PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_Farmhouse_01.prefab", worldRoot.transform, new Vector3(-25f, 0f, 16f), new Vector3(0f, 135f, 0f), Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_Silo_01.prefab", worldRoot.transform, new Vector3(12f, 0f, 22f), Vector3.zero, Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_Silo_Small_01.prefab", worldRoot.transform, new Vector3(16f, 0f, 21f), Vector3.zero, Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_WaterTower_01.prefab", worldRoot.transform, new Vector3(-14f, 0f, 23f), Vector3.zero, Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Windmill_01.prefab", worldRoot.transform, new Vector3(24f, 0f, 14f), new Vector3(0f, -35f, 0f), Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_Shelter_01.prefab", worldRoot.transform, new Vector3(-21f, 0f, 3f), new Vector3(0f, 90f, 0f), Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Well_01.prefab", worldRoot.transform, new Vector3(-20f, 0f, -5f), Vector3.zero, Vector3.one, true);

            // Pasture Props tucked cleanly along the back/side fence edges so center pasture is wide open for horses
            PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Trough_01.prefab", worldRoot.transform, new Vector3(-10f, 0f, 12.2f), Vector3.zero, Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Trough_01.prefab", worldRoot.transform, new Vector3(10f, 0f, 12.2f), Vector3.zero, Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Hay_Bale_Round_01.prefab", worldRoot.transform, new Vector3(-13.5f, 0f, 11.8f), new Vector3(0f, 25f, 0f), Vector3.one, true);
            PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Hay_Bale_Square_01.prefab", worldRoot.transform, new Vector3(13.5f, 0f, 11.8f), new Vector3(0f, -15f, 0f), Vector3.one, true);

            // Seamlessly connected 4-sided Wooden Paddock Fences
            BuildPastureFences(worldRoot.transform);

            // Trees, Rocks, Flowers around the outer Farm
            string[] treePaths =
            {
                "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_01.prefab",
                "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_02.prefab",
                "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_03.prefab",
                "Assets/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Apple_Grown_01.prefab",
                "Assets/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Cherry_Grown_01.prefab"
            };

            Vector3[] treePositions =
            {
                new Vector3(-24f, 0f, 2f), new Vector3(-28f, 0f, -10f), new Vector3(-22f, 0f, -20f),
                new Vector3(-14f, 0f, -25f), new Vector3(14f, 0f, -25f), new Vector3(24f, 0f, -18f),
                new Vector3(28f, 0f, -5f), new Vector3(26f, 0f, 6f), new Vector3(22f, 0f, 26f),
                new Vector3(-20f, 0f, 28f), new Vector3(-32f, 0f, 12f), new Vector3(32f, 0f, 18f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                string tp = treePaths[i % treePaths.Length];
                float scale = 0.9f + (i % 3) * 0.15f;
                PlacePrefab(tp, worldRoot.transform, treePositions[i], new Vector3(0f, i * 47f, 0f), Vector3.one * scale, true);
            }

            for (int i = 0; i < 16; i++)
            {
                float ang = i * (Mathf.PI * 2f / 16f);
                Vector3 pos = new Vector3(Mathf.Cos(ang) * 22f, 0f, Mathf.Sin(ang) * 20f);
                PlacePrefab("Assets/PolygonFarm/Prefabs/Environments/SM_Env_Flowers_01.prefab", worldRoot.transform, pos, new Vector3(0f, i * 30f, 0f), Vector3.one, false);
            }

            return groundRend;
        }

        // Builds a 100% visually connected, continuous wooden fence enclosure using exact mesh bounds
        private static void BuildPastureFences(Transform parent)
        {
            GameObject fenceRoot = new GameObject("PastureFences");
            fenceRoot.transform.SetParent(parent);

            string fencePath = "Assets/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_01.prefab";
            string polePath = "Assets/PolygonFarm/Prefabs/Props/SM_Prop_Fence_Wood_Pole_01.prefab";

            GameObject fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fencePath);
            GameObject polePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(polePath);
            if (fencePrefab == null) return;

            // Left wall: (-16, 0, -10) -> (-16, 0, 14)
            BuildConnectedFenceLine(fenceRoot.transform, new Vector3(-16f, 0f, -10f), new Vector3(-16f, 0f, 14f), fencePrefab, polePrefab);

            // Right wall: (16, 0, -10) -> (16, 0, 14)
            BuildConnectedFenceLine(fenceRoot.transform, new Vector3(16f, 0f, -10f), new Vector3(16f, 0f, 14f), fencePrefab, polePrefab);

            // Back Left wall (connects Left corner to Barn West wall): (-16, 0, 14) -> (-5.2, 0, 14)
            BuildConnectedFenceLine(fenceRoot.transform, new Vector3(-16f, 0f, 14f), new Vector3(-5.2f, 0f, 14f), fencePrefab, polePrefab);

            // Back Right wall (connects Barn East wall to Right corner): (5.2, 0, 14) -> (16, 0, 14)
            BuildConnectedFenceLine(fenceRoot.transform, new Vector3(5.2f, 0f, 14f), new Vector3(16f, 0f, 14f), fencePrefab, polePrefab);

            // Front Left wall (connects Left corner to Front Gate entrance): (-16, 0, -10) -> (-3.2, 0, -10)
            BuildConnectedFenceLine(fenceRoot.transform, new Vector3(-16f, 0f, -10f), new Vector3(-3.2f, 0f, -10f), fencePrefab, polePrefab);

            // Front Right wall (connects Front Gate entrance to Right corner): (3.2, 0, -10) -> (16, 0, -10)
            BuildConnectedFenceLine(fenceRoot.transform, new Vector3(3.2f, 0f, -10f), new Vector3(16f, 0f, -10f), fencePrefab, polePrefab);
        }

        private static void BuildConnectedFenceLine(Transform parent, Vector3 startPos, Vector3 endPos, GameObject fencePrefab, GameObject polePrefab)
        {
            MeshFilter mf = fencePrefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            Bounds b = mf.sharedMesh.bounds;
            bool xIsLongAxis = b.size.x >= b.size.z;
            float rawLength = xIsLongAxis ? b.size.x : b.size.z;
            if (rawLength < 0.1f) rawLength = 2.5f;

            Vector3 lineVec = endPos - startPos;
            lineVec.y = 0f;
            float totalDist = lineVec.magnitude;
            if (totalDist < 0.2f) return;

            Vector3 lineDir = lineVec / totalDist;
            int count = Mathf.Max(1, Mathf.RoundToInt(totalDist / rawLength));
            float segmentDist = totalDist / count;

            // Slightly overlap each fence rail by 3% so rails are visually embedded inside the connecting posts with zero gap
            float lengthScale = (segmentDist / rawLength) * 1.03f;

            // Compute rotation that aligns the mesh's long axis (+X or +Z) with lineDir
            Quaternion lineRot = Quaternion.LookRotation(lineDir, Vector3.up);
            Quaternion pieceRot = xIsLongAxis ? (lineRot * Quaternion.Euler(0f, -90f, 0f)) : lineRot;
            Vector3 pieceScale = xIsLongAxis ? new Vector3(lengthScale, 1f, 1f) : new Vector3(1f, 1f, lengthScale);

            // Place a connecting vertical post at the start corner
            PlaceCenteredPole(parent, polePrefab, startPos, lineRot);

            for (int i = 0; i < count; i++)
            {
                Vector3 segCenterWorld = startPos + lineDir * ((i + 0.5f) * segmentDist);

                GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, parent);
                piece.transform.rotation = pieceRot;
                piece.transform.localScale = pieceScale;

                // Offset pivot so the scaled mesh bounds center lands on segCenterWorld
                Vector3 scaledLocalCenter = Vector3.Scale(b.center, pieceScale);
                Vector3 horizOffset = pieceRot * new Vector3(scaledLocalCenter.x, 0f, scaledLocalCenter.z);
                piece.transform.position = segCenterWorld - horizOffset;

                // Place a vertical wooden post at the end of every segment so adjacent rails meet inside a post
                Vector3 jointPos = startPos + lineDir * ((i + 1) * segmentDist);
                PlaceCenteredPole(parent, polePrefab, jointPos, lineRot);
            }

            // Continuous BoxCollider wall for the entire fence segment
            GameObject wallColObj = new GameObject("FenceWallCollider");
            wallColObj.transform.SetParent(parent);
            wallColObj.transform.position = (startPos + endPos) * 0.5f + Vector3.up * 0.75f;
            wallColObj.transform.rotation = lineRot;
            BoxCollider wallBc = wallColObj.AddComponent<BoxCollider>();
            wallBc.size = new Vector3(0.35f, 1.5f, totalDist);
        }

        private static void PlaceCenteredPole(Transform parent, GameObject polePrefab, Vector3 worldPos, Quaternion rot)
        {
            if (polePrefab == null) return;
            GameObject pole = (GameObject)PrefabUtility.InstantiatePrefab(polePrefab, parent);
            pole.transform.rotation = rot;
            pole.transform.localScale = new Vector3(1.15f, 1.05f, 1.15f);

            MeshFilter pmf = pole.GetComponentInChildren<MeshFilter>();
            if (pmf != null && pmf.sharedMesh != null)
            {
                Vector3 c = Vector3.Scale(pmf.sharedMesh.bounds.center, pole.transform.localScale);
                Vector3 horizOffset = rot * new Vector3(c.x, 0f, c.z);
                pole.transform.position = worldPos - horizOffset;
            }
            else
            {
                pole.transform.position = worldPos;
            }
        }

        private static void BuildInteractiveStations()
        {
            GameObject stationsRoot = new GameObject("InteractiveStations");

            // 1. Barn & Farm Upgrades Station (North center of pasture: z = 20)
            GameObject barn = PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_Barn_01.prefab",
                stationsRoot.transform, new Vector3(0f, 0f, 20f), new Vector3(0f, 180f, 0f), Vector3.one, true);
            if (barn != null)
            {
                barn.name = "Station_Barn";
                GameObject triggerPoint = new GameObject("BarnInteractPoint");
                triggerPoint.transform.SetParent(barn.transform);
                triggerPoint.transform.position = new Vector3(0f, 0f, 14.5f);
                var st = triggerPoint.AddComponent<InteractableStation>();
                st.stationType = StationType.BarnUpgrade;
                st.promptKey = "prompt_barn_interact";
            }

            // 2. Merchant Stand (West side outside paddock front: x = -9, z = -13.5)
            GameObject merchantStand = PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_ProduceStand_01.prefab",
                stationsRoot.transform, new Vector3(-9f, 0f, -13.5f), new Vector3(0f, 25f, 0f), Vector3.one, true);
            if (merchantStand != null)
            {
                merchantStand.name = "Station_MerchantStand";
                PlacePrefab("Assets/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Male_Old_01.prefab",
                    merchantStand.transform, new Vector3(-9f, 0f, -14.7f), new Vector3(0f, 20f, 0f), Vector3.one, false);
                PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_GrainBag_Open_01.prefab",
                    merchantStand.transform, new Vector3(-7.2f, 0f, -13.0f), Vector3.zero, Vector3.one, false);
                PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Tool_Bucket_01.prefab",
                    merchantStand.transform, new Vector3(-10.5f, 0f, -12.8f), Vector3.zero, Vector3.one * 1.4f, false);

                var st = merchantStand.AddComponent<InteractableStation>();
                st.stationType = StationType.Merchant;
                st.promptKey = "prompt_merchant_interact";
            }

            // 3. Market Stand for Selling Horses (East side outside paddock front: x = 9, z = -13.5)
            GameObject marketStand = PlacePrefab("Assets/PolygonFarm/Prefabs/Buildings/SM_Bld_ProduceStand_01.prefab",
                stationsRoot.transform, new Vector3(9f, 0f, -13.5f), new Vector3(0f, -25f, 0f), Vector3.one, true);
            if (marketStand != null)
            {
                marketStand.name = "Station_MarketStand";
                PlacePrefab("Assets/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Female_01.prefab",
                    marketStand.transform, new Vector3(9f, 0f, -14.7f), new Vector3(0f, -20f, 0f), Vector3.one, false);
                PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_SignPost_01.prefab",
                    marketStand.transform, new Vector3(11.2f, 0f, -12.7f), new Vector3(0f, -30f, 0f), Vector3.one, false);

                var st = marketStand.AddComponent<InteractableStation>();
                st.stationType = StationType.Market;
                st.promptKey = "prompt_market_interact";
            }

            // 4. Region Travel Gate (South Road entrance: x = 0, z = -17.5)
            GameObject regionGate = PlacePrefab("Assets/PolygonFarm/Prefabs/Props/SM_Prop_Ranch_Sign_01.prefab",
                stationsRoot.transform, new Vector3(0f, 0f, -17.5f), Vector3.zero, Vector3.one, true);
            if (regionGate != null)
            {
                regionGate.name = "Station_RegionGate";
                var st = regionGate.AddComponent<InteractableStation>();
                st.stationType = StationType.RegionGate;
                st.promptKey = "prompt_region_gate";
            }
        }

        private static PlayerController BuildPlayerCharacter(Transform camTransform)
        {
            GameObject playerRoot = new GameObject("Player_Cowboy");
            playerRoot.transform.position = new Vector3(0f, 0.05f, -6f);
            playerRoot.transform.rotation = Quaternion.identity;

            CharacterController cc = playerRoot.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.38f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;

            GameObject cowboyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Malbers Animations/Cowboy/Prefabs/Cowboy.prefab");
            if (cowboyPrefab == null)
                cowboyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Malbers Animations/Cowboy/Model/Cowboy 1.1.prefab");

            if (cowboyPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(cowboyPrefab, playerRoot.transform);
                model.name = "CowboyModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null)
                    anim.enabled = false;
            }

            PlayerController pCtrl = playerRoot.AddComponent<PlayerController>();
            pCtrl.cameraTransform = camTransform;
            playerRoot.AddComponent<InteractionController>();

            return pCtrl;
        }

        private static void PopulateHorseCoatPrefabs(GameManager gm)
        {
            gm.horseCoatPrefabs.Clear();
            string baseDir = "Assets/LPHorse_Version_2_9/Version_2_9/Prefabs/Not Saddled/";
            string[] coats =
            {
                "Brown_01", "Brown_02", "Chestnut_01", "Chestnut_02",
                "Tan_01", "Tan_02", "Black_01", "Black_02", "White_01", "White_02"
            };

            foreach (string c in coats)
            {
                string path = $"{baseDir}LP_Horse_Mesh_2_9_{c} Variant.prefab";
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null)
                {
                    gm.horseCoatPrefabs.Add(new HorseCoatPrefabEntry { coatId = c, prefab = p });
                }
            }
        }

        private static GameObject PlacePrefab(string assetPath, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, bool addCollider)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.Euler(euler);
            inst.transform.localScale = scale;

            if (addCollider && inst.GetComponentInChildren<Collider>() == null)
            {
                MeshFilter mf = inst.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    BoxCollider bc = mf.gameObject.AddComponent<BoxCollider>();
                    bc.center = mf.sharedMesh.bounds.center;
                    bc.size = mf.sharedMesh.bounds.size;
                }
            }
            return inst;
        }
    }
}
