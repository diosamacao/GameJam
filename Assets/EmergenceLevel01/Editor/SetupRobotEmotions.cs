using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class SetupRobotEmotions
    {
        const string Root="Assets/EmergenceLevel01/";
        const string Art=Root+"Art/Characters/RobotModular/";
        static readonly string[] Emotions={"joy","anger","sadness","delight","blank"};
        static readonly string[] Bodies={"walk_0","walk_1","walk_2","walk_3","idle","jump","fall","land"};
        [MenuItem("Tools/Emergence/Setup Modular Robot Emotions")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
            Directory.CreateDirectory(Art);
            var bodyImage=Read("ArtSource/RobotMonitorAnimations/body-atlas.png");
            int cw=bodyImage.width/4,ch=bodyImage.height/2;
            var idleRect=Bounds(bodyImage,new RectInt(0,0,cw,ch));
            float scale=44f/idleRect.height;
            var anchors=new Vector2[8];
            for(int i=0;i<8;i++) {
                var area=new RectInt((i%4)*cw,(1-i/4)*ch,cw,ch);
                var box=Bounds(bodyImage,area);
                var result=new Texture2D(64,64,TextureFormat.RGBA32,false);
                result.SetPixels32(new Color32[4096]);
                int w=Mathf.Clamp(Mathf.RoundToInt(box.width*scale),1,62),h=Mathf.Clamp(Mathf.RoundToInt(box.height*scale),1,60);
                Blit(bodyImage,box,result,(64-w)/2,0,w,h);
                int neckY=0;
                for(int y=0;y<64;y++) for(int x=29;x<=34;x++) if(result.GetPixel(x,y).a>.5f) neckY=Mathf.Max(neckY,y);
                anchors[i]=new Vector2(0,(neckY-1)/48f);
                File.WriteAllBytes(Art+"body_"+Bodies[i]+".png",result.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(result);
            }
            UnityEngine.Object.DestroyImmediate(bodyImage);
            foreach(var name in Emotions) {
                var source=Read("ArtSource/RobotMonitorHeads/robot_head_"+name+".png");
                var box=Bounds(source,new RectInt(0,0,source.width,source.height));
                var result=new Texture2D(64,64,TextureFormat.RGBA32,false);
                result.SetPixels32(new Color32[4096]);
                Blit(source,box,result,6,0,52,36);
                File.WriteAllBytes(Art+"head_"+name+".png",result.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(result);UnityEngine.Object.DestroyImmediate(source);
            }
            AssetDatabase.Refresh();
            var config=AssetDatabase.LoadAssetAtPath<RobotEmotionConfig>(Root+"Generated/RobotEmotions.asset");
            if(!config) {config=ScriptableObject.CreateInstance<RobotEmotionConfig>();AssetDatabase.CreateAsset(config,Root+"Generated/RobotEmotions.asset");}
            config.joy=Sprite("head_joy");config.anger=Sprite("head_anger");config.sadness=Sprite("head_sadness");config.delight=Sprite("head_delight");config.blank=Sprite("head_blank");
            config.frames=new RobotEmotionConfig.FrameAnchor[8];
            for(int i=0;i<8;i++) config.frames[i]=new RobotEmotionConfig.FrameAnchor{body=Sprite("body_"+Bodies[i]),neck=anchors[i]};
            config.fallbackNeck=anchors[4];EditorUtility.SetDirty(config);
            string prefab=Root+"Prefabs/Characters/PlayerRobot.prefab";
            var contents=PrefabUtility.LoadPrefabContents(prefab);
            try { Configure(contents.GetComponent<RobotMotor2D>(),config);PrefabUtility.SaveAsPrefabAsset(contents,prefab); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            // Update any scene instance that overrides the original motor sprites.
            foreach(var motor in UnityEngine.Object.FindObjectsOfType<RobotMotor2D>()) {
                Configure(motor,config);
                PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(motor.visual);
                EditorSceneManager.MarkSceneDirty(motor.gameObject.scene);
            }
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path==Root+"Generated/Level01_InitialLab.unity") {
                EditorSceneManager.SaveScene(scene);
                var lab=GameObject.Find("Level01_InitialLaboratory");
                if(lab) PrefabUtility.SaveAsPrefabAsset(lab,Root+"Generated/Level01_Laboratory.prefab");
            }
            AssetDatabase.SaveAssets();Debug.Log("ROBOT_EMOTIONS_READY");
        }
        static void Configure(RobotMotor2D motor,RobotEmotionConfig config)
        {
            if(!motor || !motor.visual) throw new Exception("Player motor/visual missing");
            motor.idle=Sprite("body_idle");motor.walk=new[]{Sprite("body_walk_0"),Sprite("body_walk_1"),Sprite("body_walk_2"),Sprite("body_walk_3")};motor.visual.sprite=motor.idle;
            var controller=motor.GetComponent<RobotEmotionController>();if(!controller)controller=motor.gameObject.AddComponent<RobotEmotionController>();
            var head=motor.visual.transform.Find("EmotionHead");if(!head){head=new GameObject("EmotionHead").transform;head.SetParent(motor.visual.transform,false);}
            var renderer=head.GetComponent<SpriteRenderer>();if(!renderer)renderer=head.gameObject.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial=motor.visual.sharedMaterial;
            controller.config=config;controller.bodyRenderer=motor.visual;controller.headRenderer=renderer;controller.RefreshVisual();
            if(!motor.GetComponent<RobotEmotionKeyboardTest>())motor.gameObject.AddComponent<RobotEmotionKeyboardTest>();
            EditorUtility.SetDirty(motor);EditorUtility.SetDirty(controller);
        }
        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        static Texture2D Read(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!t.LoadImage(File.ReadAllBytes(path)))throw new Exception(path);return t;}
        static RectInt Bounds(Texture2D t,RectInt area)
        {
            int x0=area.xMax,y0=area.yMax,x1=-1,y1=-1;
            for(int y=area.yMin;y<area.yMax;y++)for(int x=area.xMin;x<area.xMax;x++)if(t.GetPixel(x,y).a>.5f){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
            if(x1<x0)throw new Exception("Empty sprite cell");return new RectInt(x0,y0,x1-x0+1,y1-y0+1);
        }
        static void Blit(Texture2D source,RectInt box,Texture2D target,int dx,int dy,int w,int h)
        {
            for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                var c=source.GetPixel(box.x+Mathf.Min(box.width-1,(int)((x+.5f)*box.width/w)),box.y+Mathf.Min(box.height-1,(int)((y+.5f)*box.height/h)));
                c.a=c.a>.5f?1:0;target.SetPixel(dx+x,dy+y,c);
            }
            target.Apply();
        }
    }
}
