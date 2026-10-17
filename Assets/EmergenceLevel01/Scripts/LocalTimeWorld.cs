using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    // Scene objects with physical/animated state participate automatically; gameplay timers use LocalTimeTarget.DeltaTime.
    public static class LocalTimeWorld
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode){foreach(var root in scene.GetRootGameObjects()){
            foreach(var body in root.GetComponentsInChildren<Rigidbody2D>(true))Ensure(body.gameObject);
            foreach(var animator in root.GetComponentsInChildren<Animator>(true))Ensure(animator.gameObject);
            foreach(var particle in root.GetComponentsInChildren<ParticleSystem>(true))Ensure(particle.gameObject);
        }}
        public static void Ensure(GameObject go){var actor=go.GetComponentInParent<RobotEmotionGameplay>();if(actor && !actor.isNpc)return;if(!go.GetComponent<LocalTimeTarget>())go.AddComponent<LocalTimeTarget>();}
    }
}
