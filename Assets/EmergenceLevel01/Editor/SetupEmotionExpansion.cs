using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class SetupEmotionExpansion
    {
        const string Root="Assets/EmergenceLevel01/";
        public const string Dir=Root+"Prefabs/EmotionExpansion/";
        [MenuItem("Tools/Emergence/Setup Emotion Expansion")]
        public static void Build(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop play first");
            Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
            var config=AssetDatabase.LoadAssetAtPath<RobotEmotionConfig>(Root+"Generated/RobotEmotions.asset");
            config.neutral=Head("neutral");config.loading=Head("loading");config.memory=Head("memory");config.guardian=Head("guardian");config.courage=Head("courage");EditorUtility.SetDirty(config);
            config.neutralScreen=Screen("neutral");config.loadingScreen=Screen("loading");config.memoryScreen=Screen("memory");config.guardianScreen=Screen("guardian");config.courageScreen=Screen("courage");
            string playerPath=Root+"Prefabs/Characters/PlayerRobot.prefab";
            var player=PrefabUtility.LoadPrefabContents(playerPath);try{Configure(player);PrefabUtility.SaveAsPrefabAsset(player,playerPath);}finally{PrefabUtility.UnloadPrefabContents(player);}
            string npcPath=Dir+"EmotionNPC.prefab";
            if(!File.Exists(npcPath)){
                var npc=PrefabUtility.LoadPrefabContents(playerPath);try{
                    npc.name="EmotionNPC";npc.tag="Untagged";var actor=npc.GetComponent<RobotEmotionGameplay>();actor.isNpc=true;actor.keyboardInput=false;actor.showSwitchProgress=false;
                    var keyboard=npc.GetComponent<RobotKeyboardInput>();keyboard.enabled=false;var test=npc.GetComponent<RobotEmotionKeyboardTest>();if(test)test.enabled=false;
                    npc.GetComponent<RobotMotor2D>().externallyDriven=true;var body=npc.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;
                    npc.AddComponent<EmotionNpc>();PrefabUtility.SaveAsPrefabAsset(npc,npcPath);
                }finally{PrefabUtility.UnloadPrefabContents(npc);}
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Generated/LabSpriteUnlit.mat");
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Tiles64/EmotionBlocks/reinforced.png");
            Create("MovingPlatform",go=>{var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=20;go.transform.localScale=new Vector3(3,.5f,1);go.AddComponent<BoxCollider2D>();var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;go.AddComponent<LocalTimeTarget>().targetBody=body;go.AddComponent<LocalMovingPlatform>();});
            Create("DangerZone",go=>{go.AddComponent<BoxCollider2D>().isTrigger=true;go.AddComponent<DangerZone>();var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.color=new Color(1,.3f,.2f,.4f);sr.sortingOrder=10;});
            Create("MemoryObject",go=>{var item=go.AddComponent<MemoryInteractable>();var off=new GameObject("BeforeMemory");off.transform.SetParent(go.transform,false);var on=new GameObject("AfterMemory");on.transform.SetParent(go.transform,false);foreach(var child in new[]{off,on}){var sr=child.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=20;sr.color=child==off?new Color(.5f,.5f,.65f):new Color(.5f,1,.85f);}item.stateOffVisual=off;item.stateOnVisual=on;on.SetActive(false);});
            Create("MemoryGridController",go=>go.AddComponent<MemoryGridTarget>());
            string lab=Root+"Generated/Level01_Laboratory.prefab";
            if(File.Exists(lab)){var content=PrefabUtility.LoadPrefabContents(lab);try{foreach(var motor in content.GetComponentsInChildren<RobotMotor2D>(true))Configure(motor.gameObject);PrefabUtility.SaveAsPrefabAsset(content,lab);}finally{PrefabUtility.UnloadPrefabContents(content);}}
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"})){
                string path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("/Backup/") || !Path.GetFileName(path).StartsWith("Level"))continue;
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())foreach(var motor in root.GetComponentsInChildren<RobotMotor2D>(true))Configure(motor.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid() && active.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("EMOTION_EXPANSION_READY");
        }
        static Sprite Head(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Characters/OriginalBody/head_"+name+".png");
        static Sprite Screen(string name){string path=Root+"Art/Characters/OriginalBody/screen_"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
        static void Configure(GameObject go){if(!go.GetComponent<RobotEmotionGameplay>())return;if(!go.GetComponent<EmotionResonance>())go.AddComponent<EmotionResonance>();if(!go.GetComponent<EmotionTransfer>())go.AddComponent<EmotionTransfer>();}
        static void Create(string name,System.Action<GameObject> configure){string path=Dir+name+".prefab";if(File.Exists(path))return;var go=new GameObject(name);try{configure(go);PrefabUtility.SaveAsPrefabAsset(go,path);}finally{Object.DestroyImmediate(go);}}
    }
}
