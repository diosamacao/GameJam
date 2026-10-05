using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
namespace Emergence.Level01.Editor
{
    public static class SetupDialogueSystem
    {
        const string Root="Assets/EmergenceLevel01/";
        [MenuItem("Tools/Emergence/Setup Dialogue System")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop play first");
            Directory.CreateDirectory(Root+"Dialogue");
            var opening=Create("01_Opening",new[]{
                L("博士","终于完成了！"),L("博士","情感数据核心！！"),L("博士","只要把它搭载到机械上，他们就能产生情感了……<pause=1>吧？",0),
                L("博士","为此我还给他们换上了能够反映情感的智能表情头部呢！！"),L("博士","虽然只有两个机械具备了这样的条件。"),L("博士","不管怎样，实践出真知！"),L("博士","启动！！")
            },"结束后：右3蓝色舱门打开，玩家走出并恢复操控。靠近博士一格时触发02_Greeting。演出由DialogueTrigger.onCompleted或Director.Finished挂接。");
            var greeting=Create("02_Greeting",new[]{
                L("","早上好，博士。"),L("博士","噢！你好！你是第一个搭载了情感数据核心的机械，就叫你伊吧！",0,"doctor_face_player"),
                L("伊","伊……<pause=1>这是我的代号吗？",0),L("博士","对！你就是机械体，代号：伊。")
            },"首句不显示姓名。博士变idle并转身面对玩家。结束后右2蓝色舱门打开，配角走出，再触发03_ExperimentBriefing。");
            var briefing=Create("03_ExperimentBriefing",new[]{
                L("博士","第二个机械也成功启动了！太好了！",0,"ni_walk_to_player"),L("博士","你的代号就叫尼吧！"),L("尼","好的，博士。"),
                L("博士","现在需要你们进入那边的实验舱里，进行情感模拟场景实验。"),L("博士","填充你们的情感数据，让你们能自发的涌现情感。"),L("博士","当然，这是最理想的情况。"),
                L("博士","实际会变成什么样我也不知道！"),L("博士","去吧，祝你们成功！"),L("伊&尼","明白了。")
            },"开场尼移动至玩家身旁，横向距离2。结束后恢复玩家操作，尼移至右实验舱等待；玩家进入左实验舱中间触发04_ChamberConversation。");
            var chamber=Create("04_ChamberConversation",new[]{L("尼","你好，你是叫伊对吧？",0,"ni_face_player"),L("伊","你好，我的代号是伊。"),L("尼","知道了，代号伊，我们进入实验舱吧。",0,"ni_smile")},
                "结束后：伊显示省略号1秒、笑脸1秒、进入实验舱；淡出黑屏后切换实验关卡。当前只提供演出挂接点，未自动驱动角色、舱门或转场。");
            string prefabPath=Root+"Prefabs/Gameplay/DialogueSystem.prefab";
            if(!File.Exists(prefabPath)){
                var go=new GameObject("DialogueSystem",typeof(DialogueDirector));try{PrefabUtility.SaveAsPrefabAsset(go,prefabPath);}finally{Object.DestroyImmediate(go);}
            }
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"})){
                string path=AssetDatabase.GUIDToAssetPath(guid);if(!Path.GetFileName(path).StartsWith("Level"))continue;
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                DialogueDirector director=null;
                foreach(var root in scene.GetRootGameObjects()){director=root.GetComponentInChildren<DialogueDirector>(true);if(director)break;}
                if(!director){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),scene);director=go.GetComponent<DialogueDirector>();}
                if(Path.GetFileName(path)=="Level01_InitialLab.unity"){
                    if(!director.playOnStart)director.playOnStart=opening;
                    foreach(var asset in new[]{greeting,briefing,chamber}){
                        var existing=director.transform.Find(asset.name);if(existing)continue;
                        var go=new GameObject(asset.name);go.transform.SetParent(director.transform,false);var trigger=go.AddComponent<DialogueTrigger>();trigger.director=director;trigger.sequence=asset;
                    }
                    EditorUtility.SetDirty(director);PrefabUtility.RecordPrefabInstancePropertyModifications(director);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("DIALOGUE_SYSTEM_READY");
        }
        static DialogueSequence.Line L(string speaker,string text,float delay=0,string cue=""){return new DialogueSequence.Line{speaker=speaker,text=text,minimumDisplaySeconds=delay,cue=cue};}
        static DialogueSequence Create(string name,DialogueSequence.Line[] lines,string notes){
            string path=Root+"Dialogue/"+name+".asset";var data=AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);if(data)return data;
            data=ScriptableObject.CreateInstance<DialogueSequence>();data.lines=lines;data.stagingNotes=notes;AssetDatabase.CreateAsset(data,path);return data;
        }
    }
}
