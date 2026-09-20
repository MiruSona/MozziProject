# c500.png 의 얼굴(눈 x25-26·39-40 y29-34, 입 x30-35 y36)만 바꿔 표정 변형을 찍는다. 몸·볼·왕관은 그대로다.
Add-Type -AssemblyName System.Drawing
$d = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $d 'faces'
New-Item -ItemType Directory -Force $out | Out-Null

$White = [System.Drawing.Color]::FromArgb(255,0xFE,0xFD,0xFC)
$Ink   = [System.Drawing.Color]::FromArgb(255,0x12,0x11,0x0F)
$Pink  = [System.Drawing.Color]::FromArgb(255,0xF2,0x8C,0x8C)
$Red   = [System.Drawing.Color]::FromArgb(255,0xE8,0x43,0x3F)
$Blue  = [System.Drawing.Color]::FromArgb(255,0x5B,0xC0,0xEB)
$Gray  = [System.Drawing.Color]::FromArgb(255,0x3A,0x3A,0x44)

function Px($bmp, $pts, $c) { foreach ($p in $pts) { $bmp.SetPixel($p[0]-1, $p[1], $c) } }
function Rect($bmp, $x0, $y0, $x1, $y1, $c) { for ($y=$y0; $y -le $y1; $y++) { for ($x=$x0; $x -le $x1; $x++) { $bmp.SetPixel($x-1,$y,$c) } } }
# 무늬 문자열(줄 배열)을 (ox,oy) 에 찍는다. X=색, 그 밖=건너뜀
function Stamp($bmp, $ox, $oy, $rows, $c) {
  for ($j=0; $j -lt $rows.Count; $j++) { $r=$rows[$j]; for ($i=0; $i -lt $r.Length; $i++) { if ($r[$i] -eq 'X') { $bmp.SetPixel($ox+$i-1, $oy+$j, $c) } } }
}
function ClearFace($bmp, [bool]$eyeL, [bool]$eyeR, [bool]$mouth) {
  if ($eyeL)  { Rect $bmp 25 29 26 34 $White }
  if ($eyeR)  { Rect $bmp 39 29 40 34 $White }
  if ($mouth) { Rect $bmp 30 36 35 36 $White }
}
function MirrorRows($rows) { $rows | ForEach-Object { $a=$_.ToCharArray(); [array]::Reverse($a); -join $a } }

$Caret   = @('..X..', '.XXX.', 'XX.XX', 'X...X')          # ^ 웃는 눈
$Vee     = @('X...X', 'XX.XX', '.XXX.', '..X..')          # v 깔깔 눈
$Gt      = @('XX...', '.XX..', '..XX.', '.XX..', 'XX...') # > 꽉 감은 눈
$Ring    = @('.XX.', 'X..X', 'X..X', 'X..X', '.XX.')      # o 놀란 눈
$Heart   = @('.X.X.', 'XXXXX', 'XXXXX', '.XXX.', '..X..')
$Dash    = @('XXXXX', 'XXXXX')                            # - 졸린 눈
$BrowL   = @('XX....', 'XXXX..', '..XXXX', '....XX')      # 화난 눈썹(왼쪽, 안쪽으로 내려감)
$SadL    = @('....XX', '..XXXX', 'XXXX..', 'XX....')      # 처진 눈(왼쪽, 바깥으로 내려감)
$Smile   = @('X....X', '.XXXX.')
$Frown   = @('.XXXX.', 'X....X')
$OpenIn  = @('......', '.XXXX.', '..XX..')                # 벌린 입 속(분홍)
$OpenOut = @('XXXXXX', 'X....X', '.X..X.', '..XX..')      # 벌린 입 테두리
$OhOut   = @('.XX.', 'X..X', 'X..X', '.XX.')
$OhIn    = @('....', '.XX.', '.XX.', '....')
$Zed     = @('XXXX', '..X.', '.X..', 'XXXX')
$ZedS    = @('XXX', '.X.', 'XXX')
$Vein    = @('.X.X.', 'XX.XX', '.....', 'XX.XX', '.X.X.')

