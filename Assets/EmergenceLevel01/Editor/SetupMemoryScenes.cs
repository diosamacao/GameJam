using System.IO;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public static class SetupMemoryScenes
    {
        public const string DirectoryPath="Assets/EmergenceLevel01/MemoryScenes";
        [MenuItem("Tools/Emergence/Setup Memory Scenes")]
        public static void Build(){
            Directory.CreateDirectory(DirectoryPath);AssetDatabase.Refresh();
            string configPath=DirectoryPath+"/MemoryScene_Default.asset";
            var config=AssetDatabase.LoadAssetAtPath<MemorySceneConfig>(configPath);
            if(!config){config=ScriptableObject.CreateInstance<MemorySceneConfig>();AssetDatabase.CreateAsset(config,configPath);}
            string prefabPath=DirectoryPath+"/MemorySceneTrigger.prefab";
            if(!File.Exists(prefabPath)){
                var go=new GameObject("MemorySceneTrigger");var trigger=go.AddComponent<MemorySceneTrigger>();trigger.memory=config;
                var collider=go.GetComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(2,3);
                PrefabUtility.SaveAsPrefabAsset(go,prefabPath);Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
