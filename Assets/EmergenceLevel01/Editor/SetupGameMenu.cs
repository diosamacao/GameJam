using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class SetupGameMenu
    {
        [MenuItem("Tools/Emergence/Create Emotion Switch Trigger Prefab")]
        public static void CreateEmotionTriggerPrefab(){
            const string path="Assets/EmergenceLevel01/Prefabs/EmotionSwitchTrigger.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path))return;
            var go=new GameObject("EmotionSwitchTrigger");go.AddComponent<EmotionSwitchTrigger>();
            var area=go.GetComponent<BoxCollider2D>();area.isTrigger=true;area.size=new Vector2(2,3);
            PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Emergence/Create Game Completion Prefab")]
        public static void CreateCompletionPrefab(){
            const string path="Assets/EmergenceLevel01/Prefabs/GameCompletionInteraction.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path))return;
            var go=new GameObject("GameCompletionInteraction");
            go.AddComponent<GameCompletionInteraction>().interactionHint="F 完成游戏";
            PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);AssetDatabase.SaveAssets();
        }
        public const string ScenePath="Assets/Scenes/MainMenu.unity";
        public const string ConfigPath="Assets/EmergenceLevel01/Resources/GameMenuConfig.asset";
        [MenuItem("Tools/Emergence/Setup Main Menu")]
        public static void Build(){
            CreateCompletionPrefab();
            var config=AssetDatabase.LoadAssetAtPath<GameMenuConfig>(ConfigPath);
            if(!config){
                config=ScriptableObject.CreateInstance<GameMenuConfig>();
                const string art="Assets/EmergenceLevel01/Art/Characters/";
                config.robotBody=AssetDatabase.LoadAssetAtPath<Sprite>(art+"OriginalBody/body_jump.png");
                config.secondRobotBody=AssetDatabase.LoadAssetAtPath<Sprite>(art+"OriginalBody/body_jump.png");
                config.doctor=AssetDatabase.LoadAssetAtPath<Sprite>(art+"scientist_work_1.png");
                config.robotFaces=AssetDatabase.LoadAssetAtPath<RobotEmotionConfig>("Assets/EmergenceLevel01/Generated/RobotEmotions.asset");
                AssetDatabase.CreateAsset(config,ConfigPath);
            }
            if(!System.IO.File.Exists(ScenePath)){
                var previous=SceneManager.GetActiveScene();if(string.IsNullOrEmpty(previous.path)){Debug.LogError("请先保存当前场景，再创建主菜单场景。");return;}var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                var go=new GameObject("Main Menu Camera");SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.backgroundColor=config.background;camera.clearFlags=CameraClearFlags.SolidColor;go.tag="MainCamera";go.transform.position=new Vector3(0,0,-10);
                EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);
            }
            var builds=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);builds.RemoveAll(item=>item.path==ScenePath);builds.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=builds.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
