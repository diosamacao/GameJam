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
    public static class VerifyMemoryBlocks
    {
        const string Key="Emergence.MemoryBlocksTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyMemoryBlocks(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SetupMemoryBlocks.Build();SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
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
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/MemoryBlocks");File.WriteAllLines("Logs/MemoryBlocks/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Spawn(string path,Vector2 pos){var a=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),pos,Quaternion.identity).GetComponent<RobotEmotionGameplay>();a.requireEmotionUnlocks=false;a.keyboardInput=false;a.GetComponent<RobotKeyboardInput>().enabled=false;a.GetComponent<Rigidbody2D>().simulated=false;return a;}
        static bool Visible(UnityEngine.Tilemaps.Tilemap m,Vector3Int c)=>m.GetColor(c).a>.99f && m.GetColliderType(c)==UnityEngine.Tilemaps.Tile.ColliderType.Grid;
        static bool Hidden(UnityEngine.Tilemaps.Tilemap m,Vector3Int c)=>m.GetColor(c).a<.001f && m.GetColliderType(c)==UnityEngine.Tilemaps.Tile.ColliderType.None;
        static IEnumerator Tests(){
            EmotionProgress.UnlockEnhancement();
            var appear=AssetDatabase.LoadAssetAtPath<MemoryBlockTile>(SetupMemoryBlocks.Dir+"Tiles/MemoryAppear.asset");var disappear=AssetDatabase.LoadAssetAtPath<MemoryBlockTile>(SetupMemoryBlocks.Dir+"Tiles/MemoryDisappear.asset");Check(appear && disappear && appear.sprite.rect.width==64 && appear.sprite.pixelsPerUnit==64,"Both 64x64 collision tile assets imported");
            var grid=new GameObject("MemoryGrid",typeof(Grid));grid.transform.position=new Vector3(1000,0);var child=new GameObject("MixedMap",typeof(UnityEngine.Tilemaps.Tilemap),typeof(UnityEngine.Tilemaps.TilemapRenderer));child.transform.SetParent(grid.transform,false);var map=child.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            var a=Vector3Int.zero;var b=new Vector3Int(1,0,0);var farA=new Vector3Int(20,0,0);var farB=new Vector3Int(21,0,0);map.SetTile(a,appear);map.SetTile(b,disappear);map.SetTile(farA,appear);map.SetTile(farB,disappear);yield return Wait(.15f);
            Check(child.GetComponent<MemoryBlockMap>() && child.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>(),"Painting memory tiles automatically installs runtime controller and collision");Check(Hidden(map,a) && Visible(map,b),"Initial hidden/visible state and collision are inverse");
            var player=Spawn("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab",new Vector2(1000,2));var npc=Spawn(SetupEmotionExpansion.Dir+"EmotionNPC.prefab",new Vector2(1001,2));player.TrySetEmotion(RobotEmotion.Memory);Check(npc.ReceiveEmotion(player),"Enhanced memory source established");yield return Wait(.2f);
            Check(Visible(map,a) && Hidden(map,b),"Enhanced memory reveals first tile and hides second with collision updated");Check(Hidden(map,farA) && Visible(map,farB),"Far cells on same map remain in default state");Physics2D.SyncTransforms();var collider=child.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();Check(collider.OverlapPoint(map.GetCellCenterWorld(a)) && !collider.OverlapPoint(map.GetCellCenterWorld(b)),"Actual physics collider agrees with visibility");
            var sadPlayer=Spawn("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab",new Vector2(1002,2));var sadNpc=Spawn(SetupEmotionExpansion.Dir+"EmotionNPC.prefab",new Vector2(1003,2));sadPlayer.TrySetEmotion(RobotEmotion.Sadness);Check(sadNpc.ReceiveEmotion(sadPlayer),"Sadness source established");yield return Wait(.1f);player.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.2f);Check(Visible(map,a) && Hidden(map,b),"Time stop preserves visible and hidden states after memory range ends");sadPlayer.TrySetEmotion(RobotEmotion.Joy);yield return Wait(.2f);Check(Hidden(map,a) && Visible(map,b),"Thaw reevaluates range and restores defaults");
            map.SetTile(a,disappear);yield return Wait(.15f);Check(Visible(map,a),"Replacing a tile at runtime rebuilds its state correctly");map.SetTile(a,null);yield return Wait(.1f);Check(!map.HasTile(a),"Removed cells are not resurrected by range updates");
            var template=AssetDatabase.LoadAssetAtPath<GameObject>(SetupMemoryBlocks.Dir+"Prefabs/MemoryBlocksGrid.prefab");Check(template.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().Length==2,"Grid prefab provides two separate layers");foreach(var name in new[]{"MemoryAppear","MemoryDisappear"})Check(AssetDatabase.LoadAssetAtPath<GameObject>(SetupMemoryBlocks.Dir+"Palettes/"+name+"_64x64.prefab"),"Independent palette exists: "+name);
        }
    }
}