$faces = [ordered]@{
  '01_기본'   = { param($b) }
  '02_웃음'   = { param($b) ClearFace $b $true $true $true; Stamp $b 23 30 $Caret $Ink; Stamp $b 38 30 $Caret $Ink; Stamp $b 30 36 $OpenOut $Ink; Stamp $b 30 36 $OpenIn $Pink }
  '03_좋아'   = { param($b) ClearFace $b $true $true $true; Stamp $b 23 29 $Gt $Ink; Stamp $b 38 29 (MirrorRows $Gt) $Ink; Stamp $b 30 36 $OpenOut $Ink; Stamp $b 30 36 $OpenIn $Pink; Stamp $b 12 13 $Heart $Red }
  '04_졸림'   = { param($b) ClearFace $b $true $true $true; Stamp $b 23 32 $Dash $Ink; Stamp $b 38 32 $Dash $Ink; Rect $b 32 36 33 36 $Ink; Stamp $b 9 12 $Zed $Gray; Stamp $b 15 8 $ZedS $Gray }
  '05_놀람'   = { param($b) ClearFace $b $true $true $true; Stamp $b 24 29 $Ring $Ink; Stamp $b 38 29 $Ring $Ink; Stamp $b 31 36 $OhOut $Ink; Stamp $b 31 36 $OhIn $Pink }
  '06_화남'   = { param($b) ClearFace $b $true $true $true; Stamp $b 22 27 $BrowL $Ink; Stamp $b 38 27 (MirrorRows $BrowL) $Ink; Rect $b 25 32 26 34 $Ink; Rect $b 39 32 40 34 $Ink; Stamp $b 30 37 $Frown $Ink; Stamp $b 19 21 $Vein $Red }
  '07_하트눈' = { param($b) ClearFace $b $true $true $true; Stamp $b 23 29 $Heart $Red; Stamp $b 38 29 $Heart $Red; Stamp $b 30 36 $Smile $Ink }
  '08_히잉'   = { param($b) ClearFace $b $true $true $true; Stamp $b 22 30 $SadL $Ink; Stamp $b 38 30 (MirrorRows $SadL) $Ink; Stamp $b 30 37 $Frown $Ink; Rect $b 22 35 23 38 $Blue; Rect $b 42 35 43 38 $Blue }
  '09_윙크'   = { param($b) ClearFace $b $false $true $true; Stamp $b 38 30 $Caret $Ink; Stamp $b 30 36 $Smile $Ink }
  '10_깔깔'   = { param($b) ClearFace $b $true $true $true; Stamp $b 23 30 $Vee $Ink; Stamp $b 38 30 $Vee $Ink; Stamp $b 30 36 $OpenOut $Ink; Stamp $b 30 36 $OpenIn $Pink }
}

# 낱장 저장
$names = @($faces.Keys)
foreach ($n in $names) {
  $src = New-Object System.Drawing.Bitmap (Join-Path $d 'c500.png')
  $b = New-Object System.Drawing.Bitmap $src   # 32bpp ARGB 복사본
  $src.Dispose()
  & $faces[$n] $b
  $b.Save((Join-Path $out "$n.png"), [System.Drawing.Imaging.ImageFormat]::Png)
  $b.Dispose()
}

# 이름표 붙인 비교판 (5열 x 2행, 5배 확대, 농장 바닥색)
$scale = 5; $cell = 64 * $scale; $pad = 10; $label = 34; $cols = 5
$rowsN = [math]::Ceiling($names.Count / $cols)
$W = $cols * ($cell + $pad) + $pad; $H = $rowsN * ($cell + $label + $pad) + $pad
$sheet = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(92,140,77))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$font = New-Object System.Drawing.Font('Malgun Gothic', 16, [System.Drawing.FontStyle]::Bold)
$fmt = New-Object System.Drawing.StringFormat; $fmt.Alignment = 'Center'
for ($i=0; $i -lt $names.Count; $i++) {
  $cx = $pad + ($i % $cols) * ($cell + $pad); $cy = $pad + [math]::Floor($i / $cols) * ($cell + $label + $pad)
  $im = New-Object System.Drawing.Bitmap (Join-Path $out "$($names[$i]).png")
  $g.DrawImage($im, $cx, $cy, $cell, $cell); $im.Dispose()
  $rect = New-Object System.Drawing.RectangleF $cx, ($cy + $cell), $cell, $label
  $g.DrawString($names[$i].Replace('_',' '), $font, [System.Drawing.Brushes]::White, $rect, $fmt)
}
# 아래 줄 : 실제 크기(1배)로 나란히 — 게임에서 보일 크기 감
$sheet.Save((Join-Path $d 'faces_sheet.png')); $g.Dispose(); $sheet.Dispose()
"saved $($names.Count) faces"
