# 涌现 · GameJam

极简机械科幻像素风的 2D 平台合成解谜原型。

## 打开工程

使用 Unity 2022.3.62f3c1 打开仓库根目录，等待资源导入，打开 `Assets/EmergenceLevel01/Generated/Level01_InitialLab.unity` 后运行。

- A / D：机器人左右移动。
- 点击可合成方块选中，再点击对应虚线位置放入。
- 门的上下两个部件安装完成后，点击门控制开合。
- 右键 / Esc：取消选择。

## 关卡编辑

地形素材每格 64×64 像素，PPU 64，Grid 每格 1×1。打开 Window > 2D > Tile Palette，选择 Lab_64x64。LabGrid64 分为 Background、Collision 和 Decoration 三层。

详细说明见 [Tile Palette 使用说明](Assets/EmergenceLevel01/Tilemaps64/README.md)。角色与交互设备继续使用独立预制体。美术源文件位于 ArtSource。

## Unity MCP

仓库包含嵌入式 `com.coplaydev.unity-mcp` 包及本地连接脚本。MCP 不是运行游戏的必要条件；编辑器自动连接需要本机安装 uvx。配置说明见 [Tools/MCP/README.md](Tools/MCP/README.md)。

Library、Temp、Logs 和个人编辑器设置不进入版本管理，由 Unity 在本机生成。
