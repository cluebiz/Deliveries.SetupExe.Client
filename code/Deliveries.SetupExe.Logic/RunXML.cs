using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Deliveries.SetupExe.Logic
{
    public class RunXML
    {
        public string ActualLabel;
        public event EventHandler SetLabelEvent;

        //griner: make static myFunction to keep values between calls
        private Functions myFunctions = new Functions();

        public bool ExecuteTask(XmlNode node)
        {
            //variables, objects
            bool isOK = true;
            int returnvalue = 0;
            string errormsg = "";
            const int STOPSERVICE = 0;
            const int STARTSERVICE = 1;

            bool abort = false;

            //logger.Info("ExecuteTask; XMLnode: " + node.Name);

            if (GlobalClass.IsAborted)
            {
                return GlobalClass.IsAborted;
            }

            switch (node.Name)
            {
                case "checkdisk":
                    string optionaldiskspace = "";
                    try
                    {
                        optionaldiskspace = node.Attributes.GetNamedItem("space").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (value was not defined and so, it's not relevant in that case)
                    }

                    string physicaldrive = "true"; //default
                    try
                    {
                        physicaldrive = node.Attributes.GetNamedItem("physical").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (default is "true")
                    }


                    SetLabel(node.Name, "Check disk and available disk space (optional)", "drive: '" + node.Attributes.GetNamedItem("driveletter").Value.ToString() + ":' - required disk space: " + optionaldiskspace + "MB - physical drive: '" + physicaldrive + "'");
                    isOK = myFunctions.CheckDiskSpace(node.Attributes.GetNamedItem("driveletter").Value.ToString(), optionaldiskspace, physicaldrive);
                    if (isOK == false)
                    {
                        GlobalClass.logger.Warn("::" + node.Name + " --> not existing drive or NOT enough available diskspace!", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                        GlobalClass.ExitCode = GlobalClass.EC_NotEnoughDiskspace;
                    }
                    else
                        GlobalClass.logger.Info("::" + node.Name + " --> drive available and free diskspace ok!", GlobalClass.SECTION);
                    break;

                case "checkmemory":
                    SetLabel(node.Name, "Check Physical Memory", node.Attributes.GetNamedItem("memory").Value.ToString() + "MB");
                    isOK = myFunctions.CheckMemory(node.Attributes.GetNamedItem("memory").Value.ToString());
                    if (isOK == false)
                    {
                        GlobalClass.logger.Warn("::" + node.Name + " --> not enough physical memory available!", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                        GlobalClass.ExitCode = GlobalClass.EC_NotEnoughMemory;
                    }
                    else
                        GlobalClass.logger.Info("::" + node.Name + " --> physical memory ok!", GlobalClass.SECTION);
                    break;

                case "checkbattery":
                    SetLabel(node.Name, "Check battery mode", "");

                    while (!myFunctions.CheckBattery() && !abort)
                    {
                        GlobalClass.logger.Warn("::" + node.Name + " --> ACLineStatus: 'offline' (Battery mode)", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                    }
                    if (!abort)
                    {
                        GlobalClass.logger.Info("::checkbattery --> ACLineStatus: 'online'", GlobalClass.SECTION);
                    }
                    break;

                case "checkfullscreen":
                    SetLabel(node.Name, "Check full screen mode", "'LOOP' attribute:" + node.Attributes.GetNamedItem("loop").Value.ToString());
                    string loopingFullScreen = node.Attributes.GetNamedItem("loop").Value.ToString();

                    if (loopingFullScreen.ToLower() == "true" || loopingFullScreen == "1")
                    {
                        while (!myFunctions.CheckFullscreen() && !abort)
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> fullscreenmode: 'on'", GlobalClass.SECTION);
                            foreach (XmlNode childnode in node.ChildNodes)
                            {
                                abort = ExecuteTask(childnode);
                            }
                        }
                        if (!abort)
                            GlobalClass.logger.Info("::" + node.Name + " --> fullscreenmode: 'off'", GlobalClass.SECTION);
                    }
                    else
                    {
                        isOK = myFunctions.CheckFullscreen();
                        if (isOK == false)
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> fullscreenmode: 'on'", GlobalClass.SECTION);
                            foreach (XmlNode childnode in node.ChildNodes)
                            {
                                abort = ExecuteTask(childnode);
                            }
                        }
                        else
                            GlobalClass.logger.Info("::" + node.Name + " --> fullscreenmode: 'off'", GlobalClass.SECTION);
                    }
                    break;

                case "checkmsiinstallation":
                    SetLabel(node.Name, "Check MSI Installation", "'" + node.Attributes.GetNamedItem("package").Value.ToString() + "'");
                    string GUID = myFunctions.GetInstalledGUID(node.Attributes.GetNamedItem("package").Value.ToString());
                    foreach (XmlNode checkmsiChildnode in node.ChildNodes)
                    {
                        switch (checkmsiChildnode.Name)
                        {
                            case "exists":
                                if (GUID != "")
                                {
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + GUID + "' installed...", GlobalClass.SECTION);
                                    foreach (XmlNode msiexistsnode in checkmsiChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(msiexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("package").Value.ToString() + "' NOT installed.", GlobalClass.SECTION);
                                break;

                            case "notexists":
                                if (GUID == "")
                                {
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("package").Value.ToString() + "' NOT installed.", GlobalClass.SECTION);
                                    foreach (XmlNode msinotexistsnode in checkmsiChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(msinotexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("package").Value.ToString() + "' installed.", GlobalClass.SECTION);
                                break;
                        }
                    }
                    break;

                case "checkfolder":
                    string folderpath = node.Attributes.GetNamedItem("path").Value.ToString();
                    string contentrequired = node.Attributes.GetNamedItem("contentrequired").Value.ToString().ToUpper();

                    SetLabel(node.Name, "Check existing folder", "'" + folderpath + "'; folder content required: '" + contentrequired + "'");
                    bool isFolderExisting = myFunctions.CheckFolder(folderpath, contentrequired, GlobalClass.VarTable, GlobalClass.ParameterTable);
                    foreach (XmlNode checkfolderChildnode in node.ChildNodes)
                    {
                        switch (checkfolderChildnode.Name)
                        {
                            case "exists":
                                //logger.Info("check type: 'EXISTS'");
                                if (isFolderExisting)
                                {
                                    if (contentrequired.IndexOf("TRUE") >= 0)
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists (and is NOT empty)!", GlobalClass.SECTION);
                                    else
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                    foreach (XmlNode fileexistsnode in checkfolderChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(fileexistsnode);
                                    }
                                }
                                else
                                    if (contentrequired.IndexOf("TRUE") >= 0)
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists OR is empty!", GlobalClass.SECTION);
                                else
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists!", GlobalClass.SECTION);
                                break;
                            case "notexists":
                                //logger.Info("check type: 'NOTEXISTS'");
                                if (isFolderExisting == false)
                                {
                                    if (contentrequired.IndexOf("TRUE") >= 0)
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exist OR is empty!", GlobalClass.SECTION);
                                    else
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exist!", GlobalClass.SECTION);
                                    foreach (XmlNode filenotexistsnode in checkfolderChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(filenotexistsnode);
                                    }
                                }
                                else
                                    if (contentrequired.IndexOf("TRUE") >= 0)
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists and is NOT empty!", GlobalClass.SECTION);
                                else
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                break;
                        }
                    }
                    break;

                case "checkfile":
                    SetLabel(node.Name, "Check existing file", "'" + node.Attributes.GetNamedItem("path").Value.ToString() + "'");
                    bool isFileExisting = myFunctions.CheckFile(node.Attributes.GetNamedItem("path").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable);
                    foreach (XmlNode checkfileChildnode in node.ChildNodes)
                    {
                        switch (checkfileChildnode.Name)
                        {
                            case "exists":
                                //logger.Info("check type: 'EXISTS'");
                                bool loop_cf_e = false;
                                try
                                {
                                    if (checkfileChildnode.Attributes.GetNamedItem("loop").Value.ToString().ToLower() == "true")
                                    {
                                        loop_cf_e = true;
                                    }
                                }
                                catch
                                {
                                    loop_cf_e = false;
                                }
                                if (!loop_cf_e)
                                {
                                    //do subtasks only once
                                    if (isFileExisting)
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                        foreach (XmlNode fileexistsnode in checkfileChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(fileexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists!", GlobalClass.SECTION);
                                    }
                                }
                                else
                                {
                                    //do subtasks until criteria is fulfilled
                                    int loopcount_cf_e = 0;
                                    while (isFileExisting)
                                    {
                                        loopcount_cf_e++;
                                        GlobalClass.logger.Info("::[loop " + loopcount_cf_e.ToString() + "]" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' still exists!", GlobalClass.SECTION);
                                        foreach (XmlNode fileexistsnode in checkfileChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(fileexistsnode);
                                        }
                                        isFileExisting = myFunctions.CheckFile(node.Attributes.GetNamedItem("path").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable);
                                    }
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists (anymore)!", GlobalClass.SECTION);
                                }
                                break;
                            case "notexists":
                                //logger.Info("check type: 'NOTEXISTS'");
                                bool loop_cf_ne = false;
                                try
                                {
                                    if (checkfileChildnode.Attributes.GetNamedItem("loop").Value.ToString().ToLower() == "true")
                                    {
                                        loop_cf_ne = true;
                                    }
                                }
                                catch
                                {
                                    loop_cf_ne = false;
                                }
                                if (!loop_cf_ne)
                                {
                                    //do subtasks only once                                        
                                    if (isFileExisting == false)
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exist!", GlobalClass.SECTION);
                                        foreach (XmlNode filenotexistsnode in checkfileChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(filenotexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                    }
                                }
                                else
                                {
                                    //do subtasks until criteria is fulfilled
                                    int loopcount_cf_ne = 0;
                                    while (!isFileExisting)
                                    {
                                        loopcount_cf_ne++;
                                        GlobalClass.logger.Info("::[loop " + loopcount_cf_ne.ToString() + "]" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists yet!", GlobalClass.SECTION);
                                        foreach (XmlNode fileexistsnode in checkfileChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(fileexistsnode);
                                        }
                                        isFileExisting = myFunctions.CheckFile(node.Attributes.GetNamedItem("path").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable);
                                    }
                                    GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists (now)!", GlobalClass.SECTION);
                                }
                                break;
                        }
                    }
                    break;

                case "checkregistry":
                    string optionalvalue = "";
                    string optionalname1 = "";
                    try
                    {
                        optionalvalue = node.Attributes.GetNamedItem("value").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (value was not defined and so, it's not relevant in that case)
                    }
                    try
                    {
                        optionalname1 = node.Attributes.GetNamedItem("name").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (value was not defined and so, it's not relevant in that case)
                    }

                    SetLabel(node.Name, "Check registry key", "key: '" + node.Attributes.GetNamedItem("path").Value.ToString() + "'; name: '" + optionalname1 + "'; value: '" + optionalvalue + "'");
                    bool isRegEntryExisting = myFunctions.CheckRegistry(node.Attributes.GetNamedItem("path").Value.ToString(), optionalname1, optionalvalue);
                    foreach (XmlNode checkregChildnode in node.ChildNodes)
                    {
                        switch (checkregChildnode.Name)
                        {
                            case "exists":
                                //logger.Info("check type: 'EXISTS'");
                                bool loop_e = false;
                                try
                                {
                                    if (checkregChildnode.Attributes.GetNamedItem("loop").Value.ToString().ToLower() == "true")
                                    {
                                        loop_e = true;
                                    }
                                }
                                catch
                                {
                                    loop_e = false;
                                }
                                if (!loop_e)
                                {
                                    //do subtasks once
                                    if (isRegEntryExisting)
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> registry key exists", GlobalClass.SECTION);
                                        foreach (XmlNode regexistsnode in checkregChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(regexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> registry key does NOT exist!", GlobalClass.SECTION);
                                    }
                                }
                                else
                                {
                                    //do subtask until criteria is fulfilled
                                    int loopcount_e = 0;
                                    while (isRegEntryExisting)
                                    {
                                        loopcount_e++;
                                        GlobalClass.logger.Info("::[loop " + loopcount_e.ToString() + "] " + node.Name + " --> registry key still exists", GlobalClass.SECTION);
                                        foreach (XmlNode regexistsnode in checkregChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(regexistsnode);
                                        }
                                        isRegEntryExisting = myFunctions.CheckRegistry(node.Attributes.GetNamedItem("path").Value.ToString(), optionalname1, optionalvalue);
                                    }
                                    GlobalClass.logger.Info("::" + node.Name + " --> registry key does NOT exist (anymore)!", GlobalClass.SECTION);
                                }
                                break;
                            case "notexists":
                                //logger.Info("check type: 'NOTEXISTS'");
                                bool loop_ne = false;
                                try
                                {
                                    if (checkregChildnode.Attributes.GetNamedItem("loop").Value.ToString().ToLower() == "true")
                                    {
                                        loop_ne = true;
                                    }
                                }
                                catch
                                {
                                    loop_ne = false;
                                }
                                if (!loop_ne)
                                {
                                    //do subtasks once
                                    if (isRegEntryExisting == false)
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> registry key does NOT exist!", GlobalClass.SECTION);
                                        foreach (XmlNode regnotexistsnode in checkregChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(regnotexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> registry key exists", GlobalClass.SECTION);
                                    }
                                }
                                else
                                {
                                    //do subtask until criteria is fulfilled
                                    int loopcount_ne = 0;
                                    while (!isRegEntryExisting)
                                    {
                                        loopcount_ne++;
                                        GlobalClass.logger.Info("::[loop " + loopcount_ne.ToString() + "] " + node.Name + " --> registry key still NOT exists", GlobalClass.SECTION);
                                        foreach (XmlNode regexistsnode in checkregChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(regexistsnode);
                                        }
                                        isRegEntryExisting = myFunctions.CheckRegistry(node.Attributes.GetNamedItem("path").Value.ToString(), optionalname1, optionalvalue);
                                    }
                                    GlobalClass.logger.Info("::" + node.Name + " --> registry key does exist (now)!", GlobalClass.SECTION);
                                }

                                break;
                        }
                    }
                    break;

                case "checkarchitecture":
                    SetLabel(node.Name, "Check architecture (x86,x64)", "");
                    bool isX64 = myFunctions.CheckArchitecture();
                    foreach (XmlNode checkx64Childnode in node.ChildNodes)
                    {
                        switch (checkx64Childnode.Name)
                        {
                            case "x64":
                                //logger.Info("::" + node.Name + " --> 'x64'");
                                if (isX64)
                                {
                                    GlobalClass.logger.Info("::" + node.Name + " --> x64 detected", GlobalClass.SECTION);
                                    foreach (XmlNode x64existsnode in checkx64Childnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(x64existsnode);
                                    }
                                }
                                else
                                {
                                    //logger.Info("x86 detected");
                                }
                                break;
                            case "x86":
                                //logger.Info("check type: 'x86'");
                                if (isX64 == false)
                                {
                                    GlobalClass.logger.Info("::" + node.Name + " --> x86 detected", GlobalClass.SECTION);
                                    foreach (XmlNode x64notexistsnode in checkx64Childnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(x64notexistsnode);
                                    }
                                }
                                else
                                {
                                    //logger.Info("::" + node.Name + " --> x64 detected");
                                }
                                break;
                        }
                    }
                    break;

                case "checkOS":
                    string name = "";
                    string version = "";
                    string servicepack = "";
                    string languageid = "";
                    try
                    {
                        name = node.Attributes.GetNamedItem("name").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (value was not defined and so, it's not relevant in that case)
                    }
                    try
                    {
                        version = node.Attributes.GetNamedItem("version").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (value was not defined and so, it's not relevant in that case)
                    }
                    try
                    {
                        servicepack = node.Attributes.GetNamedItem("servicepack").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (value was not defined and so, it's not relevant in that case)
                    }
                    try
                    {
                        languageid = node.Attributes.GetNamedItem("language").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do (value was not defined and so, it's not relevant in that case)
                    }

                    SetLabel(node.Name, "Check OS (Name, Version, Service Pack, Language)", "Name: '" + name + "'; Version: '" + version + "'; Service Pack: '" + servicepack + "'; Language: '" + languageid + "'");
                    bool isOSExisting = myFunctions.CheckOS(name, version, servicepack, languageid);
                    foreach (XmlNode checkOSChildnode in node.ChildNodes)
                    {
                        switch (checkOSChildnode.Name)
                        {
                            case "exists":
                                if (isOSExisting)
                                {
                                    GlobalClass.logger.Info("::" + node.Name + " --> OS exists", GlobalClass.SECTION);
                                    foreach (XmlNode regexistsnode in checkOSChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(regexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info("::" + node.Name + " --> OS does NOT exist!", GlobalClass.SECTION);
                                break;
                            case "notexists":
                                if (isOSExisting == false)
                                {
                                    GlobalClass.logger.Info("::" + node.Name + " --> OS does NOT exist!", GlobalClass.SECTION);
                                    foreach (XmlNode regnotexistsnode in checkOSChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(regnotexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info("::" + node.Name + " --> OS exists", GlobalClass.SECTION);
                                break;
                        }
                    }
                    break;

                case "addregkey":
                    string optionalregkeyvalue = "";
                    try
                    {
                        optionalregkeyvalue = node.Attributes.GetNamedItem("value").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do ('name' was not defined and so, it's not relevant in that case)
                    }
                    SetLabel(node.Name, "Add new registry key", "key: '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' name: '" + node.Attributes.GetNamedItem("name").Value.ToString() + "' value: '" + optionalregkeyvalue + "'");
                    returnvalue = myFunctions.AddRegKey(node.Attributes.GetNamedItem("path").Value.ToString(), node.Attributes.GetNamedItem("name").Value.ToString(), optionalregkeyvalue, node.Attributes.GetNamedItem("type").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable, ref errormsg);
                    if (returnvalue > 0)
                    {
                        GlobalClass.logger.Warn("::" + node.Name + " --> returnvalue: " + returnvalue + "; '" + errormsg + "'", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                    }
                    else
                    {
                        GlobalClass.logger.Info("::" + node.Name + " --> returnvalue: " + returnvalue, GlobalClass.SECTION);
                    }
                    break;

                case "removeregkey":
                    {
                        string optionalname = "";
                        try
                        {
                            optionalname = node.Attributes.GetNamedItem("name").Value.ToString();
                        }
                        catch (Exception)
                        {
                            //nothing to do ('name' was not defined and so, it's not relevant in that case)
                        }
                        SetLabel(node.Name, "Remove existing registry key", "key: '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' name: '" + optionalname + "'");
                        myFunctions.RemoveRegKey(node.Attributes.GetNamedItem("path").Value.ToString(), optionalname);
                    }
                    break;

                case "removeregpath":
                    {
                        string optionalname = "";
                        try
                        {
                            optionalname = node.Attributes.GetNamedItem("name").Value.ToString();
                        }
                        catch (Exception)
                        {
                            //nothing to do ('name' was not defined and so, it's not relevant in that case)
                        }
                        SetLabel(node.Name, "Remove existing registry path", "key: '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' name: '" + optionalname + "'");
                        myFunctions.RemoveRegPath(node.Attributes.GetNamedItem("path").Value.ToString(), optionalname);
                    }
                    break;

                case "checkprocess":
                    SetLabel(node.Name, "Check running process", "'LOOP' attribute:" + node.Attributes.GetNamedItem("loop").Value.ToString());
                    string loopingCheckProcess = node.Attributes.GetNamedItem("loop").Value.ToString();
                    GlobalClass.RunningProcess = node.Attributes.GetNamedItem("name").Value.ToString();

                    if (loopingCheckProcess.ToLower() == "true" || loopingCheckProcess == "1")
                    {
                        SetLabel(node.Name, "Check running process", "'" + node.Attributes.GetNamedItem("name").Value.ToString() + "'");
                        while (myFunctions.CheckProcess(node.Attributes.GetNamedItem("name").Value.ToString()) && !abort)
                        {
                            foreach (XmlNode childnode in node.ChildNodes)
                            {
                                abort = ExecuteTask(childnode);
                            }
                        }
                    }
                    else
                    {
                        SetLabel(node.Name, "Check running process", "'" + node.Attributes.GetNamedItem("name").Value.ToString() + "'");
                        if (myFunctions.CheckProcess(node.Attributes.GetNamedItem("name").Value.ToString()))
                        {
                            foreach (XmlNode childnode in node.ChildNodes)
                            {
                                abort = ExecuteTask(childnode);
                            }
                        }
                    }
                    GlobalClass.RunningProcess = "";
                    break;

                case "killprocess":
                    SetLabel(node.Name, "Kill running process", "'" + node.Attributes.GetNamedItem("name").Value.ToString() + "'");
                    myFunctions.KillProcess(node.Attributes.GetNamedItem("name").Value.ToString());
                    break;

                case "killprocessbypath":
                    SetLabel(node.Name, "Kill running processes by path", "'" + node.Attributes.GetNamedItem("path").Value.ToString() + "'");
                    myFunctions.KillProcessByPath(node.Attributes.GetNamedItem("path").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable);
                    break;

                case "messagebox":
                    //if (isUserLoggedOn)
                    //{
                    //    if (isFullySilent)
                    //    {
                    //        WriteTalkMessage("requesttime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    //        WriteTalkMessage("requestcommand", "messagebox");
                    //        WriteTalkMessage("requestarguments", @"""" + node.InnerText.ToString() + @"""");
                    //        WaitForTalkResponse();

                    //    }
                    //    else
                    //    {
                    //        SetLabel(node.Name, "Information", "type: '" + node.Attributes.GetNamedItem("type").Value.ToString() + "' message: '" + node.InnerText.ToString() + "'");
                    //        ShowMainGUI(node.InnerText.ToString(), node.Attributes.GetNamedItem("type").Value.ToString(), false, false, false, 0);
                    //    }
                    //}
                    //else
                    {
                        GlobalClass.logger.Info("::" + node.Name + " --> task '" + node.Name + "' skipped; nobody is logged on", GlobalClass.SECTION);
                    }
                    break;

                case "postpone":
                    //if (isUserLoggedOn)
                    //{
                    //    string max = "10";              //default
                    //    string optionalppmsg = "";        //default
                    //    string optionalppdialog = "true"; //default
                    //    string optionalpptimeout = "0";            //default
                    //    string current = "0";           //registry
                    //    int remaining = 0;

                    //    //max value from XML (since Schema version 1.2)
                    //    max = node.Attributes.GetNamedItem("max").Value.ToString();

                    //    //optional attributes 'dialog' and 'message' from XML (since Schema version 1.3.2)
                    //    try
                    //    {
                    //        optionalppmsg = node.Attributes.GetNamedItem("message").Value.ToString();
                    //    }
                    //    catch (Exception)
                    //    {
                    //        //nothing to do ('message' was not defined and so, it's not relevant in that case)
                    //    }
                    //    try
                    //    {
                    //        optionalppdialog = node.Attributes.GetNamedItem("dialog").Value.ToString();
                    //    }
                    //    catch (Exception)
                    //    {
                    //        //nothing to do ('message' was not defined and so, it's not relevant in that case)
                    //    }

                    //    //enable/disable Postpone dialog
                    //    if (optionalppdialog.ToLower() == "false")
                    //    {
                    //        GlobalClass.ShowPostponeDialog = false;
                    //    }
                    //    else
                    //    {
                    //        GlobalClass.ShowPostponeDialog = true;
                    //    }

                    //    try
                    //    {
                    //        optionalpptimeout = node.Attributes.GetNamedItem("timeout").Value.ToString();
                    //    }
                    //    catch { }

                    //    int liMyTimeout = 0;
                    //    try
                    //    {
                    //        liMyTimeout = Convert.ToInt32(optionalpptimeout);
                    //    }
                    //    catch { }

                    //    //current value from Registry
                    //    RegistryKey regKey = null;
                    //    string guidkey = GlobalClass.ProductCode;
                    //    try
                    //    {
                    //        regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\" + Application.CompanyName + "\\" + Application.ProductName + "\\" + guidkey);
                    //        if (regKey != null)
                    //        {
                    //            //try
                    //            //{
                    //            current = regKey.GetValue("PP").ToString();
                    //            if (current == "")
                    //            {
                    //                current = "0";
                    //            }
                    //            //}
                    //            //catch (Exception)
                    //            //{
                    //            //    current = "0";
                    //            //}
                    //        }
                    //        regKey.Close();
                    //    }
                    //    catch (Exception)
                    //    {
                    //        current = "0";
                    //    }

                    //    //max Postpones reached?
                    //    if (Convert.ToInt32(current) >= Convert.ToInt32(max))
                    //    {
                    //        //postpone not possible anymore - skip
                    //        GlobalClass.logger.Warn("::" + node.Name + " --> task ' " + node.Name + "' skipped; max # postpone (" + max + ")", GlobalClass.SECTION);
                    //    }
                    //    else
                    //    {
                    //        //postpone dialog
                    //        SetLabel(node.Name, "Start " + typelabel, "Postpone Dialog /(main) GUI enabled");
                    //        GlobalClass.logger.Info("::" + node.Name + " --> #: " + current + "/" + max, GlobalClass.SECTION);
                    //        if (optionalppmsg.Length > 0)
                    //        {
                    //            optionalppmsg = optionalppmsg.Replace("[max]", max.ToString());
                    //            optionalppmsg = optionalppmsg.Replace("[current]", current.ToString());
                    //            remaining = Convert.ToInt32(max) - Convert.ToInt32(current);
                    //            optionalppmsg = optionalppmsg.Replace("[remaining]", remaining.ToString());
                    //            if (isFullySilent)
                    //            {
                    //                WriteTalkMessage("requesttime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    //                WriteTalkMessage("requestcommand", "postpone");
                    //                WriteTalkMessage("requestarguments", @"""" + optionalppmsg + @"""|""" + current.ToString() + "/" + max.ToString() + @"|" + liMyTimeout + @"""");
                    //                WaitForTalkResponse();
                    //            }
                    //            else
                    //            {
                    //                ShowMainGUI(optionalppmsg, "POSTPONE", false, false, false, 0);
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (isFullySilent)
                    //            {
                    //                WriteTalkMessage("requesttime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    //                WriteTalkMessage("requestcommand", "postpone");
                    //                WriteTalkMessage("requestarguments", @"""" + node.InnerText.ToString() + @"""|""" + current.ToString() + "/" + max.ToString() + @"|" + liMyTimeout + @"""");
                    //                WaitForTalkResponse();
                    //            }
                    //            else
                    //            {
                    //                string lsText = "Press 'Start' to proceed the " + typelabel + "  --  Press 'Postpone' to open the Postpone dialog (" + current + "/" + max + ")";
                    //                if (liMyTimeout > 0)
                    //                {
                    //                    lsText = "Press 'Start' to proceed the " + typelabel + ", the installation will automatically continue in " + liMyTimeout.ToString() + " seconds.";
                    //                }

                    //                ShowMainGUI(lsText, "POSTPONE", false, false, false, liMyTimeout);
                    //            }
                    //        }
                    //    }
                    //}
                    //else
                    {
                        GlobalClass.logger.Info("::" + node.Name + " --> task '" + node.Name + "' skipped; nobody is logged on", GlobalClass.SECTION);
                    }
                    break;

                case "enableinstallgui":

                    //if (GlobalClass.IsMainGUIEnabled)
                    //{
                    //    GlobalClass.logger.Info("::" + node.Name + " --> Enabling Main GUI - already enabled!", GlobalClass.SECTION);
                    //}
                    //else
                    //{
                    //    if (isUserLoggedOn)
                    //    {
                    //        SetLabel(node.Name, "Start " + typelabel, "Main GUI enabled");

                    //        if (node.Attributes.GetNamedItem("unattended").Value.ToString().ToLower() == "true")
                    //        {
                    //            GlobalClass.IsUnattended = true;
                    //        }
                    //        if (!isFullySilent)
                    //        {
                    //            ShowMainGUI("", "", false, false, false, 0);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        GlobalClass.logger.Info("::" + node.Name + " --> task '" + node.Name + "' skipped; nobody is logged on", GlobalClass.SECTION);
                    //    }
                    //}
                    break;

                case "stopservice":
                case "startservice":
                    string lblText = "";
                    int startOrStop = STARTSERVICE;
                    if (node.Name == "startservice")
                    {
                        lblText = "Start local service";
                        startOrStop = STARTSERVICE;
                    }
                    else
                    {
                        lblText = "Stop running service";
                        startOrStop = STOPSERVICE;
                    }
                    SetLabel(node.Name, lblText, "'" + node.Attributes.GetNamedItem("name").Value.ToString() + "'");
                    returnvalue = myFunctions.ServiceMgr(node.Attributes.GetNamedItem("name").Value.ToString(), startOrStop, ref errormsg);
                    if (returnvalue > 0)
                    {
                        GlobalClass.logger.Warn("::" + node.Name + " --> returnvalue: " + returnvalue + "; '" + errormsg + "'", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                    }
                    else
                    {
                        GlobalClass.logger.Info("::" + node.Name + " --> returnvalue: " + returnvalue, GlobalClass.SECTION);
                    }
                    break;

                case "removefile":
                case "removefolder":
                    SetLabel(node.Name, "Remove existing file or folder", "'" + node.Attributes.GetNamedItem("path").Value.ToString() + "'");
                    returnvalue = myFunctions.RemoveFileOrFolder(node.Attributes.GetNamedItem("path").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable);
                    GlobalClass.logger.Info("::" + node.Name + " --> returnvalue: " + returnvalue, GlobalClass.SECTION);
                    break;

                case "appinstaller":
                    SetLabel(node.Name, "Running appinstaller for ", "'" + node.Attributes.GetNamedItem("action").Value.ToString() + "'");
                    string appinstalleraction = "";
                    string appinstallername = "";
                    string appinstallerfilename = "";
                    try
                    {
                        appinstalleraction = node.Attributes.GetNamedItem("action").Value.ToString();
                    }
                    catch { }
                    try
                    {
                        appinstallername = node.Attributes.GetNamedItem("name").Value.ToString();
                    }
                    catch { }
                    try
                    {
                        appinstallerfilename = node.Attributes.GetNamedItem("filename").Value.ToString();
                    }
                    catch { }
                    returnvalue = myFunctions.AppInstaller(appinstalleraction, appinstallername, appinstallerfilename);
                    GlobalClass.logger.Info("::" + node.Name + " --> returnvalue: " + returnvalue, GlobalClass.SECTION);
                    break;


                case "msiexec":
                case "execute":
                case "executenowait":
                    string cmdline = "";
                    string parameter = "";
                    string parameters = "";
                    string cmdreturnvalue = "";
                    string wait = "true"; //default
                    string windowstyle = "normal"; //default

                    //cmd to execute
                    if (node.Name == "msiexec")
                    {
                        cmdline = "msiexec";
                    }
                    else
                    {
                        //relative or absolute path the the executable
                        cmdline = node.Attributes.GetNamedItem("path").Value.ToString();
                        if (node.Name == "executenowait")
                        {
                            wait = "false";
                        }

                        //WindowStyle [hidden|maximized|minimized|normal(default)]
                        try
                        {
                            windowstyle = node.Attributes.GetNamedItem("windowstyle").Value.ToString();
                        }
                        catch
                        {
                            //do nothing
                        }

                    }
                    //parameters
                    foreach (XmlNode childnode in node.ChildNodes)
                    {
                        if (childnode.Name == "parameters")
                        {
                            foreach (XmlNode parameterchildnode in childnode.ChildNodes)
                            {
                                if (parameterchildnode.InnerXml.ToString() != "")
                                {
                                    parameter = parameterchildnode.InnerText.ToString();
                                    myFunctions.ReplaceEnvVariables(ref parameter, GlobalClass.VarTable, GlobalClass.ParameterTable);
                                    parameters = parameters + parameter + " ";
                                }
                            }
                        }
                    }
                    parameters = parameters.Trim();
                    SetLabel(node.Name, "Executing '" + cmdline + "'", "parameters: '" + parameters + "' / wait: '" + wait + "'");
                    cmdreturnvalue = myFunctions.ExecuteCMD(cmdline, parameters, ref errormsg, wait, windowstyle, GlobalClass.VarTable, GlobalClass.ParameterTable);

                    //1618 in case of "msiexec" --> wait a few seconds and try again
                    //1618: "Another installation is already in progress"
                    if (node.Name == "msiexec" && cmdreturnvalue == "1618")
                    {
                        GlobalClass.logger.Info("::" + node.Name + " --> returnvalue: " + cmdreturnvalue, GlobalClass.SECTION);
                        int attempts = 0;
                        int maxattempts = 5;

                        while (attempts < maxattempts && cmdreturnvalue == "1618")
                        {
                            Int32 l_millisecondsToSleep = 60000;
                            attempts++;
                            //sleep
                            SetLabel(node.Name, "Another installation is already in progress --> stand by (" + attempts.ToString() + "/" + maxattempts.ToString() + ")...", (l_millisecondsToSleep / 1000).ToString() + " seconds");

                            bool l_bSleepStop = false;
                            int l_intermittentSleepIncrement = 10;
                            int l_sleptTime = 0;
                            //wake up every 10 miliseconds to check if we need to stop
                            while (!l_bSleepStop && l_sleptTime < l_millisecondsToSleep)
                            {
                                l_sleptTime += l_intermittentSleepIncrement;
                                try
                                {
                                    Thread.Sleep(l_intermittentSleepIncrement);
                                }
                                catch
                                {
                                    //do nothing
                                }
                            }

                            //msiexec again
                            parameters = parameters.Trim();
                            SetLabel(node.Name, "Executing (" + (attempts + 1).ToString() + ") '" + cmdline + "'", "parameters: '" + parameters + "' / wait: '" + wait + "'");
                            cmdreturnvalue = myFunctions.ExecuteCMD(cmdline, parameters, ref errormsg, wait, windowstyle, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        }
                    }


                    //handle return values (only execute & msiexec)
                    if (node.Name == "execute" || node.Name == "msiexec")
                    {
                        if (errormsg == "")
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> returnvalue: " + cmdreturnvalue, GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> returnvalue: " + cmdreturnvalue + "; '" + errormsg + "'", GlobalClass.SECTION);
                        }

                        try
                        {
                            foreach (XmlNode childnode2 in node.ChildNodes)
                            {

                                try
                                {
                                    if (childnode2.Name == "returnvalues")
                                    {

                                        bool errorCodeExistsInXML = false;
                                        try
                                        {
                                            foreach (XmlNode retvaluechildnode in childnode2.ChildNodes)
                                            {

                                                if (cmdreturnvalue == retvaluechildnode.Attributes.GetNamedItem("value").Value.ToString())
                                                {
                                                    errorCodeExistsInXML = true;
                                                    foreach (XmlNode childnode4 in retvaluechildnode.ChildNodes)
                                                    {
                                                        abort = ExecuteTask(childnode4);
                                                    }
                                                }

                                                //general Error code? (Not 0)
                                                if (!cmdreturnvalue.Equals("0"))
                                                {
                                                    if (!errorCodeExistsInXML)
                                                    {
                                                        if (retvaluechildnode.Attributes.GetNamedItem("value").Value.ToString() == "!0")
                                                        {
                                                            foreach (XmlNode childnode3 in retvaluechildnode.ChildNodes)
                                                            {
                                                                abort = ExecuteTask(childnode3);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex0) { GlobalClass.logger.Error("###0" + ex0.InnerException + " " + ex0.InnerException, GlobalClass.SECTION); }
                                    }
                                }
                                catch (Exception ex1) { GlobalClass.logger.Error("###1" + ex1.InnerException, GlobalClass.SECTION); }
                            }
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show(ex.Message, TASKNAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            GlobalClass.logger.Error("###" + ex.ToString() + " - " + ex.InnerException, GlobalClass.SECTION);
                            GlobalClass.logger.WritetoEventLog("###" + ex.ToString() + " - " + ex.InnerException, GlobalClass.LERROR, GlobalClass.SECTION);
                        }

                    }

                    //update sourcelist
                    //if (node.Name == "msiexec")
                    //{
                    //    string curTaskID = "";
                    //    string packageSources = "";
                    //    isOK = false;
                    //    try
                    //    {
                    //        curTaskID = myFunctions.GetRunningAltirisTask();
                    //        if (curTaskID != "")
                    //        {
                    //            packageSources = myFunctions.GetAltirisPackageSources(curTaskID);
                    //            if (packageSources != "")
                    //            {
                    //                string[] sourceArray = packageSources.Split(';');
                    //                foreach (string s in sourceArray)
                    //                {
                    //                    if (s != "")
                    //                    {
                    //                        isOK = myFunctions.AddSourceToSourceList(GlobalClass.ProductCode, s, GlobalClass.VarTable);
                    //                        if (isOK)
                    //                        {
                    //                            GlobalClass.logger.Info("::" + node.Name + " --> Path added to Sourcelist: '" + s + "'", GlobalClass.SECTION);
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //    catch (Exception)
                    //    {
                    //        //do nothing!
                    //    }
                    //}
                    break;

                case "setreboot":
                    SetLabel(node.Name, "Set reboot flag", "");
                    GlobalClass.IsRebootEnabled = true;
                    break;

                case "continue":
                    int exitcode = 0;
                    try
                    {
                        exitcode = Convert.ToInt32(node.Attributes.GetNamedItem("exitcode").Value.ToString());
                        GlobalClass.ExitCode = exitcode;
                    }
                    catch (System.NullReferenceException)
                    {
                    }
                    break;
                case "abort":
                    string msg = "";
                    exitcode = GlobalClass.EC_ABORT; //default
                    abort = true;
                    GlobalClass.IsAborted = abort;
                    try
                    {
                        msg = node.Attributes.GetNamedItem("message").Value.ToString();
                    }
                    catch (System.NullReferenceException)
                    {
                        //noting to do, attribute 'message' was not defined.
                    }
                    try
                    {
                        exitcode = Convert.ToInt32(node.Attributes.GetNamedItem("exitcode").Value.ToString());
                    }
                    catch (System.NullReferenceException)
                    {
                        //nothing to do, attribute 'exitcode' was not defined.
                    }
                    GlobalClass.ExitCode = exitcode;
                    if (msg != "")
                    {
                        //if (isUserLoggedOn)
                        //{
                        //    if (isFullySilent)
                        //    {
                        //        WriteTalkMessage("requesttime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                        //        WriteTalkMessage("requestcommand", "abort");
                        //        WriteTalkMessage("requestarguments", @"""" + node.InnerText.ToString() + @"""");
                        //        WaitForTalkResponse();
                        //    }
                        //    else
                        //    {
                        //        SetLabel(node.Name, "Aborting", "defined user message: '" + msg + "'");
                        //        GlobalClass.IsUnattended = false;
                        //        ShowMainGUI(msg, "ABORT", false, false, false, 0);
                        //    }
                        //}
                        //else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> Aborting (without User Message - nobody is logged on); '" + msg + "'", GlobalClass.SECTION);
                        }
                    }
                    else
                        GlobalClass.logger.Warn("::" + node.Name + " --> Aborting (without User Message)", GlobalClass.SECTION);
                    break;

                case "sleep":
                    string seconds = "";
                    Int32 millisecondsToSleep = 1;
                    string optionalmsg = "";
                    try
                    {
                        seconds = node.Attributes.GetNamedItem("seconds").Value.ToString();
                        optionalmsg = node.Attributes.GetNamedItem("message").Value.ToString();
                    }
                    catch
                    {
                        //do nothing
                    }

                    if (optionalmsg == "")
                    {
                        optionalmsg = "working (stand by...)";
                    }
                    SetLabel(node.Name, optionalmsg, node.Attributes.GetNamedItem("seconds").Value.ToString() + " seconds");
                    millisecondsToSleep = Convert.ToInt32(node.Attributes.GetNamedItem("seconds").Value.ToString()) * 1000;


                    GlobalClass.logger.Info("::" + node.Name + " --> sleeping: for " + millisecondsToSleep, GlobalClass.SECTION);

                    //Thread.Sleep(millisecondsToSleep);

                    DateTime loStartSleepTime = DateTime.Now;
                    DateTime loEndSleepTime = loStartSleepTime.AddMilliseconds(millisecondsToSleep);

                    while (DateTime.Now < loEndSleepTime)
                    {

                        try
                        {
                            Thread.Sleep(500);
                        }
                        catch
                        {
                            //do nothing
                        }
                    }
                    //bool bSleepStop = false;
                    //        int intermittentSleepIncrement = 10;
                    //        int sleptTime = 0;
                    //        //wake up every 10 miliseconds to check if we need to stop
                    //        while (!bSleepStop && sleptTime < millisecondsToSleep)
                    //        {
                    //            sleptTime += intermittentSleepIncrement;
                    //            try
                    //            {
                    //                Thread.Sleep(500);
                    //            }
                    //            catch
                    //            {
                    //                //do nothing
                    //            }
                    //        }
                    break;

                case "startaltiristask":
                    //CreateScriptFile(startAltirisTaskScriptPath, STARTALTIRISTASKSCRIPT);
                    //string altiristaskname = node.Attributes.GetNamedItem("name").Value.ToString();
                    //SetLabel(node.Name, "Start Altiris task: '" + altiristaskname + "'", "'" + altiristaskname + "'");
                    //if (myFunctions.StartAltirisTask(altiristaskname, startAltirisTaskScriptPath + STARTALTIRISTASKSCRIPT, GlobalClass.VarTable))
                    //{
                    //    //altiris task found and executed
                    //    GlobalClass.logger.Info("::" + node.Name + " --> Altiris task '" + altiristaskname + "' found and queued.", GlobalClass.SECTION);
                    //}
                    //else
                    //{
                    //    //altiris task not found
                    //    GlobalClass.logger.Warn("::" + node.Name + " --> Altiris task '" + altiristaskname + "' is NOT available.", GlobalClass.SECTION);
                    //    foreach (XmlNode childnode in node.ChildNodes)
                    //    {
                    //        abort = ExecuteTask(childnode);
                    //    }
                    //}
                    break;

                case "setvariable":
                    {
                        {
                            string var = node.Attributes.GetNamedItem("var").Value.ToString();
                            string value = node.Attributes.GetNamedItem("value").Value.ToString();
                            myFunctions.ReplaceEnvVariables(ref value, GlobalClass.VarTable, GlobalClass.ParameterTable);

                            bool lbDoneSomething = false;
                            foreach (DataRow loVarRow in GlobalClass.VarTable.Rows)
                            {
                                if (loVarRow["name"].ToString().ToLower() == var.ToLower())
                                {
                                    loVarRow["value"] = value;

                                    lbDoneSomething = true;

                                    GlobalClass.logger.Info("::" + node.Name + " --> setvariable: updating '" + loVarRow["name"].ToString().ToLower() + "' to '" + value + @"'.", GlobalClass.SECTION);

                                }
                            }

                            if (!lbDoneSomething)
                            {
                                DataRow loNewRow = GlobalClass.VarTable.NewRow();
                                loNewRow["name"] = var;
                                loNewRow["value"] = value;
                                GlobalClass.VarTable.Rows.Add(loNewRow);

                                GlobalClass.logger.Info("::" + node.Name + " --> setvariable: adding '" + var + "' to '" + value + @"'.", GlobalClass.SECTION);
                            }
                            GlobalClass.VarTable.AcceptChanges();
                        }
                    }
                    break;


                case "setsecretvariable":
                    {
                        {

                            string var = node.Attributes.GetNamedItem("var").Value.ToString();
                            string value = node.Attributes.GetNamedItem("value").Value.ToString();

                            bool lbDoneSomething = false;
                            foreach (DataRow loVarRow in GlobalClass.VarTable.Rows)
                            {
                                if (loVarRow["name"].ToString().ToLower() == var.ToLower())
                                {

                                    if (value.StartsWith("[") && value.EndsWith("]"))
                                    {
                                        loVarRow["value"] = StringDecode(value.Substring(1, value.Length-2), GlobalClass.ProductCode);
                                    }
                                    else
                                    {
                                        loVarRow["value"] = value;
                                    }
                                    lbDoneSomething = true;

                                    GlobalClass.logger.Info("::" + node.Name + " --> setvariable: updating '" + loVarRow["name"].ToString().ToLower() + "' to '" + value + @"'.", GlobalClass.SECTION);

                                }
                            }

                            if (!lbDoneSomething)
                            {
                                DataRow loNewRow = GlobalClass.VarTable.NewRow();
                                loNewRow["name"] = var;
                                if (value.StartsWith("[") && value.EndsWith("]"))
                                {
                                    loNewRow["value"] = StringDecode(value.Substring(1, value.Length-2), GlobalClass.ProductCode);
                                }
                                else
                                {
                                    loNewRow["value"] = value;
                                }
                                GlobalClass.VarTable.Rows.Add(loNewRow);

                                GlobalClass.logger.Info("::" + node.Name + " --> setvariable: adding '" + var + "' to '" + value + @"'.", GlobalClass.SECTION);
                            }
                            GlobalClass.VarTable.AcceptChanges();
                        }
                    }
                    break;

                case "getvariablefromregistry":
                    {
                        {
                            string var = node.Attributes.GetNamedItem("var").Value.ToString();
                            string path = node.Attributes.GetNamedItem("path").Value.ToString();
                            string myname = node.Attributes.GetNamedItem("name").Value.ToString();

                            GlobalClass.logger.Info("::" + node.Name + " --> getvariablefromregistry: reading registry " + path + " - " + myname, GlobalClass.SECTION);

                            string myvalue = myFunctions.ReadRegistry(path, myname);

                            bool lbDoneSomething = false;
                            foreach (DataRow loVarRow in GlobalClass.VarTable.Rows)
                            {
                                if (loVarRow["name"].ToString().ToLower() == var.ToLower())
                                {
                                    loVarRow["value"] = myvalue;
                                    lbDoneSomething = true;

                                    GlobalClass.logger.Info("::" + node.Name + " --> getvariablefromregistry: updating '" + loVarRow["name"].ToString().ToLower() + "' to '" + myvalue + @"'.", GlobalClass.SECTION);

                                }
                            }
                            if (!lbDoneSomething)
                            {
                                DataRow loNewRow = GlobalClass.VarTable.NewRow();
                                loNewRow["name"] = var;
                                loNewRow["value"] = myvalue;
                                GlobalClass.VarTable.Rows.Add(loNewRow);

                                GlobalClass.logger.Info("::" + node.Name + " --> getvariablefromregistry: adding '" + var.ToLower() + "' to '" + myvalue + @"'.", GlobalClass.SECTION);
                            }

                            GlobalClass.VarTable.AcceptChanges();
                        }
                    }
                    break;

                case "checkvariable":
                    {
                        string var = node.Attributes.GetNamedItem("var").Value.ToString().ToLower();
                        string value = node.Attributes.GetNamedItem("value").Value.ToString();

                        SetLabel(node.Name, "Check existing variable ", "'" + var + "'; value: '" + value + "'");

                        foreach (DataRow loRow in GlobalClass.VarTable.Rows)
                        {
                            GlobalClass.logger.Info("Check existing variable, content now: " + loRow["name"].ToString() + "=" + loRow["value"].ToString(), GlobalClass.SECTION);
                        }

                        bool isVariableExisting = myFunctions.CheckVariable(var, value, GlobalClass.VarTable);
                        GlobalClass.logger.Info("Check existing variable existing: " + isVariableExisting.ToString(), GlobalClass.SECTION);

                        foreach (XmlNode checkfolderChildnode in node.ChildNodes)
                        {
                            switch (checkfolderChildnode.Name)
                            {
                                case "exists":
                                    if (isVariableExisting == true)
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                        foreach (XmlNode fileexistsnode in checkfolderChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(fileexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' does NOT exists!", GlobalClass.SECTION);
                                    }
                                    break;
                                case "notexists":
                                    if (isVariableExisting == false)
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' does NOT exist!", GlobalClass.SECTION);
                                        foreach (XmlNode filenotexistsnode in checkfolderChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(filenotexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info("::" + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                    }
                                    break;
                            }
                        }
                    }
                    break;

                case "copyfile":
                    {
                        string source = node.Attributes.GetNamedItem("source").Value.ToString();
                        string destination = node.Attributes.GetNamedItem("destination").Value.ToString();
                        string overwrite = "true"; //default
                        try
                        {
                            overwrite = node.Attributes.GetNamedItem("overwrite").Value.ToString().ToLower();
                        }
                        catch
                        {
                            //do nothing
                        }
                        SetLabel(node.Name, "Copy file: '" + source + "' TO '" + destination + "' (overwrite:" + overwrite + ")", "");
                        string source1 = source;
                        myFunctions.ReplaceEnvVariables(ref source1, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        bool isSourceFileExisting = myFunctions.CheckFile(source, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        if (!isSourceFileExisting)
                        {
                            Process currentProcess = Process.GetCurrentProcess();
                            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));
                            source = currentPath + "\\" + source;
                            isSourceFileExisting = myFunctions.CheckFile(source, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        }
                        if (!isSourceFileExisting)
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> '" + source1 + "' does NOT exists!", GlobalClass.SECTION);
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during file copy job", GlobalClass.SECTION);
                        }
                        else
                        {
                            if (myFunctions.CopyFile(source, destination, overwrite, GlobalClass.VarTable, GlobalClass.ParameterTable))
                            {
                                GlobalClass.logger.Info("::" + node.Name + " --> file copied successfully", GlobalClass.SECTION);
                            }
                            else
                            {
                                GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during file copy job", GlobalClass.SECTION);
                            }
                        }
                    }
                    break;

                case "addfolder":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        if (myFunctions.CreateDirectory(path))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> folder created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during addfolder", GlobalClass.SECTION);
                        }
                    }
                    break;


                case "addfilepermission":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string permission = node.Attributes.GetNamedItem("permission").Value.ToString();
                        string account = node.Attributes.GetNamedItem("account").Value.ToString();
                        string replace = "";
                        try
                        {
                            replace = node.Attributes.GetNamedItem("replace").Value.ToString();
                        }
                        catch { }
                        bool lbReplace = false;
                        if (replace.ToString()=="true")
                        {
                            lbReplace = true;
                        }
                        if (myFunctions.AddFilePermission(path, account, permission, lbReplace))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> permissions created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
                        }
                    }
                    break;


                case "removefilepermission":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string permission = node.Attributes.GetNamedItem("permission").Value.ToString();
                        string account = node.Attributes.GetNamedItem("account").Value.ToString();
                        string replace = "";
                        try
                        {
                            replace = node.Attributes.GetNamedItem("replace").Value.ToString();
                        }
                        catch { }
                        bool lbReplace = false;
                        if (replace.ToString() == "true")
                        {
                            lbReplace = true;
                        }
                        if (myFunctions.RemoveFilePermission(path, account, permission, lbReplace))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> permissions remove successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
                        }
                    }
                    break;


                case "addregpermission":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string permission = node.Attributes.GetNamedItem("permission").Value.ToString();
                        string account = node.Attributes.GetNamedItem("account").Value.ToString();
                        string replace = "";
                        try
                        {
                            replace = node.Attributes.GetNamedItem("replace").Value.ToString();
                        }
                        catch { }
                        bool lbReplace = false;
                        if (replace.ToString() == "true")
                        {
                            lbReplace = true;
                        }
                        if (myFunctions.AddRegPermission(path, account, permission, lbReplace))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> permissions created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
                        }
                    }
                    break;


                case "removeregpermission":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string permission = node.Attributes.GetNamedItem("permission").Value.ToString();
                        string account = node.Attributes.GetNamedItem("account").Value.ToString();
                        string replace = "";
                        try
                        {
                            replace = node.Attributes.GetNamedItem("replace").Value.ToString();
                        }
                        catch { }
                        bool lbReplace = false;
                        if (replace.ToString() == "true")
                        {
                            lbReplace = true;
                        }
                        if (myFunctions.RemoveRegPermission(path, account, permission, lbReplace))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> permissions remove successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "addshortcut":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string target = node.Attributes.GetNamedItem("destination").Value.ToString();
                        string workdir = "";
                        try
                        {
                            workdir = node.Attributes.GetNamedItem("workdir").Value.ToString();
                        }
                        catch { }
                        string iconpath = "";
                        try
                        {
                            iconpath = node.Attributes.GetNamedItem("iconpath").Value.ToString();
                        }
                        catch { }
                        int iconindex = 0;
                        try
                        {
                            iconindex = Convert.ToInt32(node.Attributes.GetNamedItem("iconindex").Value.ToString());
                        }
                        catch { }
                        string arguments = "";
                        try
                        {
                            arguments = node.Attributes.GetNamedItem("arguments").Value.ToString();
                        }
                        catch { }
                        string description = "";
                        try
                        {
                            description = node.Attributes.GetNamedItem("description").Value.ToString();
                        }
                        catch { }

                        myFunctions.ReplaceEnvVariables(ref path, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref target, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref workdir, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref arguments, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref iconpath, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref description, GlobalClass.VarTable, GlobalClass.ParameterTable);

                        if (iconpath != "")
                        {
                            if (!iconpath.Contains(@"\"))
                            {
                                try
                                {
                                    //iconpath = pc.MainModule.FileName.Substring(0, pc.MainModule.FileName.LastIndexOf(@"\")) + @"\" + iconpath;
                                }
                                catch { }
                            }
                        }

                        if (myFunctions.CreateShortcut(path, target, workdir, arguments, iconpath, iconindex, description))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> shortcut created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during addshortcut", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "copyfolder":
                    {
                        string source = node.Attributes.GetNamedItem("source").Value.ToString();
                        string destination = node.Attributes.GetNamedItem("destination").Value.ToString();
                        string overwrite = "true"; //default
                        try
                        {
                            overwrite = node.Attributes.GetNamedItem("overwrite").Value.ToString().ToLower();
                        }
                        catch { }
                        SetLabel(node.Name, "Copy folder: '" + source + "' TO '" + destination + "' (overwrite:" + overwrite + ")", "");
                        string source1 = source;
                        myFunctions.ReplaceEnvVariables(ref source1, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        bool isSourceFolderExisting = myFunctions.CheckFolder(source, "false", GlobalClass.VarTable, GlobalClass.ParameterTable);
                        if (!isSourceFolderExisting)
                        {
                            Process currentProcess = Process.GetCurrentProcess();
                            string currentPath = currentProcess.MainModule.FileName.Substring(0, currentProcess.MainModule.FileName.LastIndexOf(@"\"));
                            source = currentPath + "\\" + source;
                            isSourceFolderExisting = myFunctions.CheckFolder(source, "false", GlobalClass.VarTable, GlobalClass.ParameterTable);
                        }
                        if (!isSourceFolderExisting)
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> '" + source1 + "' does NOT exists!", GlobalClass.SECTION);
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during folder copy job", GlobalClass.SECTION);
                        }
                        else
                        {
                            if (myFunctions.CopyFolder(source, destination, overwrite, GlobalClass.VarTable, GlobalClass.ParameterTable))
                            {
                                GlobalClass.logger.Info("::" + node.Name + " --> folder copied successfully", GlobalClass.SECTION);
                            }
                            else
                            {
                                GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during folder copy job", GlobalClass.SECTION);
                            }
                        }
                    }
                    break;

                case "expandfolder":
                    {
                        string source = node.Attributes.GetNamedItem("source").Value.ToString();
                        string destination = node.Attributes.GetNamedItem("destination").Value.ToString();
                        string overwrite = "true"; //default
                        try
                        {
                            overwrite = node.Attributes.GetNamedItem("overwrite").Value.ToString().ToLower();
                        }
                        catch
                        {
                            //do nothing
                        }
                        SetLabel(node.Name, "Expand folder: '" + source + "' TO '" + destination + "' (overwrite:" + overwrite + ")", "");
                        string source1 = source;
                        string destination1 = destination;
                        myFunctions.ReplaceEnvVariables(ref source1, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref destination1, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        bool isSourceFileExisting = false;
                        if (System.IO.File.Exists(source1))
                        {
                            isSourceFileExisting = true;
                        }
                        //else
                        //{
                        //    if(System.IO.File.Exists(Application.StartupPath + @"\" + source1))
                        //    {
                        //        source1 = Application.StartupPath + @"\" + source1;
                        //    }
                        //}
                        if (!isSourceFileExisting)
                        {
                            GlobalClass.logger.Warn("::" + node.Name + " --> '" + source1 + "' does NOT exists!", GlobalClass.SECTION);
                            GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during expand folder job", GlobalClass.SECTION);
                        }
                        else
                        {
                            if (myFunctions.ExpandFolder(source1, destination1, overwrite))
                            {
                                GlobalClass.logger.Info("::" + node.Name + " --> folder expanded successfully", GlobalClass.SECTION);
                            }
                            else
                            {
                                GlobalClass.logger.Warn("::" + node.Name + " --> ERROR during expanding folder job", GlobalClass.SECTION);
                            }
                        }
                    }
                    break;

                case "addfont":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        if (myFunctions.AddFont(path))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> font entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during font entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "removefont":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        if (myFunctions.RemoveFont(path))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> font entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during font entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "addpath":
                    {
                        string path = node.Attributes.GetNamedItem("value").Value.ToString();
                        if (myFunctions.AddPath(path))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> path entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during path entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "removepath":
                    {
                        string path = node.Attributes.GetNamedItem("value").Value.ToString();
                        if (myFunctions.RemovePath(path))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> path entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during path entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "moveshortcuts":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();

                        if (myFunctions.MoveShortcuts(path, GlobalClass.StartMenuFilesBefore))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> MoveShortcuts created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during MoveShortcuts creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "removeshortcuts":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string filename = "";
                        try
                        {
                            filename = node.Attributes.GetNamedItem("filename").Value.ToString();
                        }
                        catch { }
                        string mode = "";
                        try
                        {
                            mode = node.Attributes.GetNamedItem("mode").Value.ToString();
                        }
                        catch { }

                        if (myFunctions.RemoveShortcuts(path, filename, mode, GlobalClass.StartMenuFilesBefore, GlobalClass.DesktopFilesBefore))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> RemoveShortcuts created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during RemoveShortcuts creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "writeini":
                    {
                        string iniFile = node.Attributes.GetNamedItem("inifile").Value.ToString();
                        string section = node.Attributes.GetNamedItem("section").Value.ToString();
                        string key = node.Attributes.GetNamedItem("key").Value.ToString();
                        string value = node.Attributes.GetNamedItem("value").Value.ToString();
                        if (myFunctions.WriteIni(section, key, value, iniFile))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ini entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during ini entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "addtext":
                    {
                        GlobalClass.logger.Info(":: addtext", GlobalClass.SECTION);
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string value = node.Attributes.GetNamedItem("value").Value.ToString();
                        myFunctions.ReplaceEnvVariables(ref path, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref value, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        if (myFunctions.AddText(path, value))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> addtext entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during addtext entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "removetext":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string value = node.Attributes.GetNamedItem("value").Value.ToString();
                        string aggressive = "false"; //default
                        try
                        {
                            aggressive = node.Attributes.GetNamedItem("aggressive").Value.ToString().ToLower();
                        }
                        catch { }
                        myFunctions.ReplaceEnvVariables(ref path, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref value, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        if (myFunctions.RemoveText(path, value, aggressive))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> removetext entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during removetext entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "removeexistingsoftware":
                    {
                        SetLabel(node.Name, "Removing existing software", "");
                        string uninstalltype = node.Attributes.GetNamedItem("uninstalltype").Value.ToString();
                        string value = node.Attributes.GetNamedItem("value").Value.ToString();
                        if (myFunctions.RemoveExistingSoftware(uninstalltype, value))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> removeexistingsoftware successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during removeexistingsoftware", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "sendkeys":
                    {
                        string lsctrlprocess = nodeGetAttribute(node, "ctrlprocess");
                        string lsctrlid = nodeGetAttribute(node, "ctrlid");
                        string lsKeys = nodeGetAttribute(node, "keys");

                        SetLabel(node.Name, "SendKeys", "'" + lsKeys + "'");

                        if (myFunctions.SendKeys(lsKeys, lsctrlprocess, lsctrlid))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> sendkeys successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during sendkeys", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "sendkeyboardcommand":
                    {
                        string lsctrlprocess = nodeGetAttribute(node, "ctrlprocess");
                        string lsctrlid = nodeGetAttribute(node, "ctrlid");
                        string lsKeys = nodeGetAttribute(node, "keys");
                        if (lsKeys=="")
                        {
                            lsKeys = nodeGetAttribute(node, "value");
                        }

                        SetLabel(node.Name, "sendkeyboardcommand", "'" + lsKeys + "'");

                        if (myFunctions.SendKeys(lsKeys, lsctrlprocess, lsctrlid))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> sendkeyboardcommand successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during sendkeyboardcommand", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "sendwindowcommand":
                    {
                        string lscommand = nodeGetAttribute(node, "command");
                        string lsctrlprocess = nodeGetAttribute(node, "ctrlprocess");
                        string lsctrlid = nodeGetAttribute(node, "ctrlid");

                        SetLabel(node.Name, "SendWindowCommand", "'" + lscommand + ", '" + lsctrlprocess + "'");

                        if (myFunctions.SendWindow(lscommand, lsctrlprocess, lsctrlid))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> sendwindow successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during sendwindow", GlobalClass.SECTION);
                        }

                    }
                    break;

                case "sendmousecommand":

                    {
                        string lscommand = nodeGetAttribute(node, "command");
                        string lsx = nodeGetAttribute(node, "x");
                        string lsy = nodeGetAttribute(node, "y");
                        string lspositiontype = nodeGetAttribute(node, "positiontype");
                        string lsxEnd = nodeGetAttribute(node, "xEnd");
                        string lsyEnd = nodeGetAttribute(node, "yEnd");
                        string lsctrlprocess = nodeGetAttribute(node, "ctrlprocess");
                        string lsctrlid = nodeGetAttribute(node, "ctrlid");
                        string showmove = nodeGetAttribute(node, "showmove");

                        SetLabel(node.Name, "SendMouse", "'" + lscommand + "'");

                        if (myFunctions.SendMouse(lscommand, lsx, lsy, lspositiontype, lsxEnd, lsyEnd, lsctrlprocess, lsctrlid, showmove))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> sendmouse successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during sendmouse", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "setresolution":
                    {
                        string lsresolution = nodeGetAttribute(node, "resolution");
                        myFunctions.SetResolution(lsresolution);
                    }
                    break;

                case "replacetext":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        string source = node.Attributes.GetNamedItem("source").Value.ToString();
                        string destination = node.Attributes.GetNamedItem("destination").Value.ToString();
                        myFunctions.ReplaceEnvVariables(ref path, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref source, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        myFunctions.ReplaceEnvVariables(ref destination, GlobalClass.VarTable, GlobalClass.ParameterTable);
                        if (myFunctions.ReplaceText(path, source, destination))
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> replacetext entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info("::" + node.Name + " --> ERROR during replacetext entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "checkserver":
                    bool isServer = myFunctions.CheckServer();
                    foreach (XmlNode checkfileChildnode in node.ChildNodes)
                    {
                        switch (checkfileChildnode.Name)
                        {
                            case "exists":
                                //GlobalClass.logger.Info("check type: 'EXISTS'");
                                if (isServer)
                                {
                                    GlobalClass.logger.Info("::checkserver --> " + "exists!", GlobalClass.SECTION);
                                    foreach (XmlNode fileexistsnode in checkfileChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(fileexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info("::checkserver --> " + "server does NOT exists!", GlobalClass.SECTION);
                                break;
                            case "notexists":
                                //GlobalClass.logger.Info("check type: 'NOTEXISTS'");
                                if (isServer == false)
                                {
                                    GlobalClass.logger.Info("::checkserver --> " + "server does NOT exist!", GlobalClass.SECTION);
                                    foreach (XmlNode filenotexistsnode in checkfileChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(filenotexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info("::checkserver --> server exists!", GlobalClass.SECTION);
                                break;
                        }
                    }
                    break;


                case "getoslanguage":
                    string languagecode = "";

                    SetLabel(node.Name, "Get OS language", "");
                    languagecode = myFunctions.GetOSLanguage();

                    if (languagecode == null)
                    {
                        GlobalClass.logger.Info("::" + node.Name + " --> OS language ID NOT found!", GlobalClass.SECTION);
                    }
                    else
                    {
                        GlobalClass.logger.Info("::" + node.Name + " --> OS language ID (decimal notation): " + languagecode, GlobalClass.SECTION);

                        try
                        {
                            foreach (XmlNode childnode2 in node.ChildNodes)
                            {
                                if (childnode2.Name == "languagecodes")
                                {
                                    bool errorCodeExistsInXML = false;
                                    foreach (XmlNode retvaluechildnode in childnode2.ChildNodes)
                                    {
                                        if (languagecode == retvaluechildnode.Attributes.GetNamedItem("value").Value.ToString())
                                        {
                                            errorCodeExistsInXML = true;
                                            foreach (XmlNode childnode4 in retvaluechildnode.ChildNodes)
                                            {
                                                abort = ExecuteTask(childnode4);
                                            }
                                        }

                                        //general language code? (Not 0)
                                        if (!languagecode.Equals("0"))
                                        {
                                            if (!errorCodeExistsInXML)
                                            {
                                                if (retvaluechildnode.Attributes.GetNamedItem("value").Value.ToString() == "!0")
                                                {
                                                    foreach (XmlNode childnode3 in retvaluechildnode.ChildNodes)
                                                    {
                                                        abort = ExecuteTask(childnode3);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show(ex.Message, TASKNAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            GlobalClass.logger.Error("###" + ex.ToString(), GlobalClass.SECTION);
                            GlobalClass.logger.WritetoEventLog("###" + ex.ToString(), GlobalClass.LERROR, GlobalClass.SECTION);
                        }
                    }
                    break;

            } //switch (node)
              //"1.3","1.3.1","1.3.2","1.5.0","1.6.0"


            //GlobalClass.IsAborted = abort;
            if (GlobalClass.IsAborted)
            {
                if (GlobalClass.ExitCode >= 0)
                {
                    //already defined by default <abort/> parameter
                }
                else
                {
                    GlobalClass.ExitCode = GlobalClass.EC_ABORT; //general abort ErrorCode
                }
            }
            return GlobalClass.IsAborted;
        }

        private string nodeGetAttribute(XmlNode node, string attr)
        {
            string ret = "";
            try { ret = node.Attributes.GetNamedItem(attr).Value.ToString(); }
            catch { }
            return ret;
        }

        private void SetLabel(string XMLnode, string labelText, string logDetails)
        {
            //set_labeltext(labelText);
            ActualLabel = labelText;
            SetLabelEvent(this, null);
            GlobalClass.logger.Info("TASK (XMLnode:'" + XMLnode + "'): " + labelText + " (" + logDetails + ")", GlobalClass.SECTION);
        }


        private static string StringEncode(string plainText, string key)
        {
            byte[] plainArr = Encoding.UTF8.GetBytes(plainText);
            byte[] keyArr = Encoding.UTF8.GetBytes(key);
            for (int nr = 0; nr<plainArr.Length; nr++) plainArr[nr] += keyArr[nr%keyArr.Length];
            return Convert.ToBase64String(plainArr);
        }
        private static string StringDecode(string encodedText, string key)
        {
            byte[] encodedArr = Convert.FromBase64String(encodedText);
            byte[] keyArr = Encoding.UTF8.GetBytes(key);
            for (int nr = 0; nr<encodedArr.Length; nr++) encodedArr[nr] -= keyArr[nr%keyArr.Length];
            return Encoding.UTF8.GetString(encodedArr, 0, encodedArr.Length);
        }


    }



}
