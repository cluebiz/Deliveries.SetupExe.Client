using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Deliveries.SetupExe.Logic;

namespace Deliveries.SetupExe.Logic
{
    public class Tasks
    {
        public bool ExecuteTask(XmlNode node, DataTable loVarTable, string GlobalCommandLine, string ProductCode)
        {
            //variables, objects
            bool isOK = true;
            bool abort = false;
            int returnvalue = 0;
            string errorMessage = "";
            const int STOPSERVICE = 0;
            const int STARTSERVICE = 1;
            Deliveries.SetupExe.Logic.Functions myFunctions = new Deliveries.SetupExe.Logic.Functions();
            myFunctions.GlobalCommandLine = GlobalCommandLine;
            myFunctions.ProductCode = ProductCode;

            //parse XML tasks
            switch (node.Name)
            {
                case "checkmsiinstallation":
                    string GUID = myFunctions.GetInstalledGUID(node.Attributes.Item(0).Value.ToString());
                    if (GUID != "")
                    {
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode, loVarTable, GlobalCommandLine, ProductCode);
                        }
                    }
                    break;

                case "checkbattery":
                    isOK = myFunctions.CheckBattery();
                    if (isOK == false)
                    {
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode, loVarTable, GlobalCommandLine, ProductCode);
                        }
                    }
                    break;

                case "checkfullscreen":
                    isOK = myFunctions.CheckFullscreen();
                    if (isOK == false)
                    {
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode, loVarTable, GlobalCommandLine, ProductCode);
                        }
                    }
                    break;

                case "checkdiskspace":
                    isOK = myFunctions.CheckDiskSpace(node.Attributes.Item(0).Value.ToString(), node.Attributes.Item(1).Value.ToString());
                    if (isOK == false)
                    {
                        foreach (XmlNode childnode in node.ChildNodes)
                        {
                            abort = ExecuteTask(childnode, loVarTable, GlobalCommandLine, ProductCode);
                        }
                    }
                    break;

                case "checkfile":
                    bool isFileExisting = myFunctions.CheckFile(node.Attributes.Item(0).Value.ToString(), loVarTable);
                    foreach (XmlNode checkfileChildnode in node.ChildNodes)
                    {
                        switch (checkfileChildnode.Name)
                        {
                            case "exists":
                                if (isFileExisting)
                                {
                                    foreach (XmlNode fileexistsnode in checkfileChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(fileexistsnode, loVarTable, GlobalCommandLine, ProductCode);
                                    }
                                }
                                break;
                            case "notexists":
                                if (isFileExisting == false)
                                {
                                    foreach (XmlNode filenotexistsnode in checkfileChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(filenotexistsnode, loVarTable, GlobalCommandLine, ProductCode);
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case "checkregistry":
                    bool isRegEntryExisting = myFunctions.CheckRegistry(node.Attributes.Item(0).Value.ToString(), node.Attributes.Item(1).Value.ToString(), node.Attributes.Item(2).Value.ToString());
                    foreach (XmlNode checkregChildnode in node.ChildNodes)
                    {
                        switch (checkregChildnode.Name)
                        {
                            case "exists":
                                if (isRegEntryExisting)
                                {
                                    foreach (XmlNode regexistsnode in checkregChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(regexistsnode, loVarTable, GlobalCommandLine, ProductCode);
                                    }
                                }
                                break;
                            case "notexists":
                                if (isRegEntryExisting == false)
                                {
                                    foreach (XmlNode regnotexistsnode in checkregChildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(regnotexistsnode, loVarTable, GlobalCommandLine, ProductCode);
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case "addregkey":
                    myFunctions.AddRegKey(node.Attributes.Item(0).Value.ToString(), node.Attributes.Item(1).Value.ToString(), node.InnerText.ToString(), node.Attributes.Item(2).Value.ToString(), loVarTable, ref errorMessage);
                    break;

                case "removeregkey":
                    myFunctions.RemoveRegKey(node.Attributes.Item(0).Value.ToString(), node.Attributes.Item(1).Value.ToString());
                    break;

                case "checkprocess":
                    string looping = node.Attributes.Item(1).Value.ToString();

                    if (looping == "true")
                    {
                        while (myFunctions.CheckProcess(node.Attributes.Item(0).Value.ToString()))
                        {
                            foreach (XmlNode childnode in node.ChildNodes)
                            {
                                abort = ExecuteTask(childnode, loVarTable, GlobalCommandLine, ProductCode);
                            }
                        }
                    }
                    else
                    {
                        if (myFunctions.CheckProcess(node.Attributes.Item(0).Value.ToString()))
                        {
                            foreach (XmlNode childnode in node.ChildNodes)
                            {
                                abort = ExecuteTask(childnode, loVarTable, GlobalCommandLine, ProductCode);
                            }
                        }
                    }
                    break;

                case "killprocess":
                    myFunctions.KillProcess(node.Attributes.Item(0).Value.ToString());
                    break;

                case "messagebox":
                    MessageBox.Show(node.InnerText.ToString());
                    break;

                case "stopservice":
                    myFunctions.ServiceMgr(node.Attributes.Item(0).Value.ToString(), STOPSERVICE, ref errorMessage);
                    break;

                case "startservice":
                    myFunctions.ServiceMgr(node.Attributes.Item(0).Value.ToString(), STARTSERVICE, ref errorMessage);
                    break;

                /*
                case "removemsipackage":
                    foreach(XmlNode childnode in node.ChildNodes)
                    {
                        returnvalue = myFunctions.RemoveMSIPackage(childnode.InnerText.ToString());
                    }
                    break;
                */

                case "removefile":
                case "removefolder":
                    returnvalue = myFunctions.RemoveFileOrFolder(node.Attributes.Item(0).Value.ToString(), loVarTable);
                    break;

                case "msiexec":
                case "execute":
                    string cmdline = "";
                    string parameters = "";

                    //executable
                    cmdline = node.Attributes.Item(0).Value.ToString();
                    //parameters
                    foreach (XmlNode childnode in node.ChildNodes)
                    {
                        if (childnode.Name == "parameters")
                        {
                            foreach (XmlNode parameterchildnode in childnode.ChildNodes)
                            {
                                parameters += parameterchildnode.InnerText.ToString();
                            }
                        }
                    }
                    string myreturnvalue = "";
                    myreturnvalue = myFunctions.ExecuteCMD(cmdline, parameters, ref errorMessage, "true", loVarTable);
                    returnvalue = 0;
                    try
                    {
                        returnvalue = Convert.ToInt32(myreturnvalue);
                    }
                    catch { }
                    //handle return values
                    foreach (XmlNode childnode2 in node.ChildNodes)
                    {
                        if (childnode2.Name == "returnvalues")
                        {
                            foreach (XmlNode retvaluechildnode in childnode2.ChildNodes)
                            {
                                if (retvaluechildnode.Attributes.Item(0).Value.ToString() == "!0")
                                {
                                    foreach (XmlNode childnode3 in retvaluechildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(childnode3, loVarTable, GlobalCommandLine, ProductCode);
                                    }
                                }
                                else if (returnvalue == Convert.ToInt16(retvaluechildnode.Attributes.Item(0).Value.ToString()))
                                {
                                    foreach (XmlNode childnode4 in retvaluechildnode.ChildNodes)
                                    {
                                        abort = ExecuteTask(childnode4, loVarTable, GlobalCommandLine, ProductCode);
                                    }
                                }
                            }
                        }
                    }

                    break;

                case "sendemail":
                    break;

                case "setreboot":
                    MessageBox.Show("reboot --> set");
                    //GlobalClass.IsRebootEnabled = true;
                    break;

                case "abort":
                    MessageBox.Show("abort");
                    abort = true;
                    break;
            }
            return abort;
        }
    }
}
