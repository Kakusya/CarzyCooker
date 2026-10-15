param([string]$InkscapePath = 'D:/MySoftware/Inkscape/inkscape/bin/inkscape.com')
$ErrorActionPreference = 'Stop'
$artRepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artHudOutput = Join-Path $artRepoRoot 'Unity/Assets/Res/UI/UIForm/Product/Art'
New-Item -ItemType Directory -Path $artHudOutput -Force | Out-Null
$artHudNames = @{ panel = 'Panel'; button = 'Button'; badge = 'StarBadge' }
foreach ($artId in @('panel','button','badge')) {
    $artTarget = Join-Path $artHudOutput ($artHudNames[$artId] + '.png')
    & $InkscapePath (Join-Path $PSScriptRoot 'hud.svg') "--export-id=$artId" --export-id-only --export-width=256 --export-height=256 "--export-filename=$artTarget"
    if ($LASTEXITCODE -ne 0) { throw "Inkscape export failed: $artId" }
}
