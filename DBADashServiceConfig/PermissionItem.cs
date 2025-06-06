using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace DBADashServiceConfig
{
    public class PermissionItem
    {
        public string Name { get; set; }

        public bool Grant
        {
            get => PermissionState == PermissionStates.Grant;
            set => PermissionState = value ? PermissionStates.Grant : PermissionState == PermissionStates.Grant ? PermissionStates.None : PermissionState;
        }

        public bool Revoke
        {
            get => PermissionState == PermissionStates.Revoke;
            set => PermissionState = value ? PermissionStates.Revoke : PermissionState == PermissionStates.Revoke ? PermissionStates.None : PermissionState;
        }

        public PermissionStates PermissionState { get; set; }

        public PermissionTypes PermissionType { get; set; }

        public enum PermissionTypes
        {
            ServerPermission,
            DatabaseRole,
            ServerRole,
            ExecuteProcedure
        }

        public override string ToString()
        {
            return $"{Name}: {PermissionState}";
        }

        public enum PermissionStates
        {
            None,
            Grant,
            Revoke
        }
    }
}