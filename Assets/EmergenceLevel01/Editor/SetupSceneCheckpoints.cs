using System.IO;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public static class SetupSceneCheckpoints
    {
        [MenuItem("Tools/Emergence/Setup Scene Checkpoints")]
        public static void Build(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            string dir=SetupEmotionExpansion.Dir;
            Create(dir+"SceneCheckpoint.prefab",go=>{
                var checkpoint=go.AddComponent<SceneCheckpoint>();var collider=go.GetComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(1,2);collider.offset=Vector2.up;
                var marker=new GameObject("Marker");marker.transform.SetParent(go.transform,false);marker.transform.localPosition=Vector3.up*.2f;marker.transform.localScale=Vector3.one*.4f;
                var sr=marker.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergenceLevel01/Art/Tiles64/EmotionBlocks/sadness.png");sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergenceLevel01/Generated/LabSpriteUnlit.mat");sr.color=checkpoint.inactiveColor;sr.sortingOrder=25;checkpoint.indicator=sr;
            });
            Create(dir+"SceneResetController.prefab",go=>{var reset=go.AddComponent<SceneResetController>();reset.fadeConfig=AssetDatabase.LoadAssetAtPath<LevelFlowConfig>("Assets/EmergenceLevel01/Generated/LevelFlow.asset");});
            AssetDatabase.SaveAssets();
        }
        static void Create(string path,System.Action<GameObject> setup){if(File.Exists(path))return;var go=new GameObject(Path.GetFileNameWithoutExtension(path));try{setup(go);PrefabUtility.SaveAsPrefabAsset(go,path);}finally{Object.DestroyImmediate(go);}}
    }
}
