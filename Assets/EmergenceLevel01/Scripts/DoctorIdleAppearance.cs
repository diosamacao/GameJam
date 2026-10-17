using UnityEngine;
namespace Emergence.Level01
{
    public sealed class DoctorIdleAppearance : MonoBehaviour
    {
        public SpriteRenderer visual;
        public Animator workAnimator;
        public Sprite neutral,joy,anger,sadness,courage,guardian,memory;
        public RobotEmotion emotion=RobotEmotion.Blank;
        [Tooltip("开启时使用情感Idle；关闭时恢复原工作动画。")]
        public bool useEmotionIdle=true;
        public Material pixelMaterial;
        [Min(1)] public float pixelsPerUnit=16;
        [Range(0,1)] public float alphaCutoff=.5f;
        Material originalMaterial;MaterialPropertyBlock properties;
        bool captured, animatorWasEnabled;
        void OnEnable(){if(visual)originalMaterial=visual.sharedMaterial;if(workAnimator){animatorWasEnabled=workAnimator.enabled;captured=true;}}
        void LateUpdate(){Apply();}
        public void Apply(){
            if(workAnimator)workAnimator.enabled=useEmotionIdle?false:animatorWasEnabled;
            if(!visual)return;
            if(!useEmotionIdle){visual.sharedMaterial=originalMaterial;visual.SetPropertyBlock(null);return;}
            Sprite sprite=neutral;switch(emotion){case RobotEmotion.Joy:sprite=joy;break;case RobotEmotion.Anger:sprite=anger;break;case RobotEmotion.Sadness:sprite=sadness;break;case RobotEmotion.Courage:sprite=courage;break;case RobotEmotion.Guardian:sprite=guardian;break;case RobotEmotion.Memory:sprite=memory;break;}
            if(!sprite)sprite=neutral;if(!sprite)return;visual.sprite=sprite;
            if(pixelMaterial){
                visual.sharedMaterial=pixelMaterial;if(properties==null)properties=new MaterialPropertyBlock();visual.GetPropertyBlock(properties);
                properties.SetFloat("_Cutoff",alphaCutoff);properties.SetVector("_LogicalSize",new Vector4(sprite.texture.width/sprite.pixelsPerUnit*pixelsPerUnit,sprite.texture.height/sprite.pixelsPerUnit*pixelsPerUnit,0,0));
                properties.SetVector("_Pivot",new Vector4(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height,0,0));visual.SetPropertyBlock(properties);
            }
        }
        public void SetEmotion(RobotEmotion value){emotion=value;}
        public void SetEmotionByIndex(int value){emotion=(RobotEmotion)value;}
        void OnDisable(){if(captured && workAnimator)workAnimator.enabled=animatorWasEnabled;if(visual){visual.sharedMaterial=originalMaterial;visual.SetPropertyBlock(null);}}
    }
}
