<#
.SYNOPSIS
    Helpers shared by the release scripts.
.DESCRIPTION
    Dot-source it: . (Join-Path $PSScriptRoot "ReleaseCommon.ps1").  Also inside ForEach-Object -Parallel, which doesn't see the
    caller's functions: . $using:common
#>

function Invoke-WithRetry {
    <#
        Runs a command - a native one, judged by its exit code - until it succeeds.  GitHub has the odd 5xx or dropped connection
        on a big upload, and answers 403/429 when its secondary rate limit is hit, which a wait gets past.  Waits DelaySeconds,
        then twice that, and so on.
    #>
    param (
        [Parameter(Mandatory)][string]$What,
        [Parameter(Mandatory)][scriptblock]$Command,
        [int]$Attempts = 5,
        [int]$DelaySeconds = 15
    )
    for ($attempt = 1; ; $attempt++) {
        $global:LASTEXITCODE = 0
        try {
            & $Command 2>&1 | Write-Host
            if ($LASTEXITCODE -eq 0) { return }
            $failure = "exit code $LASTEXITCODE"
        }
        catch {
            $failure = $_.Exception.Message
        }
        if ($attempt -ge $Attempts) { throw "$What failed after $Attempts attempts: $failure" }
        Write-Host "$What failed ($failure) - trying again in $DelaySeconds seconds" -ForegroundColor Yellow
        Start-Sleep -Seconds $DelaySeconds
        $DelaySeconds *= 2
    }
}

function Invoke-Sign {
    <#
        Signs a file in place with the Trimble signing tool and checks the signature.  The tool takes the file, the folder to write
        the signed file to or $ for in place, and the name shown in the signature.  It sends the whole file to the signing
        service, so a big file takes minutes - about 5 seconds a MB - and several can be signed at once.
        Now and then the service reports "Signing completed - OK" and sends the file back unsigned, and the tool exits 0 - so an
        invalid signature is retried too, not just a failed exit code.
    #>
    param (
        [Parameter(Mandatory)][string]$SignTool,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Name
    )
    $file = Split-Path $Path -Leaf
    Invoke-WithRetry -What "Signing $file" -Attempts 3 -DelaySeconds 10 -Command {
        & $SignTool $Path '$' $Name
        if ($LASTEXITCODE -eq 0) {
            $status = (Get-AuthenticodeSignature -LiteralPath $Path).Status
            if ($status -ne "Valid") { throw "the sign tool succeeded, but the signature is $status" }
        }
    }.GetNewClosure()
}
