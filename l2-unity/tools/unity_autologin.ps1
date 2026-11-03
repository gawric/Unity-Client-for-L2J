param(
    [string]$Account = "2222",
    [string]$Password = "2222",
    [int]$ServerId = 1,
    [int]$Slot = 2,
    [int]$TimeoutSec = 180,
    [string]$Chat = "",
    [int]$ChatDelaySec = 20
)

$project = Split-Path $PSScriptRoot -Parent
$temp = Join-Path $project "Temp"
New-Item -ItemType Directory -Force -Path $temp | Out-Null

$world = Join-Path $temp "autologin.world"
$play = Join-Path $temp "autologin.play"
$request = Join-Path $temp "autologin.request"
$chatFile = Join-Path $temp "autologin.chat"

if (Test-Path -LiteralPath $world) { Remove-Item -LiteralPath $world -Force }
if (Test-Path -LiteralPath $play) { Remove-Item -LiteralPath $play -Force }
if (Test-Path -LiteralPath $chatFile) { Remove-Item -LiteralPath $chatFile -Force }

$lines = @(
    "account=$Account"
    "password=$Password"
    "serverId=$ServerId"
    "slot=$Slot"
)
if ($Chat) {
    $lines += "chat=$Chat"
    $lines += "chatDelay=$ChatDelaySec"
}
$lines | Set-Content -LiteralPath $request -Encoding ascii

Set-Content -LiteralPath $play -Value "play" -Encoding ascii
Write-Output "[autologin] waiting for Unity Editor Play and world enter (max ${TimeoutSec}s)"
Write-Output "[autologin] request $request"

$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $world) {
        $text = (Get-Content -LiteralPath $world -Raw).Trim()
        Write-Output "[autologin] WORLD ENTER $text"
        if ($text -ne "OK") { exit 1 }
        if (-not $Chat) { exit 0 }
        $chatDeadline = (Get-Date).AddSeconds($ChatDelaySec + 30)
        while ((Get-Date) -lt $chatDeadline) {
            if (Test-Path -LiteralPath $chatFile) {
                $chatText = (Get-Content -LiteralPath $chatFile -Raw).Trim()
                Write-Output "[autologin] CHAT $chatText"
                if ($chatText -eq "OK") { exit 0 }
                exit 1
            }
            Start-Sleep -Milliseconds 500
        }
        Write-Output "[autologin] chat timeout"
        exit 1
    }
    Start-Sleep -Milliseconds 500
}

Write-Output "[autologin] world timeout. Unity Editor must be open on this project and finished compiling."
exit 1
