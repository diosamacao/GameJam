using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public static class SetupDoctorAndLevelFlow
    {
        const string Root="Assets/EmergenceLevel01/";
        const string Art=Root+"Art/Characters/DoctorEmotions/";
        public const string ConfigPath=Root+"Generated/LevelFlow.asset";
        [MenuItem("Tools/Emergence/Setup Doctor And Level Flow")]
        public static void Build(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("请先退出运行模式。");
            Directory.CreateDirectory(Root+"Dialogue/Returns");AssetDatabase.Refresh();
            var names=new[]{"joy","anger","sadness","courage","guardian","memory"};
            var sprites=names.Select(Import).ToArray();
            string materialPath=Root+"Generated/DoctorPixel.mat";var pixelMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!pixelMaterial){pixelMaterial=new Material(Shader.Find("Emergence/DoctorPixel"));AssetDatabase.CreateAsset(pixelMaterial,materialPath);}
            string doctorPath=Root+"Prefabs/Characters/Scientist.prefab";
            var doctor=PrefabUtility.LoadPrefabContents(doctorPath);
            try{
                var interaction=doctor.GetComponent<DoctorInteraction>();if(!interaction){interaction=doctor.AddComponent<DoctorInteraction>();interaction.interactionHint="E 与博士交谈";}
                var appearance=doctor.GetComponent<DoctorIdleAppearance>();if(!appearance)appearance=doctor.AddComponent<DoctorIdleAppearance>();
                appearance.visual=doctor.GetComponentInChildren<SpriteRenderer>(true);appearance.workAnimator=doctor.GetComponentInChildren<Animator>(true);
                appearance.neutral=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Characters/scientist_idle.png");
                appearance.pixelMaterial=pixelMaterial;
                appearance.joy=sprites[0];appearance.anger=sprites[1];appearance.sadness=sprites[2];appearance.courage=sprites[3];appearance.guardian=sprites[4];appearance.memory=sprites[5];
                PrefabUtility.SaveAsPrefabAsset(doctor,doctorPath);
            }finally{PrefabUtility.UnloadPrefabContents(doctor);}
            string variants=Root+"Prefabs/Characters/Doctors/";Directory.CreateDirectory(variants);AssetDatabase.Refresh();
            var emotionValues=new[]{RobotEmotion.Joy,RobotEmotion.Anger,RobotEmotion.Sadness,RobotEmotion.Courage,RobotEmotion.Guardian,RobotEmotion.Memory};
            for(int i=0;i<names.Length;i++){
                string variantPath=variants+"Doctor_"+names[i]+".prefab";if(File.Exists(variantPath))continue;
                var variant=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(doctorPath));
                try{variant.name="Doctor_"+names[i];variant.transform.position=Vector3.zero;variant.GetComponent<DoctorIdleAppearance>().emotion=emotionValues[i];var interaction=variant.GetComponent<DoctorInteraction>();interaction.unlockEmotion=emotionValues[i];interaction.interactOnce=true;PrefabUtility.SaveAsPrefabAsset(variant,variantPath);}finally{UnityEngine.Object.DestroyImmediate(variant);}
            }
            string[] scenes=Directory.GetFiles("Assets/Scenes","Level*_InitialLab.unity").Select(p=>p.Replace('\\','/')).OrderBy(p=>p).ToArray();
            var config=AssetDatabase.LoadAssetAtPath<LevelFlowConfig>(ConfigPath);
            if(!config){
                config=ScriptableObject.CreateInstance<LevelFlowConfig>();var routes=new List<LevelFlowConfig.Route>();
                for(int i=0;i<scenes.Length-1;i++){
                    var route=new LevelFlowConfig.Route{fromScene=Path.GetFileNameWithoutExtension(scenes[i]),nextScene=Path.GetFileNameWithoutExtension(scenes[i+1])};
                    if(i>0){string dialoguePath=Root+"Dialogue/Returns/ReturnFromLevel"+(i+1).ToString("00")+".asset";var dialogue=AssetDatabase.LoadAssetAtPath<DialogueSequence>(dialoguePath);
                        if(!dialogue){dialogue=ScriptableObject.CreateInstance<DialogueSequence>();dialogue.stagingNotes="返回实验室的占位台词，请策划替换；对话结束后自动前往 "+route.nextScene;dialogue.lines=new[]{new DialogueSequence.Line{speaker="博士",text="这次实验的记录已经保存。<pause=0.5>准备好后，我们继续下一阶段。"}};AssetDatabase.CreateAsset(dialogue,dialoguePath);}route.laboratoryDialogue=dialogue;
                    }
                    routes.Add(route);
                }
                config.routes=routes.ToArray();AssetDatabase.CreateAsset(config,ConfigPath);
            }
            // Keep unrelated build entries, and register all playable levels with level 1 first.
            var builds=scenes.Select(p=>new EditorBuildSettingsScene(p,true)).ToList();builds.AddRange(EditorBuildSettings.scenes.Where(s=>!scenes.Contains(s.path)));EditorBuildSettings.scenes=builds.ToArray();
            string exitPath=Root+"Prefabs/Gameplay/LevelExit.prefab";
            if(!File.Exists(exitPath)){
                var exit=new GameObject("LevelExit");try{
                    var component=exit.AddComponent<LevelExitInteraction>();component.flow=config;component.interactionHint="E 前往下一关";
                    var sprite=exit.AddComponent<SpriteRenderer>();sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Tiles64/EmotionBlocks/reinforced.png");sprite.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Generated/LabSpriteUnlit.mat");sprite.color=new Color(.2f,.85f,1);sprite.sortingOrder=15;
                    PrefabUtility.SaveAsPrefabAsset(exit,exitPath);
                }finally{UnityEngine.Object.DestroyImmediate(exit);}
            }
            AssetDatabase.SaveAssets();Debug.Log("DOCTOR_AND_LEVEL_FLOW_READY");
        }
        static Sprite Import(string name){
            string path=Art+"doctor_"+name+".png";if(!File.Exists(path))throw new FileNotFoundException(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.maxTextureSize=2048;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>127){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            if(maxY<minY)throw new Exception("Empty doctor sprite: "+path);
            // Original scientist silhouette is 38 pixels tall at 16 PPU; align feet and world-space height.
            importer.spritePixelsPerUnit=(maxY-minY+1)/(38f/16f);
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2((minX+maxX+1)*.5f/texture.width,(float)minY/texture.height);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.isReadable=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        [MenuItem("Tools/Emergence/Preview Doctor Emotions")]
        public static void Preview(){
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();Texture2D sheet=null;RenderTexture rt=null;var old=RenderTexture.active;
            try{
                var doctor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Characters/Scientist.prefab"),scene);doctor.transform.position=Vector3.zero;
                var appearance=doctor.GetComponent<DoctorIdleAppearance>();var cameraGo=new GameObject("DoctorPreviewCamera",typeof(Camera));SceneMove(cameraGo,scene);var camera=cameraGo.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=1.6f;camera.transform.position=new Vector3(0,1.2f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.13f,.18f);
                rt=new RenderTexture(128,160,24);camera.targetTexture=rt;sheet=new Texture2D(896,160,TextureFormat.RGB24,false);
                var values=new[]{RobotEmotion.Blank,RobotEmotion.Joy,RobotEmotion.Anger,RobotEmotion.Sadness,RobotEmotion.Courage,RobotEmotion.Guardian,RobotEmotion.Memory};
                for(int i=0;i<values.Length;i++){appearance.emotion=values[i];appearance.Apply();camera.Render();RenderTexture.active=rt;var tile=new Texture2D(128,160,TextureFormat.RGB24,false);tile.ReadPixels(new Rect(0,0,128,160),0,0);tile.Apply();sheet.SetPixels(i*128,0,128,160,tile.GetPixels());UnityEngine.Object.DestroyImmediate(tile);}
                sheet.Apply();Directory.CreateDirectory("Logs/DoctorFlow");File.WriteAllBytes("Logs/DoctorFlow/doctor-idle-preview.png",sheet.EncodeToPNG());
            }finally{RenderTexture.active=old;if(rt)UnityEngine.Object.DestroyImmediate(rt);if(sheet)UnityEngine.Object.DestroyImmediate(sheet);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void SceneMove(GameObject go,UnityEngine.SceneManagement.Scene scene){UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);}
    }
}
