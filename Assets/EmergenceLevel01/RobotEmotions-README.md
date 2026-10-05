# 机器人表情与模块化动画

已接入 PlayerRobot.prefab 和 Level01_InitialLab 场景。运行后先点击 Game 窗口取得键盘焦点。

默认空表情；A/D移动，空格跳跃。地面按住Q满1秒立即自动切换并恢复移动，无需松开；每次按住仅切换一次，松开再按可再次切换，按开心→愤怒→悲伤→开心顺序循环；初始空表情首次切到开心。无需方向键选择。未满1秒提前松开、Esc、失焦或失去地面会取消。期间显示空表情，锁定水平移动和跳跃，但不冻结重力。喜/开心跳3格，其他状态跳2格，悲伤能力暂未定义，乐为备用。YUIOP调试快捷键默认关闭。

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

## 情绪玩法配置

Generated/EmotionAbilities.asset：holdSeconds=1、baseJumpCells=2、happyJumpCells=3。格高由玩家RobotEmotionGameplay.levelGrid读取，未绑定Grid时使用fallbackCellHeight=1。运行时修改配置会作用于下一次起跳；已开始的跳跃不会改变。启用情绪系统后，Motor.jumpHeight仅作为禁用情绪系统时的备用参数。

RobotEmotionGameplay提供BeginSwitch、ReleaseSwitch、CancelSwitch；IsSwitching、Progress、CurrentEmotion、CanBreak可供其他系统读取。RobotEmotionController.SetEmotion仍可由剧情等外部系统立即切换，但蓄力期间返回false。表情层在切换期间为Blank，玩法CurrentEmotion保留原情绪，便于取消时恢复。

## 制作可破坏关卡

- 独立方块：将Prefabs/Gameplay/AngerBreakableBlock.prefab拖入场景。橙色外框表示可破坏。需要实体Collider2D；仅添加EmotionBreakable的方块可被愤怒破坏，onBroken可挂接音效或特效。
- Tile Palette：将Generated/AngerBreakableTile.asset拖入Palette；将Prefabs/Gameplay/AngerBreakableTilemap.prefab放入关卡，在其BreakableTiles子物体上绘制。自带单格示例，可擦掉后重新画。
- 现有Tilemap：挂载EmotionBreakableTilemap，将允许破坏的Tile资产填入breakableTiles，并确保TilemapCollider2D存在。allTilesBreakable只适用于专用可破坏层，默认关闭。每次只删除接触到的单格，普通地面和未标记Tile保持不变。
- 方块在当前关卡会话中移除；重新加载关卡后恢复。当前未实现跨关卡存档。

Tools > Emergence > Setup Emotion Gameplay重新接入配置和预制体；Verify Emotion Gameplay执行蓄力、跳跃高度和破坏检查，报告在Logs/EmotionGameplay/verification.txt。