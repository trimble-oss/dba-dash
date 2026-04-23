# Test API Key Authentication for DBADash AI Service
# Usage: .\test-apikey-auth.ps1 -ServiceUrl "http://lab2022:5055" -ApiKey "your-key-here"

param(
	[Parameter(Mandatory=$true)]
	[string]$ServiceUrl,

	[Parameter(Mandatory=$false)]
	[string]$ApiKey
)

$ServiceUrl = $ServiceUrl.TrimEnd('/')
$passed = 0
$failed = 0

function Test-Endpoint {
	param([string]$Name, [string]$Url, [string]$Method = "GET", [string]$Body, [string]$Key, [int]$ExpectedStatus)

	$headers = @{}
	if ($Key) { $headers["X-API-Key"] = $Key }
	if ($Body) { $headers["Content-Type"] = "application/json" }

	try {
		$params = @{ Uri = $Url; Method = $Method; Headers = $headers; UseBasicParsing = $true }
		if ($Body) { $params.Body = $Body }
		$response = Invoke-WebRequest @params -ErrorAction Stop
		$status = $response.StatusCode
	} catch {
		$status = [int]$_.Exception.Response.StatusCode
	}

	$result = if ($status -eq $ExpectedStatus) { "PASS"; $script:passed++ } else { "FAIL"; $script:failed++ }
	$icon = if ($result -eq "PASS") { "[+]" } else { "[-]" }
	Write-Host "$icon $Name - Expected: $ExpectedStatus, Got: $status" -ForegroundColor $(if ($result -eq "PASS") { "Green" } else { "Red" })
}

Write-Host "`n=== DBADash AI Service - API Key Auth Tests ===" -ForegroundColor Cyan
Write-Host "Service: $ServiceUrl`n"

# Test 1: Health endpoint should work without auth (AllowAnonymous)
Test-Endpoint -Name "Health (no key) -> 200" `
	-Url "$ServiceUrl/api/ai/health" -ExpectedStatus 200

# Test 2: Protected endpoint without API key should return 401
Test-Endpoint -Name "Diagnostics (no key) -> 401" `
	-Url "$ServiceUrl/api/ai/diagnostics" -ExpectedStatus 401

# Test 3: Protected endpoint with invalid API key should return 401
Test-Endpoint -Name "Diagnostics (bad key) -> 401" `
	-Url "$ServiceUrl/api/ai/diagnostics" -Key "invalid-key-12345" -ExpectedStatus 401

# Test 4: Tools endpoint without API key should return 401
Test-Endpoint -Name "Tools (no key) -> 401" `
	-Url "$ServiceUrl/api/ai/tools" -ExpectedStatus 401

# Test 5: Ask endpoint without API key should return 401
Test-Endpoint -Name "Ask (no key) -> 401" `
	-Url "$ServiceUrl/api/ai/ask" -Method "POST" `
	-Body '{"question":"test"}' -ExpectedStatus 401

if ($ApiKey) {
	Write-Host "`n--- With valid API key ---" -ForegroundColor Cyan

	# Test 6: Protected endpoint with valid key should return 200
	Test-Endpoint -Name "Diagnostics (valid key) -> 200" `
		-Url "$ServiceUrl/api/ai/diagnostics" -Key $ApiKey -ExpectedStatus 200

	# Test 7: Tools endpoint with valid key should return 200
	Test-Endpoint -Name "Tools (valid key) -> 200" `
		-Url "$ServiceUrl/api/ai/tools" -Key $ApiKey -ExpectedStatus 200
} else {
	Write-Host "`n--- Skipping valid-key tests (no -ApiKey provided) ---" -ForegroundColor Yellow
}

Write-Host "`n=== Results: $passed passed, $failed failed ===" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
