using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using System.Threading;
using System.Security.Principal;
using System.Management;
using System.IO;
using System.Security;
using System.Security.AccessControl;
using System.Drawing.Text;
using Microsoft.Win32;
using System.Globalization;
using Trinet.Core.IO.Ntfs;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Deliveries.SetupExe.Logic;
using System.Reflection;
using System.Windows.Interop;

namespace SetupExe
{

    public partial class formMain : Telerik.WinControls.UI.RadForm
    {

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        #region Consts and Vars



        //other consts & variables
        const string TASKNAME = "Software Distribution";
        const string LINFO = "INFO";
        const double OPACITY = 0.92;
        const string XMLSCHEMAFILE = "deploy.xsd";
        const string XMLDEFAULTFILE = "deploy.xml";

        private string productname = "";
        private string productversion = "";
        private string productcode = "";
        private string manufacturer = "";
        private string architecture = "";
        private string packager = "";
        private string creationdate = "";
        private string type = "";
        private string internalversion = "";
        private string maininstallfolderx86 = "";
        private string maininstallfolderx64 = "";
        private string maininstallexe = "";
        private string versiondetect = "";
        private string versiondetectpatch = "";

        private bool hasuserinstall = false;
        private bool hasuseruninstall = false;
        private bool hasuserrun = false;


        private string parameterXMLFile = "";
        private string parameterTYPE = "i";
        private string parameterLOGPath = "";

        private string LOG_EXCEPTIONS_PATH = "%SYSTEMROOT%\\debug\\" + Application.CompanyName + "_setupEXE\\";
        private string LOG_EXCEPTIONS_FILE = DateTime.Now.Year.ToString() + "_" + DateTime.Now.ToString("MM") + "_APPL_Exceptions.log";
        private string LOG_EXESTART_FILE = DateTime.Now.Year.ToString() + "_" + DateTime.Now.ToString("MM") + "_APPL_Starts.log";
        private string LOG_TASKS = "%SYSTEMROOT%\\debug\\" + Application.CompanyName + "_setupEXE_tasks\\";
        private string LOG_TASKS_FILE = "[ProductCode].log";
        private string LOG_MSI = "%SYSTEMROOT%\\debug\\MSIPackages\\";
        private string LOG_MSI_FILE = "[ProductCode].log";
        private string ACTIVESETUPLANGUAGE = "";

        const string STARTALTIRISTASKSCRIPT = "startAltirisTask.vbs";
        private string startAltirisTaskScriptPath = "%programfiles%\\" + Application.CompanyName + "\\" + Application.CompanyName + "_setupExe\\";
        private string typelabel = "Installation"; //default (Installation/Uninstallation/Repair)
        private string XSDversion = "unknown";
        private string loggedOnUser = "unknown";


        bool abort = false;
        bool isUserLoggedOn = false;
        bool bSleepStop = false;
        bool isFullySilent = false;

        //GUI
        const int HEIGHT_MAINFORM = 270;
        const int HEIGHT_CHILDFORM = 150;
        const int HEIGHT_SPACE = 4;

        private PrivateFontCollection MYpfc = new PrivateFontCollection();

        Process pc = Process.GetCurrentProcess();


        //my own logger class


        #endregion

        private string ReplaceLogVars(string lsName)
        {
            string lsReturn = lsName;
            lsReturn = lsReturn.Replace("[Year]", DateTime.Now.Year.ToString());
            lsReturn = lsReturn.Replace("[MM]", DateTime.Now.ToString("MM"));
            if (productname!="")
            {
                lsReturn = lsReturn.Replace("[ProductName]", productname);
            }
            if (manufacturer != "")
            {
                lsReturn = lsReturn.Replace("[Manufacturer]", manufacturer);
            }
            if (productversion != "")
            {
                lsReturn = lsReturn.Replace("[ProductVersion]", productversion);
            }
            return lsReturn;
        }

