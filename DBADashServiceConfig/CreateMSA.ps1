<# Prerequsites 

    * Install-WindowsFeature RSAT-AD-PowerShell
    * Run on server running DBA Dash service.
    * Run as a domain admin.  
    * Run as Administrator
    * KDS Root Key (script will print info if not available)

#>
#### Parameters ####

# Enter the name of the service account.  Don't include the domain name or the $.

$ServiceName = "DBADash"

####################

# Check if we have a KDS Root Key
if ((Get-KdsRootKey) -eq $null){
    Write-Warning "KDS root key required.
Run this command:
Add-KdsRootKey -EffectiveImmediately
Note: This will take some time to replicate.  See MS docs for more information
https://learn.microsoft.com/en-us/powershell/module/kds/add-kdsrootkey
"
    return
}

# A group could be used here if preferred.  Using the machine account avoids a reboot before you can install
# The AllowedPrincipals can be updated later using Set-ADServiceAccount. 
# https://learn.microsoft.com/en-us/powershell/module/activedirectory/set-adserviceaccount
$AllowedPrincipals = $env:COMPUTERNAME + "$"
$DNSHostName = $ServiceName + "." + (Get-ADDomain).DNSRoot

# Create the service account
New-ADServiceAccount -name $ServiceName -DNSHostName $DNSHostName -PrincipalsAllowedToRetrieveManagedPassword $AllowedPrincipals

# Install the service account so we can use it on this server
Install-ADServiceAccount $ServiceName

"Enter '" + ((Get-ADDomain).DNSRoot) + "\" + $ServiceName + "$' as the service name when installing the DBA Dash Service.  Use the permissions helper to assign permissions to the service account."
