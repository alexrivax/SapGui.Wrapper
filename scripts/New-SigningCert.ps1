<#
.SYNOPSIS
    Creates a self-signed code-signing certificate and exports it as a PFX,
    then signs the NuGet package(s) in the ../nupkg/ folder.

.DESCRIPTION
    Run this script once to bootstrap local package signing.
    The generated PFX is intentionally NOT committed to source control.
    Record the certificate thumbprint in your CI secret store or in a local
    notes file so you can reference it for verification later.

.PARAMETER PfxPath
    Where to write the PFX file. Defaults to .\SapGui.Wrapper.pfx (next to this script).

.PARAMETER PfxPassword
    Password to protect the PFX. You will be prompted if not supplied.

.PARAMETER NupkgFolder
    Folder containing the .nupkg files to sign. Defaults to ..\nupkg.

.EXAMPLE
    .\New-SigningCert.ps1
    .\New-SigningCert.ps1 -PfxPath "C:\certs\SapGui.pfx" -PfxPassword (ConvertTo-SecureString "s3cr3t" -AsPlainText -Force)

.NOTES
    SECURITY:  Keep the PFX file and its password secret.
               Do NOT commit the PFX to source control.
               Add *.pfx to .gitignore.
               Store the password in a secrets manager (Azure Key Vault, GitHub Actions secrets, etc.).

    THUMBPRINT: After running this script, note the thumbprint printed at the end.
                Add it to CONTRIBUTING.md or a local notes file so you can run
                    dotnet nuget verify SapGui.Wrapper.*.nupkg --certificate-fingerprint <thumbprint>
                to confirm packages are signed correctly.
#>
[CmdletBinding()]
param(
    [string]     $PfxPath     = (Join-Path $PSScriptRoot 'SapGui.Wrapper.pfx'),
    [securestring]$PfxPassword,
    [string]     $NupkgFolder = (Join-Path $PSScriptRoot '..\nupkg')
)

# Prompt for password if not provided
if (-not $PfxPassword) {
    $PfxPassword = Read-Host -AsSecureString -Prompt 'PFX password (leave blank for no password)'
}

# ── 1. Create self-signed certificate ────────────────────────────────────────
Write-Host 'Creating self-signed code-signing certificate...'

$cert = New-SelfSignedCertificate `
    -Subject        'CN=SapGui.Wrapper Package Signing, O=alexrivax' `
    -KeyUsage       DigitalSignature `
    -KeyAlgorithm   RSA `
    -KeyLength      3072 `
    -HashAlgorithm  SHA256 `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -Type           CodeSigningCert `
    -NotAfter       (Get-Date).AddYears(5)

$thumbprint = $cert.Thumbprint
Write-Host "Certificate created. Thumbprint: $thumbprint"

# ── 2. Export to PFX ─────────────────────────────────────────────────────────
Write-Host "Exporting PFX to: $PfxPath"
Export-PfxCertificate -Cert $cert -FilePath $PfxPath -Password $PfxPassword | Out-Null
Write-Host 'PFX exported.'

# ── 3. Sign all .nupkg files in the nupkg folder ─────────────────────────────
$nupkgs = Get-ChildItem -Path $NupkgFolder -Filter '*.nupkg' -ErrorAction SilentlyContinue
if ($nupkgs.Count -eq 0) {
    Write-Warning "No .nupkg files found in '$NupkgFolder'. Build and pack first, then re-run signing."
} else {
    $bstr     = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($PfxPassword)
    $plainPwd = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

    foreach ($pkg in $nupkgs) {
        Write-Host "Signing $($pkg.Name)..."
        dotnet nuget sign $pkg.FullName `
            --certificate-path $PfxPath `
            --certificate-password $plainPwd `
            --timestamper 'http://timestamp.digicert.com' `
            --overwrite
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Signing failed for $($pkg.Name) (exit $LASTEXITCODE). Continuing."
        } else {
            Write-Host "  Signed OK."
        }
    }

    # Zero out the plain-text password variable
    $plainPwd = $null
}

# ── 4. Summary ────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '──────────────────────────────────────────────────────────'
Write-Host "Certificate thumbprint : $thumbprint"
Write-Host "PFX location           : $PfxPath"
Write-Host ''
Write-Host 'NEXT STEPS:'
Write-Host '  1. Record the thumbprint above in CONTRIBUTING.md or your CI secrets.'
Write-Host '  2. Verify a signed package with:'
Write-Host "       dotnet nuget verify <package>.nupkg --certificate-fingerprint $thumbprint"
Write-Host '  3. Add *.pfx to .gitignore so the key file is never committed.'
Write-Host '──────────────────────────────────────────────────────────'
