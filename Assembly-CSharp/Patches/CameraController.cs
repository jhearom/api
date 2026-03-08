using System;
using MonoMod;
using UnityEngine;

// ReSharper disable All
#pragma warning disable 1591, 0108, 0169, 0649, 0626, 414, CS0626

namespace Modding.Patches
{
    [MonoModPatch("global::CameraController")]
    public class CameraController : global::CameraController
    {
        [MonoModIgnore]
        private global::GameManager gm;

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
            Type effectType = ResolveEffectType(assemblyQualifiedNames);
            if (effectType == null)
            {
                Debug.LogWarning($"CameraController could not resolve {displayName}; skipping effect configuration.");
                return false;
            }

            Behaviour effect = GetComponent(effectType) as Behaviour;
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
    }
}
