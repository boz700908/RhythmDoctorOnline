using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDOnline.Utils
{
    public class AssetBundleManager
    {
        public static AssetBundleManager instance = new();
        
        public AssetBundle sceneBundle;        
        public AssetBundle resourcesBundle;
        
        private readonly string LoadPath = Path.Combine(Application.dataPath, "AssetBundles");
        private AssetBundleManager()
        {
            try
            {
                instance = this;
                sceneBundle = AssetBundle.GetAllLoadedAssetBundles().ToArray().First(a => a.name.Contains("scenes"));
                resourcesBundle = AssetBundle.GetAllLoadedAssetBundles().ToArray().First(a => a.name.Contains("resources"));
            
                if (sceneBundle == null)
                {
                    sceneBundle = AssetBundle.LoadFromFile(Path.Combine(LoadPath, "adofaiol.scenes.assets.bundle"));
                }
                if (resourcesBundle == null)
                {
                    resourcesBundle = AssetBundle.LoadFromFile(Path.Combine(LoadPath, "adofaiol.resources.assets.bundle"));
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
        
        public T LoadAsset<T>(string pathOrName) where T : UnityEngine.Object
        {
            T result = null;
            result = resourcesBundle.LoadAsset<T>(pathOrName);
            if (result == null)
            {
                result = resourcesBundle.LoadAsset<T>(resourcesBundle.GetAllAssetNames().First(a => a.Contains(pathOrName)));
            }
            return result;
        }

    }
}