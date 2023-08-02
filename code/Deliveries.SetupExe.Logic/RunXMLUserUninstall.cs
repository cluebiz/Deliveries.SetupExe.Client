using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Deliveries.SetupExe.Logic
{
    public class RunXMLUserUninstall
    {
        private Functions myFunctions = new Functions();

        public bool ExecuteTask(XmlNode node)
        {
            bool abort = false;
            bool isOK = true;
            int returnvalue = 0;
            string errormsg = "";

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


                    isOK = myFunctions.CheckDiskSpace(node.Attributes.GetNamedItem("driveletter").Value.ToString(), optionaldiskspace, physicaldrive);
                    if (isOK == false)
                    {
                        GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> not existing drive or NOT enough available diskspace!", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                        GlobalClass.ExitCode = GlobalClass.EC_NotEnoughDiskspace;
                    }
                    else
                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> drive available and free diskspace ok!", GlobalClass.SECTION);
                    break;

                case "checkmemory":
                    isOK = myFunctions.CheckMemory(node.Attributes.GetNamedItem("memory").Value.ToString());
                    if (isOK == false)
                    {
                        GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> not enough physical memory available!", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                        GlobalClass.ExitCode = GlobalClass.EC_NotEnoughMemory;
                    }
                    else
                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> physical memory ok!", GlobalClass.SECTION);
                    break;

                case "checkbattery":

                    while (!myFunctions.CheckBattery() && !abort)
                    {
                        GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ACLineStatus: 'offline' (Battery mode)", GlobalClass.SECTION);
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
                    break;

                case "checkmsiinstallation":
                    string GUID = myFunctions.GetInstalledGUID(node.Attributes.GetNamedItem("package").Value.ToString());
                    foreach (XmlNode checkmsiChildnode in node.ChildNodes)
                    {
                        switch (checkmsiChildnode.Name)
                        {
                            case "exists":
                                if (GUID != "")
                                {
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + GUID + "' installed...", GlobalClass.SECTION);
                                    foreach (XmlNode msiexistsnode in checkmsiChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(msiexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("package").Value.ToString() + "' NOT installed.", GlobalClass.SECTION);
                                break;

                            case "notexists":
                                if (GUID == "")
                                {
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("package").Value.ToString() + "' NOT installed.", GlobalClass.SECTION);
                                    foreach (XmlNode msinotexistsnode in checkmsiChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(msinotexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("package").Value.ToString() + "' installed.", GlobalClass.SECTION);
                                break;
                        }
                    }
                    break;

                case "checkfolder":
                    string folderpath = node.Attributes.GetNamedItem("path").Value.ToString();
                    string contentrequired = node.Attributes.GetNamedItem("contentrequired").Value.ToString().ToUpper();

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
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists (and is NOT empty)!", GlobalClass.SECTION);
                                    else
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                    foreach (XmlNode fileexistsnode in checkfolderChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(fileexistsnode);
                                    }
                                }
                                else
                                    if (contentrequired.IndexOf("TRUE") >= 0)
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists OR is empty!", GlobalClass.SECTION);
                                else
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists!", GlobalClass.SECTION);
                                break;
                            case "notexists":
                                //logger.Info("check type: 'NOTEXISTS'");
                                if (isFolderExisting == false)
                                {
                                    if (contentrequired.IndexOf("TRUE") >= 0)
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exist OR is empty!", GlobalClass.SECTION);
                                    else
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exist!", GlobalClass.SECTION);
                                    foreach (XmlNode filenotexistsnode in checkfolderChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(filenotexistsnode);
                                    }
                                }
                                else
                                    if (contentrequired.IndexOf("TRUE") >= 0)
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists and is NOT empty!", GlobalClass.SECTION);
                                else
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                break;
                        }
                    }
                    break;

                case "checkfile":
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
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                        foreach (XmlNode fileexistsnode in checkfileChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(fileexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists!", GlobalClass.SECTION);
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
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exists (anymore)!", GlobalClass.SECTION);
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
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' does NOT exist!", GlobalClass.SECTION);
                                        foreach (XmlNode filenotexistsnode in checkfileChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(filenotexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists!", GlobalClass.SECTION);
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
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("path").Value.ToString() + "' exists (now)!", GlobalClass.SECTION);
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
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> registry key exists", GlobalClass.SECTION);
                                        foreach (XmlNode regexistsnode in checkregChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(regexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> registry key does NOT exist!", GlobalClass.SECTION);
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
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> registry key does NOT exist (anymore)!", GlobalClass.SECTION);
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
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> registry key does NOT exist!", GlobalClass.SECTION);
                                        foreach (XmlNode regnotexistsnode in checkregChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(regnotexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> registry key exists", GlobalClass.SECTION);
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
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> registry key does exist (now)!", GlobalClass.SECTION);
                                }

                                break;
                        }
                    }
                    break;

                case "checkarchitecture":
                    bool isX64 = myFunctions.CheckArchitecture();
                    foreach (XmlNode checkx64Childnode in node.ChildNodes)
                    {
                        switch (checkx64Childnode.Name)
                        {
                            case "x64":
                                //logger.Info(":: USERUNINSTALL " + node.Name + " --> 'x64'");
                                if (isX64)
                                {
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> x64 detected", GlobalClass.SECTION);
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
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> x86 detected", GlobalClass.SECTION);
                                    foreach (XmlNode x64notexistsnode in checkx64Childnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(x64notexistsnode);
                                    }
                                }
                                else
                                {
                                    //logger.Info(":: USERUNINSTALL " + node.Name + " --> x64 detected");
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

                    bool isOSExisting = myFunctions.CheckOS(name, version, servicepack, languageid);
                    foreach (XmlNode checkOSChildnode in node.ChildNodes)
                    {
                        switch (checkOSChildnode.Name)
                        {
                            case "exists":
                                if (isOSExisting)
                                {
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> OS exists", GlobalClass.SECTION);
                                    foreach (XmlNode regexistsnode in checkOSChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(regexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> OS does NOT exist!", GlobalClass.SECTION);
                                break;
                            case "notexists":
                                if (isOSExisting == false)
                                {
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> OS does NOT exist!", GlobalClass.SECTION);
                                    foreach (XmlNode regnotexistsnode in checkOSChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(regnotexistsnode);
                                    }
                                }
                                else
                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> OS exists", GlobalClass.SECTION);
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
                    returnvalue = myFunctions.AddRegKey(node.Attributes.GetNamedItem("path").Value.ToString(), node.Attributes.GetNamedItem("name").Value.ToString(), optionalregkeyvalue, node.Attributes.GetNamedItem("type").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable, ref errormsg);
                    if (returnvalue > 0)
                    {
                        GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> returnvalue: " + returnvalue + "; '" + errormsg + "'", GlobalClass.SECTION);
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode);
                        }
                    }
                    else
                    {
                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> returnvalue: " + returnvalue, GlobalClass.SECTION);
                    }
                    break;

                case "removeregkey":
                    string optionalname = "";
                    try
                    {
                        optionalname = node.Attributes.GetNamedItem("name").Value.ToString();
                    }
                    catch (Exception)
                    {
                        //nothing to do ('name' was not defined and so, it's not relevant in that case)
                    }
                    myFunctions.RemoveRegKeyForAllUsers(node.Attributes.GetNamedItem("path").Value.ToString(), optionalname);
                    break;

                case "checkprocess":                    
                case "killprocess":
                case "messagebox":
                case "postpone":
                case "stopservice":
                case "startservice":
                case "msiexec":
                case "execute":
                case "executenowait":
                case "setreboot":
                case "addfont":
                case "removefont":
                case "addpath":
                case "removepath":
                case "moveshortcuts":
                    {
                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> task '" + node.Name + "' skipped; ", GlobalClass.SECTION);
                    }
                    break;


                case "removefile":
                case "removefolder":
                    returnvalue = myFunctions.RemoveFileOrFolderForAllUsers(node.Attributes.GetNamedItem("path").Value.ToString(), GlobalClass.VarTable, GlobalClass.ParameterTable);
                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> returnvalue: " + returnvalue, GlobalClass.SECTION);
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
                        {
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> Aborting (without User Message - nobody is logged on); '" + msg + "'", GlobalClass.SECTION);
                        }
                    }
                    else
                        GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> Aborting (without User Message)", GlobalClass.SECTION);
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
                    millisecondsToSleep = Convert.ToInt32(node.Attributes.GetNamedItem("seconds").Value.ToString()) * 1000;


                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> sleeping: for " + millisecondsToSleep, GlobalClass.SECTION);

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
                    break;

                case "startaltiristask":
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

                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> setvariable: updating '" + loVarRow["name"].ToString().ToLower() + "' to '" + value + @"'.", GlobalClass.SECTION);

                                }
                            }

                            if (!lbDoneSomething)
                            {
                                DataRow loNewRow = GlobalClass.VarTable.NewRow();
                                loNewRow["name"] = var;
                                loNewRow["value"] = value;
                                GlobalClass.VarTable.Rows.Add(loNewRow);

                                GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> setvariable: adding '" + var + "' to '" + value + @"'.", GlobalClass.SECTION);
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

                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> getvariablefromregistry: reading registry " + path + " - " + myname, GlobalClass.SECTION);

                            string myvalue = myFunctions.ReadRegistry(path, myname);

                            bool lbDoneSomething = false;
                            foreach (DataRow loVarRow in GlobalClass.VarTable.Rows)
                            {
                                if (loVarRow["name"].ToString().ToLower() == var.ToLower())
                                {
                                    loVarRow["value"] = myvalue;
                                    lbDoneSomething = true;

                                    GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> getvariablefromregistry: updating '" + loVarRow["name"].ToString().ToLower() + "' to '" + myvalue + @"'.", GlobalClass.SECTION);

                                }
                            }
                            if (!lbDoneSomething)
                            {
                                DataRow loNewRow = GlobalClass.VarTable.NewRow();
                                loNewRow["name"] = var;
                                loNewRow["value"] = myvalue;
                                GlobalClass.VarTable.Rows.Add(loNewRow);

                                GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> getvariablefromregistry: adding '" + var.ToLower() + "' to '" + myvalue + @"'.", GlobalClass.SECTION);
                            }

                            GlobalClass.VarTable.AcceptChanges();
                        }
                    }
                    break;

                case "checkvariable":
                    {
                        string var = node.Attributes.GetNamedItem("var").Value.ToString().ToLower();
                        string value = node.Attributes.GetNamedItem("value").Value.ToString();


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
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' exists!", GlobalClass.SECTION);
                                        foreach (XmlNode fileexistsnode in checkfolderChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(fileexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' does NOT exists!", GlobalClass.SECTION);
                                    }
                                    break;
                                case "notexists":
                                    if (isVariableExisting == false)
                                    {
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' does NOT exist!", GlobalClass.SECTION);
                                        foreach (XmlNode filenotexistsnode in checkfolderChildnode.ChildNodes)
                                        {
                                            abort = ExecuteTask(filenotexistsnode);
                                        }
                                    }
                                    else
                                    {
                                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> '" + node.Attributes.GetNamedItem("var").Value.ToString() + "' exists!", GlobalClass.SECTION);
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
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> '" + source1 + "' does NOT exists!", GlobalClass.SECTION);
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during file copy job", GlobalClass.SECTION);
                        }
                        else
                        {
                            if (myFunctions.CopyFile(source, destination, overwrite, GlobalClass.VarTable, GlobalClass.ParameterTable))
                            {
                                GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> file copied successfully", GlobalClass.SECTION);
                            }
                            else
                            {
                                GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during file copy job", GlobalClass.SECTION);
                            }
                        }
                    }
                    break;

                case "addfolder":
                    {
                        string path = node.Attributes.GetNamedItem("path").Value.ToString();
                        if (myFunctions.CreateDirectory(path))
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> folder created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during addfolder", GlobalClass.SECTION);
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
                        if (replace.ToString() == "true")
                        {
                            lbReplace = true;
                        }
                        if (myFunctions.AddFilePermission(path, account, permission, lbReplace))
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> permissions created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> permissions remove successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> permissions created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> permissions remove successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during permissions", GlobalClass.SECTION);
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> shortcut created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during addshortcut", GlobalClass.SECTION);
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
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> '" + source1 + "' does NOT exists!", GlobalClass.SECTION);
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during folder copy job", GlobalClass.SECTION);
                        }
                        else
                        {
                            if (myFunctions.CopyFolder(source, destination, overwrite, GlobalClass.VarTable, GlobalClass.ParameterTable))
                            {
                                GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> folder copied successfully", GlobalClass.SECTION);
                            }
                            else
                            {
                                GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during folder copy job", GlobalClass.SECTION);
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
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> '" + source1 + "' does NOT exists!", GlobalClass.SECTION);
                            GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during expand folder job", GlobalClass.SECTION);
                        }
                        else
                        {
                            if (myFunctions.ExpandFolder(source1, destination1, overwrite))
                            {
                                GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> folder expanded successfully", GlobalClass.SECTION);
                            }
                            else
                            {
                                GlobalClass.logger.Warn(":: USERUNINSTALL " + node.Name + " --> ERROR during expanding folder job", GlobalClass.SECTION);
                            }
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
                            mode = node.Attributes.GetNamedItem("filename").Value.ToString();
                        }
                        catch { }

                        if (myFunctions.RemoveShortcuts(path, filename, mode, GlobalClass.StartMenuFilesBefore, GlobalClass.DesktopFilesBefore))
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> RemoveShortcuts created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> ERROR during RemoveShortcuts creation", GlobalClass.SECTION);
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> ini entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> ERROR during ini entry creation", GlobalClass.SECTION);
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> addtext entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> ERROR during addtext entry creation", GlobalClass.SECTION);
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> removetext entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> ERROR during removetext entry creation", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "removeexistingsoftware":
                    {
                        string uninstalltype = node.Attributes.GetNamedItem("uninstalltype").Value.ToString();
                        string value = node.Attributes.GetNamedItem("value").Value.ToString();
                        if (myFunctions.RemoveExistingSoftware(uninstalltype, value))
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> removeexistingsoftware successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> ERROR during removeexistingsoftware", GlobalClass.SECTION);
                        }
                    }
                    break;

                case "sendkeys":
                case "sendkeyboardcommand":
                case "sendwindowcommand":
                case "sendmousecommand":
                case "setresolution":
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
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> replacetext entry created successfully", GlobalClass.SECTION);
                        }
                        else
                        {
                            GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> ERROR during replacetext entry creation", GlobalClass.SECTION);
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

                    languagecode = myFunctions.GetOSLanguage();

                    if (languagecode == null)
                    {
                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> OS language ID NOT found!", GlobalClass.SECTION);
                    }
                    else
                    {
                        GlobalClass.logger.Info(":: USERUNINSTALL " + node.Name + " --> OS language ID (decimal notation): " + languagecode, GlobalClass.SECTION);

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

    }
}
