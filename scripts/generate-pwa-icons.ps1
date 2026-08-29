Add-Type -AssemblyName System.Drawing

function New-Icon {
    param(
        [int]$Size,
        [string]$Path,
        [bool]$Maskable
    )

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

    $bgColor = [System.Drawing.ColorTranslator]::FromHtml("#111827")
    $accentColor = [System.Drawing.ColorTranslator]::FromHtml("#6366f1")

    $bgBrush = New-Object System.Drawing.SolidBrush($bgColor)

    if ($Maskable) {
        # Maskable icons must fill the full square edge-to-edge -
        # the OS applies its own rounding/mask on top.
        $graphics.FillRectangle($bgBrush, 0, 0, $Size, $Size)
    } else {
        $radius = [int]($Size * 0.22)
        $path2 = New-Object System.Drawing.Drawing2D.GraphicsPath
        $d = $radius * 2
        $path2.AddArc(0, 0, $d, $d, 180, 90)
        $path2.AddArc($Size - $d, 0, $d, $d, 270, 90)
        $path2.AddArc($Size - $d, $Size - $d, $d, $d, 0, 90)
        $path2.AddArc(0, $Size - $d, $d, $d, 90, 90)
        $path2.CloseFigure()
        $graphics.FillPath($bgBrush, $path2)
    }

    # A simple graduation-cap glyph (two triangles + a bar), scaled to size.
    $accentBrush = New-Object System.Drawing.SolidBrush($accentColor)
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

    $cx = $Size / 2
    $cy = $Size * 0.42
    $capW = $Size * 0.62
    $capH = $Size * 0.22

    $capPoints = @(
        (New-Object System.Drawing.PointF(($cx - $capW/2), $cy)),
        (New-Object System.Drawing.PointF($cx, ($cy - $capH))),
        (New-Object System.Drawing.PointF(($cx + $capW/2), $cy)),
        (New-Object System.Drawing.PointF($cx, ($cy + $capH)))
    )
    $graphics.FillPolygon($whiteBrush, $capPoints)

    $baseW = $Size * 0.34
    $baseH = $Size * 0.26
    $graphics.FillRectangle($accentBrush, ($cx - $baseW/2), ($cy + $capH*0.35), $baseW, $baseH)

    $tasselX = $cx + $capW/2 - ($Size * 0.03)
    $tasselY1 = $cy
    $tasselY2 = $cy + $Size * 0.30
    $pen = New-Object System.Drawing.Pen($accentColor, [Math]::Max(1, $Size * 0.02))
    $graphics.DrawLine($pen, $tasselX, $tasselY1, $tasselX, $tasselY2)
    $graphics.FillEllipse($accentBrush, ($tasselX - $Size*0.02), ($tasselY2 - $Size*0.02), $Size*0.04, $Size*0.04)

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)

    $graphics.Dispose()
    $bitmap.Dispose()
}

$outDir = "D:\Desktop\School-Management\wwwroot\icons"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

New-Icon -Size 192 -Path "$outDir\icon-192.png" -Maskable $false
New-Icon -Size 512 -Path "$outDir\icon-512.png" -Maskable $false
New-Icon -Size 192 -Path "$outDir\icon-maskable-192.png" -Maskable $true
New-Icon -Size 512 -Path "$outDir\icon-maskable-512.png" -Maskable $true
New-Icon -Size 180 -Path "$outDir\apple-touch-icon.png" -Maskable $false

Write-Host "Icons generated in $outDir"
