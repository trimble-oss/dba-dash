using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.InstanceMetadata
{
    public class GenericInstanceMetadata: InstanceMetadataBase
    {
        public override string ProviderName => "Generic";
        protected override string PowerShellCommand  => """
            $meta = @{Provider='Generic'};
            {GenericMetadataScript}
            $meta | ConvertTo-Json -Depth 10
        """.Replace("{GenericMetadataScript}", GenericMetadata);
    }
}
