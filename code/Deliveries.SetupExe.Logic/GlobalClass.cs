using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Deliveries.SetupExe.Logic
{
    public class GlobalClass
    {
        private static bool isRebootEnabled = false;
        private static bool isConfirmed = false;
        private static bool isDummy = false;
        private static bool isAborted = false;
        private static bool isPostponed = false;
        private static bool isMainGUIEnabled = false;
        private static bool isUnattended = false;
        private static bool isHelp = false;
        private static bool showPostponeDialog = true;
        private static string dialogMessage = "";
        private static string dialogType = "";
        private static double restartNow = 0;
        private static int exitCode = 0;
        private static int postponeType = 0;
        private static string globalcommandline = "";
        private static string globalstartaltiristask = "";
        private static string logfileproductcode = "";
        private static string productcode = "";
        private static double opacityvalue = 1;
        private static string runningprocess = "";
        private static int timeout = 0;
        private static string applicationpath = "";
        public static Logging logger = new Logging();
        public static string SECTION = "install";

        //ExitCodes
        public const int EC_UnknownException = 1001;
        public const int EC_XMLValidationWarning = 1002;
        public const int EC_XMLValidationError = 1003;
        public const int EC_FileNotFoundException = 1004;
        public const int EC_UnauthorizedAccessException = 1005;
        public const int EC_RemoveFileOrFolder = 1006;
        public const int EC_XMLSchemaException = 1007;
        public const int EC_Postponed = 2001;
        public const int EC_Cancelled = 2002;
        public const int EC_MISSINGLogConfigGeneralXML = 3001;
        public const int EC_MISSINGLogConfigTasksXML = 3002;
        public const int EC_ABORT = 4001;
        public const int EC_NotEnoughDiskspace = 4002;
        public const int EC_NotEnoughMemory = 4003;

        public const string LERROR = "ERROR";

        public static string[] StartMenuFilesBefore;
        public static string[] StartMenuFilesAfter;
        public static string[] DesktopFilesBefore;
        public static string[] DesktopFilesAfter;

        public static DataTable UninstallBefore;
        public static DataTable UninstallAfter;

        public static DataTable VarTable = new DataTable();
        public static DataTable ParameterTable = new DataTable();

        public static string RunningProcess
        {
            get { return runningprocess; }
            set { runningprocess = value; }
        }

        public static double OpacityValue
        {
            get { return opacityvalue; }
            set { opacityvalue = value; }
        }

        public static bool IsHelp
        {
            get { return isHelp; }
            set { isHelp = value; }
        }
        
        public static bool IsMainGUIEnabled
        {
            get { return isMainGUIEnabled; }
            set { isMainGUIEnabled = value; }
        }

        public static bool IsRebootEnabled
        {
            get { return isRebootEnabled; }
            set { isRebootEnabled = value; }
        }

        public static bool IsConfirmed
        {
            get { return isConfirmed; }
            set { isConfirmed = value; }
        }

        public static bool IsDummy
        {
            get { return isDummy; }
            set { isDummy = value; }
        }

        public static bool IsAborted
        {
            get { return isAborted; }
            set { isAborted = value; }
        }

        public static bool IsPostponed
        {
            get { return isPostponed; }
            set { isPostponed = value; }
        }

        public static string DialogMessage
        {
            get { return dialogMessage; }
            set { dialogMessage = value; }
        }

        public static string DialogType
        {
            get { return dialogType; }
            set { dialogType = value; }
        }

        public static int TimeOut
        {
            get { return timeout; }
            set { timeout = value; }

        }

        public static string ApplicationPath
        {
            get { return applicationpath; }
            set { applicationpath = value; }
        }

        //RestartNow
        //-1 --> don't reboot, cancel without rebooting
        //=0 --> now, immediately
        //>0 --> reboot in x minutes
        public static double RestartNow
        {
            get { return restartNow; }
            set { restartNow = value; }
        }

        public static int ExitCode
        {
            get { return exitCode; }
            set { exitCode = value; }
        }

        //postpone type:
        //0 --> go on
        //1 --> postponed
        //2 --> cancelled
        public static int PostponeType
        {
            get { return postponeType; }
            set { postponeType = value; }
        }

        public static bool ShowPostponeDialog
        {
            get { return showPostponeDialog; }
            set { showPostponeDialog = value; }
        }

        public static string GlobalCommandLine
        {
            get { return globalcommandline; }
            set { globalcommandline = value; }
        }

        public static string GlobalStartAltirisTask
        {
            get { return globalstartaltiristask; }
            set { globalstartaltiristask = value; }
        }

        public static string LogFileProductCode
        {
            get { return logfileproductcode; }
            set { logfileproductcode = value; }
        }
        public static string ProductCode
        {
            get { return productcode; }
            set { productcode = value; }
        }

        public static bool IsUnattended
        {
            get { return isUnattended; }
            set { isUnattended = value; }
        }


    }
}
