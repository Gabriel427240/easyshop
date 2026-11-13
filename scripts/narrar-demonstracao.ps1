param([Parameter(Mandatory=$true)][string]$Directory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
$speaker=New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $speaker.SelectVoice('Microsoft Maria Desktop')
    $speaker.Rate=0
    $speaker.Volume=100
    $scenes=Get-Content -LiteralPath (Join-Path $Directory 'narracao.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    for ($i=0; $i -lt $scenes.Count; $i++) {
        $speaker.SetOutputToWaveFile((Join-Path $Directory ('voz-{0:00}.wav' -f $i)))
        $speaker.Speak($scenes[$i].text)
        $speaker.SetOutputToNull()
    }
} finally { $speaker.Dispose() }
