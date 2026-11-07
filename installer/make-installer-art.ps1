# Renders the installer artwork in the CastDesk style (blue-to-violet gradient, TV-with-play logo):
#   dialog.bmp  493 x 312  welcome/finish screens (the left 164 px show beside the text)
#   banner.bmp  493 x 58   top banner of the progress screen
# Run from Windows PowerShell:  powershell -ExecutionPolicy Bypass -File installer\make-installer-art.ps1
param([string]$OutDir = $PSScriptRoot)

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

function New-Gradient {
    $g = New-Object System.Windows.Media.LinearGradientBrush
    $g.StartPoint = '0,0'; $g.EndPoint = '1,1'
    $g.GradientStops.Add((New-Object System.Windows.Media.GradientStop ([System.Windows.Media.Color]::FromRgb(0x2F, 0x6B, 0xFF)), 0))
    $g.GradientStops.Add((New-Object System.Windows.Media.GradientStop ([System.Windows.Media.Color]::FromRgb(0x8A, 0x3F, 0xFC)), 1))
    return $g
}

# The logo's TV with a play button, drawn in white at (x, y) with the given size (designed on a 256 grid).
function Draw-Tv($dc, [double]$x, [double]$y, [double]$size, $background) {
    $dc.PushTransform((New-Object System.Windows.Media.TranslateTransform $x, $y))
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size / 256.0), ($size / 256.0)))
    $white = [System.Windows.Media.Brushes]::White
    $outline = New-Object System.Windows.Media.Pen $white, 16
    # Screen outline and stand.
    $dc.DrawRoundedRectangle($null, $outline, (New-Object System.Windows.Rect 40, 56, 176, 120), 14, 14)
    $dc.DrawRoundedRectangle($white, $null, (New-Object System.Windows.Rect 92, 196, 72, 14), 7, 7)
    # Play triangle.
    $play = New-Object System.Windows.Media.StreamGeometry
    $ctx = $play.Open()
    $ctx.BeginFigure((New-Object System.Windows.Point 110, 88), $true, $true)
    $ctx.LineTo((New-Object System.Windows.Point 160, 116), $true, $false)
    $ctx.LineTo((New-Object System.Windows.Point 110, 144), $true, $false)
    $ctx.Close()
    $dc.DrawGeometry($white, $null, $play)
    $dc.Pop(); $dc.Pop()
}

function Save-Bmp($visual, [int]$w, [int]$h, [string]$path) {
    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $w, $h, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($visual)
    # MSI dialogs need a plain 24-bit BMP.
    $rgb = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap $rtb, ([System.Windows.Media.PixelFormats]::Bgr24), $null, 0
    $encoder = New-Object System.Windows.Media.Imaging.BmpBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rgb))
    $stream = [IO.File]::Create($path)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}

# Dialog: gradient panel on the left with the logo, white where the dialog text goes.
$v = New-Object System.Windows.Media.DrawingVisual
$dc = $v.RenderOpen()
$dc.DrawRectangle([System.Windows.Media.Brushes]::White, $null, (New-Object System.Windows.Rect 0, 0, 493, 312))
$dc.DrawRectangle((New-Gradient), $null, (New-Object System.Windows.Rect 0, 0, 164, 312))
Draw-Tv $dc 22 66 120
$label = New-Object System.Windows.Media.FormattedText 'CastDesk', ([Globalization.CultureInfo]::InvariantCulture), 'LeftToRight', (New-Object System.Windows.Media.Typeface 'Segoe UI Semibold'), 20, ([System.Windows.Media.Brushes]::White), 1.0
$dc.DrawText($label, (New-Object System.Windows.Point ((164 - $label.Width) / 2), 200))
$dc.Close()
Save-Bmp $v 493 312 (Join-Path $OutDir 'dialog.bmp')

# Banner: white with a small logo tile on the right (the title text sits on the left).
$v = New-Object System.Windows.Media.DrawingVisual
$dc = $v.RenderOpen()
$dc.DrawRectangle([System.Windows.Media.Brushes]::White, $null, (New-Object System.Windows.Rect 0, 0, 493, 58))
$dc.DrawRoundedRectangle((New-Gradient), $null, (New-Object System.Windows.Rect 442, 9, 40, 40), 9, 9)
Draw-Tv $dc 442 9 40
$dc.Close()
Save-Bmp $v 493 58 (Join-Path $OutDir 'banner.bmp')

"Wrote dialog.bmp and banner.bmp to $OutDir"
