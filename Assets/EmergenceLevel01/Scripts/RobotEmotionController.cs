using System;
using UnityEngine;
namespace Emergence.Level01
{
    [DisallowMultipleComponent]
    public sealed class RobotEmotionController : MonoBehaviour
    {
        public RobotEmotionConfig config;
        public int headSortingOffset=1;
        public SpriteRenderer bodyRenderer;
        public SpriteRenderer headRenderer;
        [SerializeField] RobotEmotion currentEmotion = RobotEmotion.Blank;
        public RobotEmotion CurrentEmotion => currentEmotion;
        public event Action<RobotEmotion> EmotionChanged;
        SpriteRenderer screenRenderer;

        void OnEnable() { RefreshVisual(); }
        void LateUpdate() { RefreshVisual(); }
        public bool SetEmotion(RobotEmotion emotion)
        {
            var gameplay=GetComponent<RobotEmotionGameplay>();
            return gameplay && gameplay.isActiveAndEnabled ? gameplay.TrySetEmotion(emotion) : SetVisualEmotion(emotion);
        }
        internal bool SetVisualEmotion(RobotEmotion emotion)
        {
            if(!config || !config.GetHead(emotion)) return false;
            bool changed = emotion != currentEmotion;
            currentEmotion = emotion;
            RefreshVisual();
            if(changed) EmotionChanged?.Invoke(emotion);
            return true;
        }
        // Inspector UnityEvent / external integer configuration entry point.
        public void SetEmotionByIndex(int index) { SetEmotion((RobotEmotion)index); }
        public void RefreshVisual()
        {
            if(!headRenderer || !bodyRenderer) return;
            var head = config ? config.GetHead(currentEmotion) : null;
            headRenderer.enabled = enabled && bodyRenderer.enabled && head;
            if(!head) return;
            var screen=config.GetScreen(currentEmotion);
            headRenderer.sprite = screen && config.blank ? config.blank : head;
            if(screen && !screenRenderer){var go=new GameObject("EmotionScreen");go.transform.SetParent(headRenderer.transform,false);screenRenderer=go.AddComponent<SpriteRenderer>();screenRenderer.sharedMaterial=headRenderer.sharedMaterial;}
            if(screenRenderer){screenRenderer.enabled=screen && headRenderer.enabled;screenRenderer.sprite=screen;screenRenderer.transform.localPosition=config.screenOffset;screenRenderer.flipX=bodyRenderer.flipX;screenRenderer.flipY=bodyRenderer.flipY;screenRenderer.color=bodyRenderer.color;screenRenderer.sortingLayerID=bodyRenderer.sortingLayerID;screenRenderer.sortingOrder=bodyRenderer.sortingOrder+headSortingOffset+1;}
            var neck = config.GetNeck(bodyRenderer.sprite);
            // Head sprite uses bottom-centre pivot and is a child of the body renderer.
            headRenderer.transform.localPosition = new Vector3(bodyRenderer.flipX ? -neck.x : neck.x,
                bodyRenderer.flipY ? -neck.y : neck.y, 0);
            headRenderer.flipX = bodyRenderer.flipX;
            headRenderer.flipY = bodyRenderer.flipY;
            headRenderer.color = bodyRenderer.color;
            headRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            headRenderer.sortingOrder = bodyRenderer.sortingOrder + headSortingOffset;
        }
        void OnDisable() { if(headRenderer) headRenderer.enabled = false;if(screenRenderer)screenRenderer.enabled=false; }
    }
}
