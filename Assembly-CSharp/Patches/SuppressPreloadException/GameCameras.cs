using System;
using MonoMod;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable all
#pragma warning disable 1591, 649, 414, 169, CS0108, CS0626

namespace Modding.Patches.SuppressPreloadException
{
    [MonoModPatch("global::GameCameras")]
    public class GameCameras : global::GameCameras
    {
        [MonoModIgnore]
        private static GameCameras _instance;

        [MonoModIgnore]
        private global::GameManager gm;

        [MonoModIgnore]
        private bool init;

        [MonoModIgnore]
        private extern void SetupGameRefs();

        [MonoModIgnore]
        private extern void DisableHUDCamIfAllowed();

        [MonoModIgnore]
        public extern SceneParticlesController get_sceneParticles();

        private static string DescribeCandidate(GameCameras candidate)
        {
            if (candidate == null)
                return "<null>";

            Scene scene = candidate.gameObject.scene;
            string sceneName = scene.IsValid() ? scene.name : "<invalid>";
            return $"{candidate.name} scene={sceneName} loaded={scene.isLoaded} active={candidate.gameObject.activeInHierarchy} main={(candidate.mainCamera != null)} hud={(candidate.hudCamera != null)} ctrl={(candidate.cameraController != null)} target={(candidate.cameraTarget != null)} fade={(candidate.cameraFadeFSM != null)} soul={(candidate.soulOrbFSM != null)} color={(candidate.sceneColorManager != null)}";
        }

        private static bool IsLoadedSceneObject(GameCameras candidate)
        {
            if (candidate == null)
                return false;

            Scene scene = candidate.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }

        private static GameCameras FindLoadedSceneFallback()
        {
            GameCameras loadedCandidate = null;

            foreach (GameCameras candidate in Resources.FindObjectsOfTypeAll<GameCameras>())
            {
                if (!IsLoadedSceneObject(candidate))
                    continue;

                if (candidate.gameObject.activeInHierarchy)
                    return candidate;

                loadedCandidate ??= candidate;
            }

            return loadedCandidate;
        }

        private global::CameraController ResolveCameraController()
        {
            if ((UnityEngine.Object)this == null)
                return null;

            if (cameraController != null)
                return cameraController;

            global::CameraController resolved = null;
            try
            {
                if (mainCamera != null)
                    resolved = mainCamera.GetComponent<global::CameraController>();

                if (resolved == null && hudCamera != null)
                    resolved = hudCamera.GetComponent<global::CameraController>();

                if (resolved == null)
                    resolved = GetComponentInChildren<global::CameraController>(true);
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning("GameCameras.ResolveCameraController encountered an invalid camera host during scene init.");
                return null;
            }

            if (resolved != null)
                cameraController = resolved;

            return resolved;
        }

        private global::CameraTarget ResolveCameraTarget()
        {
            if ((UnityEngine.Object)this == null)
                return null;

            if (cameraTarget != null)
                return cameraTarget;

            global::CameraTarget resolved = null;
            global::CameraController resolvedCameraController = ResolveCameraController();
            if (resolvedCameraController != null)
                resolved = resolvedCameraController.camTarget;

            if (resolved == null)
            {
                try
                {
                    resolved = GetComponentInChildren<global::CameraTarget>(true);
                }
                catch (NullReferenceException)
                {
                    Debug.LogWarning("GameCameras.ResolveCameraTarget encountered an invalid camera host during scene init.");
                    return null;
                }
            }

            if (resolved != null)
                cameraTarget = resolved;

            return resolved;
        }

        public static bool TryGetInstance(out GameCameras instance)
        {
            bool recovered = false;

            if (GameCameras._instance == null)
            {
                GameCameras._instance = UnityEngine.Object.FindObjectOfType<GameCameras>();
                recovered = GameCameras._instance != null;
            }

            if (GameCameras._instance == null)
            {
                GameCameras._instance = FindLoadedSceneFallback();
                recovered = GameCameras._instance != null;
            }

            instance = GameCameras._instance;
            if (instance != null && recovered)
            {
                Transform root = instance.transform.root;
                Debug.Log($"[MAPI DDOL] GameCameras.TryGetInstance/recovered target={(root != null ? root.gameObject.name : instance.gameObject.name)} isRoot={ReferenceEquals(instance.transform, root)} root={(root != null ? root.name : "<null>")}");
                UnityEngine.Object.DontDestroyOnLoad(root != null ? root.gameObject : instance.gameObject);
            }

            return instance != null;
        }

