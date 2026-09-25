using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace DBADashGUI.ShellIntegration
{
    /// <summary>
    /// The applications Windows has registered to open a file type - the same list Explorer shows under Open with.
    ///
    /// The shell interfaces are called through their vtables rather than declared as [ComImport] interfaces: the
    /// runtime can't wrap the handler objects (Marshal.GetObjectForIUnknown fails with RPC_E_UNEXPECTED), and this
    /// way no COM object outlives the call that uses it - opening a handler enumerates again and finds it by name.
    /// </summary>
    public static class ShellFileHandlers
    {
        /// <summary>One application able to open the file type.  Name is the exe path for a desktop application.</summary>
        public sealed record Handler(string Name, string DisplayName, Image Image);

        /// <summary>The recommended handlers for an extension, e.g. ".xdl".</summary>
        public static List<Handler> Get(string extension)
        {
            var handlers = new List<Handler>();
            Enumerate(extension, handler =>
            {
                var name = GetString(handler, IAssocHandler_GetName);
                if (string.IsNullOrWhiteSpace(name)) return false;
                var uiName = GetString(handler, IAssocHandler_GetUIName);
                handlers.Add(new Handler(name,
                    string.IsNullOrWhiteSpace(uiName) ? Path.GetFileNameWithoutExtension(name) : uiName,
                    GetImage(handler, name)));
                return false;
            });
            return handlers;
        }

        /// <summary>Opens a file with the handler of that name - <see cref="Handler.Name"/>.</summary>
        public static void Open(string extension, string handlerName, string filePath)
        {
            var found = false;
            Enumerate(extension, handler =>
            {
                if (!string.Equals(GetString(handler, IAssocHandler_GetName), handlerName, StringComparison.OrdinalIgnoreCase)) return false;
                found = true;
                Invoke(handler, filePath);
                return true;
            });
            if (!found) throw new InvalidOperationException($"{handlerName} is no longer registered to open {extension} files.");
        }

        /// <summary>
        /// Shows the Windows Open with dialog for a file, then opens it with whatever is picked - for an
        /// application that isn't registered for the file type.
        /// </summary>
        public static void ShowOpenWithDialog(string filePath, IntPtr owner)
        {
            var info = new OpenAsInfo
            {
                File = filePath,
                InFlags = OpenAsInfoFlags.Exec | OpenAsInfoFlags.HideRegistration
            };
            var hr = SHOpenWithDialog(owner, ref info);
            // HRESULT_FROM_WIN32(ERROR_CANCELLED) is the user closing the dialog.
            if (hr < 0 && hr != unchecked((int)0x800704C7)) Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>Calls <paramref name="visit"/> for each handler until it returns true.  The pointer is only valid during the call.</summary>
        private static void Enumerate(string extension, Func<IntPtr, bool> visit)
        {
            var hr = SHAssocEnumHandlers(extension, AssocFilterRecommended, out var enumerator);
            if (hr < 0 || enumerator == IntPtr.Zero) return;
            try
            {
                var next = Method<NextFn>(enumerator, IEnumAssocHandlers_Next);
                while (next(enumerator, 1, out var handler, out var fetched) == 0 && fetched == 1 && handler != IntPtr.Zero)
                {
                    try
                    {
                        if (visit(handler)) return;
                    }
                    finally
                    {
                        Marshal.Release(handler);
                    }
                }
            }
            finally
            {
                Marshal.Release(enumerator);
            }
        }

        private static void Invoke(IntPtr handler, string filePath)
        {
            var iidShellItem = IID_IShellItem;
            Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref iidShellItem, out var item));
            try
            {
                var bhid = BHID_DataObject;
                var iidDataObject = IID_IDataObject;
                Marshal.ThrowExceptionForHR(Method<BindToHandlerFn>(item, IShellItem_BindToHandler)(item, IntPtr.Zero, ref bhid, ref iidDataObject, out var dataObject));
                try
                {
                    Marshal.ThrowExceptionForHR(Method<InvokeFn>(handler, IAssocHandler_Invoke)(handler, dataObject));
                }
                finally
                {
                    Marshal.Release(dataObject);
                }
            }
            finally
            {
                Marshal.Release(item);
            }
        }

        private static string GetString(IntPtr obj, int slot)
        {
            if (Method<GetStringFn>(obj, slot)(obj, out var psz) < 0 || psz == IntPtr.Zero) return null;
            try
            {
                return Marshal.PtrToStringUni(psz);
            }
            finally
            {
                Marshal.FreeCoTaskMem(psz);
            }
        }

        private static Image GetImage(IntPtr handler, string name)
        {
            try
            {
                if (Method<GetIconLocationFn>(handler, IAssocHandler_GetIconLocation)(handler, out var psz, out var index) < 0 || psz == IntPtr.Zero) return null;
                string path;
                try
                {
                    path = Environment.ExpandEnvironmentVariables(Marshal.PtrToStringUni(psz) ?? string.Empty);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(psz);
                }
                if (!File.Exists(path)) return null;
                using var icon = Icon.ExtractIcon(path, index, 16);
                return icon?.ToBitmap();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unable to load icon for {handler}", name);
                return null;
            }
        }

        private static T Method<T>(IntPtr obj, int slot) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj), slot * IntPtr.Size));

        // vtable slots - 0 to 2 are IUnknown
        private const int IEnumAssocHandlers_Next = 3;
        private const int IAssocHandler_GetName = 3;
        private const int IAssocHandler_GetUIName = 4;
        private const int IAssocHandler_GetIconLocation = 5;
        private const int IAssocHandler_Invoke = 8;
        private const int IShellItem_BindToHandler = 3;

        private const int AssocFilterRecommended = 1;

        private static readonly Guid BHID_DataObject = new("B8C0BD9F-ED24-455c-83E6-D5390C4FE8C4");
        private static readonly Guid IID_IShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
        private static readonly Guid IID_IDataObject = new("0000010e-0000-0000-C000-000000000046");

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int NextFn(IntPtr self, int celt, out IntPtr rgelt, out int pceltFetched);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetStringFn(IntPtr self, out IntPtr ppsz);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetIconLocationFn(IntPtr self, out IntPtr ppszPath, out int pIndex);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int InvokeFn(IntPtr self, IntPtr pdo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int BindToHandlerFn(IntPtr self, IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHAssocEnumHandlers(string pszExtra, int afFilter, out IntPtr ppEnumHandler);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, out IntPtr ppv);

        [Flags]
        private enum OpenAsInfoFlags
        {
            Exec = 0x04,
            HideRegistration = 0x20
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenAsInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string File;
            [MarshalAs(UnmanagedType.LPWStr)] public string Class;
            public OpenAsInfoFlags InFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHOpenWithDialog(IntPtr hwndParent, ref OpenAsInfo oOAI);
    }
}
