using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Globalization;
using System.Management;
using System.Drawing.Text;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Data;
using System.Windows.Automation;


//using log4net;
//using log4net.Config;
//using log4net.Appender;

namespace SetupExe
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


    
    class Functions
    {
        //variables, structs, const,..

        const int SERVICETIMEOUT = 30000; //milliseconds

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWDEFAULT = 10;

        public string SECTION = "install";

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
        private static extern bool IsWow64Process([In] IntPtr hProcess,[Out] out bool wow64Process); 
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

        const int OS_ANYSERVER = 29;

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_CHAR = 0x105;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;

        const Int32 VK_RETURN = 0x0D;
        const int VK_ENTER = 0x0D;

        /*
        bool showMouse = true;
        private Point mouseDownCoords = new Point();
        */


        //[DllImport("kernel32.dll")] 
        //private static extern void GlobalMemoryStatusEx(out MemoryStatus stat);


        //my functions

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
                CreateDirectory(dest);
            }
            if (System.IO.File.Exists(source))
            {
                Ionic.Zip.ZipFile loZip = new Ionic.Zip.ZipFile(source);
                loZip.ExtractAll(dest, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
            }

            return lbReturn;
        }

        public bool CopyFolder(string source, string dest, string overwrite, DataTable loVarTable)
        {
            bool successfullyCopied = true;
            bool overwriteb = false;
            if (overwrite.ToLower() == "true")
            {
                overwriteb = true;
            }
            ReplaceEnvVariables(ref source, loVarTable);
            ReplaceEnvVariables(ref dest, loVarTable);
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
                if(System.IO.File.Exists(lsPath))
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

                if(lsArguments!="")
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
            if(System.IO.File.Exists(lsPath))
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
                    string path = Regex.Replace(Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine),Environment.ExpandEnvironmentVariables(lsValue), "", RegexOptions.IgnoreCase);
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

        public bool RemoveShortcuts(string lsPath, string lsFileName)
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
                filesafter = Directory.GetFiles(Environment.ExpandEnvironmentVariables(lsPath), "*.*", SearchOption.AllDirectories);
                foreach (string lsFile in filesafter)
                {
                    string shortcuttargetfile = GetShortcutTargetFile(lsFile);
                    logger.Info("checking if shortcut " + lsFile + " is valid", SECTION);
                    bool lbMustDelete = false;
                    if (shortcuttargetfile != "")
                    {
                        logger.Info("targetfile is " + shortcuttargetfile, SECTION);
                        if (!System.IO.File.Exists(shortcuttargetfile))
                        {
                            logger.Info("targetfile is broken...", SECTION);
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
                        logger.Info("removing shortcut " + lsFile, SECTION);
                        try
                        {
                            System.IO.File.Delete(lsFile);
                        }
                        catch { }
                    }
                }
            }
            catch { }

            //try
            //{
            //    DirectoryInfo loDirectory = new DirectoryInfo(Environment.ExpandEnvironmentVariables(lsPath));
            //    if(loDirectory.GetFiles("*.*", SearchOption.AllDirectories).Length==0)
            //    {
            //        System.IO.Directory.Delete(Environment.ExpandEnvironmentVariables(lsPath));
            //    }
            //}
            //catch { }

            CleanEmptyStartMenuFolders();

            return true;
        }

        public bool MoveShortcuts(string lsPath, string[] lsBeforeStartMenu)
        {

           
            if(lsPath.EndsWith(@"\"))
            {
                try
                {
                    lsPath = lsPath.Substring(0,lsPath.Length-1);
                }
                catch{}
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
                            logger.Info("sourcefolder is " + lsSourceFolder,SECTION);
                            if (lsSourceFolder != "")
                            {
                                lsNewTarget = lsNewTarget + @"\" + lsSourceFolder;
                            }
                            try
                            {
                                logger.Info("creating directory " + lsNewTarget,SECTION);
                                CreateDirectory(lsNewTarget);
                            }
                            catch { }
                            try
                            {

                                logger.Info("copying '" + lsFile + "' to '" + Environment.ExpandEnvironmentVariables(lsNewTarget + @"\" + System.IO.Path.GetFileName(lsFile)) + "'",SECTION);
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
            logger.Info("removeexistingproduct",SECTION);

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
                                    NowUninstallSoftware(lsSubKey);
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
                                    NowUninstallSoftware(lsSubKey);
                                }
                            }
                        }
                        catch { }
                    }
                    break;
                case "displayname":
                    {
                        string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                        try
                        {
                            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                            foreach(string lsSubKey in regKey.GetSubKeyNames())
                            {
                                try
                                {
                                    string lsDisplayName = "";
                                    RegistryKey subKey = Registry.LocalMachine.OpenSubKey(path + @"\" + lsSubKey);
                                    lsDisplayName = subKey.GetValue("DisplayName").ToString();
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
                                    lsDisplayName = subKey.GetValue("DisplayName").ToString();
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
                                                foreach(string lsInstallerKey in subregKey.GetValueNames())
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
            string lsUninstallString = "";
            string lsProductCode = "";

            logger.Warn("uninstalling " + lsUninstallKey, SECTION);

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
                        }
                        catch { }
                        if(lsUninstallString=="")
                        {
                            lsUninstallString = subregKey.GetValue("UninstallString").ToString();
                            lsProductCode = lsSubKey;
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
                            }
                            catch { }
                            if (lsUninstallString == "")
                            {
                                lsUninstallString = subregKey.GetValue("UninstallString").ToString();
                                lsProductCode = lsSubKey;
                            }
                        }
                    }
                }
                catch { }
            }

            if(lsUninstallString!="")
            {
                if(lsUninstallString.ToLower().Contains("msiexec") && lsProductCode.Length==38)
                {
                    string lsErrorMessage = "";
                    ExecuteCMD("msiexec", "/x " + lsProductCode + " /q REBOOT=ReallySuppress",ref lsErrorMessage,"true",new DataTable());
                }

                logger.Warn(lsUninstallString, SECTION);
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
                lsUpgradeCode = "{" + Reverse(lsValue.ToUpper().Substring(0, 8) ) + "-";
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

            if(lsCtrlProcess!="")
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

        public  bool SendWindow(string lsCommand, string lsCtrlProcess, string lsCtrlId)
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
                        foreach(Process loProcess in prc)
                        {
                            logger.Info("found process " + loProcess.ProcessName + " - " + loProcess.Id.ToString(),SECTION);
                            try
                            {
                                liHWnd = Convert.ToInt32(loProcess.MainWindowHandle.ToString());
                                if (liHWnd > 0)
                                {
                                    break;
                                }
                            }
                            catch { }
                        }                       
                    }
                    if(liHWnd>0)
                    {
                        break;
                    }
                    Thread.Sleep(500);
                    liCounter += 1;
                    logger.Warn("waiting for window for process: " + lsCtrlId + " (" + liCounter.ToString() + "/50)",SECTION);
                } while (liCounter <= 50);
            }
            catch { }

            if(liHWnd > 0)
            { 
                logger.Info("found process " + lsCtrlProcess + " - " + liHWnd.ToString(),SECTION);

                lbReturn = true;
               
                switch(lsCommand.ToLower())
                {
                    case "maximize":
                        lbReturn = ActivateWindow(new IntPtr(liHWnd), "maximize");
                        break;
                    case "minimize":
                        lbReturn = ActivateWindow(new IntPtr(liHWnd), "minimize");
                        break;
                    default:
                        lbReturn = ActivateWindow(new IntPtr(liHWnd), "");
                        break;
                }
                
            }
            else
            {
                logger.Info("could not find process " + lsCtrlProcess, SECTION);
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

            // Guard: check if window already has focus.            

            try
            {
                AllowSetForegroundWindow(mainWindowHandle);
            }
            catch { }

            logger.Info("checking if ActivateWindow " + mainWindowHandle.ToString() + " has focus", SECTION);

            logger.Info("active window title: " + GetActiveWindowTitle(),SECTION);

            if (mainWindowHandle == GetForegroundWindow())
            {
                logger.Info("focus is true", SECTION);
                lbReturn = true;
            }
            else
            {
                logger.Info("mainwindowhandle (" + mainWindowHandle.ToString() + ") is not GetForeGroundWindow (" + GetForegroundWindow().ToString() + ")",SECTION);
                if (IsIconic(mainWindowHandle) == true)
                {
                    ShowWindow(mainWindowHandle, 9);
                    logger.Info("isiconic is true", SECTION);
                    lbReturn = true;
                }
                else
                {
                    SetForegroundWindow(mainWindowHandle);
                    int liCounter = 0;
                    do
                    {
                        logger.Info("sending alt-tab)", SECTION);
                        System.Windows.Forms.SendKeys.SendWait("%{TAB}");
                        Thread.Sleep(200);
                        logger.Info("checking mainwindowhandle (" + mainWindowHandle.ToString() + ") is GetForeGroundWindow (" + GetForegroundWindow().ToString() + ")", SECTION);
                        logger.Info("active window title: " + GetActiveWindowTitle(), SECTION);
                        if(mainWindowHandle==GetForegroundWindow())
                        {
                            lbReturn = true;
                            break;
                        }
                        liCounter += 1;
                    } while (liCounter <= 10);                    
                }
            }
                
                

                // Show window in forground.
//                SetForegroundWindow(mainWindowHandle);

            
          

            {
                if (lsAction.ToString() == "minimize" || lsAction.ToString() == "maximize")
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
                    if (lsAction.ToString() == "minimize")
                    {
                        // Show window maximized.
                        ShowWindow(mainWindowHandle, SHOW_MINIMIZED);
                    }
                }
            }

            System.Threading.Thread.Sleep(200);
            //SetForegroundWindow(mainWindowHandle);


            return lbReturn;

        }

        public bool SendMouse(string lsCommand, string lsX, string lsY, string lspositiontype, string lsXEnd, string lsYEnd, string lsCtrlProcess, string lsCtrlId, string lsShowMove)
       {
           logger.Info("sendmouse "+lsCommand+", "+lsX+", "+lsY+", "+lspositiontype+", "+lsXEnd+", "+lsYEnd+", "+lsCtrlProcess+", "+lsCtrlId+", "+lsShowMove, SECTION);

            Point startCoords=new Point();
            Point endCoords=new Point();

            //translate coords
            if (lspositiontype == "window")
            {
                startCoords = getControlCoords(lsCtrlProcess,"",lsX,lsY);
                endCoords = getControlCoords(lsCtrlProcess,"",lsXEnd,lsYEnd);
            }
            else if (lspositiontype == "control")
            {
                startCoords = getControlCoords(lsCtrlProcess,lsCtrlId,lsX,lsY);
                endCoords = getControlCoords(lsCtrlProcess,lsCtrlId,lsXEnd,lsYEnd);
            }
            else //lspositiontype == "screen"
            {
                startCoords=new Point(int.Parse(lsX), int.Parse(lsY));
                if (lsXEnd != "") endCoords=new Point(int.Parse(lsXEnd), int.Parse(lsYEnd));
            }

            if (startCoords.IsEmpty)
            {
                logger.Warn("could not find startCoords", SECTION);
                return false;
            }

            logger.Info("moveMouse "+startCoords.ToString(), SECTION);
            moveMouse(startCoords,lsShowMove=="true");

            lsCommand=lsCommand.ToLower();

            //LEFTDOWN = 0x02; LEFTUP = 0x04; RIGHTDOWN = 0x08; RIGHTUP = 0x10;
            uint mouseCmd=0x02;
            if (lsCommand.Contains("right"))mouseCmd=0x08;

            if (lsCommand.Contains("double"))
                mouse_event( mouseCmd*3, 0, 0, 0, UIntPtr.Zero); //click
            if (lsCommand.Contains("click"))
                mouse_event( mouseCmd*3, 0, 0, 0, UIntPtr.Zero); //click
            else if (lsCommand.Contains("drag"))
            {
                if (endCoords.IsEmpty)
                {
                    logger.Warn("could not find endCoords for drag", SECTION);
                    return false;
                }
                mouse_event( mouseCmd, 0, 0, 0, UIntPtr.Zero);  //down
                moveMouse(endCoords,true);
                mouse_event( mouseCmd*2, 0, 0, 0, UIntPtr.Zero);  //up
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
                   logger.Warn("could not find cords for process " + lsCtrlProcess + ": " + lsCtrlId, SECTION);
                   try
                   {
                       moveMouse(new Point(int.Parse(lsX), int.Parse(lsY)));
                   }
                   catch { }
               }
               else
               {
                   logger.Info("found the cords for this control", SECTION);
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
                   logger.Info("sending mousevent " + lsCommand + " - " + liCommand.ToString(), SECTION);
                   mouse_event(liCommand, 0, 0, 0, UIntPtr.Zero);
               }
           }
           catch { }
           */
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
           foreach(string lsItem in System.IO.Directory.GetFileSystemEntries(lsSource))
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

       public bool CopyFile(string source, string dest, string overwrite, DataTable loVarTable)
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
               ReplaceEnvVariables(ref source, loVarTable );
               FileInfo fi = new FileInfo(source);
               ReplaceEnvVariables(ref dest, loVarTable);

               //is dest a file or a folder (is the last char a '\' or not)
               if (dest.LastIndexOf("\\") + 1 == dest.Length)
               {
                   //dest is a folder, 
                   destpath = dest;
                   filename = fi.Name;
               }
               else
               {
                   destpath = dest.Substring(0,dest.LastIndexOf("\\"));
                   filename = dest.Substring(dest.LastIndexOf("\\"));
               }

               //check if it exists (or create it) and add file name to dest path
               if (!CheckFolder(destpath, "false", loVarTable))
               {
                   //create folder structure
                   string[] folders = destpath.Split('\\');
                   string foldertocheckandcreate = "";
                   foreach (string folder in folders)
                   {
                       if (folder.Length > 0)
                       {
                           foldertocheckandcreate += (folder + "\\");
                           if (!CheckFolder(foldertocheckandcreate, "false", loVarTable))
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
           catch(Exception)
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
                if(logicalDrives[i].ToUpper().IndexOf(driveletter.ToUpper()) >=0)
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

            if(isDriveletterOK && isDiskPhyiscal)
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

        public bool CheckFile(string path, DataTable loVarTable)
        {
            
            ReplaceEnvVariables(ref path, loVarTable);
            if (File.Exists(path))
                return true;
            else
                return false;
            
        }


        public bool CheckVariable(string var, string value, DataTable loVarTable)
        {
            bool lbReturn = false;
            logger.Info("checking variable " + var + " with value " + value,SECTION);

            try
            {
                foreach (DataRow loRow in loVarTable.Rows)
                {
                    logger.Info("checking variable enum " + loRow["name"].ToString() + " with value " + loRow["value"].ToString(),SECTION);

                    if (loRow["name"].ToString().ToLower() == var.ToLower())
                    {                        
                        logger.Info("found variable " + loRow["name"].ToString() + " with value " + loRow["value"].ToString(),SECTION);
                        logger.Info("value must be " + value,SECTION);
                        if(value==loRow["value"].ToString())
                        {
                            lbReturn = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) { logger.Info(ex.Message,SECTION); }
            return lbReturn;
        }

        public bool CheckFolder(string path, string contentrequired, DataTable loVarTable)
        {
            ReplaceEnvVariables(ref path, loVarTable);
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

            logger.Info("reading registry: hive=" + hive + ", path=" + path, SECTION);

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
                logger.Info("reading registry: could not find path: hive=" + hive + ", path=" + path, SECTION);
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
            string hive = path.ToUpper().Substring(0,4);
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
                            processToKill.Kill();
                    }
                }
            }
            return;
        }

        public int AddRegKey(string path, string name, string value, string valuekind, DataTable loVarTable, ref string errormsg)
        {
            //add reg key
            string hive = path.ToUpper().Substring(0, 4);
            RegistryKey regKey = null;
            path = path.ToUpper().Replace(hive + "\\", "");
            int rtvalue = 0;

            //(Default) - case
            if (name.ToLower() == "(default)")
            {
                name = "";
            }

            ReplaceEnvVariables(ref value, loVarTable);

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
            return rtvalue;
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
                            //Registry.LocalMachine.DeleteSubKey(path,);
                            Registry.LocalMachine.DeleteSubKeyTree(path);
                            break;
                        case "HKCU":
                            //Registry.CurrentUser.DeleteSubKey(path, false);
                            Registry.CurrentUser.DeleteSubKeyTree(path);
                            break;
                        case "HKCR":
                            //Registry.ClassesRoot.DeleteSubKey(path, false);
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

        public int RemoveFileOrFolder(string path, DataTable loVarTable)
        {
            ReplaceEnvVariables(ref path, loVarTable);

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
            catch(Exception)
            {
                //logging
                return 1;
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
        public string ExecuteCMD(string cmdline,string parameters, ref string errormsg, string wait, DataTable loVarTable)
        { 
            Process currentProcess = Process.GetCurrentProcess();
            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));
                        
            ReplaceEnvVariables(ref cmdline, loVarTable);
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

        public string ExecuteCMD(string cmdline, string parameters, ref string errormsg, string wait, string windowsstyle, DataTable loVarTable)
        {
            Process currentProcess = Process.GetCurrentProcess();
            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));

            ReplaceEnvVariables(ref cmdline, loVarTable);
            ReplaceEnvVariables(ref parameters, loVarTable);
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
            switch(windowsstyle.ToLower())
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
                toReplace = toReplace.Replace("[OSLANGUAGEID]",  GetOSLanguage());
            }
        }

        public void ReplaceEnvVariables(ref string path, DataTable loVarTable)
        {
            String pathleft = "";
            String pathmiddle = "";
            String pathright = "";
            Int32 positionFound = 0;

            //retrieve all environment variables
            IDictionary envvariables = Environment.GetEnvironmentVariables();
            foreach (string envvariable in envvariables.Keys)
            {
                if(path.IndexOf("%") >= 0)
                {
                    //"%" found, there must be an environment variable in the string!
                    if (path.ToUpper().IndexOf("%" + envvariable.ToUpper() + "%") >= 0)
                    {
                        //existing environment variable found in the string!
                        positionFound = path.ToUpper().IndexOf("%" + envvariable.ToUpper() + "%");

                        //replace %[]% with its value
                        pathleft = path.Substring(0,positionFound);
                        pathmiddle = path.Substring(positionFound, envvariable.Length + 2);
                        pathmiddle = pathmiddle.ToUpper().Replace("%" + envvariable.ToUpper() + "%", envvariables[envvariable].ToString());
                        pathright = path.Substring(positionFound + envvariable.Length + 2);
                        
                        //path = path.ToUpper().Replace("%" + envvariable.ToUpper() + "%", envvariables[envvariable].ToString());

                        path = pathleft + pathmiddle + pathright;
                    }
                }
            }

            //replace datetime

            path = Regex.Replace(path, "%Time%", DateTime.Now.ToString("yyyy-MM-dd"), RegexOptions.IgnoreCase);
            path = Regex.Replace(path, "%Date%", DateTime.Now.ToString("HH:mm:ss"), RegexOptions.IgnoreCase);
            path = Regex.Replace(path, "%DateTime%", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"), RegexOptions.IgnoreCase);
            path = Regex.Replace(path, "%CurrentFolder%", System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            
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

            foreach(DataRow loRow in loVarTable.Rows)
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
                            logger.Warn("GetInstalledGUID; Package '" + product.ToString() + "': error while getting ProductInfo",SECTION);
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
            catch(Exception)
            {
                return false;
            }
        }

        public bool StartAltirisTask(string name, string pathToScript, DataTable loVarTable)
        {
            string taskID = "";
            string errormessage = "";
            
            try
            {
                taskID = GetAltirisTaskID(name, loVarTable);
                if (taskID == "")
                {
                    //Altiris NOT found --> logging?
                    return false;
                }
                else
                {
                    string returnvalue = ExecuteCMD(pathToScript, "ID=" + taskID, ref errormessage,"true", loVarTable);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetAltirisTaskID(string taskname, DataTable loVarTable)
        {
            string taskID = "";
            string advertismentsLogPath = "%SYSTEMROOT%\\debug\\A_S_Altiris_Advertisements.log";
            Functions myFunctions = new Functions();
            myFunctions.ReplaceEnvVariables(ref advertismentsLogPath, loVarTable);

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

        public bool AddSourceToSourceList(string productcode,string path, DataTable loVarTable)
        {

            string curGUID = "";
            curGUID = GetInstalledGUID(productcode);

            //Package installed
            if(curGUID != "")
            {
                try
                {
                    Type type = Type.GetTypeFromProgID("WindowsInstaller.Installer");
                    if (type != null)
                    {
                        object myWinInstaller = Activator.CreateInstance(type);
                        object[] arguments = { productcode, "PackageName" };
                        object packagename = type.InvokeMember("ProductInfo", BindingFlags.GetProperty, null, myWinInstaller, arguments);
                    
                        if (CheckFile(path + "\\" + packagename.ToString(), loVarTable) || CheckFile(path + "/" + packagename.ToString(), loVarTable)  )
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
                catch(Exception)
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
                logger.Info("CreateScheduledTask: Postpone",SECTION);
                if (altirisTaskID == "")
                {
                    cmd = GlobalClass.GlobalCommandLine;
                }
                else
                {
                    logger.Info("CreateScheduledTask: Postpone: AltirisTaskID: " + altirisTaskID,SECTION);
                    cmd = GlobalClass.GlobalStartAltirisTask + " ID=" + altirisTaskID;
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
                logger.Info("CreateScheduledTask: Postpone: local TimeZone: '" + localZone.StandardName + "'; Daylight saving time: " + isDaylightSavingTime,SECTION);
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
                logger.Info("CreateScheduledTask: Postpone: Command: '" + cmd + "'",SECTION);
                logger.Info("CreateScheduledTaks: Postpone: StartTime: '" + starttime + lsTimeValue + "'",SECTION);
                inParams["Command"] = cmd;
                inParams["StartTime"] = starttime + lsTimeValue;
                ManagementBaseObject outParams = classInstance.InvokeMethod("Create", inParams, null);
                //uint JobId = ((uint)(outParams.Properties["JobId"].Value));
                logger.Info("CreateScheduledTaks: Postpone: Scheduled Task created",SECTION);

                return ((uint)(outParams.Properties["ReturnValue"].Value));
            }
            catch (Exception ex)
            {
                logger.Info("CreateScheduledTaks: #ERROR: '" + ex.Message + "'",SECTION);
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
                logger.Info("CreateScheduledTask: Reboot",SECTION);


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
                logger.Info("CreateScheduledTask: Reboot: local TimeZone: '" + localZone.StandardName + "'; Daylight saving time: " + isDaylightSavingTime,SECTION);
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
                logger.Info("CreateScheduledTask: Reboot: Command: '" + "shutdown /r" + "'",SECTION);
                logger.Info("CreateScheduledTaks: Reboot: StartTime: '" + starttime + lsTimeValue + "'",SECTION);
                inParams["Command"] = "shutdown /r";
                inParams["StartTime"] = starttime + lsTimeValue;
                ManagementBaseObject outParams = classInstance.InvokeMethod("Create", inParams, null);
                //uint JobId = ((uint)(outParams.Properties["JobId"].Value));
                logger.Info("CreateScheduledTaks: Reboot: Scheduled Task created",SECTION);

                return ((uint)(outParams.Properties["ReturnValue"].Value));
            }
            catch (Exception ex)
            {
                logger.Info("CreateScheduledTaks: #ERROR: '" + ex.Message + "'",SECTION);
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

            if (lsCtrlProc=="") return emptyPoint;

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
            if (lsCtrlId!="")
            {
                condition = new PropertyCondition(AutomationElement.AutomationIdProperty, lsCtrlId);
                AutomationElement element = rootElement.FindFirst(TreeScope.Descendants, condition);
                if (element == null) return emptyPoint;
                System.Windows.Point pt;
                try{
                    if (element.TryGetClickablePoint(out pt)) return (new Point((int)pt.X, (int)pt.Y));
                }
                catch
                {
                    logger.Warn("could not find ClickablePoint for control " + lsCtrlProc + ": " + lsCtrlId, SECTION);
                }
                return emptyPoint;
            }

            //control not found, find coords in window
            if (lsCtrlX=="") return emptyPoint;            
            int ctrlX = int.Parse(lsCtrlX);
            int ctrlY = int.Parse(lsCtrlY);
            System.Windows.Rect mainCoords = rootElement.Current.BoundingRectangle;
            return (new Point((int)mainCoords.X + ctrlX, (int)mainCoords.Y + ctrlY));

        }

        private void moveMouse(Point newPosition, bool showMouse=false)
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

    }
}
