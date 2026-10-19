using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Menu Config")]
    public sealed class GameMenuConfig : ScriptableObject
    {
        public string mainMenuScene="MainMenu",firstLevel="Level01_InitialLab";
        public string title="机械情感",subtitle="当数据，开始拥有情感。";
        public Font font;
        public Color background=new Color(.025f,.047f,.075f),panel=new Color(.055f,.095f,.135f);
        public Color accent=new Color(.52f,.93f,.91f),text=new Color(.88f,.95f,.97f);
        public Sprite robotBody,secondRobotBody,doctor;
        public RobotEmotionConfig robotFaces;
        public Vector2 firstRobotPosition=new Vector2(790,490),secondRobotPosition=new Vector2(1040,570),doctorPosition=new Vector2(1080,260);
        public float firstRobotScale=135,secondRobotScale=165,doctorScale=75;
        public float firstRobotRotation=17,secondRobotRotation=-13;
        [Min(0)] public float startFadeSeconds=.35f;
        [Header("开始游戏 · 接入信号过渡（秒）")]
        public string startSignalText="接入信号";
        [Min(0)] public float startBlackWaitSeconds=1f;
        [Min(0)] public float startTextFadeInSeconds=.8f;
        [Min(0)] public float startTextHoldSeconds=2f;
        [Min(0)] public float startTextFadeOutSeconds=1f;
        [Min(1)] public int startTextFontSize=36;
        public KeyCode pauseKey=KeyCode.Escape;
    }
}
