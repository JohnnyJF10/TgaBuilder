using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TgaBuilderWpfUi.Services
{
    public static class FolderPicker
    {
        public static string? ShowDialog(string title, Window? owner = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException(nameof(title));

            IFileOpenDialog? dialog = null;
            IShellItem? item = null;
            IntPtr pszPath = IntPtr.Zero;

            try
            {
                // Create COM-Object for Dialog
                dialog = (IFileOpenDialog)new FileOpenDialog();
                dialog.SetTitle(title);

                // Set Folder Picking for the dialog
                dialog.GetOptions(out uint options);
                options |= (uint)(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
                dialog.SetOptions(options);

                // Show Dialog modal to the owner window
                IntPtr hwndOwner = owner != null
                    ? new WindowInteropHelper(owner).Handle
                    : IntPtr.Zero;

                int hr = dialog.Show(hwndOwner);
                if (hr == HRESULT.S_OK) // 0
                {
                    dialog.GetResult(out item);
                    hr = item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out pszPath);
                    if (hr == HRESULT.S_OK)
                    {
                        return Marshal.PtrToStringUni(pszPath);
                    }
                }
                // Error handling (optional)
                //Marshal.ThrowExceptionForHR(hr);
                return null;
            }
            finally
            {
                // Free allocated resources
                if (pszPath != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pszPath);

                if (item != null)
                    Marshal.ReleaseComObject(item);

                if (dialog != null)
                    Marshal.ReleaseComObject(dialog);
            }
        }

        // --- COM-Interop-Definitions ---
        private static class HRESULT
        {
            public const int S_OK = 0;
        }

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        [ClassInterface(ClassInterfaceType.None)]
        private class FileOpenDialog { }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            int Show([In] IntPtr hwndParent);
            void SetFileTypes(uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions([In] uint fos);
            void GetOptions(out uint fos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, uint fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            [PreserveSig]
            int GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        private enum SIGDN : uint
        {
            SIGDN_FILESYSPATH = 0x80058000,
        }

        [Flags]
        private enum FOS : uint
        {
            FOS_PICKFOLDERS = 0x20,
            FOS_FORCEFILESYSTEM = 0x40,
            FOS_PATHMUSTEXIST = 0x800,
        }
    }
}
