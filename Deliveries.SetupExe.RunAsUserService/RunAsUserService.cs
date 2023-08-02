using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.ServiceProcess;

namespace CluebizService
{
    public partial class RunAsUserService : ServiceBase
    {
        

        public RunAsUserService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string workingDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\";
            RunCmdAsUser.workingDir= workingDir;
            RunCmdAsUser.log("START workingDir:" + workingDir);

            string cmd = File.ReadAllText(workingDir + "RunAsUserServiceCmd.txt");
            RunCmdAsUser.log("cmd:" + cmd);

            RunCmdAsUser.RunCmd(cmd, "all", false);
            //"LANBOX64\\LocalUser"  "LANBOX64\\danie"
            //@"""C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"""
            //@"""C:\Program Files(x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe"""
            //"cmd.exe"

            File.Delete(workingDir + "RunAsUserServiceCmd.txt");
        }

        protected override void OnStop()
        {
            RunCmdAsUser.log("STOP");
        }


    }
}


//https://stackoverflow.com/questions/15383684/run-a-process-from-a-windows-service-as-the-current-user
internal class RunCmdAsUser
{
    public enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin,
        MaxTokenInfoClass // MaxTokenInfoClass should always be the last enum
    }

    public const int READ_CONTROL = 0x00020000;

    public const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;

    public const int STANDARD_RIGHTS_READ = READ_CONTROL;
    public const int STANDARD_RIGHTS_WRITE = READ_CONTROL;
    public const int STANDARD_RIGHTS_EXECUTE = READ_CONTROL;

    public const int STANDARD_RIGHTS_ALL = 0x001F0000;

    public const int SPECIFIC_RIGHTS_ALL = 0x0000FFFF;

    public const int TOKEN_ASSIGN_PRIMARY = 0x0001;
    public const int TOKEN_DUPLICATE = 0x0002;
    public const int TOKEN_IMPERSONATE = 0x0004;
    public const int TOKEN_QUERY = 0x0008;
    public const int TOKEN_QUERY_SOURCE = 0x0010;
    public const int TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const int TOKEN_ADJUST_GROUPS = 0x0040;
    public const int TOKEN_ADJUST_DEFAULT = 0x0080;
    public const int TOKEN_ADJUST_SESSIONID = 0x0100;

    public const int TOKEN_ALL_ACCESS_P = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT);
    public const int TOKEN_ALL_ACCESS = TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID;
    public const int TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;
    public const int TOKEN_WRITE = STANDARD_RIGHTS_WRITE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT;
    public const int TOKEN_EXECUTE = STANDARD_RIGHTS_EXECUTE;

    public const uint MAXIMUM_ALLOWED = 0x2000000;

    public const int CREATE_NEW_PROCESS_GROUP = 0x00000200;
    public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    public const int IDLE_PRIORITY_CLASS = 0x40;
    public const int NORMAL_PRIORITY_CLASS = 0x20;
    public const int HIGH_PRIORITY_CLASS = 0x80;
    public const int REALTIME_PRIORITY_CLASS = 0x100;

    public const int CREATE_NEW_CONSOLE = 0x00000010;

    public const string SE_DEBUG_NAME = "SeDebugPrivilege";
    public const string SE_RESTORE_NAME = "SeRestorePrivilege";
    public const string SE_BACKUP_NAME = "SeBackupPrivilege";

    public const int SE_PRIVILEGE_ENABLED = 0x0002;

    public const int ERROR_NOT_ALL_ASSIGNED = 1300;

    private const uint TH32CS_SNAPPROCESS = 0x00000002;

    public static int INVALID_HANDLE_VALUE = -1;

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool LookupPrivilegeValue(IntPtr lpSystemName, string lpname, [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

    [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern bool CreateProcessAsUser(IntPtr hToken, String lpApplicationName, String lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment, String lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

    [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
    public static extern bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType, int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool SetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref uint TokenInformation, uint TokenInformationLength);

    [DllImport("userenv.dll", SetLastError = true)]
    public static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, bool bInherit);


    public static string workingDir;

    /// <summary>
    /// Run command as user(s)
    /// </summary>
    /// <param name="CommandLine">command to execute</param>
    /// <param name="sUser"> "all", "active", "LANBOX64\\daniel"</param>
    /// <param name="bElevate">run as admin (not working?)</param>
    /// <returns>on error:-1, else number of succesful user-runs</returns>
    public static int RunCmd(String CommandLine, string sUser = "all", bool bElevate = false)
    {
        // active user session
        uint dwSessionId = WTSGetActiveConsoleSessionId();

        // Find the winlogon process
        var procEntry = new PROCESSENTRY32();
        uint hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (hSnap == INVALID_HANDLE_VALUE) return -1;

        procEntry.dwSize = (uint)Marshal.SizeOf(procEntry); //sizeof(PROCESSENTRY32);
        if (Process32First(hSnap, ref procEntry) == 0) return -1;

        List<KeyValuePair<string, IntPtr>> doUsersList = new List<KeyValuePair<string, IntPtr>>();
        do
        {
            // skip proccess if not 'explorer.exe'
            if (procEntry.szExeFile.IndexOf("explorer.exe") != 0) continue; // "winlogon.exe"

            //get processToken (and userName)
            IntPtr hProcess = IntPtr.Zero, hPToken = IntPtr.Zero;
            string userName = "";
            hProcess = OpenProcess(MAXIMUM_ALLOWED, false, procEntry.th32ProcessID);
            if (OpenProcessToken(hProcess, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY | TOKEN_DUPLICATE | TOKEN_ASSIGN_PRIMARY | TOKEN_ADJUST_SESSIONID | TOKEN_READ | TOKEN_WRITE, ref hPToken))
            {
                userName = new WindowsIdentity(hPToken).Name;
            }
            else
            {
                log(String.Format("RunCmd OpenProcessToken error: {0}", Marshal.GetLastWin32Error()));
                continue;
            }

            var userDone = doUsersList.Where(x => x.Key == userName);
            if (userDone.Count()>0) continue; // userName is already in list

            // different sUser modes (all, active, LANBOX64\\daniel)
            uint winlogonSessId = 0;
            if (ProcessIdToSessionId(procEntry.th32ProcessID, ref winlogonSessId) && winlogonSessId == dwSessionId) 
                doUsersList.Insert(0, new KeyValuePair<string, IntPtr>(userName, hPToken)); //active user as first entry
            else if (sUser == "all") doUsersList.Add(new KeyValuePair<string, IntPtr>(userName, hPToken));
            else if (sUser == userName) doUsersList.Add(new KeyValuePair<string, IntPtr>(userName, hPToken));
            else CloseHandle(hPToken);
            
            CloseHandle(hProcess);
        }
        while (Process32Next(hSnap, ref procEntry) != 0);


        int okRuns=0;
        foreach (var el in doUsersList)
        {
            log("start for user '"+el.Key+"'");
            if (RunCmdWithProcessToken(CommandLine, bElevate, el.Value)) okRuns += 1;
            CloseHandle(el.Value);
        }


        return okRuns;
    }

    private static bool RunCmdWithProcessToken(String CommandLine, bool bElevate, IntPtr hPToken)
    {
        PROCESS_INFORMATION pi;

        IntPtr hUserTokenDup = IntPtr.Zero;
        var luid = new LUID();

        //griner: hUserToken is never used, hPToken is duplicated 
        //IntPtr hUserToken = IntPtr.Zero;
        //WTSQueryUserToken(dwSessionId, ref hUserToken);

        var si = new STARTUPINFO();
        si.cb = Marshal.SizeOf(si);
        si.lpDesktop = "winsta0\\default";
        si.wShowWindow = 1;

        if (!LookupPrivilegeValue(IntPtr.Zero, SE_DEBUG_NAME, ref luid)) log(String.Format("RunCmdWithProcessToken LookupPrivilegeValue error: {0}", Marshal.GetLastWin32Error()));

        var sa = new SECURITY_ATTRIBUTES();
        sa.Length = Marshal.SizeOf(sa);

        if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
        {
            log(String.Format("RunCmdWithProcessToken DuplicateTokenEx error: {0} Token does not have the privilege.", Marshal.GetLastWin32Error()));
            return false;
        }

        // run as elevated? (NOT WORKING!)
        if (false) //bElevate
        {
            uint dwSessionId = WTSGetActiveConsoleSessionId();

            var tp = new TOKEN_PRIVILEGES();

            //tp.Privileges[0].Luid = luid;
            //tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

            tp.PrivilegeCount = 1;
            tp.Privileges = new int[3];
            tp.Privileges[0] = luid.LowPart;
            tp.Privileges[1] = luid.HighPart;
            tp.Privileges[2] = SE_PRIVILEGE_ENABLED;

            if (!SetTokenInformation(hUserTokenDup, TOKEN_INFORMATION_CLASS.TokenSessionId, ref dwSessionId, (uint)IntPtr.Size))
            {
                log(String.Format("RunCmdWithProcessToken SetTokenInformation error: {0} Token does not have the privilege.", Marshal.GetLastWin32Error()));
            }

            //Adjust Token privilege
            if (!AdjustTokenPrivileges(hUserTokenDup, false, ref tp, Marshal.SizeOf(tp), /*(PTOKEN_PRIVILEGES)*/IntPtr.Zero, IntPtr.Zero))
            {
                log(String.Format("RunCmdWithProcessToken AdjustTokenPrivileges error: {0}", Marshal.GetLastWin32Error()));
            }
        }

        uint dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;
        IntPtr pEnv = IntPtr.Zero;
        if (CreateEnvironmentBlock(ref pEnv, hUserTokenDup, true)) dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
        else pEnv = IntPtr.Zero;

        // Launch the process in the client's logon session.
        CreateProcessAsUser(hUserTokenDup, // client's access token
            null, // file to execute
            CommandLine, // command line
            ref sa, // pointer to process SECURITY_ATTRIBUTES
            ref sa, // pointer to thread SECURITY_ATTRIBUTES
            false, // handles are not inheritable
            (int)dwCreationFlags, // creation flags
            pEnv, // pointer to new environment block 
            null, // name of current directory 
            ref si, // pointer to STARTUPINFO structure
            out pi // receives information about new process
            );
        // End impersonation of client.

        CloseHandle(hUserTokenDup);

        //GetLastError is 0 on success
        return (Marshal.GetLastWin32Error() == 0);
    }

    public static void log(string txt)
    {
        File.AppendAllText(workingDir + "RunAsUserServiceLog.txt", DateTime.Now + ": " + txt + "\n");
    }


    [DllImport("kernel32.dll")]
    private static extern int Process32First(uint hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll")]
    private static extern int Process32Next(uint hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hSnapshot);

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    /*
    [DllImport("Wtsapi32.dll")]
    private static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);
    */

    [DllImport("kernel32.dll")]
    private static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("advapi32", SetLastError = true)]
    [SuppressUnmanagedCodeSecurity]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, // handle to process
        int DesiredAccess, // desired access to process
        ref IntPtr TokenHandle);

    #region Nested type: LUID
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        public int LowPart;
        public int HighPart;
    }
    #endregion

    //end struct

    #region Nested type: LUID_AND_ATRIBUTES
    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID_AND_ATRIBUTES
    {
        public LUID Luid;
        public int Attributes;
    }
    #endregion

    #region Nested type: PROCESSENTRY32
    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public readonly uint cntUsage;
        public readonly uint th32ProcessID;
        public readonly IntPtr th32DefaultHeapID;
        public readonly uint th32ModuleID;
        public readonly uint cntThreads;
        public readonly uint th32ParentProcessID;
        public readonly int pcPriClassBase;
        public readonly uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public readonly string szExeFile;
    }
    #endregion

    #region Nested type: PROCESS_INFORMATION
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }
    #endregion

    #region Nested type: SECURITY_ATTRIBUTES
    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
    #endregion

    #region Nested type: SECURITY_IMPERSONATION_LEVEL
    private enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous = 0,
        SecurityIdentification = 1,
        SecurityImpersonation = 2,
        SecurityDelegation = 3,
    }
    #endregion

    #region Nested type: STARTUPINFO
    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFO
    {
        public int cb;
        public String lpReserved;
        public String lpDesktop;
        public String lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }
    #endregion

    #region Nested type: TOKEN_PRIVILEGES
    [StructLayout(LayoutKind.Sequential)]
    internal struct TOKEN_PRIVILEGES
    {
        internal int PrivilegeCount;
        //LUID_AND_ATRIBUTES
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        internal int[] Privileges;
    }
    #endregion

    #region Nested type: TOKEN_TYPE
    private enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation = 2
    }
    #endregion

    // handle to open access token
}













