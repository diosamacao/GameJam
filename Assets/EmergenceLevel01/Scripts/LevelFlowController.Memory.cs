using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    public sealed partial class LevelFlowController
    {
        bool memoryTrip;
        Scene memorySource;
        public static bool IsMemoryTrip=>instance && instance.memoryTrip;
        public static bool IsMemorySource(Scene scene)=>IsMemoryTrip && instance.memorySource==scene;
        public static bool BeginMemory(MemorySceneConfig config){
            if(!config || IsTransitioning || (DialogueDirector.Instance && DialogueDirector.Instance.IsPlaying))return false;
            if(string.IsNullOrWhiteSpace(config.memoryScene) || !Application.CanStreamedLevelBeLoaded(config.memoryScene)){
                Debug.LogError("回忆场景未加入 Build Settings："+config.memoryScene,config);return false;
            }
            if(!config.dialogue || config.dialogue.lines==null || config.dialogue.lines.Length==0){Debug.LogError("请给回忆配置指定有效的对话。",config);return false;}
            foreach(var line in config.dialogue.lines)if(line==null || string.IsNullOrWhiteSpace(line.text)){Debug.LogError("回忆对话含空白段落。",config);return false;}
            if(!instance){var go=new GameObject("LevelFlowController");instance=go.AddComponent<LevelFlowController>();DontDestroyOnLoad(go);}
            instance.memorySource=SceneManager.GetActiveScene();instance.memoryTrip=instance.busy=true;
            instance.StartCoroutine(instance.MemoryTrip(config));return true;
        }
        IEnumerator MemoryTrip(MemorySceneConfig config){
            var source=memorySource;
            var activeRoots=new List<GameObject>();
            var savedBodies=new Dictionary<Rigidbody2D,Vector2>();
            var angularVelocities=new Dictionary<Rigidbody2D,float>();
            Scene recalled=default;
            DialogueDirector memoryDirector=null;
            bool dialogueEnded=false;
            System.Action<DialogueSequence> ended=sequence=>{if(sequence==config.dialogue)dialogueEnded=true;};
            try{
                GameAudio.Play(recalled.IsValid()?GameSound.MemoryReturn:GameSound.MemoryEnter);
                fading=true;yield return Fade(1,config.fadeOutSeconds);yield return null;
                // Suspend only active roots; objects already inactive stay inactive on return.
                foreach(var root in source.GetRootGameObjects()){
                    foreach(var body in root.GetComponentsInChildren<Rigidbody2D>(true)){savedBodies[body]=body.velocity;angularVelocities[body]=body.angularVelocity;}
                    if(root.activeSelf)activeRoots.Add(root);
                }
                foreach(var root in activeRoots)if(root)root.SetActive(false);
                var operation=SceneManager.LoadSceneAsync(config.memoryScene,LoadSceneMode.Additive);
                if(operation==null){Debug.LogError("回忆场景加载失败："+config.memoryScene);yield break;}
                yield return operation;
                recalled=SceneManager.GetSceneAt(SceneManager.sceneCount-1);
                SceneManager.SetActiveScene(recalled);
                yield return null;
                if(config.blackHoldSeconds>0)yield return new WaitForSecondsRealtime(config.blackHoldSeconds);
                yield return Fade(0,config.fadeInSeconds);fading=false;
                memoryDirector=DialogueDirector.Instance;
                if(!memoryDirector){var go=new GameObject("Memory Dialogue");SceneManager.MoveGameObjectToScene(go,recalled);memoryDirector=go.AddComponent<DialogueDirector>();}
                memoryDirector.Finished+=ended;memoryDirector.Cancelled+=ended;
                if(!memoryDirector.TryPlay(config.dialogue)){Debug.LogError("回忆对话无法开始，将返回原场景。",config);dialogueEnded=true;}
                while(memoryDirector && !dialogueEnded)yield return null;
                if(memoryDirector){memoryDirector.Finished-=ended;memoryDirector.Cancelled-=ended;}
                GameAudio.Play(recalled.IsValid()?GameSound.MemoryReturn:GameSound.MemoryEnter);
                fading=true;yield return Fade(1,config.fadeOutSeconds);yield return null;
                if(source.IsValid() && source.isLoaded)SceneManager.SetActiveScene(source);
                if(recalled.IsValid() && recalled.isLoaded)yield return SceneManager.UnloadSceneAsync(recalled);
                foreach(var root in activeRoots)if(root)root.SetActive(true);
                foreach(var pair in savedBodies)if(pair.Key){pair.Key.velocity=pair.Value;pair.Key.angularVelocity=angularVelocities[pair.Key];}
                if(config.blackHoldSeconds>0)yield return new WaitForSecondsRealtime(config.blackHoldSeconds);
                yield return Fade(0,config.fadeInSeconds);
            }finally{
                if(memoryDirector){memoryDirector.Finished-=ended;memoryDirector.Cancelled-=ended;}
                if(source.IsValid() && source.isLoaded)SceneManager.SetActiveScene(source);
                foreach(var root in activeRoots)if(root && !root.activeSelf)root.SetActive(true);
                if(recalled.IsValid() && recalled.isLoaded)SceneManager.UnloadSceneAsync(recalled);
                opacity=0;fading=busy=memoryTrip=false;memorySource=default;
                EmotionResonance.InvalidateStates();
            }
        }
    }
}
