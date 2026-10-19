using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyMemoryScenes
    {
        const string Key="Emergence.MemoryScenesTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyMemoryScenes(){EditorApplication.playModeStateChanged+=Mode;}
        const string Fixture="Assets/__MemorySceneValidation.unity";
        public static void Batch(){
            SetupMemoryScenes.Build();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var intro=ScriptableObject.CreateInstance<DialogueSequence>();intro.lines=new[]{new DialogueSequence.Line{text="Unexpected intro"}};
            const string asset="Assets/__MemoryIntro.asset";if(AssetDatabase.LoadAssetAtPath<DialogueSequence>(asset))AssetDatabase.DeleteAsset(asset);AssetDatabase.CreateAsset(intro,asset);
            new GameObject("Director").AddComponent<DialogueDirector>().playOnStart=intro;
            EditorSceneManager.SaveScene(scene,Fixture);
            var builds=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);builds.RemoveAll(item=>item.path==Fixture);builds.Add(new EditorBuildSettingsScene(Fixture,true));EditorBuildSettings.scenes=builds.ToArray();
            SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;
        }
        static void Mode(PlayModeStateChange state){
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;results.Clear();stack.Push(Tests());deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetBool(Key+"Success",false)?0:1);}
        }
        static void Tick(){try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Test timeout");
            if(stack.Count==0){Finish(true);return;}
            var current=stack.Peek();if(!current.MoveNext()){stack.Pop();return;}if(current.Current is IEnumerator nested)stack.Push(nested);
        }catch(Exception ex){results.Add("FAIL: "+ex);Finish(false);}}
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/MemoryScenes");File.WriteAllLines("Logs/MemoryScenes/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Spawn(bool npc,Vector2 position){
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),position,Quaternion.identity).GetComponent<RobotEmotionGameplay>();actor.isNpc=npc;actor.requireEmotionUnlocks=false;actor.keyboardInput=false;actor.GetComponent<RobotKeyboardInput>().enabled=false;actor.GetComponent<Rigidbody2D>().simulated=false;return actor;
        }
        static DialogueSequence Sequence(string text){var sequence=ScriptableObject.CreateInstance<DialogueSequence>();sequence.typewriter=false;sequence.autoPlay=false;sequence.lines=new[]{new DialogueSequence.Line{text=text}};return sequence;}
        static IEnumerator Entered(MemorySceneConfig config){while(!DialogueDirector.Instance || DialogueDirector.Instance.Sequence!=config.dialogue)yield return null;}
        static IEnumerator Returned(){while(LevelFlowController.IsTransitioning)yield return null;}
        static IEnumerator Tests(){
            yield return Wait(.2f);
            var source=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var player=Spawn(false,new Vector2(8,3));var director=new GameObject("SourceDirector").AddComponent<DialogueDirector>();
            var inactive=new GameObject("CollectedFragmentPlaceholder");inactive.SetActive(false);
            var marker=new GameObject("SourceProgress");int markerId=marker.GetInstanceID();
            SceneCheckpointState.Activate(source,new Vector2(6,2));
            var config=ScriptableObject.CreateInstance<MemorySceneConfig>();config.memoryScene=Fixture;config.dialogue=Sequence("Recollection");config.fadeOutSeconds=config.fadeInSeconds=.04f;config.blackHoldSeconds=0;
            var trigger=new GameObject("MemoryTrigger").AddComponent<MemorySceneTrigger>();trigger.memory=config;
            var npc=Spawn(true,new Vector2(9,3));Check(!trigger.TryContact(npc.GetComponent<Collider2D>()),"NPC collision cannot start recollection");
            Time.timeScale=0;
            Check(trigger.TryContact(player.GetComponent<Collider2D>()),"Player collision starts recollection");
            Check(!LevelFlowController.BeginMemory(config),"Duplicate trips rejected");
            yield return Entered(config);
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==Fixture,"Target scene is active");
            Check(source.isLoaded && !marker.activeSelf && !player.gameObject.activeSelf,"Source is retained and suspended");
            Check(DialogueDirector.Instance.Sequence==config.dialogue,"Configured dialogue plays instead of target scene intro");
            DialogueDirector.Instance.Advance();yield return Returned();
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene()==source && marker && marker.GetInstanceID()==markerId && marker.activeSelf,"Returns to same source scene and objects");
            Check(!inactive.activeSelf && player.transform.position==new Vector3(8,3),"Collected state and original player position preserved");
            Check(SceneCheckpointState.TryGet(source,out var checkpoint) && checkpoint==new Vector2(6,2),"Checkpoint preserved across additive trip");
            Check(!trigger.TryTrigger(),"Once-only collision does not replay on return");
            Check(!LevelFlowController.IsFading && LevelFlowController.FadeOpacity==0,"Curtain cleared and input unlocked");
            var entry=Sequence("Enter memory after this line");entry.memoryAfterCompletion=config;
            Check(director.TryPlay(entry),"Source story can start after return");director.Stop();Check(!LevelFlowController.IsTransitioning,"Cancelled source story does not trigger a trip");
            director.TryPlay(entry);director.Advance();Check(LevelFlowController.IsMemoryTrip,"Finishing configured source story starts trip");
            config.dialogue.memoryAfterCompletion=config;
            yield return Entered(config);DialogueDirector.Instance.Advance();yield return Returned();
            Check(!LevelFlowController.IsMemoryTrip && UnityEngine.SceneManagement.SceneManager.sceneCount==1,"Nested recollection is suppressed and temporary scene unloaded");
            config.dialogue.memoryAfterCompletion=null;trigger.ResetTrigger();Check(trigger.TryTrigger(),"External trigger and trigger reset supported");yield return Entered(config);DialogueDirector.Instance.Stop();yield return Returned();Check(marker.activeSelf,"Cancelled recollection safely returns to original scene");
            Time.timeScale=1;
            Check(AssetDatabase.LoadAssetAtPath<GameObject>(SetupMemoryScenes.DirectoryPath+"/MemorySceneTrigger.prefab").GetComponent<BoxCollider2D>().isTrigger,"Designer trigger prefab has trigger collision enabled");
        }
    }
}
