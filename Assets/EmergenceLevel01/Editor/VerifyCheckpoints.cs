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
    public static class VerifyCheckpoints
    {
        const string Key="Emergence.CheckpointTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyCheckpoints(){EditorApplication.playModeStateChanged+=Mode;}
        const string ScenePath="Assets/__CheckpointValidation.unity";
        public static void Batch(){
            SetupSceneCheckpoints.Build();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var floor=new GameObject("Floor",typeof(BoxCollider2D));floor.transform.position=new Vector3(1005,-.5f);floor.GetComponent<BoxCollider2D>().size=new Vector2(30,1);
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),new Vector3(1000,0),Quaternion.identity);player.name="TestPlayer";var actor=player.GetComponent<RobotEmotionGameplay>();actor.requireEmotionUnlocks=false;actor.keyboardInput=false;player.GetComponent<RobotKeyboardInput>().enabled=false;
            var npc=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionExpansion.Dir+"EmotionNPC.prefab"),new Vector3(1002,0),Quaternion.identity);npc.name="TestNpc";
            npc.GetComponent<EmotionNpc>().route=new[]{new EmotionNpc.Waypoint{position=new Vector2(1003,0),requiresPlayerInteraction=true},new EmotionNpc.Waypoint{position=new Vector2(1004,0)},new EmotionNpc.Waypoint{position=new Vector2(1005,0),requiresPlayerInteraction=true}};
            npc.GetComponent<EmotionNpc>().activateOnStart=true;
            foreach(int x in new[]{1006,1009}){var point=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionExpansion.Dir+"SceneCheckpoint.prefab"));point.name="Checkpoint"+x;point.transform.position=new Vector3(x,0);}
            var fragment=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionExpansion.Dir+"EmotionFragment.prefab"));fragment.name="ResetFragment";fragment.transform.position=new Vector3(1013,3);
            new GameObject("ResetMarker");new GameObject("Camera",typeof(Camera));new GameObject("Light",typeof(Light));
            var seq=ScriptableObject.CreateInstance<DialogueSequence>();seq.lines=new[]{new DialogueSequence.Line{speaker="Test",text="Intro"}};AssetDatabase.CreateAsset(seq,"Assets/__CheckpointDialogue.asset");new GameObject("Dialogue",typeof(DialogueDirector)).GetComponent<DialogueDirector>().playOnStart=seq;
            EditorSceneManager.SaveScene(scene,ScenePath);var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);scenes.RemoveAll(x=>x.path==ScenePath);scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Mode(PlayModeStateChange state){
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;results.Clear();stack.Push(Tests());deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetBool(Key+"Success",false)?0:1);}
        }
        static void Tick(){try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Test timeout");
            if(stack.Count==0){Finish(true);return;}
            var current=stack.Peek();if(!current.MoveNext()){stack.Pop();return;}if(current.Current is IEnumerator nested)stack.Push(nested);
        }catch(Exception ex){results.Add("FAIL: "+ex);Finish(false);}}
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/Checkpoints");File.WriteAllLines("Logs/Checkpoints/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Player()=>GameObject.Find("TestPlayer").GetComponent<RobotEmotionGameplay>();
        static EmotionNpc Npc()=>GameObject.Find("TestNpc").GetComponent<EmotionNpc>();
        static bool sawOpaque, sawPartial;static Vector2 loadedPosition;
        static void Loaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode){sawOpaque=LevelFlowController.FadeOpacity>=.999f;loadedPosition=Player().GetComponent<Rigidbody2D>().position;}
        static IEnumerator Tests(){
            EmotionProgress.UnlockEnhancement();
            yield return Wait(.2f);DialogueDirector.Instance.Stop();var player=Player();var pb=player.GetComponent<Rigidbody2D>();var npc=Npc();var actor=npc.GetComponent<RobotEmotionGameplay>();var nb=npc.GetComponent<Rigidbody2D>();actor.electricGraceSeconds=.1f;player.electricGraceSeconds=.1f;
            yield return Wait(.4f);Check(npc.AwaitingInteraction && Mathf.Abs(nb.position.x-1002)<.05f,"Interactive waypoint waits before moving");
            pb.position=new Vector2(990,0);yield return Wait(.1f);Check(!npc.TryInteract(player),"Route interaction rejects distant player");pb.position=new Vector2(1001,0);yield return Wait(.1f);
            Check(StoryInteractable.TryNearest(player) && !npc.AwaitingInteraction,"E interaction dispatcher releases current NPC node");yield return Wait(1.3f);
            Check(npc.RouteIndex==2 && npc.AwaitingInteraction && Mathf.Abs(nb.position.x-1004)<.15f,"Automatic next node runs, next interactive node waits");
            pb.position=new Vector2(1003,0);yield return Wait(.1f);Check(npc.TryInteract(player),"Second interactive node requires a new interaction");yield return Wait(.8f);Check(!npc.Moving && nb.position.x>1004.8f,"Route finishes after second release");
            Check(actor.DieAndRespawn(),"NPC supports death");Check(Mathf.Abs(nb.position.x-1002)<.01f && nb.velocity==Vector2.zero && npc.RouteIndex==0 && npc.AwaitingInteraction,"NPC respawns at birth and clears route authorization");
            var hazard=new GameObject("Hazard",typeof(BoxCollider2D)).AddComponent<DangerZone>();hazard.GetComponent<BoxCollider2D>().isTrigger=true;yield return Wait(.2f);Check(hazard.TryContact(actor),"Danger zone kills NPC without enhanced courage");
            yield return Wait(.2f);pb.position=new Vector2(1001,0);player.TrySetEmotion(RobotEmotion.Courage);Check(actor.ReceiveEmotion(player),"NPC receives courage from player");yield return Wait(.1f);Check(!hazard.TryContact(actor),"Enhanced courage protects NPC in danger zone");
            pb.position=new Vector2(990,0);yield return Wait(.1f);Check(hazard.TryContact(actor),"Unenhanced courage cannot protect NPC from danger zone");
            pb.position=new Vector2(1006,0);pb.velocity=Vector2.zero;yield return Wait(.2f);Check(SceneCheckpointState.TryGet(player.gameObject.scene,out var cp) && Mathf.Abs(cp.x-1006)<.01f,"Real trigger contact activates first checkpoint");
            pb.position=new Vector2(1008,0);Check(player.DieAndRespawn() && Mathf.Abs(pb.position.x-1006)<.01f,"Player death respawns at touched checkpoint");yield return Wait(.2f);
            pb.position=new Vector2(1009,0);pb.velocity=Vector2.zero;yield return Wait(.2f);Check(SceneCheckpointState.TryGet(player.gameObject.scene,out cp) && Mathf.Abs(cp.x-1009)<.01f,"Latest touched checkpoint replaces prior checkpoint");
            Check(!GameObject.Find("Checkpoint1006").GetComponent<SceneCheckpoint>().Activate(actor),"NPC cannot activate player checkpoint");
            GameObject.Find("ResetMarker").SetActive(false);Check(GameObject.Find("ResetFragment").GetComponent<EmotionFragment>().Collect(player),"Collect fragment before reset");Check(EmotionFragment.Remaining(player.gameObject.scene)==0,"All fragments collected before reset");
            var reset=UnityEngine.Object.FindObjectOfType<SceneResetController>();Check(reset && reset.resetKey==KeyCode.R,"Scene automatically receives R reset binding");
            DialogueDirector.Instance.Play(DialogueDirector.Instance.playOnStart);Time.timeScale=0;UnityEngine.SceneManagement.SceneManager.sceneLoaded+=Loaded;Check(reset.TryReset(),"Reset can start during dialogue and paused time");Check(!reset.TryReset(),"Repeated reset requests are rejected");
            while(LevelFlowController.IsTransitioning){if(LevelFlowController.FadeOpacity>0 && LevelFlowController.FadeOpacity<1)sawPartial=true;yield return null;}UnityEngine.SceneManagement.SceneManager.sceneLoaded-=Loaded;
            Check(sawOpaque && sawPartial && LevelFlowController.FadeOpacity==0,"Reset uses fade out, opaque load, and fade in");Check(Mathf.Abs(loadedPosition.x-1009)<.01f,"Reloaded player already at last checkpoint before fade in");Check(!DialogueDirector.Instance.IsPlaying,"Scene reset suppresses repeated opening dialogue");
            Check(GameObject.Find("ResetMarker") && EmotionFragment.Remaining(Player().gameObject.scene)==1,"Reset restores scene objects and uncollected fragments");Check(Npc().RouteIndex==0 && Npc().AwaitingInteraction,"Scene reset restores NPC route zero");Time.timeScale=1;
            var load=UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(ScenePath);while(!load.isDone)yield return null;yield return Wait(.1f);DialogueDirector.Instance.Stop();Check(!SceneCheckpointState.TryGet(Player().gameObject.scene,out cp),"Normal scene load clears checkpoint memory");Check(Mathf.Abs(Player().transform.position.x-1000)<.1f,"Normal load uses original player spawn");
            reset=UnityEngine.Object.FindObjectOfType<SceneResetController>();Check(reset.TryReset(),"Reset without checkpoint is allowed");while(LevelFlowController.IsTransitioning)yield return null;Check(Mathf.Abs(Player().transform.position.x-1000)<.1f,"No checkpoint reset falls back to initial spawn");
        }
    }
}
