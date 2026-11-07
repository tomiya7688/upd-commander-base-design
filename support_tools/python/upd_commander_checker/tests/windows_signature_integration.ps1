$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

$source = Join-Path $PWD 'dist/upd-commander-check/upd-commander-check.exe'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Unsigned CUI fixture is missing: $source"
}

$temporary = Join-Path $env:RUNNER_TEMP ("upd-signature-smoke-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $temporary | Out-Null
$target = Join-Path $temporary 'fixture.exe'
$fakeSignTool = Join-Path $temporary 'signtool.exe'
$certificate = $null

try {
    Write-Information '[signature-smoke] copy unsigned fixture'
    Copy-Item -LiteralPath $source -Destination $target
    # The verifier must reject this temporary self-signed fixture before it ever launches this placeholder.
    Copy-Item -LiteralPath (Join-Path $env:WINDIR 'System32/where.exe') -Destination $fakeSignTool

    Write-Information '[signature-smoke] create self-signed certificate'
    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject 'CN=UPD CI Test Only' `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -NotAfter (Get-Date).AddDays(1)
    $fingerprint = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($certificate.RawData)).ToLowerInvariant()

    Write-Information '[signature-smoke] sign fixture without timestamp'
    Set-AuthenticodeSignature -FilePath $target -Certificate $certificate -HashAlgorithm SHA256 | Out-Null
    $metadata = Get-AuthenticodeSignature -FilePath $target
    if ($metadata.SignatureType.ToString() -ne 'Authenticode' -or -not $metadata.SignerCertificate) {
        throw "Windows did not report an embedded signer certificate: $($metadata.Status)"
    }
    $observedFingerprint = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($metadata.SignerCertificate.RawData)).ToLowerInvariant()
    if ($observedFingerprint -ne $fingerprint) {
        throw 'Windows reported an unexpected signer certificate'
    }
    if ($metadata.TimeStamperCertificate) {
        throw 'Windows unexpectedly reported a timestamp certificate'
    }

    $arguments = @(
        'scripts/verify_windows_signature.py',
        $target,
        '--signer-sha256',
        $fingerprint,
        '--signtool',
        $fakeSignTool,
        '--trust-immutable-file'
    )
    Write-Information '[signature-smoke] run verifier'
    $output = & python @arguments 2>&1
    $exitCode = $LASTEXITCODE
    $message = $output -join "`n"
    if ($exitCode -eq 0) {
        throw 'Verifier unexpectedly accepted a signature without an RFC 3161 timestamp'
    }
    if ($message -notmatch 'Windows does not report a valid Authenticode signature|Windows did not report a timestamp certificate') {
        throw "Verifier failed for an unexpected reason: $message"
    }
}
finally {
    Write-Information '[signature-smoke] cleanup'
    if ($certificate) {
        Remove-Item -LiteralPath "Cert:\CurrentUser\My/$($certificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue
}

$global:LASTEXITCODE = 0
