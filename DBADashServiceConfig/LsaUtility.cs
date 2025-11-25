using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

public static class LsaUtility
{
    [DllImport("advapi32.dll", PreserveSig = true)]
    private static extern uint LsaOpenPolicy(
        ref LSA_UNICODE_STRING SystemName,
        ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
        int DesiredAccess,
        out IntPtr PolicyHandle);

    [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
    private static extern uint LsaAddAccountRights(
        IntPtr PolicyHandle,
        IntPtr AccountSid,
        LSA_UNICODE_STRING[] UserRights,
        int CountOfRights);

    [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
    private static extern uint LsaEnumerateAccountRights(
        IntPtr PolicyHandle,
        IntPtr AccountSid,
        out IntPtr UserRights,
        out int CountOfRights);

    [DllImport("advapi32.dll", PreserveSig = true)]
    private static extern uint LsaFreeMemory(IntPtr Buffer);

    [DllImport("advapi32.dll")]
    private static extern int LsaClose(IntPtr ObjectHandle);

    [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true)]
    private static extern uint LsaNtStatusToWinError(uint Status);

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_OBJECT_ATTRIBUTES
    {
        public int Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public int Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    private const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
    private const int POLICY_CREATE_ACCOUNT = 0x00000010;
    private const int POLICY_LOOKUP_NAMES = 0x00000800;
    private const string LogonAsAServiceRight = "SeServiceLogonRight";

    public static void GrantLogOnAsService(string username)
    {
        var sid = (SecurityIdentifier)new NTAccount(username)
            .Translate(typeof(SecurityIdentifier));

        var sidBytes = new byte[sid.BinaryLength];
        sid.GetBinaryForm(sidBytes, 0);

        IntPtr sidPtr = Marshal.AllocHGlobal(sidBytes.Length);
        Marshal.Copy(sidBytes, 0, sidPtr, sidBytes.Length);

        var systemName = new LSA_UNICODE_STRING();
        var objectAttributes = new LSA_OBJECT_ATTRIBUTES
        {
            Length = Marshal.SizeOf<LSA_OBJECT_ATTRIBUTES>()
        };

        // Need POLICY_CREATE_ACCOUNT and POLICY_LOOKUP_NAMES to add account rights
        uint result = LsaOpenPolicy(ref systemName, ref objectAttributes, POLICY_CREATE_ACCOUNT | POLICY_LOOKUP_NAMES, out IntPtr policyHandle);
        if (result != 0)
        {
            Marshal.FreeHGlobal(sidPtr);
            throw new Win32Exception((int)LsaNtStatusToWinError(result), "Failed to open LSA policy. Ensure the application is running with administrative privileges.");
        }

        try
        {
            var rights = new LSA_UNICODE_STRING[1];
            rights[0] = new LSA_UNICODE_STRING();
            rights[0].Buffer = Marshal.StringToHGlobalUni(LogonAsAServiceRight);
            rights[0].Length = (ushort)(2 * LogonAsAServiceRight.Length);
            rights[0].MaximumLength = (ushort)(rights[0].Length + 2);

            try
            {
                result = LsaAddAccountRights(policyHandle, sidPtr, rights, 1);
                if (result != 0)
                {
                    throw new Win32Exception((int)LsaNtStatusToWinError(result), $"Failed to grant 'Log on as a service' right to '{username}'.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(rights[0].Buffer);
            }
        }
        finally
        {
            LsaClose(policyHandle);
            Marshal.FreeHGlobal(sidPtr);
        }
    }

    /// <summary>
    /// Checks if a user has the "Log on as a service" right.
    /// Assumes virtual service accounts and built-in accounts have implicit rights.
    /// Note: It will return false for managed service accounts which should have implicit rights, but no harm in this check failing and granting explicit rights
    /// It doesn't check group membership.  There is no harm in granting explicit rights if the user already has rights via group membership
    /// </summary>
    /// <param name="username">Username in format "DOMAIN\Username" or just "Username"</param>
    /// <returns>True if the user has logon as a service right</returns>
    public static bool HasLogOnAsServiceRight(string username)
    {
        if (UserHasImplicitLogonAsAServiceRight(username))
        {
            return true;
        }
        var userIdentity = new NTAccount(username);
        var userSid = (SecurityIdentifier)userIdentity.Translate(typeof(SecurityIdentifier));

        // Check direct assignment to the user
        return HasRightForSid(userSid, LogonAsAServiceRight);
    }

    private static bool UserHasImplicitLogonAsAServiceRight(string username)
    {
        // Virtual service accounts and built-in accounts like LocalSystem, NetworkService, and LocalService should have implicit rights
        return username.StartsWith("NT SERVICE\\", StringComparison.OrdinalIgnoreCase) || username.StartsWith("NT AUTHORITY\\", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a specific SID has the right specified. e.g. SeServiceLogonRight
    /// </summary>
    private static bool HasRightForSid(SecurityIdentifier sid, string rightToCheck)
    {
        var sidBytes = new byte[sid.BinaryLength];
        sid.GetBinaryForm(sidBytes, 0);

        IntPtr sidPtr = Marshal.AllocHGlobal(sidBytes.Length);
        Marshal.Copy(sidBytes, 0, sidPtr, sidBytes.Length);

        var systemName = new LSA_UNICODE_STRING();
        var objectAttributes = new LSA_OBJECT_ATTRIBUTES
        {
            Length = Marshal.SizeOf<LSA_OBJECT_ATTRIBUTES>()
        };

        uint result = LsaOpenPolicy(ref systemName, ref objectAttributes, POLICY_LOOKUP_NAMES, out IntPtr policyHandle);
        if (result != 0)
        {
            Marshal.FreeHGlobal(sidPtr);
            throw new Win32Exception((int)LsaNtStatusToWinError(result));
        }

        try
        {
            result = LsaEnumerateAccountRights(policyHandle, sidPtr, out IntPtr rightsPtr, out int rightsCount);

            // If the account has no rights assigned, STATUS_OBJECT_NAME_NOT_FOUND is returned
            if (result == STATUS_OBJECT_NAME_NOT_FOUND)
            {
                return false;
            }

            if (result != 0)
            {
                throw new Win32Exception((int)LsaNtStatusToWinError(result));
            }

            try
            {
                var currentRight = rightsPtr;
                for (int i = 0; i < rightsCount; i++)
                {
                    var lsaString = Marshal.PtrToStructure<LSA_UNICODE_STRING>(currentRight);
                    var rightName = Marshal.PtrToStringUni(lsaString.Buffer, lsaString.Length / 2);

                    if (rightName == rightToCheck)
                    {
                        return true;
                    }

                    currentRight = IntPtr.Add(currentRight, Marshal.SizeOf<LSA_UNICODE_STRING>());
                }

                return false;
            }
            finally
            {
                LsaFreeMemory(rightsPtr);
            }
        }
        finally
        {
            LsaClose(policyHandle);
            Marshal.FreeHGlobal(sidPtr);
        }
    }
}