        public formMain(string[] args)
        {
            Deliveries.SetupExe.Logic.Functions myFunctions = new Deliveries.SetupExe.Logic.Functions();

            GlobalClass.ApplicationPath = Application.ExecutablePath;

            //Initialize form components
            InitializeComponent();



            //disable Main form
            this.Visible = false;
            //this.Enabled = false;

            try
            {
                //get Data from XML
                const string XMLNAMESPACE = "deploy";

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(parameterXMLFile);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace(XMLNAMESPACE, nsmgr.DefaultNamespace.ToString());

                //get metadata
                try
                {
                    foreach (XmlNode nd in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":metadata", nsmgr))
                    {
                        XmlElement package = nd as XmlElement;
                        XmlElement elproductname = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":productname", nsmgr);
                        productname = elproductname.InnerText;
                        XmlElement elproductversion = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":productversion", nsmgr);
                        productversion = elproductversion.InnerText;
                        XmlElement elproductcode = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":productcode", nsmgr);
                        productcode = elproductcode.InnerText;
                        XmlElement elmanufacturer = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":manufacturer", nsmgr);
                        manufacturer = elmanufacturer.InnerText;
                        XmlElement elarchitecture = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":architecture", nsmgr);
                        architecture = elarchitecture.InnerText;
                        XmlElement elpackager = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":packager", nsmgr);
                        packager = elpackager.InnerText;
                        XmlElement elcreationdate = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":creationdate", nsmgr);
                        creationdate = elcreationdate.InnerText;
                        XmlElement eltype = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":type", nsmgr);
                        type = eltype.InnerText;
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":internalversion", nsmgr);
                            internalversion = eltype2.InnerText;
                        }
                        catch { }
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":maininstallfolderx86", nsmgr);
                            maininstallfolderx86 = eltype2.InnerText;
                        }
                        catch { }
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":maininstallfolderx64", nsmgr);
                            maininstallfolderx64 = eltype2.InnerText;
                        }
                        catch { }
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":maininstallexe", nsmgr);
                            maininstallexe = eltype2.InnerText;
                        }
                        catch { }
                    }
                }
                catch { }


                //get configuration
                try
                {
                    foreach (XmlNode nd in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":configuration", nsmgr))
                    {
                        foreach (XmlNode sd in nd.ChildNodes)
                        {
                            try
                            {


                                if (sd.Attributes["name"].Value == "versiondetect")
                                {
                                    try
                                    {
                                        versiondetect = sd.Attributes["value"].Value;
                                    }
                                    catch { }
                                }
                                if (sd.Attributes["name"].Value == "versiondetectpatch")
                                {
                                    try
                                    {
                                        versiondetectpatch = sd.Attributes["value"].Value;
                                    }
                                    catch { }
                                }

                                if (sd.Attributes["type"].Value == "checksum")
                                {
                                    
                                    if (System.IO.File.Exists(GlobalClass.ApplicationPath + @"\" + sd.Attributes["name"].Value))
                                    {

                                    }
                                }

                            }
                            catch { }
                            
                        }
                    }
                }
                catch { }



            }
            catch { }

            try
            {
                if (Properties.Settings.Default.LogfilePath != "")
                {
                    LOG_TASKS = ReplaceLogVars(Properties.Settings.Default.LogfilePath);
                }
            }
            catch { }
            try
            {
                if (Properties.Settings.Default.LogfileName != "")
                {
                    LOG_TASKS_FILE = ReplaceLogVars(Properties.Settings.Default.LogfileName);
                }
            }
            catch { }
            try
            {
                if (Properties.Settings.Default.ExeLogfileName != "")
                {
                    LOG_MSI_FILE = ReplaceLogVars(Properties.Settings.Default.ExeLogfileName);
                }
            }
            catch { }
            try
            {
                if (Properties.Settings.Default.ExeLogfilePath != "")
                {
                    LOG_MSI = ReplaceLogVars(Properties.Settings.Default.ExeLogfilePath);
                }
            }
            catch { }
            try
            {
                if (Properties.Settings.Default.ExceptionLogfilePath != "")
                {
                    LOG_EXCEPTIONS_PATH = ReplaceLogVars(Properties.Settings.Default.ExceptionLogfilePath);
                }
            }
            catch { }
            try
            {
                if (Properties.Settings.Default.ExceptionlLogfileName != "")
                {
                    LOG_EXCEPTIONS_FILE = ReplaceLogVars(Properties.Settings.Default.ExceptionlLogfileName);
                }
            }
            catch { }
            try
            {
                if (Properties.Settings.Default.GeneralLogfilePath != "")
                {
                    LOG_EXCEPTIONS_PATH = ReplaceLogVars(Properties.Settings.Default.ExceptionLogfilePath);
                }
            }
            catch { }
            try
            {
                if (Properties.Settings.Default.GeneralLogfileName != "")
                {

                    LOG_EXCEPTIONS_FILE = ReplaceLogVars(Properties.Settings.Default.ExceptionlLogfileName);

                }
            }
            catch { }

            try
            {
                if (Properties.Settings.Default.ActiveSetupLanguage != "")
                {
                    ACTIVESETUPLANGUAGE = ReplaceLogVars(Properties.Settings.Default.ActiveSetupLanguage);
                }
            }
            catch { }

            GlobalClass.VarTable = new DataTable();
            GlobalClass.VarTable.Columns.Add("name", typeof(string));
            GlobalClass.VarTable.Columns.Add("value", typeof(string));

            GlobalClass.ParameterTable = new DataTable();
            GlobalClass.ParameterTable.Columns.Add("name", typeof(string));
            GlobalClass.ParameterTable.Columns.Add("value", typeof(string));


            //alternative Header Pic (setup.png)
            string picPath = Application.StartupPath + @"\setup.png";
            if (File.Exists(picPath))
            {
                Image img = Image.FromFile(picPath);
                this.pictureBoxHeaderPic.Image = img;
                this.pictureBoxHeaderPic.SizeMode = PictureBoxSizeMode.CenterImage;
            }

            //apply texts to Notify Icon
            this.notifyIcon1.BalloonTipText = Application.CompanyName + " Software Setup\r\n----------------------\r\n (c) 2009-2019 " + Application.CompanyName + "\r\n";
            this.notifyIcon1.BalloonTipTitle = TASKNAME;
            this.notifyIcon1.Text = TASKNAME;



            string currentPath = pc.MainModule.FileName.Substring(0, pc.MainModule.FileName.LastIndexOf(@"\"));
            string currentExecutable = pc.MainModule.FileName;
            string parameterstotal = "";


            ResetTalkResponse();


            //unblock
            foreach (FileInfo file in new System.IO.DirectoryInfo(Application.StartupPath).GetFiles())
            {

                try
                {
                    if (file.AlternateDataStreamExists("Zone.Identifier"))
                    {
                        GlobalClass.logger.Info("found zone identifier stream in " + file.FullName, GlobalClass.SECTION);

                        AlternateDataStreamInfo s = file.GetAlternateDataStream("Zone.Identifier", FileMode.Open);
                        using (TextReader reader = s.OpenText())
                        {
                            GlobalClass.logger.Info("zone identifier content " + reader.ReadToEnd(), GlobalClass.SECTION);
                        }

                        s.Delete();
                    }

                    file.DeleteAlternateDataStream("Zone.Identifier");
                }
                catch { }
            }


            //                        
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }

            if (!GlobalClass.IsAborted)
            {


                //get command line parameters "XML" & "TYPE"
                foreach (string parameter in args)
                {
                    bool lbDoneSomething = false;
                    parameterstotal = " " + parameterstotal + parameter;
                    if (parameter.ToUpper().IndexOf("XML=") >= 0)
                    {
                        lbDoneSomething = true;
                        parameterXMLFile = parameter.ToUpper().Replace("XML=", "");
                    }
                    if (parameter.ToUpper().IndexOf("TYPE=") >= 0)
                    {
                        lbDoneSomething = true;
                        parameterTYPE = parameter.ToUpper().Replace("TYPE=", "");
                    }
                    if (parameterTYPE=="D")
                    {
                        lbDoneSomething = true;
                        GlobalClass.IsDummy = true;
                        GlobalClass.IsAborted = true;
                    }
                    if (parameter.ToUpper().IndexOf("LOGPATH=") >= 0)
                    {
                        lbDoneSomething = true;
                        parameterLOGPath = parameter.ToUpper().Replace("LOGPATH=", "");
                        parameterLOGPath = parameterLOGPath.Replace("\"", "");
                    }
                    if (parameter.ToUpper().IndexOf("ACTIVESETUPLANGUAGE=") >=0)
                    {
                        lbDoneSomething = true;
                        ACTIVESETUPLANGUAGE = "";
                        try
                        {
                            string lsParameterValue = "";
                            try
                            {
                                lsParameterValue = parameter.Substring(parameter.IndexOf("=") + 1);
                            }
                            catch { }
                            lsParameterValue = lsParameterValue.Trim();
                            if (lsParameterValue != "")
                            {
                                ACTIVESETUPLANGUAGE = lsParameterValue;
                            }
                        }
                        catch { }
                    }
                    if (parameter.ToUpper().IndexOf("/?") >= 0)
                    {
                        lbDoneSomething = true;
                        GlobalClass.IsHelp = true;
                        GlobalClass.IsAborted = true;
                    }
                    if (parameter.ToUpper().IndexOf("/SILENT")>=0)
                    {
                        lbDoneSomething = true;
                        isFullySilent = true;
                    }
                    if (!lbDoneSomething)
                    {
                        if (parameter.Contains("="))
                        {
                            string lsParameterName = "";
                            string lsParameterValue = "";
                            try
                            {
                                lsParameterName = parameter.Substring(0, parameter.IndexOf("="));
                            }
                            catch { }
                            try
                            {
                                lsParameterValue = parameter.Substring(parameter.IndexOf("=") + 1);
                            }
                            catch { }
                            lsParameterName = lsParameterName.Trim();
                            lsParameterValue = lsParameterValue.Trim();
                            if (lsParameterName!="")
                            {
                                DataRow loNewRow = GlobalClass.ParameterTable.NewRow();
                                loNewRow["name"] = lsParameterName;
                                loNewRow["value"] = lsParameterValue;
                                GlobalClass.ParameterTable.Rows.Add(loNewRow);
                            }
                        }
                    }
                }

                if (!GlobalClass.IsHelp)
                {
                    //prepare directories
                    if (parameterLOGPath.Length > 1)
                    {
                        LOG_TASKS = parameterLOGPath + "\\" + Application.CompanyName + "_setupEXE_tasks\\";
                        LOG_MSI = parameterLOGPath + "\\MSIPackages\\";
                        LOG_EXCEPTIONS_PATH = parameterLOGPath + "\\" + Application.CompanyName + "_setupEXE\\";
                        LOG_EXCEPTIONS_FILE = DateTime.Now.Year.ToString() + "_" + DateTime.Now.ToString("MM") + "_APPL_Exceptions.log";
                        LOG_EXESTART_FILE = DateTime.Now.Year.ToString() + "_" + DateTime.Now.ToString("MM") + "_APPL_Starts.log";
                    }

                    myFunctions.ReplaceEnvVariables(ref LOG_EXCEPTIONS_PATH, new DataTable(), new DataTable());
                    myFunctions.ReplaceEnvVariables(ref LOG_TASKS, new DataTable(), new DataTable());
                    myFunctions.ReplaceEnvVariables(ref LOG_MSI, new DataTable(), new DataTable());

                    try
                    {
                        Directory.CreateDirectory(LOG_TASKS);
                    }
                    catch { }
                    try
                    {
                        Directory.CreateDirectory(LOG_EXCEPTIONS_PATH);
                    }
                    catch { }
                    try
                    {
                        Directory.CreateDirectory(LOG_MSI);
                    }
                    catch
                    {
                        //LOG_TASKS = "%TEMP%\\debug\\" + Application.CompanyName + "_setupEXE_tasks\\";
                        //LOG_EXCEPTIONS_PATH = "%TEMP%\\debug\\" + Application.CompanyName + "_setupEXE\\";
                        //LOG_MSI = "%TEMP%\\debug\\MSIPACKAGES\\";
                        //myFunctions.ReplaceEnvVariables(ref LOG_TASKS);
                        //myFunctions.ReplaceEnvVariables(ref LOG_EXCEPTIONS_PATH);
                        //myFunctions.ReplaceEnvVariables(ref LOG_MSI);
                        //try
                        //{
                        //    Directory.CreateDirectory(LOG_TASKS);
                        //    Directory.CreateDirectory(LOG_EXCEPTIONS_PATH);
                        //    Directory.CreateDirectory(LOG_MSI);
                        //}
                        //catch(Exception ex)
                        //{
                        //    HandleExceptions(ex, EC_UnknownException);
                        //}
                    }

                    //directory security
                    try
                    {
                        string SID = "S-1-1-0"; //everyone
                        SecurityIdentifier sid = new SecurityIdentifier(SID);

                        DirectoryInfo directoryInfoT = new DirectoryInfo(LOG_TASKS);
                        DirectorySecurity directorySecurityT = directoryInfoT.GetAccessControl();
                        directorySecurityT.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
                        directoryInfoT.SetAccessControl(directorySecurityT);

                        DirectoryInfo directoryInfoE = new DirectoryInfo(LOG_EXCEPTIONS_PATH);
                        DirectorySecurity directorySecurityE = directoryInfoE.GetAccessControl();
                        directorySecurityE.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
                        directoryInfoE.SetAccessControl(directorySecurityE);

                        DirectoryInfo directoryInfoM = new DirectoryInfo(LOG_MSI);
                        DirectorySecurity directorySecurityM = directoryInfoM.GetAccessControl();
                        directorySecurityM.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
                        directoryInfoM.SetAccessControl(directorySecurityM);
                    }
                    catch (Exception)
                    {
                        //do nothing?
                        //MessageBox.Show(ex.Message);
                    }

                    //set global command line (for scheduled tasks) & generate script.
                    GlobalClass.GlobalCommandLine = (char)34 + currentExecutable + (char)34 + parameterstotal;
                    myFunctions.ReplaceEnvVariables(ref startAltirisTaskScriptPath, new DataTable(), new DataTable());
                    GlobalClass.GlobalStartAltirisTask = "wscript.exe " + (char)34 + startAltirisTaskScriptPath + STARTALTIRISTASKSCRIPT + (char)34;

                    //removed by adrian
                    //CreateScriptFile(startAltirisTaskScriptPath, STARTALTIRISTASKSCRIPT);

                    //get application version
                    Version vrs = new Version(Application.ProductVersion);

                    //get 'current user' and 'run as - user' 
                    WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                    String username = wp.Identity.Name;
                    loggedOnUser = GetLoggedOnUser();
                    if (loggedOnUser.Length > 0)
                    {
                        isUserLoggedOn = true;
                    }

                    isUserLoggedOn = true;


                    //get info






                    //get directory size
                    //double TotalDirectorySize = Folder.Size(@currentPath, true);

                    //logging start
                    myFunctions.UpdateFileAppenderPath(LOG_EXCEPTIONS_PATH + LOG_EXESTART_FILE);
                    GlobalClass.logger.Info("--------------------------------------------------------------------------", GlobalClass.SECTION);
                    GlobalClass.logger.Info("Start '" + pc.MainModule.FileName + "'", GlobalClass.SECTION);
                    GlobalClass.logger.Info("MetaData: '" + manufacturer + " " + productname + "';" + productcode + ";" + productversion + ";" + type + ";" + packager + ";" + architecture + ";" + creationdate, GlobalClass.SECTION);
                    GlobalClass.logger.Info("Application Version: " + vrs.Major + "." + vrs.Minor + "." + vrs.MajorRevision + "." + vrs.MinorRevision, GlobalClass.SECTION);
                    GlobalClass.logger.Info("Parameter: 'XML=" + parameterXMLFile + " TYPE=" + parameterTYPE + " LOGPATH=" + "'", GlobalClass.SECTION);
                    GlobalClass.logger.Info("Working Folder: " + GlobalClass.ApplicationPath, GlobalClass.SECTION);
                    GlobalClass.logger.Info("Runtime Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString(), GlobalClass.SECTION);
                    GlobalClass.logger.Info("Run as: '" + username + "'", GlobalClass.SECTION);
                    GlobalClass.logger.Info("At least one user is logged on: '" + isUserLoggedOn + "'", GlobalClass.SECTION);
                    GlobalClass.logger.Info("Current logged on user: '" + loggedOnUser + "'", GlobalClass.SECTION);
                    GlobalClass.logger.Info("Current (source) folder: '" + currentPath + "'", GlobalClass.SECTION);
                    //logger.Info("Total folder size: " + TotalDirectorySize.ToString() + " Bytes");
                    GlobalClass.logger.Info("Interactive mode: " + Environment.UserInteractive.ToString(), GlobalClass.SECTION);
                    GlobalClass.logger.WritetoEventLog("Start '" + pc.MainModule.FileName + "'; " + "Parameter: 'XML=" + parameterXMLFile + " TYPE=" + parameterTYPE + "'", LINFO, GlobalClass.SECTION);

                    //set new location on screen (bottom right)
                    this.Location = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width - this.Width - 10, Screen.PrimaryScreen.Bounds.Height - this.Height - 80);



                }
            }
        }

        private void formMain_Load(object sender, EventArgs e)
        {

            System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("en-US");
            this.WindowState = FormWindowState.Minimized;


            if (!GlobalClass.IsAborted)
            {
                Thread ta = new Thread(new ThreadStart(StartWorking));
                ta.CurrentCulture = cultureInfo;
                ta.Start();
            }
            else
            {
                if (GlobalClass.IsHelp)
                {
                    //get application version
                    Version vrs = new Version(Application.ProductVersion);

                    //build help message string
                    string helpmsg = Application.ProductName + "\r\nApplication Version: " + vrs.Major + "." + vrs.Minor + "." + vrs.MajorRevision + "." + vrs.MinorRevision + "\r\n\n";
                    helpmsg += Application.ProductName + " [Optional Parameter]\r\n\n";
                    helpmsg += "\tXML=<" + XMLDEFAULTFILE + ">\r\n";
                    helpmsg += "\t\trelative or absolute path to the configuration .xml file\r\n\n";
                    helpmsg += "\tTYPE=[i|x|f|ui|ux|uf]\r\n";
                    helpmsg += "\t\tInstallation type (Install, Uninstall, Repair, UserInstall, UserUninstall, UserRepair, UserRun, Snapshot, Testing)\r\n";
                    helpmsg += "\t\ti - install\r\n";
                    helpmsg += "\t\tx - uninstall\r\n";
                    helpmsg += "\t\tf - repair\r\n\n";
                    helpmsg += "\t\tui - userinstall\r\n";
                    helpmsg += "\t\tux - useruninstall\r\n";
                    helpmsg += "\t\tuf - userrepair\r\n\n";
                    helpmsg += "\t\tur - userrun\r\n\n";
                    helpmsg += "\t\ts - snapshot\r\n\n";
                    helpmsg += "\t\tt - testing\r\n\n";
                    helpmsg += "\t/silent\r\n";
                    helpmsg += "\t\tUse this if the messages are shown per user\r\n\n";
                    helpmsg += "\t/?\r\n";
                    helpmsg += "\t\tHelp Information\r\n";
                    MessageBox.Show(helpmsg, Application.ProductName);
                    //Exit the application
                    Application.Exit();
                }
                else
                {
                    Thread ta = new Thread(new ThreadStart(PostWorkingTask));
                    ta.CurrentCulture = cultureInfo;
                    ta.Start();
                }
            }
        }

        #region Queue

        public void StartWorking()
        {
            //GUI shouldn't be visible at start
            this.Invoke(new MethodInvoker(DisableGUI));

            //log config
            string currentPath = pc.MainModule.FileName.Substring(0, pc.MainModule.FileName.LastIndexOf(@"\"));
            //XmlConfigurator.Configure(new System.IO.FileInfo(currentPath + "\\" + LOGCONFIG_TASKS));

            //default values
            string installtype = "i";
            string nodetoparse = "";
            const string XMLNAMESPACE = "deploy";
            const string TYPEINSTALL = "i";
            const string TYPEUNINSTALL = "x";
            const string TYPEREPAIR = "f";
            const string TYPEUSERINSTALL = "ui";
            const string TYPEUSERUNINSTALL = "ux";
            const string TYPEUSERREPAIR = "uf";
            const string TYPEUSERRUN = "ur";
            const string TYPEDUMMY = "d";
            const string TYPESNAPSHOT = "s";
            const string TYPETEST = "t";

            //init(s)
            GlobalClass.ExitCode = 0;

            //init Functions
            Functions myFunctions = new Functions();


            //get installation queue data from xml
            try
            {
                //get XDS Schema version
                XmlDocument xsdDoc = new XmlDocument();
                xsdDoc.Load(currentPath + "\\" + XMLSCHEMAFILE);
                XmlNamespaceManager nsmgrXSD = new XmlNamespaceManager(xsdDoc.NameTable);
                nsmgrXSD.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
                XmlNode versionNode = xsdDoc.SelectSingleNode("//xs:element/xs:annotation/xs:appinfo", nsmgrXSD);
                XSDversion = versionNode.InnerText.ToString();
            }
            catch (Exception)
            {
                XSDversion = "unknown";
            }

            try
            {
                //XML Validation (XSD Schema)
                XmlReaderSettings deploySettings = new XmlReaderSettings();
                deploySettings.Schemas.Add(null, currentPath + "\\" + XMLSCHEMAFILE);
                deploySettings.ValidationType = ValidationType.Schema;
                deploySettings.ValidationEventHandler += new ValidationEventHandler(deploySettings_ValidationEventHandler);
                if (parameterXMLFile == "")
                {
                    parameterXMLFile = currentPath + "\\" + XMLDEFAULTFILE;
                }
                else
                {
                    if (!File.Exists(parameterXMLFile))
                    {
                        parameterXMLFile = currentPath + "\\" + parameterXMLFile;
                    }
                }
                XmlReader deployXML = XmlReader.Create(parameterXMLFile, deploySettings);
                while (deployXML.Read()) { }

                //XML Validation successfull?
                if (!abort)
                {
                    //get Data from XML
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(parameterXMLFile);
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                    nsmgr.AddNamespace(XMLNAMESPACE, nsmgr.DefaultNamespace.ToString());

                    //get metadata
                    foreach (XmlNode nd in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":metadata", nsmgr))
                    {
                        XmlElement package = nd as XmlElement;
                        XmlElement elproductname = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":productname", nsmgr);
                        productname = elproductname.InnerText;
                        XmlElement elproductversion = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":productversion", nsmgr);
                        productversion = elproductversion.InnerText;
                        XmlElement elproductcode = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":productcode", nsmgr);
                        productcode = elproductcode.InnerText;
                        XmlElement elmanufacturer = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":manufacturer", nsmgr);
                        manufacturer = elmanufacturer.InnerText;
                        XmlElement elarchitecture = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":architecture", nsmgr);
                        architecture = elarchitecture.InnerText;
                        XmlElement elpackager = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":packager", nsmgr);
                        packager = elpackager.InnerText;
                        XmlElement elcreationdate = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":creationdate", nsmgr);
                        creationdate = elcreationdate.InnerText;
                        XmlElement eltype = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":type", nsmgr);
                        type = eltype.InnerText;
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":internalversion", nsmgr);
                            internalversion = eltype2.InnerText;
                        }
                        catch { }
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":maininstallfolderx86", nsmgr);
                            maininstallfolderx86 = eltype2.InnerText;
                        }
                        catch { }
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":maininstallfolderx64", nsmgr);
                            maininstallfolderx64 = eltype2.InnerText;
                        }
                        catch { }
                        try
                        {
                            XmlElement eltype2 = (XmlElement)package.SelectSingleNode("//" + XMLNAMESPACE + ":maininstallexe", nsmgr);
                            maininstallexe = eltype2.InnerText;
                        }
                        catch { }
                    }


                    //get configuration
                    try
                    {
                        foreach (XmlNode nd in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":configuration", nsmgr))
                        {
                            foreach (XmlNode sd in nd.ChildNodes)
                            {
                                if (sd.Attributes["name"].Value == "versiondetect")
                                {
                                    try
                                    {
                                        versiondetect = sd.Attributes["value"].Value;
                                    }
                                    catch { }
                                }
                                if (sd.Attributes["name"].Value == "versiondetectpatch")
                                {
                                    try
                                    {
                                        versiondetectpatch = sd.Attributes["value"].Value;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }



                    try
                    {
                        foreach (XmlNode node in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":userinstall", nsmgr))
                        {
                            foreach (XmlNode nodeType in node.ChildNodes)
                            {
                                hasuserinstall = true;
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        foreach (XmlNode node in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":useruninstall", nsmgr))
                        {
                            foreach (XmlNode nodeType in node.ChildNodes)
                            {
                                hasuseruninstall = true;
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        foreach (XmlNode node in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":userrun", nsmgr))
                        {
                            foreach (XmlNode nodeType in node.ChildNodes)
                            {
                                hasuserrun = true;
                            }
                        }
                    }
                    catch { }

                    //fill up MetaData Text Box
                    this.Invoke(new MethodInvoker(SetRichTextBoxMetaData));

                    //get installtype
                    switch (parameterTYPE.ToUpper())
                    {
                        case "I":
                        case "/I":
                            installtype = TYPEINSTALL;
                            break;
                        case "X":
                        case "/X":
                            installtype = TYPEUNINSTALL;
                            break;
                        case "F":
                        case "/F":
                            installtype = TYPEREPAIR;
                            break;
                        case "UI":
                        case "/UI":
                            installtype = TYPEUSERINSTALL;
                            break;
                        case "UX":
                        case "/UX":
                            installtype = TYPEUSERUNINSTALL;
                            break;
                        case "UF":
                        case "/UF":
                            installtype = TYPEUSERREPAIR;
                            break;
                        case "UR":
                        case "/UR":
                            installtype = TYPEUSERRUN;
                            break;
                        case "D":
                        case "/D":
                            installtype = TYPEDUMMY;
                            break;
                        case "S":
                        case "/S":
                            installtype = TYPESNAPSHOT;
                            break;
                        case "T":
                        case "/T":
                            installtype = TYPETEST;
                            break;
                        default:
                            installtype = TYPEINSTALL;
                            break;
                    }







                    //get (un)install queue
                    switch (installtype)
                    {

                        case TYPEUNINSTALL:
                            nodetoparse = "//" + XMLNAMESPACE + ":uninstall";
                            try
                            {
                                labelQueue.Text = "Uninstallation";
                            }
                            catch { }
                            typelabel = "Uninstallation";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableUnInstallQueuePic));
                            if (internalversion != "")
                            {
                                try
                                {
                                    myFunctions.RemoveRegKey(@"HKLM\Software\Microsoft\Windows\CurrentVersion\" + productcode, "InternalVersion");
                                }
                                catch { }
                                try
                                {
                                    myFunctions.RemoveRegKey(@"HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\" + productcode, "InternalVersion");
                                }
                                catch { }
                            }
                            try
                            {
                                if (myFunctions.CheckArchitecture())
                                {
                                    System.IO.Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(maininstallfolderx64));
                                    try
                                    {
                                        System.IO.File.Delete(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs");
                                    }
                                    catch { }
                                    try
                                    {
                                        System.IO.File.Delete(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.ps1");
                                    }
                                    catch { }
                                    string lsGuid = GetGuidFromProductCode(productcode);
                                    myFunctions.RemoveRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "");
                                }
                                else
                                {
                                    System.IO.Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(maininstallfolderx86));
                                    try
                                    {
                                        System.IO.File.Delete(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs");
                                    }
                                    catch { }
                                    try
                                    {
                                        System.IO.File.Delete(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.ps1");
                                    }
                                    catch { }
                                    string lsGuid = GetGuidFromProductCode(productcode);
                                    myFunctions.RemoveRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "");

                                }
                            }
                            catch { }
                            break;
                        case TYPEREPAIR:
                            nodetoparse = "//" + XMLNAMESPACE + ":repair";
                            try
                            {
                                labelQueue.Text = "Repair";
                            }
                            catch { }
                            typelabel = "Repair";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableRepairQueuePic));
                            break;
                        case TYPEUSERINSTALL:
                            nodetoparse = "//" + XMLNAMESPACE + ":userinstall";
                            try
                            {
                                labelQueue.Text = "User Installation";
                            }
                            catch { }
                            typelabel = "Installation";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableInstallQueuePic));
                            break;
                        case TYPEUSERUNINSTALL:
                            nodetoparse = "//" + XMLNAMESPACE + ":useruninstall";
                            try
                            {
                                labelQueue.Text = "User Uninstallation";
                            }
                            catch { }
                            typelabel = "Uninstallation";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableUnInstallQueuePic));
                            break;
                        case TYPEUSERREPAIR:
                            nodetoparse = "//" + XMLNAMESPACE + ":userrepair";
                            try
                            {
                                labelQueue.Text = "User Repair";
                            }
                            catch { }
                            typelabel = "Repair";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableRepairQueuePic));
                            break;
                        case TYPEUSERRUN:
                            nodetoparse = "//" + XMLNAMESPACE + ":userrun";
                            try
                            {
                                labelQueue.Text = "User Run";
                            }
                            catch { }
                            typelabel = "Run";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableRepairQueuePic));
                            break;
                        case TYPESNAPSHOT:
                            nodetoparse = "//" + XMLNAMESPACE + ":snapshot";
                            try
                            {
                                labelQueue.Text = "Snapshot";
                            }
                            catch { }
                            typelabel = "Snapshot";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableRepairQueuePic));
                            break;
                        case TYPETEST:
                            nodetoparse = "//" + XMLNAMESPACE + ":testing";
                            try
                            {
                                labelQueue.Text = "Testing";
                            }
                            catch { }
                            typelabel = "Testing";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableRepairQueuePic));
                            break;
                        case TYPEDUMMY:
                            nodetoparse = "//" + XMLNAMESPACE + ":dummy";
                            try
                            {
                                labelQueue.Text = "Dummy";
                            }
                            catch { }
                            typelabel = "Dummy";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableRepairQueuePic));
                            break;
                        case TYPEINSTALL:
                        default:
                            nodetoparse = "//" + XMLNAMESPACE + ":install";
                            try
                            {
                                labelQueue.Text = "Installation";
                            }
                            catch { }
                            typelabel = "Installation";
                            this.Invoke(new MethodInvoker(DisableQueuePics));
                            this.Invoke(new MethodInvoker(EnableInstallQueuePic));



                            break;
                            //default:
                            //    nodetoparse = "//" + XMLNAMESPACE + ":install";
                            //    try
                            //    {
                            //        labelQueue.Text = "Installation";
                            //    }
                            //    catch { }
                            //    typelabel = "Installation";
                            //    this.Invoke(new MethodInvoker(DisableQueuePics));
                            //    this.Invoke(new MethodInvoker(EnableInstallQueuePic));
                            //    break;
                    }

                    try
                    {
                        if (Properties.Settings.Default.ExeLogFileSplit.ToLower() == "true")
                        {
                            switch (installtype)
                            {
                                case TYPEUNINSTALL:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_uninstall" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                                case TYPEREPAIR:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_repair" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                                case TYPEUSERINSTALL:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_userinstall" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                                case TYPEUSERUNINSTALL:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_useruninstall" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                                case TYPEUSERREPAIR:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_userrepair" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                                case TYPEUSERRUN:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_userrun" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                                case TYPEDUMMY:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_dummy" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                                default:
                                    LOG_TASKS_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_TASKS_FILE) + "_install" + System.IO.Path.GetExtension(LOG_TASKS_FILE);
                                    break;
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        if (Properties.Settings.Default.LogfileSplit.ToLower() == "true")
                        {
                            switch (installtype)
                            {
                                case TYPEUSERUNINSTALL:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_useruninstall" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                                case TYPEUSERREPAIR:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_userrepair" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                                case TYPEUSERINSTALL:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_userinstall" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                                case TYPEUSERRUN:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_userrun" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                                case TYPEUNINSTALL:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_uninstall" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                                case TYPEREPAIR:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_repair" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                                case TYPEDUMMY:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_dummy" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                                default:
                                    LOG_MSI_FILE = System.IO.Path.GetFileNameWithoutExtension(LOG_MSI_FILE) + "_install" + System.IO.Path.GetExtension(LOG_MSI_FILE);
                                    break;
                            }
                        }
                    }
                    catch { }

                    //logging
                    GlobalClass.ProductCode = productcode;
                    GlobalClass.LogFileProductCode = LOG_TASKS + LOG_TASKS_FILE.Replace("[ProductCode]", productcode);
                    if (!myFunctions.CheckLogFilePath(GlobalClass.LogFileProductCode))
                    {
                        HandleUnvalidLogFilePath(GlobalClass.LogFileProductCode, GlobalClass.EC_FileNotFoundException);
                        return;
                    }
                    myFunctions.UpdateFileAppenderPath(LOG_TASKS + LOG_TASKS_FILE.Replace("[ProductCode]", productcode));
                    GlobalClass.logger.Info(" ", GlobalClass.SECTION);
                    string lsIntializeText = "**** START (un)install ****";
                    try
                    {
                        switch (installtype)
                        {
                            case TYPEINSTALL:
                                lsIntializeText = "**** START install ****";
                                GlobalClass.SECTION = "install";
                                break;
                            case TYPEUNINSTALL:
                                lsIntializeText = "**** START uninstall ****";
                                GlobalClass.SECTION = "uninstall";
                                break;
                            case TYPEREPAIR:
                                lsIntializeText = "**** START repair ****";
                                GlobalClass.SECTION = "repair";
                                break;
                            case TYPEUSERINSTALL:
                                lsIntializeText = "**** START user install ****";
                                GlobalClass.SECTION = "userinstall";
                                break;
                            case TYPEUSERUNINSTALL:
                                lsIntializeText = "**** START user uninstall ****";
                                GlobalClass.SECTION = "useruninstall";
                                break;
                            case TYPEUSERREPAIR:
                                lsIntializeText = "**** START user repair ****";
                                GlobalClass.SECTION = "userrepair";
                                break;
                            case TYPEUSERRUN:
                                lsIntializeText = "**** START user run ****";
                                GlobalClass.SECTION = "userrun";
                                break;
                            case TYPEDUMMY:
                                lsIntializeText = "**** START dummy ****";
                                GlobalClass.SECTION = "dummy";
                                break;
                            case TYPETEST:
                                lsIntializeText = "**** START test ***";
                                GlobalClass.SECTION = "testing";
                                break;
                        }
                    }
                    catch { }
                    GlobalClass.logger.Info(lsIntializeText, GlobalClass.SECTION);
                    GlobalClass.logger.Info("METADATA: '" + manufacturer + " " + productname + "';" + productcode + ";" + productversion + ";" + type + ";" + packager + ";" + creationdate, GlobalClass.SECTION);
                    GlobalClass.logger.Info("TASK QUEUE TYPE: " + installtype, GlobalClass.SECTION);
                    GlobalClass.logger.Info("SCHEMA VERSION (deploy.XSD): " + XSDversion, GlobalClass.SECTION);
                    GlobalClass.logger.Info("--------------------------------------", GlobalClass.SECTION);
                    GlobalClass.logger.Info("--------- TASK QUEUE . START ---------", GlobalClass.SECTION);
                    GlobalClass.logger.Info("XML node to parse: '" + nodetoparse + "'", GlobalClass.SECTION);
                    GlobalClass.logger.Info("USERINSTALL Part: '" + hasuserinstall.ToString() + "'", GlobalClass.SECTION);
                    GlobalClass.logger.Info("USERUNINSTALL Part: '" + hasuseruninstall.ToString() + "'", GlobalClass.SECTION);
                    GlobalClass.logger.WritetoEventLog("Start setup queue (" + installtype + ") - '" + productname + "'", LINFO, GlobalClass.SECTION);




                    //get configuration

                    bool goodHash = true;

                    try
                    {

                        GlobalClass.logger.Info("Looking for checksum information", GlobalClass.SECTION);

                        foreach (XmlNode nd in xmlDoc.SelectNodes("//" + XMLNAMESPACE + ":configuration", nsmgr))
                        {
                            foreach (XmlNode sd in nd.ChildNodes)
                            {
                                try
                                {

                                    if (sd.Attributes["type"].Value == "checksum")
                                    {
                                        if (System.IO.File.Exists(currentPath + @"\" + sd.Attributes["name"].Value))
                                        {
                                            string localhash = "";
                                            using (var stream = new BufferedStream(File.OpenRead(currentPath + @"\" + sd.Attributes["name"].Value), 1200000))
                                            {
                                                try
                                                {
                                                    SHA256Managed sha = new SHA256Managed();
                                                    byte[] checksum = sha.ComputeHash(stream);
                                                    localhash = BitConverter.ToString(checksum).Replace("-", String.Empty);
                                                }
                                                catch { }
                                            }
                                            if (localhash == sd.Attributes["value"].Value)
                                            {
                                                //GlobalClass.logger.Info("Hash is successful for " + currentPath + @"\" + sd.Attributes["name"].Value, GlobalClass.SECTION);
                                            }
                                            else
                                            {
                                                GlobalClass.logger.Info("Hash is broken for " + currentPath + @"\" + sd.Attributes["name"].Value, GlobalClass.SECTION);
                                                goodHash = false;
                                            }
                                        }
                                    }

                                }
                                catch { }

                            }
                        }
                    }
                    catch { }

                    if(!goodHash)
                    {
                        GlobalClass.logger.Info("Must abort immediately, one or more of the checksums are broken, either fix the checksums or remove them from the configuartion section.", GlobalClass.SECTION);
                    }
                    else
                    {
                        GlobalClass.logger.Info("All checksums are fine!", GlobalClass.SECTION);
                    }

                    GlobalClass.logger.Info("CAPTURING Start Menu", GlobalClass.SECTION);

                    //read files
                    try
                    {
                        GlobalClass.StartMenuFilesBefore = Directory.GetFiles(Environment.ExpandEnvironmentVariables(@"%ProgramData%\Microsoft\Windows\Start Menu"), "*.*", SearchOption.AllDirectories);
                    }
                    catch { }

                    //read files
                    try
                    {
                        GlobalClass.DesktopFilesBefore = Directory.GetFiles(Environment.ExpandEnvironmentVariables(@"%Public%\Desktop"), "*.*", SearchOption.AllDirectories);
                    }
                    catch { }

                    GlobalClass.UninstallBefore = new DataTable();
                    GlobalClass.UninstallBefore.Columns.Add("key");
                    GlobalClass.UninstallBefore.Columns.Add("architecture");

                    string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                    try
                    {
                        RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                        foreach (string lsSubKey in regKey.GetSubKeyNames())
                        {
                            DataRow loUninstallRow = GlobalClass.UninstallBefore.NewRow();
                            loUninstallRow["architecture"]="x64";
                            loUninstallRow["key"] = System.IO.Path.GetFileName(lsSubKey);
                            GlobalClass.UninstallBefore.Rows.Add(loUninstallRow);
                        }
                    }
                    catch { }
                    path = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                    try
                    {
                        RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                        foreach (string lsSubKey in regKey.GetSubKeyNames())
                        {
                            DataRow loUninstallRow = GlobalClass.UninstallBefore.NewRow();
                            loUninstallRow["architecture"]="x86";
                            loUninstallRow["key"] = System.IO.Path.GetFileName(lsSubKey);
                            GlobalClass.UninstallBefore.Rows.Add(loUninstallRow);
                        }
                    }
                    catch { }

                    GlobalClass.UninstallAfter = new DataTable();
                    GlobalClass.UninstallAfter.Columns.Add("key");
                    GlobalClass.UninstallAfter.Columns.Add("architecture");

                    if (goodHash)
                    {

                        RunXML loRunXML = new RunXML();
                        loRunXML.SetLabelEvent += c_ThresholdReached;
                        //start parsing tasks, defined in external XML file
                        foreach (XmlNode node in xmlDoc.SelectNodes(nodetoparse, nsmgr))
                        {
                            foreach (XmlNode nodeType in node.ChildNodes)
                            {
                                foreach (XmlNode nodeTask in nodeType.ChildNodes)
                                {

                                    abort = loRunXML.ExecuteTask(nodeTask);


                                    //uninstallafter
                                    GlobalClass.UninstallAfter.Rows.Clear();
                                    path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                                    try
                                    {
                                        RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                                        foreach (string lsSubKey in regKey.GetSubKeyNames())
                                        {
                                            DataRow loUninstallRow = GlobalClass.UninstallAfter.NewRow();
                                            loUninstallRow["architecture"]="x64";
                                            loUninstallRow["key"] = System.IO.Path.GetFileName(lsSubKey);
                                            GlobalClass.UninstallAfter.Rows.Add(loUninstallRow);
                                        }
                                    }
                                    catch { }
                                    path = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                                    try
                                    {
                                        RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path);
                                        foreach (string lsSubKey in regKey.GetSubKeyNames())
                                        {
                                            DataRow loUninstallRow = GlobalClass.UninstallAfter.NewRow();
                                            loUninstallRow["architecture"]="x86";
                                            loUninstallRow["key"] = System.IO.Path.GetFileName(lsSubKey);
                                            GlobalClass.UninstallAfter.Rows.Add(loUninstallRow);
                                        }
                                    }
                                    catch { }

                                    if (abort)
                                    {
                                        //logger.Warn("ABORT queue by this task: '" + nodeTask.Name + "'");
                                        //Exit the 'for'-loop with 'break'
                                        break;
                                    }
                                }
                                if (abort)
                                    break;
                            }
                            if (abort)
                                break;
                        }



                        //create activesetup
                        switch (installtype)
                        {
                            case TYPEINSTALL:
                                if ((maininstallfolderx64 != "" || maininstallfolderx86 != "") && hasuserinstall)
                                {
                                    try
                                    {
                                        if (myFunctions.CheckArchitecture())
                                        {
                                            System.IO.Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(maininstallfolderx64));
                                            string lsGuid = GetGuidFromProductCode(productcode);
                                            string lsError = "";

                                            switch (ACTIVESETUPLANGUAGE.ToLower())
                                            {
                                                case "powershell":
                                                    try
                                                    {
                                                        System.IO.Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(maininstallfolderx64));
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        string lsDeployXML = File.ReadAllText(parameterXMLFile);
                                                        string lsVBScript = Deliveries.FastWrapper.Logic.clsConvertEngine.CreatePSScripts("userinstall", lsDeployXML, manufacturer, productname, productversion);
                                                        byte[] utf8Bytes = Encoding.UTF8.GetBytes(lsVBScript);
                                                        string str2 = Encoding.UTF8.GetString(utf8Bytes);
                                                        System.IO.File.WriteAllText(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.ps1", str2, Encoding.Unicode);
                                                    }
                                                    catch { }
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "ComponentId", productname, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "DontAsk", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "IsInstalled", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "StubPath", @"powershell -ExecutionPolicy Bypass -file """ + Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.ps1""", "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    if (internalversion != "")
                                                    {
                                                        myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "Version", internalversion, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    }

                                                    break;
                                                case "none":
                                                    break;
                                                default:
                                                    try
                                                    {
                                                        System.IO.Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(maininstallfolderx64));
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        string lsDeployXML = File.ReadAllText(parameterXMLFile);
                                                        string lsVBScript = Deliveries.FastWrapper.Logic.clsConvertEngine.CreateVBScripts("userinstall", lsDeployXML, manufacturer, productname, productversion, "", "");
                                                        byte[] utf8Bytes = Encoding.UTF8.GetBytes(lsVBScript);
                                                        string str2 = Encoding.UTF8.GetString(utf8Bytes);
                                                        try
                                                        {
                                                            System.IO.File.Delete(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs");
                                                        }
                                                        catch { }
                                                        System.IO.File.WriteAllText(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs", str2, Encoding.Unicode);
                                                    }
                                                    catch { }
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "ComponentId", productname, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "DontAsk", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "IsInstalled", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "StubPath", @"""" + Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs""", "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    if (internalversion != "")
                                                    {
                                                        myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "Version", internalversion, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            System.IO.Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(maininstallfolderx86));

                                            string lsGuid = GetGuidFromProductCode(productcode);
                                            string lsError = "";

                                            switch (ACTIVESETUPLANGUAGE.ToLower())
                                            {
                                                case "powershell":
                                                    try
                                                    {
                                                        string lsDeployXML = File.ReadAllText(parameterXMLFile);
                                                        string lsVBScript = Deliveries.FastWrapper.Logic.clsConvertEngine.CreatePSScripts("userinstall", lsDeployXML, manufacturer, productname, productversion);
                                                        System.IO.File.WriteAllText(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.ps1", lsVBScript);
                                                    }
                                                    catch { }
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "ComponentId", productname, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "DontAsk", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "IsInstalled", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "StubPath", @"powershell -ExecutionPolicy Bypass -file """ + Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.ps1""", "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    if (internalversion != "")
                                                    {
                                                        myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "Version", internalversion, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    }

                                                    break;
                                                default:
                                                    try
                                                    {
                                                        string lsDeployXML = File.ReadAllText(parameterXMLFile);
                                                        string lsVBScript = Deliveries.FastWrapper.Logic.clsConvertEngine.CreateVBScripts("userinstall", lsDeployXML, manufacturer, productname, productversion, "", "");
                                                        System.IO.File.WriteAllText(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs", lsVBScript);
                                                    }
                                                    catch { }
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "ComponentId", productname, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "DontAsk", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "IsInstalled", "1", "REG_DWORD", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "StubPath", @"""" + Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs""", "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    if (internalversion != "")
                                                    {
                                                        myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Active Setup\Installed Components\" + lsGuid, "Version", internalversion, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                                    }
                                                    break;
                                            }

                                        }
                                    }
                                    catch { }
                                }
                                break;

                        }


                    }

                    //read files
                    try
                    {
                        GlobalClass.StartMenuFilesAfter = Directory.GetFiles(Environment.ExpandEnvironmentVariables(@"%ProgramData%\Microsoft\Windows\Start Menu"), "*.*", SearchOption.AllDirectories);
                    }
                    catch { }
                    try
                    {
                        foreach (string lsFile in GlobalClass.StartMenuFilesAfter)
                        {
                            bool lbFoundInBefore = false;
                            foreach (string lsBeforeFile in GlobalClass.StartMenuFilesBefore)
                            {
                                if (lsBeforeFile==lsFile)
                                {
                                    lbFoundInBefore = true;
                                }
                            }
                            if (!lbFoundInBefore)
                            {
                                GlobalClass.logger.Info("new startmenu shortcut: " + lsFile, GlobalClass.SECTION);
                            }
                        }
                    }
                    catch { }


                    //read files
                    try
                    {
                        GlobalClass.DesktopFilesAfter = Directory.GetFiles(Environment.ExpandEnvironmentVariables(@"%Public%\Desktop"), "*.*", SearchOption.AllDirectories);
                    }
                    catch { }

                    try
                    {
                        foreach (string lsFile in GlobalClass.DesktopFilesAfter)
                        {
                            bool lbFoundInBefore = false;
                            foreach (string lsBeforeFile in GlobalClass.DesktopFilesAfter)
                            {
                                if (lsBeforeFile==lsFile)
                                {
                                    lbFoundInBefore = true;
                                }
                            }
                            if (!lbFoundInBefore)
                            {
                                GlobalClass.logger.Info("new desktop shortcut: " + lsFile, GlobalClass.SECTION);
                            }
                        }
                    }
                    catch { }

                    if (goodHash)
                    {
                        switch (installtype)
                        {
                            case TYPEINSTALL:
                                if (internalversion != "")
                                {
                                    string lsError = "";
                                    try
                                    {
                                        if (myFunctions.CheckRegistry(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode, "", ""))
                                        {
                                            myFunctions.AddRegKey(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode, "InternalVersion", internalversion, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                        }
                                    }
                                    catch { }
                                    try
                                    {
                                        if (myFunctions.CheckRegistry(@"HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode, "", ""))
                                        {
                                            myFunctions.AddRegKey(@"HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode, "InternalVersion", internalversion, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);
                                        }
                                    }
                                    catch { }
                                }

                                if (hasuserinstall)
                                {
                                    GlobalClass.logger.Info("---------- STARTING USERINSTALL NOW . START ----------", GlobalClass.SECTION);
                                    string lsCMD = "";
                                    try
                                    {
                                        bool lbDoneSomething = false;
                                        if (!lbDoneSomething)
                                        {
                                            if (System.IO.File.Exists(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.ps1"))
                                            {
                                                lsCMD = @"powershell.exe -file """ + Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.ps1""";
                                                lbDoneSomething = true;
                                            }
                                        }
                                        if (!lbDoneSomething)
                                        {
                                            if (System.IO.File.Exists(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.ps1"))
                                            {
                                                lsCMD = @"powershell.exe -file """ + Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.ps1"""; ;
                                                lbDoneSomething = true;
                                            }
                                        }
                                        if (!lbDoneSomething)
                                        {
                                            if (System.IO.File.Exists(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs"))
                                            {
                                                lsCMD = @"wscript.exe """ + Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs""";
                                                lbDoneSomething = true;
                                            }
                                        }
                                        if (!lbDoneSomething)
                                        {
                                            if (System.IO.File.Exists(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs"))
                                            {
                                                lsCMD = @"wscript.exe """ + Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs""";
                                                lbDoneSomething = true;
                                            }
                                        }
                                    }
                                    catch { }
                                    try
                                    {
                                        if (lsCMD != "")
                                        {
                                            GlobalClass.logger.Info("---------- MUST RUN " + lsCMD + " ----------", GlobalClass.SECTION);

                                            string workingDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\";
                                            string serviceExe = workingDir + "Deliveries_Setup_RunAsUserService.exe";

                                            WriteResourceToFile("SetupExe.Resources.CluebizRunAsUserService.exe", serviceExe);

                                            File.WriteAllText(workingDir + "RunAsUserServiceCmd.txt", lsCMD);

                                            Process process = Process.Start(@"C:\Windows\system32\sc.exe", "create DeliveriesSetupRunAsUserService binPath=\"" + serviceExe + "\"");
                                            process.WaitForExit();

                                            process = Process.Start(@"C:\Windows\system32\sc.exe", @"start DeliveriesSetupRunAsUserService");
                                            process.WaitForExit();

                                            //wait max 10sec for service to finish
                                            int runs = 0;
                                            while (File.Exists(workingDir + "RunAsUserServiceCmd.txt") && ++runs < 11) Thread.Sleep(1000);

                                            process = Process.Start(@"C:\Windows\system32\sc.exe", @"stop DeliveriesSetupRunAsUserService");
                                            process.WaitForExit();

                                            process = Process.Start(@"C:\Windows\system32\sc.exe", @"delete DeliveriesSetupRunAsUserService");
                                            process.WaitForExit();
                                        }
                                    }
                                    catch { }
                                    GlobalClass.logger.Info("---------- STARTING USERINSTALL NOW . END ----------", GlobalClass.SECTION);
                                }
                                break;

                            case TYPEUNINSTALL:
                                if (hasuseruninstall)
                                {
                                    GlobalClass.logger.Info("---------- STARTING USERUNINSTALL NOW . START ----------", GlobalClass.SECTION);


                                    GlobalClass.logger.Info(xmlDoc.InnerXml.ToString(), GlobalClass.SECTION);

                                    RunXMLUserUninstall loRunXMLUserUninstall = new RunXMLUserUninstall();

                                    foreach (XmlNode node in xmlDoc.ChildNodes)
                                    {
                                        if (node.Name == "deploy")
                                        {
                                            foreach (XmlNode mainnode in node.ChildNodes)
                                            {
                                                if (mainnode.Name=="useruninstall")
                                                {
                                                    foreach (XmlNode subnode in mainnode.ChildNodes)
                                                    {
                                                        foreach (XmlNode nodeTask in subnode.ChildNodes)
                                                        {
                                                            loRunXMLUserUninstall.ExecuteTask(nodeTask);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    GlobalClass.logger.Info("---------- STARTING USERUNINSTALL NOW . END ----------", GlobalClass.SECTION);

                                }
                                break;
                        }

                    }

                    GlobalClass.logger.Info("---------- TASK QUEUE . END ----------", GlobalClass.SECTION);
                    GlobalClass.logger.Info("--------------------------------------", GlobalClass.SECTION);
                    GlobalClass.logger.WritetoEventLog("End setup queue", "INFO", GlobalClass.SECTION);

                }
                //PostWorkingTask(); --> moved to 'finally'
                return;

            }
            catch (System.IO.FileNotFoundException ex)
            {
                //logging & MsgBox
                HandleExceptions(ex, GlobalClass.EC_FileNotFoundException);
            }
            catch (System.UnauthorizedAccessException ex)
            {
                //logging & MsgBox
                HandleExceptions(ex, GlobalClass.EC_UnauthorizedAccessException);
            }
            catch (XmlSchemaException ex)
            {
                //logging & MsgBox
                HandleExceptions(ex, GlobalClass.EC_XMLSchemaException);
            }
            //TODO Beat wieder entfernen oder besser machen
            catch (Exception ex)
            {
                //logger.Fatal("unknown exception",ex);
                HandleExceptions(ex, GlobalClass.EC_UnknownException);
            }
            finally
            {
                PostWorkingTask();
            }

        }


        private void PostWorkingTask()
        {
            bool applicationExit = false;
            this.Invoke(new MethodInvoker(DisableAnimatedGif));
            GlobalClass.logger.Info("---- POST " + typelabel + " tasks . start ----", GlobalClass.SECTION);
            GlobalClass.logger.Info("isUnattended: " + GlobalClass.IsUnattended, GlobalClass.SECTION);
            GlobalClass.logger.Info("Postponed: " + GlobalClass.IsPostponed, GlobalClass.SECTION);
            GlobalClass.logger.Info("Aborted: " + GlobalClass.IsAborted, GlobalClass.SECTION);
            GlobalClass.logger.Info("isMainGuiEnabled: " + GlobalClass.IsMainGUIEnabled, GlobalClass.SECTION);
            GlobalClass.logger.Info("Reboot required: " + GlobalClass.IsRebootEnabled, GlobalClass.SECTION);
            GlobalClass.logger.Info("Versiondetect: " + versiondetect, GlobalClass.SECTION);
            GlobalClass.logger.Info("Versiondetectpatch: " + versiondetectpatch, GlobalClass.SECTION);

            if (typelabel=="Installation" || typelabel=="Testing")
            {
                GlobalClass.logger.Info("", GlobalClass.SECTION);
                GlobalClass.logger.Info("---- TEST RESULTS FOR " + typelabel + " ----", GlobalClass.SECTION);

                bool lbDetectionRule = false;
                if (versiondetect != "")
                {
                    if (architecture.ToLower() == "x64" && versiondetectpatch != "forcewow6432")
                    {
                        try
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode))
                            {
                                if (key != null)
                                {
                                    Object o = key.GetValue("DisplayVersion");
                                    if (o != null)
                                    {
                                        if (o.ToString()==versiondetect)
                                        {
                                            lbDetectionRule=true;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode))
                            {
                                if (key != null)
                                {
                                    Object o = key.GetValue("DisplayVersion");
                                    if (o != null)
                                    {
                                        if (o.ToString()==versiondetect)
                                        {
                                            lbDetectionRule=true;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    GlobalClass.logger.Info("Test Result for detection rule: " + lbDetectionRule.ToString().ToUpper() + " | Detection rule: looking for " + productcode + " and " + versiondetect, GlobalClass.SECTION);

                }
                else
                {
                    if (architecture.ToLower() == "x64" && versiondetectpatch != "forcewow6432")
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode))
                        {
                            if (key != null)
                            {
                                lbDetectionRule=true;
                            }
                        }
                    }
                    else
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + productcode))
                        {
                            if (key != null)
                            {
                                lbDetectionRule=true;
                            }
                        }
                    }
                    GlobalClass.logger.Info("Test Result for detection rule: " + lbDetectionRule.ToString().ToUpper() + " | Detection rule: looking for " + productcode, GlobalClass.SECTION);

                }


                bool lbMainInstallExe = false;
                if(maininstallfolderx64 != "")
                {
                    if(maininstallexe != "")
                    {
                        try
                        {
                            if(System.IO.File.Exists(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\" + maininstallexe))
                            {
                                lbMainInstallExe=true;
                            }
                        } catch { }
                    }
                }
                else
                {
                    lbMainInstallExe=true;
                }

                GlobalClass.logger.Info("Test Result for main executable: " + lbMainInstallExe.ToString().ToUpper() + " | Main Executable: looking for " + maininstallfolderx64 + @"\" + maininstallexe, GlobalClass.SECTION);

                GlobalClass.logger.Info("", GlobalClass.SECTION);

            }


            //postponed by user interaction
            if (GlobalClass.IsPostponed)
            {
                //increment registry
                Int32 icurrent = 0;
                string scurrent = "0";
                RegistryKey regKey = null;
                try
                {
                    regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\" + Application.CompanyName + "\\" + Application.ProductName + "\\" + GlobalClass.ProductCode);
                    if (regKey != null)
                    {
                        scurrent = regKey.GetValue("PP").ToString();
                        if (scurrent == "")
                        {
                            scurrent = "0";
                        }
                    }
                    regKey.Close();
                }
                catch
                {
                    scurrent = "0";
                }


                try
                {
                    regKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\" + Application.CompanyName + "\\" + Application.ProductName + "\\" + GlobalClass.ProductCode);
                    icurrent = Convert.ToInt32(scurrent) + 1;
                    regKey.SetValue("PP", icurrent.ToString(), RegistryValueKind.String);
                    regKey.Close();
                }
                catch (Exception)
                {
                    //not enough rights?? do nothing.
                }

                switch (GlobalClass.PostponeType)
                {
                    case 1:
                        //scheduled task
                        this.Invoke(new MethodInvoker(EnableBtnFinish));
                        GlobalClass.ExitCode = GlobalClass.EC_Postponed;
                        //notifyIcon1.ShowBalloonTip(5000, TASKNAME, "Software " + typelabel + " postponed. Please click on 'Finish'.", ToolTipIcon.Warning);
                        set_labeltext("Software " + typelabel + " postponed. Please click on 'Finish'.");
                        GlobalClass.logger.Warn("Software " + typelabel + " postponed by the user", GlobalClass.SECTION);
                        GlobalClass.logger.WritetoEventLog("Software " + typelabel + " postponed by the user", "WARNING", GlobalClass.SECTION);
                        this.Invoke(new MethodInvoker(ThisRefresh));
                        break;
                    case 2:
                        //installation cancelled
                        this.Invoke(new MethodInvoker(EnableBtnFinish));
                        GlobalClass.ExitCode = GlobalClass.EC_Cancelled;
                        //notifyIcon1.ShowBalloonTip(5000, TASKNAME, "Software " + typelabel + " cancelled. Please click on 'Finish'.", ToolTipIcon.Warning);
                        set_labeltext("Software " + typelabel + " cancelled. Please click on 'Finish'.");
                        GlobalClass.logger.Warn("Software " + typelabel + " cancelled by the user", GlobalClass.SECTION);
                        GlobalClass.logger.WritetoEventLog("Software " + typelabel + " cancelled by the user", "WARNING", GlobalClass.SECTION);
                        this.Invoke(new MethodInvoker(ThisRefresh));
                        break;
                }
            }
            else
            {
                //aborted?
                if (GlobalClass.IsAborted)
                {
                    set_labeltext("Software " + typelabel + " aborted.");
                    GlobalClass.logger.Error("Software " + typelabel + " aborted", GlobalClass.SECTION);
                    GlobalClass.logger.WritetoEventLog("Software " + typelabel + " aborted", "ERROR", GlobalClass.SECTION);
                    if (GlobalClass.IsMainGUIEnabled && GlobalClass.IsUnattended == false)
                    {
                        this.Invoke(new MethodInvoker(EnableBtnFinish));
                        notifyIcon1.ShowBalloonTip(5000, TASKNAME, "Software " + typelabel + " aborted. Please click on 'Finish'.", ToolTipIcon.Error);
                        this.Invoke(new MethodInvoker(ThisRefresh));
                    }
                    else
                    {
                        applicationExit = true;
                    }
                }
                else
                {
                    //NOT aborted: somebody is logged on --> show always the final GUI!
                    //nobody is logged on --> automatic restart!
                    //with reboot message?
                    GlobalClass.logger.Info("Software " + typelabel + " successfully finished", GlobalClass.SECTION);
                    GlobalClass.logger.WritetoEventLog("Software " + typelabel + " successfully finished", "INFO", GlobalClass.SECTION);
                    if (GlobalClass.IsRebootEnabled)
                    {
                        //notifyIcon1.ShowBalloonTip(10000, TASKNAME, "Software " + typelabel + " completed. A system restart is required.", ToolTipIcon.Info);
                        set_labeltext("System restart is required.");
                        GlobalClass.logger.Warn("System restart is required.", GlobalClass.SECTION);
                        GlobalClass.logger.WritetoEventLog("System restart is required", "WARNING", GlobalClass.SECTION);

                        this.Invoke(new MethodInvoker(ThisRefresh));

                        //anybody logged on??
                        if (isUserLoggedOn)
                        {
                            if (isFullySilent)
                            {
                                WriteTalkMessage("requesttime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                                WriteTalkMessage("requestcommand", "reboot");
                                if (WaitForTalkResponse()=="yes")
                                {
                                    this.Invoke(new MethodInvoker(DisableGUI));
                                    if (MessageBox.Show("The restart process will be initialized!", TASKNAME, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                                    {
                                        GlobalClass.logger.Info("System restart initialized by user! Restarting now...", GlobalClass.SECTION);
                                        GlobalClass.logger.WritetoEventLog("System restart initialized by user! Restarting now...", "INFO", GlobalClass.SECTION);
                                        Process.Start("ShutDown", "/r");
                                    }
                                    applicationExit = true;
                                }
                                else
                                {
                                    //this.Invoke(new MethodInvoker(EnableBtnFinish));
                                    ////notifyIcon1.ShowBalloonTip(5000, TASKNAME, "Software " + typelabel + " finished. In order to complete the " + typelabel + ", the system needs to be restarted. Please click on 'Finish'.", ToolTipIcon.Info);
                                    //set_labeltext("Software " + typelabel + " is completed. Please Restart!");
                                    //this.Invoke(new MethodInvoker(ThisRefresh));
                                    applicationExit = true;
                                }

                            }
                            else
                            {
                                if (ShowRebootGUI())
                                {
                                    this.Invoke(new MethodInvoker(DisableGUI));
                                    if (MessageBox.Show("The restart process will be initialized!", TASKNAME, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                                    {
                                        GlobalClass.logger.Info("System restart initialized by user! Restarting now...", GlobalClass.SECTION);
                                        GlobalClass.logger.WritetoEventLog("System restart initialized by user! Restarting now...", "INFO", GlobalClass.SECTION);
                                        Process.Start("ShutDown", "/r");
                                    }
                                    applicationExit = true;
                                }
                                else
                                {
                                    this.Invoke(new MethodInvoker(EnableBtnFinish));
                                    //notifyIcon1.ShowBalloonTip(5000, TASKNAME, "Software " + typelabel + " finished. In order to complete the " + typelabel + ", the system needs to be restarted. Please click on 'Finish'.", ToolTipIcon.Info);
                                    set_labeltext("Software " + typelabel + " is completed. Please Restart!");
                                    this.Invoke(new MethodInvoker(ThisRefresh));
                                }
                            }



                            if (hasuserinstall)
                            {

                                GlobalClass.logger.Info("looking for activesetup.", GlobalClass.SECTION);
                                GlobalClass.logger.WritetoEventLog("looking for activesetup.", "INFO", GlobalClass.SECTION);

                                if (maininstallfolderx64 != "")
                                {
                                    try
                                    {

                                        if (System.IO.File.Exists(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs"))
                                        {
                                            Functions myFunctions = new Functions();
                                            string lsError = "";
                                            GlobalClass.logger.Info("running activesetup " + Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs", GlobalClass.SECTION);
                                            myFunctions.ExecuteCMD(Environment.ExpandEnvironmentVariables(maininstallfolderx64) + @"\ActiveSetup.vbs", "", ref lsError, "true", GlobalClass.VarTable, GlobalClass.ParameterTable);

                                            string lsGuid = GetGuidFromProductCode(productcode);

                                            string lsJson = @"{""cmd"":""" + Application.ExecutablePath + @""", ""param"":""TYPE=UI"",""workdir"":""" + Application.ExecutablePath + @""",""waitproc"":""true"",""enddate"":""" + DateTime.Now.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ss") + @"""}";
                                            myFunctions.AddRegKey(@"HKLM\SOFTWARE\cluebiz\deliveries_setup\usertasks", lsGuid, lsJson, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);

                                        }

                                    }
                                    catch { }
                                }

                                if (maininstallfolderx86 != "")
                                {
                                    if (System.IO.File.Exists(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs"))
                                    {
                                        Functions myFunctions = new Functions();
                                        string lsError = "";
                                        GlobalClass.logger.Info("running activesetup " + Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs", GlobalClass.SECTION);
                                        myFunctions.ExecuteCMD(Environment.ExpandEnvironmentVariables(maininstallfolderx86) + @"\ActiveSetup.vbs", "", ref lsError, "true", GlobalClass.VarTable, GlobalClass.ParameterTable);

                                        string lsGuid = GetGuidFromProductCode(productcode);

                                        string lsJson = @"{""cmd"":""" + Application.ExecutablePath + @""", ""param"":""TYPE=UI"",""workdir"":""" + Application.ExecutablePath + @""",""waitproc"":""true"",""enddate"":""" + DateTime.Now.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ss") + @"""}";
                                        myFunctions.AddRegKey(@"HKLM\SOFTWARE\cluebiz\deliveries_setup\usertasks", lsGuid, lsJson, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);

                                    }
                                }
                            }

                            if (hasuserrun)
                            {
                                Functions myFunctions = new Functions();
                                string lsError = "";

                                string lsGuid = GetGuidFromProductCode(productcode);

                                string lsJson = @"{""cmd"":""" + Application.ExecutablePath + @""", ""param"":""TYPE=UR"",""workdir"":""" + Application.ExecutablePath + @""",""waitproc"":""true"",""enddate"":""" + DateTime.Now.AddHours(3).ToString("yyyy-MM-ddTHH:mm:ss") + @"""}";
                                myFunctions.AddRegKey(@"HKLM\SOFTWARE\cluebiz\deliveries_setup\usertasks", lsGuid, lsJson, "REG_SZ", GlobalClass.VarTable, GlobalClass.ParameterTable, ref lsError);

                            }


                        }
                        else
                        {
                            GlobalClass.logger.Info("System restart initialized automatically (nobody is logged on)! Restarting now...", GlobalClass.SECTION);
                            GlobalClass.logger.WritetoEventLog("System restart initialized automatically (nobody is logged on)! Restarting now...", "INFO", GlobalClass.SECTION);
                            Process.Start("ShutDown", "/r");
                            applicationExit = true;
                        }
                    }
                    //finish without reboot message
                    else
                    {
                        //anybody logged on??
                        if (isUserLoggedOn && GlobalClass.IsMainGUIEnabled && GlobalClass.IsUnattended == false)
                        {
                            this.Invoke(new MethodInvoker(EnableBtnFinish));
                            //notifyIcon1.ShowBalloonTip(5000, TASKNAME, "Software " + typelabel + " completed. Please click on 'Finish'.", ToolTipIcon.Info);
                            set_labeltext("Software " + typelabel + " is completed. Please click on 'Finish'.");
                            this.Invoke(new MethodInvoker(ThisRefresh));
                        }
                        else
                        {
                            GlobalClass.logger.Info("Software " + typelabel + " successfully completed.", GlobalClass.SECTION);
                            GlobalClass.logger.WritetoEventLog("Software " + typelabel + " successfully completed.", "INFO", GlobalClass.SECTION);
                            applicationExit = true;
                        }
                    }



                }
            }
            GlobalClass.logger.Info("ExitCode: " + GlobalClass.ExitCode, GlobalClass.SECTION);
            GlobalClass.logger.WritetoEventLog("ExitCode: " + GlobalClass.ExitCode, "INFO", GlobalClass.SECTION);
            if (applicationExit)
            {
                ResetTalkResponse();
                GlobalClass.logger.Info("**** Finished; exit application automatically ****", GlobalClass.SECTION);
                GlobalClass.logger.WritetoEventLog("Finished; exit application automatically.", "INFO", GlobalClass.SECTION);
                Application.Exit();
            }
            return;
        }

        //#endregion

        #region MessageBox

        private void ShowDialogGUI(string message, string type, int timeout)
        {
            string retValue = null;
            txtBoxBtn.Text = "";

            if (timeout > 0)
            {
                timer1.Enabled = true;
            }

            try
            {
                //if (message.Length > 0 && GlobalClass.IsUnattended == false)
                if (message.Length > 0 && Environment.UserInteractive)
                {
                    this.Invoke(new MethodInvoker(EnableGUI));
                    this.Invoke(new MethodInvoker(DisableAnimatedGif));
                    GlobalClass.DialogMessage = message;
                    GlobalClass.DialogType = type;
                    GlobalClass.TimeOut = timeout;


                    this.Invoke(new MethodInvoker(ShowPanelMessageBox));
                    while (retValue == null)
                    {
                        if (txtBoxBtn.Text.Length > 0)
                        {
                            retValue = txtBoxBtn.Text;
                        }
                        System.Threading.Thread.Sleep(1000);
                        if (timer1.Enabled)
                        {
                            GlobalClass.TimeOut = GlobalClass.TimeOut - 1;
                            if (GlobalClass.TimeOut <= 0)
                            {
                                retValue = "ok";
                                timer1.Enabled = false;
                            }
                        }

                    }

                    this.Invoke(new MethodInvoker(HideDialogPanel));

                    switch (retValue)
                    {
                        case "ok":
                            break;
                        case "yes":
                            break;
                        case "postpone":
                            switch (ShowPostponeGUI())
                            {
                                case 0:
                                    //nothing to do --> go on within the queue
                                    //notifyIcon1.ShowBalloonTip(100000, TASKNAME, "Installation continued...", ToolTipIcon.Info);
                                    break;
                                case 1:
                                    //postponed
                                    //notifyIcon1.ShowBalloonTip(10000, TASKNAME, "Installation postponed...", ToolTipIcon.Warning);
                                    abort = true;
                                    GlobalClass.IsAborted = true;
                                    break;
                                case 2:
                                    //installation cancelled
                                    //notifyIcon1.ShowBalloonTip(100000, TASKNAME, "Installation cancelled!", ToolTipIcon.Warning);
                                    abort = true;
                                    GlobalClass.IsAborted = true;
                                    break;
                            }
                            break;
                    }

                    /*
                    MessageDialog MessageDialog = new MessageDialog();
                    MessageDialog.Location = new System.Drawing.Point(this.Location.X, this.Location.Y - MessageDialog.Height - 5);
                    MessageDialog.TopMost = true;

                    switch(MessageDialog.ShowDialog())
                    {
                        case DialogResult.Yes:
                            //notifyIcon1.ShowBalloonTip(100000, TASKNAME, "Installation started...", ToolTipIcon.Info);
                            break;
                        case DialogResult.OK:
                            //notifyIcon1.ShowBalloonTip(100000, TASKNAME, "Installation continued...", ToolTipIcon.Info);
                            break;
                        case DialogResult.No:
                            switch (ShowPostponeGUI())
                            {
                                case 0:
                                    //nothing to do --> go on within the queue
                                    //notifyIcon1.ShowBalloonTip(100000, TASKNAME, "Installation continued...", ToolTipIcon.Info);
                                    break;
                                case 1:
                                    //postponed
                                    //notifyIcon1.ShowBalloonTip(10000, TASKNAME, "Installation postponed...", ToolTipIcon.Warning);
                                    abort = true;
                                    GlobalClass.IsAborted = true;
                                    break;
                                case 2:
                                    //installation cancelled
                                    //notifyIcon1.ShowBalloonTip(100000, TASKNAME, "Installation cancelled!", ToolTipIcon.Warning);
                                    abort = true;
                                    GlobalClass.IsAborted = true;
                                    break;
                            }
                            break;
                    }
                    MessageDialog.Dispose();
                    */

                }
                else
                {
                    this.Invoke(new MethodInvoker(EnableGUI));
                    this.Invoke(new MethodInvoker(DisableAnimatedGif));
                }
                this.Invoke(new MethodInvoker(EnableAnimatedGif));
                return;
            }
            catch (Exception)
            {
                return;
            }
        }

        private void ShowPanelMessageBox()
        {

            //Rich Text Box formating
            string tempDialogMsg = GlobalClass.DialogMessage;

            tempDialogMsg = tempDialogMsg.Replace("[n]", "\n");
            tempDialogMsg = tempDialogMsg.Replace("[t]", "\t");
            rtbDialogMessage.ForeColor = Color.Black;
            rtbDialogMessage.Text = tempDialogMsg;

            //Buttons / Pics / Others
            pictureBoxError.Visible = false;
            pictureBoxInfo.Visible = false;
            pictureBoxLoad.Visible = false;
            pictureBoxWarning.Visible = false;
            pictureBoxBattery.Visible = false;
            pictureBoxRunningProcess.Visible = false;
            btnOK.Visible = false;
            btnPostpone.Visible = false;
            btnStart.Visible = false;
            cBRunningProcessClose.Visible = false;
            cBRunningProcessClose.Checked = false;
            switch (GlobalClass.DialogType.ToUpper())
            {
                case "INFO":
                    pictureBoxInfo.Visible = true;
                    btnOK.Visible = true;
                    break;
                case "WARNING":
                    pictureBoxWarning.Visible = true;
                    btnOK.Visible = true;
                    rtbDialogMessage.ForeColor = Color.Red;
                    break;
                case "ERROR":
                    pictureBoxError.Visible = true;
                    btnOK.Visible = true;
                    rtbDialogMessage.ForeColor = Color.Red;
                    break;
                case "POSTPONE":
                    if (GlobalClass.TimeOut <= 0)
                    {
                        btnPostpone.Visible = true;
                    }
                    else
                    {
                        btnPostpone.Visible = false;
                    }
                    btnStart.Visible = true;
                    pictureBoxLoad.Visible = true;
                    break;
                case "BATTERY":
                    btnPostpone.Visible = true;
                    pictureBoxBattery.Visible = true;
                    break;
                case "ABORT":
                    btnOK.Visible = true;
                    pictureBoxError.Visible = true;
                    rtbDialogMessage.ForeColor = Color.Red;
                    break;
                case "PROCESS":
                    btnOK.Visible = true;
                    pictureBoxRunningProcess.Visible = true;
                    if (!(GlobalClass.RunningProcess == ""))
                    {
                        cBRunningProcessClose.Checked = false;
                        cBRunningProcessClose.Visible = true;
                    }
                    rtbDialogMessage.ForeColor = Color.Red;
                    break;
            }

            //Calculate height of the Dialog Text
            int LINESIZE = 100;
            int LINEHEIGHT = 15;
            int HEIGHT_TEXT = 0;

            string[] textlines = rtbDialogMessage.Text.Split(Convert.ToChar("\r"));

            foreach (string textline in textlines)
            {
                int textlinelength = textline.Length;
                int sublines = (textlinelength / LINESIZE) + 1;

                HEIGHT_TEXT = HEIGHT_TEXT + (sublines * LINEHEIGHT);
            }
            //cosmetic
            if (HEIGHT_TEXT >= LINEHEIGHT)
            {
                HEIGHT_TEXT -= LINEHEIGHT;
            }

            //Dimensions / Locations
            this.Height = HEIGHT_MAINFORM + HEIGHT_CHILDFORM + HEIGHT_SPACE + HEIGHT_TEXT;
            this.Location = new Point(this.Location.X, this.Location.Y - (HEIGHT_CHILDFORM + HEIGHT_SPACE + HEIGHT_TEXT));
            this.panelMessageBox.Height = HEIGHT_CHILDFORM + HEIGHT_TEXT;
            this.panelMessageBoxMessage.Height = this.panelMessageBox.Height - this.panelMessageBoxControls.Height - 10;
            this.rtbDialogMessage.Height = this.panelMessageBox.Height;
            this.panelMain.Location = new Point(0, HEIGHT_CHILDFORM + HEIGHT_SPACE + HEIGHT_TEXT);
            //rtbDialogMessage.Location = new Point(rtbDialogMessage.Location.X, rtbDialogMessage.Location.Y - ((sublines-2) * LINEHEIGHT));

            this.panelMessageBox.Visible = true;
            this.Refresh();
        }

        private void HideDialogPanel()
        {
            //Calculate height of the Dialog Text
            int LINESIZE = 100;
            int LINEHEIGHT = 15;
            int HEIGHT_TEXT = 0;

            string[] textlines = rtbDialogMessage.Text.Split(Convert.ToChar("\r"));

            foreach (string textline in textlines)
            {
                int textlinelength = textline.Length;
                int sublines = (textlinelength / LINESIZE) + 1;

                HEIGHT_TEXT = HEIGHT_TEXT + (sublines * LINEHEIGHT);
            }
            //cosmetic
            if (HEIGHT_TEXT >= LINEHEIGHT)
            {
                HEIGHT_TEXT -= LINEHEIGHT;
            }


            this.rtbDialogMessage.Height = 0;
            this.panelMessageBoxMessage.Height = 0;
            this.panelMessageBox.Height = 0;
            this.panelMain.Location = new Point(0, 0);
            this.Height = HEIGHT_MAINFORM;
            this.Location = new Point(this.Location.X, this.Location.Y + (HEIGHT_CHILDFORM + HEIGHT_SPACE + HEIGHT_TEXT));

            this.panelMessageBox.Visible = false;
            this.Refresh();

            GlobalClass.DialogMessage = "";
            GlobalClass.DialogType = "";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            txtBoxBtn.Text = "ok";

            //close a running process automatically if the dialog type is "PROCESS" and the corresponding checkbox is checked by the user
            if (GlobalClass.DialogType.ToUpper() == "PROCESS")
            {
                if (cBRunningProcessClose.Checked && !(GlobalClass.RunningProcess == ""))
                {
                    Functions myFunctions = new Functions();
                    Logging myLogger = new Logging();
                    myLogger.Info("TASK (XMLnode:'messagebox'): Information (type: 'PROCESS' --> running process '" + GlobalClass.RunningProcess + "' closing automatically...)", GlobalClass.SECTION);
                    myFunctions.KillProcess(GlobalClass.RunningProcess);
                }
            }

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            txtBoxBtn.Text = "yes";
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            txtBoxBtn.Text = "postpone";
        }

        #endregion

        #region RebootDialog

        private bool ShowRebootGUI()
        {
            string retValue = null;
            txtBoxBtnReboot.Text = "";
            try
            {
                if (Environment.UserInteractive)
                {
                    this.Invoke(new MethodInvoker(EnableGUI));
                    this.Invoke(new MethodInvoker(DisableAnimatedGif));
                    bool rebootNow = false;

                    this.Invoke(new MethodInvoker(ShowPanelReboot));

                    /*
                    //if(GlobalClass.IsUnattended == false)
                    //{
                    RebootDialog MsgRebootDialog = new RebootDialog();
                    MsgRebootDialog.Location = new System.Drawing.Point(this.Location.X, this.Location.Y - MsgRebootDialog.Height - 5);
                    MsgRebootDialog.TopMost = true;

                    while (MsgRebootDialog.ShowDialog() == DialogResult.OK && GlobalClass.RestartNow > 0)
                    {
                        //MessageBox.Show("wait for " + GlobalClass.RestartNow + " minutes");
                        this.Invoke(new MethodInvoker(DisableGUI));
                        Thread.Sleep((int)GlobalClass.RestartNow * 60 * 1000);
                        this.Invoke(new MethodInvoker(EnableGUI));
                    }
                    
                    MsgRebootDialog.Dispose();
                    //}
                    */

                    while (retValue == null)
                    {
                        if (txtBoxBtnReboot.Text.Length > 0)
                        {
                            retValue = txtBoxBtnReboot.Text;
                        }
                    }

                    while (retValue == "ok" && GlobalClass.RestartNow > 0)
                    {
                        //MessageBox.Show("wait for " + GlobalClass.RestartNow + " minutes");
                        this.Invoke(new MethodInvoker(DisableGUI));
                        Thread.Sleep((int)GlobalClass.RestartNow * 60 * 1000);
                        this.Invoke(new MethodInvoker(EnableGUI));
                    }

                    this.Invoke(new MethodInvoker(HidePanelReboot));

                    if (GlobalClass.RestartNow == 0)
                    {
                        rebootNow = true;
                    }
                    return rebootNow;
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

        private void ShowPanelReboot()
        {
            //Dimensions / Locations
            this.Height = HEIGHT_MAINFORM + HEIGHT_CHILDFORM + HEIGHT_SPACE;
            this.Location = new Point(this.Location.X, this.Location.Y - (HEIGHT_CHILDFORM + HEIGHT_SPACE));
            this.panelReboot.Height = HEIGHT_CHILDFORM;
            this.panelMain.Location = new Point(0, HEIGHT_CHILDFORM + HEIGHT_SPACE);

            panelReboot.Visible = true;
            this.Refresh();
        }

        private void HidePanelReboot()
        {
            this.panelReboot.Height = 0;
            this.panelMain.Location = new Point(0, 0);
            this.Height = HEIGHT_MAINFORM;
            this.Location = new Point(this.Location.X, this.Location.Y + (HEIGHT_CHILDFORM + HEIGHT_SPACE));

            panelReboot.Visible = false;
            this.Refresh();
        }

        private void radioBtnRebootRestartNow_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownMinutes.Enabled = false;
            dateTimePickerDate.Enabled = false;
            dateTimePickerTime.Enabled = false;
        }

        private void radioBtnRebootRemindMeIn_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownMinutes.Enabled = true;
            dateTimePickerDate.Enabled = false;
            dateTimePickerTime.Enabled = false;
        }

        private void radioBtnRebootRestartOn_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownMinutes.Enabled = false;
            dateTimePickerDate.Enabled = true;
            dateTimePickerTime.Enabled = true;
        }

        private void radioBtnRebootDontBotherMeAgain_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownMinutes.Enabled = false;
            dateTimePickerDate.Enabled = false;
            dateTimePickerTime.Enabled = false;
        }

        private void btnRebootOK_Click(object sender, EventArgs e)
        {
            if (radioBtnRebootRestartNow.Checked)
            {
                GlobalClass.RestartNow = 0;
            }

            if (radioBtnRebootRemindMeIn.Checked)
            {
                //get value from numericUpDown
                double minutes = (double)numericUpDownMinutes.Value;
                GlobalClass.RestartNow = minutes;
            }

            if (radioBtnRebootRestartOn.Checked)
            {
                Functions myFunctions = new Functions();

                //get value from dateTimePickers
                DateTime d = dateTimePickerDate.Value;
                DateTime t = dateTimePickerTime.Value;
                string starttime = d.ToString("yyyyMMdd") + t.ToString("HHmm");
                uint returnValue = myFunctions.CreateScheduledRebootTask(starttime);
                GlobalClass.logger.Info("CreateScheduledTask; returnValue: " + returnValue.ToString(), GlobalClass.SECTION);
                GlobalClass.RestartNow = -1;
            }
            if (radioBtnRebootDontBotherMeAgain.Checked)
            {
                GlobalClass.RestartNow = -1;
            }

            txtBoxBtnReboot.Text = "ok";
        }

        #endregion

        #region PostponeDialog

        private int ShowPostponeGUI()
        {
            string retValue = null;
            txtBoxBtnPostpone.Text = "";
            try
            {
                if (GlobalClass.ShowPostponeDialog)
                {
                    if (Environment.UserInteractive)
                    {
                        this.Invoke(new MethodInvoker(EnableGUI));
                        this.Invoke(new MethodInvoker(DisableAnimatedGif));

                        this.Invoke(new MethodInvoker(ShowPanelPostpone));
                        while (retValue == null)
                        {
                            if (txtBoxBtnPostpone.Text.Length > 0)
                            {
                                retValue = txtBoxBtnPostpone.Text;
                            }
                        }

                        this.Invoke(new MethodInvoker(HidePanelPostpone));
                        return GlobalClass.PostponeType;

                        /*
                        //if (GlobalClass.IsUnattended == false)
                        //{
                        PostponeDialog MsgPostponeDialog = new PostponeDialog();
                        MsgPostponeDialog.Location = new System.Drawing.Point(this.Location.X, this.Location.Y - MsgPostponeDialog.Height - 5);
                        MsgPostponeDialog.TopMost = true;
                        if (MsgPostponeDialog.ShowDialog() == DialogResult.OK)
                        {
                            //nothing to do
                        }
                        MsgPostponeDialog.Dispose();
                        return GlobalClass.PostponeType;
                        //}
                        */

                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    //postponed (just click the button) without the dialog
                    //Postpone type: 2 (cancelled)
                    GlobalClass.IsPostponed = true;
                    GlobalClass.PostponeType = 2;
                    return 2;
                }
            }
            catch
            {
                return 0;
            }
        }

        private void ShowPanelPostpone()
        {
            //Dimensions / Locations
            this.Height = HEIGHT_MAINFORM + HEIGHT_CHILDFORM + HEIGHT_SPACE;
            this.Location = new Point(this.Location.X, this.Location.Y - (HEIGHT_CHILDFORM + HEIGHT_SPACE));
            this.panelPostpone.Height = HEIGHT_CHILDFORM;
            this.panelMain.Location = new Point(0, HEIGHT_CHILDFORM + HEIGHT_SPACE);

            panelPostpone.Visible = true;
            this.Refresh();
        }

        private void HidePanelPostpone()
        {
            this.panelPostpone.Height = 0;
            this.panelMain.Location = new Point(0, 0);
            this.Height = HEIGHT_MAINFORM;
            this.Location = new Point(this.Location.X, this.Location.Y + (HEIGHT_CHILDFORM + HEIGHT_SPACE));

            panelPostpone.Visible = false;
            this.Refresh();
        }

        private void btnPostponeOK_Click(object sender, EventArgs e)
        {
            //init Functions
            Functions myFunctions = new Functions();
            //log Appender
            myFunctions.UpdateFileAppenderPath(GlobalClass.LogFileProductCode);

            try
            {
                GlobalClass.PostponeType = 1;
                GlobalClass.IsPostponed = true;
                if (radioButtonStartNow.Checked)
                {
                    GlobalClass.PostponeType = 0;
                    GlobalClass.IsPostponed = false;
                }
                if (radioButtonCancel.Checked)
                {
                    GlobalClass.PostponeType = 2;
                    GlobalClass.IsPostponed = true;
                }
                if (radioButtonRestartIn.Checked)
                {
                    //get value from numericUpDown
                    double minutes = (double)numericUpDownMinutes.Value;
                    DateTime t = DateTime.Now.AddMinutes(minutes);
                    string starttime = t.ToString("yyyyMMddHHmm");
                    string taskID = "";
                    try
                    {
                        taskID = myFunctions.GetRunningAltirisTask();
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        GlobalClass.logger.Warn("Altiris Agent NOT found", GlobalClass.SECTION);
                    }
                    finally
                    {
                        uint returnValue = myFunctions.CreateScheduledTask(starttime, taskID);
                        GlobalClass.logger.Info("CreateScheduledTask; returnValue: " + returnValue.ToString(), GlobalClass.SECTION);
                    }
                }
                if (radioButtonRestartOn.Checked)
                {
                    //get value from dateTimePickers
                    DateTime d = dateTimePickerDate.Value;
                    DateTime t = dateTimePickerTime.Value;
                    string starttime = d.ToString("yyyyMMdd") + t.ToString("HHmm");
                    string taskID = "";
                    try
                    {
                        taskID = myFunctions.GetRunningAltirisTask();
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        GlobalClass.logger.Warn("Altiris Agent NOT found", GlobalClass.SECTION);
                    }
                    finally
                    {
                        uint returnValue = myFunctions.CreateScheduledTask(starttime, taskID);
                        GlobalClass.logger.Info("CreateScheduledTask; returnValue: " + returnValue.ToString(), GlobalClass.SECTION);
                    }
                }
                txtBoxBtnPostpone.Text = "ok";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void radioButtonRemindMeIn_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePickerDate.Enabled = false;
            dateTimePickerTime.Enabled = false;
            numericUpDownMinutes.Enabled = true;
        }

        private void radioButtonRestartOn_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDownMinutes.Enabled = false;
            dateTimePickerDate.Enabled = true;
            dateTimePickerTime.Enabled = true;
        }

        private void radioButtonRestartNow_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePickerDate.Enabled = false;
            dateTimePickerTime.Enabled = false;
            numericUpDownMinutes.Enabled = false;
        }

        private void radioButtonDontBotherMeAgain_CheckedChanged(object sender, EventArgs e)
        {
            dateTimePickerDate.Enabled = false;
            dateTimePickerTime.Enabled = false;
            numericUpDownMinutes.Enabled = false;
        }

        #endregion

        #region MainForm

        private void ShowMainGUI(string message, string type, bool showRestart, bool showCancel, bool showFinish, int timeOut)
        {
            //buttons
            if (showFinish)
            {
                this.Invoke(new MethodInvoker(EnableBtnFinish));
            }
            this.Invoke(new MethodInvoker(EnableGUI));
            //MessageBox.Show("GUI enabled");
            GlobalClass.IsMainGUIEnabled = true;

            if (message != "")
            {
                //Dialog(s)
                GlobalClass.DialogMessage = message;
                /*
                switch (type.ToUpper())
                {
                    case "INFO":
                        notifyIcon1.ShowBalloonTip(100000, TASKNAME, message, ToolTipIcon.Info);
                        break;
                    case "WARNING":
                        notifyIcon1.ShowBalloonTip(100000, TASKNAME, message, ToolTipIcon.Warning);
                        break;
                    case "ERROR":
                        notifyIcon1.ShowBalloonTip(100000, TASKNAME, message, ToolTipIcon.Error);
                        break;
                    case "POSTPONE":
                        notifyIcon1.ShowBalloonTip(100000, TASKNAME, message, ToolTipIcon.Info);
                        break;
                    case "BATTERY":
                        notifyIcon1.ShowBalloonTip(100000, TASKNAME, message, ToolTipIcon.Warning);
                        break;
                    case "ABORT":
                        notifyIcon1.ShowBalloonTip(100000, TASKNAME, message, ToolTipIcon.Error);
                        break;
                }
                */

                ShowDialogGUI(message, type.ToUpper(), timeOut);
            }
            else
            {
                ShowDialogGUI("", type.ToUpper(), timeOut);
            }
            return;
        }

        private void DisableAnimatedGif()
        {
            pictureBoxAnimatedGif.Visible = false;
            this.Refresh();
        }

        private void EnableInstallQueuePic()
        {
            picInstallQueue.Visible = true;
        }

        private void EnableUnInstallQueuePic()
        {
            picUninstallQueue.Visible = true;
        }

        private void EnableRepairQueuePic()
        {
            picRepairQueue.Visible = true;
        }

        private void DisableQueuePics()
        {
            picRepairQueue.Visible = false;
            picUninstallQueue.Visible = false;
            picInstallQueue.Visible = false;
        }

        private void EnableAnimatedGif()
        {
            pictureBoxAnimatedGif.Visible = true;
            this.Refresh();
        }

        private void EnableBtnFinish()
        {
            btnFinshed.Visible = true;
            this.Refresh();
        }

        private void EnableGUI()
        {
            //set opacitiy to 95% if it's W7
            Functions myFunctions = new Functions();
            if (myFunctions.CheckOS("Windows 7", null, null, null))
            {
                this.Opacity = OPACITY;
                GlobalClass.OpacityValue = OPACITY;
            }
            else
            {
                GlobalClass.OpacityValue = 1;
            }
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.Refresh();
        }

        private void DisableGUI()
        {
            this.Visible = false;
            this.WindowState = FormWindowState.Minimized;
            this.Refresh();
        }

        private void SetRichTextBoxMetaData()
        {
            //richTextBoxMetaData.Text = "";
            richTextBoxMetaData.Text=("'" + productname + "'");
            //richTextBoxMetaData.AppendText("Product name: '" + productname + "'");
            //richTextBoxMetaData.AppendText("\r\nProduct version:\t" + productversion);
            //richTextBoxMetaData.AppendText("\r\nProduct code:\t" + productcode);            
            SetTalkMetaData(productname);
            this.Refresh();
        }

        private void SetLabel(string XMLnode, string labelText, string logDetails)
        {
            set_labeltext(labelText);
            GlobalClass.logger.Info("TASK (XMLnode:'" + XMLnode + "'): " + labelText + " (" + logDetails + ")", GlobalClass.SECTION);
        }

        private void c_ThresholdReached(object sender, EventArgs e)
        {
            set_labeltext(((RunXML)sender).ActualLabel);
        }

        private void set_labeltext(string labeltext)
        {
            //logger.Info("set_labeltext --> DoCheapGUIAccess: " + labeltext);

            try
            {
                lblCurrentTask.Invoke(new label_handler(DoCheapGUIAccess), new Object[] { labeltext });
            }
            catch (Exception)
            {
                //do nothing; go on without setting the Label
            }
            //lblCurrentTask.BeginInvoke(new label_handler(DoCheapGUIAccess), new Object[] { labeltext });
            /*
            try
            {
                logger.Info("set_labeltext; invoke required: " + this.lblCurrentTask.InvokeRequired);
                if (this.lblCurrentTask.InvokeRequired)
                {
                    
                    lblCurrentTask.Invoke(new label_handler(set_labeltext),new Object[] { labeltext });
                }
                else
                {
                    lblCurrentTask.Text = labeltext;
                    lblCurrentTask.Visible = true;
                    lblCurrentTaskName.Visible = true;
                    this.Refresh();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, TASKNAME, MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
            */
        }
        private delegate void label_handler(string labeltext);

        private void btnFinshed_Click(object sender, EventArgs e)
        {
            GlobalClass.logger.Info("**** Finished; close application ****", GlobalClass.SECTION);
            GlobalClass.logger.Info("*************************************", GlobalClass.SECTION);
            GlobalClass.logger.Info("", GlobalClass.SECTION);
            GlobalClass.logger.WritetoEventLog("Finished; close application.", "INFO", GlobalClass.SECTION);
            WriteTalkMessage("currenttask", "");
            this.Close();
        }

        private void DoCheapGUIAccess(string labeltext)
        {
            //logger.Info("DoCheapGUIAccess: " + labeltext);
            this.Activate();
            this.lblCurrentTask.Text = labeltext;
            //this.lblCurrentTask.Visible = true;
            this.richTextBoxCurrentTask.Text = labeltext;
            this.lblCurrentTaskName.Visible = true;
            this.Refresh();
            this.Invalidate();

            WriteTalkMessage("currenttask", labeltext);
            /*
            this.Activate();
            this.BringToFront();
            this.Focus();
            this.Show();
            */
        }

        private void ThisRefresh()
        {
            this.Refresh();
            this.Invalidate();
        }

        private void ThisRefresh(Object stateInfo)
        {
            this.Activate();
            //this.Refresh();
            this.Invalidate();
        }

        private void WriteTalkMessage(string name, string value)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey sk1 = rk.CreateSubKey(@"Software\cluebiz\deliveries_setup\talk");
                sk1.SetValue(name, value);
            }
            catch
            {
            }
        }

        private string WaitForTalkResponse()
        {
            string responseText = "";
            do
            {
                try
                {
                    RegistryKey rk = Registry.LocalMachine;
                    RegistryKey sk1 = rk.OpenSubKey(@"Software\cluebiz\deliveries_setup\talk");
                    responseText = sk1.GetValue("responsetime", "").ToString();
                }
                catch { }
                System.Threading.Thread.Sleep(1000);
                Application.DoEvents();
            } while (responseText == "");
            responseText = "";
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey sk1 = rk.OpenSubKey(@"Software\cluebiz\deliveries_setup\talk");
                responseText = sk1.GetValue("responsearguments", "").ToString();
            }
            catch { }
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey sk1 = rk.CreateSubKey(@"Software\cluebiz\deliveries_setup\talk");
                sk1.SetValue("responsetime", "");
                sk1.SetValue("requesttime", "");
            }
            catch
            {
            }
            return responseText;
        }

        private void SetTalkMetaData(string product)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey sk1 = rk.CreateSubKey(@"Software\cluebiz\deliveries_setup\talk");
                sk1.SetValue("metaproduct", product);
            }
            catch { }
        }

        private void ResetTalkResponse()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey sk1 = rk.CreateSubKey(@"Software\cluebiz\deliveries_setup\talk");
                sk1.SetValue("responsetime", "");
                sk1.SetValue("responsearguments", "");
                sk1.SetValue("currenttask", "");
                sk1.SetValue("requesttime", "");
                sk1.SetValue("requestarguments", "");
                sk1.SetValue("requestcommand", "");
                sk1.SetValue("requesttime", "");
                sk1.SetValue("metaproduct", "");
            }
            catch { }
        }

        #endregion

        #region Misc

        //method to execute a scheduled task which executes an altiris task
        private void CreateScriptFile(string pathname, string filename)
        {
            try
            {
                Directory.CreateDirectory(pathname);
                FileInfo fi = new FileInfo(pathname + filename);
                StreamWriter sw = fi.CreateText();
                sw.WriteLine("On Error Resume Next");
                sw.WriteLine("Const runByUser = 1");
                sw.WriteLine("Dim goArgs,goAgent,goSWDAgent,goTasks,goTask,giCount,gsTaskID");
                sw.Write(sw.NewLine);
                sw.WriteLine("Set goArgs = WScript.Arguments");
                sw.WriteLine("Set goAgent = CreateObject(" + (char)34 + "Altiris.AeXNSClient" + (char)34 + ")");
                sw.WriteLine("Set goSWDAgent = goAgent.ClientPolicyMgr.ClientAgent(" + (char)34 + "Altiris.SWD" + (char)34 + ")");
                sw.WriteLine("Set goTasks = goSWDAgent.Advertisements");
                sw.Write(sw.NewLine);
                sw.WriteLine("If goArgs.Count > 0 Then");
                sw.WriteLine("\tFor giCount=0 To goArgs.Count - 1");
                sw.WriteLine("\t\tIf InStr(UCase(goArgs(giCount))," + (char)34 + "ID=" + (char)34 + ") > 0 Then");
                sw.WriteLine("\t\t\tgsTaskID = Replace(UCase(goArgs(giCount))," + (char)34 + "ID=" + (char)34 + "," + (char)34 + (char)34 + ")");
                sw.WriteLine("\t\tEnd If");
                sw.WriteLine("\tNext");
                sw.WriteLine("End If");
                sw.Write(sw.NewLine);
                //sw.WriteLine("For Each goTask In goTasks");
                //sw.WriteLine("\tIf goTask.ID = gsTaskID Then");
                //sw.WriteLine("\t\tgoSWDAgent.RunAdvertisement gsTaskID, runByUser");
                sw.WriteLine("goSWDAgent.RunAdvertisement gsTaskID, runByUser");
                //sw.WriteLine("\tEnd If");
                //sw.WriteLine("Next");
                sw.Close();
                return;
            }
            catch (Exception)
            {
                return;
            }
        }

        private String GetLoggedOnUser()
        {
            try
            {
                /*
                LogonType : 2
                Intended for users who are interactively using the machine, such as a user being logged on by a terminal server, remote shell, or similar process.
                */


                String lsLogonID = "";
                String lsName = "";
                String lsFullName = "";
                String lsRetName = "";
                ManagementObjectSearcher qry = new ManagementObjectSearcher("SELECT * FROM Win32_LogonSession WHERE LogonType=2 OR LogonType=10");
                ManagementObjectCollection qryCol = qry.Get();
                foreach (ManagementObject mo in qryCol)
                {
                    PropertyDataCollection propCol = mo.Properties;
                    foreach (PropertyData propdata in propCol)
                    {
                        if (propdata.Name.ToUpper() == "LOGONID")
                        {
                            if (propdata.Value != null)
                            {
                                lsLogonID = propdata.Value.ToString();
                                lsName = propdata.Value.ToString();
                                lsFullName = propdata.Value.ToString();
                            }
                        }
                    }
                }
                if (lsName.Length == 0)
                {
                    //nobody is logged on
                    return "";
                }
                else
                {
                    //somebody is logged on
                    lsRetName = lsName;
                    if (lsFullName.Length > 0)
                    {
                        lsRetName = lsRetName + " (" + lsFullName + ")";
                    }
                    return lsRetName;
                }
            }
            catch (Exception)
            {
                //default: a user is logged on
                return "unknown";
            }

        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            bSleepStop = true;
            notifyIcon1.ShowBalloonTip(10000);
        }

        private void HandleExceptions(Exception ex, int exCode)
        {
            Functions myFunctions = new Functions();

            //log file
            myFunctions.UpdateFileAppenderPath(LOG_EXCEPTIONS_PATH + LOG_EXCEPTIONS_FILE);
            GlobalClass.logger.Error("###" + exCode + "###" + ex.ToString(), GlobalClass.SECTION);
            GlobalClass.logger.WritetoEventLog("###" + exCode + "###" + ex.ToString(), GlobalClass.LERROR, GlobalClass.SECTION);

            //messages
            notifyIcon1.ShowBalloonTip(10000, TASKNAME, ex.Message, ToolTipIcon.Error);
            if (isUserLoggedOn)
                MessageBox.Show("###" + exCode + "###\n\r" + ex.Message + "\n\n\rPlease consider: '" + LOG_EXCEPTIONS_PATH + LOG_EXCEPTIONS_FILE + "'", TASKNAME, MessageBoxButtons.OK, MessageBoxIcon.Error);

            //send to webservice
            //...

            //set abort flag(s) & ExitCode
            abort = true;
            GlobalClass.IsAborted = abort;
            GlobalClass.ExitCode = exCode;
        }

        private void HandleUnvalidLogFilePath(string logfilepath, int exCode)
        {
            //eventlog
            GlobalClass.logger.WritetoEventLog("###" + exCode + "###" + " .Log File Path '" + logfilepath + "' not valid!", GlobalClass.LERROR, GlobalClass.SECTION);
            //messages
            notifyIcon1.ShowBalloonTip(10000, TASKNAME, ".Log File Path '" + logfilepath + "' not valid!", ToolTipIcon.Error);
            if (isUserLoggedOn)
                MessageBox.Show("###" + exCode + "###\n\r" + " .Log File Path '" + logfilepath + "' not valid!", TASKNAME, MessageBoxButtons.OK, MessageBoxIcon.Error);

            //set abort flag(s) & ExitCode
            abort = true;
            GlobalClass.IsAborted = abort;
            GlobalClass.ExitCode = exCode;
        }

        private void deploySettings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            Functions myFunctions = new Functions();

            myFunctions.UpdateFileAppenderPath(LOG_EXCEPTIONS_PATH + LOG_EXCEPTIONS_FILE);
            if (e.Severity == XmlSeverityType.Warning)
            {
                //logging & MsgBox
                GlobalClass.logger.Warn(e.ToString(), GlobalClass.SECTION);
                GlobalClass.ExitCode = GlobalClass.EC_XMLValidationWarning;
                notifyIcon1.ShowBalloonTip(10000, TASKNAME, "XML Validation -- Warning...", ToolTipIcon.Warning);
                MessageBox.Show(e.Message, TASKNAME, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (e.Severity == XmlSeverityType.Error)
            {
                //logging & MsgBox
                GlobalClass.logger.Error(e.ToString(), GlobalClass.SECTION);
                GlobalClass.ExitCode = GlobalClass.EC_XMLValidationError;
                notifyIcon1.ShowBalloonTip(10000, TASKNAME, "XML Validation -- Error...", ToolTipIcon.Error);
                MessageBox.Show(e.Message, TASKNAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            abort = true;
            GlobalClass.IsAborted = abort;
        }

        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            //gTimeout = gTimeout - 1;
            //lblTimeout.Text = gTimeout.ToString();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private string GetGuidFromProductCode(string lsProductCode)
        {
            string lsReturn = lsProductCode;

            if (!lsProductCode.StartsWith("{"))
            {
                string lsHash = CalculateMD5Hash(lsProductCode);
                lsHash = lsHash + "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
                lsReturn = "{" +  lsHash.Substring(0, 8) + "-" + lsHash.Substring(8, 4) + "-" + lsHash.Substring(12, 4) + "-" + lsHash.Substring(16, 4) + "-" + lsHash.Substring(20, 12) + "}";
            }

            return lsReturn;

        }



        private string CalculateMD5Hash(string input)
        {

            // step 1, calculate MD5 hash from input

            MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)

            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();

        }


        private static void WriteResourceToFile(string resourceName, string fileName)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    resource.CopyTo(file);
                }
            }
        }

        private string nodeGetAttribute(XmlNode node, string attr)
        {
            string ret = "";
            try { ret = node.Attributes.GetNamedItem(attr).Value.ToString(); }
            catch { }
            return ret;
        }

        private void formMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason==CloseReason.WindowsShutDown)
            {
                Process.Start("shutdown", "-a");
            }
        }
    }


    public class Folder
    {
        private static double sizeInBytes;

        public static double Size(string directory, bool deep)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(directory);
                foreach (FileInfo f in dir.GetFiles())
                {
                    sizeInBytes += f.Length;
                }
                if (deep)
                {
                    foreach (DirectoryInfo d in dir.GetDirectories())
                    {
                        Size(d.FullName, deep);
                    }
                }
                return sizeInBytes;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    #endregion


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

        /// <summary>
        /// Run command as user(s)
        /// </summary>
        /// <param name="CommandLine">command to execute</param>
        /// <param name="sUser"> "all", "active", "LANBOX64\\daniel"</param>
        /// <param name="bElevate">run as admin (not working?)</param>
        /// <returns>on error:-1, else number of succesful user-runs</returns>
        public static int RunCmd(String CommandLine, string sUser = "all", bool bElevate = false)
        {

            GlobalClass.logger.Info("now inside runcmd", GlobalClass.SECTION);

            // active user session
            uint dwSessionId = WTSGetActiveConsoleSessionId();

            GlobalClass.logger.Info("dwSessionId " + dwSessionId.ToString(), GlobalClass.SECTION);


            // Find the winlogon process
            var procEntry = new PROCESSENTRY32();
            uint hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (hSnap == INVALID_HANDLE_VALUE) return -1;

            procEntry.dwSize = (uint)Marshal.SizeOf(procEntry); //sizeof(PROCESSENTRY32);
            if (Process32First(hSnap, ref procEntry) == 0) return -1;

            int runs = 0;
            List<string> doneUsersArr = new List<string>();
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
                    GlobalClass.logger.Info("userName " + userName.ToString(), GlobalClass.SECTION);

                }
                else
                {
                    GlobalClass.logger.Error(String.Format("RunCmd OpenProcessToken error: {0}", Marshal.GetLastWin32Error()), GlobalClass.SECTION);
                    continue;
                }

                // different sUser modes (all, active, LANBOX64\\daniel)

                GlobalClass.logger.Info("running task for  " + sUser, GlobalClass.SECTION);

                uint winlogonSessId = 0;
                if (sUser == "all")
                {
                    if (doneUsersArr.Contains(userName)) continue; // userName is in list: already done
                    if (RunCmdWithProcessToken(CommandLine, bElevate, hPToken)) runs += 1;
                    doneUsersArr.Add(userName);
                }
                else if (sUser == "active" && ProcessIdToSessionId(procEntry.th32ProcessID, ref winlogonSessId) && winlogonSessId == dwSessionId)
                {
                    if (RunCmdWithProcessToken(CommandLine, bElevate, hPToken)) runs = 1;
                    break;
                }
                else if (sUser == userName)
                {
                    if (RunCmdWithProcessToken(CommandLine, bElevate, hPToken)) runs = 1;
                    break;
                }

                CloseHandle(hProcess);
                CloseHandle(hPToken);
            }
            while (Process32Next(hSnap, ref procEntry) != 0);

            return runs;
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

            if (!LookupPrivilegeValue(IntPtr.Zero, SE_DEBUG_NAME, ref luid))
            {
                GlobalClass.logger.Error(String.Format("RunCmdWithProcessToken LookupPrivilegeValue error: {0}", Marshal.GetLastWin32Error()), GlobalClass.SECTION);
            }

            var sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
            {
                GlobalClass.logger.Error((String.Format("RunCmdWithProcessToken DuplicateTokenEx error: {0} Token does not have the privilege.", Marshal.GetLastWin32Error())), GlobalClass.SECTION);
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
                    GlobalClass.logger.Error(String.Format("RunCmdWithProcessToken SetTokenInformation error: {0} Token does not have the privilege.", Marshal.GetLastWin32Error()), GlobalClass.SECTION);
                }

                //Adjust Token privilege
                if (!AdjustTokenPrivileges(hUserTokenDup, false, ref tp, Marshal.SizeOf(tp), /*(PTOKEN_PRIVILEGES)*/IntPtr.Zero, IntPtr.Zero))
                {
                    GlobalClass.logger.Error(String.Format("RunCmdWithProcessToken AdjustTokenPrivileges error: {0}", Marshal.GetLastWin32Error()), GlobalClass.SECTION);
                }
            }

            uint dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;
            IntPtr pEnv = IntPtr.Zero;
            if (CreateEnvironmentBlock(ref pEnv, hUserTokenDup, true)) dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
            else pEnv = IntPtr.Zero;

            // Launch the process in the client's logon session.

            GlobalClass.logger.Info("CreateProcessAsUser  " + hUserTokenDup.ToString() + " - " + CommandLine, GlobalClass.SECTION);

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

            //GetLastError should be 0
            return (Marshal.GetLastWin32Error() == 0);
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



}
