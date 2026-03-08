using System;
using MonoMod;
using UnityEngine;

// ReSharper disable All
#pragma warning disable 1591, 0108, 0169, 0649, 0414, CS0626

namespace Modding.Patches
{
    [MonoModPatch("global::ObjectPool")]
    public class ObjectPool
    {
        [MonoModIgnore]
        private static ObjectPool _instance;

        private static extern GameObject orig_Spawn
        (
            GameObject prefab,
            Transform parent,
            Vector3 position,
            Quaternion rotation
        );

        public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            try
            {
                GameObject obj = orig_Spawn(prefab, parent, position, rotation);
                return ModHooks.OnObjectPoolSpawn(obj);
            }
            catch (NullReferenceException) when (!ModLoader.LoadState.HasFlag(ModLoader.ModLoadState.Preloaded))
            {
                return null;
            }
        }

        public static extern void orig_CreatePool(GameObject prefab, int initialPoolSize);

        public static void CreatePool(GameObject prefab, int initialPoolSize)
        {
            try
            {
                orig_CreatePool(prefab, initialPoolSize);
            }
            catch (NullReferenceException) when (!ModLoader.LoadState.HasFlag(ModLoader.ModLoadState.Preloaded))
            { }
        }

        public extern void orig_Awake();

        [MonoModIgnore]
        public extern Transform get_transform();

        [MonoModIgnore]
        public extern GameObject get_gameObject();

        public void Awake()
        {
            if (_instance == null)
            {
                _instance = this;

                Transform root = get_transform().root;
                UnityEngine.Object.DontDestroyOnLoad(root != null ? root.gameObject : get_gameObject());
                return;
            }

            orig_Awake();
        }
    }
}
