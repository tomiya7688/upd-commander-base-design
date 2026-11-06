$ErrorActionPreference = 'Stop'

$source = Join-Path $PWD 'dist/upd-commander-check/upd-commander-check.exe'
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Unsigned CUI fixture is missing: $source"
}

$temporary = Join-Path $env:RUNNER_TEMP ("upd-signature-smoke-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $temporary | Out-Null
$target = Join-Path $temporary 'fixture.exe'
$fakeSignTool = Join-Path $temporary 'signtool.exe'
$certificateFile = Join-Path $temporary 'test-code-signing.cer'
$certificate = $null
$rootCertificateInstalled = $false

try {
    Copy-Item -LiteralPath $source -Destination $target
    # The verifier must reject the absent timestamp before it ever launches this placeholder.
    Copy-Item -LiteralPath (Join-Path $env:WINDIR 'System32/where.exe') -Destination $fakeSignTool

    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject 'CN=UPD CI Test Only' `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -NotAfter (Get-Date).AddDays(1)
    Export-Certificate -Cert $certificate -FilePath $certificateFile | Out-Null
    Import-Certificate -FilePath $certificateFile -CertStoreLocation 'Cert:\CurrentUser\Root' | Out-Null
    $rootCertificateInstalled = $true

    $signature = Set-AuthenticodeSignature -FilePath $target -Certificate $certificate -HashAlgorithm SHA256
    if ($signature.Status.ToString() -ne 'Valid') {
        throw "Test fixture signing failed: $($signature.Status)"
    }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $fingerprint = [BitConverter]::ToString($sha256.ComputeHash($certificate.RawData)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
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
    $output = & python @arguments 2>&1
    $exitCode = $LASTEXITCODE
    $message = $output -join "`n"
    if ($exitCode -eq 0) {
        throw 'Verifier unexpectedly accepted a signature without an RFC 3161 timestamp'
    }
    if ($message -notmatch 'Windows did not report a timestamp certificate') {
        throw "Verifier failed for an unexpected reason: $message"
    }
}
finally {
    if ($certificate) {
        Remove-Item -LiteralPath "Cert:\CurrentUser\My/$($certificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
        if ($rootCertificateInstalled) {
            Remove-Item -LiteralPath "Cert:\CurrentUser\Root/$($certificate.Thumbprint)" -Force -ErrorAction SilentlyContinue
        }
    }
    Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue
}

$global:LASTEXITCODE = 0
