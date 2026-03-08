using UnityEngine;

namespace Modding.Menu
{
    internal static class MenuPersistence
    {
        internal static void DontDestroyRoot(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            Transform root = gameObject.transform.root;
            GameObject.DontDestroyOnLoad(root != null ? root.gameObject : gameObject);
        }
    }
}
