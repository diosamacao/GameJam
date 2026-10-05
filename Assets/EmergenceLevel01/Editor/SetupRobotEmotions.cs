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
        const string Art=Root+"Art/Characters/OriginalBody/";
        static readonly string[] Emotions={"joy","anger","sadness","delight","blank"};
        static readonly string[] Bodies={"walk_0","walk_1","walk_2","walk_3","idle","jump","fall","land"};
        [MenuItem("Tools/Emergence/Setup Modular Robot Emotions")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
            Directory.CreateDirectory(Art);
            // Top-left pixel coordinates from the original 32x40 animation sheets.
            var rects=new[]{new RectInt(10,13,12,10),new RectInt(11,13,12,10),new RectInt(10,13,12,10),new RectInt(11,13,12,10),new RectInt(11,13,11,10),new RectInt(10,14,12,10),new RectInt(10,12,12,10),new RectInt(12,18,12,10)};
            var anchors=new Vector2[Bodies.Length];
            for(int i=0;i<Bodies.Length;i++) {
                var body=Read(Root+"Art/Characters/robot_"+Bodies[i]+".png");
                var r=rects[i];
                if(body.width!=32 || body.height!=40)throw new Exception("Unexpected original body dimensions");
                for(int y=r.yMin;y<r.yMax;y++)for(int x=r.xMin;x<r.xMax;x++)body.SetPixel(x,39-y,Color.clear);
                body.Apply();
                anchors[i]=new Vector2((r.x+r.width*.5f-16)/16f,(40-r.yMax-1)/16f);
                File.WriteAllBytes(Art+"body_"+Bodies[i]+".png",body.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(body);
            }
            foreach(var name in Emotions) {
                var source=Read("ArtSource/RobotMonitorHeads/robot_head_"+name+".png");
                var box=Bounds(source,new RectInt(0,0,source.width,source.height));
                var result=new Texture2D(160,112,TextureFormat.RGBA32,false);
                result.SetPixels32(new Color32[160*112]);
                Blit(source,box,result,8,0,144,Mathf.RoundToInt(144f*box.height/box.width));
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
            string labPath=Root+"Generated/Level01_Laboratory.prefab";
            if(File.Exists(labPath)) {
                var lab=PrefabUtility.LoadPrefabContents(labPath);
                try {foreach(var motor in lab.GetComponentsInChildren<RobotMotor2D>(true))Configure(motor,config);PrefabUtility.SaveAsPrefabAsset(lab,labPath);}
                finally {PrefabUtility.UnloadPrefabContents(lab);}
            }
            // Open other game scenes additively; preserve the user's active scene and layout.
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"})) {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
                bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                bool changed=false;
                foreach(var root in scene.GetRootGameObjects())foreach(var motor in root.GetComponentsInChildren<RobotMotor2D>(true)) {
                    Configure(motor,config);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(motor.visual);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(motor.GetComponent<RobotEmotionController>());
                    changed=true;
                }
                if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            AssetDatabase.SaveAssets();Debug.Log("ROBOT_EMOTIONS_READY");
        }
        static void Configure(RobotMotor2D motor,RobotEmotionConfig config)
        {
            if(!motor || !motor.visual) throw new Exception("Player motor/visual missing");
            motor.idle=Sprite("body_idle");motor.walk=new[]{Sprite("body_walk_0"),Sprite("body_walk_1"),Sprite("body_walk_2"),Sprite("body_walk_3")};motor.visual.sprite=motor.idle;motor.jump=Sprite("body_jump");motor.fall=Sprite("body_fall");motor.land=Sprite("body_land");
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
