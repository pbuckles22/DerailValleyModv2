<#
.SYNOPSIS
  Export this repo to Gemini_Snapshot.txt with XML file tags for Gemini 1.5 Pro.

.DESCRIPTION
  Gemini's attention is tuned for XML boundaries. Each source file is wrapped as:
    <file path="YardMasterSuite/Main.cs"><![CDATA[ ... ]]></file>
  Build folders and binaries are skipped. The dump is gitignored.

  Focused (≤10 file) Gemini packs still use docs/gemini/ — this script is the
  full-repo architecture dump.

.EXAMPLE
  .\Export-GeminiSnapshot.ps1
#>
[CmdletBinding()]
param(
    [string]$OutputFileName = "Gemini_Snapshot.txt"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($root)) {
    $root = (Get-Location).Path
}
$root = [System.IO.Path]::GetFullPath($root)
$outputFilePath = Join-Path $root $OutputFileName

$excludeDirNames = @(
    ".git", ".vs", ".idea", ".vscode",
    "bin", "obj", "Library", "Temp", "Logs",
    "Build", "Builds", "build", "dist", "out", "coverage",
    "node_modules", "Packages", "Managed", "DerailValley_Data"
)

$excludeExts = @(
    ".dll", ".exe", ".pdb", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".ico",
    ".mp4", ".wav", ".zip", ".7z", ".rar", ".cache", ".user", ".suo", ".binlog"
)

$excludeFileNames = @(
    $OutputFileName,
    "Directory.Build.targets",
    "build_number.txt"
)

function Test-InExcludedDirectory {
    param([string]$FullName)
    $rel = $FullName.Substring($root.Length).TrimStart("\", "/")
    $parts = $rel.Split([char[]]@("\", "/"))
    if ($parts.Length -lt 2) {
        return $false
    }
    foreach ($part in $parts[0..($parts.Length - 2)]) {
        if ($excludeDirNames -contains $part) {
            return $true
        }
    }
    if ($rel -match '(^|[/\\])docs[/\\]_templates([/\\]|$)') {
        return $true
    }
    return $false
}

function Test-LooksBinary {
    param([string]$Path)
    try {
        $fs = [System.IO.File]::Open($Path, "Open", "Read", "ReadWrite")
        try {
            $buf = New-Object byte[] 512
            $n = $fs.Read($buf, 0, $buf.Length)
            for ($i = 0; $i -lt $n; $i++) {
                if ($buf[$i] -eq 0) {
                    return $true
                }
            }
        }
        finally {
            $fs.Dispose()
        }
    }
    catch {
        return $true
    }
    return $false
}

Write-Host "Scanning repository for Gemini export..." -ForegroundColor Cyan

$allFiles = @(Get-ChildItem -LiteralPath $root -Recurse -File -Force | Where-Object {
    $excludeFileNames -notcontains $_.Name -and
    $_.Name -notmatch '^\d{4}-handoff-' -and
    $_.Name -notmatch 'HANDOFF-' -and
    -not (Test-InExcludedDirectory $_.FullName) -and
    ($excludeExts -notcontains $_.Extension.ToLowerInvariant()) -and
    -not (Test-LooksBinary $_.FullName)
} | Sort-Object FullName)

$utf8 = New-Object System.Text.UTF8Encoding $false
$sb = New-Object System.Text.StringBuilder
$dateStr = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

[void]$sb.AppendLine("<repository_snapshot generated=`"$dateStr`">")
[void]$sb.AppendLine("<directory_structure>")
foreach ($file in $allFiles) {
    $rel = $file.FullName.Substring($root.Length).TrimStart("\", "/").Replace("\", "/")
    [void]$sb.AppendLine($rel)
}
[void]$sb.AppendLine("</directory_structure>")
[void]$sb.AppendLine()
[void]$sb.AppendLine("<files>")

Write-Host "Appending $($allFiles.Count) files..."
foreach ($file in $allFiles) {
    $rel = $file.FullName.Substring($root.Length).TrimStart("\", "/").Replace("\", "/")
    Write-Host " -> $rel" -ForegroundColor DarkGray
    [void]$sb.AppendLine("<file path=`"$rel`">")
    try {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $content = $content.Replace("]]>", "]]]]><![CDATA[>")
        [void]$sb.AppendLine("<![CDATA[")
        [void]$sb.AppendLine($content)
        [void]$sb.AppendLine("]]>")
    }
    catch {
        [void]$sb.AppendLine("<!-- Error reading file: $($_.Exception.Message) -->")
    }
    [void]$sb.AppendLine("</file>")
}

[void]$sb.AppendLine("</files>")
[void]$sb.AppendLine("</repository_snapshot>")

[System.IO.File]::WriteAllText($outputFilePath, $sb.ToString(), $utf8)

Write-Host ""
Write-Host "Done. $($allFiles.Count) files -> $OutputFileName" -ForegroundColor Green
Write-Host "Upload that .txt to Gemini. It is gitignored." -ForegroundColor Yellow
