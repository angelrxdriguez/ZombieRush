param(
    [switch]$Headless,
    [switch]$SkipBuild,
    [switch]$InstallMissing
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -LiteralPath $ProjectRoot

$CsprojPath = Join-Path $ProjectRoot "ZombieRush.csproj"

function Find-Dotnet {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $candidates = @(
        (Join-Path $env:ProgramFiles "dotnet\dotnet.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "dotnet\dotnet.exe"),
        (Join-Path $env:USERPROFILE ".dotnet\dotnet.exe")
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

    return $candidates | Select-Object -First 1
}

function Get-FirstMatch {
    param(
        [string[]]$Patterns,
        [string]$Root
    )

    if ($Root -and (Test-Path -LiteralPath $Root)) {
        foreach ($pattern in $Patterns) {
            $foundRecursive = Get-ChildItem -LiteralPath $Root -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($foundRecursive) {
                return $foundRecursive.FullName
            }
        }
    }

    foreach ($pattern in $Patterns) {
        $foundDirect = Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($foundDirect) {
            return $foundDirect.FullName
        }
    }

    return $null
}

function Find-Godot {
    $result = @{
        Gui = $null
        Console = $null
    }

    $godotCmd = Get-Command godot -ErrorAction SilentlyContinue
    if ($godotCmd) {
        $result.Gui = $godotCmd.Source
    }

    $godotConsoleCmd = Get-Command godot_console -ErrorAction SilentlyContinue
    if ($godotConsoleCmd) {
        $result.Console = $godotConsoleCmd.Source
    }

    if (-not $result.Gui -or -not $result.Console) {
        $wingetPackages = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages"
        if (Test-Path -LiteralPath $wingetPackages) {
            $monoRoots = Get-ChildItem -LiteralPath $wingetPackages -Directory -Filter "GodotEngine.GodotEngine.Mono*" -ErrorAction SilentlyContinue
            foreach ($root in $monoRoots) {
                if (-not $result.Gui) {
                    $result.Gui = Get-FirstMatch -Patterns @(
                        "Godot_v*_mono_win64.exe",
                        "Godot*_mono_win64.exe"
                    ) -Root $root.FullName
                }

                if (-not $result.Console) {
                    $result.Console = Get-FirstMatch -Patterns @(
                        "Godot_v*_mono_win64_console.exe",
                        "Godot*_mono_win64_console.exe"
                    ) -Root $root.FullName
                }
            }
        }
    }

    return $result
}

function Ensure-Dependencies {
    param(
        [string]$DotnetPath,
        [hashtable]$GodotPaths
    )

    $missingDotnet = -not $DotnetPath
    $missingGodot = -not $GodotPaths.Gui -and -not $GodotPaths.Console

    if (-not $missingDotnet -and -not $missingGodot) {
        return
    }

    if ($InstallMissing) {
        $winget = Get-Command winget -ErrorAction SilentlyContinue
        if (-not $winget) {
            throw "No se encuentra winget. Instala dotnet 8 SDK y Godot Mono manualmente."
        }

        if ($missingDotnet) {
            Write-Host "Instalando .NET 8 SDK con winget..."
            & $winget.Source install --id Microsoft.DotNet.SDK.8 --exact --source winget
        }

        if ($missingGodot) {
            Write-Host "Instalando Godot Mono con winget..."
            & $winget.Source install --id GodotEngine.GodotEngine.Mono --exact --source winget
        }

        return
    }

    if ($missingDotnet) {
        Write-Host "No se encontro dotnet."
        Write-Host "Instala .NET 8 SDK con:"
        Write-Host "  winget install --id Microsoft.DotNet.SDK.8 --exact --source winget"
    }

    if ($missingGodot) {
        Write-Host "No se encontro Godot Mono."
        Write-Host "Instala Godot Mono con:"
        Write-Host "  winget install --id GodotEngine.GodotEngine.Mono --exact --source winget"
    }

    throw "Dependencias faltantes. Ejecuta .\run.ps1 -InstallMissing o instala manualmente."
}

$dotnetPath = Find-Dotnet
$godotPaths = Find-Godot

Ensure-Dependencies -DotnetPath $dotnetPath -GodotPaths $godotPaths

$dotnetPath = Find-Dotnet
$godotPaths = Find-Godot

if (-not $SkipBuild) {
    Write-Host "Restaurando dependencias..."
    & $dotnetPath restore $CsprojPath

    Write-Host "Compilando proyecto..."
    & $dotnetPath build $CsprojPath
}

if ($Headless) {
    $headlessExe = if ($godotPaths.Console) { $godotPaths.Console } else { $godotPaths.Gui }
    if (-not $headlessExe) {
        throw "No se encontro ejecutable de Godot para validacion headless."
    }

    Write-Host "Ejecutando validacion headless..."
    & $headlessExe --headless --path $ProjectRoot --quit
}
else {
    $gameExe = if ($godotPaths.Gui) { $godotPaths.Gui } else { $godotPaths.Console }
    if (-not $gameExe) {
        throw "No se encontro ejecutable de Godot para arrancar el juego."
    }

    Write-Host "Arrancando ZombieRush..."
    & $gameExe --path $ProjectRoot
}
