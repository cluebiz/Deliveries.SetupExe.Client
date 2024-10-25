﻿using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

namespace Deliveries.SetupExe.Logic
{


    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);


    }



    public class Functions
    {
        //variables, structs, const,..
        private ResolutionNative.DEVMODE originalResolution = new ResolutionNative.DEVMODE();

        const int SERVICETIMEOUT = 30000; //milliseconds

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWDEFAULT = 10;

        public const int CDS_UPDATEREGISTRY = 0x01;
        public const int CDS_TEST = 0x02;

        public const int DISP_CHANGE_SUCCESSFUL = 0;
        public const int DISP_CHANGE_RESTART = 1;
        public const int DISP_CHANGE_FAILED = -1;

        //my own class
        Logging logger = new Logging();

        private struct SystemPowerStatus
        {
            public byte ACLineStatus;
            byte BatteryFlag;
            byte BatteryLifePercent;
            byte Reserved1;
            int BatteryLifeTime;
            int BatteryFullLifeTime;
        }

        private struct RECT
        {
            public int left;
            public int right;
            public int top;
            public int bottom;
        }

        private struct MonitorInfo
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        /*
        public struct MemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public uint TotalPhysical;
            public uint AvailablePhysical;
            public uint TotalPageFile;
            public uint AvailablePageFile;
            public uint TotalVirtual;
            public uint AvailableVirtual;
        } 
        */




        //external functions
        [DllImport("kernel32.dll")]
        private static extern bool GetSystemPowerStatus(ref SystemPowerStatus systemPowerStatus);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromRect([In] ref RECT rc, uint dwFlags);
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, out MonitorInfo lpMi);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(IntPtr hWnd);


        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);
        [DllImport("shell32.dll")]
        static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);


        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extraInfo);


        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        public static extern int AddFontResource([In][MarshalAs(UnmanagedType.LPWStr)]
                                         string lpFileName);

        [DllImport("gdi32.dll", EntryPoint = "RemoveFontResourceW", SetLastError = true)]
        public static extern int RemoveFontResource([In][MarshalAs(UnmanagedType.LPWStr)]
                                            string lpFileName);


        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        //private IDictionary<string, uint> MouseActions = new Dictionary<string, uint>()
        //{
        //    { "LeftDown", 0x00000002},
        //    { "LeftUp", 0x00000004},
        //    { "MIDDLEDOWN", 0x00000020},
        //    { "MIDDLEUP", 0x00000040},
        //    { "MOVE", 0x00000001},
        //    { "ABSOLUTE", 0x00008000},
        //    { "RightDown", 0x00000008},
        //    { "RightUp", 0x00000010},
        //    { "WHEEL", 0x00000800},
        //    { "XDOWN", 0x00000080},
        //    { "XUP", 0x00000100}
        //};

        [DllImport("shlwapi.dll", SetLastError = true, EntryPoint = "#437")]
        private static extern bool IsOS(int os);

        [DllImport("user32.dll")]
        public extern static bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string pwszReason);



        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public int LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public LUID Luid;
            public int Attributes;
            public int PrivilegeCount;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int OpenProcessToken(int ProcessHandle, int DesiredAccess, ref int tokenhandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetCurrentProcess();

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int LookupPrivilegeValue(string lpsystemname, string lpname, [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int AdjustTokenPrivileges(int tokenhandle, int disableprivs, [MarshalAs(UnmanagedType.Struct)] ref TOKEN_PRIVILEGES Newstate, int bufferlength, int PreivousState, int Returnlength);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegLoadKey(uint hKey, string lpSubKey, string lpFile);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegUnLoadKey(uint hKey, string lpSubKey);

        public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        public const int TOKEN_QUERY = 0x00000008;
        public const int SE_PRIVILEGE_ENABLED = 0x00000002;
        public const string SE_RESTORE_NAME = "SeRestorePrivilege";
        public const string SE_BACKUP_NAME = "SeBackupPrivilege";
        public const uint HKEY_USERS = 0x80000003;

        //temporary hive key
        public const string HIVE_SUBKEY = "DELIVERIESSETUPUSER";


        static private Boolean gotPrivileges = false;






        const int OS_ANYSERVER = 29;

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_CHAR = 0x105;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;

        const Int32 VK_RETURN = 0x0D;
        const int VK_ENTER = 0x0D;


        public bool CheckArchitecture()
        {
            bool isx64 = false;

            try
            {
                ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                ManagementObjectCollection qryCol = qry.Get();
                foreach (ManagementObject mo in qryCol)
                {
                    PropertyDataCollection propCol = mo.Properties;
                    foreach (PropertyData propdata in propCol)
                    {
                        if (propdata.Name.ToUpper() == "OSARCHITECTURE") //property not availabe on XP
                        {
                            if (propdata.Value.ToString().ToUpper() == "64-BIT")
                            {
                                isx64 = true;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //do nothing
            }

            if (!isx64)
            {
                bool is64BitProcess = (IntPtr.Size == 8);
                bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();
                if (is64BitOperatingSystem)
                {
                    isx64 = true;
                }
            }
            return isx64;
        }

        public static bool InternalCheckIsWow64()
        {
            try
            {
                if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6)
                {
                    using (Process p = Process.GetCurrentProcess())
                    {
                        bool retVal;
                        if (!IsWow64Process(p.Handle, out retVal))
                        {
                            return false;
                        }
                        return retVal;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CheckMemory(string memory)
        {
            /*
            MemoryStatus stat = new MemoryStatus();
            GlobalMemoryStatusEx(out stat);
            long ram = (long)stat.TotalPhysical;
            MessageBox.Show(ram.ToString());
            */
            bool memoryOK = true;
            try
            {
                Int64 memory2Check = Convert.ToInt64(memory) * 1024 * 1024;
                ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                ManagementObjectCollection qryCol = qry.Get();
                foreach (ManagementObject mo in qryCol)
                {
                    PropertyDataCollection propCol = mo.Properties;
                    foreach (PropertyData propdata in propCol)
                    {
                        if (propdata.Name.ToUpper() == "TOTALPHYSICALMEMORY")
                        {
                            if (Convert.ToInt64(propdata.Value.ToString()) >= memory2Check)
                            {
                                memoryOK = true;
                            }
                            else
                            {
                                memoryOK = false;
                            }
                        }
                    }
                }
                return memoryOK;
            }
            catch (Exception)
            {
                return memoryOK;
            }
        }

        public bool ExpandFolder(string source, string dest, string overwrite)
        {
            bool lbReturn = true;

            if (!System.IO.Directory.Exists(dest))
            {
                try
                {
                    CreateDirectory(dest);
                }
                catch { }
            }
            if (System.IO.File.Exists(source))
            {
                try
                {
                    Ionic.Zip.ZipFile loZip = new Ionic.Zip.ZipFile(source);
                    if (overwrite=="false")
                    {
                        loZip.ExtractAll(dest, Ionic.Zip.ExtractExistingFileAction.DoNotOverwrite);
                    }
                    else
                    {
                        loZip.ExtractAll(dest, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
                    }
                }
                catch { }
            }

            return lbReturn;
        }

        public bool CopyFolder(string source, string dest, string overwrite, DataTable loVarTable, DataTable loParameterTable)
        {
            bool successfullyCopied = true;
            bool overwriteb = false;
            if (overwrite.ToLower() == "true")
            {
                overwriteb = true;
            }
            ReplaceEnvVariables(ref source, loVarTable, loParameterTable);
            ReplaceEnvVariables(ref dest, loVarTable, loParameterTable);
            try
            {
                CopyDirectory(source, dest, overwriteb);
            }
            catch { }
            return successfullyCopied;
        }

        public bool CopyDirectory(string lsSource, string lsTarget, bool lbOverwrite)
        {
            if (lsSource.EndsWith(@"\"))
            {
                lsSource = lsSource.Substring(0, lsSource.Length - 1);
            }
            if (lsTarget.EndsWith(@"\"))
            {
                lsTarget = lsTarget.Substring(0, lsTarget.Length - 1);
            }
            CreateDirectory(lsTarget);

            CopyCurrentFilesFromDirectory(lsSource, lsTarget, lbOverwrite);


            return true;
        }


        public bool CreateShortcut(string lsPath, string lsTarget, string lsWorkDir, string lsArguments, string lsIconPath, Int32 liIconIndex, string lsDescription)
        {

            try
            {
                if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(lsPath)))
                {
                    CreateDirectory(System.IO.Path.GetDirectoryName(lsPath));
                }
            }
            catch { }

            try
            {
                if (System.IO.File.Exists(lsPath))
                {
                    System.IO.File.Delete(lsPath);
                }
            }
            catch { }

            try
            {
                IShellLink link = (IShellLink)new ShellLink();

                // setup shortcut information
                //link.SetDescription("My Description");
                link.SetPath(lsTarget);

                if (lsWorkDir != "")
                {
                    try
                    {
                        link.SetWorkingDirectory(lsWorkDir);
                    }
                    catch { }
                }

                if (lsIconPath != "")
                {
                    try
                    {
                        link.SetIconLocation(lsIconPath, liIconIndex);
                    }
                    catch { }
                }

                if (lsArguments != "")
                {
                    try
                    {
                        link.SetArguments(lsArguments);
                    }
                    catch { }
                }

                if (lsDescription != "")
                {
                    try
                    {
                        link.SetDescription(lsDescription);
                    }
                    catch { }
                }

                // save it
                System.Runtime.InteropServices.ComTypes.IPersistFile file = (System.Runtime.InteropServices.ComTypes.IPersistFile)link;
                //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                file.Save(lsPath, false);
            }
            catch { }


            bool lbReturn = false;
            if (System.IO.File.Exists(lsPath))
            {
                lbReturn = true;
            }

            return lbReturn;

        }

        public bool AddFont(string lsFont)
        {

            int result = 0;
            try
            {
                result = AddFontResource(lsFont);
            }
            catch { }
            return true;
        }

        public bool RemoveFont(string lsFont)
        {

            int result = 0;
            try
            {
                result = RemoveFontResource(lsFont);
            }
            catch { }
            return true;
        }

        public bool AddPath(string lsValue)
        {

            int result = 0;
            try
            {
                try
                {
                    string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine) + ";" + Environment.ExpandEnvironmentVariables(lsValue);
                    Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Machine);
                    result = 1;
                }
                catch { }
            }
            catch { }
            return true;
        }

        public bool RemovePath(string lsValue)
        {

            int result = 0;
            try
            {
                try
                {
                    string path = Regex.Replace(Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine), Environment.ExpandEnvironmentVariables(lsValue), "", RegexOptions.IgnoreCase);
                    path = path.Replace(";;", ";");
                    path = path.Trim();
                    Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Machine);
                    result = 1;
                }
                catch { }
            }
            catch { }
            return true;
        }

        [STAThread]
        public static string GetShortcutTargetFile(string shortcutFilename)
        {
            string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);

            try
            {
                IWshRuntimeLibrary.WshShell loShell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.WshShortcut loShortcut = (IWshRuntimeLibrary.WshShortcut)loShell.CreateShortcut(shortcutFilename);
                return loShortcut.TargetPath;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return string.Empty;
        }

        public bool RemoveShortcuts(string lsPath, string lsFileName, string lsMode, string[] lsBeforeStartMenu, string [] lsBeforeDesktop)
        {
            if (lsPath.EndsWith(@"\"))
            {
                try
                {
                    lsPath = lsPath.Substring(0, lsPath.Length - 1);
                }
                catch { }
            }

            switch(lsMode)
            {
                case "createdduringinstall":

                    if (lsPath.ToLower().Contains("public"))
                    {
                        try {
                            string[] filesafter;
                            filesafter = Directory.GetFiles(Environment.ExpandEnvironmentVariables(@"%Public%\Desktop"), "*.*", SearchOption.AllDirectories);
                            foreach (string lsFile in filesafter)
                            {
                                bool lbFoundInBefore = false;
                                foreach (string lsBeforeFile in lsBeforeDesktop)
                                {
                                    if (lsBeforeFile == lsFile)
                                    {
                                        lbFoundInBefore = true;
                                    }
                                }
                                if (!lbFoundInBefore)
                                {
                                    try
                                    {
                                        System.IO.File.Delete(lsFile);
                                    }
                                    catch { }
                                }
                            }

                        }
                        catch { }
                    }
                


            break;
                case "brokentarget":
                default:

                    try
                    {
                        string[] filesafter;
                        filesafter = Directory.GetFiles(Environment.ExpandEnvironmentVariables(lsPath), "*.*", SearchOption.AllDirectories);
                        foreach (string lsFile in filesafter)
                        {
                            string shortcuttargetfile = GetShortcutTargetFile(lsFile);
                            logger.Info("checking if shortcut " + lsFile + " is valid", GlobalClass.SECTION);
                            bool lbMustDelete = false;
                            if (shortcuttargetfile != "")
                            {
                                logger.Info("targetfile is " + shortcuttargetfile, GlobalClass.SECTION);
                                if (!System.IO.File.Exists(shortcuttargetfile))
                                {
                                    logger.Info("targetfile is broken...", GlobalClass.SECTION);
                                    lbMustDelete = true;
                                }

                                if (lbMustDelete)
                                {
                                    //prevent removal of net drives
                                    if (shortcuttargetfile.StartsWith(@"\\"))
                                    {
                                        lbMustDelete = false;
                                    }
                                    if (lbMustDelete)
                                    {
                                        string lsDrive = shortcuttargetfile.Substring(0, 3);
                                        if (lsDrive.EndsWith(@"\"))
                                        {
                                            bool lbFoundDrive = false;
                                            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
                                            foreach (var drive in drives)
                                            {
                                                string driveName = drive.Name; // C:\, E:\, etc:\

                                                if (driveName.ToUpper() == lsDrive.ToUpper())
                                                {
                                                    lbFoundDrive = true;
                                                    System.IO.DriveType driveType = drive.DriveType;
                                                    switch (driveType)
                                                    {
                                                        case System.IO.DriveType.CDRom:
                                                            lbMustDelete = false;
                                                            break;
                                                        case System.IO.DriveType.Fixed:
                                                            // Local Drive
                                                            break;
                                                        case System.IO.DriveType.Network:
                                                            // Mapped Drive
                                                            lbMustDelete = false;
                                                            break;
                                                        case System.IO.DriveType.NoRootDirectory:
                                                            lbMustDelete = false;
                                                            break;
                                                        case System.IO.DriveType.Ram:
                                                            lbMustDelete = false;
                                                            break;
                                                        case System.IO.DriveType.Removable:
                                                            lbMustDelete = false;
                                                            // Usually a USB Drive
                                                            break;
                                                        case System.IO.DriveType.Unknown:
                                                            lbMustDelete = false;
                                                            break;
                                                    }
                                                }


                                            }

                                            if (!lbFoundDrive)
                                            {
                                                lbMustDelete = false;
                                            }
                                        }
                                    }
                                }


                            }

                            if (lsFileName != "")
                            {
                                if (System.IO.Path.GetFileName(lsFile).ToLower() == lsFileName.ToLower())
                                {
                                    lbMustDelete = true;
                                }
                            }

                            if (lbMustDelete)
                            {
                                logger.Info("removing shortcut " + lsFile, GlobalClass.SECTION);
                                try
                                {
                                    System.IO.File.Delete(lsFile);
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }


                    break;
            }

       

            CleanEmptyStartMenuFolders();

            return true;
        }

        public bool MoveShortcuts(string lsPath, string[] lsBeforeStartMenu)
        {


            if (lsPath.EndsWith(@"\"))
            {
                try
                {
                    lsPath = lsPath.Substring(0, lsPath.Length - 1);
                }
                catch { }
            }

            try
            {
                string[] filesafter;
                filesafter = Directory.GetFiles(Environment.ExpandEnvironmentVariables(@"%ProgramData%\Microsoft\Windows\Start Menu"), "*.*", SearchOption.AllDirectories);
                foreach (string lsFile in filesafter)
                {
                    bool lbFoundInBefore = false;
                    foreach (string lsBeforeFile in lsBeforeStartMenu)
                    {
                        if (lsBeforeFile == lsFile)
                        {
                            lbFoundInBefore = true;
                        }
                    }
                    if (!lbFoundInBefore)
                    {

                        //******************

                        string lsSourceFolder = System.IO.Path.GetDirectoryName(lsFile).ToString();
                        if (!lsSourceFolder.Contains(@"\startup"))
                        {
                            string lsNewTarget = lsPath;
                            try
                            {
                                lsSourceFolder = lsSourceFolder.Replace(Environment.ExpandEnvironmentVariables(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs\"), "");
                            }
                            catch { }
                            try
                            {
                                lsSourceFolder = lsSourceFolder.Replace(Environment.ExpandEnvironmentVariables(@"%ProgramData%\Microsoft\Windows\Start Menu\"), "");
                            }
                            catch { }
                            try
                            {
                                lsSourceFolder = Regex.Replace(lsSourceFolder, @"Start Menu\", "", RegexOptions.IgnoreCase);
                            }
                            catch { }
                            try
                            {
                                lsSourceFolder = Regex.Replace(lsSourceFolder, @"Programs\", "", RegexOptions.IgnoreCase);
                            }
                            catch { }
                            if (lsSourceFolder.ToLower() == "programs")
                            {
                                lsSourceFolder = "";
                            }
                            logger.Info("sourcefolder is " + lsSourceFolder, GlobalClass.SECTION);
                            if (lsSourceFolder != "")
                            {
                                lsNewTarget = lsNewTarget + @"\" + lsSourceFolder;
                            }
                            try
                            {
                                logger.Info("creating directory " + lsNewTarget, GlobalClass.SECTION);
                                CreateDirectory(lsNewTarget);
                            }
                            catch { }
                            try
                            {

                                logger.Info("copying '" + lsFile + "' to '" + Environment.ExpandEnvironmentVariables(lsNewTarget + @"\" + System.IO.Path.GetFileName(lsFile)) + "'", GlobalClass.SECTION);
                                System.IO.File.Copy(lsFile, Environment.ExpandEnvironmentVariables(lsNewTarget + @"\" + System.IO.Path.GetFileName(lsFile)));
                            }
                            catch { }
                            try
                            {
                                System.IO.File.Delete(lsFile);
                            }
                            catch { }
                        }


                    }
                }
            }
            catch { }


            CleanEmptyStartMenuFolders();

            return true;
        }

        public void CleanEmptyStartMenuFolders()
        {
            try
            {
                DirectoryInfo loDirectoryInfo = new System.IO.DirectoryInfo(Environment.ExpandEnvironmentVariables(@"%ProgramData%\Microsoft\Windows\Start Menu"));
                RecursiveDelete(loDirectoryInfo);
            }
            catch { }
        }

        public void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }
            if (CheckFolderEmpty(baseDir.FullName))
            {
                baseDir.Delete(true);
            }
        }

        public bool WriteIni(string lsSection, string lsKey, string lsValue, string lsFileName)
        {
            string lsRealFileName = System.Environment.ExpandEnvironmentVariables(lsFileName);

            try
            {
                var MyIni = new IniFile(lsRealFileName);

                if (!MyIni.KeyExists(lsKey, lsSection))
                {
                    MyIni.Write(lsKey, lsValue, lsSection);
                }
            }
            catch { }

            return true;
        }

        public bool AddText(string lsPath, string lsValue)
        {

            lsPath = System.Environment.ExpandEnvironmentVariables(lsPath);

            if (lsValue != "")
            {
                string[] lsLines = { "" };
                try
                {
                    lsLines = System.IO.File.ReadAllLines(lsPath);
                }
                catch { }

                try
                {
                    System.IO.File.Delete(lsPath);
                }
                catch { }

                try
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(lsPath))
                    {
                        foreach (string line in lsLines)
                        {
                            if (!line.ToLower().Contains(lsValue))
                            {
                                file.WriteLine(line);
                            }
                        }
                        file.WriteLine(lsValue);
                    }
                }
                catch { }
            }

            return true;
        }

        public bool RemoveExistingSoftware(string lsUninstallType, string lsValue)
        {
            logger.Info("removeexistingproduct " + lsUninstallType + " " + lsValue, GlobalClass.SECTION);

            switch (lsUninstallType.ToLower())
            {
                case "productcode":
                    {
                        string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                        try
                        {
                            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                            foreach (string lsSubKey in regKey.GetSubKeyNames())
                            {
                                if (lsSubKey.ToLower() == lsValue.ToLower())
                                {
                                    string lsGenerateUninstallKey = "";
                                    try
                                    {
                                        RegistryKey subregKey = Registry.LocalMachine.OpenSubKey(path + @"\" + lsSubKey);
                                        lsGenerateUninstallKey = subregKey.GetValue("GeneratedUninstallKeys").ToString();
                                    }
                                    catch { }
                                    if (lsGenerateUninstallKey != "")
                                    {
                                        logger.Info("removeexistingproduct found " + lsGenerateUninstallKey, GlobalClass.SECTION);

                                        foreach (string lsItem in lsGenerateUninstallKey.Split(Convert.ToChar("|")))
                                        {
                                            NowUninstallSoftware(lsItem);
                                        }
                                    }
                                    else
                                    {
                                        NowUninstallSoftware(lsSubKey);
                                    }
                                }
                            }
                        }
                        catch { }
                        path = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                        try
                        {
                            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                            foreach (string lsSubKey in regKey.GetSubKeyNames())
                            {
                                if (lsSubKey.ToLower() == lsValue.ToLower())
                                {
                                    string lsGenerateUninstallKey = "";
                                    try
                                    {
                                        RegistryKey subregKey = Registry.LocalMachine.OpenSubKey(path + @"\" + lsSubKey);
                                        lsGenerateUninstallKey = subregKey.GetValue("GeneratedUninstallKeys").ToString();
                                    }
                                    catch { }
                                    if (lsGenerateUninstallKey != "")
                                    {
                                        foreach (string lsItem in lsGenerateUninstallKey.Split(Convert.ToChar("|")))
                                        {
                                            NowUninstallSoftware(lsItem);
                                        }
                                    }
                                    else
                                    {
                                        NowUninstallSoftware(lsSubKey);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    break;                
                case "machinekeyname":
                case "displayname":
                    {
                        string searchforname = "DisplayName";
                        if(lsUninstallType.ToLower()=="machinekeyname")
                        {
                            searchforname="MachineKeyName";
                        }
                        string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                        try
                        {
                            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                            foreach (string lsSubKey in regKey.GetSubKeyNames())
                            {
                                try
                                {
                                    string lsDisplayName = "";
                                    RegistryKey subKey = Registry.LocalMachine.OpenSubKey(path + @"\" + lsSubKey);
                                    lsDisplayName = subKey.GetValue(searchforname).ToString();
                                    if (lsDisplayName != "")
                                    {
                                        lsValue = lsValue.Replace("*", "");
                                        if (lsDisplayName.ToLower().Contains(lsValue.ToLower()))
                                        {
                                            NowUninstallSoftware(lsSubKey);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                        path = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                        try
                        {
                            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                            foreach (string lsSubKey in regKey.GetSubKeyNames())
                            {
                                try
                                {
                                    string lsDisplayName = "";
                                    RegistryKey subKey = Registry.LocalMachine.OpenSubKey(path + @"\" + lsSubKey);
                                    lsDisplayName = subKey.GetValue(searchforname).ToString();
                                    if (lsDisplayName != "")
                                    {
                                        lsValue = lsValue.Replace("*", "");
                                        if (lsDisplayName.ToLower().Contains(lsValue.ToLower()))
                                        {
                                            NowUninstallSoftware(lsSubKey);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                    break;
                case "upgradecode":
                    {
                        try
                        {
                            string lsUpgradeCode = ConvertProductCodeToInstaller(lsValue);
                            string path = @"Installer\UpgradeCodes";
                            try
                            {
                                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(path);
                                foreach (string lsSubKey in regKey.GetSubKeyNames())
                                {
                                    if (lsSubKey.ToLower() == lsUpgradeCode.ToLower())
                                    {
                                        try
                                        {
                                            RegistryKey subregKey = Registry.ClassesRoot.OpenSubKey(@"Installer\UpgradeCodes\" + lsSubKey);
                                            try
                                            {
                                                foreach (string lsInstallerKey in subregKey.GetValueNames())
                                                {
                                                    NowUninstallSoftware(ConvertInstallerToProductCode(lsInstallerKey));
                                                }
                                            }
                                            catch { }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }

                        }
                        catch { }

                    }

                    break;
            }

            return true;
        }


        private bool NowUninstallSoftware(string lsUninstallKey)
        {

            string lsCurrentPath = "";
            string lsUninstallString = "";
            string lsProductCode = "";
            string lsMachineKeyName = "";

            logger.Warn("uninstalling " + lsUninstallKey, GlobalClass.SECTION);

            string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            try
            {
                RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                foreach (string lsSubKey in regKey.GetSubKeyNames())
                {
                    if (lsSubKey.ToLower() == lsUninstallKey.ToLower())
                    {

                        RegistryKey subregKey = Registry.LocalMachine.OpenSubKey(path + @"\" + lsSubKey);
                        try
                        {
                            lsUninstallString = subregKey.GetValue("QuietUninstallString").ToString();
                            lsProductCode = lsSubKey;
                            lsCurrentPath = path;
                            try
                            {
                                lsMachineKeyName =  subregKey.GetValue("MachineKeyName").ToString();
                            }
                            catch { }
                        }
                        catch { }
                        
                        if (lsUninstallString == "")
                        {
                            lsUninstallString = subregKey.GetValue("UninstallString").ToString();
                            lsProductCode = lsSubKey;
                            lsCurrentPath = path;
                            try
                            {
                                lsMachineKeyName =  subregKey.GetValue("MachineKeyName").ToString();
                            }
                            catch { }
                        }

                    
                    }
                }
            }
            catch { }

            if (lsUninstallString == "")
            {
                path = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                try
                {
                    RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                    foreach (string lsSubKey in regKey.GetSubKeyNames())
                    {
                        if (lsSubKey.ToLower() == lsUninstallKey.ToLower())
                        {
                            RegistryKey subregKey = Registry.LocalMachine.OpenSubKey(path + @"\" + lsSubKey);
                            try
                            {
                                lsUninstallString = subregKey.GetValue("QuietUninstallString").ToString();
                                lsProductCode = lsSubKey;
                                lsCurrentPath = path;
                                try
                                {
                                    lsMachineKeyName =  subregKey.GetValue("MachineKeyName").ToString();
                                }
                                catch { }
                            }
                            catch { }
                            if (lsUninstallString == "")
                            {
                                lsUninstallString = subregKey.GetValue("UninstallString").ToString();
                                lsProductCode = lsSubKey;
                                lsCurrentPath = path;
                                try
                                {
                                    lsMachineKeyName =  subregKey.GetValue("MachineKeyName").ToString();
                                }
                                catch { }
                            }
                         
                        }
                    }
                }
                catch { }
            }

            if (lsUninstallString != "")
            {
                bool lbFoundSomething = false;

                if (lsUninstallString.ToLower().Contains("msiexec") && lsProductCode.Length == 38)
                {
                    string lsErrorMessage = "";
                    ExecuteCMD("msiexec", "/x " + lsProductCode + " /q REBOOT=ReallySuppress", ref lsErrorMessage, "true", new DataTable(), new DataTable());
                    lbFoundSomething=true;
                }

                if(lsMachineKeyName != "")
                {
                    logger.Info("Found Empirum Installation", GlobalClass.SECTION);
                    string lsFolderToDelete = Environment.ExpandEnvironmentVariables("%ProgramData%") + @"\" + lsMachineKeyName;
                    logger.Info(@"Must delete " + lsFolderToDelete, GlobalClass.SECTION);
                    try
                    {
                        if (System.IO.Directory.Exists(lsFolderToDelete))
                        {
                            System.IO.Directory.Delete(lsFolderToDelete, true);
                        }
                    }
                    catch (Exception ex) {

                        logger.Error(@"Could not delete " + lsFolderToDelete + " " + ex.Message, GlobalClass.SECTION);
                    }

                    lsFolderToDelete = lsFolderToDelete.Replace("$Matrix42Packages", "$Matrix42Scripts");
                    logger.Info(@"Must delete " + lsFolderToDelete, GlobalClass.SECTION);
                    try
                    {
                        if (System.IO.Directory.Exists(lsFolderToDelete))
                        {
                            System.IO.Directory.Delete(lsFolderToDelete, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(@"Could not delete " + lsFolderToDelete + " " + ex.Message, GlobalClass.SECTION);
                    }

                    logger.Info(@"Must delete " + lsCurrentPath + @"\" + lsUninstallKey, GlobalClass.SECTION);
                    try
                    {
                        using (RegistryKey explorerKey =
                            Registry.LocalMachine.OpenSubKey(lsCurrentPath, writable: true))
                        {
                            if(explorerKey != null)
                            {
                                explorerKey.DeleteSubKeyTree(lsUninstallKey);
                            }
                        }
                    }
                    catch (Exception ex) {
                        logger.Error("Could not delete " + lsCurrentPath + @"\" + lsUninstallKey + " " + ex.Message, GlobalClass.SECTION);
                    }
                    lbFoundSomething=true;
                }

                if (!lbFoundSomething)
                {
                    if (lsUninstallString.ToLower().Contains(".exe"))
                    {
                        if (lsUninstallString.Contains(" "))
                        {
                            if (!lsUninstallString.ToLower().Contains(@".exe"""))
                            {
                                lsUninstallString = @"""" + lsUninstallString + @"""";
                            }
                        }
                        bool lbIsNullSoft = false;
                        if(lsUninstallString.ToLower().Contains("/s"))
                        {
                            lbIsNullSoft= true;
                        }                        
                        if (!lsUninstallString.ToLower().Contains("/"))
                        {
                            lsUninstallString = lsUninstallString + " /S";
                            lbIsNullSoft= true;
                        }

                        if (lbIsNullSoft)
                        {
                            logger.Info("Detected Nullsoft uninstaller", GlobalClass.SECTION);


                            Process consoleProcess = new Process();
                            consoleProcess.StartInfo.FileName = "cmd.exe";
                            consoleProcess.StartInfo.Arguments = @"/c """ + lsUninstallString + @"""";
                            consoleProcess.StartInfo.UseShellExecute = true;
                            consoleProcess.StartInfo.CreateNoWindow = true;
                            consoleProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                            logger.Info("Running " + lsUninstallString, GlobalClass.SECTION);

                            consoleProcess.Start();
                            consoleProcess.WaitForExit();

                            logger.Info("Finished Nullsoft uninstaller", GlobalClass.SECTION);
                        }
                    }
                }

                //logger.Warn(lsUninstallString, GlobalClass.SECTION);
            }

            return true;
        }

        public static string ConvertProductCodeToInstaller(string lsValue)
        {
            string lsUpgradeCode = "";
            try
            {
                lsUpgradeCode = Reverse(lsValue.ToUpper().Substring(1, 8));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(10, 4));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(15, 4));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(20, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(22, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(25, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(27, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(29, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(31, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(33, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(35, 2));
            }
            catch { }
            return lsUpgradeCode;
        }

        public static string ConvertInstallerToProductCode(string lsValue)
        {
            string lsUpgradeCode = "";
            try
            {
                lsUpgradeCode = "{" + Reverse(lsValue.ToUpper().Substring(0, 8)) + "-";
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(8, 4)) + "-";
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(12, 4)) + "-";
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(16, 2)) + Reverse(lsValue.ToUpper().Substring(18, 2)) + "-";

                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(20, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(22, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(24, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(26, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(28, 2));
                lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(30, 2));

                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(10, 4));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(15, 4));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(20, 2));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(22, 2));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(25, 2));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(27, 2));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(29, 2));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(31, 2));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(33, 2));
                //lsUpgradeCode += Reverse(lsValue.ToUpper().Substring(35, 2));
                lsUpgradeCode += "}";
            }
            catch { }
            return lsUpgradeCode;
        }
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public bool RemoveText(string lsPath, string lsValue, string lsAggressive)
        {
            lsPath = System.Environment.ExpandEnvironmentVariables(lsPath);

            if (lsValue != "")
            {

                string[] lsLines = { "" };
                try
                {
                    lsLines = System.IO.File.ReadAllLines(lsPath);
                }
                catch { }

                try
                {
                    System.IO.File.Delete(lsPath);
                }
                catch { }

                try
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(lsPath))
                    {
                        foreach (string line in lsLines)
                        {
                            // If the line doesn't contain the word 'Second', write the line to the file.
                            if (!line.ToLower().Contains(lsValue))
                            {
                                file.WriteLine(line);
                            }
                        }
                    }
                }
                catch { }
            }

            return true;
        }

        public bool SendKeys(string lsKeys, string lsCtrlProcess, string lsCtrlId)
        {
            bool lbReturn = true;
            if (lsCtrlProcess != "")
            {
                try
                {
                    lbReturn = SendWindow("activate", lsCtrlProcess, "");
                }
                catch { }
            }

            if (lbReturn)
            {
                System.Windows.Forms.SendKeys.SendWait(lsKeys);
                Thread.Sleep(1000);
            }

            return lbReturn;
        }

        public bool SendWindow(string lsCommand, string lsCtrlProcess, string lsCtrlId)
        {
            bool lbReturn = false;
            //set foreground window
            //get the window handle

            //trying to find the process
            var prc = Process.GetProcessesByName(lsCtrlProcess);
            Int32 liHWnd = 0;

            int liCounter = 0;
            try
            {
                do
                {
                    prc = Process.GetProcessesByName(lsCtrlProcess);
                    if (prc.Length > 0)
                    {
                        foreach (Process loProcess in prc)
                        {
                            logger.Info("found process " + loProcess.ProcessName + " - " + loProcess.Id.ToString(), GlobalClass.SECTION);
                            try
                            {
                                liHWnd = Convert.ToInt32(loProcess.MainWindowHandle.ToString());
                                if (liHWnd > 0) goto WindowFound;
                            }
                            catch { }
                        }
                    }

                    Thread.Sleep(500);
                    liCounter += 1;
                    logger.Warn("waiting for window for process: " + lsCtrlId + " (" + liCounter.ToString() + "/50)", GlobalClass.SECTION);
                } while (liCounter <= 50);
            }
            catch { }

WindowFound:
            if (liHWnd > 0)
            {
                logger.Info("found process " + lsCtrlProcess + " - " + liHWnd.ToString(), GlobalClass.SECTION);

                lbReturn = true;

                switch (lsCommand.ToLower())
                {
                    case "maximize":
                        lbReturn = ActivateWindow(new IntPtr(liHWnd), "maximize");
                        break;
                    case "minimize":
                        lbReturn = ActivateWindow(new IntPtr(liHWnd), "minimize");
                        break;
                    case "normal":
                        lbReturn = ActivateWindow(new IntPtr(liHWnd), "normal");
                        break;
                    default:
                        lbReturn = ActivateWindow(new IntPtr(liHWnd), "");
                        break;
                }
            }
            else
            {
                logger.Info("could not find process " + lsCtrlProcess, GlobalClass.SECTION);
            }

            return lbReturn;
        }




        public bool ActivateWindow(IntPtr mainWindowHandle, string lsAction)
        {
            bool lbReturn = false;

            int ALT = 0xA4;
            uint EXTENDEDKEY = 0x1;
            uint KEYUP = 0x2;
            int SHOW_MAXIMIZED = 3;
            int SHOW_MINIMIZED = 6;

            try
            {
                AllowSetForegroundWindow(mainWindowHandle);
            }
            catch { }

            logger.Info("checking if ActivateWindow " + mainWindowHandle.ToString() + " has focus", GlobalClass.SECTION);

            logger.Info("active window title: " + GetActiveWindowTitle(), GlobalClass.SECTION);

            // Guard: check if window already has focus.
            if (mainWindowHandle == GetForegroundWindow())
            {
                logger.Info("focus is true", GlobalClass.SECTION);
                lbReturn = true;
            }
            else
            {
                logger.Info("mainwindowhandle (" + mainWindowHandle.ToString() + ") is not GetForeGroundWindow (" + GetForegroundWindow().ToString() + ")", GlobalClass.SECTION);
                if (IsIconic(mainWindowHandle) == true)
                {
                    ShowWindow(mainWindowHandle, 9);
                    logger.Info("isiconic is true", GlobalClass.SECTION);
                    lbReturn = true;
                }
                else
                {
                    SetForegroundWindow(mainWindowHandle);

                    //get 'parent' window
                    int run = 0;
                    IntPtr foregroundWindow = IntPtr.Zero;
                    while (foregroundWindow==IntPtr.Zero && run++<10) {
                        foregroundWindow=GetForegroundWindow();
                        Thread.Sleep(100);
                    }
                    //SetForegroundWindow(foregroundWindow);
                    ShowWindow(foregroundWindow, 5);

                    uint mainProcId = 0;
                    uint mainThread = GetWindowThreadProcessId(mainWindowHandle, out mainProcId);
                    uint foregroundProcId = 0;
                    uint foregroundThread = GetWindowThreadProcessId(foregroundWindow, out foregroundProcId);
                    string mainProc = Process.GetProcessById((int)mainProcId).ProcessName;
                    string foregroundProc = Process.GetProcessById((int)foregroundProcId).ProcessName;

                    logger.Info("SetForegroundWindow - mainwindowhandle:" + mainWindowHandle.ToString() + " foregroundWindow:" + foregroundWindow.ToString() + " mainProc:'"+mainProc+"' foregroundProc:'"+foregroundProc+"'", GlobalClass.SECTION);
                    //logger.Info("active window title: " + GetActiveWindowTitle(), GlobalClass.SECTION);

                    if (mainProc == foregroundProc) {
                        mainWindowHandle=foregroundWindow; //use 'parent' window
                        lbReturn = true;
                    }

                    /* alt-tab stuff
                    int liCounter = 0;
                    do
                    {
                        liCounter += 1;
                        logger.Info("sending alt-tab (" + liCounter.ToString() + ")", GlobalClass.SECTION);
                        System.Windows.Forms.SendKeys.SendWait("%{TAB}");
                        Thread.Sleep(1000);
                        logger.Info("checking mainwindowhandle (" + mainWindowHandle.ToString() + ") is GetForeGroundWindow (" + GetForegroundWindow().ToString() + ")", GlobalClass.SECTION);
                        logger.Info("active window title: " + GetActiveWindowTitle(), GlobalClass.SECTION);
                        if (mainWindowHandle == GetForegroundWindow())
                        {
                            lbReturn = true;
                            break;
                        }
                    } while (liCounter <= 10);
                */
                }
            }

            // Show window in forground.
            //SetForegroundWindow(mainWindowHandle);

            try
            {
                if (lsAction.ToString() == "maximize")
                {
                    // Show window maximized.
                    ShowWindow(mainWindowHandle, SHOW_MAXIMIZED);
                    // Simulate an "ALT" key press.
                    keybd_event((byte)ALT, 0x45, EXTENDEDKEY | 0, 0);
                    // Simulate an "ALT" key release.
                    keybd_event((byte)ALT, 0x45, EXTENDEDKEY | KEYUP, 0);
                }
                else if (lsAction.ToString() == "minimize")
                {
                    // Show window maximized.
                    ShowWindow(mainWindowHandle, SHOW_MINIMIZED);
                }
                else if (lsAction.ToString() == "normal")
                {
                    // Show window normal.
                    ShowWindow(mainWindowHandle, SW_SHOWNORMAL);
                }
            }
            catch { }

            //System.Threading.Thread.Sleep(200);
            //SetForegroundWindow(mainWindowHandle);
            logger.Info("exiting activatewindow", GlobalClass.SECTION);
            return lbReturn;
        }

        public bool SendMouse(string lsCommand, string lsX, string lsY, string lspositiontype, string lsXEnd, string lsYEnd, string lsCtrlProcess, string lsCtrlId, string lsShowMove)
        {
            logger.Info("sendmouse " + lsCommand + ", " + lsX + ", " + lsY + ", " + lspositiontype + ", " + lsXEnd + ", " + lsYEnd + ", " + lsCtrlProcess + ", " + lsCtrlId + ", " + lsShowMove, GlobalClass.SECTION);

            Point startCoords = new Point();
            Point endCoords = new Point();

            //translate coords
            if (lspositiontype == "window")
            {
                startCoords = getControlCoords(lsCtrlProcess, "", lsX, lsY);
                endCoords = getControlCoords(lsCtrlProcess, "", lsXEnd, lsYEnd);
            }
            else if (lspositiontype == "control")
            {
                startCoords = getControlCoords(lsCtrlProcess, lsCtrlId, lsX, lsY);
                endCoords = getControlCoords(lsCtrlProcess, lsCtrlId, lsXEnd, lsYEnd);
            }
            else //lspositiontype == "screen"
            {
                startCoords = new Point(int.Parse(lsX), int.Parse(lsY));
                if (lsXEnd != "") endCoords = new Point(int.Parse(lsXEnd), int.Parse(lsYEnd));
            }

            if (startCoords.IsEmpty)
            {
                logger.Warn("could not find startCoords", GlobalClass.SECTION);
                return false;
            }

            logger.Info("moveMouse " + startCoords.ToString(), GlobalClass.SECTION);
            moveMouse(startCoords, lsShowMove == "true");

            lsCommand = lsCommand.ToLower();

            //LEFTDOWN = 0x02; LEFTUP = 0x04; RIGHTDOWN = 0x08; RIGHTUP = 0x10;
            uint mouseCmd = 0x02;
            if (lsCommand.Contains("right")) mouseCmd = 0x08;

            if (lsCommand.Contains("double"))
                mouse_event(mouseCmd * 3, 0, 0, 0, UIntPtr.Zero); //click
            if (lsCommand.Contains("click"))
                mouse_event(mouseCmd * 3, 0, 0, 0, UIntPtr.Zero); //click
            else if (lsCommand.Contains("drag"))
            {
                if (endCoords.IsEmpty)
                {
                    logger.Warn("could not find endCoords for drag", GlobalClass.SECTION);
                    return false;
                }
                mouse_event(mouseCmd, 0, 0, 0, UIntPtr.Zero);  //down
                moveMouse(endCoords, true);
                mouse_event(mouseCmd * 2, 0, 0, 0, UIntPtr.Zero);  //up
            }

            /* RAW cmds
             bool showMouse = true;
             Point mouseDownCoords = new Point();

            if (lsCommand.ToLower().Contains("down") || lsCommand.ToLower().Contains("up"))
            {
                try
                {
                    mouseDownCoords.X = int.Parse(lsX);
                    mouseDownCoords.Y = int.Parse(lsY);
                }
                catch { }
            }            

            if (lsCtrlId != "")
            {
                Point ctrlCoords = getControlCoords(lsCtrlProcess, lsCtrlId, lsX, lsY);
                if (ctrlCoords.IsEmpty)
                {
                    logger.Warn("could not find cords for process " + lsCtrlProcess + ": " + lsCtrlId, GlobalClass.SECTION);
                    try
                    {
                        moveMouse(new Point(int.Parse(lsX), int.Parse(lsY)));
                    }
                    catch { }
                }
                else
                {
                    logger.Info("found the cords for this control", GlobalClass.SECTION);
                    moveMouse(ctrlCoords);
                }
            }
            else
            {
                try
                {
                    moveMouse(new Point(int.Parse(lsX), int.Parse(lsY)));
                }
                catch { }
            }


            uint liCommand = 0x00000002;
            switch (lsCommand.ToLower())
            {
                case "leftdown":
                    liCommand = 0x00000002;
                    break;
                case "leftup":
                    liCommand = 0x00000004;
                    break;
                default:
                    liCommand = 0x00000000;
                    break;
            }

            try
            {
                if (liCommand.ToString() != "0")
                {
                    logger.Info("sending mousevent " + lsCommand + " - " + liCommand.ToString(), GlobalClass.SECTION);
                    mouse_event(liCommand, 0, 0, 0, UIntPtr.Zero);
                }
            }
            catch { }
            */
            return true;
        }


        public bool SetResolution(string lsResolution)
        {
            logger.Info("setresolution " + lsResolution, GlobalClass.SECTION);

            try
            {
                //if originalResolution is "empty" save original resolution
                if (originalResolution.dmPelsWidth == 0)
                    ResolutionNative.EnumDisplaySettings(null, ResolutionNative.ENUM_CURRENT_SETTINGS, ref originalResolution);

                if (lsResolution == "original")
                {
                    var result = ResolutionNative.ChangeDisplaySettings(ref originalResolution, 0);
                }
                else
                {
                    string[] resArr = lsResolution.Split('x');

                    ResolutionNative.DEVMODE mode = new ResolutionNative.DEVMODE();
                    ResolutionNative.EnumDisplaySettings(null, ResolutionNative.ENUM_CURRENT_SETTINGS, ref mode);
                    mode.Initialize();

                    mode.dmPelsWidth = (uint)Int32.Parse(resArr[0]);
                    mode.dmPelsHeight = (uint)Int32.Parse(resArr[1]);
                    /*
                    mode.dmDisplayOrientation = (uint)0;
                    mode.dmBitsPerPel = (uint)32;
                    mode.dmDisplayFrequency = (uint)60;
                    */
                    //var result = ResolutionNative.ChangeDisplaySettings(ref mode, 0);

                    try
                    {
                        logger.Info("testing displaychange to " + mode.dmPelsWidth.ToString() + "x" + mode.dmPelsHeight.ToString(), GlobalClass.SECTION);
                    }
                    catch { }

                    int iRet = ResolutionNative.ChangeDisplaySettings(ref mode, CDS_TEST);

                    logger.Info("testing return " + iRet.ToString(), GlobalClass.SECTION);


                    if (iRet == DISP_CHANGE_FAILED)
                    {
                        logger.Error("Unable To change resolution Your Request. Sorry For This Inconvenience.", GlobalClass.SECTION);

                    }
                    else
                    {
                        iRet = ResolutionNative.ChangeDisplaySettings(ref mode, CDS_UPDATEREGISTRY);
                    }
                }
            }
            catch (Exception ex) { logger.Error("Error while changing resolution: " + ex.Message, GlobalClass.SECTION); }
            return true;
        }


        public bool ReplaceText(string lsPath, string lsSource, string lsDestination)
        {
            lsPath = System.Environment.ExpandEnvironmentVariables(lsPath);

            if (lsSource != "")
            {

                try
                {

                    string text = File.ReadAllText(lsPath);
                    text = Regex.Replace(text, lsSource, lsDestination, RegexOptions.IgnoreCase);
                    File.WriteAllText(lsPath, text);
                }
                catch { }

                //string[] lsLines = { "" };
                //try
                //{
                //    lsLines = System.IO.File.ReadAllLines(lsPath);
                //}
                //catch { }

                //try
                //{
                //    System.IO.File.Delete(lsPath);
                //}
                //catch { }

                //try
                //{
                //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(lsPath))
                //    {
                //        foreach (string line in lsLines)
                //        {
                //           file.WriteLine(Regex.Replace(line, lsSource, lsDestination, RegexOptions.IgnoreCase));
                //        }
                //    }
                //}
                //catch { }
            }

            return true;
        }

        private string GetKnownSIDUser(string lsAccount)
        {
            string lsReturn = lsAccount;
            switch (lsAccount.ToLower())
            {
                case "everyone":
                    {
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
                        System.Security.Principal.NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;
                        lsReturn = acct.ToString();
                    }
                    break;
                case "users":
                    {
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinUsersSid, null);
                        System.Security.Principal.NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;
                        lsReturn = acct.ToString();
                    }
                    break;
                case "authenticated users":
                    {
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.AuthenticatedUserSid, null);
                        System.Security.Principal.NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;
                        lsReturn = acct.ToString();
                    }
                    break;
                case "administrators":
                    {
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null);
                        System.Security.Principal.NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;
                        lsReturn = acct.ToString();
                    }
                    break;
                case "administrator":
                    {
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.AccountAdministratorSid, null);
                        System.Security.Principal.NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;
                        lsReturn = acct.ToString();
                    }
                    break;
                case "power users":
                    {
                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinPowerUsersSid, null);
                        System.Security.Principal.NTAccount acct = sid.Translate(typeof(System.Security.Principal.NTAccount)) as System.Security.Principal.NTAccount;
                        lsReturn = acct.ToString();
                    }
                    break;
            }
            return lsReturn;
        }



        public bool AddFilePermission(string lsPath, string lsAccount, string lsPermission, bool lbReplace)
        {
            bool lbReturn = true;

            lsPath = Environment.ExpandEnvironmentVariables(lsPath);
            string lsKnownUser = GetKnownSIDUser(lsAccount);


            logger.Info("Setting file permissions for " + lsPermission, GlobalClass.SECTION);
            logger.Info("Known users " + lsKnownUser, GlobalClass.SECTION);

            System.Security.AccessControl.FileSystemRights loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile | System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles | System.Security.AccessControl.FileSystemRights.Modify | System.Security.AccessControl.FileSystemRights.Delete | System.Security.AccessControl.FileSystemRights.FullControl;
            switch (lsPermission.ToLower())
            {
                case "read":
                    loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile;
                    break;
                case "write":
                    loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile | System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles;
                    break;
                case "modify":
                    loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile | System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles | System.Security.AccessControl.FileSystemRights.Modify | System.Security.AccessControl.FileSystemRights.Delete | System.Security.AccessControl.FileSystemRights.Modify;
                    break;
            }

            bool lbIsFolder = false;
            if (System.IO.Directory.Exists(lsPath))
            {
                lbIsFolder = true;
            }

            try
            {
                if (lbIsFolder)
                {

                    logger.Info("Setting folder permissions: " + lsPath, GlobalClass.SECTION);

                    try
                    {
                        DirectoryInfo dInfo = new DirectoryInfo(lsPath);
                        System.Security.AccessControl.DirectorySecurity fSecurity = dInfo.GetAccessControl();
                        fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, System.Security.AccessControl.AccessControlType.Allow));
                        fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, InheritanceFlags.ContainerInherit, PropagationFlags.InheritOnly, System.Security.AccessControl.AccessControlType.Allow));
                        fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, System.Security.AccessControl.AccessControlType.Allow));
                        dInfo.SetAccessControl(fSecurity);
                    }
                    catch (Exception ex) {
                        logger.Error("Error: " + ex.Message, GlobalClass.SECTION);
                        lbReturn=false;
                    }
                }
                else
                {
                    logger.Info("Setting file permissions: " + lsPath, GlobalClass.SECTION);

                    try { 
                        System.Security.AccessControl.FileSecurity fSecurity = File.GetAccessControl(lsPath);
                        fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, System.Security.AccessControl.AccessControlType.Allow));
                        File.SetAccessControl(lsPath, fSecurity);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error: " + ex.Message, GlobalClass.SECTION);
                        lbReturn=false;
                    }

                }
            }
            catch {
                lbReturn = false;
            }

            return lbReturn;
        }

        public bool RemoveFilePermission(string lsPath, string lsAccount, string lsPermission, bool lbReplace)
        {
            bool lbReturn = true;

            lsPath = Environment.ExpandEnvironmentVariables(lsPath);
            string lsKnownUser = GetKnownSIDUser(lsAccount);

            System.Security.AccessControl.FileSystemRights loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile | System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles | System.Security.AccessControl.FileSystemRights.Modify | System.Security.AccessControl.FileSystemRights.Delete | System.Security.AccessControl.FileSystemRights.FullControl;
            switch (lsPermission.ToLower())
            {
                case "read":
                    loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile;
                    break;
                case "write":
                    loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile | System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles;
                    break;
                case "modify":
                    loRights = System.Security.AccessControl.FileSystemRights.Read | System.Security.AccessControl.FileSystemRights.ExecuteFile | System.Security.AccessControl.FileSystemRights.Write | System.Security.AccessControl.FileSystemRights.CreateFiles | System.Security.AccessControl.FileSystemRights.Modify | System.Security.AccessControl.FileSystemRights.Delete | System.Security.AccessControl.FileSystemRights.Modify;
                    break;
            }

            bool lbIsFolder = false;
            if (System.IO.Directory.Exists(lsPath))
            {
                lbIsFolder = true;
            }

            try
            {
                if (lbIsFolder)
                {
                    DirectoryInfo dInfo = new DirectoryInfo(lsPath);
                    System.Security.AccessControl.DirectorySecurity fSecurity = dInfo.GetAccessControl();
                    fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, System.Security.AccessControl.AccessControlType.Deny));
                    fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, InheritanceFlags.ContainerInherit, PropagationFlags.InheritOnly, System.Security.AccessControl.AccessControlType.Deny));
                    fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, System.Security.AccessControl.AccessControlType.Deny));
                    dInfo.SetAccessControl(fSecurity);
                }
                else
                {
                    System.Security.AccessControl.FileSecurity fSecurity = File.GetAccessControl(lsPath);
                    fSecurity.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(lsKnownUser, loRights, System.Security.AccessControl.AccessControlType.Deny));
                    File.SetAccessControl(lsPath, fSecurity);
                }
            }
            catch {
                lbReturn = false;
            }

            return lbReturn;
        }



        public bool AddRegPermission(string lsPath, string lsAccount, string lsPermission, bool lbReplace)
        {
            bool lbReturn = true;

            lsPath = Environment.ExpandEnvironmentVariables(lsPath);
            string lsKnownUser = GetKnownSIDUser(lsAccount);

            RegistryRights loRights = RegistryRights.WriteKey | RegistryRights.ReadKey | RegistryRights.Delete | RegistryRights.FullControl;
            switch (lsPermission.ToLower())
            {
                case "read":
                    loRights = RegistryRights.ReadKey;
                    break;
                case "modify":
                    loRights = RegistryRights.WriteKey | RegistryRights.ReadKey;
                    break;
            }


            try
            {
                RegistrySecurity rs = new RegistrySecurity();
                rs.AddAccessRule(new RegistryAccessRule(lsKnownUser, loRights, AccessControlType.Allow));

            }
            catch
            {
                lbReturn = false;
            }

            return lbReturn;
        }

        public bool RemoveRegPermission(string lsPath, string lsAccount, string lsPermission, bool lbReplace)
        {
            bool lbReturn = true;

            lsPath = Environment.ExpandEnvironmentVariables(lsPath);
            string lsKnownUser = GetKnownSIDUser(lsAccount);

            RegistryRights loRights = RegistryRights.WriteKey | RegistryRights.ReadKey | RegistryRights.Delete | RegistryRights.FullControl;
            switch (lsPermission.ToLower())
            {
                case "read":
                    loRights = RegistryRights.ReadKey;
                    break;
                case "modify":
                    loRights = RegistryRights.WriteKey | RegistryRights.ReadKey;
                    break;
            }


            try
            {
                RegistrySecurity rs = new RegistrySecurity();
                rs.AddAccessRule(new RegistryAccessRule(lsKnownUser, loRights, AccessControlType.Deny));

            }
            catch
            {
                lbReturn = false;
            }

            return lbReturn;

        }

        public bool CreateDirectory(string lsDirectory)
        {
            lsDirectory = Environment.ExpandEnvironmentVariables(lsDirectory);
            string lsActualDirectory = "";
            foreach (string lsMyString in lsDirectory.Split(Convert.ToChar(@"\")))
            {
                lsActualDirectory += lsMyString + @"\";
                try
                {
                    System.IO.Directory.CreateDirectory(lsActualDirectory.Substring(0, lsActualDirectory.Length - 1));
                }
                catch { }
            }
            return true;
        }

        private void CopyCurrentFilesFromDirectory(string lsSource, string lsTarget, bool lbOverwrite)
        {
            foreach (string lsItem in System.IO.Directory.GetFileSystemEntries(lsSource))
            {
                if (System.IO.Directory.Exists(lsItem))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(lsTarget + @"\" + System.IO.Path.GetFileName(lsItem));
                    }
                    catch { }
                    try
                    {
                        CopyCurrentFilesFromDirectory(lsItem, lsTarget + @"\" + System.IO.Path.GetFileName(lsItem), lbOverwrite);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        System.IO.File.Copy(lsItem, lsTarget + @"\" + System.IO.Path.GetFileName(lsItem), lbOverwrite);
                    }
                    catch { }
                }
            }
        }

        public bool CopyFile(string source, string dest, string overwrite, DataTable loVarTable, DataTable loParameterTable)
        {
            bool overwriteb = false;
            bool successfullyCopied = true;
            string destpath = "";
            string filename = "";
            if (overwrite.ToLower() == "true")
            {
                overwriteb = true;
            }
            //copies file from source to dest
            try
            {
                ReplaceEnvVariables(ref source, loVarTable, loParameterTable);
                FileInfo fi = new FileInfo(source);
                ReplaceEnvVariables(ref dest, loVarTable, loParameterTable);

                //is dest a file or a folder (is the last char a '\' or not)
                if (dest.LastIndexOf("\\") + 1 == dest.Length)
                {
                    //dest is a folder, 
                    destpath = dest;
                    filename = fi.Name;
                }
                else
                {
                    destpath = dest.Substring(0, dest.LastIndexOf("\\"));
                    filename = dest.Substring(dest.LastIndexOf("\\"));
                }

                //check if it exists (or create it) and add file name to dest path
                if (!CheckFolder(destpath, "false", loVarTable, loParameterTable))
                {
                    //create folder structure
                    string[] folders = destpath.Split('\\');
                    string foldertocheckandcreate = "";
                    foreach (string folder in folders)
                    {
                        if (folder.Length > 0)
                        {
                            foldertocheckandcreate += (folder + "\\");
                            if (!CheckFolder(foldertocheckandcreate, "false", loVarTable, loParameterTable))
                            {
                                Directory.CreateDirectory(foldertocheckandcreate);
                            }
                        }
                    }
                }
                destpath += filename;

                File.Copy(source, destpath, overwriteb);
            }
            catch (Exception)
            {
                //do nothing?
                successfullyCopied = false;
            }
            return successfullyCopied;
        }

        public void SetSystemEnvironmentVariable(string name, string value)
        {
            //Creates, modifies, or deletes an environment variable stored in the current process.
            try
            {
                Environment.SetEnvironmentVariable(name, value);
            }
            catch (Exception)
            {
                //do nothing?
            }

        }

        public bool CheckServer()
        {
            return IsOS(OS_ANYSERVER);
        }

        public bool CheckOS(string name, string version, string servicepack, string languageid)
        {
            int i = -1;
            bool criterianame = false;
            bool criteriaversion = false;
            bool criteriaservicepack = false;
            bool criterialanguageid = false;

            //MessageBox.Show(System.Environment.OSVersion.ToString() + ";" + CultureInfo.CurrentCulture.LCID.ToString() + ";" + CultureInfo.CurrentCulture.EnglishName.ToString());

            try
            {
                //check version
                if (version == null)
                {
                    version = "";
                }
                if (version.Length > 0)
                {
                    i = System.Environment.OSVersion.ToString().ToUpper().IndexOf(version.ToUpper());
                    if (i >= 0)
                    {
                        //MessageBox.Show("'" + name + "' found!");
                        criteriaversion = true;
                    }
                }
                else
                {
                    criteriaversion = true;
                }

                //check servicepack
                if (servicepack == null)
                {
                    servicepack = "";
                }
                if (servicepack.Length > 0)
                {
                    servicepack = "SERVICE PACK " + servicepack;
                    i = System.Environment.OSVersion.ToString().ToUpper().IndexOf(servicepack.ToUpper());
                    if (i >= 0)
                    {
                        criteriaservicepack = true;
                    }
                }
                else
                {
                    criteriaservicepack = true;
                }

                //check language ID or english language name
                /*
                if (languageid.Length > 0)
                {
                    i = CultureInfo.CurrentCulture.LCID.ToString().IndexOf(languageid);
                    if (i >= 0)
                    {
                        criterialanguageid = true;
                    }
                    else
                    {
                        i = CultureInfo.CurrentCulture.EnglishName.ToString().ToUpper().IndexOf(languageid.ToUpper());
                        if (i >= 0)
                        {
                            criterialanguageid = true;
                        }
                    }
                }
                else
                {
                    criterialanguageid = true;
                }
                */

                //check language
                if (languageid == null)
                {
                    languageid = "";
                }
                if (languageid.Length > 0)
                {
                    ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                    ManagementObjectCollection qryCol = qry.Get();
                    foreach (ManagementObject mo in qryCol)
                    {
                        PropertyDataCollection propCol = mo.Properties;
                        foreach (PropertyData propdata in propCol)
                        {
                            if (propdata.Name.ToUpper() == "OSLANGUAGE")
                            {
                                //MessageBox.Show(propdata.Value.ToString());
                                i = propdata.Value.ToString().ToUpper().IndexOf(languageid.ToUpper());
                                if (i >= 0)
                                {
                                    criterialanguageid = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    criterialanguageid = true;
                }


                //check name
                if (name == null)
                {
                    name = "";
                }
                if (name.Length > 0)
                {
                    ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                    ManagementObjectCollection qryCol = qry.Get();
                    foreach (ManagementObject mo in qryCol)
                    {
                        PropertyDataCollection propCol = mo.Properties;
                        foreach (PropertyData propdata in propCol)
                        {
                            if (propdata.Name.ToUpper() == "CAPTION")
                            {
                                //MessageBox.Show(propdata.Value.ToString());
                                i = propdata.Value.ToString().ToUpper().Replace("(R)", "").IndexOf(name.ToUpper());
                                if (i >= 0)
                                {
                                    criterianame = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    criterianame = true;
                }
            }
            catch (Exception)
            {
                //do nothing
            }


            if (criterialanguageid && criterianame && criteriaservicepack && criteriaversion)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CheckBattery()
        {

            SystemPowerStatus SPS = new SystemPowerStatus();
            bool returnvalue = GetSystemPowerStatus(ref SPS);
            byte aclineStatus = SPS.ACLineStatus;  //(offline:0, online:1, unknown:255)
            if (aclineStatus != 1)
                return false;
            else
                return true;
        }

        public bool CheckFullscreen()
        {

            const int MONITOR_DEFAULTTONULL = 0x00000000;
            IntPtr llhWnd;
            IntPtr llhMonitor;
            RECT lpRect = new RECT();
            int countEqual = 0;

            //Monitor info
            MonitorInfo lpMi = new MonitorInfo();
            lpMi.cbSize = Marshal.SizeOf(lpMi);

            //get Forground Window
            llhWnd = GetForegroundWindow();
            if (GetWindowRect(llhWnd, out lpRect))
            {
                llhMonitor = MonitorFromRect(ref lpRect, MONITOR_DEFAULTTONULL);
                if (llhMonitor.ToInt32() != 0)
                {
                    if (GetMonitorInfo(llhMonitor, out lpMi))
                    {
                        //at least 3 values must be equal (the 4th could be different, cause of the taskbar)
                        if (lpRect.bottom == lpMi.rcWork.bottom)
                            countEqual++;
                        if (lpRect.top == lpMi.rcWork.top)
                            countEqual++;
                        if (lpRect.right == lpMi.rcWork.right)
                            countEqual++;
                        if (lpRect.left == lpMi.rcWork.left)
                            countEqual++;
                    }
                }
            }
            if (countEqual >= 3)
                return false;
            else
                return true;

        }

        //CheckDiskSpace is availalbe with 2 parameters or 3 parameters (necessary since XSD v1.5.0)
        public bool CheckDiskSpace(string driveletter, string freespace)
        {
            bool isDriveletterOK = false;
            bool isDiskSpaceOK = false;

            //check drive
            string[] logicalDrives = Directory.GetLogicalDrives();
            for (int i = 0; i < logicalDrives.Length; i++)
            {
                if (logicalDrives[i].ToUpper().IndexOf(driveletter.ToUpper()) >= 0)
                {
                    isDriveletterOK = true;
                }
            }

            if (isDriveletterOK)
            {
                //check disk space
                if (freespace.Length > 0)
                {
                    //convert to int and convert MB to Bytes
                    Int64 spacetocheck = Convert.ToInt64(freespace) * 1024 * 1024;
                    ulong freeBytesAvail;
                    ulong totalNumOfBytes;
                    ulong totalNumOfFreeBytes;

                    if (GetDiskFreeSpaceEx(driveletter + ":\\", out freeBytesAvail, out totalNumOfBytes, out totalNumOfFreeBytes))
                    {
                        if (Convert.ToInt64(freeBytesAvail) >= spacetocheck)
                        {
                            isDiskSpaceOK = true;
                        }
                    }
                }
                else
                {
                    //no need to check diskspace
                    isDiskSpaceOK = true;
                }
            }

            if (isDriveletterOK && isDiskSpaceOK)
                return true;
            else
                return false;
        }

        public bool CheckDiskSpace(string driveletter, string freespace, string physical)
        {
            bool isDriveletterOK = false;
            bool isDiskSpaceOK = false;
            bool isDiskPhyiscal = false;

            //check drive
            string[] logicalDrives = Directory.GetLogicalDrives();
            for (int i = 0; i < logicalDrives.Length; i++)
            {
                if (logicalDrives[i].ToUpper().IndexOf(driveletter.ToUpper()) >= 0)
                {
                    isDriveletterOK = true;
                }
            }

            if (physical.ToLower() == "true" || physical == "1")
            {
                DriveInfo drv = new DriveInfo(driveletter);
                if (drv.DriveType.ToString().ToLower() == "fixed")
                {
                    if (drv.IsReady)
                    {
                        isDiskPhyiscal = true;
                    }
                }
            }
            else
            {
                isDiskPhyiscal = true; //check is not necessary in this case
            }

            if (isDriveletterOK && isDiskPhyiscal)
            {
                //check disk space
                if (freespace.Length > 0)
                {
                    //convert to int and convert MB to Bytes
                    Int64 spacetocheck = Convert.ToInt64(freespace) * 1024 * 1024;
                    ulong freeBytesAvail;
                    ulong totalNumOfBytes;
                    ulong totalNumOfFreeBytes;

                    if (GetDiskFreeSpaceEx(driveletter + ":\\", out freeBytesAvail, out totalNumOfBytes, out totalNumOfFreeBytes))
                    {
                        if (Convert.ToInt64(freeBytesAvail) >= spacetocheck)
                        {
                            isDiskSpaceOK = true;
                        }
                    }
                }
                else
                {
                    //no need to check diskspace
                    isDiskSpaceOK = true;
                }
            }

            if (isDriveletterOK && isDiskSpaceOK && isDiskPhyiscal)
                return true;
            else
                return false;
        }

        public bool CheckFile(string path, DataTable loVarTable, DataTable loParameterTable)
        {

            ReplaceEnvVariables(ref path, loVarTable, loParameterTable);
            if (File.Exists(path))
                return true;
            else
                return false;

        }


        public bool CheckVariable(string var, string value, DataTable loVarTable)
        {
            bool lbReturn = false;
            logger.Info("checking variable " + var + " with value " + value, GlobalClass.SECTION);

            try
            {
                foreach (DataRow loRow in loVarTable.Rows)
                {
                    logger.Info("checking variable enum " + loRow["name"].ToString() + " with value " + loRow["value"].ToString(), GlobalClass.SECTION);

                    if (loRow["name"].ToString().ToLower() == var.ToLower())
                    {
                        logger.Info("found variable " + loRow["name"].ToString() + " with value " + loRow["value"].ToString(), GlobalClass.SECTION);
                        logger.Info("value must be " + value, GlobalClass.SECTION);
                        if (value == loRow["value"].ToString())
                        {
                            lbReturn = true;
                            break;
                        }
                        if (value.StartsWith("*"))
                        {
                            if (loRow["value"].ToString().Contains(value.Replace("*", "")))
                            {
                                lbReturn = true;
                                break;
                            }
                        }
                        if (value.EndsWith("*"))
                        {
                            if (loRow["value"].ToString().Contains(value.Replace("*", "")))
                            {
                                lbReturn = true;
                                break;
                            }
                        }
                        logger.Info("value check is " + lbReturn.ToString(), GlobalClass.SECTION);

                    }
                }
            }
            catch (Exception ex) { logger.Info(ex.Message, GlobalClass.SECTION); }
            return lbReturn;
        }

        public bool CheckFolder(string path, string contentrequired, DataTable loVarTable, DataTable loParameterTable)
        {
            ReplaceEnvVariables(ref path, loVarTable, loParameterTable);
            if (Directory.Exists(path))
            {
                //check content
                if (contentrequired.ToUpper().IndexOf("TRUE") >= 0)
                {
                    try
                    {
                        if (CheckFolderEmpty(path))
                            return false;
                        else
                            return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                else
                    return true;
            }
            else
                return false;
        }

        public bool CheckFolderEmpty(string path)
        {
            bool lbReturn = false;
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                if (dir.GetFiles().Length == 0 && dir.GetDirectories().Length == 0)
                {
                    lbReturn = true;
                }
                if (!lbReturn)
                {
                    if (dir.GetDirectories().Length == 0)
                    {
                        int liFileCount = 0;
                        foreach (FileInfo file in dir.GetFiles())
                        {
                            if (file.Name.ToLower() != "thumbs.db")
                            {
                                liFileCount += 1;
                            }
                            if (liFileCount > 0)
                            {
                                break;
                            }
                        }
                        if (liFileCount == 0)
                        {
                            lbReturn = true;
                        }
                    }
                }
            }
            catch { }
            return lbReturn;

        }



        public string ReadRegistry(string path, string name)
        {

            string lsReturn = "";

            string hive = path.ToUpper().Substring(0, 4);
            if (hive == @"HKU\")
            {
                hive = "HKU";
            }
            RegistryKey regKey = null;
            path = path.ToUpper().Replace(hive + "\\", "");

            logger.Info("reading registry: hive=" + hive + ", path=" + path, GlobalClass.SECTION);

            switch (hive)
            {
                case "HKLM":
                    regKey = Registry.LocalMachine.OpenSubKey(path);
                    break;
                case "HKCU":
                    regKey = Registry.CurrentUser.OpenSubKey(path);
                    break;
                case "HKCR":
                    regKey = Registry.ClassesRoot.OpenSubKey(path);
                    break;
                case "HKU":
                    regKey = Registry.Users.OpenSubKey(path);
                    break;
            }
            //regkey exists?
            if (regKey == null)
            {
                logger.Info("reading registry: could not find path: hive=" + hive + ", path=" + path, GlobalClass.SECTION);
                return "";
            }
            else
            {
                if (name.Length > 0)
                {
                    if (regKey.GetValue(name) == null)
                        lsReturn = "";
                    else
                    {
                        lsReturn = regKey.GetValue(name).ToString();
                    }
                }
                else
                {
                    try
                    {
                        lsReturn = regKey.GetValue(null).ToString();
                    }
                    catch { }
                }
            }
            return lsReturn;
        }
        public bool CheckRegistry(string path, string name, string value)
        {
            string hive = path.ToUpper().Substring(0, 4);
            RegistryKey regKey = null;
            path = path.ToUpper().Replace(hive + "\\", "");

            switch (hive)
            {
                case "HKLM":
                    regKey = Registry.LocalMachine.OpenSubKey(path);
                    break;
                case "HKCU":
                    regKey = Registry.CurrentUser.OpenSubKey(path);
                    break;
                case "HKCR":
                    regKey = Registry.ClassesRoot.OpenSubKey(path);
                    break;
            }
            //regkey exists?
            if (regKey == null)
                //regkey (path) does NOT exists
                return false;
            else
            {
                //name (optional) specified?
                if (name.Length > 0)
                {
                    //regname exists?
                    if (regKey.GetValue(name) == null)
                        //regname does NOT exists
                        return false;
                    else
                    {
                        //value correct?
                        if (value.Length > 0) //it's necessary to check also the value
                        {
                            if (value.ToUpper() == regKey.GetValue(name).ToString().ToUpper())
                                //value is correct
                                return true;
                            else
                                //value is NOT correct
                                return false;
                        }
                        else
                            //regname exists, NOT necessary to check also the value
                            return true;
                    }
                }
                else
                {
                    //path exists; name not defined
                    return true;
                }
            }
        }

        public bool CheckProcess(string processname)
        {

            string[] processes = processname.Split(Convert.ToChar("|"));

            //processname = processname.ToLower();

            //get all running processes on computer
            Process[] processlist = Process.GetProcesses();
            //loop through the process list

            bool returnvalue = false;
            foreach (Process runningProcess in processlist)
            {
                foreach (string process in processes)
                {
                    if (process != "")
                    {
                        string processnameWithExe = process + ".exe";
                        string processnameWithoutExe = process.Replace(".exe", "");
                        if (runningProcess.ProcessName.ToLower().Equals(process) || runningProcess.ProcessName.ToLower().Equals(processnameWithExe) || runningProcess.ProcessName.ToLower().Equals(processnameWithoutExe))
                        {
                            //running process found --> return a true
                            returnvalue = true;
                            break;
                        }
                    }
                }
            }
            //otherwise return a false
            return returnvalue;
        }

        public void KillProcess(string processname)
        {

            string[] processes = processname.Split(Convert.ToChar("|"));

            //processname = processname.ToLower().Replace(".exe", "");
            foreach (string process in processes)
            {
                if (process != "")
                {
                    process.ToLower().Replace(".exe", "");
                    if (CheckProcess(process))
                    {
                        //kill process(es)
                        Process[] processesToKill = Process.GetProcessesByName(process);
                        foreach (Process processToKill in processesToKill)
                            try
                            {
                                processToKill.Kill();
                            }
                            catch { }
                    }
                }
            }
            return;
        }

        public void KillProcessByPath(string path, DataTable loVarTable, DataTable loParameterTable)
        {
            string lsPath = path;
            ReplaceEnvVariables(ref lsPath, loVarTable, loParameterTable);

            foreach (Process myprocess in Process.GetProcesses())
            {
                string lsMainModuleFileName = "";
                try
                {
                    lsMainModuleFileName = myprocess.MainModule.FileName;
                }
                catch { }
                if (lsMainModuleFileName != "")
                {
                    if (lsMainModuleFileName.ToLower().StartsWith(lsPath.ToLower()))
                    {
                        logger.Info("killing process " + lsMainModuleFileName, GlobalClass.SECTION);
                        try
                        {
                            myprocess.Kill();
                        }
                        catch { }
                    }
                }

            }

            //string[] processes = processname.Split(Convert.ToChar("|"));

            ////processname = processname.ToLower().Replace(".exe", "");
            //foreach (string process in processes)
            //{
            //    if (process != "")
            //    {
            //        process.ToLower().Replace(".exe", "");
            //        if (CheckProcess(process))
            //        {
            //            //kill process(es)
            //            Process[] processesToKill = Process.GetProcessesByName(process);
            //            foreach (Process processToKill in processesToKill)
            //                processToKill.Kill();
            //        }
            //    }
            //}
            return;
        }

        public int AddRegKey(string path, string name, string value, string valuekind, DataTable loVarTable, DataTable loParameterTable, ref string errormsg)
        {
            //add reg key
            string hive = path.ToUpper().Substring(0, 4);
            RegistryKey regKey = null;
            //path = path.ToUpper().Replace(hive + "\\", "");
            path = path.Replace(hive + "\\", "");


            int rtvalue = 0;

            //(Default) - case
            if (name.ToLower() == "(default)")
            {
                name = "";
            }

            string newvalue = value;
            ReplaceEnvVariables(ref newvalue, loVarTable, loParameterTable);

            bool donotoverwrite = false;
            if(value.Contains("%GeneratedUninstallKeys%"))
            {
                if(newvalue=="")
                {
                    donotoverwrite=true;
                }
            }
            value = newvalue;

            if (donotoverwrite == false)
            {
                try
                {
                    switch (hive)
                    {
                        case "HKLM":
                            regKey = Registry.LocalMachine.CreateSubKey(path);
                            break;
                        case "HKCU":
                            regKey = Registry.CurrentUser.CreateSubKey(path);
                            break;
                        case "HKCR":
                            regKey = Registry.ClassesRoot.CreateSubKey(path);
                            break;
                        case "HKU":
                            regKey = Registry.Users.CreateSubKey(path);
                            break;
                    }
                    if (regKey != null)
                    {

                        switch (valuekind)
                        {
                            case "REG_DWORD":
                                regKey.SetValue(name, value, RegistryValueKind.DWord);
                                break;
                            case "REG_QWORD":
                                regKey.SetValue(name, value, RegistryValueKind.QWord);
                                break;
                            case "REG_SZ":
                                regKey.SetValue(name, value, RegistryValueKind.String);
                                break;
                            case "REG_BINARY":
                                byte[] loByte = ConvertHexStringToByteArray(value.Replace(",", ""));
                                regKey.SetValue(name, loByte, RegistryValueKind.Binary);
                                break;
                            case "REG_MULTI_SZ":
                                try
                                {
                                    string[] lsMultiString = value.Replace("[~]", "~").Split(Convert.ToChar("~"));
                                    regKey.SetValue(name, lsMultiString, RegistryValueKind.MultiString);
                                }
                                catch { }
                                break;
                            case "REG_EXPAND_SZ":
                                regKey.SetValue(name, value, RegistryValueKind.ExpandString);
                                break;
                            case "":
                                break;
                            default:
                                regKey.SetValue(name, value, RegistryValueKind.Unknown);
                                break;
                        }
                        regKey.Close();
                    }
                    rtvalue = 0;
                }
                catch (ArgumentException ex)
                {
                    errormsg = ex.Message;
                    rtvalue = 1;
                }
                catch (IOException ex)
                {
                    errormsg = ex.Message;
                    rtvalue = 1;
                }
            }
            return rtvalue;
        }

        public void RemoveRegPath(string path, string name)
        {
            //remove reg key
            string hive = path.ToUpper().Substring(0, 4);
            if (hive == @"HKU\")
            {
                hive = "HKU";
            }
            RegistryKey regKey = null;
            // path = path.ToUpper().Replace(hive + "\\", "");


            if(path.Contains("*"))
            {
                string parentpath = System.IO.Path.GetDirectoryName(path);
                string patternname = path.Substring(System.IO.Path.GetDirectoryName(path).Length + 1);

                logger.Info("parent path is " + parentpath, GlobalClass.SECTION);
                logger.Info("patternname path is " + patternname, GlobalClass.SECTION);

                try
                {
                    switch (hive)
                    {
                        case "HKLM":
                            regKey = Registry.LocalMachine.OpenSubKey(parentpath.Substring(hive.Length + 1));
                            break;
                        case "HKCU":
                            regKey = Registry.CurrentUser.OpenSubKey(parentpath.Substring(hive.Length + 1));
                            break;
                        case "HKCR":
                            regKey = Registry.ClassesRoot.OpenSubKey(parentpath.Substring(hive.Length + 1));
                            break;
                        case "HKU":
                            regKey = Registry.Users.OpenSubKey(parentpath.Substring(hive.Length + 1));
                            break;
                    }

                    try
                    {
                        foreach (string regName in regKey.GetSubKeyNames())
                        {
                            bool validRegName = true;
                            foreach (string regPart in patternname.Split(Convert.ToChar("*")))
                            {
                                if (!regName.Contains(regPart))
                                {
                                    validRegName = false;
                                }
                            }
                            if (validRegName)
                            {
                                logger.Info("deleting path " + parentpath.Substring(hive.Length + 1) + @"\" + regName, GlobalClass.SECTION);
                                try
                                {
                                    switch (hive)
                                    {
                                        case "HKLM":
                                            logger.Info("DeleteSubKeyTree path " + parentpath.Substring(hive.Length + 1) + @"\" + regName, GlobalClass.SECTION);
                                            Registry.LocalMachine.DeleteSubKeyTree(parentpath.Substring(hive.Length + 1) + @"\" + regName);
                                            break;
                                        case "HKCU":
                                            Registry.CurrentUser.DeleteSubKeyTree(parentpath.Substring(hive.Length + 1) + @"\" + regName);
                                            break;
                                        case "HKCR":
                                            Registry.ClassesRoot.DeleteSubKeyTree(parentpath.Substring(hive.Length + 1) + @"\" + regName);
                                            break;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
                catch { }

            }
            else
            {
                try
                {
                    logger.Info("remove registry path: hive=" + hive + ", path=" + path, GlobalClass.SECTION);

                    switch (hive)
                    {
                        case "HKLM":
                            Registry.LocalMachine.DeleteSubKeyTree(path);
                            break;
                        case "HKCU":
                            Registry.CurrentUser.DeleteSubKeyTree(path);
                            break;
                        case "HKCR":
                            Registry.ClassesRoot.DeleteSubKeyTree(path);
                            break;
                    }
                }
                catch { }
            }


          

            //try
            //{
            //    //name defined --> delete (only) name
            //    if (name != "")
            //    {
                  
            //    }

            //    //name NOT defined --> delete key (inkl. subkeys & names)
            //    else
            //    {
                  
            //    }
            //}
            //catch (Exception)
            //{
            //    //nothing to do
            //    return;
            //}
            return;
        }




        public void RemoveRegKey(string path, string name)
        {
            //remove reg key
            string hive = path.ToUpper().Substring(0, 4);
            RegistryKey regKey = null;
            path = path.ToUpper().Replace(hive + "\\", "");

            try
            {
                //name defined --> delete (only) name
                if (name != "")
                {
                    switch (hive)
                    {
                        case "HKLM":
                            regKey = Registry.LocalMachine.OpenSubKey(path, true);
                            break;
                        case "HKCU":
                            regKey = Registry.CurrentUser.OpenSubKey(path, true);
                            break;
                        case "HKCR":
                            regKey = Registry.ClassesRoot.OpenSubKey(path, true);
                            break;
                    }
                    if (regKey != null)
                    {
                        regKey.DeleteValue(name, false);
                        regKey.Close();
                    }
                }

                //name NOT defined --> delete key (inkl. subkeys & names)
                else
                {
                    switch (hive)
                    {
                        case "HKLM":
                            Registry.LocalMachine.DeleteSubKeyTree(path);
                            break;
                        case "HKCU":
                            Registry.CurrentUser.DeleteSubKeyTree(path);
                            break;
                        case "HKCR":
                            Registry.ClassesRoot.DeleteSubKeyTree(path);
                            break;
                    }
                }
            }
            catch (Exception)
            {
                //nothing to do
                return;
            }
            return;
        }

        public void RemoveRegKeyForAllUsers(string path, string name)
        {
            //remove reg key
            string hive = path.ToUpper().Substring(0, 4);
            path = path.ToUpper().Replace(hive + "\\", "");

            bool isUserFolder = false;
            if (hive=="HKCU")
            {
                isUserFolder = true;
            }

            if (!isUserFolder)
            {
                RemoveRegKey(path, name);
            }
            else
            {
                if (System.IO.Directory.Exists(Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\Users")))
                {
                    foreach (DirectoryInfo loDirectory in new System.IO.DirectoryInfo(Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\Users")).GetDirectories("*", SearchOption.TopDirectoryOnly))
                    {
                        if (System.IO.File.Exists(loDirectory.FullName + @"\ntuser.dat"))
                        {
                            bool lbValid = true;
                            if ((loDirectory.FullName).ToLower() == Environment.ExpandEnvironmentVariables("%PUBLIC%").ToLower())
                            {
                                lbValid = false;
                            }
                            if (lbValid)
                            {
                                string loadedHiveKey = RegistryLoad(loDirectory.FullName + @"\ntuser.dat");

                                RegistryKey rk = Registry.Users.OpenSubKey(loadedHiveKey);

                                if (rk != null)
                                {
                                    logger.Info("loaded reg file " + loDirectory.FullName + @"\ntuser.dat", GlobalClass.SECTION);

                                    try
                                    {
                                        RegistryKey ExistKey = rk.OpenSubKey(path, RegistryKeyPermissionCheck.ReadWriteSubTree);

                                        if (ExistKey != null)
                                        {
                                            if (name!="")
                                            {
                                                foreach (string lsName in ExistKey.GetValueNames())
                                                {
                                                    if (lsName.ToLower()==name.ToLower())
                                                    {
                                                        logger.Info("deleting key " + path + " width name " + name + " inside " + loDirectory.FullName + @"\ntuser.dat", GlobalClass.SECTION);
                                                        try
                                                        {
                                                            ExistKey.DeleteValue(lsName);
                                                        }
                                                        catch { }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                logger.Info("deleting path " + path + " inside " + loDirectory.FullName + @"\ntuser.dat", GlobalClass.SECTION);
                                                try
                                                {
                                                    ExistKey.Close();
                                                }
                                                catch { }
                                                string lsParentKey = "";
                                                string lsSubKey = "";
                                                int liItemCount = 0;
                                                foreach (string lsItem in path.Split(Convert.ToChar(@"\")))
                                                {
                                                    if (liItemCount < path.Split(Convert.ToChar(@"\")).Length - 1)
                                                    {
                                                        lsParentKey += lsItem + @"\";
                                                    }
                                                    else
                                                    {
                                                        lsSubKey = lsItem;
                                                    }
                                                    liItemCount += 1;
                                                }
                                                try
                                                {
                                                    lsParentKey = lsParentKey.Substring(0, lsParentKey.Length - 1);
                                                }
                                                catch { }
                                                try
                                                {
                                                    RegistryKey ExistKey2 = rk.OpenSubKey(lsParentKey, true);
                                                    foreach (string lsMyKey in ExistKey2.GetSubKeyNames())
                                                    {
                                                        if (lsMyKey.ToLower() == lsSubKey.ToLower())
                                                        {
                                                            try
                                                            {
                                                                logger.Info("deleting " + lsMyKey, GlobalClass.SECTION);
                                                                ExistKey2.DeleteSubKeyTree(lsMyKey);
                                                            }
                                                            catch (Exception ex2) { logger.Warn("error DeleteSubKeyTree " + loadedHiveKey + @"\" + lsParentKey + ":" + lsMyKey + " " + ex2.Message, GlobalClass.SECTION); }

                                                        }
                                                    }
                                                    ExistKey2.Close();
                                                }
                                                catch (Exception ex)
                                                {
                                                    logger.Warn("ex error DeleteSubKeyTree " + ex.Message, GlobalClass.SECTION);
                                                }
                                                try
                                                {
                                                    RegistryKey ExistKey2 = rk.OpenSubKey(lsParentKey, true);
                                                    foreach (string lsMyKey in ExistKey2.GetSubKeyNames())
                                                    {
                                                        if (lsMyKey.ToLower() == lsSubKey.ToLower())
                                                        {
                                                            try
                                                            {
                                                                logger.Info("deleting " + lsMyKey, GlobalClass.SECTION);
                                                                ExistKey2.DeleteSubKey(lsMyKey);
                                                            }
                                                            catch (Exception ex2) { logger.Warn("error DeleteSubKey " + loadedHiveKey + @"\" + lsParentKey + ":" + lsMyKey + " " + ex2.Message, GlobalClass.SECTION); }

                                                        }
                                                    }
                                                    ExistKey2.Close();
                                                }
                                                catch { }
                                                try
                                                {
                                                    Registry.Users.DeleteSubKeyTree(loadedHiveKey + @"\" + path);
                                                }
                                                catch (Exception exx) { logger.Warn("error direct DeleteSubKey " + loadedHiveKey + @"\" + path + " " + exx.Message, GlobalClass.SECTION); }



                                            }
                                            try
                                            {
                                                ExistKey.Close();
                                            }
                                            catch { }
                                        }
                                    }
                                    catch (Exception ex) {
                                        logger.Warn("error " + ex.Message, GlobalClass.SECTION);
                                    }

                                    try
                                    {
                                        rk.Close();
                                    }
                                    catch { }
                                }

                                RegistryUnload();

                                //RemoveFileOrFolder(loDirectory.FullName + lsPathInsideUser, loVarTable, loParameterTable);
                            }
                        }
                    }
                }
            }

            return;
        }

        public int ServiceMgr(string servicename, int function, ref string errormsg)
        {
            ServiceController service = new ServiceController(servicename);
            TimeSpan timeout = TimeSpan.FromMilliseconds(SERVICETIMEOUT);
            int rtvalue = 0;

            switch (function)
            {
                //stop service
                case 0:
                    try
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        rtvalue = 0;
                    }
                    catch (Win32Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        errormsg = ex.Message;
                        rtvalue = 11;
                    }
                    catch (InvalidOperationException ex)
                    {
                        //MessageBox.Show(ex.Message);
                        errormsg = ex.Message;
                        rtvalue = 12;
                    }
                    catch (System.ServiceProcess.TimeoutException ex)
                    {
                        //MessageBox.Show(ex.Message);
                        errormsg = ex.Message;
                        rtvalue = 13;
                    }
                    break;
                //start service
                case 1:
                    try
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                        rtvalue = 0;
                    }
                    catch (Win32Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        errormsg = ex.Message;
                        rtvalue = 21;
                    }
                    catch (InvalidOperationException ex)
                    {
                        //MessageBox.Show(ex.Message);
                        errormsg = ex.Message;
                        rtvalue = 22;
                    }
                    catch (System.ServiceProcess.TimeoutException ex)
                    {
                        //MessageBox.Show(ex.Message);
                        errormsg = ex.Message;
                        rtvalue = 23;
                    }
                    break;
            }
            return rtvalue;
        }


        public int AppInstaller(string action, string name, string filename, DataTable loVarTable, DataTable loParameterTable)
        {

            Process currentProcess = Process.GetCurrentProcess();
            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));

            logger.Info("current path is " + currentPath, GlobalClass.SECTION);

            if (filename!="")
            {
                filename = Environment.ExpandEnvironmentVariables(filename);
                if (!System.IO.File.Exists(filename))
                {
                    if(System.IO.File.Exists(currentPath + @"\" + filename))
                    {
                        filename = currentPath + @"\" + filename;
                    }
                }
            }

            switch (action)
            {
                case "install":
                    logger.Info("Looking for " + filename, GlobalClass.SECTION);
                    if (System.IO.File.Exists(filename))
                    {
                        switch(System.IO.Path.GetExtension(filename).ToLower())
                        {
                            case ".msix":
                            case ".appx":
                            case ".msixbundle":
                            case ".appxbundle":
                                string commandline = "";
                                string msixfolder = currentPath;
                                try
                                {
                                    if (filename.StartsWith(@"\\"))
                                    {
                                        msixfolder = System.IO.Path.GetDirectoryName(filename);
                                    }
                                    if (filename.Contains(":"))
                                    {
                                        msixfolder = System.IO.Path.GetDirectoryName(filename);
                                    }
                                }
                                catch { }

                                string errormessage = "";

                                logger.Info("Looking for prerequisites in " + msixfolder, GlobalClass.SECTION);
                                foreach (string prefilename in System.IO.Directory.GetFiles(msixfolder,"*.*",SearchOption.TopDirectoryOnly))
                                {
                                    if(System.IO.Path.GetExtension(prefilename.ToLower()) == ".msix" || System.IO.Path.GetExtension(prefilename.ToLower()) == ".appx")
                                    {
                                        if (System.IO.Path.GetFileName(prefilename) != System.IO.Path.GetFileName(filename))
                                        {
                                            string prefullfilename = prefilename;
                                            //commandline = @"Add-AppxPackage -Path '" + prefullfilename + @"'";
//                                            commandline = @"/online /add-ProvisionedAppxPackage /packagepath:""" + prefullfilename + @""" /skip-license";
                                            commandline = @"/Online /Add-ProvisionedAppxPackage /PackagePath:""" + prefullfilename + @""" /SkipLicense";
                                            logger.Info("Installing prerequisites: " + commandline, GlobalClass.SECTION);
                                            //logger.Info(PSExecute(commandline), GlobalClass.SECTION);
                                            ExecuteCMD("DISM.EXE", commandline, ref errormessage, "true", "hidden", loVarTable, loParameterTable);
                                        }
                                    }
                                }
                                string fullfilename = @".\" + filename;
                                if (filename.StartsWith(@"\\"))
                                {
                                    fullfilename = filename;
                                }
                                if (filename.Contains(":"))
                                {
                                    fullfilename = filename;
                                }
                                //commandline = @"Add-AppxPackage -Path '" + fullfilename + @"'";
                                //commandline = @"/online /add-ProvisionedAppxPackage /packagepath:""" + fullfilename + @""" /skip-license";
                                commandline = @"/Online /Add-ProvisionedAppxPackage /PackagePath:""" + fullfilename + @""" /SkipLicense";

                                logger.Info("Running " + commandline, GlobalClass.SECTION);
                                ExecuteCMD("DISM.EXE", commandline, ref errormessage, "true", "hidden", loVarTable, loParameterTable);
                                break;
                            default:
                                logger.Error("Unknown file type for " + filename, GlobalClass.SECTION);
                                break;
                        }
                    }
                    else
                    {
                        logger.Error("File " + filename + " not found.",GlobalClass.SECTION);
                    }
                    break;
                case "uninstall":
                    if(name!="")
                    {
                        if(name.Length>3)
                        {
                            string commandline = @"Get-AppxPackage -Name '" + name + @"' -AllUsers";
                            logger.Info("Running " + commandline, GlobalClass.SECTION);
                            string returnvalue = PSExecute(commandline);

                            string packageName = PSParseOutput(returnvalue, "PackageFullName");

                            if(packageName=="")
                            {
                                commandline = @"Get-AppxPackage -Allusers";
                                logger.Info("Running " + commandline, GlobalClass.SECTION);
                                returnvalue = PSExecute(commandline);

                                packageName = PSParseOutputs(returnvalue, "PackageFullName", name);

                            }


                            if (packageName != "")
                            {
                                commandline = @"Remove-AppxPackage -Package '" + packageName + @"'";
                                logger.Info("Running " + commandline, GlobalClass.SECTION);
                                logger.Info(PSExecute(commandline), GlobalClass.SECTION);
                            }
                        }
                    }
                    break;
                case "repair":
                    break;
            }
            return 0;

        }


        public int RemoveFileOrFolder(string path, DataTable loVarTable, DataTable loParameterTable)
        {
            ReplaceEnvVariables(ref path, loVarTable, loParameterTable);

            try
            {
                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //read-only file --> reset attribute
                        FileInfo filePath = new FileInfo(path);
                        FileAttributes attribute;
                        attribute = (FileAttributes)(filePath.Attributes - FileAttributes.ReadOnly);
                        File.SetAttributes(filePath.FullName, attribute);
                        File.Delete(path);
                    }
                }
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //use my own recursive method to reset file attribute and delete files
                        DeleteRecursiveFolder(path);
                    }
                }
            }
            catch (Exception)
            {
                //logging
                return 1;
            }
            return 0;
        }

        public int RemoveFileOrFolderForAllUsers(string path, DataTable loVarTable, DataTable loParameterTable)
        {

            string lsOriginalPath = path;

            ReplaceEnvVariables(ref path, loVarTable, loParameterTable);

            bool isUserFolder = false;
            if(path.ToLower().Contains(@"\users\"))
            {
                isUserFolder = true;
            }

            if(!isUserFolder)
            {
                RemoveFileOrFolder(path, loVarTable, loParameterTable);
            }
            else
            {
                if(System.IO.Directory.Exists(Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\Users")))
                {
                    string lsPathInsideUser = "";
                    try
                    {
                        int liCount = 0;
                        foreach (string lsPart in path.Split(Convert.ToChar(@"\")))
                        {
                            if (liCount >= 3)
                            {
                                lsPathInsideUser += @"\" + lsPart;
                            }
                            liCount += 1;
                        }
                    }
                    catch { }
                    if (lsPathInsideUser != "")
                    {
                        foreach (DirectoryInfo loDirectory in new System.IO.DirectoryInfo(Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\Users")).GetDirectories("*", SearchOption.TopDirectoryOnly))
                        {
                            bool lbValid = true;
                            if((loDirectory.FullName).ToLower()==Environment.ExpandEnvironmentVariables("%PUBLIC%").ToLower())
                            {
                                lbValid = false;
                            }
                            if (lbValid)
                            {
                                logger.Info("found file or directory " + loDirectory.FullName + lsPathInsideUser, GlobalClass.SECTION);
                                RemoveFileOrFolder(loDirectory.FullName + lsPathInsideUser, loVarTable, loParameterTable);
                            }
                        }
                    }
                }
            }
          
            return 0;
        }


        private void DeleteRecursiveFolder(string path)
        {
            try
            {
                foreach (string folder in Directory.GetDirectories(path))
                {
                    DeleteRecursiveFolder(folder);
                }

                foreach (string file in Directory.GetFiles(path))
                {
                    FileInfo fi = new FileInfo(file);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                Directory.Delete(path);
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        //ExecuteCMD is available with 4 parameters or 5 parameters (since XSD 1.5.0.0)
        public string ExecuteCMD(string cmdline, string parameters, ref string errormsg, string wait, DataTable loVarTable, DataTable loParameterTable)
        {
            Process currentProcess = Process.GetCurrentProcess();
            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));

            ReplaceEnvVariables(ref cmdline, loVarTable, loParameterTable);
            //ReplaceEnvVariables(ref parameters);
            Process consoleProcess = new Process();
            if (File.Exists(currentPath + "\\" + cmdline))
            {
                cmdline = currentPath + "\\" + cmdline;
            }
            cmdline = cmdline.Replace("%DELIVERIES_SETUP%", currentPath);
            consoleProcess.StartInfo.FileName = cmdline;
            parameters = parameters.Replace("%DELIVERIES_SETUP%", currentPath);
            consoleProcess.StartInfo.Arguments = parameters;
            consoleProcess.StartInfo.UseShellExecute = true;
            consoleProcess.StartInfo.CreateNoWindow = true;
            consoleProcess.StartInfo.WorkingDirectory = currentPath;
            consoleProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            try
            {
                consoleProcess.Start();
                if (wait == "true")
                {
                    while (consoleProcess.HasExited == false)
                    {
                        //do nothing... or maybe exit if too much time has passed...
                        Thread.Sleep(100);
                        Application.DoEvents();

                        /*
                        //set foreground window
                        //get the window handle
                        IntPtr hWnd = consoleProcess.MainWindowHandle;
                        //if iconic, we need to restore the window
                        if (IsIconic(hWnd))
                        {
                            ShowWindowAsync(hWnd, SW_RESTORE);
                        }
                        //bring it to foreground
                        SetForegroundWindow(hWnd);
                        */
                    }
                    return consoleProcess.ExitCode.ToString();
                }
                else
                {
                    return "0";
                }
            }
            catch (Win32Exception ex)
            {
                errormsg = ex.Message;
                return "1";
            }
        }

        public string ExecuteCMD(string cmdline, string parameters, ref string errormsg, string wait, string windowsstyle, DataTable loVarTable, DataTable loParameterTable)
        {
            Process currentProcess = Process.GetCurrentProcess();
            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));

            ReplaceEnvVariables(ref cmdline, loVarTable, loParameterTable);
            ReplaceEnvVariables(ref parameters, loVarTable, loParameterTable);
            ReplacePlaceholders(ref parameters);
            Process consoleProcess = new Process();
            if (File.Exists(currentPath + "\\" + cmdline))
            {
                cmdline = currentPath + "\\" + cmdline;
            }
            cmdline = cmdline.Replace("%DELIVERIES_SETUP%", currentPath);
            consoleProcess.StartInfo.FileName = cmdline;
            parameters = parameters.Replace("%DELIVERIES_SETUP%", currentPath);
            consoleProcess.StartInfo.Arguments = parameters;
            consoleProcess.StartInfo.UseShellExecute = true;
            consoleProcess.StartInfo.CreateNoWindow = true;
            consoleProcess.StartInfo.WorkingDirectory = currentPath;
            switch (windowsstyle.ToLower())
            {
                case "normal":
                    consoleProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    break;
                case "minimized":
                    consoleProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    break;
                case "maximized":
                    consoleProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    break;
                case "hidden":
                    consoleProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    break;
            }

            try
            {
                consoleProcess.Start();
                if (wait == "true")
                {
                    while (consoleProcess.HasExited == false)
                    {
                        //do nothing... or maybe exit if too much time has passed...
                        Thread.Sleep(100);
                        Application.DoEvents();

                        /*
                        //set foreground window
                        //get the window handle
                        IntPtr hWnd = consoleProcess.MainWindowHandle;
                        //if iconic, we need to restore the window
                        if (IsIconic(hWnd))
                        {
                            ShowWindowAsync(hWnd, SW_RESTORE);
                        }
                        //bring it to foreground
                        SetForegroundWindow(hWnd);
                        */
                    }
                    return consoleProcess.ExitCode.ToString();
                }
                else
                {
                    return "0";
                }
            }
            catch (Win32Exception ex)
            {
                errormsg = ex.Message;
                return "1";
            }
        }

        public void ReplacePlaceholders(ref string toReplace)
        {
            //[OSLANGUAGEID] -- OS LanguageID
            if (toReplace.IndexOf("[OSLANGUAGEID]") > 0)
            {
                toReplace = toReplace.Replace("[OSLANGUAGEID]", GetOSLanguage());
            }
        }

        public void ReplaceEnvVariables(ref string path, DataTable loVarTable, DataTable loParameterTable)
        {
            String pathleft = "";
            String pathmiddle = "";
            String pathright = "";
            Int32 positionFound = 0;

            //retrieve all environment variables
            IDictionary envvariables = Environment.GetEnvironmentVariables();
            foreach (string envvariable in envvariables.Keys)
            {
                if (path.IndexOf("%") >= 0)
                {
                    //"%" found, there must be an environment variable in the string!
                    if (path.ToUpper().IndexOf("%" + envvariable.ToUpper() + "%") >= 0)
                    {
                        //existing environment variable found in the string!
                        positionFound = path.ToUpper().IndexOf("%" + envvariable.ToUpper() + "%");

                        //replace %[]% with its value
                        pathleft = path.Substring(0, positionFound);
                        pathmiddle = path.Substring(positionFound, envvariable.Length + 2);
                        pathmiddle = pathmiddle.ToUpper().Replace("%" + envvariable.ToUpper() + "%", envvariables[envvariable].ToString());
                        pathright = path.Substring(positionFound + envvariable.Length + 2);

                        //path = path.ToUpper().Replace("%" + envvariable.ToUpper() + "%", envvariables[envvariable].ToString());

                        path = pathleft + pathmiddle + pathright;
                    }
                }
            }

            //replace datetime

            path = Regex.Replace(path, "%Time%", DateTime.Now.ToString("HH:mm:ss"), RegexOptions.IgnoreCase);
            path = Regex.Replace(path, "%Date%", DateTime.Now.ToString("yyyy-MM-dd"), RegexOptions.IgnoreCase);
            path = Regex.Replace(path, "%DateTime%", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"), RegexOptions.IgnoreCase);
            path = Regex.Replace(path, "%CurrentFolder%", System.IO.Path.GetDirectoryName(GlobalClass.ApplicationPath));

            if(path.IndexOf("%LegacySetupUninstall%")>=0)
            {
                string lsProductCode = GlobalClass.ProductCode;
                string lsLegacySetupUninstall = "";

                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + lsProductCode))
                    {
                        if (key != null)
                        {
                            Object o = key.GetValue("UninstallString");
                            if (o != null)
                            {
                                lsLegacySetupUninstall = o.ToString();
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + lsProductCode))
                    {
                        if (key != null)
                        {
                            Object o = key.GetValue("UninstallString");
                            if (o != null)
                            {
                                lsLegacySetupUninstall = o.ToString();
                            }
                        }
                    }
                }
                catch { }

                path = path.Replace("%LegacySetupUninstall%", lsLegacySetupUninstall);
            }

            if(path.IndexOf("%deliveriespassword:")>=0)
            {
                string lsPassword = "";
                try
                {
                    lsPassword = path.Substring(path.IndexOf("%deliveriespassword:") + "%deliveriespassword:".Length);
                    lsPassword = lsPassword.Substring(0, lsPassword.IndexOf("%"));
                }
                catch { }
            }

            if(path.Contains("%GeneratedUninstallKeys%"))
            {
                logger.Info("Resolving GeneratedUninstallKeys now ", GlobalClass.SECTION);
                logger.Info("UninstallKeys before: " + GlobalClass.UninstallBefore.Rows.Count.ToString(), GlobalClass.SECTION);
                logger.Info("UninstallKeys now: " + GlobalClass.UninstallAfter.Rows.Count.ToString(), GlobalClass.SECTION);
                string lsGeneratedUninstallKeys = "";
                try
                {
                    foreach (DataRow loAfterInstallRow in GlobalClass.UninstallAfter.Rows)
                    {
                        if (GlobalClass.UninstallBefore.Select("key='" + loAfterInstallRow["key"].ToString() + "'").Length==0)
                        {
                            lsGeneratedUninstallKeys += loAfterInstallRow["key"].ToString() + "|";
                        }
                    }
                }
                catch { }
                try
                {
                    if (lsGeneratedUninstallKeys.EndsWith("|"))
                    {
                        lsGeneratedUninstallKeys = lsGeneratedUninstallKeys.Substring(0, lsGeneratedUninstallKeys.Length-1);
                    }
                }
                catch { }
                logger.Info("Resolved: [" + lsGeneratedUninstallKeys + "]", GlobalClass.SECTION);
                path = path.Replace("%GeneratedUninstallKeys%", lsGeneratedUninstallKeys);

            }


            //replace special folders
            if (path.IndexOf("#") == 0)
            {
                //"#" found, there must be a special folder in the string!
                const int CSIDL_COMMON_STARTMENU = 0x16;
                const int CSIDL_COMMON_DESKTOP = 0x19;
                const int CSIDL_COMMON_APPDATA = 0x23;
                const int CSIDL_COMMON_STARTUP = 0x18;

                try
                {
                    int csidl = 0;
                    int endpos = path.IndexOf("#", 1);
                    //string s = "";
                    string special_placeholder = path.Substring(1, endpos - 1).ToUpper();
                    switch (special_placeholder)
                    {
                        case "COMMON_STARTMENU":
                            csidl = CSIDL_COMMON_STARTMENU;
                            break;
                        case "COMMON_DESKTOP":
                            csidl = CSIDL_COMMON_DESKTOP;
                            break;
                        case "COMMON_APPDATA":
                            csidl = CSIDL_COMMON_APPDATA;
                            break;
                        case "COMMON_STARTUP":
                            csidl = CSIDL_COMMON_STARTUP;
                            break;
                    }
                    if (csidl > 0)
                    {
                        StringBuilder specialfolderpath = new StringBuilder(260);
                        SHGetSpecialFolderPath(IntPtr.Zero, specialfolderpath, csidl, false);
                        string s = specialfolderpath.ToString();
                        path = s + path.Substring(endpos + 1);
                    }
                }
                catch (Exception)
                {
                    //do nothing??
                }
            }


            


            foreach (DataRow loRow in loVarTable.Rows)
            {
                path = Regex.Replace(path, "%" + loRow["name"].ToString() + "%", loRow["value"].ToString(), RegexOptions.IgnoreCase);

            }
            foreach (DataRow loRow in loParameterTable.Rows)
            {
                path = Regex.Replace(path, "%" + loRow["name"].ToString() + "%", loRow["value"].ToString(), RegexOptions.IgnoreCase);

            }

        }

        public string GetOSLanguage()
        {
            string languagecode = null;
            string languagecodehex = "";

            try
            {
                //1) WMI
                ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                ManagementObjectCollection qryCol = qry.Get();
                foreach (ManagementObject mo in qryCol)
                {
                    PropertyDataCollection propCol = mo.Properties;
                    foreach (PropertyData propdata in propCol)
                    {
                        if (propdata.Name.ToUpper() == "OSLANGUAGE")
                        {
                            languagecode = propdata.Value.ToString();
                        }
                    }
                }

                //2) HKEY_USERS\.DEFAULT\Control Panel\Desktop\MultiUILanguageId
                RegistryKey regKey = null;
                string path = ".DEFAULT\\Control Panel\\Desktop";
                string name = "MultiUILanguageId";
                regKey = Registry.Users.OpenSubKey(path);
                if (regKey != null)
                {
                    if (regKey.GetValue(name) != null)
                    {
                        languagecodehex = regKey.GetValue(name).ToString();
                        int langID = Convert.ToInt32(languagecodehex, 16);
                        if (langID > 0)
                        {
                            languagecode = langID.ToString();
                        }
                    }
                }
                return languagecode;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string GetInstalledGUID(string productCodeOrName)
        {
            string installedGUID = "";
            try
            {
                Type type = Type.GetTypeFromProgID("WindowsInstaller.Installer");
                if (type != null)
                {
                    object myWinInstaller = Activator.CreateInstance(type);

                    object products = type.InvokeMember("Products", BindingFlags.GetProperty, null, myWinInstaller, null);

                    foreach (object product in products as System.Collections.IEnumerable)
                    {

                        if (product.ToString().ToUpper() == productCodeOrName.ToUpper())
                        {
                            installedGUID = product.ToString().ToUpper();
                        }

                        try
                        {
                            object[] arguments = { product.ToString(), "InstalledProductName" };
                            object productname = type.InvokeMember("ProductInfo", BindingFlags.GetProperty, null, myWinInstaller, arguments);

                            if (productname.ToString().ToUpper() == productCodeOrName.ToUpper())
                            {
                                installedGUID = product.ToString().ToUpper();
                            }
                        }
                        catch (System.Reflection.TargetInvocationException)
                        {
                            logger.Warn("GetInstalledGUID; Package '" + product.ToString() + "': error while getting ProductInfo", GlobalClass.SECTION);
                        }
                    }
                }
                return installedGUID;
            }
            catch (Exception)
            {
                return installedGUID;
            }
        }

        public static object COMObjectCreate(string progID)
        {
            Type type = Type.GetTypeFromProgID(progID);
            if (type != null)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public void UpdateFileAppenderPath(string newPath)
        {
            GlobalClass.LogFileProductCode = newPath;
        }

        /*
        public void UpdateFileAppenderPath(string newPath)
        {
            log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            foreach (IAppender a in h.Root.Appenders)
            {
                if (a is FileAppender)
                {
                    FileAppender fa = (FileAppender)a;
                    fa.File = newPath;
                    fa.ActivateOptions();
                }
            }
        }
        */

        public bool CheckLogFilePath(string logFilePath)
        {
            try
            {
                TextWriter tw = new StreamWriter(logFilePath, true);
                tw.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool StartAltirisTask(string name, string pathToScript, DataTable loVarTable, DataTable loParameterTable)
        {
            string taskID = "";
            string errormessage = "";

            try
            {
                taskID = GetAltirisTaskID(name, loVarTable, loParameterTable);
                if (taskID == "")
                {
                    //Altiris NOT found --> logging?
                    return false;
                }
                else
                {
                    string returnvalue = ExecuteCMD(pathToScript, "ID=" + taskID, ref errormessage, "true", loVarTable, loParameterTable);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetAltirisTaskID(string taskname, DataTable loVarTable, DataTable loParameterTable)
        {
            string taskID = "";
            string advertismentsLogPath = "%SYSTEMROOT%\\debug\\A_S_Altiris_Advertisements.log";
            Functions myFunctions = new Functions();
            myFunctions.ReplaceEnvVariables(ref advertismentsLogPath, loVarTable, loParameterTable);

            //cleanup log file
            try
            {
                if (File.Exists(advertismentsLogPath))
                {
                    File.Delete(advertismentsLogPath);
                }
            }
            catch
            { }


            try
            {
                //Create .log File
                TextWriter tw = new StreamWriter(advertismentsLogPath, true);

                //Altiris.AeXNSClient
                Type t0 = System.Type.GetTypeFromProgID("Altiris.AeXNSClient");
                object o1 = Activator.CreateInstance(t0);
                object obj1 = t0.InvokeMember("ClientPolicyMgr", BindingFlags.GetProperty, null, o1, new object[] { });

                //ClientAgent
                Type t1 = obj1.GetType();
                object obj2 = t1.InvokeMember("ClientAgent", BindingFlags.GetProperty, null, obj1, new object[] { "Altiris.SWD" });

                //Advertisement
                Type t2 = obj2.GetType();
                object obj3 = t2.InvokeMember("Advertisements", BindingFlags.GetProperty, null, obj2, new object[] { });
                Type t3 = obj3.GetType();

                foreach (object task in obj3 as System.Collections.IEnumerable)
                {
                    string task_id = "n/a";
                    string task_nextruntime = "n/a";
                    try
                    {
                        Type t4 = task.GetType();
                        object objTaskEnabled = t4.InvokeMember("IsEnabled", BindingFlags.GetProperty, null, task, null);
                        if (objTaskEnabled != null)
                        {
                            if (objTaskEnabled.ToString() == "1")
                            {
                                object obj5 = t4.InvokeMember("Name", BindingFlags.GetProperty, null, task, null);
                                object obj7 = t4.InvokeMember("Program", BindingFlags.GetProperty, null, task, null);
                                Type t7 = obj7.GetType();
                                object obj8 = t7.InvokeMember("Status", BindingFlags.GetProperty, null, obj7, null);
                                object obj9 = t4.InvokeMember("Id", BindingFlags.GetProperty, null, task, null);
                                object obj10 = t4.InvokeMember("NextRunTime", BindingFlags.GetProperty, null, task, null);

                                if (obj9 != null)
                                {
                                    task_id = obj9.ToString();
                                }
                                if (obj10 != null)
                                {
                                    task_nextruntime = obj10.ToString();
                                }

                                tw.WriteLine(obj5.ToString() + "; Status=" + obj8.ToString() + "; ID='" + task_id + "'; NextRunTime='" + task_nextruntime + "'");
                                if (String.Compare(obj5.ToString().ToLower(), taskname.ToLower(), true) == 0)
                                {
                                    object obj6 = t4.InvokeMember("Id", BindingFlags.GetProperty, null, task, null);
                                    taskID = obj6.ToString();

                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //do nothing
                    }
                } // for each
                tw.Close();

                if (taskID.Length == 0)
                {
                    //search in registry ([HKLM\SOFTWARE\Altiris\Altiris Agent\SMFAgent\Delivery\Policies\])
                    RegistryKey myRegKey = Registry.LocalMachine;
                    myRegKey = myRegKey.OpenSubKey("SOFTWARE\\Altiris\\Altiris Agent\\SMFAgent\\Delivery\\Policies");
                    String[] subkeyNames = myRegKey.GetSubKeyNames();
                    foreach (String s in subkeyNames)
                    {
                        RegistryKey PolicyKey = Registry.LocalMachine;
                        PolicyKey = PolicyKey.OpenSubKey("SOFTWARE\\Altiris\\Altiris Agent\\SMFAgent\\Delivery\\Policies\\" + s);
                        try
                        {
                            Object oValue = PolicyKey.GetValue("Name");
                            if (String.Compare(oValue.ToString().ToLower(), taskname.ToLower(), true) == 0)
                            {
                                taskID = s;
                            }
                        }
                        catch (NullReferenceException)
                        {
                            //do nothing
                        }
                    }

                }

                return taskID;
            }
            catch (System.IO.FileNotFoundException)
            {
                //MessageBox.Show(ex.Message);
                return taskID;
            }
        }

        public string GetAltirisPackageSources(string curtaskID)
        {
            string taskID = "";

            try
            {
                string curAltirisPackageSources = "";

                //Altiris.AeXNSClient
                Type t0 = System.Type.GetTypeFromProgID("Altiris.AeXNSClient");
                object o1 = Activator.CreateInstance(t0);
                object obj1 = t0.InvokeMember("ClientPolicyMgr", BindingFlags.GetProperty, null, o1, new object[] { });

                //ClientAgent
                Type t1 = obj1.GetType();
                object obj2 = t1.InvokeMember("ClientAgent", BindingFlags.GetProperty, null, obj1, new object[] { "Altiris.SWD" });

                //Advertisments
                Type t2 = obj2.GetType();
                object obj3 = t2.InvokeMember("Advertisements", BindingFlags.GetProperty, null, obj2, new object[] { });
                Type t3 = obj3.GetType();

                foreach (object task in obj3 as System.Collections.IEnumerable)
                {
                    Type t4 = task.GetType();
                    object obj6 = t4.InvokeMember("Id", BindingFlags.GetProperty, null, task, null);
                    taskID = obj6.ToString();
                    if (taskID.ToUpper() == curtaskID.ToUpper())
                    {
                        //get sources
                        object obj4 = t4.InvokeMember("Package", BindingFlags.GetProperty, null, task, null);
                        Type t5 = obj4.GetType();
                        object obj5 = t5.InvokeMember("Source", BindingFlags.GetProperty, null, obj4, null);
                        foreach (object source in obj5 as System.Collections.IEnumerable)
                        {
                            curAltirisPackageSources = curAltirisPackageSources + source.ToString() + ";";
                        }
                    }


                }
                return curAltirisPackageSources;
            }

            catch (Exception)
            {
                //MessageBox.Show(ex.Message);
                return "";
            }
        }

        public string GetRunningAltirisTask()
        {
            const int STATUS_RUNNING = 8;
            //const int STATUS_QUEUED = 6;
            //const int STATUS_RUNCOMPLETED = 0;
            //const int STATUS_NEVERSTARTED = 1;

            string taskID = "";

            try
            {
                //Altiris.AeXNSClient
                Type t0 = System.Type.GetTypeFromProgID("Altiris.AeXNSClient");
                object o1 = Activator.CreateInstance(t0);
                object obj1 = t0.InvokeMember("ClientPolicyMgr", BindingFlags.GetProperty, null, o1, new object[] { });

                //ClientAgent
                Type t1 = obj1.GetType();
                object obj2 = t1.InvokeMember("ClientAgent", BindingFlags.GetProperty, null, obj1, new object[] { "Altiris.SWD" });

                //Advertisments
                Type t2 = obj2.GetType();
                object obj3 = t2.InvokeMember("Advertisements", BindingFlags.GetProperty, null, obj2, new object[] { });
                Type t3 = obj3.GetType();

                foreach (object task in obj3 as System.Collections.IEnumerable)
                {
                    //get running Altiris task
                    Type t4 = task.GetType();
                    object obj4 = t4.InvokeMember("Program", BindingFlags.GetProperty, null, task, null);
                    Type t5 = obj4.GetType();
                    object obj5 = t5.InvokeMember("Status", BindingFlags.GetProperty, null, obj4, null);

                    if ((int)obj5 == STATUS_RUNNING)
                    {
                        //currently running task
                        object obj6 = t4.InvokeMember("Id", BindingFlags.GetProperty, null, task, null);
                        taskID = obj6.ToString();
                    }

                }
                return taskID;
            }
            //catch (System.IO.FileNotFoundException ex)
            catch (Exception)
            {
                //MessageBox.Show(ex.Message);
                return taskID;
            }
        }

        public bool AddSourceToSourceList(string productcode, string path, DataTable loVarTable, DataTable loParameterTable)
        {

            string curGUID = "";
            curGUID = GetInstalledGUID(productcode);

            //Package installed
            if (curGUID != "")
            {
                try
                {
                    Type type = Type.GetTypeFromProgID("WindowsInstaller.Installer");
                    if (type != null)
                    {
                        object myWinInstaller = Activator.CreateInstance(type);
                        object[] arguments = { productcode, "PackageName" };
                        object packagename = type.InvokeMember("ProductInfo", BindingFlags.GetProperty, null, myWinInstaller, arguments);

                        if (CheckFile(path + "\\" + packagename.ToString(), loVarTable, loParameterTable) || CheckFile(path + "/" + packagename.ToString(), loVarTable, loParameterTable))
                        {
                            //addsource
                            object[] args = new Object[3];
                            args[0] = productcode;
                            args[1] = "";
                            args[2] = path;
                            type.InvokeMember("AddSource", BindingFlags.InvokeMethod, null, myWinInstaller, args);
                            return true;
                        }
                        else
                            return false;
                    }
                    return false;

                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public uint CreateScheduledTask(string starttime, string altirisTaskID)
        {
            ManagementClass classInstance = new ManagementClass("root\\CIMV2", "Win32_ScheduledJob", null);
            string cmd = "";
            string lsTimeValue = "";
            //int liBias = 0;
            Int32 liBias = 0;

            try
            {
                // Obtain in-parameters for the method
                ManagementBaseObject inParams = classInstance.GetMethodParameters("Create");
                logger.Info("CreateScheduledTask: Postpone", GlobalClass.SECTION);
                if (altirisTaskID == "")
                {
                    cmd = GlobalClass.GlobalCommandLine;
                }
                else
                {
                    logger.Info("CreateScheduledTask: Postpone: AltirisTaskID: " + altirisTaskID, GlobalClass.SECTION);
                    //cmd = GlobalClass.GlobalStartAltirisTask + " ID=" + altirisTaskID;
                }

                //Calculate startime
                //Bias
                ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_TimeZone");
                ManagementObjectCollection qryCol = qry.Get();
                foreach (ManagementObject mo in qryCol)
                {
                    PropertyDataCollection propCol = mo.Properties;
                    foreach (PropertyData propdata in propCol)
                    {
                        if (propdata.Name.ToUpper() == "BIAS")
                        {
                            liBias = Convert.ToInt32(propdata.Value.ToString());
                        }
                    }
                }
                if (liBias > 0)
                {
                    lsTimeValue = "00.000000+";
                }
                else
                {
                    lsTimeValue = "00.000000-";
                }
                liBias = Math.Abs(liBias);

                //Daylight saving time
                //Get the local time zone and the current local time and year.
                TimeZone localZone = TimeZone.CurrentTimeZone;
                DateTime currentDate = DateTime.Now;
                bool isDaylightSavingTime = localZone.IsDaylightSavingTime(currentDate);
                logger.Info("CreateScheduledTask: Postpone: local TimeZone: '" + localZone.StandardName + "'; Daylight saving time: " + isDaylightSavingTime, GlobalClass.SECTION);
                if (isDaylightSavingTime)
                {
                    liBias = liBias + 60;
                }

                if (liBias < 100)
                {
                    lsTimeValue = lsTimeValue + "0";
                }
                if (liBias < 10)
                {
                    lsTimeValue = lsTimeValue + "0";
                }

                lsTimeValue = lsTimeValue + Convert.ToString(liBias);

                // Command
                logger.Info("CreateScheduledTask: Postpone: Command: '" + cmd + "'", GlobalClass.SECTION);
                logger.Info("CreateScheduledTaks: Postpone: StartTime: '" + starttime + lsTimeValue + "'", GlobalClass.SECTION);
                inParams["Command"] = cmd;
                inParams["StartTime"] = starttime + lsTimeValue;
                ManagementBaseObject outParams = classInstance.InvokeMethod("Create", inParams, null);
                //uint JobId = ((uint)(outParams.Properties["JobId"].Value));
                logger.Info("CreateScheduledTaks: Postpone: Scheduled Task created", GlobalClass.SECTION);

                return ((uint)(outParams.Properties["ReturnValue"].Value));
            }
            catch (Exception ex)
            {
                logger.Info("CreateScheduledTaks: #ERROR: '" + ex.Message + "'", GlobalClass.SECTION);
                return (uint)(1);
            }
        }

        public uint CreateScheduledRebootTask(string starttime)
        {
            ManagementClass classInstance = new ManagementClass("root\\CIMV2", "Win32_ScheduledJob", null);
            string lsTimeValue = "";
            Int32 liBias = 0;

            try
            {
                // Obtain in-parameters for the method
                ManagementBaseObject inParams = classInstance.GetMethodParameters("Create");
                logger.Info("CreateScheduledTask: Reboot", GlobalClass.SECTION);


                //Calculate startime
                //Bias
                ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_TimeZone");
                ManagementObjectCollection qryCol = qry.Get();
                foreach (ManagementObject mo in qryCol)
                {
                    PropertyDataCollection propCol = mo.Properties;
                    foreach (PropertyData propdata in propCol)
                    {
                        if (propdata.Name.ToUpper() == "BIAS")
                        {
                            liBias = Convert.ToInt32(propdata.Value.ToString());
                        }
                    }
                }
                if (liBias > 0)
                {
                    lsTimeValue = "00.000000+";
                }
                else
                {
                    lsTimeValue = "00.000000-";
                }
                liBias = Math.Abs(liBias);

                //Daylight saving time
                //Get the local time zone and the current local time and year.
                TimeZone localZone = TimeZone.CurrentTimeZone;
                DateTime currentDate = DateTime.Now;
                bool isDaylightSavingTime = localZone.IsDaylightSavingTime(currentDate);
                logger.Info("CreateScheduledTask: Reboot: local TimeZone: '" + localZone.StandardName + "'; Daylight saving time: " + isDaylightSavingTime, GlobalClass.SECTION);
                if (isDaylightSavingTime)
                {
                    liBias = liBias + 60;
                }

                if (liBias < 100)
                {
                    lsTimeValue = lsTimeValue + "0";
                }
                if (liBias < 10)
                {
                    lsTimeValue = lsTimeValue + "0";
                }

                lsTimeValue = lsTimeValue + Convert.ToString(liBias);

                // Command
                logger.Info("CreateScheduledTask: Reboot: Command: '" + "shutdown /r" + "'", GlobalClass.SECTION);
                logger.Info("CreateScheduledTaks: Reboot: StartTime: '" + starttime + lsTimeValue + "'", GlobalClass.SECTION);
                inParams["Command"] = "shutdown /r";
                inParams["StartTime"] = starttime + lsTimeValue;
                ManagementBaseObject outParams = classInstance.InvokeMethod("Create", inParams, null);
                //uint JobId = ((uint)(outParams.Properties["JobId"].Value));
                logger.Info("CreateScheduledTaks: Reboot: Scheduled Task created", GlobalClass.SECTION);

                return ((uint)(outParams.Properties["ReturnValue"].Value));
            }
            catch (Exception ex)
            {
                logger.Info("CreateScheduledTaks: #ERROR: '" + ex.Message + "'", GlobalClass.SECTION);
                return (uint)(1);
            }
        }

        static byte[] ConvertHexStringToByteArray(string hexString)
        {

            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] HexAsBytes = new byte[hexString.Length / 2];
            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return HexAsBytes;
        }

        private Point getControlCoords(string lsCtrlProc, string lsCtrlId, string lsCtrlX, string lsCtrlY)
        {
            Point emptyPoint = new Point();

            if (lsCtrlProc == "") return emptyPoint;

            Process[] procList = Process.GetProcessesByName(lsCtrlProc);
            if (procList.Length == 0) return emptyPoint;
            int procID = procList[0].Id;

            /*
            TreeWalker tWalker = TreeWalker.ControlViewWalker;
            AutomationElement element = tWalker.GetFirstChild(AutomationElement.RootElement);
            while (element.Current.ProcessId != procID) element = tWalker.GetNextSibling(element);
            if (element.Current.ProcessId != procID) return;
            */

            //find main window from processID
            PropertyCondition condition = new PropertyCondition(AutomationElement.ProcessIdProperty, procID);
            AutomationElement rootElement = AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
            if (rootElement == null) return emptyPoint;

            //find control by ID in main window
            if (lsCtrlId != "")
            {
                condition = new PropertyCondition(AutomationElement.AutomationIdProperty, lsCtrlId);
                AutomationElement element = rootElement.FindFirst(TreeScope.Descendants, condition);
                if (element == null) return emptyPoint;
                System.Windows.Point pt;
                try
                {
                    if (element.TryGetClickablePoint(out pt)) return (new Point((int)pt.X, (int)pt.Y));
                }
                catch
                {
                    logger.Warn("could not find ClickablePoint for control " + lsCtrlProc + ": " + lsCtrlId, GlobalClass.SECTION);
                }
                return emptyPoint;
            }

            //control not found, find coords in window
            if (lsCtrlX == "") return emptyPoint;
            int ctrlX = int.Parse(lsCtrlX);
            int ctrlY = int.Parse(lsCtrlY);
            System.Windows.Rect mainCoords = rootElement.Current.BoundingRectangle;
            return (new Point((int)mainCoords.X + ctrlX, (int)mainCoords.Y + ctrlY));

        }

        private void moveMouse(Point newPosition, bool showMouse = false)
        {
            if (showMouse)
            {
                PointF pos = Cursor.Position;
                PointF slope = new PointF(newPosition.X - pos.X, newPosition.Y - pos.Y);
                float distance = (float)Math.Sqrt((double)(slope.X * slope.X + slope.Y * slope.Y));
                int steps = (int)Math.Round(distance / 20);
                slope.X = slope.X / steps;
                slope.Y = slope.Y / steps;

                for (int i = 0; i < steps; i++)
                {
                    pos = new PointF(pos.X + slope.X, pos.Y + slope.Y);
                    Cursor.Position = Point.Round(pos);
                    Thread.Sleep(10);
                }
            }
            Cursor.Position = newPosition;
        }

        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }


        static private string decrypt(string lsstring)
        {
            string x = "";
            int i = 0;
            string tmp = "";
            if (lsstring != "")
            {
                do
                {
                    x = lsstring.Substring(i, 1);
                    tmp = Convert.ToChar(Convert.ToInt16(Convert.ToChar(x)) - 1) + tmp;
                    i += 4;
                } while (i < lsstring.Length);
            }
            return tmp;
        }

        static private string PSParseOutput(string command, string name)
        {
            string output = "";
            foreach (string line in command.Split(Convert.ToChar("\n")))
            {
                Debug.Write(line);
                if(line.Contains(":"))
                {
                    if(line.Split(Convert.ToChar(":"))[0].Contains(name))
                    {
                        output = line.Split(Convert.ToChar(":"))[1];
                        output = output.Replace("\r", "");
                        output=output.Trim();
                        break;
                    }
                }
            }
            return output;

        }

        static private string PSParseOutputs(string command, string name, string searchfor)
        {
            string output = "";
            string lastpackagefullname = "";
            foreach (string line in command.Split(Convert.ToChar("\n")))
            {
                Debug.Write(line);
                if (line.Contains(":"))
                {
                    if (line.Split(Convert.ToChar(":"))[0].Contains(name))
                    {
                        lastpackagefullname = line.Split(Convert.ToChar(":"))[1];
                        lastpackagefullname = lastpackagefullname.Replace("\r", "");
                        lastpackagefullname = lastpackagefullname.Trim();                        
                    }
                    if (line.Split(Convert.ToChar(":"))[1].Contains(searchfor))
                    {
                        output = lastpackagefullname;
                    }
                }
                if(output!="")
                {
                    break;
                }
            }
            return output;

        }


        static private string PSExecute(string command)
        {

            Process currentProcess = Process.GetCurrentProcess();
            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName="powershell.exe";
            startInfo.WorkingDirectory = currentPath;
            startInfo.Arguments = @"-command """ + command + @"""";
            startInfo.RedirectStandardOutput=true;
            startInfo.RedirectStandardError=true;
            startInfo.UseShellExecute=false;
            startInfo.CreateNoWindow=true;


            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            return output;

        }

        static private void GetPrivileges()
        {
            int token = 0;
            int retval = 0;
            TOKEN_PRIVILEGES tpRestore = new TOKEN_PRIVILEGES();
            TOKEN_PRIVILEGES tpBackup = new TOKEN_PRIVILEGES();
            LUID RestoreLuid = new LUID();
            LUID BackupLuid = new LUID();

            retval = LookupPrivilegeValue(null, SE_RESTORE_NAME, ref RestoreLuid);
            tpRestore.PrivilegeCount = 1;
            tpRestore.Attributes = SE_PRIVILEGE_ENABLED;
            tpRestore.Luid = RestoreLuid;

            retval = LookupPrivilegeValue(null, SE_BACKUP_NAME, ref BackupLuid);
            tpBackup.PrivilegeCount = 1;
            tpBackup.Attributes = SE_PRIVILEGE_ENABLED;
            tpBackup.Luid = BackupLuid;

            retval = OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref token);
            retval = AdjustTokenPrivileges(token, 0, ref tpRestore, 1024, 0, 0);
            retval = AdjustTokenPrivileges(token, 0, ref tpBackup, 1024, 0, 0);

            gotPrivileges = true;
        }

        static public string RegistryLoad(string file)
        {
            if (!gotPrivileges)
                GetPrivileges();
            RegLoadKey(HKEY_USERS, HIVE_SUBKEY, file);
            return HIVE_SUBKEY;
        }

        static public void RegistryUnload()
        {
            if (!gotPrivileges)
                GetPrivileges();
            int output = RegUnLoadKey(HKEY_USERS, HIVE_SUBKEY);
        }
    }




}




static class ResolutionNative
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            // You can define the following constant
            // but OUTSIDE the structure because you know
            // that size and layout of the structure is very important
            // CCHDEVICENAME = 32 = 0x50
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            // In addition you can define the last character array
            // as following:
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            //public Char[] dmDeviceName;

            // After the 32-bytes array
            [MarshalAs(UnmanagedType.U2)]
            public UInt16 dmSpecVersion;

            [MarshalAs(UnmanagedType.U2)]
            public UInt16 dmDriverVersion;

            [MarshalAs(UnmanagedType.U2)]
            public UInt16 dmSize;

            [MarshalAs(UnmanagedType.U2)]
            public UInt16 dmDriverExtra;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmFields;

            public POINTL dmPosition;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmDisplayOrientation;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmDisplayFixedOutput;

            [MarshalAs(UnmanagedType.I2)]
            public Int16 dmColor;

            [MarshalAs(UnmanagedType.I2)]
            public Int16 dmDuplex;

            [MarshalAs(UnmanagedType.I2)]
            public Int16 dmYResolution;

            [MarshalAs(UnmanagedType.I2)]
            public Int16 dmTTOption;

            [MarshalAs(UnmanagedType.I2)]
            public Int16 dmCollate;

            // CCHDEVICENAME = 32 = 0x50
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            // Also can be defined as
            //[MarshalAs(UnmanagedType.ByValArray, 
            //    SizeConst = 32, ArraySubType = UnmanagedType.U1)]
            //public Byte[] dmFormName;

            [MarshalAs(UnmanagedType.U2)]
            public UInt16 dmLogPixels;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmBitsPerPel;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmPelsWidth;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmPelsHeight;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmDisplayFlags;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmDisplayFrequency;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmICMMethod;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmICMIntent;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmMediaType;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmDitherType;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmReserved1;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmReserved2;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmPanningWidth;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dmPanningHeight;

            /// <summary>
            /// Initializes the structure variables.
            /// </summary>
            public void Initialize()
            {
                this.dmDeviceName = new string(new char[32]);
                this.dmFormName = new string(new char[32]);
                this.dmSize = (ushort)Marshal.SizeOf(this);
            }
        }

            // 8-bytes structure
        [StructLayout(LayoutKind.Sequential)]
        public struct POINTL
        {
            public Int32 x;
            public Int32 y;
        }

        [DllImport("User32.dll", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean EnumDisplaySettings(
            [param: MarshalAs(UnmanagedType.LPTStr)]
            String lpszDeviceName,  // display device
            [param: MarshalAs(UnmanagedType.U4)]
            Int32 iModeNum,         // graphics mode
            [In, Out]
            ref DEVMODE lpDevMode       // graphics mode settings
            );

        public const int ENUM_CURRENT_SETTINGS = -1;

        [DllImport("User32.dll", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int ChangeDisplaySettings(
            [In, Out]
            ref DEVMODE lpDevMode,
            [param: MarshalAs(UnmanagedType.U4)]
            uint dwflags);
}