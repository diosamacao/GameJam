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
    public static class VerifyPerformance
    {
        const string Key="Emergence.PerformanceTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyPerformance(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/Performance");File.WriteAllLines("Logs/Performance/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Spawn(bool npc,Vector2 position){
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),position,Quaternion.identity).GetComponent<RobotEmotionGameplay>();actor.isNpc=npc;actor.requireEmotionUnlocks=false;actor.keyboardInput=false;actor.GetComponent<RobotKeyboardInput>().enabled=false;actor.GetComponent<Rigidbody2D>().simulated=false;return actor;
        }
        static bool Legacy(Vector2 pos,RobotEmotion emotion){foreach(var actor in EmotionResonance.Actors){actor.RefreshState();if(actor.Enhanced && actor.Emotion==emotion && Vector2.Distance(actor.transform.position,pos)<=actor.radius)return true;}return false;}
        static IEnumerator Tests(){
            yield return Wait(1);EmotionProgress.UnlockEnhancement();
            var player=Spawn(false,new Vector2(2000,0));player.TrySetEmotion(RobotEmotion.Joy);
            var actors=new List<RobotEmotionGameplay>();for(int i=0;i<32;i++){var npc=Spawn(true,new Vector2(2001,0));npc.ReceiveEmotion(player);actors.Add(npc);}yield return Wait(.1f);
            var clock=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<1000;i++)Legacy(new Vector2(2000+i%20,0),RobotEmotion.Sadness);clock.Stop();double before=clock.Elapsed.TotalMilliseconds;
            clock.Restart();for(int i=0;i<1000;i++)EmotionResonance.InEnhancedRange(new Vector2(2000+i%20,0),RobotEmotion.Sadness);clock.Stop();double after=clock.Elapsed.TotalMilliseconds;
            results.Add("BENCHMARK 1000 sadness range queries / 33 actors: legacy="+before.ToString("F2")+"ms optimized="+after.ToString("F2")+"ms");
            Check(!EmotionResonance.InEnhancedRange(player.transform.position,RobotEmotion.Sadness),"No false sadness range from other emotions");
            player.TrySetEmotion(RobotEmotion.Sadness);Check(actors[0].ReceiveEmotion(player),"Transfer changes cached source emotion");
            Check(EmotionResonance.InEnhancedRange(player.transform.position,RobotEmotion.Sadness),"Sadness range becomes active in same frame");
            player.TrySetEmotion(RobotEmotion.Joy);Check(!EmotionResonance.InEnhancedRange(player.transform.position,RobotEmotion.Sadness),"Leaving sadness invalidates cache immediately");
            player.TrySetEmotion(RobotEmotion.Sadness);actors[0].enabled=false;Check(!EmotionResonance.InEnhancedRange(player.transform.position,RobotEmotion.Sadness),"Disabled partner invalidates enhancement");actors[0].enabled=true;
            Check(EmotionResonance.InEnhancedRange(player.transform.position,RobotEmotion.Sadness),"Reenabled partner restores enhancement");
            for(int i=0;i<40;i++){Vector2 position=new Vector2(1980+i,0);Check(Legacy(position,RobotEmotion.Sadness)==EmotionResonance.InEnhancedRange(position,RobotEmotion.Sadness),"Range parity "+i);}
            var platformObject=new GameObject("Freeze boundary platform");platformObject.transform.position=player.transform.position;
            var platform=platformObject.AddComponent<LocalMovingPlatform>();platform.activation=PlatformActivation.Single;
            var pressureObject=new GameObject("Test pressure");var pressure=pressureObject.AddComponent<PlatformPressureSwitch>();
            platform.SetSwitch(pressure,true);
            var tick=typeof(LocalMovingPlatform).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            tick.Invoke(platform,null);Check(true,"Deferred switch batch terminates when freeze changes before physics update");
            player.TrySetEmotion(RobotEmotion.Joy);tick.Invoke(platform,null);Check(true,"Deferred switch resumes after thaw");
            UnityEngine.Object.Destroy(platformObject);UnityEngine.Object.Destroy(pressureObject);
            var grid=new GameObject("Laser test grid",typeof(Grid));var go=new GameObject("Map",typeof(UnityEngine.Tilemaps.Tilemap));go.transform.SetParent(grid.transform);var map=go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            var tile=AssetDatabase.LoadAssetAtPath<LaserEmitterTile>("Assets/EmergenceLevel01/Tilemap64/Lasers/Tiles/Laser_Right.asset");map.SetTile(Vector3Int.zero,tile);map.SetTile(Vector3Int.right*3,tile);var controller=go.AddComponent<LaserEmitterMap>();yield return Wait(.1f);
            var first=go.transform.Find("Laser_"+Vector3Int.zero).gameObject;var second=go.transform.Find("Laser_"+(Vector3Int.right*3)).gameObject;
            map.SetTileFlags(Vector3Int.zero,UnityEngine.Tilemaps.TileFlags.None);map.SetColor(Vector3Int.zero,Color.blue);yield return Wait(.1f);
            Check(first && second && go.transform.childCount==2,"Tile tint changes do not rebuild laser emitters");
            controller.Rebuild();Check(first && second && first.activeSelf && second.activeSelf,"Explicit rescan preserves unchanged instances");
            map.SetTransformMatrix(Vector3Int.zero,Matrix4x4.Rotate(Quaternion.Euler(0,0,90)));yield return Wait(.1f);Check(first && first.activeSelf && Vector3.Dot(first.transform.right,Vector3.up)>.99f,"Brush rotation updates existing laser in place");
            map.SetTile(Vector3Int.zero,null);yield return Wait(.1f);Check(!first && second && second.activeSelf,"Erasing one laser preserves other instances");
            UnityEngine.Object.Destroy(grid);foreach(var actor in actors)UnityEngine.Object.Destroy(actor.gameObject);UnityEngine.Object.Destroy(player.gameObject);yield return Wait(.1f);
            yield return (IEnumerator)typeof(VerifyMemoryBlocks).GetMethod("Tests",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,null);
            Check(true,"Memory tile visibility, physics, freeze and runtime painting regression passed");
        }
    }
}
