using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDOnline.Utils
{
    public class AssetBundleManager
    {
        public static AssetBundleManager instance = new();
        
        public AssetBundle sceneBundle;        
        public AssetBundle resourcesBundle;
        
        private readonly string LoadPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private AssetBundleManager()
        {
            try
            {
                instance = this;
                sceneBundle = AssetBundle.GetAllLoadedAssetBundles()
                    .ToArray()
                    .First(a => a.name.Contains("rdol.scenes"));
                resourcesBundle = AssetBundle.GetAllLoadedAssetBundles()
                    .ToArray()
                    .First(a => a.name.Contains("rdol.resources"));
            
                if (sceneBundle == null)
                {
                    sceneBundle = AssetBundle.LoadFromFile(Path.Combine(LoadPath, "rdol.scenes.assets"));
                }
                if (resourcesBundle == null)
                {
                    resourcesBundle = AssetBundle.LoadFromFile(Path.Combine(LoadPath, "rdol.resources.assets"));
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
        
        public T LoadAsset<T>(string pathOrName) where T : UnityEngine.Object
        {
            T result = Resources.Load<T>(pathOrName);
            if (result != null) return result;
            result = resourcesBundle.LoadAsset<T>(pathOrName);
            if (result == null)
            {
                result = resourcesBundle.LoadAsset<T>(resourcesBundle.GetAllAssetNames().First(a => a.Contains(pathOrName)));
            }
            return result;
        }

    }
}