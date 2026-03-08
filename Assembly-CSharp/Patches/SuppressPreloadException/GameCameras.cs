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

        private static bool IsLoadedSceneObject(GameCameras candidate)
        {
            if (candidate == null)
                return false;

            Scene scene = candidate.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }

        private static int ScoreCandidate(GameCameras candidate)
        {
            if (!IsLoadedSceneObject(candidate))
                return int.MinValue;

            int score = 0;

            if (candidate.gameObject.activeInHierarchy)
                score += 16;

            if (candidate.mainCamera != null)
                score += 8;

            if (candidate.hudCamera != null)
                score += 8;

            if (candidate.cameraController != null)
                score += 4;

            if (candidate.cameraTarget != null)
                score += 4;

            if (candidate.cameraFadeFSM != null)
                score += 2;

            if (candidate.sceneColorManager != null)
                score += 1;

            return score;
        }

        private static void ConsiderCandidate(ref GameCameras best, GameCameras candidate)
        {
            if (!IsLoadedSceneObject(candidate))
                return;

            if (best == null || ScoreCandidate(candidate) > ScoreCandidate(best))
                best = candidate;
        }

        public static bool TryGetInstance(out GameCameras instance)
        {
            if (ScoreCandidate(GameCameras._instance) < 24)
            {
                GameCameras best = null;

                ConsiderCandidate(ref best, GameCameras._instance);
                ConsiderCandidate(ref best, UnityEngine.Object.FindObjectOfType<GameCameras>());

                foreach (GameCameras candidate in Resources.FindObjectsOfTypeAll<GameCameras>())
                    ConsiderCandidate(ref best, candidate);

                GameCameras._instance = best;
            }

            instance = GameCameras._instance;
            return instance != null;
        }

        public static GameCameras instance
        {
            get
            {
                if (!TryGetInstance(out GameCameras instance))
                {
                    Debug.LogError("Couldn't find GameCameras, make sure one exists in the scene.");
                }

                return instance;
            }
        }

        public void SceneInit()
        {
            if (TryGetInstance(out GameCameras instance) && instance != null && instance != this)
            {
                instance.StartScene();
                return;
            }

            StartScene();
        }

        public void MoveMenuToHUDCamera()
        {
            if (TryGetInstance(out GameCameras instance) && instance != null && instance != this)
            {
                instance.MoveMenuToHUDCamera();
                return;
            }

            if (mainCamera == null || hudCamera == null)
            {
                Debug.LogWarning("Skipping GameCameras.MoveMenuToHUDCamera because camera refs are not ready.");
                return;
            }

            int mainCameraMask = mainCamera.cullingMask;
            int hudCameraMask = hudCamera.cullingMask;

            global::UIManager uiManager = global::UIManager.instance;
            if (uiManager != null && uiManager.UICanvas != null)
            {
                uiManager.UICanvas.worldCamera = hudCamera;
                uiManager.UICanvas.renderMode = RenderMode.ScreenSpaceCamera;
            }

            mainCamera.cullingMask = mainCameraMask ^ 134217728;
            hudCamera.cullingMask = hudCameraMask | 134217728;
        }

        public void StartScene()
        {
            if (TryGetInstance(out GameCameras instance) && instance != null && instance != this)
            {
                instance.StartScene();
                return;
            }

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

            cameraController?.SceneInit();
            cameraTarget?.SceneInit();
            sceneColorManager?.SceneInit();
            get_sceneParticles()?.SceneInit();
        }
    }
}
