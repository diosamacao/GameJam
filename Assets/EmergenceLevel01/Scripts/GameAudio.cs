using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    // A bounded, persistent 2D voice pool. Missing clips and disabled cues are silent.
    public sealed class GameAudio : MonoBehaviour
    {
        static GameAudio instance;
        public GameAudioConfig config;
        [Range(0,1)] public float volume=1;
        public bool muted;
        sealed class Voice {public AudioSource source;public float until,gain;public int priority;}
        Voice[] voices;
        readonly float[] lastPlayed=new float[System.Enum.GetValues(typeof(GameSound)).Length];
        AudioListener fallbackListener;AudioListener[] listeners=new AudioListener[0];
        public static GameAudio Instance=>instance;
        public int PlayCount {get;private set;}
        public GameSound LastSound {get;private set;}
        public int PoolSize=>voices==null?0:voices.Length;
        public static float FootstepInterval=>instance && instance.config?Mathf.Max(.05f,instance.config.footstepInterval):.28f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatic(){instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install(){if(!instance)new GameObject("GameAudio").AddComponent<GameAudio>();}
        void Awake(){
            if(instance && instance!=this){Destroy(gameObject);return;}
            instance=this;DontDestroyOnLoad(gameObject);
            if(!config)config=Resources.Load<GameAudioConfig>("GameAudioConfig");
            for(int i=0;i<lastPlayed.Length;i++)lastPlayed[i]=float.NegativeInfinity;
            BuildPool();fallbackListener=gameObject.AddComponent<AudioListener>();
            SceneManager.sceneLoaded+=Loaded;RefreshListeners();
        }
        void BuildPool(){
            int count=config?Mathf.Clamp(config.maxVoices,1,32):12;
            if(voices!=null && voices.Length==count)return;
            if(voices!=null)foreach(var voice in voices)if(voice.source){voice.source.Stop();Destroy(voice.source.gameObject);}
            voices=new Voice[count];for(int i=0;i<count;i++){
                var go=new GameObject("SFX Voice "+i);go.transform.SetParent(transform,false);
                var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.dopplerLevel=0;
                voices[i]=new Voice{source=source};
            }
        }
        void Loaded(Scene scene,LoadSceneMode mode){RefreshListeners();}
        public void RefreshListeners(){listeners=FindObjectsOfType<AudioListener>(true);UpdateListener();}
        void UpdateListener(){bool external=false;foreach(var listener in listeners)if(listener && listener!=fallbackListener && listener.isActiveAndEnabled){external=true;break;}if(fallbackListener)fallbackListener.enabled=!external;}
        void LateUpdate(){
            UpdateListener();BuildPool();float master=muted || !config?0:Mathf.Clamp01(volume)*Mathf.Clamp01(config.masterVolume);
            foreach(var voice in voices)voice.source.volume=voice.gain*master;
        }
        public static bool Play(GameSound sound,Vector3 position=default){return instance && instance.TryPlay(sound,position);}
        public static void Movement(GameSound sound,RobotEmotionGameplay actor,Vector3 position){if(instance && instance.config && (!actor || !actor.isNpc || instance.config.npcMovementSounds))Play(sound,position);}
        public static GameSound EmotionSound(RobotEmotion emotion){switch(emotion){case RobotEmotion.Anger:case RobotEmotion.StrongAnger:return GameSound.Anger;case RobotEmotion.Sadness:return GameSound.Sadness;case RobotEmotion.Courage:return GameSound.Courage;case RobotEmotion.Guardian:return GameSound.Guardian;case RobotEmotion.Memory:return GameSound.Memory;default:return GameSound.Joy;}}
        public bool TryPlay(GameSound sound,Vector3 position){
            var cue=config?config.Find(sound):null;int index=(int)sound;
            if(index<0 || index>=lastPlayed.Length || muted || volume<=0 || !config || config.masterVolume<=0 || cue==null || !cue.enabled || cue.volume<=0 || cue.clips==null || cue.clips.Length==0)return false;
            if(Time.unscaledTime-lastPlayed[index]<Mathf.Max(0,cue.cooldown))return false;
            var clip=cue.clips[Random.Range(0,cue.clips.Length)];if(!clip)return false;
            float attenuation=1;
            if(cue.audibleDistance>0){var camera=Camera.main;if(camera)attenuation=Mathf.Clamp01(1-Vector2.Distance(camera.transform.position,position)/cue.audibleDistance);}
            if(attenuation<=0)return false;
            BuildPool();Voice chosen=null;
            foreach(var voice in voices){if(Time.unscaledTime>=voice.until){chosen=voice;break;}if(voice.priority>=cue.priority && (chosen==null || voice.priority>chosen.priority || (voice.priority==chosen.priority && voice.until<chosen.until)))chosen=voice;}
            if(chosen==null)return false;
            float low=Mathf.Clamp(Mathf.Min(cue.pitchRange.x,cue.pitchRange.y),.1f,3);float high=Mathf.Clamp(Mathf.Max(cue.pitchRange.x,cue.pitchRange.y),low,3);
            var audio=chosen.source;audio.Stop();audio.ignoreListenerPause=sound==GameSound.DialogueLine;audio.clip=clip;audio.pitch=Random.Range(low,high);audio.priority=cue.priority;
            chosen.gain=Mathf.Clamp01(cue.volume)*attenuation;audio.volume=chosen.gain*Mathf.Clamp01(config.masterVolume)*Mathf.Clamp01(volume);
            chosen.priority=cue.priority;chosen.until=Time.unscaledTime+clip.length/audio.pitch;audio.Play();
            lastPlayed[index]=Time.unscaledTime;LastSound=sound;PlayCount++;return true;
        }
        public void StopAll(){if(voices==null)return;foreach(var voice in voices){voice.source.Stop();voice.until=0;}}
        public void SetVolume(float value){volume=Mathf.Clamp01(value);}
        public void SetMuted(bool value){muted=value;}
        void OnDestroy(){SceneManager.sceneLoaded-=Loaded;if(instance==this)instance=null;}
    }
}
