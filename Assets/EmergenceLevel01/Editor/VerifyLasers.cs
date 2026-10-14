using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyLasers
    {
        const string Key="Emergence.VerifyLasers";
        static readonly List<string> results=new List<string>();
        static VerifyLasers(){EditorApplication.playModeStateChanged+=Mode;}
        [MenuItem("Tools/Emergence/Verify Lasers")]
        public static void Run(){SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Bg",Application.runInBackground);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){Application.runInBackground=true;EditorApplication.delayCall+=Test;}
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false)){Application.runInBackground=SessionState.GetBool(Key+"Bg",false);SessionState.SetBool(Key,false);}
        }
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);results.Add("PASS: "+message);}
        static void Test(){results.Clear();try{
            if(DialogueDirector.Instance)DialogueDirector.Instance.Stop();
            var player=UnityEngine.Object.FindObjectOfType<RobotEmotionGameplay>();player.keyboardInput=false;player.GetComponent<RobotKeyboardInput>().enabled=false;var body=player.GetComponent<Rigidbody2D>();body.gravityScale=0;
            var checkpoint=new GameObject("LaserTestCheckpoint");checkpoint.transform.position=new Vector3(990,5);player.SetRespawnPoint(checkpoint.transform);body.position=new Vector2(990,5);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(SetupLaserEmitters.Dir+"Prefabs/LaserEmitter_Right.prefab");var emitter=UnityEngine.Object.Instantiate(source,new Vector3(1000,0),Quaternion.identity).GetComponent<LaserEmitter>();
            var wall=new GameObject("BlockingWall",typeof(BoxCollider2D));wall.transform.position=new Vector3(1004,0);Physics2D.SyncTransforms();emitter.Trace(false);
            Check(Mathf.Abs(emitter.BeamLength-3.01f)<.03f,"Solid collider stops laser at nearest surface");
            wall.GetComponent<Collider2D>().enabled=false;Physics2D.SyncTransforms();emitter.Trace(false);Check(emitter.BeamLength>29,"Disabling collider immediately extends laser");
            wall.GetComponent<Collider2D>().enabled=true;wall.GetComponent<Collider2D>().isTrigger=true;Physics2D.SyncTransforms();emitter.Trace(false);Check(emitter.BeamLength>29,"Gameplay trigger volumes do not block laser");
            wall.GetComponent<Collider2D>().isTrigger=false;Physics2D.SyncTransforms();
            foreach(var dir in new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down}){emitter.direction=dir;wall.transform.position=emitter.transform.position+(Vector3)(dir*4);Physics2D.SyncTransforms();emitter.Trace(false);Check(Mathf.Abs(emitter.BeamLength-3.01f)<.03f,"Direction blocked: "+dir);}
            emitter.direction=Vector2.right;wall.transform.position=new Vector3(1004,0);body.position=new Vector2(1006,0);player.TrySetEmotion(RobotEmotion.Courage);Physics2D.SyncTransforms();emitter.Trace(true);Check(body.position.x>1005,"Wall shields player behind it");
            wall.GetComponent<Collider2D>().enabled=false;Physics2D.SyncTransforms();emitter.Trace(true);Check(Vector2.Distance(body.position,checkpoint.transform.position)<.01f,"Courage also dies to laser and returns to checkpoint");
            Check(!player.DieAndRespawn(),"Respawn grace prevents repeated death in same frame");
            var grid=new GameObject("LaserTestGrid",typeof(Grid));grid.transform.position=new Vector3(1020,0);var mapObject=new GameObject("Map",typeof(Tilemap),typeof(TilemapRenderer),typeof(LaserEmitterMap));mapObject.transform.SetParent(grid.transform,false);var map=mapObject.GetComponent<Tilemap>();var rules=mapObject.GetComponent<LaserEmitterMap>();
            string[] names={"Right","Up","Left","Down"};for(int i=0;i<4;i++)map.SetTile(new Vector3Int(i*3,0,0),AssetDatabase.LoadAssetAtPath<LaserEmitterTile>(SetupLaserEmitters.Dir+"Tiles/Laser_"+names[i]+".asset"));rules.Rebuild();Check(mapObject.GetComponentsInChildren<LaserEmitter>().Length==4,"Painted four direction tiles create four runtime emitters");
            map.SetTile(Vector3Int.zero,null);rules.Rebuild();Check(mapObject.GetComponentsInChildren<LaserEmitter>().Length==3,"Erasing a tile removes its active hazard");
            map.SetTile(Vector3Int.zero,AssetDatabase.LoadAssetAtPath<LaserEmitterTile>(SetupLaserEmitters.Dir+"Tiles/Laser_Right.asset"));map.SetTileFlags(Vector3Int.zero,TileFlags.None);map.SetTransformMatrix(Vector3Int.zero,Matrix4x4.Rotate(Quaternion.Euler(0,0,90)));rules.Rebuild();
            LaserEmitter rotated=null;foreach(var item in mapObject.GetComponentsInChildren<LaserEmitter>())if(Vector3.Distance(item.transform.position,map.GetCellCenterWorld(Vector3Int.zero))<.01f)rotated=item;
            Check(rotated && Vector2.Distance(rotated.BeamDirection,Vector2.up)<.01f,"Brush rotation rotates beam with housing");
            var blockerObject=new GameObject("TilemapBlocker",typeof(Tilemap),typeof(TilemapCollider2D));blockerObject.transform.SetParent(grid.transform,false);blockerObject.transform.position=new Vector3(1003.5f,-.5f);var blocker=blockerObject.GetComponent<Tilemap>();blocker.SetTile(Vector3Int.zero,AssetDatabase.LoadAssetAtPath<EmotionBlockTile>(SetupEmotionBlocks.Dir+"Tiles/sadness.asset"));blockerObject.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();Physics2D.SyncTransforms();emitter.Trace(false);Check(Mathf.Abs(emitter.BeamLength-3.01f)<.03f,"Tilemap collider blocks beam");
            blocker.SetColliderType(Vector3Int.zero,Tile.ColliderType.None);blockerObject.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();Physics2D.SyncTransforms();emitter.Trace(false);Check(emitter.BeamLength>10,"Vanishing tile without collision lets beam through to next obstacle");
            wall.GetComponent<Collider2D>().enabled=true;Physics2D.SyncTransforms();emitter.Trace(false);
            var sr=wall.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergenceLevel01/Art/Tiles64/EmotionBlocks/reinforced.png");sr.sharedMaterial=emitter.beamMaterial;sr.sortingOrder=20;
            var camera=new GameObject("LaserPreviewCamera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.6f;camera.transform.position=new Vector3(1002,0,-10);camera.backgroundColor=new Color(.07f,.1f,.15f);camera.clearFlags=CameraClearFlags.SolidColor;
            Directory.CreateDirectory("Logs/Lasers");var rt=new RenderTexture(768,384,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var texture=new Texture2D(768,384,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,768,384),0,0);texture.Apply();File.WriteAllBytes("Logs/Lasers/preview.png",texture.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
        }catch(Exception e){results.Add("FAIL: "+e);}
            Directory.CreateDirectory("Logs/Lasers");File.WriteAllLines("Logs/Lasers/verification.txt",results);Debug.Log(string.Join("\n",results));EditorApplication.isPlaying=false;
        }
    }
}
