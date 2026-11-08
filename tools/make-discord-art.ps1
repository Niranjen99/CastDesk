# Renders CastDesk's artwork for its Discord application (Discord Developer Portal), in the installer's style
# (blue-to-violet gradient, TV-with-play logo; see installer\make-installer-art.ps1):
#   discord-cover.png  512 x 288    cover image: logo and name
#   discord-icon.png   1024 x 1024  app icon and Rich Presence art asset (Discord shows it round or rounded)
# Run from Windows PowerShell:  powershell -ExecutionPolicy Bypass -File tools\make-discord-art.ps1
param([string]$OutDir = (Join-Path $PSScriptRoot '..\docs\discord'))

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path

function New-Gradient {
    $g = New-Object System.Windows.Media.LinearGradientBrush
    $g.StartPoint = '0,0'; $g.EndPoint = '1,1'
    $g.GradientStops.Add((New-Object System.Windows.Media.GradientStop ([System.Windows.Media.Color]::FromRgb(0x2F, 0x6B, 0xFF)), 0))
    $g.GradientStops.Add((New-Object System.Windows.Media.GradientStop ([System.Windows.Media.Color]::FromRgb(0x8A, 0x3F, 0xFC)), 1))
    return $g
}

# The logo's TV with a play button, drawn in white at (x, y) with the given size (designed on a 256 grid).
function Draw-Tv($dc, [double]$x, [double]$y, [double]$size) {
    $dc.PushTransform((New-Object System.Windows.Media.TranslateTransform $x, $y))
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size / 256.0), ($size / 256.0)))
    $white = [System.Windows.Media.Brushes]::White
    $outline = New-Object System.Windows.Media.Pen $white, 16
    $dc.DrawRoundedRectangle($null, $outline, (New-Object System.Windows.Rect 40, 56, 176, 120), 14, 14)
    $dc.DrawRoundedRectangle($white, $null, (New-Object System.Windows.Rect 92, 196, 72, 14), 7, 7)
    $play = New-Object System.Windows.Media.StreamGeometry
    $ctx = $play.Open()
    $ctx.BeginFigure((New-Object System.Windows.Point 110, 88), $true, $true)
    $ctx.LineTo((New-Object System.Windows.Point 160, 116), $true, $false)
    $ctx.LineTo((New-Object System.Windows.Point 110, 144), $true, $false)
    $ctx.Close()
    $dc.DrawGeometry($white, $null, $play)
    $dc.Pop(); $dc.Pop()
}

function Save-Png($visual, [int]$w, [int]$h, [string]$path) {
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $w, $h, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $stream = [IO.File]::Create($path)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}

# Cover, 512 x 288: the logo above the name, centred on the gradient.
$v = New-Object System.Windows.Media.DrawingVisual
$dc = $v.RenderOpen()
$dc.DrawRectangle((New-Gradient), $null, (New-Object System.Windows.Rect 0, 0, 512, 288))
Draw-Tv $dc 181 26 150
$label = New-Object System.Windows.Media.FormattedText 'CastDesk', ([Globalization.CultureInfo]::InvariantCulture), 'LeftToRight', (New-Object System.Windows.Media.Typeface 'Segoe UI Semibold'), 40, ([System.Windows.Media.Brushes]::White), 1.0
$dc.DrawText($label, (New-Object System.Windows.Point ((512 - $label.Width) / 2), 180))
$dc.Close()
Save-Png $v 512 288 (Join-Path $OutDir 'discord-cover.png')

# Icon, 1024 x 1024: the logo on the gradient, inside the circle Discord crops it to.
$v = New-Object System.Windows.Media.DrawingVisual
$dc = $v.RenderOpen()
$dc.DrawRectangle((New-Gradient), $null, (New-Object System.Windows.Rect 0, 0, 1024, 1024))
Draw-Tv $dc 192 176 640
$dc.Close()
Save-Png $v 1024 1024 (Join-Path $OutDir 'discord-icon.png')

"Wrote discord-cover.png and discord-icon.png to $OutDir"
