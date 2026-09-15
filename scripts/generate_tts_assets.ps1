# generate_tts_assets.ps1 - Typecast API로 입/퇴실·실패 안내 음성(mp3) 8개를 한 번만 생성해 둔다.
#
# docs\plan-tts-voice-guidance.md에서 결정한 대로, 매 이벤트마다 API를 호출하지 않고
# 문구별로 한 번씩만 생성해서 client\Assets\Sounds\에 mp3로 저장한다. 이후 실행 시점에는
# Sound.cs가 이 로컬 파일만 재생하므로 API 호출이 전혀 없다.
#
# 사용법(프로젝트 루트에서 실행):
#   $env:TYPECAST_API_KEY = "your-key"     (https://studio.typecast.ai/developers/api 에서 발급)
#   .\scripts\generate_tts_assets.ps1 -VoiceId "tc_xxxxxxxxxxxxxxxxxxxxxxxx"
#
# voice_id(목소리/화자)는 아직 정해지지 않았다(plan 문서의 "미결정 사항" 참고).
# https://studio.typecast.ai 의 보이스 라이브러리에서 원하는 목소리의 ID를 확인해서 넘긴다.
#
# 이 .ps1은 BOM 없이 저장되면 PowerShell 5.1에서 한글 리터럴이 깨질 수 있어(start_demo.ps1과
# 동일한 이유), 실행 중 출력 문자열은 영문으로 통일한다. 한글 문구는 JSON으로 변환되기 전
# 해시테이블 값으로만 쓰고, ConvertTo-Json이 \uXXXX로 이스케이프하므로 인코딩 문제가 없다.

param(
    [Parameter(Mandatory = $true)]
    [string]$VoiceId,

    [string]$Language = "kor",

    [string]$Model = "ssfm-v30"
)

$ErrorActionPreference = "Stop"

$apiKey = $env:TYPECAST_API_KEY
if (-not $apiKey) {
    Write-Host "ERROR: TYPECAST_API_KEY environment variable is not set."
    Write-Host "  1. Get a key at https://studio.typecast.ai/developers/api"
    Write-Host '  2. $env:TYPECAST_API_KEY = "your-key"'
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $scriptDir
$outDir = Join-Path $root "client\Assets\Sounds"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# 사유 코드 -> 녹음 문구. docs\plan-tts-voice-guidance.md의 확정 문구 표와 항상 동일하게 유지할 것.
# 키 이름이 그대로 파일명(<키>.mp3)이 된다.
$phrases = [ordered]@{
    "checkin_voice"    = "입실하셨습니다"
    "checkout_voice"   = "퇴실하셨습니다"
    "spoof_suspected"  = "사진이 감지되었습니다"
    "no_match"         = "등록되지 않은 사용자입니다"
    "connection_error" = "서버에 연결할 수 없습니다"
    "send_failure"     = "전송에 실패했습니다"
    "save_failure"     = "저장에 실패했습니다"
    "comm_failure"     = "통신에 실패했습니다"
}

# Typecast attribution: 코드 작성 에이전트(이 스크립트를 만든 claude-code)를 식별.
$userAgent = "typecast-direct/1 powershell typecast-integration/1 (source=api-page; generated_by=claude-code)"
$headers = @{
    "X-API-KEY"  = $apiKey
    "User-Agent" = $userAgent
}

$done = 0
$total = $phrases.Count

foreach ($key in $phrases.Keys) {
    $text = $phrases[$key]
    $outPath = Join-Path $outDir "$key.mp3"

    Write-Host "[$($done + 1)/$total] Generating $key.mp3 ..."

    $body = @{
        model    = $Model
        voice_id = $VoiceId
        text     = $text
        language = $Language
        output   = @{ audio_format = "mp3" }
    } | ConvertTo-Json

    try {
        Invoke-WebRequest -Uri "https://api.typecast.ai/v1/text-to-speech" `
            -Method Post `
            -Headers $headers `
            -ContentType "application/json; charset=utf-8" `
            -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) `
            -OutFile $outPath | Out-Null
    }
    catch {
        Write-Host "  FAILED: $($_.Exception.Message)"
        continue
    }

    Write-Host "  -> saved to $outPath"
    $done++
}

Write-Host ""
Write-Host "Done. $done/$total files generated in $outDir"
if ($done -lt $total) {
    Write-Host "Some files failed - check the errors above and re-run (existing files will be overwritten)."
}
