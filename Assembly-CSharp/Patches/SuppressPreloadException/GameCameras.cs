using MonoMod;
using UnityEngine;

// ReSharper disable all
#pragma warning disable 1591, 649, 414, 169, CS0108, CS0626

namespace Modding.Patches.SuppressPreloadException
{
    [MonoModPatch("global::GameCameras")]
    public class GameCameras : global::GameCameras
    {
        [MonoModIgnore]
        private static GameCameras _instance;

        public static bool TryGetInstance(out GameCameras instance)
        {
            if (GameCameras._instance == null)
            {
                GameCameras._instance = UnityEngine.Object.FindObjectOfType<GameCameras>();
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
    }
}