/* OLD
namespace CluebizService
{
    public partial class RunAsUserService : ServiceBase
    {
        Thread th;
        bool isRunning = false;

        public RunAsUserService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            th=new Thread(serviceThread);
            th.Start();
            isRunning=true;
        }
        private void serviceThread() {
            RegistryKey rk = Registry.LocalMachine.CreateSubKey("Software\\cluebiz\\deliveries_setup\\usertasks", true);
            LaunchAsUser launcher = new LaunchAsUser();

            while (isRunning)
            {
                string[] taskNames = rk.GetValueNames();
                Array.Sort(taskNames);
                foreach (string taskName in taskNames)
                {
                    string json = rk.GetValue(taskName).ToString().Replace("\\", "\\\\");
                    dynamic obj;
                    try { obj = JObject.Parse(json); }
                    catch { continue; }

                    //remove old tasks
                    if(obj.ContainsKey("enddate")){
                        try
                        {
                            DateTime enddate = (DateTime)obj.enddate;
                            if (DateTime.Now > enddate){
                                rk.DeleteValue(taskName);
                                continue;
                            }
                        }
                        catch { }
                    }

                    //skip done tasks
                    if (obj.status=="done") continue;

                    //launch task
                    if (obj.ContainsKey("cmd")) {
                        string cmd = (string)obj.cmd;
                        if (obj.ContainsKey("param")) cmd +=" "+(string)obj.param;

                        obj.status="running";
                        json = obj.ToString(Newtonsoft.Json.Formatting.None).Replace(@"\\", @"\");
                        rk.SetValue(taskName, json);

                        launcher.LaunchProcess(cmd, (string)obj.workdir, (string)obj.waitproc== "true");
                        obj.status="done";
                    }
                    else obj.status="ERROR:cmd missing";

                    json = obj.ToString(Newtonsoft.Json.Formatting.None).Replace(@"\\", @"\"); ;
                    rk.SetValue(taskName, json);
                }
                Thread.Sleep(2000);
            }
            rk.Close();
        }
        protected override void OnStop()
        {
            isRunning=false;
            th = null;
        }
    }




    class LaunchAsUser
    {
        static public IntPtr WTS_CURRENT_SERVER_HANDLE = (IntPtr)0;

        [DllImport("wtsapi32", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        static extern bool WTSEnumerateSessions(IntPtr hServer, int Reserved, uint Version, out IntPtr ppSessionInfo, out int pCount);

        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public extern static void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public extern static bool WTSQueryUserToken(int SessionId, out IntPtr phToken);

        public enum WTSConnectState
        {
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO
        {
            public int SessionId;
            [MarshalAs(UnmanagedType.LPTStr)]
            public String pWinStationName;
            public WTSConnectState State;
        }

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public extern static bool CloseHandle(IntPtr handle);

        public const int LOGON32_LOGON_INTERACTIVE = 2;
        public const int LOGON32_PROVIDER_DEFAULT = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpReserved;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpDesktop;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUserA", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public extern static bool CreateProcessAsUser(IntPtr hToken, [MarshalAs(UnmanagedType.LPStr)] string lpApplicationName, [MarshalAs(UnmanagedType.LPStr)] string lpCommandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandle, uint dwCreationFlags, IntPtr lpEnvironment,
            [MarshalAs(UnmanagedType.LPStr)] string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        /// <summary>launch a process from all active users</summary>
        /// <param name="process">'process parameters'</param>
        /// <param name="dir">run in dir</param>
        /// <param name="single">start single process and wait for end</param>
        public bool LaunchProcess(string process, string dir=null, bool single=false)
        {
            IntPtr ppBuffer = IntPtr.Zero;
            IntPtr processHandle= IntPtr.Zero;
            int count;

            // enumerate the sessions
            if (!WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, out ppBuffer, out count))
            {
                //Console.WriteLine("WTSEnumerateSessions failed with " + Marshal.GetLastWin32Error().ToString());
                return false;
            }

            WTS_SESSION_INFO wsi = new WTS_SESSION_INFO();

            UInt32 StructSize = (UInt32)Marshal.SizeOf(typeof(WTS_SESSION_INFO));
            IntPtr CurrentStruct;
            IntPtr hToken;

            for (int i = 0; i < count; i++)
            {
                CurrentStruct = (IntPtr)(ppBuffer.ToInt64() + (StructSize * i));
                wsi = (WTS_SESSION_INFO)(Marshal.PtrToStructure(CurrentStruct, typeof(WTS_SESSION_INFO)));
                //Console.WriteLine("Session: " + wsi.SessionId.ToString());
                //Console.WriteLine("State:   " + wsi.State.ToString());

                if (wsi.State == WTSConnectState.Active)
                {
                    if (!WTSQueryUserToken(wsi.SessionId, out hToken))
                    {
                        //Console.WriteLine("WTSQueryUserToken failed with " + Marshal.GetLastWin32Error().ToString());
                        return false;
                    }

                    bool ret;

                    STARTUPINFO si = new STARTUPINFO();
                    si.cb = Marshal.SizeOf(si);

                    PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

                    ret = CreateProcessAsUser(hToken, null, process, IntPtr.Zero, IntPtr.Zero, true, 0, IntPtr.Zero, dir, ref si, out pi);
                    if (ret != false) {
                        //Console.WriteLine("CreateProcessAsUser SUCCESS.  The child PID is " + pi.dwProcessId.ToString());

                        if (single)
                        {
                            Process proc = Process.GetProcessById((int)pi.dwProcessId);
                            proc.WaitForExit();
                        }

                        // close Handles
                        CloseHandle(pi.hProcess);
                        CloseHandle(pi.hThread);
                    }
                    
                    //else
                    //{
                    //    Console.WriteLine("CreateProcessAsUser failed with " + Marshal.GetLastWin32Error().ToString());
                    //}
                    

                    CloseHandle(hToken);
                }
            }

            WTSFreeMemory(ppBuffer);
            return true;
        }
    }
}
*/