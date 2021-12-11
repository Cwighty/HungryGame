param(
    [Parameter(HelpMessage = "host and port of server")]    
    [string]$url = "http://localhost:5291",
    [Parameter(HelpMessage = "your player name")]    
    [string]$name = [DateTime]::Now.Ticks % 10000
)

function randomDirection {
    $directions = "up", "down", "right", "left"
    return $directions[(get-random -max 4)]
}

function get-gameState {
    $r = call "$url/state"
    return $r
}

$client = [System.Net.Http.HttpClient]::new()

function call($path) {
    $clientResult = $client.GetStringAsync($path).
        GetAwaiter().
        GetResult()
    return $clientResult
}

$token = call "$url/join?userName=$name"
write-host "$name joined game w/token $token"

$gameState = get-gameState
while ( $gameState -eq "Joining") {
    write-verbose "Game state is $gameState...sleeping"
    Start-Sleep -Seconds 2
    $gameState = get-gameState
}

$direction = randomDirection;
$reqCount = 0;

while ($true) {
    $reqCount++;
    try {
        $moveResult = (call "$url/move/$direction/?token=$token") | ConvertFrom-Json
        if ($null -eq $moveResult -or $moveResult.ateAPill -eq $false) {
            $newDirection = randomDirection
            Write-verbose "$name didn't eat a token going $direction, so I'll try going $newDirection"
            $direction = $newDirection;
        }    
    }
    catch {
        write-error "$name got an exception.  I'm out.`n Exception details: `n$_"        
        break;
    }

    if ($reqCount -gt 100) {
        if ((get-gameState) -eq "GameOver") {
            Write-Host "Game over. Player $name ends."
            break;
        }
    }
}