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
    public static class VerifyNpcRoutes
    {
        const string Key="Emergence.NpcRoutesTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyNpcRoutes(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SetupNpcRoutes.Build();SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/NpcRoutes");File.WriteAllLines("Logs/NpcRoutes/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static IEnumerator Tests(){
            var floor=new GameObject("Floor",typeof(BoxCollider2D));floor.transform.position=new Vector3(1000,-.5f);floor.GetComponent<BoxCollider2D>().size=new Vector2(40,1);
            var player=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),new Vector3(990,0),Quaternion.identity).GetComponent<RobotEmotionGameplay>();
            player.keyboardInput=false;player.requireEmotionUnlocks=false;player.GetComponent<RobotKeyboardInput>().enabled=false;
            var npc=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionExpansion.Dir+"EmotionNPC.prefab"),new Vector3(1000,2),Quaternion.identity).GetComponent<EmotionNpc>();
            var actor=npc.GetComponent<RobotEmotionGameplay>();var body=npc.GetComponent<Rigidbody2D>();
            yield return Wait(1);Check(body.bodyType==RigidbodyType2D.Dynamic && body.position.y<.1f,"NPC falls with gravity and lands on floor");
            npc.route=new[]{new EmotionNpc.Waypoint{position=new Vector2(1002,0),waitSeconds=.2f},new EmotionNpc.Waypoint{position=new Vector2(1004,0),movement=NpcRouteMovement.Jump,jumpApexHeight=1}};npc.Activate();
            float peak=0;float until=Time.time+4;while(Time.time<until){peak=Mathf.Max(peak,body.position.y);yield return null;}
            Check(!npc.Moving && Mathf.Abs(body.position.x-1004)<.2f,"NPC completes ordered walk and jump route");Check(peak>.7f,"Jump follows an airborne arc");
            player.transform.position=new Vector3(1003,0);player.TrySetEmotion(RobotEmotion.Sadness);Check(actor.ReceiveEmotion(player),"Player transmits sadness");yield return Wait(.1f);
            var resonance=actor.GetComponent<EmotionResonance>();resonance.RefreshState();Check(resonance.Enhanced,"NPC receives enhanced emotion");
            Check(npc.GetComponent<LocalTimeTarget>().Frozen,"NPC freezes without losing sadness resonance");Check(!LocalTimeTarget.IsStopped(player),"Player remains immune");
            var position=body.position;npc.destinationPosition=new Vector2(1006,0);npc.route=new EmotionNpc.Waypoint[0];npc.Activate();yield return Wait(.3f);Check(Vector2.Distance(position,body.position)<.01f,"Frozen NPC route does not advance");
            var block=new GameObject("Cycle",typeof(BoxCollider2D),typeof(SpriteRenderer));block.transform.position=new Vector3(1002,3);var b=block.AddComponent<EmotionBlock>();b.kind=EmotionBlockKind.Vanishing;b.fadeSeconds=.25f;b.restoreSeconds=.3f;var cycle=block.AddComponent<CyclicVanishingBlock>();cycle.visibleSeconds=.2f;
            yield return Wait(.6f);Check(!b.IsVanishing && block.GetComponent<Collider2D>().enabled,"Visible cyclic block stays solid while frozen");
            player.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.32f);float alpha=block.GetComponent<SpriteRenderer>().color.a;Check(b.IsVanishing && alpha<1,"Cyclic block begins fading after thaw");
            player.TrySetEmotion(RobotEmotion.Sadness);yield return Wait(.1f);alpha=block.GetComponent<SpriteRenderer>().color.a;yield return Wait(.4f);Check(Mathf.Abs(alpha-block.GetComponent<SpriteRenderer>().color.a)<.001f,"Mid-fade alpha pauses exactly");
            player.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.3f);Check(!block.GetComponent<Collider2D>().enabled,"Faded block loses collision");
            cycle.enabled=false;yield return Wait(.4f);Check(block.GetComponent<Collider2D>().enabled && !b.IsVanishing,"Block restores collision after local timer");
            npc.StopMoving();Check(body.position.x>position.x+.1f,"NPC resumes route after thaw");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var frag=new GameObject("Fragment").AddComponent<EmotionFragment>();frag.transform.position=new Vector3(1010,3);var inactive=new GameObject("InactiveFragment");inactive.SetActive(false);var f2=inactive.AddComponent<EmotionFragment>();
            Check(EmotionFragment.Remaining(scene)==2,"All scene fragments including inactive ones count");Check(!frag.Collect(actor),"NPC cannot collect player fragments");Check(frag.Collect(player) && !frag.Collect(player),"Player collects each fragment once");Check(EmotionFragment.Remaining(scene)==1,"Collected fragment reduces exit requirement");inactive.SetActive(true);Check(f2.Collect(player) && EmotionFragment.Remaining(scene)==0,"Collecting every fragment clears exit requirement");
            var lift=new GameObject("Lift",typeof(BoxCollider2D));lift.transform.position=new Vector3(1012,0);lift.GetComponent<BoxCollider2D>().size=new Vector2(3,.3f);lift.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;var platform=lift.AddComponent<LocalMovingPlatform>();platform.travel=new Vector2(0,3);platform.speed=1;platform.pingPong=false;body.position=new Vector2(1012,.2f);body.velocity=Vector2.zero;
            yield return Wait(1.2f);Check(body.position.y>.8f,"Moving lift physically carries dynamic NPC upward: NPC="+body.position+" lift="+lift.transform.position);
            body.position=new Vector2(1004,0);body.velocity=Vector2.zero;player.TrySetEmotion(RobotEmotion.Sadness);Check(actor.CurrentEmotion==RobotEmotion.Sadness,"NPC retains transmitted sadness after thaw");yield return Wait(.1f);
            var grid=new GameObject("TestGrid",typeof(Grid));var mapObject=new GameObject("TestMap",typeof(UnityEngine.Tilemaps.Tilemap));mapObject.transform.SetParent(grid.transform);var map=mapObject.GetComponent<UnityEngine.Tilemaps.Tilemap>();var tile=ScriptableObject.CreateInstance<EmotionBlockTile>();tile.kind=EmotionBlockKind.Vanishing;tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.Grid;map.SetTile(new Vector3Int(1003,3,0),tile);map.SetTile(new Vector3Int(1020,3,0),tile);var blocks=mapObject.AddComponent<EmotionBlockMap>();blocks.fadeSeconds=.1f;blocks.restoreSeconds=.2f;
            Check(!blocks.BeginVanish(new Vector3Int(1003,3,0)) && blocks.BeginVanish(new Vector3Int(1020,3,0)),"Tilemap freezes individual cells only inside range");
            var physical=new GameObject("OtherBody");physical.transform.position=new Vector3(1002,2);var physicalBody=physical.AddComponent<Rigidbody2D>();physicalBody.gravityScale=1;physicalBody.velocity=Vector2.right;LocalTimeWorld.Ensure(physical);yield return Wait(.1f);var fixedPosition=physicalBody.position;yield return Wait(.2f);Check(Vector2.Distance(fixedPosition,physicalBody.position)<.001f,"Other rigidbodies freeze within sadness range");
            player.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.2f);Check(physicalBody.position.x>fixedPosition.x+.1f,"Other rigidbody velocity resumes after thaw");
            var lost=new GameObject("LostRequiredFragment").AddComponent<EmotionFragment>();EmotionFragment.Remaining(scene);UnityEngine.Object.Destroy(lost.gameObject);yield return null;Check(EmotionFragment.Remaining(scene)==1,"Destroying an uncollected fragment cannot bypass exit gate");
            Check(!LevelFlowController.Begin(null),"Invalid exit request remains rejected");
        }
    }
}
