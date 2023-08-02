using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Deliveries.FastWrapper.Logic
{
    public class clsOfflineRegistry
    {

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
        public static extern int AdjustTokenPrivileges(int tokenhandle, int disableprivs, [MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGES Newstate, int bufferlength, int PreivousState, int Returnlength);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegLoadKey(uint hKey, string lpSubKey, string lpFile);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegUnLoadKey(uint hKey, string lpSubKey);

        public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        public const int TOKEN_QUERY = 0x00000008;
        public const int SE_PRIVILEGE_ENABLED = 0x00000002;
        public const string SE_RESTORE_NAME = "SeRestorePrivilege";
        public const string SE_BACKUP_NAME = "SeBackupPrivilege";
        public const uint HKEY_CURRENT_USER = 0x80000001;
        public const uint HKEY_LOCAL_MACHINE = 0x80000002;
        public const uint HKEY_USERS = 0x80000003;

        //temporary hive key
        public string HIVE_SUBKEY = "Test";


        static private Boolean gotPrivileges = false;

        private void GetPrivileges()
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

            try
            {
                retval = OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref token);
            }
            catch { }
            writeToLogFile("retval0: " + retval.ToString());
            retval = AdjustTokenPrivileges(token, 0, ref tpRestore, 1024, 0, 0);
            writeToLogFile("retval1: " + retval.ToString());
            retval = AdjustTokenPrivileges(token, 0, ref tpBackup, 1024, 0, 0);
            writeToLogFile("retval2: " + retval.ToString());

            gotPrivileges = true;
        }

        public string Load(string file)
        {
            HIVE_SUBKEY = Guid.NewGuid().ToString().ToUpper().Replace("-", "");
            writeToLogFile("checking priviledges");
            if (!gotPrivileges)
            {
                GetPrivileges();
                writeToLogFile("getting priviledges: " + gotPrivileges.ToString());
            }
            writeToLogFile("loading key");
            long retVal = -1;
            retVal = RegLoadKey(HKEY_LOCAL_MACHINE, HIVE_SUBKEY, file);
            writeToLogFile("key is " + HIVE_SUBKEY + "retval is " + retVal.ToString());
            if(retVal==1314)
            {

            }
            return HIVE_SUBKEY;
        }

        public void Unload()
        {
            if (!gotPrivileges)
                GetPrivileges();
            int output = RegUnLoadKey(HKEY_LOCAL_MACHINE, HIVE_SUBKEY);
        }
        

        public static void writeToLogFile(string logMessage)
        {

            string strLogMessage = string.Empty;
            string strLogFile = Environment.ExpandEnvironmentVariables(@"%temp%\offlineregistry.log");
            StreamWriter swLog;

            strLogMessage = string.Format("{0}: {1}", DateTime.Now, logMessage);

            try
            {
                if (!File.Exists(strLogFile))
                {
                    swLog = new StreamWriter(strLogFile);
                }
                else
                {
                    swLog = File.AppendText(strLogFile);
                }

                swLog.WriteLine(strLogMessage);

                swLog.Close();
            }
            catch { }





        }
    }
}
