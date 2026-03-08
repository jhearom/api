using System;
using MonoMod;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable All
#pragma warning disable 1591, 0108, 0169, 0649, 0626, 414, CS0626

namespace Modding.Patches
{
    [MonoModPatch("global::CameraController")]
    public class CameraController : global::CameraController
    {
        [MonoModIgnore]
        private global::GameManager gm;

        private string DescribeController()
        {
            try
            {
                if ((UnityEngine.Object)this == null)
                    return "<destroyed>";

                Scene scene = gameObject.scene;
                string sceneName = scene.IsValid() ? scene.name : "<invalid>";
                return $"{name} scene={sceneName} active={gameObject.activeInHierarchy}";
            }
            catch (MissingReferenceException)
            {
                return "<destroyed>";
            }
            catch (NullReferenceException)
            {
                return "<invalid>";
            }
        }

        private static Type ResolveEffectType(params string[] assemblyQualifiedNames)
        {
            foreach (string assemblyQualifiedName in assemblyQualifiedNames)
            {
                Type type = Type.GetType(assemblyQualifiedName);
                if (type != null)
                    return type;
            }

            return null;
        }

        private bool TrySetEffectEnabled(bool enabled, string displayName, params string[] assemblyQualifiedNames)
        {
            if ((UnityEngine.Object)this == null)
            {
                Debug.LogWarning($"Skipping {displayName} effect configuration because CameraController is no longer valid.");
                return false;
            }

            Type effectType = ResolveEffectType(assemblyQualifiedNames);
            if (effectType == null)
            {
                Debug.LogWarning($"CameraController could not resolve {displayName}; skipping effect configuration.");
                return false;
            }

            Behaviour effect;
            try
            {
                effect = GetComponent(effectType) as Behaviour;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning($"CameraController became invalid while configuring {displayName}; controller={DescribeController()}");
                return false;
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning($"CameraController was not fully bound while configuring {displayName}; controller={DescribeController()}");
                return false;
            }

            if (effect == null)
            {
                Debug.LogWarning($"CameraController is missing {displayName}; skipping effect configuration.");
                return false;
            }

            effect.enabled = enabled;
            return true;
        }

        [MonoModReplace]
        public void ApplyEffectConfiguration(bool isGameplayLevel, bool isBloomForced)
        {
            if ((UnityEngine.Object)this == null)
            {
                Debug.LogWarning("Skipping CameraController.ApplyEffectConfiguration because the controller is no longer valid.");
                return;
            }

            bool supportsFullEffects = Platform.Current.GraphicsTier > Platform.GraphicsTiers.Low;
            bool hasGameSettings = gm != null && gm.gameSettings != null;

            bool cameraNoiseEnabled = false;
            if (isGameplayLevel && supportsFullEffects && hasGameSettings)
            {
                cameraNoiseEnabled = gm.gameSettings.cameraNoise;
            }

            TrySetEffectEnabled(
                cameraNoiseEnabled,
                "FastNoise",
                "UnityStandardAssets.ImageEffects.FastNoise, Assembly-CSharp"
            );
            TrySetEffectEnabled(
                supportsFullEffects || isBloomForced,
                "BloomOptimized",
                "UnityStandardAssets.ImageEffects.BloomOptimized, Assembly-CSharp-firstpass"
            );
            TrySetEffectEnabled(
                supportsFullEffects,
                "BrightnessEffect",
                "BrightnessEffect, Assembly-CSharp"
            );
            TrySetEffectEnabled(
                true,
                "ColorCorrectionCurves",
                "UnityStandardAssets.ImageEffects.ColorCorrectionCurves, Assembly-CSharp-firstpass"
            );

            bool ditheringEnabled = false;
            if (supportsFullEffects && hasGameSettings)
            {
                ditheringEnabled = gm.gameSettings.dithering > 1;
            }

            if (supportsFullEffects && hasGameSettings && gm.gameSettings.dithering > 0)
            {
                Shader.EnableKeyword("DITHERING_NOISE");
            }
            else
            {
                Shader.DisableKeyword("DITHERING_NOISE");
            }

            TrySetEffectEnabled(
                ditheringEnabled,
                "DebandEffect",
                "DebandEffect, Assembly-CSharp"
            );
        }

        [MonoModReplace]
        public void FadeSceneIn()
        {
            if (Modding.Patches.SuppressPreloadException.GameCameras.TryGetInstance(out var gameCameras) &&
                gameCameras.cameraFadeFSM != null)
            {
                gameCameras.cameraFadeFSM.Fsm.Event("FADE SCENE IN");
                return;
            }

            Debug.LogWarning("Skipping CameraController.FadeSceneIn because GameCameras.cameraFadeFSM is not ready.");
        }
    }
}
