using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    // Session-only state. A scene handle prevents an old checkpoint leaking into a normal revisit.
    public static class SceneCheckpointState
    {
        static bool hasCheckpoint,reloading;
        static int sceneHandle;
        static string scenePath;
        static Vector2 position;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Clear();SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Clear(){hasCheckpoint=reloading=false;sceneHandle=0;scenePath=null;position=Vector2.zero;}
        public static void Activate(Scene scene,Vector2 point){hasCheckpoint=true;sceneHandle=scene.handle;scenePath=scene.path;position=point;}
        public static bool TryGet(Scene scene,out Vector2 point){point=position;return hasCheckpoint && (scene.handle==sceneHandle || (reloading && scene.path==scenePath));}
        public static void PrepareReload(Scene scene){if(!TryGet(scene,out _))hasCheckpoint=false;sceneHandle=scene.handle;scenePath=scene.path;reloading=true;}
        static void Loaded(Scene scene,LoadSceneMode mode){
            if(reloading && scene.path==scenePath){sceneHandle=scene.handle;reloading=false;return;}
            if(mode==LoadSceneMode.Single && scene.handle!=sceneHandle)Clear();
        }
    }
}
