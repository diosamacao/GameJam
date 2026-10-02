# 第一关：门板合成解谜原型

进入 `Assets/EmergenceLevel01/Generated/Level01_InitialLab.unity`，点击 Play，再点击 Game 窗口。

- A/D：机器人左右移动。
- 左键点击地上的门板：选中并显示它对应的门洞虚线框。
- 左键点击虚线框：消耗场景门板并安装到门上。
- 点击另一块：切换选择。点击同一块、空白区域、右键或 Esc：取消选择。
- 两块门板可按任意顺序安装。未完成时出口门保持锁定。
- 完成后左键点击门：上下开门；再次点击关门。

这是点选放置，不需要拖拽，也没有背包、点击距离限制或保存游戏进度；重新进入运行模式会重置谜题。

## 可复用组件

`Prefabs/Puzzles/UpperDoorPiece.prefab` 和 `LowerDoorPiece.prefab` 是可收集合成块；`AssemblyDoor.prefab` 包含两处槽位、安装后的门板、门禁状态和门碰撞；`AssemblySocket.prefab` 是独立槽位模板，使用时需要指定 `installedVisual`。

通过 `AssemblyPiece.partId` 与 `AssemblySocket.acceptedPartId` 匹配素材，例如 `door_upper` / `door_lower`。新增关卡可复用选中、虚线、放置组件。当前完成行为由 `AssemblyDoor` 负责。

虚线是运行时生成的像素线条，不增加图片素材。`AssemblyInteraction` 负责点选及顶部进度提示。门的原有上下动画仍由 `LabDoor` 负责。

## 验证与备份

编辑器菜单 `Tools > Emergence > Verify Assembly Puzzle` 会测试匹配、切换、取消、错误槽位、反序拼装、防止重复放置及开关门，输出 `Logs/AssemblyPuzzle/playmode-verification.txt`。

更改前的场景与整关预制体备份位于 `Logs/AssemblyPuzzle`。`Setup Door Assembly Puzzle` 只用于把旧第一关转换为谜题版本；现有场景已经处理，无需再次生成。
