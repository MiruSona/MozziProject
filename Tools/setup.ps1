# 툴 서브모듈의 실행 파일(exe·.venv)을 한 줄로 다시 만든다.
# 인자 없으면 빌드, -Check 면 낡았는지만 본다 (아무것도 안 만들고 언제나 종료 0).
[CmdletBinding()]
param(
    [switch]$Check
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# CodeTool 의 install.ps1 은 템플릿 복사용이라 여기서 부르면 안 된다.
$skipNames = @('CodeTool')

# AIMemoryTool 은 이 폴더가 아니라 ~/.aimemory/bin 에 깔려 PATH 로 쓴다.
# 그래서 여기서 빌드하지 않고, 아래 「전역 mem」 줄에서 한 번만 본다.
$globalNames = @('AIMemoryTool')

# 소스가 exe 보다 새것인지는 파일 수정 시각으로 본다.
# git checkout 은 바뀐 파일만 새 시각으로 쓰므로, 서브모듈 갱신이 곧 「소스가 더 새것」 이 되고 문서만 고친 커밋에는 안 울린다.
# 훑는 자리는 bin·.git·Docs·testdata 를 뺀 툴 폴더 전체의 `*.go` 와 go.mod·go.sum 이다.
# 툴 뿌리 바로 아래 ui·Unity·templates 는 exe 에 통째로 박히는 embed 자산이라 안의 파일 전부를 본다.
function Get-NewestSourceTime {
    param([string]$ToolDir)

    $skipDirs = @('bin', '.git', 'Docs', 'testdata')
    $embedDirs = @('ui', 'Unity', 'templates')

    $files = @()
    foreach ($top in (Get-ChildItem -LiteralPath $ToolDir -Force -ErrorAction SilentlyContinue)) {
        if ($top.PSIsContainer) {
            if ($skipDirs -contains $top.Name) { continue }
            if ($embedDirs -contains $top.Name) {
                $files += Get-ChildItem -LiteralPath $top.FullName -Recurse -File -ErrorAction SilentlyContinue
            }
            else {
                $files += Get-ChildItem -LiteralPath $top.FullName -Recurse -File -Filter '*.go' -ErrorAction SilentlyContinue
            }
        }
        elseif (($top.Extension -eq '.go') -or ($top.Name -eq 'go.mod') -or ($top.Name -eq 'go.sum')) {
            $files += $top
        }
    }

    $newest = $null
    foreach ($file in $files) {
        if (($null -eq $newest) -or ($file.LastWriteTime -gt $newest)) { $newest = $file.LastWriteTime }
    }
    return $newest
}

# 파이썬 툴은 pyproject.toml 만이 아니라 src 아래 *.py 도 소스다.
function Get-NewestPySourceTime {
    param([string]$ToolDir)

    $newest = $null
    $pyproject = Join-Path $ToolDir 'pyproject.toml'
    if (Test-Path -LiteralPath $pyproject) { $newest = (Get-Item -LiteralPath $pyproject).LastWriteTime }

    $src = Join-Path $ToolDir 'src'
    if (Test-Path -LiteralPath $src) {
        $found = Get-ChildItem -LiteralPath $src -Recurse -File -Filter '*.py' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($found -and (($null -eq $newest) -or ($found.LastWriteTime -gt $newest))) { $newest = $found.LastWriteTime }
    }
    return $newest
}

# .venv 를 언제 만들었나. setup.ps1 이 남긴 .setup-stamp 가 정답이고, 없으면 site-packages 폴더 시각으로 갈음한다.
# .venv 폴더 자체의 시각은 pip 재설치로 안 바뀌어서 쓰면 영원히 「낡음」 이 된다.
function Get-VenvTime {
    param([string]$VenvDir)

    $stamp = Join-Path $VenvDir '.setup-stamp'
    if (Test-Path -LiteralPath $stamp) { return (Get-Item -LiteralPath $stamp).LastWriteTime }

    $sitePackages = Join-Path $VenvDir 'Lib\site-packages'
    if (Test-Path -LiteralPath $sitePackages) { return (Get-Item -LiteralPath $sitePackages).LastWriteTime }

    return $null
}

function Get-NewestExeFile {
    param([string]$ToolDir)

    $bin = Join-Path $ToolDir 'bin'
    if (-not (Test-Path -LiteralPath $bin)) { return $null }
    return Get-ChildItem -LiteralPath $bin -Filter '*.exe' -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

# exe 가 스스로 찍는 커밋을 읽는다. `(abc1234 ·` 또는 `커밋 abc1234` 꼴이다.
# 안 찍는 옛 exe 는 빈 값이 나오고, 부르는 쪽이 시각 비교로 떨어진다.
function Get-ExeCommit {
    param([string]$ExePath)

    foreach ($arg in @('--version', 'version')) {
        $out = ''
        try { $out = (& $ExePath $arg 2>&1 | Out-String) } catch { $out = '' }
        $global:LASTEXITCODE = 0
        if ($out -match '\(([0-9a-f]{7,40})[^)]* ·') { return $Matches[1] }
        if ($out -match '커밋 ([0-9a-f]{7,40})') { return $Matches[1] }
    }
    return ''
}

# 서브모듈이 지금 가리키는 커밋. 빌드가 박는 값과 같은 방식으로 뽑는다.
function Get-ToolCommit {
    param([string]$ToolDir)

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { return '' }
    $out = ''
    try { $out = (& git -C $ToolDir log -1 --format=%h -- cmd internal go.mod 2>&1 | Out-String) } catch { $out = '' }
    if ($LASTEXITCODE -ne 0) { $out = '' }
    $global:LASTEXITCODE = 0
    $out = $out.Trim()
    if ($out -notmatch '^[0-9a-f]{7,40}$') { return '' }
    return $out
}

# 낡았으면 까닭 한 줄, 최신이면 빈 문자열.
# exe 가 커밋을 찍으면 그 비교가 가장 정확하다 — 시각 비교보다 먼저 본다.
function Get-GoExeStaleReason {
    param([string]$ToolDir, $ExeFile)

    $exeCommit = Get-ExeCommit $ExeFile.FullName
    if ($exeCommit -ne '') {
        $gitCommit = Get-ToolCommit $ToolDir
        if ($gitCommit -ne '') {
            $len = [Math]::Min($exeCommit.Length, $gitCommit.Length)
            if ($exeCommit.Substring(0, $len) -ne $gitCommit.Substring(0, $len)) {
                return ("낡음 — exe 커밋 {0} / 서브모듈 {1}" -f $exeCommit, $gitCommit)
            }
            return ''
        }
    }

    $srcTime = Get-NewestSourceTime $ToolDir
    if ($srcTime -and ($srcTime -gt $ExeFile.LastWriteTime)) {
        return ("낡음 — exe {0} / 소스 {1}" -f $ExeFile.LastWriteTime.ToString('MM-dd HH:mm'), $srcTime.ToString('MM-dd HH:mm'))
    }
    return ''
}

function New-Row {
    param([string]$Name, [string]$Did, [string]$Result, [string]$Seconds)

    return [PSCustomObject]@{
        '툴'   = $Name
        '한 일' = $Did
        '결과'  = $Result
        '걸린초' = $Seconds
    }
}

$toolDirs = Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue | Sort-Object Name

# ── 낡았는지만 보는 모드 ──
if ($Check) {
    $rows = @()
    $needSetup = 0      # setup.ps1 / build.ps1 을 돌리면 고쳐지는 것
    $needSubmodule = 0  # 빌드 스크립트가 없다 — 서브모듈 갱신이 먼저인 것
    $needInstall = 0    # 사용자가 직접 -Install 해야 하는 전역 툴

    foreach ($dir in $toolDirs) {
        $name = $dir.Name
        if ($skipNames -contains $name) { continue }
        if ($globalNames -contains $name) { continue }

        $toolDir = $dir.FullName
        $hasGo = Test-Path -LiteralPath (Join-Path $toolDir 'go.mod')
        $pyproject = Join-Path $toolDir 'pyproject.toml'
        $hasPy = Test-Path -LiteralPath $pyproject
        if (-not ($hasGo -or $hasPy)) { continue }

        $hasScript = (Test-Path -LiteralPath (Join-Path $toolDir 'setup.ps1')) -or
                     (Test-Path -LiteralPath (Join-Path $toolDir 'build.ps1'))

        # (한 일, 결과) 짝을 모아 두고 한 번에 표로 만든다.
        $found = @()

        if ($hasGo) {
            $exeFile = Get-NewestExeFile $toolDir
            if ($null -eq $exeFile) {
                $found += , @('Go exe', '없음 — 빌드해야 한다')
            }
            else {
                $reason = Get-GoExeStaleReason $toolDir $exeFile
                if ($reason -ne '') { $found += , @('Go exe', $reason) }
            }
        }

        if ($hasPy) {
            $venv = Join-Path $toolDir '.venv'
            if (-not (Test-Path -LiteralPath $venv)) {
                $found += , @('.venv', '없음 — 만들어야 한다')
            }
            else {
                $venvTime = Get-VenvTime $venv
                $srcTime = Get-NewestPySourceTime $toolDir
                if ($null -eq $venvTime) {
                    $found += , @('.venv', '언제 만든 것인지 알 수 없다 — 다시 만들어라')
                }
                elseif ($srcTime -and ($srcTime -gt $venvTime)) {
                    $found += , @('.venv', ("낡음 — .venv {0} / 소스 {1}" -f $venvTime.ToString('MM-dd HH:mm'), $srcTime.ToString('MM-dd HH:mm')))
                }
            }
        }

        foreach ($item in $found) {
            if ($hasScript) {
                $needSetup = $needSetup + 1
                $rows += New-Row $name $item[0] $item[1] ''
            }
            else {
                $needSubmodule = $needSubmodule + 1
                $rows += New-Row $name $item[0] ($item[1] + ' · 빌드 스크립트가 없다 — 서브모듈 갱신이 먼저') ''
            }
        }
    }

    # PATH 에 깔린 전역 mem 은 여기서 못 고친다 — 사용자가 직접 -Install 을 돌려야 한다.
    $memDir = Join-Path $root 'AIMemoryTool'
    $memCmd = Get-Command 'mem' -ErrorAction SilentlyContinue
    if ($memCmd -and $memCmd.Source -and (Test-Path -LiteralPath $memDir)) {
        $memExe = Get-Item -LiteralPath $memCmd.Source
        $reason = Get-GoExeStaleReason $memDir $memExe
        if ($reason -ne '') {
            $needInstall = $needInstall + 1
            $rows += New-Row 'mem (PATH 전역)' 'exe' $reason ''
        }
    }

    if ($rows.Count -eq 0) {
        Write-Host '툴 실행 파일 : 모두 최신'
    }
    else {
        Write-Host '툴 실행 파일 : 낡은 것이 있다'
        $rows | Select-Object '툴', '한 일', '결과' | Format-Table -AutoSize | Out-String | Write-Host
        if ($needSetup -gt 0) {
            Write-Host '다시 만들면 되는 것 : powershell -NoProfile -ExecutionPolicy Bypass -File Tools\setup.ps1'
        }
        if ($needSubmodule -gt 0) {
            Write-Host '빌드 스크립트가 없는 것 : 서브모듈이 옛 커밋이다 — 먼저 서브모듈을 갱신하고 나서 위 setup 을 돌린다'
        }
        if ($needInstall -gt 0) {
            Write-Host '전역 mem : 사용자가 직접 powershell -File Tools\AIMemoryTool\build.ps1 -Install 을 돌린다'
        }
    }
    exit 0
}

# ── 다시 만드는 모드 ──
$rows = @()
$failed = 0
foreach ($dir in $toolDirs) {
    $name = $dir.Name
    $toolDir = $dir.FullName

    if ($skipNames -contains $name) {
        $rows += New-Row $name '건너뜀 (템플릿 복사용)' '—' '—'
        continue
    }

    # 전역 설치 툴은 설치 자리가 이 폴더가 아니라 사용자 홈이다. 실패로 세지 않는다.
    if ($globalNames -contains $name) {
        Write-Host ("[{0}] 전역 설치 툴 — 사용자가 Tools\{0}\build.ps1 -Install 을 돌린다. 건너뛴다." -f $name)
        $rows += New-Row $name '건너뜀 (전역 설치 툴)' '사용자가 -Install' '—'
        continue
    }

    $script = $null
    $did = ''
    $setup = Join-Path $toolDir 'setup.ps1'
    $build = Join-Path $toolDir 'build.ps1'
    if (Test-Path -LiteralPath $setup) {
        $script = $setup
        $did = 'setup.ps1'
    }
    elseif (Test-Path -LiteralPath $build) {
        $script = $build
        $did = 'build.ps1'
    }

    if ($null -eq $script) {
        $hasGo = Test-Path -LiteralPath (Join-Path $toolDir 'go.mod')
        $hasPy = Test-Path -LiteralPath (Join-Path $toolDir 'pyproject.toml')
        if ($hasGo -or $hasPy) {
            # 빌드할 소스는 있는데 빌드 스크립트가 없다 = 서브모듈이 스크립트 이전 커밋이다.
            $rows += New-Row $name '—' '실패 — 서브모듈이 옛 커밋이다, 갱신하라' '—'
            $failed = $failed + 1
        }
        else {
            $rows += New-Row $name '—' '빌드 없음' '—'
        }
        continue
    }

    Write-Host ("[{0}] {1} 부르는 중 ..." -f $name, $did)
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$script"
    $code = $LASTEXITCODE
    $watch.Stop()
    $seconds = ('{0:N1}' -f $watch.Elapsed.TotalSeconds)

    if ($code -eq 0) {
        $rows += New-Row $name $did '성공' $seconds
    }
    else {
        $rows += New-Row $name $did ("실패 (종료 {0})" -f $code) $seconds
        $failed = $failed + 1
    }
}

Write-Host ''
$rows | Format-Table -AutoSize | Out-String | Write-Host

if ($failed -gt 0) {
    Write-Host ("실패 {0} 개 — 위 표를 보라." -f $failed)
    exit 1
}
Write-Host '모두 끝났다.'
exit 0
