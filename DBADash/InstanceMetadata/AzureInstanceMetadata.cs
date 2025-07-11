namespace DBADash.InstanceMetadata
{
    public class AzureInstanceMetadata : InstanceMetadataBase
    {
        public override string ProviderName => "Azure";

        protected override string PowerShellCommand => """
            try {
                $meta = Invoke-RestMethod -Headers @{"Metadata"="true"} -Method GET -Uri "http://169.254.169.254/metadata/instance?api-version=2021-12-13" -TimeoutSec 15
                
                $meta | Add-Member -MemberType NoteProperty -Name "Provider" -Value "Azure" -Force
                
                {GenericMetadataScript}

                $meta | ConvertTo-Json -Depth 10
            } catch {
                throw "Azure metadata retrieval failed: $($_.Exception.Message)"
            }
            
            """.Replace("{GenericMetadataScript}", GenericMetadata);
    }
}