        public static GameCameras instance
        {
            get
            {
                if (!TryGetInstance(out GameCameras instance))
                {
                    Debug.LogError($"Couldn't find GameCameras, make sure one exists in the scene. Active scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                }

                return instance;
            }
        }

        public void SceneInit()
        {
            if (this == GameCameras._instance)
                StartScene();
        }

        public void MoveMenuToHUDCamera()
        {
            if (mainCamera == null || hudCamera == null)
            {
                Debug.LogWarning("Skipping GameCameras.MoveMenuToHUDCamera because camera refs are not ready.");
                return;
            }

            if (!hudCamera.gameObject.activeSelf)
                hudCamera.gameObject.SetActive(true);

            int mainCameraMask = mainCamera.cullingMask;
            int hudCameraMask = hudCamera.cullingMask;

            global::UIManager uiManager = global::UIManager.instance;
            if (uiManager != null && uiManager.UICanvas != null)
            {
                uiManager.UICanvas.worldCamera = hudCamera;
                uiManager.UICanvas.renderMode = RenderMode.ScreenSpaceCamera;
            }

            Debug.Log(
                $"[MAPI CAM] MoveMenuToHUDCamera " +
                $"scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} " +
                $"mainCamera={(mainCamera != null ? mainCamera.name : "<null>")} " +
                $"hudCamera={(hudCamera != null ? hudCamera.name : "<null>")} " +
                $"hudCameraActive={(hudCamera != null && hudCamera.gameObject != null ? hudCamera.gameObject.activeInHierarchy.ToString() : "<null>")} " +
                $"uiCanvasWorldCamera={(uiManager != null && uiManager.UICanvas != null && uiManager.UICanvas.worldCamera != null ? uiManager.UICanvas.worldCamera.name : "<null>")} " +
                $"uiCanvasRenderMode={(uiManager != null && uiManager.UICanvas != null ? uiManager.UICanvas.renderMode.ToString() : "<null>")}"
            );

            mainCamera.cullingMask = mainCameraMask ^ 134217728;
            hudCamera.cullingMask = hudCameraMask | 134217728;
        }

        public void StartScene()
        {
            if (!init)
                SetupGameRefs();

            if (gm == null)
                return;

            if (!gm.IsGameplayScene())
            {
                if (gm.ShouldKeepHUDCameraActive())
                {
                    MoveMenuToHUDCamera();

                    if (hudCamera != null && !hudCamera.gameObject.activeSelf)
                        hudCamera.gameObject.SetActive(true);
                }
                else if (hudCamera != null)
                {
                    DisableHUDCamIfAllowed();
                }
            }

            if (gm.IsMenuScene())
            {
                if (cameraController != null)
                    Extensions.SetPosition2D(cameraController.transform, 14.6f, 8.5f);

                return;
            }

            if (gm.IsCinematicScene())
            {
                if (cameraController != null)
                    Extensions.SetPosition2D(cameraController.transform, 14.6f, 8.5f);

                return;
            }

            if (gm.IsNonGameplayScene())
            {
                if (cameraController != null)
                {
                    if (gm.IsBossDoorScene())
                    {
                        Extensions.SetPosition2D(cameraController.transform, 17.5f, 17.5f);
                    }
                    else if (InGameCutsceneInfo.IsInCutscene)
                    {
                        Extensions.SetPosition2D(cameraController.transform, InGameCutsceneInfo.CameraPosition);
                    }
                    else
                    {
                        Extensions.SetPosition2D(cameraController.transform, 14.6f, 8.5f);
                    }
                }

                return;
            }

            if (hudCamera != null && !hudCamera.gameObject.activeSelf)
                hudCamera.gameObject.SetActive(true);

            Debug.Log(
                $"[MAPI CAM] GameplayStart " +
                $"scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} " +
                $"hudCamera={(hudCamera != null ? hudCamera.name : "<null>")} " +
                $"hudCameraActive={(hudCamera != null && hudCamera.gameObject != null ? hudCamera.gameObject.activeInHierarchy.ToString() : "<null>")}"
            );

            global::CameraController liveCameraController = ResolveCameraController();
            if (liveCameraController != null)
            {
                liveCameraController.SceneInit();
            }
            else
            {
                Debug.LogWarning($"GameCameras has no live CameraController during StartScene: {DescribeCandidate(this)}");
            }

            global::CameraTarget liveCameraTarget = ResolveCameraTarget();
            if (liveCameraTarget != null)
                liveCameraTarget.SceneInit();

            var liveSceneColorManager = sceneColorManager;
            if (liveSceneColorManager != null)
                liveSceneColorManager.SceneInit();

            var sceneParticles = get_sceneParticles();
            if (sceneParticles != null)
                sceneParticles.SceneInit();

            if (cameraFadeFSM == null || soulOrbFSM == null)
            {
                Debug.LogWarning($"GameCameras incomplete after StartScene: {DescribeCandidate(this)}");
            }
        }
    }
}
