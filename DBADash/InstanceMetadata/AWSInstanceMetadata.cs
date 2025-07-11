namespace DBADash.InstanceMetadata
{
    public class AWSInstanceMetadata : InstanceMetadataBase
    {
        public override string ProviderName => "AWS";

        protected override string PowerShellCommand => """
            try {
                # Get IMDSv2 token
                $token = Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token-ttl-seconds"="21600"} -Method PUT -Uri "http://169.254.169.254/latest/api/token" -TimeoutSec 10
                
                # Get base instance identity document
                $meta = Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/dynamic/instance-identity/document" -TimeoutSec 10
                
                # Get additional metadata
                $additionalMetadata = @{
                    SecurityGroups = try { (Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/security-groups" -TimeoutSec 5) -split "`n" } catch { @() };
                    IamRole = try { Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/iam/security-credentials/" -TimeoutSec 5 } catch { $null };
                    PublicHostname = try { Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/public-hostname" -TimeoutSec 5 } catch { $null };
                    LocalHostname = try { Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/local-hostname" -TimeoutSec 5 } catch { $null };
                    MacAddress = try { Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/mac" -TimeoutSec 5 } catch { $null };
                    PlacementGroupName = try { Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/placement/group-name" -TimeoutSec 5 } catch { $null };
                    PlacementPartitionNumber = try { Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/placement/partition-number" -TimeoutSec 5 } catch { $null };
                    ReservationId = try { Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/reservation-id" -TimeoutSec 5 } catch { $null };
                    Provider = "AWS"
                }
                try {
                    # Get tag keys.  
                    $tagKeys = (Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/tags/instance/" -TimeoutSec 5) -split "`n"

                    # For each tag key, get its value
                    foreach ($key in $tagKeys) {
                        if ($key) { # Ensure the key is not empty
                            $tagValue = Invoke-RestMethod -Headers @{"X-aws-ec2-metadata-token"=$token} -Method GET -Uri "http://169.254.169.254/latest/meta-data/tags/instance/$key" -TimeoutSec 5
                            $tags[$key] = $tagValue
                        }
                    }
                    $additionalMetadata.Tags = $tags
                } catch {
                    # Ignore error.  We might get a 404 if allow tags in instance metadata is not enabled
                }
                foreach ($key in $additionalMetadata.Keys) {
                    $meta | Add-Member -MemberType NoteProperty -Name $key -Value $additionalMetadata[$key] -Force
                }
                
                {GenericMetadataScript}

                $meta | ConvertTo-Json -Depth 10
            } catch {
                throw "AWS metadata retrieval failed: $($_.Exception.Message)"
            }
            """.Replace("{GenericMetadataScript}", GenericMetadata);
    }
}
