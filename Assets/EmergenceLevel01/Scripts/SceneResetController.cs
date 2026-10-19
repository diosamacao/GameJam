using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    public sealed class SceneResetController : MonoBehaviour
    {
        public KeyCode resetKey=KeyCode.R;
        [Tooltip("填写时使用已有换关黑幕时长。留空会查找场景出口的Flow配置。")]
        public LevelFlowConfig fadeConfig;
        [Min(0)] public float fadeOutSeconds=.35f, fadeInSeconds=.35f, blackHoldSeconds=.1f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode){
            foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<SceneResetController>(true))return;
            var go=new GameObject("SceneResetController");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<SceneResetController>();
        }
        void Update(){if(gameObject.scene==SceneManager.GetActiveScene() && Input.GetKeyDown(resetKey))TryReset();}
        public void ResetScene(){TryReset();}
        public bool TryReset(){
            if(GameMenuUI.BlocksGameplay || gameObject.scene!=SceneManager.GetActiveScene())return false;
            var config=fadeConfig;
            if(!config)foreach(var exit in FindObjectsOfType<LevelExitInteraction>())if(exit.gameObject.scene==gameObject.scene && exit.flow){config=exit.flow;break;}
            return LevelFlowController.ResetCurrentScene(config?config.fadeOutSeconds:fadeOutSeconds,config?config.fadeInSeconds:fadeInSeconds,config?config.blackHoldSeconds:blackHoldSeconds);
        }
    }
}
