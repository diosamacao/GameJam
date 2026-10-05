# 机器人表情与模块化动画

已接入 PlayerRobot.prefab 和 Level01_InitialLab 场景。运行后先点击 Game 窗口取得键盘焦点。

Y=喜（Joy），U=怒（Anger），I=哀（Sadness），O=乐（Delight），P=空白（Blank）。默认空白；A/D移动，空格跳跃。PlayerRobot上的RobotMotor2D组件提供jumpHeight（Jump Height）配置，默认2个世界单位；运行时修改会在下一次跳跃生效。空中不能二次跳跃，按住空格不会自动连跳。

## 对外接口

```csharp
using Emergence.Level01;
var emotions = player.GetComponent<RobotEmotionController>();
bool changedSuccessfully = emotions.SetEmotion(RobotEmotion.Joy);
RobotEmotion current = emotions.CurrentEmotion;
emotions.EmotionChanged += value => UnityEngine.Debug.Log(value);
```

SetEmotion 返回是否成功设置；重复设置同一表情不重复触发事件；无效值或缺少对应素材返回 false 并保留当前状态。SetEmotionByIndex(int) 可用于 Inspector 的 UnityEvent，索引0~4对应喜、怒、哀、乐、空白。

配置文件：Generated/RobotEmotions.asset。可在 Inspector 替换五张头部 Sprite 与身体帧的颈部锚点。
测试输入独立在 RobotEmotionKeyboardTest，取消 enableKeyboardTest 或禁用组件即可移除测试按键，不影响业务接口。

## 美术与动画

Art/Characters/OriginalBody 使用最初版机器人的身体：32×40，PPU16，只去除原头部，保留原有身体像素。包含四帧行走、待机、起跳、下落、落地。五张显示器头部为160×112画布、PPU128，显示器可见宽144像素，相当于身体18像素。使用Point过滤、无压缩。

头部源图位于 ArtSource/RobotMonitorHeads。每个身体帧使用独立接头位置，显示器底边覆盖原接缝1个身体像素；地形规格仍为64×64。

头身分层，头部在LateUpdate根据当前身体Sprite选择锚点并同步翻转，不会被走路动画覆盖。移动控制器根据地面接触和竖直速度播放待机、行走、起跳、下落与落地，表情独立保持。对外可调用RequestJump()请求跳跃，通过IsGrounded读取地面状态。运行时在Inspector修改的值退出Play后会恢复；要长期保存请在编辑模式修改并保存场景或预制体。

Tools > Emergence > Setup Modular Robot Emotions 可重新导出并配置素材（会覆盖OriginalBody导出图及默认配置，请在修改配置前备份）。
Tools > Emergence > Verify Robot Emotions 运行接口与动画集成检查，报告输出 Logs/RobotEmotions/verification.txt。
