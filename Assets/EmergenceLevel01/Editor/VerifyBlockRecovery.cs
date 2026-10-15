using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyBlockRecovery
    {
        const string Key="Emergence.BlockRecoveryTest";
        static readonly List<string> results=new List<string>();
        static int phase,frame;static float started,lastFrame;static RobotEmotionGameplay player;static EmotionBlock blue,red;static Tilemap map;static EmotionBlockMap rules;static Camera camera;
        static VerifyBlockRecovery(){EditorApplication.playModeStateChanged+=Mode;}
        [MenuItem("Tools/Emergence/Verify Block Recovery And Shatter")]
        public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play first");SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Background",Application.runInBackground);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){Application.runInBackground=true;phase=frame=0;results.Clear();started=Time.time;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false)){Application.runInBackground=SessionState.GetBool(Key+"Background",false);SessionState.SetBool(Key,false);}
        }
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);results.Add("PASS: "+text);}
        static void Tick(){try{
            if(Time.time-started>12)throw new Exception("Timeout "+phase);
            if(phase==0){
                if(DialogueDirector.Instance)DialogueDirector.Instance.Stop();player=UnityEngine.Object.FindObjectOfType<RobotEmotionGameplay>();player.keyboardInput=false;player.GetComponent<RobotKeyboardInput>().enabled=false;player.TrySetEmotion(RobotEmotion.Sadness);
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionBlocks.Dir+"Prefabs/Block_sadness.prefab");blue=UnityEngine.Object.Instantiate(asset,new Vector3(1010,0),Quaternion.identity).GetComponent<EmotionBlock>();
                blue.GetComponent<SpriteRenderer>().color=new Color(.8f,.9f,1,.8f);
                var grid=new GameObject("RecoveryGrid",typeof(Grid));grid.transform.position=new Vector3(1020,0);var go=new GameObject("Sadness",typeof(Tilemap),typeof(TilemapCollider2D),typeof(EmotionBlockMap));go.transform.SetParent(grid.transform,false);map=go.GetComponent<Tilemap>();rules=go.GetComponent<EmotionBlockMap>();
                var tile=AssetDatabase.LoadAssetAtPath<EmotionBlockTile>(SetupEmotionBlocks.Dir+"Tiles/sadness.asset");map.SetTile(Vector3Int.zero,tile);map.SetTile(Vector3Int.right,tile);
                bool respawned;Check(blue.TryInteract(player) && rules.TryInteractCell(Vector3Int.zero,player,out respawned),"Sadness activates prefab and cell");
                Check(blue.GetComponent<Collider2D>().enabled && Mathf.Approximately(blue.GetComponent<SpriteRenderer>().color.a,.8f),"Prefab starts fading without immediately removing collision");
                Check(map.GetColliderType(Vector3Int.zero)==Tile.ColliderType.Grid && Mathf.Approximately(map.GetColor(Vector3Int.zero).a,1),"Tile starts fading with original collision");
                started=Time.time;phase=10;return;
            }
            if(phase==10){if(Time.time-started<.25f)return;
                float alpha=blue.GetComponent<SpriteRenderer>().color.a;
                Check(alpha>.3f && alpha<.8f && blue.GetComponent<Collider2D>().enabled,"Prefab progressively fades while retaining collision");
                alpha=map.GetColor(Vector3Int.zero).a;
                Check(alpha>.3f && alpha<1 && map.GetColliderType(Vector3Int.zero)==Tile.ColliderType.Grid,"Tile progressively fades while retaining collision");phase=11;return;
            }
            if(phase==11){if(Time.time-started<.65f)return;bool respawned;
                Check(blue.gameObject.activeSelf && Mathf.Approximately(blue.GetComponent<SpriteRenderer>().color.a,.3f) && !blue.GetComponent<Collider2D>().enabled,"Prefab remains visible at 30 percent without collision");
                Check(map.HasTile(Vector3Int.zero) && Mathf.Approximately(map.GetColor(Vector3Int.zero).a,.3f) && map.GetColliderType(Vector3Int.zero)==Tile.ColliderType.None,"Tile remains at 30 percent without collision");
                Check(map.GetColliderType(Vector3Int.right)==Tile.ColliderType.Grid && Mathf.Approximately(map.GetColor(Vector3Int.right).a,1),"Neighbor unaffected");
                Check(!blue.TryInteract(player) && !rules.TryInteractCell(Vector3Int.zero,player,out respawned),"Repeated touch cannot restart timer");phase=1;return;
            }
            if(phase==1){if(Time.time-started<1)return;bool respawned;rules.TryInteractCell(Vector3Int.right,player,out respawned);phase=2;return;}
            if(phase==2){if(Time.time-started<3.25f)return;
                Check(!blue.GetComponent<Collider2D>().enabled && map.GetColliderType(Vector3Int.zero)==Tile.ColliderType.None,"Three-second intangible duration starts after fade completes");phase=12;return;
            }
            if(phase==12){if(Time.time-started<3.7f)return;
                Check(blue.GetComponent<Collider2D>().enabled && Mathf.Approximately(blue.GetComponent<SpriteRenderer>().color.a,.8f),"Prefab restores original opacity and collision after three seconds");
                Check(map.GetColliderType(Vector3Int.zero)==Tile.ColliderType.Grid && Mathf.Approximately(map.GetColor(Vector3Int.zero).a,1),"Tile restores opacity and collision after three seconds");
                Check(map.GetColliderType(Vector3Int.right)==Tile.ColliderType.None,"Neighbor has its own timer");phase=3;return;
            }
            if(phase==3){if(Time.time-started<4.7f)return;
                Check(map.GetColliderType(Vector3Int.right)==Tile.ColliderType.Grid,"Second cell independently restores");bool respawned;rules.TryInteractCell(Vector3Int.zero,player,out respawned);rules.enabled=false;Check(map.GetColliderType(Vector3Int.zero)==Tile.ColliderType.Grid,"Disabling map restores pending cells");
                blue.TryInteract(player);blue.enabled=false;Check(blue.GetComponent<Collider2D>().enabled,"Disabling block restores pending collider");
                red=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SetupEmotionBlocks.Dir+"Prefabs/Block_anger.prefab"),new Vector3(1000,0),Quaternion.identity).GetComponent<EmotionBlock>();
                camera=new GameObject("ShatterPreviewCamera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.25f;camera.transform.position=new Vector3(1000,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.1f,.15f);
                Capture();player.TrySetEmotion(RobotEmotion.Anger);Check(red.TryInteract(player) && !red.gameObject.activeSelf,"Anger removes collider immediately");
                var effect=UnityEngine.Object.FindObjectOfType<BlockShatterEffect>();Check(effect && effect.GetComponentsInChildren<SpriteRenderer>().Length==16,"Sixteen textured shards spawned");started=lastFrame=Time.time;phase=4;return;
            }
            if(phase==4){if(Time.time-lastFrame>=.055f){Capture();lastFrame=Time.time;}
                if(Time.time-started<.75f)return;Check(!UnityEngine.Object.FindObjectOfType<BlockShatterEffect>(),"Shatter effect cleans itself up");Finish(null);
            }
        }catch(Exception e){Finish("FAIL: "+e.Message);}}
        static void Capture(){
            Directory.CreateDirectory("Logs/BlockRecovery/frames");var rt=new RenderTexture(384,256,24);var old=RenderTexture.active;Texture2D image=null;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(384,256,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,384,256),0,0);image.Apply();File.WriteAllBytes("Logs/BlockRecovery/frames/frame-"+(frame++).ToString("D3")+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);if(image)UnityEngine.Object.DestroyImmediate(image);}
        }
        static void Finish(string error){if(error!=null)results.Add(error);Directory.CreateDirectory("Logs/BlockRecovery");File.WriteAllLines("Logs/BlockRecovery/verification.txt",results);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;Debug.Log(string.Join("\n",results));}
    }
}
