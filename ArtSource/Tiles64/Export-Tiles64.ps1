$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Join-Path $PSScriptRoot 'Output'
New-Item -ItemType Directory -Force -Path $root | Out-Null
$source=[System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'tiles64-source.png'))
$names=@('bg_plain','bg_bolts','bg_vent','bg_light','solid_fill','floor_top','ceiling_bottom','wall_left','wall_right','corner_top_left','corner_top_right','corner_bottom_left','corner_bottom_right','platform_left','platform_middle','platform_right')
$xs=@(@(10,304),@(321,619),@(636,932),@(950,1244));$ys=@(@(10,302),@(321,618),@(633,931),@(947,1244))
$atlas=[System.Drawing.Bitmap]::new(256,256);$ag=[System.Drawing.Graphics]::FromImage($atlas)
$manifest=@()
for($i=0;$i -lt 16;$i++){
 $c=$i%4;$r=[int][Math]::Floor($i/4);$x=$xs[$c][0];$y=$ys[$r][0];$w=$xs[$c][1]-$x;$h=$ys[$r][1]-$y
 $tile=[System.Drawing.Bitmap]::new(64,64);$g=[System.Drawing.Graphics]::FromImage($tile)
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
 $g.DrawImage($source,[System.Drawing.Rectangle]::new(0,0,64,64),$x,$y,$w,$h,[System.Drawing.GraphicsUnit]::Pixel);$g.Dispose()
 $tile.Save((Join-Path $root ($names[$i]+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $ag.DrawImageUnscaled($tile,$c*64,$r*64)
 $manifest+=@{name=$names[$i];width=64;height=64;ppu=64;collider=$(if($i-lt 4){'None'}else{'Grid'})}
 $tile.Dispose()
}
$ag.Dispose();$atlas.Save((Join-Path $PSScriptRoot 'tiles64-atlas.png'));$atlas.Dispose();$source.Dispose()
$manifest|ConvertTo-Json|Set-Content (Join-Path $PSScriptRoot 'tiles64-manifest.json')
