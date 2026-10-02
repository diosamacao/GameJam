# 64×64 Tile Palette

每格素材为 64×64 像素，Pixels Per Unit = 64，Grid Cell Size = (1,1,1)。Point 过滤、无压缩、无 Mipmap。16 张独立 PNG 位于 Art/Tiles64，16 个 Tile 位于本目录 Tiles。

## 绘制关卡
1. 打开 Generated/Level01_InitialLab.unity。
2. 打开 Window > 2D > Tile Palette，选择 Lab_64x64（全部）、Background_64x64（背景）或 Structure_64x64（结构）。
3. Active Tilemap 选择 LabGrid64 下的 Background、Collision 或 Decoration。
4. 地面、墙、天花板和可站立平台画在 Collision；背景画在 Background；无碰撞装饰画在 Decoration。

Collision 已配置 TilemapCollider2D、静态 Rigidbody2D 与 CompositeCollider2D，绘制结构后自动更新碰撞。基础 Tile 不会自动选择转角，请使用对应转角、平台端点素材。

初始房间宽40格、高14格，Grid位置(0,0.5,0)，地面顶面保持y=1.5。LabGrid64.prefab 可复用；当前场景中的Grid已解包，方便直接绘制。

角色、仓室、终端、数据线和合成门继续使用原有交互预制体及原有尺寸（原素材PPU16），不作为地形Tile。64×64规范适用于本次新增地形素材。旧素材保留以兼容已有预制体引用。

## 检查与备份
Tools > Emergence > Validate 64px Tilemap 检查规格和场景结构。转换前场景与整关预制体备份在工程 Logs/Tilemap64。不要再次使用旧的 Build Level 01 Art Scene 重建当前关卡。

素材由内置 imagegen 生成后按格裁切、最近邻规范至64×64。生成提示词、原始图及导出脚本见工程 ArtSource/Tiles64。
