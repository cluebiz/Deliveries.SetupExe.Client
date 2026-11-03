using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;

namespace Deliveries.FastWrapper.Logic
{
    public class clsConvertEngine
    {


        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }



        public static string ModifyDeployXMLWithNewSubfolder(string lsDeployXML, string lsNewSubFolder)
        {

            writeToLogFile("modifydeployxmlwithnewsubfolder: start");
            int liCount = 0;

            bool lbFoundSomething = false;
            do
            {
                writeToLogFile("modifydeployxmlwithnewsubfolder: round " + liCount.ToString());
                liCount += 1;
                if (liCount > 1000)
                {
                    lbFoundSomething = false;
                    break;
                }

                lbFoundSomething = false;
                string lsResult = FindBetween(lsDeployXML, @"path=""", @"""");
                if (lsResult != "")
                {
                    if (!lsResult.StartsWith("%") && !lsResult.StartsWith(lsNewSubFolder))
                    {
                        string lsNewResult = lsNewSubFolder + lsResult;
                        lsDeployXML = lsDeployXML.Replace(@"path=""" + lsResult + @"""", @"path=""" + lsNewResult + @"""");
                        lbFoundSomething = true;
                    }
                }
                lsResult = "";
                if (!lbFoundSomething)
                {
                    lsResult = "";
                    lsResult = FindBetween(lsDeployXML, @"source=""", @"""");
                    if (lsResult != "")
                    {
                        if (!lsResult.StartsWith("%") && !lsResult.StartsWith(lsNewSubFolder))
                        {
                            string lsNewResult = lsNewSubFolder + lsResult;
                            lsDeployXML = lsDeployXML.Replace(@"source=""" + lsResult + @"""", @"source=""" + lsNewResult + @"""");
                            lbFoundSomething = true;
                        }
                    }
                }
                lsResult = "";
                if (!lbFoundSomething)
                {
                    lsResult = "";
                    for (int liPosition = 0; liPosition < lsDeployXML.Length - "<parameter>".Length; liPosition++)
                    {
                        lsResult = "";
                        if (lsDeployXML.Substring(liPosition).StartsWith("<parameter>"))
                        {
                            lsResult = lsDeployXML.Substring(liPosition + "<parameter>".Length);
                            lsResult = lsResult.Substring(0, lsResult.IndexOf(@"</parameter>"));
                            writeToLogFile("modifydeployxmlwithnewsubfolder: <parameter>" + lsResult + "</parameter>");
                            if (lsResult != "")
                            {
                                if (lsResult.ToLower().Contains(".msi"))
                                {
                                    writeToLogFile("converting msi: " + lsResult);
                                    if (!lsResult.Contains(lsNewSubFolder))
                                    {
                                        if (lsResult.StartsWith(@""""))
                                        {
                                            lsDeployXML = lsDeployXML.Replace("<parameter>" + lsResult + "</parameter>", @"<parameter>""" + lsNewSubFolder + lsResult.Replace(@"""", "") + @"""</parameter>");
                                            lbFoundSomething = true;
                                        }
                                        else
                                        {
                                            lsDeployXML = lsDeployXML.Replace("<parameter>" + lsResult + "</parameter>", @"<parameter>""" + lsNewSubFolder + lsResult + @"""</parameter>");
                                            lbFoundSomething = true;
                                        }
                                        break;
                                    }
                                }
                                if (lsResult.ToLower().Contains(".mst"))
                                {
                                    if (!lsResult.Contains(lsNewSubFolder))
                                    {
                                        if (lsResult.Contains("TRANSFORMS="))
                                        {
                                            if (lsResult.StartsWith(@"TRANSFORMS="""))
                                            {
                                                string lsMyTransform = lsResult.Replace(@"TRANSFORMS=""", "");
                                                lsMyTransform = lsMyTransform.Replace(@"""", "");
                                                lsDeployXML = lsDeployXML.Replace("<parameter>" + lsResult + "</parameter>", @"<parameter>TRANSFORMS=""" + lsNewSubFolder + lsMyTransform + @"""</parameter>");
                                                lbFoundSomething = true;
                                            }
                                            else
                                            {
                                                string lsMyTransform = lsResult.Replace(@"TRANSFORMS=", "");
                                                lsDeployXML = lsDeployXML.Replace("<parameter>" + lsResult + "</parameter>", @"<parameter>TRANSFORMS=" + lsNewSubFolder + lsMyTransform + @"</parameter>");
                                                lbFoundSomething = true;
                                            }
                                            break;
                                        }
                                        lbFoundSomething = true;
                                        break;
                                    }
                                }
                            }

                        }
                    }
                }



            } while (lbFoundSomething == true);

            writeToLogFile("modifydeployxmlwithnewsubfolder: end");

            return lsDeployXML;
        }







        public static string CreateCustomScripts00(string lsDeployXML, string lsManufacturer, string lsProduct, string lsVersion, string lsReleaseName, string lsUninstallKey)
        {

            StringBuilder loStringBuilder = new StringBuilder();

            DataTable loTable = ConvertXMLToDataTable("install", lsDeployXML, false);
            DataTable loUninstallTable = ConvertXMLToDataTable("uninstall", lsDeployXML, false);

            string lsMSI = "";
            string lsMST = "";
            string lsMSP = "";

            foreach (DataRow loRow in loTable.Rows)
            {
                string lsInherit = "";
                for (int i = 1; i < Convert.ToInt32(loRow["level"].ToString()); i++)
                {
                    lsInherit = lsInherit + "  ";
                }

                //    loStringBuilder.AppendLine(@"#parsing action " + loRow["actiontype"].ToString());
                //try
                //{
                //    loStringBuilder.AppendLine(@"#        parameters " + loRow["param"].ToString());
                //}
                //catch { }




                switch (loRow["actiontype"].ToString().ToLower())
                {

                    case "launchprocess":

                        //switch (loRow["command"].ToString().ToLower())
                        //{



                        //}

                        break;

                    case "msiexec":
                        {

                            try
                            {
                                foreach (string lsArgument in CommandLineToArgs(loRow["param"].ToString()))
                                {
                                    if (lsArgument.ToLower().Contains(".msi"))
                                    {
                                        lsMSI = lsArgument;
                                    }
                                    if (lsArgument.ToLower().Contains(".mst"))
                                    {
                                        lsMST = lsArgument;
                                        lsMST = lsMST.Replace("TRANSFORMS=", "");
                                        lsMST = lsMST.Replace("TRANSFORMS =", "");
                                        lsMST = lsMST.Replace(@"""", "");
                                    }
                                    if (lsArgument.ToLower().Contains(".msp"))
                                    {
                                        lsMSP = lsArgument;
                                        lsMSP = lsMSP.Replace(@"""", "");
                                    }
                                }
                            }
                            catch { }


                            break;
                        }
                }
            }


            loStringBuilder.AppendLine(@";------------------------------------------------------------");
            loStringBuilder.AppendLine(@";File:		spf_Setup.ini");
            loStringBuilder.AppendLine(@";Purpose:	Settings File for Setup_automated.vbs");
            loStringBuilder.AppendLine(@";------------------------------------------------------------");
            loStringBuilder.AppendLine(@";Author:	Clearbyte GmbH");
            loStringBuilder.AppendLine(@";Date:		" + DateTime.Now.Year.ToString());
            loStringBuilder.AppendLine(@";------------------------------------------------------------");
            loStringBuilder.AppendLine(@"; NOTE: Lines starting with semicolon(;) will be ignored !");
            loStringBuilder.AppendLine(@";------------------------------------------------------------");
            loStringBuilder.AppendLine(@";Description:");
            loStringBuilder.AppendLine(@";");
            loStringBuilder.AppendLine(@";Section 1");
            loStringBuilder.AppendLine(@";0001 is the executable an MSI? (Values 'Y' or 'N')");
            loStringBuilder.AppendLine(@";0002 Name of the executable");
            loStringBuilder.AppendLine(@";0003 Arguments for the executable");
            loStringBuilder.AppendLine(@";0004 Filenames of the TRANSFORMS for an .MSI (if you have more than one file you have to separate it via ';')");
            loStringBuilder.AppendLine(@";00041 Filenames of patches for an .MSI (if you have more than one file you have to separate it via ';')");
            loStringBuilder.AppendLine(@";0005 User Interface Mode for an .MSI. Please refer to the Windows Installer SDK under ""Command Line Options""");
            loStringBuilder.AppendLine(@";0006 Properties to set for an .MSI via Command Line");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 2");
            loStringBuilder.AppendLine(@";0007 ProductCode (if it is a .MSI)");
            loStringBuilder.AppendLine(@";0008 Product Name");
            loStringBuilder.AppendLine(@";0009 Product Version");
            loStringBuilder.AppendLine(@";0010 Swiss Soft Packaging Factory Oder Number, which you can easily refer to an order made");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 3");
            loStringBuilder.AppendLine(@";0011 ProductCode's of previous .MSI Packages to uninstall before installing the product");
            loStringBuilder.AppendLine(@";0012 Uninstall commands for previous Legacy Installations to uninstall before installing the Product");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 4");
            loStringBuilder.AppendLine(@";0013 Should the Inventory Registry Keys be written via the VBSkript? (Value 'Y' or 'N'). If the Value is set to 'N' the Inventory Keys must be written via the installation routine (normally the .MSI)");
            loStringBuilder.AppendLine(@";0014 Root Key for the Inventory Keys to be written");
            loStringBuilder.AppendLine(@";0015 Key for Inventory in the Registry under the choosen Root Key");
            loStringBuilder.AppendLine(@";0016 expectet Return Codes by success of the installation");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 5");
            loStringBuilder.AppendLine(@";0017 Path to the Logfile");
            loStringBuilder.AppendLine(@";0018 Logging Options for an .MSI. Please refer to the Windows Installer SDK under ""Command Line Options""");
            loStringBuilder.AppendLine(@";0019 Prefix for the installation logfile");
            loStringBuilder.AppendLine(@";0020 Prefix for the deinstallation logfile");
            loStringBuilder.AppendLine(@";0021 Name of the Logfile");
            loStringBuilder.AppendLine(@";0022 Suffix for the installation logfile");
            loStringBuilder.AppendLine(@";0023 Suffix for the deinstallation logfile");
            loStringBuilder.AppendLine(@";0024 Extension for the logfile");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 6");
            loStringBuilder.AppendLine(@";0025 Header of Message that the spf_Setup.ini exists");
            loStringBuilder.AppendLine(@";0026 Header of Message that the installation was aborted");
            loStringBuilder.AppendLine(@";0027 Header of Message that the Registry Key defined in the .ini already exists");
            loStringBuilder.AppendLine(@";0028 Header of Message that the spf_Setup.ini does not exist");
            loStringBuilder.AppendLine(@";0029 Header of Message that the Registry Key defined in the .ini does not exist");
            loStringBuilder.AppendLine(@";0030 Header of Message that the installation failed");
            loStringBuilder.AppendLine(@";0031 Header of Message that a running process was detected an the user cancelled the installation");
            loStringBuilder.AppendLine(@";0032 Header of Message that it was successfully uninstalled");
            loStringBuilder.AppendLine(@";0033 Header of Message that it was successfully installed");
            loStringBuilder.AppendLine(@";0034 Header of Message that it was multiple times tried to install and it failed");
            loStringBuilder.AppendLine(@";0035 Header of Message with the ErrorCode");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 1 ------------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"[FileExecution]");
            loStringBuilder.AppendLine(@";0001");
            loStringBuilder.AppendLine(@"IsFileAnMSI=Y");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0002");
            loStringBuilder.AppendLine(@"File2Execute=" + lsMSI);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0003");
            loStringBuilder.AppendLine(@"SetupArguments=");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0004");
            loStringBuilder.AppendLine(@"TRANSFORMS=" + lsMST);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";00041");
            loStringBuilder.AppendLine(@"PATCH=");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0005");
            loStringBuilder.AppendLine(@"UIMode=/qb-!");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0006");
            loStringBuilder.AppendLine(@"Properties=");
            loStringBuilder.AppendLine(@";End Section 1 --------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 2 ------------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"[Product]");
            loStringBuilder.AppendLine(@";0007");
            loStringBuilder.AppendLine(@"ProductCode=" + lsUninstallKey);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0008");
            loStringBuilder.AppendLine(@"ProductName=" + lsManufacturer + " " + lsProduct + " " + lsVersion);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0009");
            loStringBuilder.AppendLine(@"ProductVersion=" + lsReleaseName);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0010");
            loStringBuilder.AppendLine(@"SSPF_Puid=");
            loStringBuilder.AppendLine(@";End Section 2 --------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 3 ------------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"[UNINSTALL]");
            loStringBuilder.AppendLine(@";0011");
            //loStringBuilder.AppendLine(@"OldProductCodes={26A24AE4-039D-4CA4-87B4-2F83218065F0}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0012");
            loStringBuilder.AppendLine(@"UninstallCommands=");
            loStringBuilder.AppendLine(@";End Section 3 --------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 4 ------------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"[Global]");
            loStringBuilder.AppendLine(@";0013");
            loStringBuilder.AppendLine(@"WriteInventoryKeys=N");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0014");
            loStringBuilder.AppendLine(@"InventoryKeyRoot=HKEY_LOCAL_MACHINE");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0015");
            loStringBuilder.AppendLine(@"InventoryKeys=Software\Clearbyte\InstalledSoftware\");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0016");
            loStringBuilder.AppendLine(@"SuccessCodes=0;3010");
            loStringBuilder.AppendLine(@";End Section 4 --------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 5 ------------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"[Logging]");
            loStringBuilder.AppendLine(@";0017");
            loStringBuilder.AppendLine(@"Path=C:\Windows\Maintenance\logfiles\Applications\");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0018");
            loStringBuilder.AppendLine(@"Type=*v");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0019");
            loStringBuilder.AppendLine(@"InstallLogPrefix=");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0020");
            loStringBuilder.AppendLine(@"UninstLogPrefix=");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0021");
            loStringBuilder.AppendLine(@"LogFileName=" + (lsManufacturer + " " + lsProduct + " " + lsVersion).Replace(" ", "_"));
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0022");
            loStringBuilder.AppendLine(@"InstallLogSuffix=_Inst");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0023");
            loStringBuilder.AppendLine(@"UninstLogSuffix=_Rem");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0024");
            loStringBuilder.AppendLine(@"LogExtension=.log");
            loStringBuilder.AppendLine(@";End Section 5 --------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";Section 6 ------------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"[LogEventMessages]");
            loStringBuilder.AppendLine(@";0025");
            loStringBuilder.AppendLine(@"LogEventMsg1=""; file exists (spf_Setup.ini): """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0026");
            loStringBuilder.AppendLine(@"LogEventMsg2=""; installation aborted!""");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0027");
            loStringBuilder.AppendLine(@"LogEventMsg3=""; regkey exists (spf_Setup.ini): """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0028");
            loStringBuilder.AppendLine(@"LogEventMsg4=""; file does NOT exist (spf_Setup.ini): """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0029");
            loStringBuilder.AppendLine(@"LogEventMsg5=""; regkey does NOT exist (spf_Setup.ini): """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0030");
            loStringBuilder.AppendLine(@"LogEventMsg6=""; Failed to install """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0031");
            loStringBuilder.AppendLine(@"LogEventMsg7=""; Running Processes detected (User cancelled) !""");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0032");
            loStringBuilder.AppendLine(@"LogEventMsg8=""; Successfully uninstalled """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0033");
            loStringBuilder.AppendLine(@"LogEventMsg9=""; Successfully installed """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0034");
            loStringBuilder.AppendLine(@"LogEventMsg10=""; Failed multiple times to install """);
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@";0035");
            loStringBuilder.AppendLine(@"LogEventMsg11=""; ErrorCode: """);
            loStringBuilder.AppendLine(@";End Section 6 --------------------------------------------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");


            //loStringBuilder.AppendLine(@"[general]");");
            //loStringBuilder.AppendLine(@"LogPath=C:\Windows\Maintenance\logfiles\Applications");");
            //loStringBuilder.AppendLine(@"WriteInventoryKeys=0");");
            //loStringBuilder.AppendLine(@"InventoryKeys=Software\clearByte\InstalledSoftware\");");
            //loStringBuilder.AppendLine(@"DefaultUIMode=/qb-!");");
            //loStringBuilder.AppendLine(@"");
            //loStringBuilder.AppendLine(@"[application]");
            //loStringBuilder.AppendLine(@"ProductName=" + lsProduct);
            //loStringBuilder.AppendLine(@"ProductVersion=" + lsVersion);
            //loStringBuilder.AppendLine(@"ProductCode=" + lsUninstallKey);
            //loStringBuilder.AppendLine(@"PackageName=" + lsManufacturer + " " + lsProduct + " " + lsVersion);
            //loStringBuilder.AppendLine(@"SpfPuid=");
            //loStringBuilder.AppendLine(@"");
            //loStringBuilder.AppendLine(@"[Action]");
            //loStringBuilder.AppendLine(@"-INSTALL -----------------");
            //loStringBuilder.AppendLine(@"RunExe::deliveries_setup.exe|#|TYPE=I|#|i");
            //loStringBuilder.AppendLine(@"-UNINSTALL ---------------");
            //loStringBuilder.AppendLine(@"RunExe::deliveries_setup.exe|#|TYPE=X|#|x");
            //                loStringBuilder.AppendLine(@"_RunExe::notepad.exe|#||#|i");
            //                loStringBuilder.AppendLine(@"_UninstSingleMsi::{07EEE598-5F21-4B57-B40B-46592625B3D9}|#|meinlogfile.log|#|i");
            //                loStringBuilder.AppendLine(@"_InstMsi::meinmsi.msi|#|meintransform.mst,deintransform.mst|#|ALLUSERS=1 ROOTRDIVE=D:\|#|meinlogfile.log|#|i");
            //                loStringBuilder.AppendLine(@"_UninstMultipleMsi::{07EEE598-5F21-4B57-B40B-46592625B3D9}|#|{09782D89-1CA6-4B7D-82C5-2DE01AF5601B}|#|{0ADF605D-2D94-4467-91F7-D75C71CF328D}|#|i");
            //                loStringBuilder.AppendLine(@"_RunMsp::meinmsp.msp|#|meinlogfile.log|#|i");
            //                loStringBuilder.AppendLine(@"_RegAdd::HKLM|#|Software\MyReg\blup|#|MeinEintrag1|#|01,a2,10|#|Binary|#|64|#|i");
            //                loStringBuilder.AppendLine(@"_RegAdd::HKCU|#|Software\MyReg\blup|#|MeinEintrag2|#|%LOGONSERVER%|#|Regsz|#|64|#|i");
            //                loStringBuilder.AppendLine(@"-UNINSTALL ---------------");
            //                loStringBuilder.AppendLine(@"_RegAdd::HKCU|#|Software\MyReg\blup|#|MeinEintrag3|#|%ALLUSERSPROFILE%|#|Regsz|#|64|#|i");
            //                loStringBuilder.AppendLine(@"_RegDelKey::HKCU|#|Software\MyReg\blup|#|64|#|x");
            //                loStringBuilder.AppendLine(@"_RegDelValue::HKCU|#|Software\MyReg\blup|#|MeinEintrag3|#|64|#|i");
            //                loStringBuilder.AppendLine(@"_FileDel::c:\temp|#|mbusslinger1.png|#|i");
            //                loStringBuilder.AppendLine(@"_FileCopyMove::E:\|#|mbusslinger.png|#|c:\temp\mbusslinger1.png|#|0|#|i");
            //                loStringBuilder.AppendLine(@"_FolderCreate::E:\test\blup\aaa\bbb\ccc|#|1|#|i");
            //                loStringBuilder.AppendLine(@"_FolderDel::E:\test\todelete|#|0|#|i");
            //                loStringBuilder.AppendLine(@"_ServiceHandling::FoxitCloudUpdateService|#|0|#|i");
            //                loStringBuilder.AppendLine(@"_KillProcess::winword.exe|#|i");
            //                loStringBuilder.AppendLine(@"_FolderCreate::%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\meineFirma\clearByte|#|1|#|i");
            //                loStringBuilder.AppendLine(@"_CreateShortcut::meineFirma\clearByte|#|CB App.lnk|#|/abc|#|Die super App|#|3|#|%windir%\notepad.exe|#|%windir%\notepad.exe|#||#|i");
            //                loStringBuilder.AppendLine(@"_SetFileAttributes::C:\repoFolder\wrapper_script\Branches\wrapper_script-1.1\tst.ini|#|r|#|-|#|i");

            return loStringBuilder.ToString();
        }

        public static string CreateCustomScripts02(string lsDeployXML, string lsManufacturer, string lsProduct, string lsVersion, string lsReleaseName, string lsUninstallKey, string lsLanguages, string lsArchitectures, string lsDescription)
        {
            StringBuilder loStringBuilder = new StringBuilder();


            loStringBuilder.AppendLine(@"<?xml version=""1.0"" standalone=""yes""?>");
            loStringBuilder.AppendLine(@"<metainfo>");
            loStringBuilder.AppendLine(@"  <software>");
            loStringBuilder.AppendLine(@"    <Product>" + lsProduct + @"</Product>");
            loStringBuilder.AppendLine(@"    <Manufacturer>" + lsManufacturer + @"</Manufacturer>");
            loStringBuilder.AppendLine(@"    <Version>" + lsVersion + @"</Version>");
            loStringBuilder.AppendLine(@"    <Release>" + lsReleaseName + "</Release>");
            loStringBuilder.AppendLine(@"    <AssetId>" + Guid.NewGuid().ToString() + "</AssetId>");
            loStringBuilder.AppendLine(@"    <InternalVersion>" + lsReleaseName + "</InternalVersion>");
            loStringBuilder.AppendLine(@"    <Languages>de;fr</Languages>");
            loStringBuilder.AppendLine(@"    <Architectures>x86</Architectures>");
            loStringBuilder.AppendLine(@"    <Description>" + Regex.Replace(lsDescription, "<.*?>", String.Empty) + @"</Description>");
            loStringBuilder.AppendLine(@"    <CommandInstall>deliveries_setup.exe type=i</CommandInstall>");
            loStringBuilder.AppendLine(@"    <CommandUninstall>deliveries_setup.exe type=x</CommandUninstall>");
            loStringBuilder.AppendLine(@"    <CommandRepair>deliveries_setup.exe type=r</CommandRepair>");
            loStringBuilder.AppendLine(@"    <DetectionKey>" + lsUninstallKey + @"</DetectionKey>");
            loStringBuilder.AppendLine(@"    <SuccessCodes>0;3010</SuccessCodes>");
            loStringBuilder.AppendLine(@"    <RetryCodes>1066</RetryCodes>");
            loStringBuilder.AppendLine(@"  </software>");
            loStringBuilder.AppendLine(@"</metainfo>");


            return loStringBuilder.ToString();

        }

        public static string CreateCustomScripts01(string lsDeployXML, string lsManufacturer, string lsProduct, string lsVersion, string lsReleaseName, string lsUninstallKey, string lsArchitectures, DataTable loParameterTable)
        {




            StringBuilder loStringBuilder = new StringBuilder();

            loStringBuilder.AppendLine(@"# * SIX Group");
            loStringBuilder.AppendLine(@"# *");
            loStringBuilder.AppendLine(@"# * " + lsManufacturer + " " + lsProduct + " " + lsVersion + ".x");
            loStringBuilder.AppendLine(@"# *");
            loStringBuilder.AppendLine(@"# *		");
            loStringBuilder.AppendLine(@"# * Author:			Package Shop");
            loStringBuilder.AppendLine(@"# * Creation date:	" + DateTime.Now.ToString("yyyy-MM-dd"));
            loStringBuilder.AppendLine(@"# * Changed by:		clearByte Gmbh Christian Wegmann");
            loStringBuilder.AppendLine(@"# * Change date:	" + DateTime.Now.ToString("yyyy-MM-dd"));
            loStringBuilder.AppendLine(@"# * Change reason:	unknown");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * Usage:");
            loStringBuilder.AppendLine(@"# * _SIX_Setup_Package.ps1 { install | remove | check | reinstall* | repair* } [-f]* [-l LogFile]*");
            loStringBuilder.AppendLine(@"# * _SIX_Setup_Package.ps1 { info | summary* | help }");
            loStringBuilder.AppendLine(@"# * _SIX_Setup_Package.ps1 exportxml Output-Filename");
            loStringBuilder.AppendLine(@"# * (* not implemented yet)");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"$VERSION = '0.20120808+.0' # version of this script");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * package settings:");
            loStringBuilder.AppendLine(@"function Get-PackageConfiguration {");
            loStringBuilder.AppendLine(@"if ($global:ExternalConfiguration.Package) { $global:Package = $global:ExternalConfiguration.Package } else {");
            loStringBuilder.AppendLine(@"$global:Package = @{");
            loStringBuilder.AppendLine(@"	Vendor =		""" + lsManufacturer.Replace(" ", "") + @""";			# e.g. ""Adobe"" (mandatory)");
            loStringBuilder.AppendLine(@"	Product =		""" + lsProduct.Replace(" ", "") + @""";				# e.g. ""Reader"" (mandatory)");
            loStringBuilder.AppendLine(@"	Version =		""" + lsReleaseName + @""";		# e.g. ""10"" (mandatory)");
            loStringBuilder.AppendLine(@"	#Architecture =	""" + lsArchitectures + @""";	# ""x86"" or ""x64"" (optional)");
            loStringBuilder.AppendLine(@"	Release =		""001"";				# e.g. ""001"" (mandatory)");
            loStringBuilder.AppendLine(@"	#LogFile =		""<LogFile>"";		# alternative LogFile - this will override $OS[""LogFile""];");
            loStringBuilder.AppendLine(@"										#  but parameter -L will override this again (optional)");
            loStringBuilder.AppendLine(@"	#IDOverride =	""<SpecialID>"";		# overrides the autogenerated ID (Vendor_Product_Version[_Architecture]_Release)");
            loStringBuilder.AppendLine(@"										#  (optional)");
            loStringBuilder.AppendLine(@"	#Reboot = 		$true				# Enforce reboot after installation process");
            loStringBuilder.AppendLine(@"										#  (in real this script does never do a reboot -");
            loStringBuilder.AppendLine(@"										#   instead it returns the errorcode 3010 at its end)");
            loStringBuilder.AppendLine(@"}}}");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * organization settings:");
            loStringBuilder.AppendLine(@"function Get-OrganizationConfiguration {");
            loStringBuilder.AppendLine(@"if ($global:ExternalConfiguration.Organization) { $global:Organization = $global:ExternalConfiguration.Organization } else {");
            loStringBuilder.AppendLine(@"$global:Organization = @{");
            loStringBuilder.AppendLine(@"	Name = 'SIX';							# the name of your organization (mandatory)");
            loStringBuilder.AppendLine(@"	Mail = '4040EWPhotline@six-group.com';	# servicedesk/hotline email adress of your organization (mandatory for ARP)");
            loStringBuilder.AppendLine(@"	ServiceTelephone = '4040';				# servicedesk/hotline telephon number of your organization (mandatory for ARP)");
            loStringBuilder.AppendLine(@"	Tag = 'SIX';							# a short tag of your organization name (mandatory)");
            loStringBuilder.AppendLine(@"}}}");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * operating system presets");
            loStringBuilder.AppendLine(@"function Get-OSConfiguration {");
            loStringBuilder.AppendLine(@"if ($global:ExternalConfiguration.OS) { $global:OS = $global:ExternalConfiguration.OS } else {");
            loStringBuilder.AppendLine(@"$global:OS = @{");
            loStringBuilder.AppendLine(@"	LogFile = ""${env:LocalLogPath}\"" + $Package[""ID""] + "".log""	# global definition for LogFile; but parameter -L will override this");
            loStringBuilder.AppendLine(@"	MSIExecDefaultParameters = @(""/qn"", ""REBOOT=ReallySuppress"", ""ALLUSERS=1"")	# default arguments for msiexec");
            loStringBuilder.AppendLine(@"}}}");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * setup definition");
            loStringBuilder.AppendLine(@"function Get-SetupJobs {");
            loStringBuilder.AppendLine(@"if ($global:ExternalConfiguration.Setup) { $global:Setup = $global:ExternalConfiguration.Setup } else {");
            loStringBuilder.AppendLine(@"$Setup = @(); # do not touch this!");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");

            DataTable loTable = ConvertXMLToDataTable("install", lsDeployXML, false);
            DataTable loUninstallTable = ConvertXMLToDataTable("uninstall", lsDeployXML, false);


            loStringBuilder.AppendLine(@"# CaptureShortcuts");
            loStringBuilder.AppendLine(@"$Setup += @{");
            loStringBuilder.AppendLine(@"	Type = ""exe"";");
            loStringBuilder.AppendLine(@"	Name = ""MoveShortcuts.exe"";");
            loStringBuilder.AppendLine(@"	Parameters = @(");
            loStringBuilder.AppendLine(@"	""action=capture"");");
            loStringBuilder.AppendLine(@"}");

            GetMyPowershellScript(ref loStringBuilder, ref loTable, "install", lsUninstallKey);

            loStringBuilder.AppendLine(@"# MoveShortcuts");
            loStringBuilder.AppendLine(@"$StartMenuPath=""${env:ProgramData}\Microsoft\Windows\Start Menu\Programs\Applications""");
            loStringBuilder.AppendLine(@"$Setup += @{");
            loStringBuilder.AppendLine(@"	Type = ""exe"";");
            loStringBuilder.AppendLine(@"	Name = ""MoveShortcuts.exe"";");
            loStringBuilder.AppendLine(@"	Parameters = @(");
            loStringBuilder.AppendLine(@"	'action=moveshortcuts target=""' + $StartMenuPath + '""');");
            loStringBuilder.AppendLine(@"}");

            loStringBuilder.AppendLine(@"# RemoveShortcuts");
            loStringBuilder.AppendLine(@"$Setup += @{");
            loStringBuilder.AppendLine(@"	Type = ""exe"";");
            loStringBuilder.AppendLine(@"	UninstallName = ""MoveShortcuts.exe"";");
            loStringBuilder.AppendLine(@"	UninstallParameters = @(");
            loStringBuilder.AppendLine(@"	""action=cleanup"");");
            loStringBuilder.AppendLine(@"}");

            GetMyPowershellScript(ref loStringBuilder, ref loUninstallTable, "uninstall", lsUninstallKey);

            //if (1 == 2)
            //{
            //    loStringBuilder.AppendLine(@"# Reste abraeumen");
            //    loStringBuilder.AppendLine(@"#Cleanup");
            //    loStringBuilder.AppendLine(@"$Setup += @{");
            //    loStringBuilder.AppendLine(@"	Type = ""ps"";");
            //    loStringBuilder.AppendLine(@"	UninstallName = ""Remove-Item"";");
            //    loStringBuilder.AppendLine(@"	UninstallParameters = @(");
            //    loStringBuilder.AppendLine(@"		""-Path"",	(""`"""" + $CommonDirs[""ProgramFiles32""] + ""\CBK_Client`""""),");
            //    loStringBuilder.AppendLine(@"		""-Force"",");
            //    loStringBuilder.AppendLine(@"		""-Recurse""");
            //    loStringBuilder.AppendLine(@"	);");
            //    loStringBuilder.AppendLine(@"	UninstallAcceptExitCodes = @(1);");
            //    loStringBuilder.AppendLine(@"}");
            //    loStringBuilder.AppendLine(@"");
            //}


            //if (1 == 2)
            //{
            //    loStringBuilder.AppendLine(@"# Software installieren");
            //    loStringBuilder.AppendLine(@"$Setup += @{");
            //    loStringBuilder.AppendLine(@"	Type = ""msi"";");
            //    loStringBuilder.AppendLine(@"	Name = ""setup.msi"";");
            //    loStringBuilder.AppendLine(@"	GUID = """ + lsUninstallKey + @""";");
            //    loStringBuilder.AppendLine(@"");
            //    loStringBuilder.AppendLine(@"}");
            //}

            //loStringBuilder.AppendLine(@"");
            //if (1 == 2)
            //{
            //    loStringBuilder.AppendLine(@"# Berechtigungen setzen");
            //    loStringBuilder.AppendLine(@"$Setup += @{");
            //    loStringBuilder.AppendLine(@"	Type = ""exe"";");
            //    loStringBuilder.AppendLine(@"	PATH = ""C:\Support\Tools""");
            //    loStringBuilder.AppendLine(@"	Name = ""SubInacl.exe"";");
            //    loStringBuilder.AppendLine(@"	Parameters = @(");
            //    loStringBuilder.AppendLine(@"	""/subdirectories"", (""`"""" + $CommonDirs[""ProgramFiles32""] + ""\CBK_Client`""""),");
            //    loStringBuilder.AppendLine(@"        ""/Grant=Users=C""");
            //    loStringBuilder.AppendLine(@"    );");
            //    loStringBuilder.AppendLine(@"	");
            //    loStringBuilder.AppendLine(@"	WindowStyle = ""Hidden"";");
            //    loStringBuilder.AppendLine(@"}");
            //    loStringBuilder.AppendLine(@"");
            //}
            //if (1 == 2)
            //{
            //    loStringBuilder.AppendLine(@"$Setup += @{");
            //    loStringBuilder.AppendLine(@"	Type = ""exe"";");
            //    loStringBuilder.AppendLine(@"	PATH = ""C:\Support\Tools""");
            //    loStringBuilder.AppendLine(@"	Name = ""SubInacl.exe"";");
            //    loStringBuilder.AppendLine(@"	Parameters = @(");
            //    loStringBuilder.AppendLine(@"	""/subdirectories"", (""`"""" + $CommonDirs[""ProgramFiles32""] + ""\CBK_Client\*.*`""""),");
            //    loStringBuilder.AppendLine(@"        ""/Grant=Users=C""");
            //    loStringBuilder.AppendLine(@"    );");
            //    loStringBuilder.AppendLine(@"	");
            //    loStringBuilder.AppendLine(@"	WindowStyle = ""Hidden"";");
            //    loStringBuilder.AppendLine(@"}");
            //    loStringBuilder.AppendLine(@"");
            //}


            loStringBuilder.AppendLine(@"# Last action: register installation:");
            loStringBuilder.AppendLine(@"$Setup += @{");
            loStringBuilder.AppendLine(@"	Type = ""reg"";");
            loStringBuilder.AppendLine(@"	Path = ""HKLM:\Software\_SW-Delivery\"" + $Package[""ID""];");
            loStringBuilder.AppendLine(@"	Value = ""InstallDate"";");
            loStringBuilder.AppendLine(@"	Data = (get-date -Format ""yyyy-MM-dd HH:mm:ss zz"").ToString();");
            loStringBuilder.AppendLine(@"	ValueType = ""String"";");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# --------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"# * Examples and documentation for setup definition");
            loStringBuilder.AppendLine(@"# * please only copy from examples instead of modifying them - so later revisitors have a chance to get them...");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * single setup block with all possible options:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"# * generic:");
            loStringBuilder.AppendLine(@"#	Type = ""<TYPE>"";				# type ""msi"", ""msp"", ""msu"", ""exe"", ""ps"" or ""reg"" (mandatory)");
            loStringBuilder.AppendLine(@"#	Architecture = ""<Architecture>"";# defines the architecture for which only this block have to proceed");
            loStringBuilder.AppendLine(@"#									#  (optional: Standard is unset, valid values: x86, x64)");
            loStringBuilder.AppendLine(@"#	Features = @(""Feature1"", ...);	# list of msi-features to be installed/removed (only msi, optional)");
            loStringBuilder.AppendLine(@"#	GUID = ""<GUID>"";				# msi-ProductCode (only msi, mandatory)");
            loStringBuilder.AppendLine(@"#	LogFile = ""${env:LocalLogPath}\"" + $Package[""ID""] + "".setup.log"";");
            loStringBuilder.AppendLine(@"#									# log file for directly supported setup types (msi/msp/msu, optional)");
            loStringBuilder.AppendLine(@"#	Properties = @(""PROP1=x"", ...);	# list of additional msi-properties (only msi, optional)");
            loStringBuilder.AppendLine(@"#	WindowStyle = ""<Style>"";		# state of the windows used for the process");
            loStringBuilder.AppendLine(@"#									# (only msi/msp/exe, optional: Standard ""Normal"",");
            loStringBuilder.AppendLine(@"#									#  valid values: Normal, Hidden, Minimized, Maximized)");
            loStringBuilder.AppendLine(@"#");
            loStringBuilder.AppendLine(@"# * installation:");
            loStringBuilder.AppendLine(@"#	Name = ""<SETUP>"";				# msi, msp, msu, executable or ps-command");
            loStringBuilder.AppendLine(@"#									#  (only msi/msp/msu/exe/ps, optional: installation action will be skipped if not set)");
            loStringBuilder.AppendLine(@"#	Parameters = @(""<PAR1>"", ...);	# parameters for executable or ps-command (only exe/ps, optional)");
            loStringBuilder.AppendLine(@"#	Path = ""<PATH>"";				# installation source path (only msi/msp/exe, optional: Standard $SourcePath) or");
            loStringBuilder.AppendLine(@"#									#  path to registry key (only reg, mandatory)");
            loStringBuilder.AppendLine(@"#	Transforms = ""<MST>"";			# msi-transforms file relative to Path (only msi/msp, optional)");
            loStringBuilder.AppendLine(@"#	AcceptExitCodes = @(0, 5, ...);	# list of accepted return codes at installation (optional)");
            loStringBuilder.AppendLine(@"#");
            loStringBuilder.AppendLine(@"# * uninstallation:");
            loStringBuilder.AppendLine(@"#	UninstallName = ""<SETUP>"";					# executable or ps-command");
            loStringBuilder.AppendLine(@"#												#  (only exe/ps, optional: uninstallation action will be skipped if not set)");
            loStringBuilder.AppendLine(@"#	UninstallParameters = @(""<PAR1>"", ...);		# parameters to executable or ps-command (only exe/ps, optional)");
            loStringBuilder.AppendLine(@"#	UninstallPath = ""<PATH>"";					# uninstallation source path (only exe, optional: Standard $SourcePath)");
            loStringBuilder.AppendLine(@"#	UninstallAcceptExitCodes = @(0, 5, ...);	# list of accepted return codes at uninstallation (optional)");
            loStringBuilder.AppendLine(@"#");
            loStringBuilder.AppendLine(@"# * ARP:");
            loStringBuilder.AppendLine(@"#	ARPID = ""<Uninstall-ID in Registry>"";	# key name of registry uninstall entry");
            loStringBuilder.AppendLine(@"#											#  (optional; but mandatory if any ARP setting is used)");
            loStringBuilder.AppendLine(@"#	ARPWow6432Node = $false;				# ARP entry below Wow6432Node? (optional, default $false, only on x64 OS)");
            loStringBuilder.AppendLine(@"#	ARPName = [String]$Package[""Product"",""Version"",""Architecture""];		# ProductName (optional)");
            loStringBuilder.AppendLine(@"#	ARPPublisher = $Package[""Vendor""];		# ProductVendor/ProductPublisher (optional)");
            loStringBuilder.AppendLine(@"#	ARPVersion = $Package[""Version""];		# ProductVersion (optional)");
            loStringBuilder.AppendLine(@"#	ARPIcon = ""${env:ProgramFiles}\Vendor\Product\program.exe,0"";	# ARPPRODUCTICON (optional)");
            loStringBuilder.AppendLine(@"#	ARPUninstallString = ""${env:ProgramFiles}\V\P\uninstall.exe"";	# sets the uninstall command;");
            loStringBuilder.AppendLine(@"#											# if its neither given nor existent in registry, a messagebox command will be");
            loStringBuilder.AppendLine(@"#											# placed and all ARPNo* will be set to true (optional)");
            loStringBuilder.AppendLine(@"#	ARPContact = ""IT-Servicedesk"";			# ARPCONTACT (optional)");
            loStringBuilder.AppendLine(@"#	ARPComments = ""Repackaging/Customization for "" + $Organization[""Name""];		# ARPCOMMENTS (optional)");
            loStringBuilder.AppendLine(@"#	ARPHelpLink = ""mailto:"" + $Organization[""Mail""];		# ARPHELPLINK (optional)");
            loStringBuilder.AppendLine(@"#	ARPHelpTelephone = $Organization[""ServiceTelephone""];	# ARPHELPTELEPHONE (optional)");
            loStringBuilder.AppendLine(@"#	ARPNoModify = $true;					# ARPNOMODIFY (optional)");
            loStringBuilder.AppendLine(@"#	ARPNoRemove = $false;					# ARPNOREMOVE (optional)");
            loStringBuilder.AppendLine(@"#	ARPNoRepair = $true;					# ARPNOREPAIR (optional)");
            loStringBuilder.AppendLine(@"#	ARPSystemComponent = $false;			# ARPSYSTEMCOMPONENT (optional)");
            loStringBuilder.AppendLine(@"#	RemoveARPURLUpdateInfo = $true;			# remove ARPURLUPDATEINFO  (optional)");
            loStringBuilder.AppendLine(@"#	UninstallARP = $false;					# remove whole ARP key at uninstallation (optional)");
            loStringBuilder.AppendLine(@"#");
            loStringBuilder.AppendLine(@"# * registry:");
            loStringBuilder.AppendLine(@"#	Value = ""ProductName"";		# registry value (only reg, mandatory)");
            loStringBuilder.AppendLine(@"#	Data = [String]$Package[""Product"",""Version"",""Architecture""];	# registry data (only reg, mandatory)");
            loStringBuilder.AppendLine(@"#	ValueType = ""String"";		# registry data type (only reg, optional: Standard ""String"",");
            loStringBuilder.AppendLine(@"#								#  valid values: String, ExpandString, DWord, MultiString, QWord, Binary)");
            loStringBuilder.AppendLine(@"#");
            loStringBuilder.AppendLine(@"# * check installation:");
            loStringBuilder.AppendLine(@"#	CheckType = ""<Type>""		# type of check: ""file"", ""registry"", ""uninstall"", or ""execute"" (optional)");
            loStringBuilder.AppendLine(@"#								#  description:");
            loStringBuilder.AppendLine(@"#								#   execute: test a exitcode of an script or binary");
            loStringBuilder.AppendLine(@"#								#   file: test a file existence, size, version or date");
            loStringBuilder.AppendLine(@"#								#   registry: test a registry key/value existence or data");
            loStringBuilder.AppendLine(@"#								#   uninstall: test a ARP entry existence or version");
            loStringBuilder.AppendLine(@"#	CheckCondition = ""<COND>""	# condition for check (mandatory if CheckType is set)");
            loStringBuilder.AppendLine(@"#								#  description:");
            loStringBuilder.AppendLine(@"#								#   equals: equals CheckPaths data with CheckValue? (only for registry)");
            loStringBuilder.AppendLine(@"#								#   exitcode_{eq|lt|le|gt|ge}: is exitcode eq/lt/le/gt/ge CheckValue? (only for execute)");
            loStringBuilder.AppendLine(@"#								#   {c|m|a}date_{eq|lt|le|gt|ge}: is files creation/modification/access date eq/lt/le/gt/ge CheckValue? (only for file)");
            loStringBuilder.AppendLine(@"#								#   exists: does file/registry key/registry value/ARP entry in CheckPath exists? (only for file, registry and uninstall)");
            loStringBuilder.AppendLine(@"#								#   size_{eq|lt|le|gt|ge}: is file size eq/lt/le/gt/ge CheckValue? (only for file)");
            loStringBuilder.AppendLine(@"#								#   version_{eq|lt|le|gt|ge}: is file version/ARP entry version eq/lt/le/gt/ge CheckValue? (only for file and uninstall)");
            loStringBuilder.AppendLine(@"#	CheckPath = ""<OBJECT>""		# object of the test: file path, registry key or value, executable path or ARP DisplayName");
            loStringBuilder.AppendLine(@"#	CheckValue = ""<DATA>""		# data to test for: registry data, exitcode, date in ISO8601, size in bytes or version number");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * MSI example with TRANSFORMS:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""msi"";");
            loStringBuilder.AppendLine(@"#	Name = ""setupA.msi"";");
            loStringBuilder.AppendLine(@"#	Transforms = ""setupA.mst"";");
            loStringBuilder.AppendLine(@"#	GUID = ""{11111111-1111-1111-1111-111111111111}"";");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * MSI example without TRANSFORMS:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""msi"";");
            loStringBuilder.AppendLine(@"#	Name = ""setupB.msi"";");
            loStringBuilder.AppendLine(@"#	GUID = ""{00000000-0000-0000-0000-000000000000}"";");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * MSP example:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""msp"";");
            loStringBuilder.AppendLine(@"#	Name = ""update.msp"";");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * MSU example:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""msu"";");
            loStringBuilder.AppendLine(@"#	Name = ""update.msu"";");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * Executable setup example with uninstallation from source directory:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""exe"";");
            loStringBuilder.AppendLine(@"#	Name = ""setupC.exe"";");
            loStringBuilder.AppendLine(@"#	Parameters = @(""/SILENT"");");
            loStringBuilder.AppendLine(@"#	UninstallName = ""setupC.exe"";");
            loStringBuilder.AppendLine(@"#	UninstallParameters = @(""/SILENT"", ""/REMOVE"");");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * Executable setup example with uninstallation from target directory:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""exe"";");
            loStringBuilder.AppendLine(@"#	Name = ""setupD.exe"";");
            loStringBuilder.AppendLine(@"#	Parameters = @(""/S"", ""/Q"");");
            loStringBuilder.AppendLine(@"#	UninstallName = ""uninstall.exe"";");
            loStringBuilder.AppendLine(@"#	UninstallParameters = @(""/S"", ""/Q"", ""/R"");");
            loStringBuilder.AppendLine(@"#	UninstallPath = ""${env:ProgramFiles(x86)}\Vendor\Product"";");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * PowerShell example:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""ps"";");
            loStringBuilder.AppendLine(@"#	Name = ""Copy-Item"";");
            loStringBuilder.AppendLine(@"#	Parameters = @(");
            loStringBuilder.AppendLine(@"#		""-Path"",		""`""${SourcePath}\CustomConfiguration`"""",");
            loStringBuilder.AppendLine(@"#		""-Destination"",	""`""${env:ProgramData}\Vendor\Product\Configuration`"""",");
            loStringBuilder.AppendLine(@"#		""-Force"",");
            loStringBuilder.AppendLine(@"#		""-Recurse""");
            loStringBuilder.AppendLine(@"#	);");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * PowerShell example to create shortcut:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""ps"";");
            loStringBuilder.AppendLine(@"#	Name = ""Create-Shortcut"";");
            loStringBuilder.AppendLine(@"#	Parameters = @(");
            loStringBuilder.AppendLine(@"#		""-Path"",				""`"""" + $CommonDirs[""Programs""] + ""\Notepad`"""",	# directory path to the shortcut (mandatory)");
            loStringBuilder.AppendLine(@"#		""-Name"",				""`""Notepad Editor.lnk`"""",						# filename of the shortcut (mandatory)");
            loStringBuilder.AppendLine(@"#		""-Target"",				""`"""" + $CommonDirs[""SystemRoot""] + ""\notepad.exe`"""",	# target of the shortcut (mandatory)");
            loStringBuilder.AppendLine(@"#		""-Arguments"",			""`""N:\my.txt`"""",								# arguments to the target (optional)");
            loStringBuilder.AppendLine(@"#		""-Icon"",				""`"""" + $CommonDirs[""System32_Current""] + ""\shell32.dll,152`"""",	# icon (optional)");
            loStringBuilder.AppendLine(@"#		""-WorkingDirectory"",	""`""N:\`"""",										# working directory of target (optional)");
            loStringBuilder.AppendLine(@"#		""-Description"",			""`""A test shortcut.`"""",							# description (optional)");
            loStringBuilder.AppendLine(@"#		""-HotKey"",				""`""CTRL+ALT+N`""""								# hot key (optional)");
            loStringBuilder.AppendLine(@"#	);");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * PowerShell example to remove empty directory or registry paths:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""ps"";");
            loStringBuilder.AppendLine(@"#	Name = ""Remove-Item-IfEmpty"";");
            loStringBuilder.AppendLine(@"#	Parameters = @(");
            loStringBuilder.AppendLine(@"#		""-Path"", ""`""${env:ProgramData}\Vendor`""""");
            loStringBuilder.AppendLine(@"#	);");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * Registry example:");
            loStringBuilder.AppendLine(@"#$Setup += @{");
            loStringBuilder.AppendLine(@"#	Type = ""reg"";");
            loStringBuilder.AppendLine(@"#	Path = ""HKLM:\Software\Classes\Installer\Products\aaaabbbbccccddddeeeeffff00001111"";");
            loStringBuilder.AppendLine(@"#	Value = ""ProductName"";");
            loStringBuilder.AppendLine(@"#	Data = [String]$Package[""Product"",""Version"",""Architecture""];");
            loStringBuilder.AppendLine(@"#	ValueType = ""String"";");
            loStringBuilder.AppendLine(@"#}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * End of examples.");
            loStringBuilder.AppendLine(@"# --------------------------------------------------------------------------------------------------");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"$global:Setup = $Setup }}	# do not touch this!");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * do not touch anything below!");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"if ($PSVersionTable.PSVersion.Major -lt 2) {");
            loStringBuilder.AppendLine(@"	""ERROR: This Script requires at least version 2.0 of the PowerShell!""");
            loStringBuilder.AppendLine(@"	""For more information and download visit: http://www.microsoft.com/powershell""");
            loStringBuilder.AppendLine(@"	exit 1");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"# * generic functions:");
            loStringBuilder.AppendLine(@"function Create-Shortcut {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Path,");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Name,");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Target,");
            loStringBuilder.AppendLine(@"		[String]$Arguments,");
            loStringBuilder.AppendLine(@"		[String]$Icon,");
            loStringBuilder.AppendLine(@"		[String]$WorkingDirectory,");
            loStringBuilder.AppendLine(@"		[String]$Description,");
            loStringBuilder.AppendLine(@"		[String]$HotKey");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	New-Item -type directory -path $Path -force | Out-Null");
            loStringBuilder.AppendLine(@"	$WShell = New-Object -comObject WScript.Shell");
            loStringBuilder.AppendLine(@"	$Shortcut = $WShell.CreateShortcut(""$Path\$Name"")");
            loStringBuilder.AppendLine(@"	$Shortcut.TargetPath = $Target");
            loStringBuilder.AppendLine(@"	if ($Arguments)			{ $Shortcut.Arguments = $Arguments }");
            loStringBuilder.AppendLine(@"	if ($Icon)				{ $Shortcut.IconLocation = $Icon }");
            loStringBuilder.AppendLine(@"	if ($WorkingDirectory)	{ $Shortcut.WorkingDirectory = $WorkingDirectory }");
            loStringBuilder.AppendLine(@"	if ($Description)		{ $Shortcut.Description = $Description }");
            loStringBuilder.AppendLine(@"	if ($HotKey)			{ $Shortcut.HotKey = $HotKey }");
            loStringBuilder.AppendLine(@"	$Shortcut.Save()");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Get-CommonShellFolder {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Identifier");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	Get-RegistryValue -Path ""hklm:\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"" -Value ""$Identifier""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Get-RegistryValue {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Path,");
            loStringBuilder.AppendLine(@"		[String]$Value");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	if (-not $Value) {");
            loStringBuilder.AppendLine(@"		$Value = split-Path -leaf $Path;");
            loStringBuilder.AppendLine(@"		$Path = split-Path -parent $Path;");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	Get-ItemProperty -Path ""$Path"" -name ""$Value"" -ErrorAction SilentlyContinue `");
            loStringBuilder.AppendLine(@"		| Select-Object -ExpandProperty ""$Value"" -ErrorAction SilentlyContinue");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function hr { ""-"" * ($Host.UI.RawUI.MaxWindowSize.Width - 1) }");
            loStringBuilder.AppendLine(@"function Is-MSI-Installed {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$ProductCode");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	Get-WmiObject -class ""Win32_Product"" | ForEach-Object {");
            loStringBuilder.AppendLine(@"		if ( $_.IdentifyingNumber.ToLower() -eq $ProductCode.ToLower() ) { return $true }");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	return $false");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function title {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Title");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	hr; "">>> "" + $Title + "":""; """"");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Remove-Item-IfEmpty {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Path");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	if (Test-Path ""$Path"") {");
            loStringBuilder.AppendLine(@"		if ( -not (get-childitem ""$Path"" -ErrorAction SilentlyContinue)) { Remove-Item ""$Path"" -Force }");
            loStringBuilder.AppendLine(@"		else { ""Path `""$Path`"" not empty - removing ignored."" }");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	else { ""Path `""$Path`"" not found - removing ignored.""	}");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Remove-RegistryValue {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Path,");
            loStringBuilder.AppendLine(@"		[String]$Value");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	if (-not $Value) {");
            loStringBuilder.AppendLine(@"		$Value = split-Path -leaf $Path;");
            loStringBuilder.AppendLine(@"		$Path = split-Path -parent $Path;");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	Remove-ItemProperty -Path ""$Path"" -Name ""$Value"" `");
            loStringBuilder.AppendLine(@"		-Force -ErrorAction SilentlyContinue | Out-Null");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Set-RegistryValue {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Path,");
            loStringBuilder.AppendLine(@"		[String]$Value,");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Data,");
            loStringBuilder.AppendLine(@"		[String]$Type");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	if (-not $Value) {");
            loStringBuilder.AppendLine(@"		$Value = split-Path -leaf $Path;");
            loStringBuilder.AppendLine(@"		$Path = split-Path -parent $Path;");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	if (-not $Type) { $Type = 'String' }");
            loStringBuilder.AppendLine(@"	if ( -not (Test-Path ""$Path"") ) { New-Item -Path ""$Path"" -type directory -force | Out-Null }");
            loStringBuilder.AppendLine(@"	New-ItemProperty -Path ""$Path"" -Name ""$Value"" -Value ""$Data"" -Type $Type -Force | Out-Null");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Test-Setup {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)]$SetupDefinition");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"	if ( (""msi"", ""msp"" -contains $SetupDefinition.Type.ToLower()) -and $SetupDefinition.GUID ) {");
            loStringBuilder.AppendLine(@"		if ( -not (Is-MSI-Installed -ProductCode $SetupDefinition.GUID) ) { return $false }");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	if ( $SetupDefinition.Name -and $SetupDefinition.CheckType -and $SetupDefinition.CheckCondition -and $SetupDefinition.CheckPath ) {");
            loStringBuilder.AppendLine(@"		switch ( $SetupDefinition.CheckType.ToLower() ) {");
            loStringBuilder.AppendLine(@"			""file"" {");
            loStringBuilder.AppendLine(@"				if ( (Test-Path $SetupDefinition.CheckPath) ) {");
            loStringBuilder.AppendLine(@"					switch -regex ( $SetupDefinition.CheckCondition.ToLower() ) {");
            loStringBuilder.AppendLine(@"						""^exists$"" {}");
            loStringBuilder.AppendLine(@"						""^(c|m|a)date_(eq|lt|le|gt|ge)$"" {");
            loStringBuilder.AppendLine(@"							switch ( $SetupDefinition.CheckCondition.ToLower().Substring(0,1) ) {");
            loStringBuilder.AppendLine(@"								""c"" { $filedate = Get-Date -UFormat %s -Date (Get-Item $SetupDefinition.CheckPath).CreationTime }");
            loStringBuilder.AppendLine(@"								""m"" { $filedate = Get-Date -UFormat %s -Date (Get-Item $SetupDefinition.CheckPath).LastWriteTime }");
            loStringBuilder.AppendLine(@"								""a"" { $filedate = Get-Date -UFormat %s -Date (Get-Item $SetupDefinition.CheckPath).LastAccessTime }");
            loStringBuilder.AppendLine(@"							}");
            loStringBuilder.AppendLine(@"							$condition = [String]$SetupDefinition.CheckCondition.ToLower().Split(""_"")[1]");
            loStringBuilder.AppendLine(@"							$checkvalue = Get-Date -UFormat %s -Date $SetupDefinition.CheckValue");
            loStringBuilder.AppendLine(@"							Invoke-Expression -Command ""if ( -not ($filedate -$condition $checkvalue) ) { return $false }""");
            loStringBuilder.AppendLine(@"						}");
            loStringBuilder.AppendLine(@"						""^size_(eq|lt|le|gt|ge)$"" {");
            loStringBuilder.AppendLine(@"							$filesize = (Get-Item $SetupDefinition.CheckPath).Length");
            loStringBuilder.AppendLine(@"							$condition = [String]$SetupDefinition.CheckCondition.ToLower().Split(""_"")[1]");
            loStringBuilder.AppendLine(@"							$checkvalue = $SetupDefinition.CheckValue");
            loStringBuilder.AppendLine(@"							Invoke-Expression -Command ""if ( -not ($filesize -$condition $checkvalue) ) { return $false }""");
            loStringBuilder.AppendLine(@"						}");
            loStringBuilder.AppendLine(@"						""^version_(eq|lt|le|gt|ge)$"" {");
            loStringBuilder.AppendLine(@"							$fileversion = (Get-Item $SetupDefinition.CheckPath).VersionInfo.FileVersion");
            loStringBuilder.AppendLine(@"							$condition = [String]$SetupDefinition.CheckCondition.ToLower().Split(""_"")[1]");
            loStringBuilder.AppendLine(@"							$checkvalue = $SetupDefinition.CheckValue");
            loStringBuilder.AppendLine(@"							Invoke-Expression -Command ""if ( -not ([Version]$fileversion -$condition [Version]$checkvalue) ) { return $false }""");
            loStringBuilder.AppendLine(@"						}");
            loStringBuilder.AppendLine(@"						default { return 1024 }");
            loStringBuilder.AppendLine(@"					}");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"				else { return $false }");
            loStringBuilder.AppendLine(@"			} # end of switch:file");
            loStringBuilder.AppendLine(@"			""registry"" {");
            loStringBuilder.AppendLine(@"				switch ( $SetupDefinition.CheckCondition.ToLower() ) {");
            loStringBuilder.AppendLine(@"					""exists"" {");
            loStringBuilder.AppendLine(@"						$Path = $SetupDefinition.CheckPath");
            loStringBuilder.AppendLine(@"						if ( -not (Get-ItemProperty -Path $Path -ErrorAction SilentlyContinue)) {");
            loStringBuilder.AppendLine(@"							$Value = split-Path -leaf $Path");
            loStringBuilder.AppendLine(@"							$Path = split-Path -parent $Path");
            loStringBuilder.AppendLine(@"							if ( -not (Get-ItemProperty -Path $Path -Name $Value -ErrorAction SilentlyContinue) ) { return $false }");
            loStringBuilder.AppendLine(@"						}");
            loStringBuilder.AppendLine(@"					}");
            loStringBuilder.AppendLine(@"					""equals"" {");
            loStringBuilder.AppendLine(@"						if ( -not ((Get-RegistryValue -Path $SetupDefinition.CheckPath) -eq $SetupDefinition.CheckValue) ) { return $false }");
            loStringBuilder.AppendLine(@"					}");
            loStringBuilder.AppendLine(@"					default { return 1024 }");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			} # end of switch:registry");
            loStringBuilder.AppendLine(@"			""uninstall"" {");
            loStringBuilder.AppendLine(@"				switch -regex ( $SetupDefinition.CheckCondition.ToLower() ) {");
            loStringBuilder.AppendLine(@"					""^exists$"" {");
            loStringBuilder.AppendLine(@"						$found = $false");
            loStringBuilder.AppendLine(@"						ForEach ($SearchRegkey in @(");
            loStringBuilder.AppendLine(@"								""HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall"",");
            loStringBuilder.AppendLine(@"								""HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"")");
            loStringBuilder.AppendLine(@"								) {");
            loStringBuilder.AppendLine(@"							ForEach ($SearchPath in (Get-ChildItem -Path $SearchRegkey -ErrorAction SilentlyContinue)) {");
            loStringBuilder.AppendLine(@"								if ((Get-RegistryValue -Path $SearchPath.PSPath -Value ""DisplayName"") -eq $SetupDefinition.CheckPath) {");
            loStringBuilder.AppendLine(@"									$found = $true");
            loStringBuilder.AppendLine(@"								}");
            loStringBuilder.AppendLine(@"							}");
            loStringBuilder.AppendLine(@"						}");
            loStringBuilder.AppendLine(@"						if ($found -eq $false) { return $false }");
            loStringBuilder.AppendLine(@"					}");
            loStringBuilder.AppendLine(@"					""^version_(eq|lt|le|gt|ge)$"" {");
            loStringBuilder.AppendLine(@"						$found = $false");
            loStringBuilder.AppendLine(@"						$condition = [String]$SetupDefinition.CheckCondition.ToLower().Split(""_"")[1]");
            loStringBuilder.AppendLine(@"						$checkvalue = $SetupDefinition.CheckValue");
            loStringBuilder.AppendLine(@"						ForEach ($SearchRegkey in @(");
            loStringBuilder.AppendLine(@"								""HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall"",");
            loStringBuilder.AppendLine(@"								""HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"")");
            loStringBuilder.AppendLine(@"								) {");
            loStringBuilder.AppendLine(@"							ForEach ($SearchPath in (Get-ChildItem -Path $SearchRegkey -ErrorAction SilentlyContinue)) {");
            loStringBuilder.AppendLine(@"								$foundversion = Get-RegistryValue -Path $SearchPath.PSPath -Value ""DisplayVersion""");
            loStringBuilder.AppendLine(@"								Invoke-Expression -Command ""if ( [Version]$foundversion -$condition [Version]$checkvalue ) {`$found = `$true}""");
            loStringBuilder.AppendLine(@"							}");
            loStringBuilder.AppendLine(@"						}");
            loStringBuilder.AppendLine(@"						if ($found -eq $false) { return $false }");
            loStringBuilder.AppendLine(@"					}");
            loStringBuilder.AppendLine(@"					default { return 1024 }");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			} # end of switch:uninstall");
            loStringBuilder.AppendLine(@"			""execute"" {");
            loStringBuilder.AppendLine(@"				switch -regex ( $SetupDefinition.CheckCondition.ToLower() ) {");
            loStringBuilder.AppendLine(@"					""^exitcode_(eq|lt|le|gt|ge)$"" {");
            loStringBuilder.AppendLine(@"						$executable = $SetupDefinition.CheckPath");
            loStringBuilder.AppendLine(@"						$condition = [String]$SetupDefinition.CheckCondition.ToLower().Split(""_"")[1]");
            loStringBuilder.AppendLine(@"						$checkvalue = $SetupDefinition.CheckValue");
            loStringBuilder.AppendLine(@"						$exitcode = (Start-Process -Wait -WindowStyle ""Hidden"" -Passthru -FilePath ""$executable"").ExitCode");
            loStringBuilder.AppendLine(@"						Invoke-Expression -Command ""if ( -not ($exitcode -$condition $checkvalue) ) { return $false }""");
            loStringBuilder.AppendLine(@"					}");
            loStringBuilder.AppendLine(@"					default { return 1024 }");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			} # end of switch:execute");
            loStringBuilder.AppendLine(@"			default { return 1024}");
            loStringBuilder.AppendLine(@"		} # end of switch");
            loStringBuilder.AppendLine(@"		return $true");
            loStringBuilder.AppendLine(@"	} # end of if");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * presets and other constants:");
            loStringBuilder.AppendLine(@"$global:ConfirmPreference = 'None'");
            loStringBuilder.AppendLine(@"$global:ErrorActionPreference = 'Stop'");
            loStringBuilder.AppendLine(@"$SetupLoopVariables = @(");
            loStringBuilder.AppendLine(@"	""Command"", ""Parameters"", ""GUID"", ""MSI"", ""MSU"", ""Path"",");
            loStringBuilder.AppendLine(@"	""Value"", ""Data"", ""ValueType"", ""ARPRegPath"",");
            loStringBuilder.AppendLine(@"	""CurrentExitCode"", ""CurrentExitMessage"")");
            loStringBuilder.AppendLine(@"$SourcePath = split-Path -parent $MyInvocation.MyCommand.Definition");
            loStringBuilder.AppendLine(@"$ScriptName = split-Path -Leaf $MyInvocation.MyCommand.Definition");
            loStringBuilder.AppendLine(@"$ScriptConfigurationXML = $SourcePath + ""\"" + [System.IO.Path]::GetFileNameWithoutExtension($ScriptName) + "".xml""");
            loStringBuilder.AppendLine(@"Remove-Variable -Name ExternalConfiguration -Scope global -ErrorAction SilentlyContinue");
            loStringBuilder.AppendLine(@"if ( Test-Path -Path $ScriptConfigurationXML ) {");
            loStringBuilder.AppendLine(@"	$global:ExternalConfiguration = Import-Clixml -Path $ScriptConfigurationXML");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"Get-OrganizationConfiguration");
            loStringBuilder.AppendLine(@"Get-PackageConfiguration");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * generating Package-ID:");
            loStringBuilder.AppendLine(@"if ( $Package[""IDOverride""] ) {	$global:Package[""ID""] = $Package[""IDOverride""]; }");
            loStringBuilder.AppendLine(@"else {");
            loStringBuilder.AppendLine(@"	$global:Package[""ID""] = `");
            loStringBuilder.AppendLine(@"		( $Package[""Vendor""] -replace ""\s"","""" ) + ""_"" + `");
            loStringBuilder.AppendLine(@"		( $Package[""Product""] -replace ""\s"","""" ) + ""_"" + `");
            loStringBuilder.AppendLine(@"		$Package[""Version""] + ""_"";");
            loStringBuilder.AppendLine(@"	if ( $Package[""Architecture""] ) { $global:Package[""ID""] += $Package[""Architecture""] + ""_""; }");
            loStringBuilder.AppendLine(@"	$global:Package[""ID""] += $Package[""Release""];");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"Get-OSConfiguration");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"if ($OS[""LogFile""]) { $LogFile = $OS[""LogFile""] }");
            loStringBuilder.AppendLine(@"if ($Package[""LogFile""]) { $LogFile = $Package[""LogFile""] }");
            loStringBuilder.AppendLine(@"if ( $LogFile ) {");
            loStringBuilder.AppendLine(@"	if ( -not (Test-Path (split-path -parent ""$LogFile"")) ) {");
            loStringBuilder.AppendLine(@"		New-Item -Path (split-path -parent ""$LogFile"") -type directory -force | Out-Null");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	Start-Transcript -Path ""$LogFile"" -Append -Force");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * refining $OS:");
            loStringBuilder.AppendLine(@"$global:OS += @{");
            loStringBuilder.AppendLine(@"	Architecture = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).OSArchitecture;");
            loStringBuilder.AppendLine(@"		#FIXME: this won't work with <NT6 - see: http://msdn.microsoft.com/en-us/library/aa394239.aspx");
            loStringBuilder.AppendLine(@"	BuildVersion = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).BuildNumber;");
            loStringBuilder.AppendLine(@"	CompleteVersion = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).Version;");
            loStringBuilder.AppendLine(@"	CurrentUser = ${env:UserName};");
            loStringBuilder.AppendLine(@"	CurrentDomain = ${env:UserDNSDomain};");
            loStringBuilder.AppendLine(@"	Hostname = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).CSName;");
            loStringBuilder.AppendLine(@"	LCID = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).OSLanguage;");
            loStringBuilder.AppendLine(@"	MajorVersion = [Int32]((Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).Version `");
            loStringBuilder.AppendLine(@"					| %{$_.split('.')[0]});");
            loStringBuilder.AppendLine(@"	MinorVersion = [Int32]((Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).Version `");
            loStringBuilder.AppendLine(@"					| %{$_.split('.')[1]});");
            loStringBuilder.AppendLine(@"	Name = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).Caption;");
            loStringBuilder.AppendLine(@"	PSVersion = [String]$PSVersionTable.PSVersion.Major + ""."" + [String]$PSVersionTable.PSVersion.Minor;");
            loStringBuilder.AppendLine(@"	RegisteredOrganization = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).Organization;");
            loStringBuilder.AppendLine(@"	RegisteredUser = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).RegisteredUser;");
            loStringBuilder.AppendLine(@"	SPMajorVersion = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).ServicePackMajorVersion;");
            loStringBuilder.AppendLine(@"	SPMinorVersion = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction SilentlyContinue).ServicePackMinorVersion;");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"if ( $OS[""Architecture""] -eq '64-bit' ) { $global:OS[""Architecture""] = 'x64' }");
            loStringBuilder.AppendLine(@"else { $global:OS[""Architecture""] = 'x86' }");
            loStringBuilder.AppendLine(@"if ([System.IntPtr]::Size -eq 4) { $global:OS[""ArchitectureEnvironment""] = 'x86' }");
            loStringBuilder.AppendLine(@"else { $global:OS[""ArchitectureEnvironment""] = 'x64' }");
            loStringBuilder.AppendLine(@"$global:OS[""Version""] = [String]$OS[""MajorVersion""] + '.' + [String]$OS[""MinorVersion""];");
            loStringBuilder.AppendLine(@"$global:OS[""Name""] += '[' + $OS[""Architecture""] + ']';");
            loStringBuilder.AppendLine(@"if ( $OS[""SPMajorVersion""] -gt 0 -or $OS[""SPMinorVersion""] -gt 0 ) {");
            loStringBuilder.AppendLine(@"	$global:OS[""SPVersion""] = [String]$OS[""SPMajorVersion""] + '.' + [String]$OS[""SPMinorVersion""];");
            loStringBuilder.AppendLine(@"	$global:OS[""Name""] += ' (' + (Get-WmiObject -Class Win32_OperatingSystem).CSDVersion + ')';");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"if ( -not $OS[""CurrentDomain""] ) { $global:OS[""CurrentDomain""] = ${env:UserDomain} }");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# * refining $CommonDirs:");
            loStringBuilder.AppendLine(@"$CommonDirs = @{");
            loStringBuilder.AppendLine(@"	AdministrativeTools =	(Get-CommonShellFolder ""Common Administrative Tools"");");
            loStringBuilder.AppendLine(@"	AppData =				(Get-CommonShellFolder ""Common AppData"");");
            loStringBuilder.AppendLine(@"	Desktop =				(Get-CommonShellFolder ""Common Desktop"");");
            loStringBuilder.AppendLine(@"	Documents =				(Get-CommonShellFolder ""Common Documents"");");
            loStringBuilder.AppendLine(@"	Music =					(Get-CommonShellFolder ""CommonMusic"");");
            loStringBuilder.AppendLine(@"	Pictures =				(Get-CommonShellFolder ""CommonPictures"");");
            loStringBuilder.AppendLine(@"	Programs =				(Get-CommonShellFolder ""Common Programs"");");
            loStringBuilder.AppendLine(@"	StartMenu =				(Get-CommonShellFolder ""Common Start Menu"");");
            loStringBuilder.AppendLine(@"	Startup =				(Get-CommonShellFolder ""Common Startup"");");
            loStringBuilder.AppendLine(@"	Templates =				(Get-CommonShellFolder ""Common Templates"");");
            loStringBuilder.AppendLine(@"	Video =					(Get-CommonShellFolder ""CommonVideo"");");
            loStringBuilder.AppendLine(@"	SystemDrive =			${env:SystemDrive};");
            loStringBuilder.AppendLine(@"	SystemRoot =			${env:SystemRoot};");
            loStringBuilder.AppendLine(@"	System32_Current =		${env:SystemRoot} + ""\system32"";");
            loStringBuilder.AppendLine(@"	HKLMSoftware_Current =	""hklm:\Software"";");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"if ( $OS[""ArchitectureEnvironment""] -eq 'x64' ) {");
            loStringBuilder.AppendLine(@"	$CommonDirs[""ProgramFiles32""] = ${env:ProgramFiles(x86)}");
            loStringBuilder.AppendLine(@"	$CommonDirs[""ProgramFiles64""] = ${env:ProgramFiles}");
            loStringBuilder.AppendLine(@"	$CommonDirs[""System32""] = ${env:SystemRoot} + ""\SysWOW64""");
            loStringBuilder.AppendLine(@"	$CommonDirs[""System64""] = ${env:SystemRoot} + ""\System32""");
            loStringBuilder.AppendLine(@"	$CommonDirs[""HKLMSoftware32""] = ""hklm:\Software\Wow6432Node""");
            loStringBuilder.AppendLine(@"	$CommonDirs[""HKLMSoftware64""] = ""hklm:\Software""");
            loStringBuilder.AppendLine(@"} else {");
            loStringBuilder.AppendLine(@"	$CommonDirs[""ProgramFiles32""] = ${env:ProgramFiles}");
            loStringBuilder.AppendLine(@"	$CommonDirs[""System32""] = ${env:SystemRoot} + ""\System32""");
            loStringBuilder.AppendLine(@"	$CommonDirs[""HKLMSoftware32""] = ""hklm:\Software""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"$CommonDirs[""ProgramsApplications""] = $CommonDirs[""Programs""] + ""\Applications""");
            loStringBuilder.AppendLine(@"$CommonDirs[""ProgramsUtilities""] = $CommonDirs[""Programs""] + ""\Utilities""");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * job functions:");
            loStringBuilder.AppendLine(@"function niy { ""Not implemented yet!""; $global:returncode = 1 }");
            loStringBuilder.AppendLine(@"function Run-Check {");
            loStringBuilder.AppendLine(@"	title ""Check""");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"	$CheckResult = 0");
            loStringBuilder.AppendLine(@"	$CheckLoop = 0");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"	:checkloop foreach ($CurrentSetup in $Setup) {");
            loStringBuilder.AppendLine(@"		foreach ( $v in $SetupLoopVariables ) { Remove-Variable $v -ErrorAction SilentlyContinue }");
            loStringBuilder.AppendLine(@"		if ( $CurrentSetup.Architecture ) { if ( $CurrentSetup.Architecture -ne $OS[""Architecture""] ) { continue checkloop } }");
            loStringBuilder.AppendLine(@"		$CheckLoop++");
            loStringBuilder.AppendLine(@"		switch ( Test-Setup -SetupDefinition $CurrentSetup ) {");
            loStringBuilder.AppendLine(@"			$true {}");
            loStringBuilder.AppendLine(@"			$false { $CheckResult++ }");
            loStringBuilder.AppendLine(@"			1024 {");
            loStringBuilder.AppendLine(@"				$global:errormessage = ""Unknown check configuration at setup block no. ${CheckLoop}!""");
            loStringBuilder.AppendLine(@"				$global:returncode = 1024");
            loStringBuilder.AppendLine(@"				break checkloop");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			default {}");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"	} # end of :checkloop");
            loStringBuilder.AppendLine(@"	$global:returncode = $CheckResult");
            loStringBuilder.AppendLine(@"	""RESULT: $CheckResult check(s) are failed.""");
            loStringBuilder.AppendLine(@"	""""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Run-Install {");
            loStringBuilder.AppendLine(@"	title ""Installation""");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"	:installloop foreach ($CurrentSetup in $Setup) {");
            loStringBuilder.AppendLine(@"		foreach ( $v in $SetupLoopVariables ) { Remove-Variable $v -ErrorAction SilentlyContinue }");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		if ( $CurrentSetup.Architecture ) { if ( $CurrentSetup.Architecture -ne $OS[""Architecture""] ) { continue installloop } }");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		# prepare setup");
            loStringBuilder.AppendLine(@"		if ( ""msi"", ""msp"" -contains $CurrentSetup.Type.ToLower() ) {");
            loStringBuilder.AppendLine(@"			# MSI/MSP setup");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Name ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Path ) { $CurrentSetup.Path = $SourcePath; }");
            loStringBuilder.AppendLine(@"			$Command = $CommonDirs[""System32_Current""] + ""\msiexec.exe""");
            loStringBuilder.AppendLine(@"			$MSI = ""`"""" + $CurrentSetup.Path + ""\"" + $CurrentSetup.Name + ""`""""");
            loStringBuilder.AppendLine(@"			switch ($CurrentSetup.Type.ToLower()) {");
            loStringBuilder.AppendLine(@"				""msi"" { $Parameters = @(""/package"") }");
            loStringBuilder.AppendLine(@"				""msp"" { $Parameters = @(""/update"") }");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			$Parameters += @($MSI, [String]$OS[""MSIExecDefaultParameters""])");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Transforms ) {");
            loStringBuilder.AppendLine(@"				$Parameters += ""TRANSFORMS="" + ""`"""" + $CurrentSetup.Path + ""\"" + $CurrentSetup.Transforms + ""`""""");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.MSILogfile ) {");
            loStringBuilder.AppendLine(@"				# for compatibility");
            loStringBuilder.AppendLine(@"				$CurrentSetup.LogFile = $CurrentSetup.MSILogfile");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.LogFile ) {");
            loStringBuilder.AppendLine(@"				$Parameters += ""/l*v "" + ""`"""" + $CurrentSetup.LogFile + ""`""""");
            loStringBuilder.AppendLine(@"				if ( -not (Test-Path (split-path -parent $CurrentSetup.LogFile)) ) {");
            loStringBuilder.AppendLine(@"					New-Item -Path (split-path -parent $CurrentSetup.LogFile) -type directory -force | Out-Null");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Properties ) {");
            loStringBuilder.AppendLine(@"				$Parameters += $CurrentSetup.Properties");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Features ) {");
            loStringBuilder.AppendLine(@"				$OFSbak = $OFS");
            loStringBuilder.AppendLine(@"				$OFS = "",""");
            loStringBuilder.AppendLine(@"				$Parameters += ""ADDLOCAL="" + $CurrentSetup.Features");
            loStringBuilder.AppendLine(@"				$OFS = $OFSbak");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""msu"" ) {");
            loStringBuilder.AppendLine(@"			# MSU update");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Name ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Path ) { $CurrentSetup.Path = $SourcePath; }");
            loStringBuilder.AppendLine(@"			$Command = $CommonDirs[""System32_Current""] + ""\wusa.exe""");
            loStringBuilder.AppendLine(@"			$MSU = ""`"""" + $CurrentSetup.Path + ""\"" + $CurrentSetup.Name + ""`""""");
            loStringBuilder.AppendLine(@"			$Parameters = @($MSU, ""/quiet"", ""/norestart"")");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.LogFile ) {");
            loStringBuilder.AppendLine(@"				$Parameters += ""/log:"" + ""`"""" + $CurrentSetup.LogFile + ""`""""");
            loStringBuilder.AppendLine(@"				if ( -not (Test-Path (split-path -parent $CurrentSetup.LogFile)) ) {");
            loStringBuilder.AppendLine(@"					New-Item -Path (split-path -parent $CurrentSetup.LogFile) -type directory -force | Out-Null");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""ps"" ) {");
            loStringBuilder.AppendLine(@"			# PS command");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Name ) { continue }");
            loStringBuilder.AppendLine(@"			$Command = $CurrentSetup.Name");
            loStringBuilder.AppendLine(@"			$Parameters = $CurrentSetup.Parameters");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""exe"" ) {");
            loStringBuilder.AppendLine(@"			# executable setup");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Name ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Path ) { $CurrentSetup.Path = $SourcePath; }");
            loStringBuilder.AppendLine(@"			$Command = $CurrentSetup.Path + ""\"" + $CurrentSetup.Name");
            loStringBuilder.AppendLine(@"			$Parameters = $CurrentSetup.Parameters");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""directexe"" ) {");
            loStringBuilder.AppendLine(@"			# direct executable command");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Name ) { continue }");
            loStringBuilder.AppendLine(@"			$Command = $CurrentSetup.Name");
            loStringBuilder.AppendLine(@"			$Parameters = $CurrentSetup.Parameters");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""reg"" ) {");
            loStringBuilder.AppendLine(@"			# registry setting");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Path ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Value ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Data ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.ValueType ) { $CurrentSetup.ValueType = ""String"" }");
            loStringBuilder.AppendLine(@"			$Path = $CurrentSetup.Path");
            loStringBuilder.AppendLine(@"			$Value = $CurrentSetup.Value");
            loStringBuilder.AppendLine(@"			$Data = $CurrentSetup.Data");
            loStringBuilder.AppendLine(@"			$ValueType = $CurrentSetup.ValueType");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		if ( -not $CurrentSetup.WindowStyle ) { $CurrentSetup.WindowStyle = ""Normal"" }");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		# run setup");
            loStringBuilder.AppendLine(@"		if ( $Command ) {");
            loStringBuilder.AppendLine(@"			$CurrentExitCode = ""1860""");
            loStringBuilder.AppendLine(@"			""Command: $Command $Parameters""");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Type.ToLower() -ne ""ps"") {");
            loStringBuilder.AppendLine(@"				if ($Parameters) {");
            loStringBuilder.AppendLine(@"		        	$CurrentExitCode = ( `");
            loStringBuilder.AppendLine(@"						Start-Process -Wait -WindowStyle $CurrentSetup.WindowStyle -Passthru `");
            loStringBuilder.AppendLine(@"							-FilePath ""$Command"" -ArgumentList ""$Parameters"" `");
            loStringBuilder.AppendLine(@"					).ExitCode");
            loStringBuilder.AppendLine(@"				} else {");
            loStringBuilder.AppendLine(@"					$CurrentExitCode = ( `");
            loStringBuilder.AppendLine(@"						Start-Process -Wait -WindowStyle $CurrentSetup.WindowStyle -Passthru `");
            loStringBuilder.AppendLine(@"							-FilePath ""$Command"" `");
            loStringBuilder.AppendLine(@"					).ExitCode");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			else {");
            loStringBuilder.AppendLine(@"				Invoke-Expression -Command `");
            loStringBuilder.AppendLine(@"					""$Command -ErrorAction SilentlyContinue -ErrorVariable CurrentExitMessage $Parameters""");
            loStringBuilder.AppendLine(@"				if ( $CurrentExitMessage ) { $CurrentExitCode = 1 }");
            loStringBuilder.AppendLine(@"				else { $CurrentExitCode = 0 }");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"	        ""Exit code of last command: "" + $CurrentExitCode");
            loStringBuilder.AppendLine(@"	        if ( $CurrentExitMessage ) { ""Error message of last command: $CurrentExitMessage"" }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.AcceptExitCodes -contains $CurrentExitCode ) {");
            loStringBuilder.AppendLine(@"				""This exit code ($CurrentExitCode) was expected so exit code 0 will be assumed.""");
            loStringBuilder.AppendLine(@"				$CurrentExitCode = 0");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Type -eq 'directexe' ) {");
            loStringBuilder.AppendLine(@"				""This exit code ($CurrentExitCode) was expected so exit code 0 will be assumed.""");
            loStringBuilder.AppendLine(@"				$CurrentExitCode = 0");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			switch ($CurrentExitCode) {");
            loStringBuilder.AppendLine(@"				""0"" {");
            loStringBuilder.AppendLine(@"					""Last command was successful.""");
            loStringBuilder.AppendLine(@"					break");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"				""1604"" {");
            loStringBuilder.AppendLine(@"					""Last command reported its already installed. This will be ignored.""");
            loStringBuilder.AppendLine(@"					break");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"				""3010"" {");
            loStringBuilder.AppendLine(@"					""Last command requested a system reboot.""");
            loStringBuilder.AppendLine(@"					""This will be signalized at end of package setup with exit code 3010.""");
            loStringBuilder.AppendLine(@"					$global:Package[""Reboot""] = $true");
            loStringBuilder.AppendLine(@"					break");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"				default {");
            loStringBuilder.AppendLine(@"					$global:errormessage = ""Last command exited with an error! Package setup will will canceled!""");
            loStringBuilder.AppendLine(@"					$global:returncode = $CurrentExitCode");
            loStringBuilder.AppendLine(@"					break installloop");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""reg"" ) {");
            loStringBuilder.AppendLine(@"			""Registry: set value `""${Path}\${Value}=${Data}`"" (${ValueType})""");
            loStringBuilder.AppendLine(@"			Set-RegistryValue -Path ""$Path"" -Value ""$Value"" -Data ""$Data"" -Type $ValueType");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		# setup ARP");
            loStringBuilder.AppendLine(@"		if ( $CurrentSetup.ARPID ) {");
            loStringBuilder.AppendLine(@"			""ARP: set `""$CurrentSetup.ARPID`""""");
            loStringBuilder.AppendLine(@"			if ( $OS[""Architecture""] -eq 'x64' `");
            loStringBuilder.AppendLine(@"					-and $OS[""ArchitectureEnvironment""] -eq 'x64' `");
            loStringBuilder.AppendLine(@"					-and $CurrentSetup.ARPWow6432Node -eq $true ) {");
            loStringBuilder.AppendLine(@"				$ARPRegPath = $OS[""HKLMSoftware32""]");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			else { $ARPRegPath = $OS[""HKLMSoftware_Current""] }");
            loStringBuilder.AppendLine(@"			$ARPRegPath += ""\Microsoft\Windows\CurrentVersion\Uninstall\"" + $CurrentSetup.ARPID");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.UninstallString ) {");
            loStringBuilder.AppendLine(@"				Set-RegistryValue `");
            loStringBuilder.AppendLine(@"					-Path ""$ARPRegPath"" -Value ""UninstallString"" `");
            loStringBuilder.AppendLine(@"					-Data $CurrentSetup.UninstallString -Type ExpandString");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			elseif ( -not (Get-RegistryValue -path ""$ARPRegPath"" -value ""UninstallString"") ) {");
            loStringBuilder.AppendLine(@"				Set-RegistryValue `");
            loStringBuilder.AppendLine(@"					-Path ""$ARPRegPath"" -Value ""UninstallString"" `");
            loStringBuilder.AppendLine(@"					-Data	""msg * `""A uninstallation via this function is not intended."" + `");
            loStringBuilder.AppendLine(@"							""Ask your servicedesk or administrator how to do this.`"""" `");
            loStringBuilder.AppendLine(@"					-Type ExpandString");
            loStringBuilder.AppendLine(@"				$CurrentSetup.ARPNoRemove = $true");
            loStringBuilder.AppendLine(@"				$CurrentSetup.ARPNoModify = $true");
            loStringBuilder.AppendLine(@"				$CurrentSetup.ARPNoRepair = $true");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPName )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""DisplayName"" -Data $CurrentSetup.ARPName }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPName )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""DisplayName"" -Data $CurrentSetup.ARPName }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPVersion )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""DisplayVersion"" -Data $CurrentSetup.ARPVersion }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPIcon )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""DisplayIcon"" -Data $CurrentSetup.ARPIcon }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPContact )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""Contact"" -Data $CurrentSetup.ARPContact }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPComments )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""Comments"" -Data $CurrentSetup.ARPComments }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPHelpLink ) {");
            loStringBuilder.AppendLine(@"				Set-RegistryValue -Path ""$ARPRegPath"" -Value ""HelpLink"" -Data $CurrentSetup.ARPHelpLink -Type ExpandString");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPHelpTelephone )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""HelpTelephone"" -Data $CurrentSetup.ARPHelpTelephone }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPNoModify -eq $true )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""NoModify"" -Data 0x1 -Type DWord }");
            loStringBuilder.AppendLine(@"			else { Set-RegistryValue -Path ""$ARPRegPath"" -Value ""NoModify"" -Data 0x0 -Type DWord }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPNoRemove -eq $true )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""NoRemove"" -Data 0x1 -Type DWord }");
            loStringBuilder.AppendLine(@"			else { Set-RegistryValue -Path ""$ARPRegPath"" -Value ""NoRemove"" -Data 0x0 -Type DWord }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPNoRepair -eq $true )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""NoRepair"" -Data 0x1 -Type DWord }");
            loStringBuilder.AppendLine(@"			else { Set-RegistryValue -Path ""$ARPRegPath"" -Value ""NoRepair"" -Data 0x0 -Type DWord }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.ARPSystemComponent -eq $true )");
            loStringBuilder.AppendLine(@"				{ Set-RegistryValue -Path ""$ARPRegPath"" -Value ""SystemComponent"" -Data 0x1 -Type DWord }");
            loStringBuilder.AppendLine(@"			else { Set-RegistryValue -Path ""$ARPRegPath"" -Value ""SystemComponent"" -Data 0x0 -Type DWord }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.RemoveARPURLUpdateInfo -eq $true ) {");
            loStringBuilder.AppendLine(@"				Remove-RegistryValue -Path ""$ARPRegPath"" -Value ""URLUpdateInfo""");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"		} # end of setup ARP");
            loStringBuilder.AppendLine(@"	} # end of :installloop");
            loStringBuilder.AppendLine(@"	""""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Run-Uninstall {");
            loStringBuilder.AppendLine(@"	title ""Uninstallation""");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"	[array]::Reverse($Setup)");
            loStringBuilder.AppendLine(@"	:removeloop foreach ($CurrentSetup in $Setup) {");
            loStringBuilder.AppendLine(@"		foreach ( $v in $SetupLoopVariables ) { Remove-Variable $v -ErrorAction SilentlyContinue }");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		if ( $CurrentSetup.Architecture ) { if ( $CurrentSetup.Architecture -ne $OS[""Architecture""] ) { continue removeloop } }");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		# prepare setup");
            loStringBuilder.AppendLine(@"		if ( $CurrentSetup.Type.ToLower() -eq ""msi"" ) {");
            loStringBuilder.AppendLine(@"			# MSI setup");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.GUID ) { continue }");
            loStringBuilder.AppendLine(@"			$Command = $CommonDirs[""System32_Current""] + ""\msiexec.exe""");
            loStringBuilder.AppendLine(@"			$GUID = ""`"""" + $CurrentSetup.GUID + ""`""""");
            loStringBuilder.AppendLine(@"			$Parameters = @(""/uninstall"", $GUID, [String]$OS[""MSIExecDefaultParameters""])");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.MSILogfile ) {");
            loStringBuilder.AppendLine(@"				# for compatibility");
            loStringBuilder.AppendLine(@"				$CurrentSetup.LogFile = $CurrentSetup.MSILogfile");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.LogFile ) {");
            loStringBuilder.AppendLine(@"				$Parameters += ""/l*v "" + ""`"""" + $CurrentSetup.LogFile + ""`""""");
            loStringBuilder.AppendLine(@"				if ( -not (Test-Path (split-path -parent $CurrentSetup.LogFile)) ) {");
            loStringBuilder.AppendLine(@"					New-Item -Path (split-path -parent $CurrentSetup.LogFile) -type directory -force | Out-Null");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Properties ) {");
            loStringBuilder.AppendLine(@"				$Parameters += $CurrentSetup.Properties");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Features ) {");
            loStringBuilder.AppendLine(@"				$OFSbak = $OFS");
            loStringBuilder.AppendLine(@"				$OFS = "",""");
            loStringBuilder.AppendLine(@"				$Parameters += ""REMOVE="" + $CurrentSetup.Features");
            loStringBuilder.AppendLine(@"				$OFS = $OFSbak");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			else {");
            loStringBuilder.AppendLine(@"				$Parameters += ""REMOVE=ALL""");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""msp"" ) { continue }");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""msu"" ) {");
            loStringBuilder.AppendLine(@"			# MSU update");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Name ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Path ) { $CurrentSetup.Path = $SourcePath; }");
            loStringBuilder.AppendLine(@"			$Command = $CommonDirs[""System32_Current""] + ""\wusa.exe""");
            loStringBuilder.AppendLine(@"			$MSU = ""`"""" + $CurrentSetup.Path + ""\"" + $CurrentSetup.Name + ""`""""");
            loStringBuilder.AppendLine(@"			$Parameters = @(""/uninstall"", $MSU, ""/quiet"", ""/norestart"")");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.LogFile ) {");
            loStringBuilder.AppendLine(@"				$Parameters += ""/log:"" + ""`"""" + $CurrentSetup.LogFile + ""`""""");
            loStringBuilder.AppendLine(@"				if ( -not (Test-Path (split-path -parent $CurrentSetup.LogFile)) ) {");
            loStringBuilder.AppendLine(@"					New-Item -Path (split-path -parent $CurrentSetup.LogFile) -type directory -force | Out-Null");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""ps"" ) {");
            loStringBuilder.AppendLine(@"			# PS command");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.UninstallName ) { continue }");
            loStringBuilder.AppendLine(@"			$Command = $CurrentSetup.UninstallName");
            loStringBuilder.AppendLine(@"			$Parameters = $CurrentSetup.UninstallParameters");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""exe"" ) {");
            loStringBuilder.AppendLine(@"			# executable setup");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.UninstallName ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.UninstallPath ) { $CurrentSetup.UninstallPath = $SourcePath; }");
            loStringBuilder.AppendLine(@"			$Command = $CurrentSetup.UninstallPath + ""\"" + $CurrentSetup.UninstallName");
            loStringBuilder.AppendLine(@"			$Parameters = $CurrentSetup.UninstallParameters");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""directexe"" ) {");
            loStringBuilder.AppendLine(@"			# executable setup");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.UninstallName ) { continue }");
            loStringBuilder.AppendLine(@"			$Command = $CurrentSetup.UninstallName");
            loStringBuilder.AppendLine(@"			$Parameters = $CurrentSetup.UninstallParameters");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""reg"" ) {");
            loStringBuilder.AppendLine(@"			# registry setting");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Path ) { continue }");
            loStringBuilder.AppendLine(@"			if ( ! $CurrentSetup.Value ) { continue }");
            loStringBuilder.AppendLine(@"			$Path = $CurrentSetup.Path");
            loStringBuilder.AppendLine(@"			$Value = $CurrentSetup.Value");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		if ( -not $CurrentSetup.WindowStyle ) { $CurrentSetup.WindowStyle = ""Normal"" }");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		# run setup");
            loStringBuilder.AppendLine(@"		if ( $Command ) {");
            loStringBuilder.AppendLine(@"			$CurrentExitCode = ""1860""");
            loStringBuilder.AppendLine(@"			""Command: $Command $Parameters""");
            loStringBuilder.AppendLine(@"	        if ( $CurrentSetup.Type.ToLower() -ne ""ps"") {");
            loStringBuilder.AppendLine(@"				if ($Parameters) {");
            loStringBuilder.AppendLine(@"		        	$CurrentExitCode = ( `");
            loStringBuilder.AppendLine(@"						Start-Process -Wait -WindowStyle $CurrentSetup.WindowStyle -Passthru `");
            loStringBuilder.AppendLine(@"							-FilePath ""$Command"" -ArgumentList ""$Parameters"" `");
            loStringBuilder.AppendLine(@"					).ExitCode");
            loStringBuilder.AppendLine(@"				} else {");
            loStringBuilder.AppendLine(@"					$CurrentExitCode = ( `");
            loStringBuilder.AppendLine(@"						Start-Process -Wait -WindowStyle $CurrentSetup.WindowStyle -Passthru `");
            loStringBuilder.AppendLine(@"							-FilePath ""$Command"" `");
            loStringBuilder.AppendLine(@"						).ExitCode");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			else {");
            loStringBuilder.AppendLine(@"				Invoke-Expression `");
            loStringBuilder.AppendLine(@"					-Command ""$Command -ErrorAction SilentlyContinue -ErrorVariable CurrentExitMessage $Parameters""");
            loStringBuilder.AppendLine(@"				if ( $CurrentExitMessage ) { $CurrentExitCode = 1 }");
            loStringBuilder.AppendLine(@"				else { $CurrentExitCode = 0 }");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"	        ""Exit code of last command: "" + $CurrentExitCode");
            loStringBuilder.AppendLine(@"	        if ( $CurrentExitMessage ) { ""Error message of last command: $CurrentExitMessage"" }");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.UninstallAcceptExitCodes -contains $CurrentExitCode ) {");
            loStringBuilder.AppendLine(@"				""This exit code ($CurrentExitCode) was expected so exit code 0 will be assumed.""");
            loStringBuilder.AppendLine(@"				$CurrentExitCode = 0");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			if ( $CurrentSetup.Type.ToLower() -eq 'directexe' ) {");
            loStringBuilder.AppendLine(@"				""This exit code ($CurrentExitCode) was expected so exit code 0 will be assumed.""");
            loStringBuilder.AppendLine(@"				$CurrentExitCode = 0");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"	        switch ($CurrentExitCode) {");
            loStringBuilder.AppendLine(@"				""0"" {");
            loStringBuilder.AppendLine(@"					""Last command was successful.""");
            loStringBuilder.AppendLine(@"					break");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"				""1605"" {");
            loStringBuilder.AppendLine(@"					""Last command reported its NOT installed. This will be ignored.""");
            loStringBuilder.AppendLine(@"					break");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"				""3010"" {");
            loStringBuilder.AppendLine(@"					""Last command requested a system reboot.""");
            loStringBuilder.AppendLine(@"					""This will be signalized at end of package setup with exit code 3010.""");
            loStringBuilder.AppendLine(@"					$global:Package[""Reboot""] = $true");
            loStringBuilder.AppendLine(@"					break");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"				default {");
            loStringBuilder.AppendLine(@"					$global:errormessage = ""Last command exited with an error! Package setup will will canceled!""");
            loStringBuilder.AppendLine(@"					$global:returncode = $CurrentExitCode");
            loStringBuilder.AppendLine(@"					break removeloop");
            loStringBuilder.AppendLine(@"				}");
            loStringBuilder.AppendLine(@"	        }");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		elseif ( $CurrentSetup.Type.ToLower() -eq ""reg"" ) {");
            loStringBuilder.AppendLine(@"			""Registry: remove value `""${Path}\${Value}`""""");
            loStringBuilder.AppendLine(@"			Remove-RegistryValue -Path ""$Path"" -Value ""$Value""");
            loStringBuilder.AppendLine(@"			if ( -not (get-itemproperty ""$Path"" -ErrorAction SilentlyContinue) `");
            loStringBuilder.AppendLine(@"					-and -not (get-childitem ""$Path"" -ErrorAction SilentlyContinue)) {");
            loStringBuilder.AppendLine(@"				""Registry: remove Path `""$Path`""""");
            loStringBuilder.AppendLine(@"				Remove-Item ""$Path"" -ErrorAction SilentlyContinue | Out-Null");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"		# remove ARP");
            loStringBuilder.AppendLine(@"        if ( $CurrentSetup.ARPID -and $CurrentSetup.UninstallARP -eq $true ) {");
            loStringBuilder.AppendLine(@"			""ARP: remove `""$CurrentSetup.ARPID`""""");
            loStringBuilder.AppendLine(@"			if ( $OS[""Architecture""] -eq 'x64' `");
            loStringBuilder.AppendLine(@"					-and $OS[""ArchitectureEnvironment""] -eq 'x64' `");
            loStringBuilder.AppendLine(@"					-and $CurrentSetup.ARPWow6432Node -eq $true ) {");
            loStringBuilder.AppendLine(@"				$ARPRegPath = $OS[""HKLMSoftware32""]");
            loStringBuilder.AppendLine(@"			}");
            loStringBuilder.AppendLine(@"			else { $ARPRegPath = $OS[""HKLMSoftware_Current""] }");
            loStringBuilder.AppendLine(@"			$ARPRegPath += ""\Microsoft\Windows\CurrentVersion\Uninstall\"" + $CurrentSetup.ARPID");
            loStringBuilder.AppendLine(@"			Remove-Item ""$ARPRegPath"" -recurse -force -ErrorAction SilentlyContinue");
            loStringBuilder.AppendLine(@"        } # end of remove ARP");
            loStringBuilder.AppendLine(@"	} # end of :removeloop");
            loStringBuilder.AppendLine(@"	""""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Write-Configuration {");
            loStringBuilder.AppendLine(@"	title ""Configuration""");
            loStringBuilder.AppendLine(@"	""===== Global variables and constants =====""; """"");
            loStringBuilder.AppendLine(@"	""Action = $Action""");
            loStringBuilder.AppendLine(@"	""ConfirmPreference = $ConfirmPreference""");
            loStringBuilder.AppendLine(@"	""ErrorActionPreference = $ErrorActionPreference""");
            loStringBuilder.AppendLine(@"	""LogFile = $LogFile""");
            loStringBuilder.AppendLine(@"	""Parameters = "" + [String]$Parameters");
            loStringBuilder.AppendLine(@"	""SourcePath = $SourcePath""");
            loStringBuilder.AppendLine(@"	""ScriptName = $ScriptName""");
            loStringBuilder.AppendLine(@"	""VERSION (of script) = $VERSION""");
            loStringBuilder.AppendLine(@"	""""");
            loStringBuilder.AppendLine(@"	""===== Hash `$Package ====="";");
            loStringBuilder.AppendLine(@"	$Package.GetEnumerator() | sort name | Format-Table;");
            loStringBuilder.AppendLine(@"	""===== Hash `$Organization ====="";");
            loStringBuilder.AppendLine(@"	$Organization.GetEnumerator() | sort name | Format-Table;");
            loStringBuilder.AppendLine(@"	""===== Hash `$OS ====="";");
            loStringBuilder.AppendLine(@"	$OS.GetEnumerator() | sort name | Format-Table;");
            loStringBuilder.AppendLine(@"	""===== Hash `$CommonDirs ====="";");
            loStringBuilder.AppendLine(@"	$CommonDirs.GetEnumerator() | sort name | Format-Table;");
            loStringBuilder.AppendLine(@"	""===== Hash `$Setup ====="";");
            loStringBuilder.AppendLine(@"	foreach ($CurrentSetup in $Setup) {");
            loStringBuilder.AppendLine(@"		$CurrentSetup.GetEnumerator() | sort name | Format-Table;");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Write-Help {");
            loStringBuilder.AppendLine(@"	title ""Usage""");
            loStringBuilder.AppendLine(@"	""$ScriptName { install | remove | check | reinstall* | repair* } [-f]* [-l LogFile]*""");
            loStringBuilder.AppendLine(@"	""$ScriptName { info | summary* | help }""");
            loStringBuilder.AppendLine(@"	""$ScriptName exportxml Output-Filename""");
            loStringBuilder.AppendLine(@"	""(* not implemented yet)""");
            loStringBuilder.AppendLine(@"	""""");
            loStringBuilder.AppendLine(@"	""check:     Test if deployment was successful.""");
            loStringBuilder.AppendLine(@"	""           Resulting exit code will be the count of failed setup""");
            loStringBuilder.AppendLine(@"	""           actions. If exit code is 1024 a script error occurred.""");
            loStringBuilder.AppendLine(@"	""exportxml: Exports package configuration to an XML file.""");
            loStringBuilder.AppendLine(@"	""help:      Shows this help.""");
            loStringBuilder.AppendLine(@"	""info:      Shows package informations and script configuration.""");
            loStringBuilder.AppendLine(@"	""install:   Installs this package to the system.""");
            loStringBuilder.AppendLine(@"	""remove:    Removes this package from the system.""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Write-DeploymentResultMessages {");
            loStringBuilder.AppendLine(@"	if ($global:returncode -eq 0) {");
            loStringBuilder.AppendLine(@"		""RESULT: Deployment of `"""" + [String]$Package[""Vendor"",""Product"",""Version"",""Architecture""] + ""`"" was successful.""");
            loStringBuilder.AppendLine(@"		if ($global:Package[""Reboot""] -eq $true) {");
            loStringBuilder.AppendLine(@"			""But a restart of the system is neccessary to complete installation!""");
            loStringBuilder.AppendLine(@"			$global:returncode = 3010");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	else {");
            loStringBuilder.AppendLine(@"		""RESULT: Deployment of `"""" + [String]$Package[""Vendor"",""Product"",""Version"",""Architecture""] + ""`"" was NOT successful!""");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"function Write-XML {");
            loStringBuilder.AppendLine(@"	param (");
            loStringBuilder.AppendLine(@"		[Parameter(Mandatory=$true)][String]$Path");
            loStringBuilder.AppendLine(@"	)");
            loStringBuilder.AppendLine(@"	title ""Export XML""");
            loStringBuilder.AppendLine(@"	if ( ! $Path ) {");
            loStringBuilder.AppendLine(@"		$global:errormessage = ""Missing file argument for XML output!""");
            loStringBuilder.AppendLine(@"		$global:returncode = 1");
            loStringBuilder.AppendLine(@"		return");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	Export-Clixml -Path $Path -InputObject @{");
            loStringBuilder.AppendLine(@"		Package = $global:Package;");
            loStringBuilder.AppendLine(@"		Organization = $global:Organization;");
            loStringBuilder.AppendLine(@"		OS = @{");
            loStringBuilder.AppendLine(@"			LogFile = $global:OS[""LogFile""];");
            loStringBuilder.AppendLine(@"			MSIExecDefaultParameters = $global:OS[""MSIExecDefaultParameters""];");
            loStringBuilder.AppendLine(@"		};");
            loStringBuilder.AppendLine(@"		Setup = $global:Setup;");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	""Package configuration exported to '"" + (Resolve-Path -Path $Path).Path + ""'.""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * main:");
            loStringBuilder.AppendLine(@"hr");
            loStringBuilder.AppendLine(@"$Organization[""Name""] + "" - Deployment Setup Script - Version ${VERSION}""");
            loStringBuilder.AppendLine(@"""Unattended Setup of `"""" + [String]$Package[""Vendor"",""Product"",""Version"",""Architecture""] + ""`""""");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"$global:returncode = 0");
            loStringBuilder.AppendLine(@"$Action, $Parameters = $args;");
            loStringBuilder.AppendLine(@"if ( -not $Action) { $Action = 'none' }");
            loStringBuilder.AppendLine(@"Set-Location ""$SourcePath""");
            loStringBuilder.AppendLine(@"Get-SetupJobs");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"switch -regex ($Action.ToLower()) {");
            loStringBuilder.AppendLine(@"	""^install$""				{ Run-Install; Write-DeploymentResultMessages }");
            loStringBuilder.AppendLine(@"	""^(remove|uninstall)$""	{ Run-Uninstall; Write-DeploymentResultMessages }");
            loStringBuilder.AppendLine(@"	""^check$""				{ Run-Check; Write-DeploymentResultMessages }");
            loStringBuilder.AppendLine(@"	""^reinstall$""			{ niy }");
            loStringBuilder.AppendLine(@"	""^repair$""				{ niy }");
            loStringBuilder.AppendLine(@"	""^info$""				{ Write-Configuration }");
            loStringBuilder.AppendLine(@"	""^summary$""				{ niy }");
            loStringBuilder.AppendLine(@"	""^(help|--help|-help|-h|-\?|\/\?|\?)$""	{ Write-Help }");
            loStringBuilder.AppendLine(@"	""^exportxml$"" {");
            loStringBuilder.AppendLine(@"		if ( $Parameters -and ($Parameters.GetType().Name -eq ""String"") ) {");
            loStringBuilder.AppendLine(@"			Write-XML -Path $Parameters");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"		else {");
            loStringBuilder.AppendLine(@"			$global:errormessage = ""Missing or wrong file argument for XML output!""");
            loStringBuilder.AppendLine(@"			$global:returncode = 1");
            loStringBuilder.AppendLine(@"		}");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"	default	{");
            loStringBuilder.AppendLine(@"		Write-Help");
            loStringBuilder.AppendLine(@"		$global:errormessage = ""Missing or wrong arguments! See documentation and usage above.""");
            loStringBuilder.AppendLine(@"		$global:returncode = 1");
            loStringBuilder.AppendLine(@"	}");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# * closing:");
            loStringBuilder.AppendLine(@"title ""End""");
            loStringBuilder.AppendLine(@"if ($global:returncode -ne 0) {");
            loStringBuilder.AppendLine(@"	if ($global:errormessage) { ""ERROR: "" + $global:errormessage }");
            loStringBuilder.AppendLine(@"	elseif (-not $Action.ToLower() -eq ""check"") { ""An error occurred!"" }");
            loStringBuilder.AppendLine(@"	""Exit code: $global:returncode""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"""""");
            loStringBuilder.AppendLine(@"if ( $LogFile ) { Stop-Transcript }");
            loStringBuilder.AppendLine(@"exit $global:returncode");
            loStringBuilder.AppendLine(@"# **************************************************************************************************");
            loStringBuilder.AppendLine(@"# END");
            loStringBuilder.AppendLine(@"");


            string lsReturn = loStringBuilder.ToString();
            lsReturn = ConvertWindowsVariablesToPowershell(lsReturn);


            return lsReturn;
        }

        public static void GetMyAppDeploymentScript(ref StringBuilder loStringBuilder, ref DataTable loTable, string lsTableType, string lsUninstallKey, string lsShopProcess)
        {

            DataRow[] loRows;
            loRows = loTable.Select("", "order asc");


            string lsLastCommand = "";
            string lsLastProductCode = "";

            foreach (DataRow loRow in loRows)
            {
                string lsInherit = "";
                for (int i = 1; i < Convert.ToInt32(loRow["level"].ToString()); i++)
                {
                    lsInherit = lsInherit + "  ";
                }


                switch (loRow["actiontype"].ToString().ToLower())
                {

                    case "launchprocess":
                    case "launchprocessnowait":
                        {
                            string lsEXE = loRow["command"].ToString();
                            switch (lsEXE.ToLower())
                            {
                                case "taskkill":
                                    break;
                                case "checkupdate.vbs":
                                    break;
                                case "cmd.exe":
                                case "cmd":
                                    {
                                        string lsRealExe = "";
                                        try
                                        {
                                            lsRealExe = System.IO.Path.GetFileName(lsEXE);
                                        }
                                        catch { }
                                        //switch (lsTableType)
                                        //{
                                        //    case "uninstall":
                                        //        //loStringBuilder.AppendLine(@"# direct uninstall");
                                        //        ////loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                        //        //loStringBuilder.AppendLine(@"$Setup += @{");
                                        //        //loStringBuilder.AppendLine(@"	Type = ""directexe"";");
                                        //        //loStringBuilder.AppendLine(@"	UninstallName = """ + lsRealExe + @""";");
                                        //        //loStringBuilder.AppendLine(@"	UninstallParameters = @(");
                                        //        //loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                        //        //loStringBuilder.AppendLine(@"}");
                                        //        break;
                                        //    default:
                                        //        //loStringBuilder.AppendLine(@"# direct install");
                                        //        ////loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                        //        //loStringBuilder.AppendLine(@"$Setup += @{");
                                        //        //loStringBuilder.AppendLine(@"	Type = ""directexe"";");
                                        //        //loStringBuilder.AppendLine(@"	Name = """ + lsRealExe + @""";");
                                        //        //loStringBuilder.AppendLine(@"	Parameters = @(");
                                        //        //loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                        //        //loStringBuilder.AppendLine(@"}");
                                        //        break;
                                        //}

                                    }
                                    break;

                                //    case "powershell":
                                //        //switch (lsTableType)
                                //        //{
                                //        //    case "uninstall":
                                //        //        //loStringBuilder.AppendLine(@"# App-V deinstallieren");
                                //        //        ////loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                //        //        //loStringBuilder.AppendLine(@"$Setup += @{");
                                //        //        //loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                //        //        //loStringBuilder.AppendLine(@"	UninstallName = """ + "uninstallappv.cmd" + @""";");
                                //        //        //loStringBuilder.AppendLine(@"}");
                                //        //        break;
                                //        //    default:
                                //        //        //loStringBuilder.AppendLine(@"# App-V installieren");
                                //        //        ////loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                //        //        //loStringBuilder.AppendLine(@"$Setup += @{");
                                //        //        //loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                //        //        //loStringBuilder.AppendLine(@"	Name = """ + "installappv.cmd" + @""";");
                                //        //        //loStringBuilder.AppendLine(@"}");
                                //        //        break;
                                //        //}
                                //        break;
                                default:
                                    if (lsEXE != lsLastCommand)
                                    {
                                        loStringBuilder.AppendLine(@"	<Description>Running " + lsEXE + @"</Description>");
                                        if (loRow["actiontype"].ToString().ToLower() == "launchprocessnowait")
                                        {
                                            loStringBuilder.AppendLine(@"	<Execution>Execute-Process -Path '" + lsEXE + @"' -Parameters '" + loRow["param"].ToString().Replace(@"""", @"""""") + @"' -NoWait -ContinueOnError");
                                        }
                                        else
                                        {
                                            loStringBuilder.AppendLine(@"	<Execution>Execute-Process -Path '" + lsEXE + @"' -Parameters '" + loRow["param"].ToString().Replace(@"""", @"""""") + @"' -ContinueOnError");
                                        }
                                    }
                                    lsLastCommand = lsEXE;
                                    break;
                                    //        switch (lsTableType)
                                    //        {
                                    //            //case "uninstall":
                                    //            //    {
                                    //            //        string lsRealExe = "";
                                    //            //        try
                                    //            //        {
                                    //            //            lsRealExe = System.IO.Path.GetFileName(lsEXE);
                                    //            //        }
                                    //            //        catch { }
                                    //            //        string lsRealpath = "";
                                    //            //        try
                                    //            //        {
                                    //            //            lsRealpath = System.IO.Path.GetDirectoryName(lsEXE);
                                    //            //        }
                                    //            //        catch { }


                                    //            //        loStringBuilder.AppendLine(@"# Software deinstallieren");

                                    //            //        loStringBuilder.AppendLine(@"if (Test-Path (""" + lsEXE + @""")) {");
                                    //            //        loStringBuilder.AppendLine(@"$Setup += @{");
                                    //            //        loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                    //            //        loStringBuilder.AppendLine(@"	UninstallName = """ + lsRealExe + @""";");
                                    //            //        loStringBuilder.AppendLine(@"	UninstallPath = """ + lsRealpath + @""";");
                                    //            //        loStringBuilder.AppendLine(@"	UninstallParameters = @(");
                                    //            //        loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                    //            //        loStringBuilder.AppendLine(@"}");
                                    //            //        loStringBuilder.AppendLine(@"}");
                                    //            //    }
                                    //            //    break;
                                    //            default:


                                    //                ////fix for flash

                                    //                //writeToLogFile("chkecking if lslastcommand: " + lsEXE + " -> " + lsLastCommand);
                                    //                //if (lsEXE != lsLastCommand)
                                    //                //{
                                    //                //    loStringBuilder.AppendLine(@"# Software installieren");
                                    //                //    //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                    //                //    loStringBuilder.AppendLine(@"$Setup += @{");
                                    //                //    loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                    //                //    loStringBuilder.AppendLine(@"	Name = """ + lsEXE + @""";");
                                    //                //    loStringBuilder.AppendLine(@"	Parameters = @(");
                                    //                //    loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                    //                //    loStringBuilder.AppendLine(@"}");
                                    //                //}
                                    //                //lsLastCommand = lsEXE;

                                    //                break;

                                    //        }
                                    //        break;
                            }
                            lsLastCommand = loRow["command"].ToString();
                            writeToLogFile("settings lastcommand to: " + lsLastCommand);
                            break;


                        }


                    case "msiexec":
                        {
                            if (lsTableType == "install")
                            {
                                string lsMSI = "";
                                string lsMST = "";
                                string lsMSP = "";
                                string lsParameters = "";
                                string lsProductCode = "";
                                bool lbIsInstallation = true;

                                try
                                {
                                    foreach (string lsArgument in CommandLineToArgs(loRow["param"].ToString()))
                                    {
                                        bool lbDoneSomething = false;

                                        if (lsArgument.ToLower().StartsWith("/x"))
                                        {
                                            lbIsInstallation = false;
                                        }


                                        if (lsArgument.ToLower().Contains(".msi"))
                                        {
                                            lsMSI = lsArgument;
                                            lbDoneSomething = true;
                                        }
                                        if (lsArgument.ToLower().Contains(".msp"))
                                        {
                                            lsMSP = lsArgument;
                                            lbDoneSomething = true;
                                        }

                                        if (lsArgument.Contains("{") && lsArgument.Contains("}"))
                                        {
                                            lsProductCode = lsArgument;
                                            try
                                            {
                                                if (!lsProductCode.StartsWith("{"))
                                                {
                                                    lsProductCode = lsProductCode.Substring(lsProductCode.IndexOf("{"));
                                                }
                                            }
                                            catch { }
                                            try
                                            {
                                                if (!lsProductCode.EndsWith("}"))
                                                {
                                                    lsProductCode = lsProductCode.Substring(0, lsProductCode.IndexOf("}") + 1);
                                                }
                                            }
                                            catch { }
                                            lbDoneSomething = true;
                                        }

                                        if (lsArgument.ToLower().Contains(".mst"))
                                        {
                                            lsMST = lsArgument;
                                            lsMST = lsMST.Replace("TRANSFORMS=", "");
                                            lsMST = lsMST.Replace("TRANSFORMS =", "");
                                            lsMST = lsMST.Replace(@"""", "");
                                            lbDoneSomething = true;
                                        }
                                        if (!lbDoneSomething)
                                        {
                                            if (lsArgument.Contains("="))
                                            {
                                                bool lbArgumentIsValid = true;
                                                if (lsArgument.Contains("REBOOT"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.StartsWith("MSI"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.Contains("ALLUSERS"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.Contains("ROOTDRIVE"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.Trim().EndsWith("="))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lbArgumentIsValid)
                                                {
                                                    lsParameters += @", """ + lsArgument + @"""";
                                                }
                                            }
                                        }
                                    }
                                }
                                catch { }

                                if (lsParameters.StartsWith(@", "))
                                {
                                    lsParameters = lsParameters.Substring(2);
                                }

                                writeToLogFile("chkecking if lslastcommand: " + lsMSI + " -> " + lsLastCommand);

                                if (!lbIsInstallation)
                                {
                                    loStringBuilder.AppendLine(@"	<Description>Removing " + lsProductCode + @"</Description>");
                                    loStringBuilder.AppendLine(@"	<Execution>Execute-MSI -Action 'Uninstall' -Path '" + lsProductCode + "'</Execution>");
                                }
                                else
                                {
                                    if (lsMSI != lsLastCommand)
                                    {
                                        if (lsMSP != "")
                                        {


                                            loStringBuilder.AppendLine(@"	<Description>Patching " + lsMSI + @"</Description>");
                                            loStringBuilder.AppendLine(@"	<Execution>Execute-MSI -Action 'Patch' -Path '" + lsMSI + "' -Path '" + lsMSP + "'</Execution>");

                                        }
                                        else
                                        {
                                            if (lsLastProductCode == "" || lsLastProductCode != lsProductCode)
                                            {
                                                if (lsParameters != "")
                                                {
                                                    lsParameters = " " + lsParameters;
                                                }
                                                loStringBuilder.AppendLine(@"	<Description>Installing " + lsMSI + @"</Description>");
                                                if (lsMST != "")
                                                {
                                                    loStringBuilder.AppendLine(@"	<Execution>Execute-MSI -Action 'Install' -Path '" + lsMSI + "' -Transform '" + lsMST + "' -Parameters '/QN" + lsParameters + "'</Execution>");
                                                }
                                                else
                                                {
                                                    loStringBuilder.AppendLine(@"	<Execution>Execute-MSI -Action 'Install' -Path '" + lsMSI + "' -Parameters '/QN" + lsParameters + "'</Execution>");
                                                }

                                                //loStringBuilder.AppendLine(@"# Software installieren");
                                                ////loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                                //loStringBuilder.AppendLine(@"$Setup += @{");
                                                //loStringBuilder.AppendLine(@"	Type = ""msi"";");
                                                //loStringBuilder.AppendLine(@"	Name = """ + lsMSI + @""";");
                                                //if (lsMST != "")
                                                //{
                                                //    loStringBuilder.AppendLine(@"	Transforms = """ + lsMST + @""";");
                                                //}
                                                //if (lsParameters != "")
                                                //{
                                                //    loStringBuilder.AppendLine(@"	Properties = @(" + lsParameters + @");");
                                                //}
                                                //if (lsProductCode != "")
                                                //{
                                                //    lsLastProductCode = lsProductCode;
                                                //    loStringBuilder.AppendLine(@"	GUID = """ + lsProductCode + @""";");
                                                //}
                                                //else
                                                //{
                                                //    loStringBuilder.AppendLine(@"	GUID = """ + lsUninstallKey + @""";");
                                                //}
                                                //loStringBuilder.AppendLine(@"");
                                                //loStringBuilder.AppendLine(@"}");
                                            }
                                        }
                                    }
                                }
                                lsLastCommand = lsMSI;
                            }
                            break;
                        }

                    case "removefile":
                        {
                            loStringBuilder.AppendLine(@"	<Description>Removing file " + loRow["command"].ToString() + @"</Description>");
                            loStringBuilder.AppendLine(@"	<Execution>Remove-File -Path '" + loRow["command"].ToString() + "' -ContinueOnError</Execution>");
                        }
                        lsLastCommand = "";
                        break;

                    case "removeregkey":
                        {
                            string lsRoot = "HKEY_LOCAL_MACHINE";
                            string lsPath = loRow["command"].ToString();
                            string lsName = "";
                            try
                            {
                                lsPath = lsPath.Substring(lsPath.IndexOf(@"\") + 1);
                            }
                            catch { }
                            try
                            {
                                lsPath = lsPath.Substring(0, lsPath.IndexOf(Convert.ToChar("|")));
                            }
                            catch { }
                            try
                            {
                                lsPath = lsPath.Replace(@"WOW6432Node\", "");
                                lsPath = lsPath.Replace(@"Wow6432Node\", "");

                            }
                            catch { }
                            try
                            {
                                lsName = loRow["command"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCU"))
                            {
                                lsRoot = "HKEY_CURRENT_USER";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKU"))
                            {
                                lsRoot = "HKEY_USERS";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCR"))
                            {
                                lsRoot = "HKEY_CLASSES_ROOT";
                            }
                            if (lsRoot != "")
                            {
                                loStringBuilder.AppendLine(@"	<Description>Removing registry key</Description>");
                                string lsMyName = lsName;
                                if (lsMyName == "(default)" || lsMyName == "")
                                {
                                    loStringBuilder.AppendLine(@"	<Execution>Remove-RegistryKey -Key '" + lsRoot + @"\" + lsPath + "' -ContinueOnError</Execution>");
                                }
                                else
                                {
                                    loStringBuilder.AppendLine(@"	<Execution>Remove-RegistryKey -Key '" + lsRoot + @"\" + lsPath + "' -Name '" + lsName + "' -ContinueOnError</Execution>");
                                }
                            }
                        }
                        break;

                    case "copyfile":
                        {


                            loStringBuilder.AppendLine(@"# Dateien kopieren ");
                            loStringBuilder.AppendLine(@"    $Setup += @{ ");
                            loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                            loStringBuilder.AppendLine(@"            Name = ""Copy-Item""; ");
                            loStringBuilder.AppendLine(@"            Parameters = @( ");
                            loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                            loStringBuilder.AppendLine(@"                ""-Destination"", (""'" + loRow["param"].ToString() + @"'""), ");
                            loStringBuilder.AppendLine(@"                ""-Force"" ");
                            loStringBuilder.AppendLine(@"            ); ");
                            loStringBuilder.AppendLine(@"               AcceptExitCodes = @(1); ");
                            loStringBuilder.AppendLine(@"    } ");




                        }
                        lsLastCommand = "";
                        break;

                    case "copyfolder":
                        {

                            loStringBuilder.AppendLine(@"# Folder kopieren ");
                            loStringBuilder.AppendLine(@"    $Setup += @{ ");
                            loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                            loStringBuilder.AppendLine(@"            Name = ""Copy-Item""; ");
                            loStringBuilder.AppendLine(@"            Parameters = @( ");
                            loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                            loStringBuilder.AppendLine(@"                ""-Destination"", (""'" + loRow["param"].ToString() + @"'""), ");
                            loStringBuilder.AppendLine(@"                ""-Recurse"", ");
                            loStringBuilder.AppendLine(@"                ""-Force"" ");
                            loStringBuilder.AppendLine(@"            ); ");
                            loStringBuilder.AppendLine(@"               AcceptExitCodes = @(1); ");
                            loStringBuilder.AppendLine(@"    } ");


                        }
                        lsLastCommand = "";
                        break;

                    case "removefolder":
                        {
                            loStringBuilder.AppendLine(@"	<Description>Removing folder</Description>");
                            loStringBuilder.AppendLine(@"	<Execution>Remove-Folder -Path '" + loRow["command"].ToString() + "' -ContinueOnError</Execution>");
                        }
                        lsLastCommand = "";
                        break;

                    case "addregkey":
                        {
                            string lsRoot = "HKLM";
                            string lsPath = loRow["command"].ToString();
                            string lsName = "";
                            string lsRegType = "";
                            string lsValue = "";
                            try
                            {
                                lsPath = lsPath.Substring(lsPath.IndexOf(@"\") + 1);
                            }
                            catch { }
                            try
                            {
                                lsPath = lsPath.Substring(0, lsPath.IndexOf(Convert.ToChar("|")));
                            }
                            catch { }
                            try
                            {
                                lsName = loRow["command"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCU"))
                            {
                                lsRoot = "HKCU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKU"))
                            {
                                lsRoot = "HKU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCR"))
                            {
                                lsRoot = "HKCR";
                            }
                            try
                            {
                                lsValue = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            try
                            {
                                lsRegType = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                            }
                            catch { }
                            if (lsRegType != "")
                            {
                                loStringBuilder.AppendLine(@"	<Description>Adding registry key</Description>");
                                loStringBuilder.AppendLine(@"	<Execution>Set-RegistryKey -Key '" + lsRoot + @"\" + lsPath + "' -Name '" + lsName + "' -Value '" + lsValue + "' -Type '" + lsRegType + "' -ContinueOnError</Execution>");
                            }
                        }
                        lsLastCommand = "";
                        break;

                    case "addshortcut":
                        {
                            if (loRow["param"].ToString().Contains("|"))
                            {
                                string lsDestination = "";
                                string lsWorkDir = "";
                                string lsIconPath = "";
                                string lsIconIndex = "0";
                                string lsDescription = "";
                                try
                                {
                                    lsDestination = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                catch { }
                                try
                                {
                                    lsWorkDir = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                try
                                {
                                    lsIconPath = loRow["param"].ToString().Split(Convert.ToChar("|"))[2];
                                }
                                catch { }
                                try
                                {
                                    lsIconIndex = loRow["param"].ToString().Split(Convert.ToChar("|"))[3];
                                }
                                catch { }
                                try
                                {
                                    lsDescription = loRow["param"].ToString().Split(Convert.ToChar("|"))[4];
                                }
                                catch { }
                                string lsMyExe = "";
                                string lsMyPath = "";
                                try
                                {
                                    lsMyExe = System.IO.Path.GetFileName(loRow["command"].ToString());
                                }
                                catch { }
                                try
                                {
                                    lsMyPath = System.IO.Path.GetDirectoryName(loRow["command"].ToString());
                                }
                                catch { }
                                loStringBuilder.AppendLine(@"	<Description>Adding Shortcut</Description>");
                                loStringBuilder.AppendLine(@"	<Execution>New-Shortcut -Path '" + lsMyPath + @"\" + lsMyExe + ".lnk' -TargetPath '" + lsDestination + "' -ContinueOnError</Execution>");

                            }

                        }
                        lsLastCommand = "";
                        break;

                    case "expandfolder":
                        lsLastCommand = "";
                        break;

                    case "killprocess":
                        lsLastCommand = "";
                        if (lsShopProcess == "kill")
                        {
                            loStringBuilder.AppendLine(@"	<Description>Killing " + loRow["command"].ToString() + "</Description>");
                            loStringBuilder.AppendLine(@"	<Execution>Stop-ServiceAndDependencies -Name '" + loRow["command"].ToString() + "' -ContinueOnError</Execution>");
                        }
                        break;

                    case "checkarchitecture":
                        loStringBuilder.AppendLine("$os_type = (Get-WmiObject -Class Win32_ComputerSystem).SystemType -match '(x64)'");
                        switch (loRow["command"].ToString().ToLower())
                        {
                            case "x86":
                                loStringBuilder.AppendLine("if($os_type -eq $false) {");
                                break;
                            case "x64":
                                loStringBuilder.AppendLine("if($os_type -eq $true) {");
                                break;
                        }
                        lsLastCommand = "";
                        break;

                    case "checkregistry":
                        if (lsLastCommand != "checkregistry")
                        {
                            loStringBuilder.AppendLine("{");
                        }
                        lsLastCommand = "checkregistry";
                        break;

                    case "endcheck":
                        if (lsLastCommand != "endcheck")
                        {
                            //loStringBuilder.AppendLine("}");
                        }
                        lsLastCommand = "endcheck";
                        break;


                }
            }


        }


        public static void GetMyPowershellScript(ref StringBuilder loStringBuilder, ref DataTable loTable, string lsTableType, string lsUninstallKey)
        {

            DataRow[] loRows;
            if (lsTableType == "install")
            {
                loRows = loTable.Select("", "order asc");
            }
            else
            {
                loRows = loTable.Select("", "order desc");
            }

            string lsLastCommand = "";
            string lsLastProductCode = "";

            foreach (DataRow loRow in loRows)
            {
                string lsInherit = "";
                for (int i = 1; i < Convert.ToInt32(loRow["level"].ToString()); i++)
                {
                    lsInherit = lsInherit + "  ";
                }


                switch (loRow["actiontype"].ToString().ToLower())
                {

                    case "launchprocess":

                        {
                            string lsEXE = loRow["command"].ToString();
                            switch (lsEXE.ToLower())
                            {
                                case "taskkill":
                                    break;
                                case "checkupdate.vbs":
                                    break;
                                case "cmd.exe":
                                case "cmd":
                                    {
                                        string lsRealExe = "";
                                        try
                                        {
                                            lsRealExe = System.IO.Path.GetFileName(lsEXE);
                                        }
                                        catch { }
                                        switch (lsTableType)
                                        {
                                            case "uninstall":
                                                loStringBuilder.AppendLine(@"# direct uninstall");
                                                //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                                loStringBuilder.AppendLine(@"$Setup += @{");
                                                loStringBuilder.AppendLine(@"	Type = ""directexe"";");
                                                loStringBuilder.AppendLine(@"	UninstallName = """ + lsRealExe + @""";");
                                                loStringBuilder.AppendLine(@"	UninstallParameters = @(");
                                                loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                                loStringBuilder.AppendLine(@"}");
                                                break;
                                            default:
                                                loStringBuilder.AppendLine(@"# direct install");
                                                //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                                loStringBuilder.AppendLine(@"$Setup += @{");
                                                loStringBuilder.AppendLine(@"	Type = ""directexe"";");
                                                loStringBuilder.AppendLine(@"	Name = """ + lsRealExe + @""";");
                                                loStringBuilder.AppendLine(@"	Parameters = @(");
                                                loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                                loStringBuilder.AppendLine(@"}");
                                                break;
                                        }
                                    }
                                    break;

                                case "powershell":
                                    switch (lsTableType)
                                    {
                                        case "uninstall":
                                            loStringBuilder.AppendLine(@"# App-V deinstallieren");
                                            //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                            loStringBuilder.AppendLine(@"$Setup += @{");
                                            loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                            loStringBuilder.AppendLine(@"	UninstallName = """ + "uninstallappv.cmd" + @""";");
                                            loStringBuilder.AppendLine(@"}");
                                            break;
                                        default:
                                            loStringBuilder.AppendLine(@"# App-V installieren");
                                            //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                            loStringBuilder.AppendLine(@"$Setup += @{");
                                            loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                            loStringBuilder.AppendLine(@"	Name = """ + "installappv.cmd" + @""";");
                                            loStringBuilder.AppendLine(@"}");
                                            break;
                                    }
                                    break;
                                default:
                                    switch (lsTableType)
                                    {
                                        case "uninstall":
                                            {
                                                string lsRealExe = "";
                                                try
                                                {
                                                    lsRealExe = System.IO.Path.GetFileName(lsEXE);
                                                }
                                                catch { }
                                                string lsRealpath = "";
                                                try
                                                {
                                                    lsRealpath = System.IO.Path.GetDirectoryName(lsEXE);
                                                }
                                                catch { }


                                                loStringBuilder.AppendLine(@"# Software deinstallieren");

                                                loStringBuilder.AppendLine(@"if (Test-Path (""" + lsEXE + @""")) {");
                                                loStringBuilder.AppendLine(@"$Setup += @{");
                                                loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                                loStringBuilder.AppendLine(@"	UninstallName = """ + lsRealExe + @""";");
                                                loStringBuilder.AppendLine(@"	UninstallPath = """ + lsRealpath + @""";");
                                                loStringBuilder.AppendLine(@"	UninstallParameters = @(");
                                                loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                                loStringBuilder.AppendLine(@"}");
                                                loStringBuilder.AppendLine(@"}");
                                            }
                                            break;
                                        default:


                                            //fix for flash

                                            writeToLogFile("chkecking if lslastcommand: " + lsEXE + " -> " + lsLastCommand);
                                            if (lsEXE != lsLastCommand)
                                            {
                                                loStringBuilder.AppendLine(@"# Software installieren");
                                                //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                                loStringBuilder.AppendLine(@"$Setup += @{");
                                                loStringBuilder.AppendLine(@"	Type = ""exe"";");
                                                loStringBuilder.AppendLine(@"	Name = """ + lsEXE + @""";");
                                                loStringBuilder.AppendLine(@"	Parameters = @(");
                                                loStringBuilder.AppendLine(@"	""" + loRow["param"].ToString().Replace(@"""", @"""""") + @""");");
                                                loStringBuilder.AppendLine(@"}");
                                            }
                                            lsLastCommand = lsEXE;

                                            break;

                                    }
                                    break;
                            }
                            lsLastCommand = loRow["command"].ToString();
                            writeToLogFile("settings lastcommand to: " + lsLastCommand);
                            break;


                        }


                    case "msiexec":
                        {
                            if (lsTableType == "install")
                            {
                                string lsMSI = "";
                                string lsMST = "";
                                string lsMSP = "";
                                string lsParameters = "";
                                string lsProductCode = "";
                                try
                                {
                                    foreach (string lsArgument in CommandLineToArgs(loRow["param"].ToString()))
                                    {
                                        bool lbDoneSomething = false;



                                        if (lsArgument.ToLower().Contains(".msi"))
                                        {
                                            lsMSI = lsArgument;
                                            lbDoneSomething = true;
                                        }
                                        if (lsArgument.ToLower().Contains(".msp"))
                                        {
                                            lsMSP = lsArgument;
                                            lbDoneSomething = true;
                                        }

                                        if (lsArgument.Contains("{") && lsArgument.Contains("}"))
                                        {
                                            lsProductCode = lsArgument;
                                            try
                                            {
                                                if (!lsProductCode.StartsWith("{"))
                                                {
                                                    lsProductCode = lsProductCode.Substring(lsProductCode.IndexOf("{"));
                                                }
                                            }
                                            catch { }
                                            try
                                            {
                                                if (!lsProductCode.EndsWith("}"))
                                                {
                                                    lsProductCode = lsProductCode.Substring(0, lsProductCode.IndexOf("}") + 1);
                                                }
                                            }
                                            catch { }
                                            lbDoneSomething = true;
                                        }

                                        if (lsArgument.ToLower().Contains(".mst"))
                                        {
                                            lsMST = lsArgument;
                                            lsMST = lsMST.Replace("TRANSFORMS=", "");
                                            lsMST = lsMST.Replace("TRANSFORMS =", "");
                                            lsMST = lsMST.Replace(@"""", "");
                                            lbDoneSomething = true;
                                        }
                                        if (!lbDoneSomething)
                                        {
                                            if (lsArgument.Contains("="))
                                            {
                                                bool lbArgumentIsValid = true;
                                                if (lsArgument.Contains("REBOOT"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.StartsWith("MSI"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.Contains("ALLUSERS"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.Contains("ROOTDRIVE"))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lsArgument.Trim().EndsWith("="))
                                                {
                                                    lbArgumentIsValid = false;
                                                }
                                                if (lbArgumentIsValid)
                                                {
                                                    lsParameters += @", """ + lsArgument + @"""";
                                                }
                                            }
                                        }
                                    }
                                }
                                catch { }

                                if (lsParameters.StartsWith(@", "))
                                {
                                    lsParameters = lsParameters.Substring(2);
                                }

                                writeToLogFile("chkecking if lslastcommand: " + lsMSI + " -> " + lsLastCommand);
                                if (lsMSI != lsLastCommand)
                                {
                                    if (lsMSP != "")
                                    {
                                        loStringBuilder.AppendLine(@"# Patch installieren");
                                        //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                        loStringBuilder.AppendLine(@"$Setup += @{");
                                        loStringBuilder.AppendLine(@"	Type = ""msp"";");
                                        loStringBuilder.AppendLine(@"	Name = """ + lsMSP + @""";");
                                        loStringBuilder.AppendLine(@"}");
                                    }
                                    else
                                    {
                                        if (lsLastProductCode == "" || lsLastProductCode != lsProductCode)
                                        {
                                            loStringBuilder.AppendLine(@"# Software installieren");
                                            //loStringBuilder.AppendLine(@"# " + loRow["param"].ToString());
                                            loStringBuilder.AppendLine(@"$Setup += @{");
                                            loStringBuilder.AppendLine(@"	Type = ""msi"";");
                                            loStringBuilder.AppendLine(@"	Name = """ + lsMSI + @""";");
                                            if (lsMST != "")
                                            {
                                                loStringBuilder.AppendLine(@"	Transforms = """ + lsMST + @""";");
                                            }
                                            if (lsParameters != "")
                                            {
                                                loStringBuilder.AppendLine(@"	Properties = @(" + lsParameters + @");");
                                            }
                                            if (lsProductCode != "")
                                            {
                                                lsLastProductCode = lsProductCode;
                                                loStringBuilder.AppendLine(@"	GUID = """ + lsProductCode + @""";");
                                            }
                                            else
                                            {
                                                loStringBuilder.AppendLine(@"	GUID = """ + lsUninstallKey + @""";");
                                            }
                                            loStringBuilder.AppendLine(@"");
                                            loStringBuilder.AppendLine(@"}");
                                        }
                                    }
                                }
                                lsLastCommand = lsMSI;
                            }
                            break;
                        }

                    case "removefile":
                        {
                            switch (lsTableType)
                            {
                                case "uninstall":
                                    {
                                        loStringBuilder.AppendLine(@"# Dateien loeschen ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            UninstallName = ""Remove-Item""; ");
                                        loStringBuilder.AppendLine(@"            UninstallParameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               UninstallAcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                                default:
                                    {
                                        loStringBuilder.AppendLine(@"# Dateien loeschen ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            Name = ""Remove-Item""; ");
                                        loStringBuilder.AppendLine(@"            Parameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               AcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                            }


                        }
                        lsLastCommand = "";
                        break;

                    case "copyfile":
                        {
                            switch (lsTableType)
                            {
                                case "uninstall":
                                    {
                                        loStringBuilder.AppendLine(@"# Dateien kopieren ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            UninstallName = ""Copy-Item""; ");
                                        loStringBuilder.AppendLine(@"            UninstallParameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Destination"", (""'" + loRow["param"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               UninstallAcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                                default:
                                    {
                                        loStringBuilder.AppendLine(@"# Dateien kopieren ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            Name = ""Copy-Item""; ");
                                        loStringBuilder.AppendLine(@"            Parameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Destination"", (""'" + loRow["param"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               AcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                            }


                        }
                        lsLastCommand = "";
                        break;

                    case "copyfolder":
                        {
                            switch (lsTableType)
                            {
                                case "uninstall":
                                    {
                                        loStringBuilder.AppendLine(@"# Folder kopieren ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            UninstallName = ""Copy-Item""; ");
                                        loStringBuilder.AppendLine(@"            UninstallParameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Destination"", (""'" + loRow["param"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Recurse"", ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               UninstallAcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                                default:
                                    {
                                        loStringBuilder.AppendLine(@"# Folder kopieren ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            Name = ""Copy-Item""; ");
                                        loStringBuilder.AppendLine(@"            Parameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Destination"", (""'" + loRow["param"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Recurse"", ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               AcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                            }


                        }
                        lsLastCommand = "";
                        break;

                    case "removefolder":
                        {
                            switch (lsTableType)
                            {
                                case "uninstall":
                                    {
                                        loStringBuilder.AppendLine(@"# Verzeichnisse loeschen ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            UninstallName = ""Remove-Item""; ");
                                        loStringBuilder.AppendLine(@"            UninstallParameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Recurse"" ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               UninstallAcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                                default:
                                    {
                                        loStringBuilder.AppendLine(@"# Verzeichnisse loeschen ");
                                        loStringBuilder.AppendLine(@"    $Setup += @{ ");
                                        loStringBuilder.AppendLine(@"            Type = ""ps""; ");
                                        loStringBuilder.AppendLine(@"            Name = ""Remove-Item""; ");
                                        loStringBuilder.AppendLine(@"            Parameters = @( ");
                                        loStringBuilder.AppendLine(@"                ""-Path"", (""'" + loRow["command"].ToString() + @"'""), ");
                                        loStringBuilder.AppendLine(@"                ""-Recurse"" ");
                                        loStringBuilder.AppendLine(@"                ""-Force"" ");
                                        loStringBuilder.AppendLine(@"            ); ");
                                        loStringBuilder.AppendLine(@"               AcceptExitCodes = @(1); ");
                                        loStringBuilder.AppendLine(@"    } ");
                                    }
                                    break;
                            }
                        }
                        lsLastCommand = "";
                        break;

                    case "addregkey":
                        {
                            string lsRoot = "HKLM";
                            string lsPath = loRow["command"].ToString();
                            string lsName = "";
                            string lsRegType = "";
                            string lsValue = "";
                            try
                            {
                                lsPath = lsPath.Substring(lsPath.IndexOf(@"\") + 1);
                            }
                            catch { }
                            try
                            {
                                lsPath = lsPath.Substring(0, lsPath.IndexOf(Convert.ToChar("|")));
                            }
                            catch { }
                            try
                            {
                                lsName = loRow["command"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCU"))
                            {
                                lsRoot = "HKCU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKU"))
                            {
                                lsRoot = "HKU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCR"))
                            {
                                lsRoot = "HKCR";
                            }
                            try
                            {
                                lsValue = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            try
                            {
                                lsRegType = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                            }
                            catch { }
                            if (lsRegType != "")
                            {
                                loStringBuilder.AppendLine(@"$Setup += @{");
                                loStringBuilder.AppendLine(@"	Type = ""reg"";");
                                loStringBuilder.AppendLine(@"	Path = """ + lsRoot + @":\" + lsPath + @""";");
                                loStringBuilder.AppendLine(@"	Value = """ + lsName + @""";");
                                loStringBuilder.AppendLine(@"	Data = """ + lsValue + @""";");
                                loStringBuilder.AppendLine(@"	ValueType = ""String"";");
                                loStringBuilder.AppendLine(@"}");
                            }
                        }
                        lsLastCommand = "";
                        break;

                    case "addshortcut":
                        {
                            if (loRow["param"].ToString().Contains("|"))
                            {
                                string lsDestination = "";
                                string lsWorkDir = "";
                                string lsIconPath = "";
                                string lsIconIndex = "0";
                                string lsDescription = "";
                                try
                                {
                                    lsDestination = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                catch { }
                                try
                                {
                                    lsWorkDir = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                try
                                {
                                    lsIconPath = loRow["param"].ToString().Split(Convert.ToChar("|"))[2];
                                }
                                catch { }
                                try
                                {
                                    lsIconIndex = loRow["param"].ToString().Split(Convert.ToChar("|"))[3];
                                }
                                catch { }
                                try
                                {
                                    lsDescription = loRow["param"].ToString().Split(Convert.ToChar("|"))[4];
                                }
                                catch { }
                                string lsMyExe = "";
                                string lsMyPath = "";
                                try
                                {
                                    lsMyExe = System.IO.Path.GetFileName(loRow["command"].ToString());
                                }
                                catch { }
                                try
                                {
                                    lsMyPath = System.IO.Path.GetDirectoryName(loRow["command"].ToString());
                                }
                                catch { }
                                loStringBuilder.AppendLine(@"# * PowerShell example to create shortcut:");
                                loStringBuilder.AppendLine(@"$Setup += @{");
                                loStringBuilder.AppendLine(@"	Type = ""ps"";");
                                loStringBuilder.AppendLine(@"	Name = ""Create-Shortcut"";");
                                loStringBuilder.AppendLine(@"	Parameters = @(");
                                loStringBuilder.AppendLine(@"		""-Path"",				""`""" + lsMyPath + @"`"""",	# directory path to the shortcut (mandatory)");
                                loStringBuilder.AppendLine(@"		""-Name"",				""`""" + lsMyExe + @"`"""",						# filename of the shortcut (mandatory)");
                                loStringBuilder.AppendLine(@"		""-Target"",				""`""" + lsDestination + @"`""""	# target of the shortcut (mandatory)");
                                //loStringBuilder.AppendLine(@"		""-Arguments"",			""`""N:\my.txt`"""",								# arguments to the target (optional)");
                                //loStringBuilder.AppendLine(@"		""-Icon"",				""`"""" + $CommonDirs[""System32_Current""] + ""\shell32.dll,152`"""",	# icon (optional)");
                                //loStringBuilder.AppendLine(@"		""-WorkingDirectory"",	""`""N:\`"""",										# working directory of target (optional)");
                                //loStringBuilder.AppendLine(@"		""-Description"",			""`""A test shortcut.`"""",							# description (optional)");
                                //loStringBuilder.AppendLine(@"		""-HotKey"",				""`""CTRL+ALT+N`""""								# hot key (optional)");
                                loStringBuilder.AppendLine(@"	);");
                                loStringBuilder.AppendLine(@"}");
                            }

                        }
                        lsLastCommand = "";
                        break;

                    case "expandfolder":
                        lsLastCommand = "";
                        break;

                    case "checkarchitecture":
                        loStringBuilder.AppendLine("$os_type = (Get-WmiObject -Class Win32_ComputerSystem).SystemType -match '(x64)'");
                        switch (loRow["command"].ToString().ToLower())
                        {
                            case "x86":
                                loStringBuilder.AppendLine("if($os_type -eq $false) {");
                                break;
                            case "x64":
                                loStringBuilder.AppendLine("if($os_type -eq $true) {");
                                break;
                        }
                        lsLastCommand = "";
                        break;

                    case "checkregistry":
                        if (lsLastCommand != "checkregistry")
                        {
                            loStringBuilder.AppendLine("{");
                        }
                        lsLastCommand = "checkregistry";
                        break;

                    case "endcheck":
                        if (lsLastCommand != "endcheck")
                        {
                            loStringBuilder.AppendLine("}");
                        }
                        lsLastCommand = "endcheck";
                        break;

                    case "killprocess":
                        switch (lsTableType)
                        {
                            case "uninstall":
                                loStringBuilder.AppendLine(@"# * Kill process:");
                                loStringBuilder.AppendLine(@"$Setup += @{");
                                loStringBuilder.AppendLine(@"	Type = ""ps"";");
                                loStringBuilder.AppendLine(@"	UninstallName = ""Stop-Process"";");
                                loStringBuilder.AppendLine(@"	UninstallParameters = @(");
                                loStringBuilder.AppendLine(@"		""-Name"",		""`""" + loRow["command"].ToString() + @"`"""",");
                                loStringBuilder.AppendLine(@"      "" -Force""");
                                loStringBuilder.AppendLine(@"	);");
                                loStringBuilder.AppendLine(@"   UninstallAcceptExitCodes = @(0, 1);");
                                loStringBuilder.AppendLine(@"}");
                                break;
                            default:
                                loStringBuilder.AppendLine(@"# * Kill process:");
                                loStringBuilder.AppendLine(@"$Setup += @{");
                                loStringBuilder.AppendLine(@"	Type = ""ps"";");
                                loStringBuilder.AppendLine(@"	Name = ""Stop-Process"";");
                                loStringBuilder.AppendLine(@"	Parameters = @(");
                                loStringBuilder.AppendLine(@"		""-Name"",		""`""" + loRow["command"].ToString() + @"`"""",");
                                loStringBuilder.AppendLine(@"      "" -Force""");
                                loStringBuilder.AppendLine(@"	);");
                                loStringBuilder.AppendLine(@"   AcceptExitCodes = @(0, 1);");
                                loStringBuilder.AppendLine(@"}");
                                break;
                        }
                        lsLastCommand = "";
                        break;
                }
            }



            loStringBuilder = loStringBuilder.Replace("{" + Environment.NewLine + "}", "");


        }

        public static string GetMSIXScript(string lsType)
        {
            StringBuilder tw = new StringBuilder();

            tw.AppendLine(@"$rootpath = $PSScriptRoot");
            tw.AppendLine(@"$arr = [System.IO.Directory]::EnumerateFiles($rootpath,'*.msix','TopDirectoryOnly')");
            tw.AppendLine(@"$mainpackage = ''");
            tw.AppendLine(@"");
            tw.AppendLine(@"if($arr.Count -eq 1)");
            tw.AppendLine(@"{");
            tw.AppendLine(@" $mainpackage = $arr[0]");
            tw.AppendLine(@"}");
            tw.AppendLine(@"if($arr.Count -lt 1)");
            tw.AppendLine(@"{");
            tw.AppendLine(@" if($arr[0].Length > $arr[1].Length)");
            tw.AppendLine(@" {");
            tw.AppendLine(@" $mainpackage = $arr[0]");
            tw.AppendLine(@" }");
            tw.AppendLine(@" else");
            tw.AppendLine(@" {");
            tw.AppendLine(@" $mainpackage = $arr[1]");
            tw.AppendLine(@" }");
            tw.AppendLine(@"}");

            switch (lsType)
            {
                case "uninstall":
                    tw.AppendLine(@"$packagename = ''");
                    tw.AppendLine(@"");
                    tw.AppendLine(@"if ([System.IO.File]::Exists($mainpackage))");
                    tw.AppendLine(@"{");
                    tw.AppendLine(@"try");
                    tw.AppendLine(@"{");
                    tw.AppendLine(@"	[Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')");
                    tw.AppendLine(@"	$zipstream = [System.IO.Compression.ZipFile]::OpenRead($mainpackage)");
                    tw.AppendLine(@"");
                    tw.AppendLine(@"    foreach($zippedfile in $zipstream.Entries)");
                    tw.AppendLine(@"    {");
                    tw.AppendLine(@"        if($zippedfile.Name.ToLower() -eq 'appxmanifest.xml')");
                    tw.AppendLine(@"        {");
                    tw.AppendLine(@"            $memorystream = New-Object System.IO.MemoryStream");
                    tw.AppendLine(@"            $file = $zippedfile.Open()");
                    tw.AppendLine(@"            $file.CopyTo($memorystream)");
                    tw.AppendLine(@"            $file.Close");
                    tw.AppendLine(@"            $memorystream.Position=0;");
                    tw.AppendLine(@"            $streamread = New-Object System.IO.StreamReader($memorystream)");
                    tw.AppendLine(@"            $stringstream = $streamread.ReadToEnd()");
                    tw.AppendLine(@"            $packagename=$stringstream.Substring($stringstream.IndexOf('Identity Name=""') + 'Identity Name=""'.Length)");
                    tw.AppendLine(@"            $packagename=$packagename.Substring(0,$packagename.IndexOf('""'))");
                    tw.AppendLine(@"            break;");
                    tw.AppendLine(@"        }");
                    tw.AppendLine(@"    }");
                    tw.AppendLine(@"}");
                    tw.AppendLine(@"catch { }");
                    tw.AppendLine(@"}");
                    tw.AppendLine(@"if ($packagename.Length -gt 0) {");
                    tw.AppendLine(@"        Remove-AppxProvisionedPackage -Online -PackageName $packagename -AllUsers");
                    tw.AppendLine(@"}");
                    break;
                default:
                    tw.AppendLine(@"        Add-AppxProvisionedPackage -Online -PackagePath $mainpackage");
                    break;
            }

            return tw.ToString();

        }


        public static DataTable ConvertXMLToDataTable(string lsType, string lsDeployXML, bool lbNotDeep)
        {

            DataTable loTable = new DataTable();
            loTable.Columns.Add("id", typeof(Guid));
            loTable.Columns.Add("actiontype");
            loTable.Columns.Add("order", typeof(int));
            loTable.Columns.Add("level", typeof(int));
            loTable.Columns.Add("command");
            loTable.Columns.Add("param");
            loTable.Columns.Add("varname");
            loTable.Columns.Add("pidvarname");
            loTable.Columns.Add("options");
            loTable.Columns.Add("path");
            loTable.Columns.Add("name");
            loTable.Columns.Add("condition");
            loTable.Columns.Add("conditionid", typeof(string));

            string lsLastConditionId = "";

            string[] lsLevel = new string[10] { "", "", "", "", "", "", "", "", "", "" };
            string[] lsConditionId = new string[] { "", "", "", "", "", "", "", "", "", "" };
            for (int i = 0; i < 10; i++)
            {
                lsConditionId[i] = Guid.NewGuid().ToString();
            }

            writeToLogFile("does <" + lsType + "> exist in " + lsDeployXML);
            if (lsDeployXML.Contains("<" + lsType + ">"))
            {

                try
                {
                    XDocument loDoc = XDocument.Parse(lsDeployXML);

                    writeToLogFile("parsing successful for " + lsType);

                    int liOrder = 0;
                    string lsOldPath = "";

                    foreach (XElement install in loDoc.Root
                                        .Element(lsType)
                                        .Elements("tasks"))
                    {

                        int liPreviousLevel = 0;

                        foreach (XNode element in install.DescendantNodes())
                        {
                            switch (element.NodeType)
                            {
                                case XmlNodeType.Comment:
                                    XComment mycomment = (XComment)element;
                                    if (mycomment.Value.Contains(" combined: start"))
                                    {
                                        liOrder += 1;
                                        DataRow loNewRow = loTable.NewRow();
                                        loNewRow["id"] = Guid.NewGuid().ToString();
                                        loNewRow["actiontype"] = "combinedsection";
                                        loNewRow["order"] = liOrder;
                                        loNewRow["level"] = 0;
                                        loNewRow["command"] = "";
                                        loNewRow["param"] = mycomment.Value;
                                        loNewRow["conditionid"] = "";
                                        loTable.Rows.Add(loNewRow);
                                    }
                                    if (mycomment.Value.Contains(" combined: end"))
                                    {
                                        liOrder += 1;
                                        DataRow loNewRow = loTable.NewRow();
                                        loNewRow["id"] = Guid.NewGuid().ToString();
                                        loNewRow["actiontype"] = "combinedsection";
                                        loNewRow["order"] = liOrder;
                                        loNewRow["level"] = 0;
                                        loNewRow["command"] = "";
                                        loNewRow["param"] = mycomment.Value;
                                        loNewRow["conditionid"] = "";
                                        loTable.Rows.Add(loNewRow);
                                    }
                                    break;
                                case XmlNodeType.Element:
                                    XElement myelement = (XElement)element;
                                    writeToLogFile("element is " + myelement.Name);
                                    writeToLogFile("pathactual is " + GetMyPath(myelement));
                                    writeToLogFile("pathold is " + lsOldPath);
                                    string lsXMLPath = GetMyPath(myelement).ToLower();
                                    string lsConditionPath = "";
                                    foreach (string lsCondition in lsXMLPath.Split(Convert.ToChar(@"\")))
                                    {
                                        if (lsCondition.ToLower() == "checkvariable")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                        if (lsCondition.ToLower() == "checkregistry")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                        if (lsCondition.ToLower() == "checkfile")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                        if (lsCondition.ToLower() == "checkfolder")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                        if (lsCondition.ToLower() == "checkvariable")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                        if (lsCondition.ToLower() == "x86")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                        if (lsCondition.ToLower() == "x64")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                        if (lsCondition.ToLower() == "languagecode")
                                        {
                                            lsConditionPath += lsCondition.ToString() + @"\";
                                        }
                                    }

                                    writeToLogFile("pathcondition is " + lsConditionPath);
                                    writeToLogFile("level is " + (lsConditionPath.Split(Convert.ToChar(@"\")).Length - 1).ToString());

                                    int liNodeLevel = lsConditionPath.Split(Convert.ToChar(@"\")).Length - 1;

                                    if (liNodeLevel < liPreviousLevel)
                                    {
                                        //find condition:
                                        string lsMyConditionId = loTable.Select("conditionid<>''", "order DESC")[0]["conditionid"].ToString();
                                        string lsMyCondition = "";
                                        foreach (DataRow loRow in loTable.Select("conditionid='" + lsMyConditionId + "' and actiontype='checkarchitecture'"))
                                        {
                                            lsMyCondition = loRow["condition"].ToString();
                                        }


                                        liOrder += 1;
                                        DataRow loNewRow = loTable.NewRow();
                                        loNewRow["id"] = Guid.NewGuid().ToString();
                                        loNewRow["actiontype"] = "endcheck";
                                        loNewRow["order"] = liOrder;
                                        loNewRow["level"] = liPreviousLevel;
                                        loNewRow["command"] = "";
                                        loNewRow["param"] = @"based on previous";
                                        loNewRow["conditionid"] = lsMyConditionId;
                                        loNewRow["condition"] = lsMyCondition;
                                        //lsConditionId[liNodeLevel] = Guid.NewGuid().ToString();
                                        loTable.Rows.Add(loNewRow);
                                        lsLevel[liPreviousLevel] = "";
                                    }

                                    switch (myelement.Name.ToString().ToLower())
                                    {
                                        case "checkregistry":
                                        case "checkfile":
                                        case "checkfolder":
                                            {
                                                if ((lsLevel[liNodeLevel]) != "")
                                                {
                                                    liOrder += 1;
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "endcheck";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = "";
                                                    loNewRow["param"] = "based on " + myelement.Name.ToString().ToLower();
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];
                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = "";
                                                }

                                                try
                                                {
                                                    lsLastConditionId = lsConditionId[liNodeLevel];
                                                }
                                                catch { }
                                                lsConditionId[liNodeLevel] = Guid.NewGuid().ToString();

                                                string lsPath = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                }

                                                string lsName = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "name")
                                                    {
                                                        lsName = loAttribute.Value;
                                                    }
                                                }

                                                string lsValue = "";
                                                string lsContentRequired = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "contentrequired")
                                                    {
                                                        lsContentRequired = loAttribute.Value;
                                                    }
                                                }

                                                liOrder += 1;
                                                {
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = myelement.Name.ToString();
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = lsValue;
                                                    loNewRow["param"] = lsName;
                                                    loNewRow["path"] = lsPath;
                                                    loNewRow["options"] = lsContentRequired;
                                                    if (String.Concat(myelement.Nodes()).ToString().Trim().StartsWith("<exists>"))
                                                    {
                                                        loNewRow["condition"] = "exists";
                                                    }
                                                    if (String.Concat(myelement.Nodes()).ToString().Trim().StartsWith("<notexists>"))
                                                    {
                                                        loNewRow["condition"] = "notexists";
                                                    }
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = myelement.Name.ToString();
                                                }
                                            }
                                            break;
                                        case "x86":
                                        case "x64":
                                            {
                                                if ((lsLevel[liNodeLevel]) != "")
                                                {
                                                    liOrder += 1;
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "endcheck";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = "";
                                                    loNewRow["param"] = "based on " + myelement.Name.ToString().ToLower() + " - " + lsLevel[liNodeLevel];
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];
                                                    loNewRow["condition"] = lsLevel[liNodeLevel];
                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = "";
                                                }
                                                liOrder += 1;
                                                lsLastConditionId = lsConditionId[liNodeLevel];
                                                lsConditionId[liNodeLevel] = Guid.NewGuid().ToString();
                                                {
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "checkarchitecture";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = myelement.Name.ToString();
                                                    loNewRow["param"] = "";
                                                    loNewRow["path"] = "";
                                                    loNewRow["condition"] = myelement.Name.ToString().ToLower();
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = myelement.Name.ToString();
                                                }
                                            }
                                            break;
                                        case "languagecode":
                                            {
                                                string lsValue = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                }

                                                if ((lsLevel[liNodeLevel]) != "")
                                                {
                                                    liOrder += 1;
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "endcheck";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = "";
                                                    loNewRow["param"] = "based on " + myelement.Name.ToString().ToLower() + " - " + lsLevel[liNodeLevel];
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];
                                                    loNewRow["condition"] = lsLevel[liNodeLevel];
                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = "";
                                                }
                                                liOrder += 1;
                                                lsLastConditionId = lsConditionId[liNodeLevel];
                                                lsConditionId[liNodeLevel] = Guid.NewGuid().ToString();

                                                {
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "checklanguagecode";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = lsValue;
                                                    loNewRow["param"] = "";
                                                    loNewRow["path"] = "";
                                                    loNewRow["condition"] = myelement.Name.ToString().ToLower();
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = myelement.Name.ToString();
                                                }
                                            }
                                            break;
                                        case "checkvariable":
                                            {
                                                if ((lsLevel[liNodeLevel]) != "")
                                                {
                                                    liOrder += 1;
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "endcheck";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = "";
                                                    loNewRow["param"] = "based on " + myelement.Name.ToString().ToLower() + " - " + lsLevel[liNodeLevel];
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];
                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = "";
                                                }

                                                string lsVariable = "";
                                                string lsValue = "";

                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "var")
                                                    {
                                                        lsVariable = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                }


                                                liOrder += 1;
                                                lsLastConditionId = lsConditionId[liNodeLevel];
                                                lsConditionId[liNodeLevel] = Guid.NewGuid().ToString();
                                                {
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = myelement.Name.ToString();
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = lsVariable;
                                                    loNewRow["param"] = lsValue;
                                                    loNewRow["path"] = "";
                                                    if (String.Concat(myelement.Nodes()).ToString().ToLower().Trim().StartsWith("<exists>"))
                                                    {
                                                        loNewRow["condition"] = "exists";
                                                    }
                                                    if (String.Concat(myelement.Nodes()).ToString().ToLower().Trim().StartsWith("<notexists>"))
                                                    {
                                                        loNewRow["condition"] = "notexists";
                                                    }
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                    loTable.Rows.Add(loNewRow);
                                                    lsLevel[liNodeLevel] = myelement.Name.ToString();
                                                }
                                            }
                                            break;




                                        case "execute":
                                        case "executenowait":
                                            {
                                                //writeToLogFile(GetPath(myelement));
                                                string lsCommand = "";
                                                string lsOptions = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsCommand = loAttribute.Value;
                                                    }
                                                    try
                                                    {
                                                        if (loAttribute.Name.ToString().ToLower() == "windowstyle")
                                                        {
                                                            lsOptions = loAttribute.Value;
                                                        }
                                                    }
                                                    catch { }
                                                }
                                                string lsParameters = "";
                                                foreach (XElement loParameter in myelement.Descendants("parameter"))
                                                {
                                                    lsParameters = lsParameters + loParameter.Value.ToString() + " ";
                                                }
                                                lsParameters = lsParameters.Trim();
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                if (myelement.Name.ToString().ToLower() == "execute")
                                                {
                                                    loNewRow["actiontype"] = "LaunchProcess";
                                                }
                                                else
                                                {
                                                    loNewRow["actiontype"] = "LaunchProcessNoWait";
                                                }
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsCommand;
                                                loNewRow["param"] = lsParameters;
                                                loNewRow["options"] = lsOptions;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        //case "executenowait":
                                        //    {
                                        //        //writeToLogFile(GetPath(myelement));
                                        //        string lsCommand = "";
                                        //        foreach (XAttribute loAttribute in myelement.Attributes())
                                        //        {
                                        //            if (loAttribute.Name.ToString().ToLower() == "path")
                                        //            {
                                        //                lsCommand = loAttribute.Value;
                                        //            }
                                        //        }
                                        //        string lsParameters = "";
                                        //        foreach (XElement loParameter in myelement.Descendants("parameter"))
                                        //        {
                                        //            lsParameters = lsParameters + loParameter.Value.ToString() + " ";
                                        //        }
                                        //        lsParameters = lsParameters.Trim();
                                        //        liOrder += 1;
                                        //        DataRow loNewRow = loTable.NewRow();
                                        //        loNewRow["id"] = Guid.NewGuid().ToString();
                                        //        loNewRow["actiontype"] = "LaunchProcessNoWait";
                                        //        loNewRow["order"] = liOrder;
                                        //        loNewRow["level"] = liNodeLevel;
                                        //        loNewRow["command"] = lsCommand;
                                        //        loNewRow["param"] = lsParameters;
                                        //        loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                        //        loTable.Rows.Add(loNewRow);
                                        //    }
                                        //    break;

                                        case "sleep":
                                            {
                                                string lsCommand = "";
                                                string lsParameters = "";

                                                lsCommand = "sleep";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "seconds")
                                                    {
                                                        lsParameters = loAttribute.Value;
                                                    }
                                                }
                                                lsParameters = lsParameters.Trim();
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "sleep";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsCommand;
                                                loNewRow["param"] = lsParameters;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "killprocess":
                                            {
                                                string lsCommand = "";
                                                string lsParameters = "";

                                                //lsCommand = "taskkill";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "name")
                                                    {
                                                        //lsParameters = @"""" + loAttribute.Value + @""" /F /IM cmd.exe /T";
                                                        lsCommand = loAttribute.Value;
                                                    }
                                                }
                                                lsParameters = lsParameters.Trim();
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "killprocess";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsCommand;
                                                loNewRow["param"] = lsParameters;
                                                loNewRow["options"] = "hidden";
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        //case "abort":
                                        //    {

                                        //        liOrder += 1;
                                        //        DataRow loNewRow = loTable.NewRow();
                                        //        loNewRow["id"] = Guid.NewGuid().ToString();
                                        //        loNewRow["actiontype"] = "abort";
                                        //        loNewRow["order"] = liOrder;
                                        //        loNewRow["level"] = liNodeLevel;
                                        //        loNewRow["command"] = "abort";
                                        //        loNewRow["param"] = "";
                                        //        loNewRow["options"] = "";
                                        //        loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                        //        loTable.Rows.Add(loNewRow);
                                        //    }
                                        //    break;

                                        case "removeexistingsoftware":
                                            {
                                                string lsCommand = "";
                                                string lsParameters = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "uninstalltype")
                                                    {
                                                        lsCommand = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsParameters = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "removeexistingsoftware";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsCommand;
                                                loNewRow["param"] = lsParameters;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];
                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "msiexec":
                                            {
                                                string lsCommand = "";
                                                string lsParameters = "";
                                                foreach (XElement loParameter in myelement.Descendants("parameter"))
                                                {
                                                    writeToLogFile("msiexec: parameter is " + loParameter.Value.ToString());
                                                    bool lbDoneSomething = false;
                                                    if (loParameter.Value.ToLower().Trim() == "/i")
                                                    {
                                                        lsCommand = "/i";
                                                        lbDoneSomething = true;
                                                    }
                                                    if (loParameter.Value.ToLower().Trim() == "/x")
                                                    {
                                                        lsCommand = "/x";
                                                        lbDoneSomething = true;
                                                    }
                                                    if (loParameter.Value.ToLower().Trim() == "/p")
                                                    {
                                                        lsCommand = "/p";
                                                        lbDoneSomething = true;
                                                    }
                                                    if (loParameter.Value.ToLower().Trim().StartsWith("/f"))
                                                    {
                                                        lsCommand = loParameter.Value.Trim();
                                                        lbDoneSomething = true;
                                                    }
                                                    if (!lbDoneSomething)
                                                    {
                                                        lsParameters = lsParameters + loParameter.Value.ToString() + " ";
                                                    }
                                                }
                                                lsParameters = lsParameters.Trim();
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "msiexec";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsCommand;
                                                loNewRow["param"] = lsParameters;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);

                                            }
                                            break;

                                        case "copyfile":
                                            {
                                                string lsSource = "";
                                                string lsDestination = "";
                                                string lsOverwrite = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "source")
                                                    {
                                                        lsSource = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "destination")
                                                    {
                                                        lsDestination = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "overwrite")
                                                    {
                                                        lsOverwrite = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "copyfile";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsSource;
                                                loNewRow["param"] = lsDestination;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "addfolder":
                                            {
                                                string lsPath = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "addfolder";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "removeshortcuts":
                                            {
                                                string lsPath = "";
                                                string lsFileName = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "filename")
                                                    {
                                                        lsFileName = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "removeshortcuts";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                loNewRow["param"] = lsFileName;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "setvariable":
                                            {
                                                string lsVariable = "";
                                                string lsValue = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "var")
                                                    {
                                                        lsVariable = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "setvariable";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsVariable;
                                                loNewRow["param"] = lsValue;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "getvariablefromregistry":
                                            {
                                                string lsVariable = "";
                                                string lsPath = "";
                                                string lsName = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "var")
                                                    {
                                                        lsVariable = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "name")
                                                    {
                                                        lsName = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "getvariablefromregistry";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsVariable;
                                                loNewRow["param"] = lsPath + "|" + lsName;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "expandfolder":
                                            {
                                                string lsSource = "";
                                                string lsDestination = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "source")
                                                    {
                                                        lsSource = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "destination")
                                                    {
                                                        lsDestination = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;

                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "expandfolder";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsSource;
                                                loNewRow["param"] = lsDestination;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "copyfolder":
                                            {
                                                string lsSource = "";
                                                string lsDestination = "";
                                                string lsOverwrite = "";

                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "source")
                                                    {
                                                        lsSource = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "destination")
                                                    {
                                                        lsDestination = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "overwrite")
                                                    {
                                                        lsOverwrite = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "copyfolder";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsSource;
                                                loNewRow["param"] = lsDestination;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "removefile":
                                            {
                                                string lsSource = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsSource = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "removefile";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsSource;
                                                loNewRow["param"] = "";
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                bool lbValid = true;
                                                if (loNewRow["command"].ToString().ToLower() == "machvars.wms")
                                                {
                                                    lbValid = false;
                                                }
                                                if (lbValid)
                                                {
                                                    loTable.Rows.Add(loNewRow);
                                                }
                                            }
                                            break;

                                        case "removefolder":
                                            {
                                                string lsSource = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsSource = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "removefolder";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsSource;
                                                loNewRow["param"] = "";
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "addpath":
                                            {
                                                string lsValue = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "addpath";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsValue;
                                                loNewRow["param"] = "";
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];
                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "removepath":
                                            {
                                                string lsValue = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "removepath";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsValue;
                                                loNewRow["param"] = "";
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];
                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "addregkey":
                                            {
                                                string lsPath = "";
                                                string lsName = "";
                                                string lsValue = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "name")
                                                    {
                                                        lsName = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "type")
                                                    {
                                                        lsType = loAttribute.Value;
                                                    }
                                                }
                                                if (lsType != "")
                                                {
                                                    liOrder += 1;
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "addregkey";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = lsPath + @"|" + lsName;
                                                    loNewRow["param"] = lsType + "|" + lsValue;
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                    loTable.Rows.Add(loNewRow);
                                                }
                                            }
                                            break;

                                        case "abort":
                                            {
                                                string lsExitCode = "0";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "exitcode")
                                                    {
                                                        lsExitCode = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "abort";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsExitCode;
                                                loNewRow["param"] = "";
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "removeregkey":
                                            {
                                                string lsPath = "";
                                                string lsName = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "name")
                                                    {
                                                        lsName = loAttribute.Value;
                                                    }
                                                }
                                                if (lsType != "")
                                                {
                                                    liOrder += 1;
                                                    DataRow loNewRow = loTable.NewRow();
                                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                                    loNewRow["actiontype"] = "removeregkey";
                                                    loNewRow["order"] = liOrder;
                                                    loNewRow["level"] = liNodeLevel;
                                                    loNewRow["command"] = lsPath + @"|" + lsName;
                                                    loNewRow["param"] = "";
                                                    loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                    loTable.Rows.Add(loNewRow);
                                                }
                                            }
                                            break;

                                        case "addshortcut":
                                            {
                                                string lsPath = "";
                                                string lsDestination = "";
                                                string lsWorkDir = "";
                                                string lsIconPath = "";
                                                string lsIconIndex = "0";
                                                string lsDescription = "";
                                                string lsArguments = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "destination")
                                                    {
                                                        lsDestination = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "workdir")
                                                    {
                                                        lsWorkDir = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "iconpath")
                                                    {
                                                        lsIconPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "iconindex")
                                                    {
                                                        lsIconIndex = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "description")
                                                    {
                                                        lsDescription = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "arguments")
                                                    {
                                                        lsArguments = loAttribute.Value;
                                                    }
                                                }

                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "addshortcut";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                writeToLogFile("addshortcut: workdir " + lsWorkDir);
                                                writeToLogFile("addshortcut: iconpath " + lsIconPath);
                                                writeToLogFile("addshortcut: arguments " + lsArguments);
                                                string lsParam = lsDestination;
                                                if (lsWorkDir != "" || lsIconPath != "" || lsArguments != "")
                                                {
                                                    lsParam = lsDestination + "|" + lsWorkDir + "|" + lsIconPath + "|" + lsIconIndex + "|" + lsDescription + "|" + lsArguments;
                                                }
                                                writeToLogFile("addshortcut: param " + lsParam);
                                                loNewRow["param"] = lsParam;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "writeini":
                                            {
                                                string lsIniFile = "";
                                                string lsSection = "";
                                                string lsValue = "";
                                                string lsKey = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "inifile")
                                                    {
                                                        lsIniFile = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "section")
                                                    {
                                                        lsSection = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "key")
                                                    {
                                                        lsKey = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "writeini";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsIniFile;
                                                loNewRow["param"] = lsSection + "|" + lsKey + "|" + lsValue;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "addtext":
                                            {
                                                string lsPath = "";
                                                string lsValue = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "addtext";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                loNewRow["param"] = lsValue;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "removetext":
                                            {
                                                string lsPath = "";
                                                string lsValue = "";
                                                string lsAggressive = "false";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "value")
                                                    {
                                                        lsValue = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "aggressive")
                                                    {
                                                        lsAggressive = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "removetext";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                loNewRow["param"] = lsValue + "|" + lsAggressive;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;

                                        case "replacetext":
                                            {
                                                string lsPath = "";
                                                string lsSource = "";
                                                string lsDestination = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "source")
                                                    {
                                                        lsSource = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "destination")
                                                    {
                                                        lsDestination = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "replacetext";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                loNewRow["param"] = lsSource + "|" + lsDestination;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;


                                        case "addfilepermission":
                                            {
                                                string lsPath = "";
                                                string lsPermission = "";
                                                string lsAccount = "";
                                                string lsReplace = "";
                                                string lsInheritance = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "permission")
                                                    {
                                                        lsPermission = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "account")
                                                    {
                                                        lsAccount = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "replace")
                                                    {
                                                        lsReplace = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "inheritance")
                                                    {
                                                        lsInheritance = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "addfilepermission";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                loNewRow["param"] = lsPermission + "|" + lsAccount + "|" + lsReplace + "|" + lsInheritance;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;


                                        case "removefilepermission":
                                            {
                                                string lsPath = "";
                                                string lsPermission = "";
                                                string lsAccount = "";
                                                string lsInheritance = "";
                                                foreach (XAttribute loAttribute in myelement.Attributes())
                                                {
                                                    if (loAttribute.Name.ToString().ToLower() == "path")
                                                    {
                                                        lsPath = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "permission")
                                                    {
                                                        lsPermission = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "account")
                                                    {
                                                        lsAccount = loAttribute.Value;
                                                    }
                                                    if (loAttribute.Name.ToString().ToLower() == "inheritance")
                                                    {
                                                        lsInheritance = loAttribute.Value;
                                                    }
                                                }
                                                liOrder += 1;
                                                DataRow loNewRow = loTable.NewRow();
                                                loNewRow["id"] = Guid.NewGuid().ToString();
                                                loNewRow["actiontype"] = "removefilepermission";
                                                loNewRow["order"] = liOrder;
                                                loNewRow["level"] = liNodeLevel;
                                                loNewRow["command"] = lsPath;
                                                loNewRow["param"] = lsPermission + "|" + lsAccount + "|" + lsInheritance;
                                                loNewRow["conditionid"] = lsConditionId[liNodeLevel];

                                                loTable.Rows.Add(loNewRow);
                                            }
                                            break;




                                    }



                                    liPreviousLevel = liNodeLevel;

                                    lsOldPath = GetMyPath(myelement);

                                    break;
                            }
                        }

                        for (int liMyLevel = liPreviousLevel; liMyLevel > 0; liMyLevel -= 1)
                        {
                            if (lsLevel[liMyLevel] != "")
                            {
                                if (loTable.Select("actiontype='endcheck' and conditionid='" + lsLastConditionId + "'").Length == 0)
                                {
                                    if (loTable.Select("conditionid='" + lsLastConditionId + "'").Length > 0)
                                    {
                                        liOrder += 1;
                                        DataRow loNewRow = loTable.NewRow();
                                        loNewRow["id"] = Guid.NewGuid().ToString();
                                        loNewRow["actiontype"] = "endcheck";
                                        loNewRow["order"] = liOrder;
                                        loNewRow["level"] = liMyLevel;
                                        loNewRow["command"] = "";
                                        loNewRow["param"] = "from level";
                                        loNewRow["conditionid"] = lsLastConditionId;
                                        loTable.Rows.Add(loNewRow);
                                    }
                                }
                            }
                        }


                    }
                }
                catch (Exception ex) { writeToLogFile("convertxmldata: " + ex.Message); }

                writeToLogFile("now preparing endcheck...");
                //fix last endcheck

                bool lbEcheDonesomething = true;
                do
                {
                    int liOrder = 0;
                    try
                    {
                        liOrder = Convert.ToInt32(loTable.Select("", "order desc")[0]["order"]) + 1;
                    }
                    catch { }
                    writeToLogFile("rows: " + loTable.Rows.Count.ToString());
                    lbEcheDonesomething = false;
                    foreach (DataRow loRow in loTable.Rows)
                    {
                        if (loRow["actiontype"].ToString().ToLower().StartsWith("check"))
                        {
                            writeToLogFile("found command: " + loRow["actiontype"].ToString().ToLower() + "width " + loRow["conditionid"].ToString());
                            writeToLogFile("actiontype='endcheck' and [conditionid]='" + loRow["conditionid"].ToString() + "'");
                            writeToLogFile(loTable.Select("actiontype='endcheck' and [conditionid]='" + loRow["conditionid"].ToString() + "'").Length.ToString());
                            if (loTable.Select("actiontype='endcheck' and [conditionid]='" + loRow["conditionid"].ToString() + "'").Length == 0)
                            {
                                writeToLogFile("missing endcheck for " + loRow["conditionid"].ToString());
                                try
                                {
                                    DataRow loNewRow = loTable.NewRow();
                                    loNewRow["id"] = Guid.NewGuid().ToString();
                                    loNewRow["actiontype"] = "endcheck";
                                    loNewRow["order"] = liOrder;
                                    loNewRow["level"] = Convert.ToInt32(loRow["level"].ToString());
                                    loNewRow["command"] = "";
                                    loNewRow["param"] = "fixed";
                                    loNewRow["conditionid"] = loRow["conditionid"].ToString();
                                    loTable.Rows.Add(loNewRow);
                                    lbEcheDonesomething = true;
                                    break;
                                }
                                catch (Exception exx) { writeToLogFile(exx.Message); }
                            }
                        }
                    }
                } while (lbEcheDonesomething);

                writeToLogFile("end preparing endcheck...");


                if (lbNotDeep)
                {
                    writeToLogFile("looking for 6432node");
                    foreach (DataRow loRow in loTable.Rows)
                    {
                        if (loRow["actiontype"].ToString().ToLower() == "checkregistry")
                        {
                            //if (loTable.Select("actiontype='checkregistry' and [level]<>'" + loRow["level"].ToString() + "'").Length > 1)
                            //{
                            if (loRow["path"].ToString().ToLower().Contains(@"6432node\"))
                            {
                                writeToLogFile("deleting all 6432node: " + loRow["conditionid"].ToString());
                                foreach (DataRow losubrow in loTable.Select("conditionid='" + loRow["conditionid"].ToString() + "'"))
                                {
                                    //if (!losubrow["actiontype"].ToString().ToLower().StartsWith("check") && !losubrow["actiontype"].ToString().ToLower().StartsWith("endcheck") && Convert.ToInt32(losubrow["level"])>1)
                                    if (!(losubrow["actiontype"].ToString().ToLower().Contains("check")) && Convert.ToInt32(losubrow["level"]) > 1)
                                    {
                                        //check within check, keep actions alive...
                                    }
                                    else
                                    {
                                        //writeToLogFile("6432node: looking if conditionid exists: actiontype='checkregistry' and conditionid='" + losubrow["conditionid"].ToString() + "'");



                                        if (losubrow["actiontype"].ToString().ToLower() == "endcheck")
                                        {
                                            if (loTable.Select("actiontype='checkregistry' and conditionid='" + losubrow["conditionid"].ToString() + "'").Length > 0)
                                            {

                                            }
                                            else
                                            {
                                                writeToLogFile("deleted all 6432node: " + loRow["actiontype"].ToString());
                                                losubrow["conditionid"] = "deleted";
                                            }
                                        }
                                        else
                                        {
                                            writeToLogFile("deleted all 6432node: " + loRow["actiontype"].ToString());
                                            losubrow["conditionid"] = "deleted";
                                        }

                                    }
                                }
                            }
                            //}
                        }
                    }
                    loTable.AcceptChanges();
                }


                writeToLogFile("final table: ");
                foreach (DataRow loRow in loTable.Select("", "[order] ASC"))
                {
                    string lsLog = "";
                    foreach (DataColumn loColumn in loTable.Columns)
                    {

                        //lsLog += "[" + loColumn.ColumnName + "]=" + loRow[loColumn.ColumnName] + " ";
                    }
                    lsLog += loRow["order"].ToString() + ":" + loRow["level"].ToString() + "  " + loRow["actiontype"].ToString() + "    " + loRow["command"].ToString() + "   -   " + loRow["param"].ToString() + "   " + loRow["conditionid"].ToString();
                    writeToLogFile(lsLog);
                }
                writeToLogFile("end final table");
            }
            else
            {
                writeToLogFile("could now find section " + lsType + " in xml");
            }

            return loTable;
        }




        public static string GetMyPath(XElement node)
        {
            string path = node.Name.ToString();
            XElement currentNode = node;
            while (currentNode.Parent != null)
            {
                currentNode = currentNode.Parent;
                path = currentNode.Name.ToString() + @"\" + path;
            }
            return path;
        }



        public static string ConvertWindowsVariablesToPowershell(string lsLine)
        {
            string lsReturn = lsLine;
            lsReturn = lsReturn.Replace("%ProgramFiles(x86)%", @"${env:ProgramFiles(x86)}");
            lsReturn = lsReturn.Replace("%programfiles(x86)%", @"${env:ProgramFiles(x86)}");
            lsReturn = lsReturn.Replace("%ProgramFiles(x64)%", @"${env:ProgramFiles}");
            lsReturn = lsReturn.Replace("%programfiles(x64)%", @"${env:ProgramFiles}");
            lsReturn = lsReturn.Replace("%ProgramFiles%", @"${env:ProgramFiles}");
            lsReturn = lsReturn.Replace("%programfiles%", @"${env:ProgramFiles}");
            lsReturn = lsReturn.Replace("%ProgramData%", @"${env:ProgramData}");
            lsReturn = lsReturn.Replace("%PUBLIC%", @"${env:Public}");
            lsReturn = lsReturn.Replace("%USERPROFILE%", @"${env:UserProfile}");
            lsReturn = Regex.Replace(lsReturn, "%USERPROFILE%", @"${env:UserProfile}", RegexOptions.IgnoreCase);
            return lsReturn;
        }



        //public static string GetDeployXMLWithSettings2(string lsDeployXMLContent, string lsRequestString, string lsReleaseName, string lsReleaseComment, string lsLanguages, string lsRealLanguages, string lsArchitectures, string lsMaininstallfolderx86, string lsMaininstallfolderx64, string lsMainExe, string lsProduct, string lsUninstallKey, string lsManufacturer, string lsVersion, string lsSoftwareId, string lsReleaseId, string lsResolvedDownloadName, string lsInternalId, DataTable loParameterTable, string lsTaskSequence, DataTable loPreReqTable)
        //{
        //    //int liMyCounter = 0;

        //    writeToLogFile("getdeployxmlwithsettings: " + lsMaininstallfolderx86);

        //    writeToLogFile("languages=" + lsLanguages);

        //    writeToLogFile(lsDeployXMLContent);




        //    string lsCustomerTemplate = "";
        //    if (lsRequestString.ToLower().Contains("clisources"))
        //    {
        //        lsCustomerTemplate = "kssg";
        //    }

        //    try
        //    {

        //        if (lsManufacturer == "")
        //        {
        //            lsManufacturer = FindBetween(lsDeployXMLContent, "<manufacturer>", "</manufacturer>");
        //        }
        //        if (lsProduct == "")
        //        {
        //            lsProduct = FindBetween(lsDeployXMLContent, "<productname>", "</productname>");
        //        }
        //        if (lsVersion == "")
        //        {
        //            lsVersion = FindBetween(lsDeployXMLContent, "<productversion>", "</productversion>");
        //        }
        //        if (lsReleaseName == "")
        //        {
        //            lsReleaseName = FindBetween(lsDeployXMLContent, "<releaseversion>", "</releaseversion>");
        //        }

        //        string lsTaskSequences = "";
        //        if (lsDeployXMLContent.Contains("start tasksequence:"))
        //        {
        //            for (int i = 0; i < lsDeployXMLContent.Length; i++)
        //            {
        //                try
        //                {
        //                    if (lsDeployXMLContent.Substring(i, "start tasksequence:".Length) == "start tasksequence:")
        //                    {
        //                        lsTaskSequences = lsTaskSequences + lsDeployXMLContent.Substring(i + "start tasksequence:".Length, 9) + ";";
        //                    }
        //                }
        //                catch { }
        //            }
        //        }

        //        writeToLogFile("tasksequences: " + lsTaskSequences);

        //        foreach (string lsMyTaskSequence in lsTaskSequences.Split(Convert.ToChar(";")))
        //        {
        //            if (lsTaskSequence != "")
        //            {
        //                if (lsTaskSequence.Trim() == lsMyTaskSequence.Trim())
        //                {
        //                    writeToLogFile("found tasksequence: " + lsMyTaskSequence.Trim());
        //                    string lsInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                    writeToLogFile("lsInstall: " + lsInstall);
        //                    writeToLogFile("looking for " + "<!-- start tasksequence: " + lsTaskSequence.Trim() + " -->");
        //                    string lsNewInstall = FindBetween(lsInstall, "<!-- start tasksequence: " + lsTaskSequence.Trim() + " -->", "<!-- end tasksequence: " + lsTaskSequence.Trim() + " -->");
        //                    writeToLogFile("lsNewInstall: " + lsNewInstall);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsInstall, "<tasks>" + lsNewInstall + @"</tasks>");
        //                }
        //            }
        //        }



        //        try
        //        {
        //            XmlDocument myxDocument = new XmlDocument();
        //            myxDocument.LoadXml(lsDeployXMLContent);
        //            lsDeployXMLContent = Beautify(myxDocument);
        //        }
        //        catch (Exception exx) { writeToLogFile("beautify failed: " + exx.Message); }


        //        writeToLogFile("after beautify: ");

        //        writeToLogFile(lsDeployXMLContent);

        //        string lsRaiffeisenPackageId = "00000000";
        //        try
        //        {
        //            lsRaiffeisenPackageId = "0" + FindBetween(lsDeployXMLContent, "SCCMOriginalFolder=", "-->").Substring(1, 7);
        //        }
        //        catch { }
        //        if (lsReleaseComment.Length == 8)
        //        {
        //            lsRaiffeisenPackageId = lsReleaseComment;
        //        }
        //        if (lsInternalId.Length == 8)
        //        {
        //            lsRaiffeisenPackageId = lsInternalId;
        //        }


        //        if (lsDeployXMLContent.Contains("installappv.ps1"))
        //        {
        //            if (!lsDeployXMLContent.Contains(@"HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"))
        //            {
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(@"<execute path=""powershell"">", @"<addregkey path=""HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"" name=""ExecutionPolicy"" value=""Unrestricted"" type=""REG_SZ"" /><execute path=""powershell"">");
        //            }
        //        }

        //        //append 
        //        if (!lsDeployXMLContent.ToLower().Contains("<releaseversion>"))
        //        {
        //            if (lsReleaseName != "")
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "</metadata>", "<releaseversion>" + lsReleaseName + "</releaseversion></metadata>", RegexOptions.IgnoreCase);
        //            }
        //            //lsDeployXMLContent = lsDeployXMLContent.ToLower().Replace("</metadata>", "<releaseversion>" + lsReleaseName + "</releaseversion></metadata>");
        //        }
        //        if (!lsDeployXMLContent.ToLower().Contains("<languages>"))
        //        {
        //            if (lsLanguages != "")
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "</metadata>", "<languages>" + lsLanguages + "</languages></metadata>", RegexOptions.IgnoreCase);
        //            }
        //        }
        //        else
        //        {
        //            if (FindBetween(lsDeployXMLContent, "<languages>", "</languages>").Length > 0)
        //            {
        //                lsDeployXMLContent = lsDeployXMLContent.Replace("<languages>" + FindBetween(lsDeployXMLContent, "<languages>", "</languages>") + "</languages>", "<languages>" + lsLanguages + "</languages>");
        //            }
        //        }
        //        if (!lsDeployXMLContent.ToLower().Contains("<architecture>"))
        //        {
        //            if (lsArchitectures != "")
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "</metadata>", "<architecture>" + lsArchitectures + "</architecture></metadata>", RegexOptions.IgnoreCase);
        //            }
        //        }
        //        if (!lsDeployXMLContent.ToLower().Contains("<maininstallfolderx86>"))
        //        {
        //            if (lsMaininstallfolderx86 != "")
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "</metadata>", "<maininstallfolderx86>" + lsMaininstallfolderx86 + "</maininstallfolderx86></metadata>", RegexOptions.IgnoreCase);
        //            }
        //        }
        //        if (!lsDeployXMLContent.ToLower().Contains("<maininstallfolderx64>"))
        //        {
        //            if (lsMaininstallfolderx64 != "")
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "</metadata>", "<maininstallfolderx64>" + lsMaininstallfolderx64 + "</maininstallfolderx64></metadata>", RegexOptions.IgnoreCase);
        //            }
        //        }
        //        if (!lsDeployXMLContent.ToLower().Contains("<maininstallexe>"))
        //        {
        //            if (lsMainExe != "")
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "</metadata>", "<maininstallexe>" + lsMainExe + "</maininstallexe></metadata>", RegexOptions.IgnoreCase);
        //            }
        //        }
        //        if (!lsDeployXMLContent.ToLower().Contains("<originalsoftwareid>"))
        //        {
        //            if (lsSoftwareId != "")
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "</metadata>", "<originalsoftwareid>" + lsSoftwareId + "</originalsoftwareid></metadata>", RegexOptions.IgnoreCase);
        //            }
        //        }

        //        //shop
        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopgui") == "enabled")
        //        {
        //            lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<tasks>", @"<tasks><enableinstallgui unattended=""true"" />", RegexOptions.IgnoreCase);
        //        }
        //        else
        //        {
        //            lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, @"<enableinstallgui unattended=""true"" />", "", RegexOptions.IgnoreCase);
        //        }

        //        //allusers
        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopallusers") != "")
        //        {
        //            lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<parameter>ALLUSERS=1</parameter>", "<parameter>ALLUSERS=" + clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopallusers") + "</parameter>", RegexOptions.IgnoreCase);
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopdisableadvtshortcuts") == "disabled")
        //        {
        //            lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<parameter>REBOOT=ReallySuppress</parameter>", "<parameter>REBOOT=ReallySuppress</parameter><parameter>DISABLEADVTSHORTCUTS=1</parameter>", RegexOptions.IgnoreCase);
        //        }

        //        //shopsilentparam                
        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsilentparam") != "")
        //        {
        //            lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "/qb!-", "/" + clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsilentparam"), RegexOptions.IgnoreCase);
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopdisablearp") == "disabled")
        //        {
        //            if (!lsDeployXMLContent.Contains("ARPNOMODIFY="))
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<parameter>REBOOT=ReallySuppress</parameter>", "<parameter>REBOOT=ReallySuppress</parameter><parameter>ARPNOMODIFY=1</parameter><parameter>ARPNOREPAIR=1</parameter><parameter>ARPNOREMOVE=1</parameter>", RegexOptions.IgnoreCase);
        //            }
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopactivesetup") == "disabled")
        //        {
        //            lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<parameter>REBOOT=ReallySuppress</parameter>", "<parameter>REBOOT=ReallySuppress</parameter><parameter>DISABLEACTIVESETUP=1</parameter>", RegexOptions.IgnoreCase);
        //        }

        //        switch (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoprootdrive"))
        //        {
        //            case "ignore":
        //                if (lsDeployXMLContent.ToLower().Contains("<parameter>rootdrive=c:</parameter>"))
        //                {
        //                    lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<parameter>ROOTDRIVE=C:</parameter>", "", RegexOptions.IgnoreCase);
        //                }
        //                break;
        //            case "":
        //                break;
        //            default:
        //                if (lsDeployXMLContent.ToLower().Contains("<parameter>rootdrive=c:</parameter>"))
        //                {
        //                    lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<parameter>ROOTDRIVE=C:</parameter>", "<parameter>ROOTDRIVE=" + clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoprootdrive") + "</parameter>", RegexOptions.IgnoreCase);
        //                }
        //                else
        //                {
        //                    if (lsDeployXMLContent.Contains("<parameter>REBOOT=ReallySuppress</parameter>"))
        //                    {
        //                        lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, "<parameter>REBOOT=ReallySuppress</parameter>", "<parameter>REBOOT=ReallySuppress</parameter><parameter>ROOTDRIVE=" + clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoprootdrive") + "</parameter>", RegexOptions.IgnoreCase);
        //                    }
        //                }
        //                break;
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoplog").ToLower() != @"%systemroot%\debug\msipackages" && clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoplog").ToLower() != @"")
        //        {
        //            string lsLogPath = clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoplog");
        //            if (lsLogPath.ToLower() != "false" && lsLogPath != "")
        //            {
        //                try
        //                {
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(@"%systemroot%\debug\msipackages", lsLogPath);
        //                }
        //                catch (Exception ex)
        //                {
        //                    writeToLogFile("replacing log: error " + ex.Message);
        //                }
        //            }
        //            writeToLogFile("replacing log: now lsDeployXMLContent is : " + lsDeployXMLContent);
        //        }


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoplogexename").ToLower() != "")
        //        {
        //            string lsNewValue = clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoplogexename");
        //            lsNewValue = getlogname(lsNewValue, lsManufacturer, lsProduct, lsVersion, lsReleaseName);
        //            writeToLogFile("replacing exelog: ");
        //            writeToLogFile("replacing exelog: from " + lsUninstallKey + ".log" + " to " + lsNewValue);
        //            try
        //            {
        //                lsDeployXMLContent = Regex.Replace(lsDeployXMLContent, lsUninstallKey + ".log", lsNewValue, RegexOptions.IgnoreCase);
        //            }
        //            catch (Exception ex)
        //            {
        //                writeToLogFile("replacing exelog: error " + ex.Message);
        //            }
        //            writeToLogFile("replacing exelog: now lsDeployXMLContent is : " + lsDeployXMLContent);
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopprocess").ToLower() == "askuser")
        //        {
        //            writeToLogFile("askuser: parsing");
        //            XDocument doc = new XDocument();
        //            doc = XDocument.Parse(lsDeployXMLContent);
        //            var matchingElements = doc.Descendants().Where(x => x.Name == "killprocess");
        //            foreach (XElement loNode in matchingElements)
        //            {
        //                writeToLogFile("askuser: found " + loNode.ToString());
        //                try
        //                {
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(loNode.ToString(), @"<messagebox type=""PROCESS"">Please close the running process '" + loNode.Attribute("name").Value + "' to proceed with the installation of '" + lsManufacturer + " " + lsProduct + " " + lsVersion + "'.</messagebox>");
        //                }
        //                catch { }
        //            }
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsilentprocess") == "enabled")
        //        {
        //            for (int i = 0; i < lsDeployXMLContent.Length; i++)
        //            {
        //                try
        //                {
        //                    if (lsDeployXMLContent.ToLower().Substring(i, "<execute".Length) == "<execute")
        //                    {
        //                        string lsTempValue = lsDeployXMLContent.Substring(i);
        //                        string lsTempContent = lsTempValue.Substring(0, lsTempValue.IndexOf(">"));
        //                        if (!lsTempContent.ToLower().Contains(@"windowstyle="""))
        //                        {
        //                            writeToLogFile("windowstyle::adding " + lsTempContent);
        //                            lsDeployXMLContent = lsDeployXMLContent.Replace(lsTempContent, lsTempContent.Replace("<execute ", @"<execute windowstyle=""hidden"" "));
        //                            lsDeployXMLContent = lsDeployXMLContent.Replace(lsTempContent, lsTempContent.Replace("<executenowait ", @"<executenowait windowstyle=""hidden"" "));
        //                        }
        //                    }
        //                }
        //                catch { }

        //            }
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopreinstall") == "enabled")
        //        {
        //            string lsInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //            string lsUninstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //            lsUninstall = FindBetween(lsUninstall, "<tasks>", "</tasks>");
        //            lsUninstall = Regex.Replace(lsUninstall, @"<abort exitcode=""0"" />", "");
        //            lsInstall = Regex.Replace(lsInstall, "<tasks>", @"<tasks>" + lsUninstall, RegexOptions.IgnoreCase);
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(FindBetween(lsDeployXMLContent, "<install>", "</install>"), lsInstall);
        //        }

        //        writeToLogFile("looking for shopprereq (" + lsRequestString + ")");

        //        try
        //        {
        //            writeToLogFile("now getreq:");
        //            writeToLogFile("getreq: " + clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopprereq"));
        //        }
        //        catch (Exception ex) { writeToLogFile("erro3: " + ex.Message); }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopprereq") == "separate")
        //        {
        //            try
        //            {
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(FindBetween(lsDeployXMLContent, "<!-- start prereqs -->", "<!-- end prereqs -->"), "");
        //            }
        //            catch { }
        //        }
        //        else
        //        {
        //            if (loPreReqTable.Rows.Count > 0)
        //            {
        //                try
        //                {
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(FindBetween(lsDeployXMLContent, "<!-- start prereqs -->", "<!-- end prereqs -->"), "");
        //                }
        //                catch { }

        //                writeToLogFile("forcing prereqs...");

        //                string lsInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                lsInstall = FindBetween(lsInstall, "<tasks>", "</tasks>");

        //                string lsPrereq = "";
        //                if (loPreReqTable.Rows.Count > 0)
        //                {
        //                    foreach (DataRow loPreReqRow in loPreReqTable.Rows)
        //                    {
        //                        lsPrereq = lsPrereq + loPreReqRow["installsection"].ToString();
        //                    }
        //                }

        //                if (lsDeployXMLContent.Contains("<!-- start prereqs -->"))
        //                {
        //                    if (lsPrereq != "")
        //                    {
        //                        lsDeployXMLContent = lsDeployXMLContent.Replace(lsInstall, "<!-- start prereqs -->" + lsPrereq + "<!-- end prereqs -->" + lsInstall);
        //                    }
        //                }

        //            }
        //        }

        //        writeToLogFile("done for shopprereq");

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopadditionalfolders").ToLower() != "")
        //        {
        //            string addfolders = "";
        //            foreach (string lsItem in (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopadditionalfolders").Split(Convert.ToChar("|"))))
        //            {
        //                if (lsItem != "")
        //                {
        //                    if (!lsDeployXMLContent.Contains(@"addfolder path=""" + lsItem.Trim().Replace("%3A", ":") + @""""))
        //                    {
        //                        addfolders = addfolders + @"<addfolder path=""" + lsItem.Trim().Replace("%3A", ":") + @""" />";
        //                    }
        //                }
        //            }
        //            if (addfolders != "")
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "<tasks>", @"<tasks>" + addfolders, RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopforceremoval").ToLower() == "enabled")
        //        {
        //            writeToLogFile("found shop removal");
        //            if (lsDeployXMLContent.ToLower().Contains("msiexec"))
        //            {
        //                if (!lsDeployXMLContent.Contains(@"<removeregkey path=""HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\"))
        //                {
        //                    try
        //                    {
        //                        string lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                        string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<removeregkey path=""HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""" /><removeregkey path=""HKLM\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""" /></tasks>", RegexOptions.IgnoreCase);
        //                        lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                    }
        //                    catch { }
        //                }
        //            }
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsupportshortcuts") != "")
        //        {
        //            string lsFinalName = "Linde_" + lsManufacturer.Replace(" ", "_") + "_" + lsProduct.Replace(" ", "_") + "_" + lsReleaseName.Replace(" ", "_") + "_001";
        //            lsDeployXMLContent = lsDeployXMLContent.Replace("<productname>" + FindBetween(lsDeployXMLContent, "<productname>", "</productname>") + "</productname>", "<productname>" + lsFinalName + "</productname>");
        //            string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //            string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<addshortcut path=""%SystemDrive%\_HI\Application_Detection\" + lsFinalName + @"\Uninstall_Software.lnk"" destination=""\\chrzmdb2025\PackageSource\01_Applications\" + lsFinalName + @"\Deliveries_Setup.exe"" arguments=""Type=X"" workdir=""\\chrzmdb2025\PackageSource\01_Applications\" + lsFinalName + @""" /></tasks>", RegexOptions.IgnoreCase);
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //            lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<removefolder path=""%SystemDrive%\_HI\Application_Detection\" + lsFinalName + @""" /></tasks>", RegexOptions.IgnoreCase);
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //            lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<addshortcut path=""%SystemDrive%\_HI\Application_Detection\" + lsFinalName + @"\Uninstall_Software.lnk"" destination=""\\chrzmdb2025\PackageSource\01_Applications\" + lsFinalName + @"\Deliveries_Setup.exe"" arguments=""Type=X"" workdir=""\\chrzmdb2025\PackageSource\01_Applications\" + lsFinalName + @""" /></tasks>", RegexOptions.IgnoreCase);
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsccmrefresh").ToLower() == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains(@"""{00000000-0000-0000-0000-000000000001}"""))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000001}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000001}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000001}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }

        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsccmsoftwareinventory").ToLower() == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains(@"""{00000000-0000-0000-0000-000000000002}"""))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000002}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000002}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000002}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }

        //        }


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsccmrequestmachinepolicy").ToLower() == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains(@"""{00000000-0000-0000-0000-000000000021}"""))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000021}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000021}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000021}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }

        //        }


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsccmevaluatemachinepolicy").ToLower() == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains(@"""{00000000-0000-0000-0000-000000000022}"""))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000022}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000022}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000022}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }

        //        }


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsccmappmanagerpolicy").ToLower() == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains(@"""{00000000-0000-0000-0000-000000000121}"""))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000121}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000121}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000121}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }

        //        }


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopsccmuseragenteval").ToLower() == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains(@"""{00000000-0000-0000-0000-000000000108}"""))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000108}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000108}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<execute path=""wmic"" windowstyle=""hidden""><parameters><parameter>/namespace:\\root\ccm path sms_client CALL TriggerSchedule ""{00000000-0000-0000-0000-000000000108}"" /NOINTERACTIVE</parameter></parameters></execute></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }

        //        }


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopdefenderpathinternal").ToLower() == "enabled")
        //        {

        //            if (!lsDeployXMLContent.Contains(@"""Add-MpPreference"""))
        //            {
        //                string lsDefenderInstallx86 = "";
        //                string lsDefenderInstallx64 = "";
        //                string lsDefenderUninstallx86 = "";
        //                string lsDefenderUninstallx64 = "";
        //                if (lsMaininstallfolderx86 != "")
        //                {
        //                    lsDefenderInstallx86 = @"-ExecutionPolicy ByPass -Command &quot;Add-MpPreference -ExclusionPath '" + lsMaininstallfolderx86 + "'&quot;";
        //                    lsDefenderUninstallx86 = @"-ExecutionPolicy ByPass -Command &quot;Remove-MpPreference -ExclusionPath '" + lsMaininstallfolderx86 + "'&quot;";
        //                }
        //                if (lsMaininstallfolderx64 != "")
        //                {
        //                    if (lsMaininstallfolderx86 != lsMaininstallfolderx64)
        //                    {
        //                        lsDefenderInstallx64 = @"-ExecutionPolicy ByPass -Command &quot;Add-MpPreference -ExclusionPath '" + lsMaininstallfolderx64 + "'&quot;";
        //                        lsDefenderInstallx64 = @"-ExecutionPolicy ByPass -Command &quot;Add-MpPreference -ExclusionPath '" + lsMaininstallfolderx64 + "'&quot;";
        //                    }
        //                }
        //                string lsDefenderInstall = "";
        //                string lsDefenderUninstall = "";
        //                if (lsDefenderInstallx86 != "")
        //                {
        //                    lsDefenderInstall += @"<execute path=""powershell.exe"" windowstyle=""hidden""><parameters><parameter>" + lsDefenderInstallx86 + @"</parameter></parameters></execute>";
        //                    lsDefenderUninstall += @"<execute path=""powershell.exe"" windowstyle=""hidden""><parameters><parameter>" + lsDefenderUninstallx86 + @"</parameter></parameters></execute>";
        //                }
        //                if (lsDefenderInstallx64 != "")
        //                {
        //                    lsDefenderInstall += @"<execute path=""powershell.exe"" windowstyle=""hidden""><parameters><parameter>" + lsDefenderInstallx64 + @"</parameter></parameters></execute>";
        //                    lsDefenderUninstall += @"<execute path=""powershell.exe"" windowstyle=""hidden""><parameters><parameter>" + lsDefenderUninstallx64 + @"</parameter></parameters></execute>";
        //                }

        //                if (lsDefenderInstall != "")
        //                {
        //                    string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                    string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", lsDefenderInstall + @"</tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                    lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                    lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", lsDefenderUninstall + "</tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                    lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                    lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", lsDefenderInstall + "</tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                }
        //            }

        //        }


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopfirewallinternal").ToLower() == "enabled")
        //        {

        //            if (!lsDeployXMLContent.Contains(@"""advfirewall"""))
        //            {
        //                string lsFirewallInstallx86 = "";
        //                string lsFirewallInstallx64 = "";
        //                if (lsMaininstallfolderx86 != "")
        //                {
        //                    lsFirewallInstallx86 = @"advfirewall firewall add rule name=""" + lsManufacturer + " " + lsProduct + " " + lsVersion + @""" dir=in action=allow program=""" + lsMaininstallfolderx86 + @"\" + lsMainExe + @""" enable=yes profile=any";
        //                }
        //                if (lsMaininstallfolderx64 != "")
        //                {
        //                    if (lsMaininstallfolderx86 != lsMaininstallfolderx64)
        //                    {
        //                        lsFirewallInstallx64 = @"advfirewall firewall add rule name=""" + lsManufacturer + " " + lsProduct + " " + lsVersion + @""" dir=in action=allow program=""" + lsMaininstallfolderx64 + @"\" + lsMainExe + @""" enable=yes profile=any";
        //                    }
        //                }
        //                string lsFirewallInstall = "";
        //                if (lsFirewallInstallx86 != "")
        //                {
        //                    lsFirewallInstall += @"<execute path=""netsh.exe"" windowstyle=""hidden""><parameters><parameter>" + lsFirewallInstallx86 + @"</parameter></parameters></execute>";
        //                }
        //                if (lsFirewallInstallx64 != "")
        //                {
        //                    lsFirewallInstall += @"<execute path=""netsh.exe"" windowstyle=""hidden""><parameters><parameter>" + lsFirewallInstallx64 + @"</parameter></parameters></execute>";
        //                }

        //                if (lsFirewallInstall != "")
        //                {
        //                    string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                    string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", lsFirewallInstall + @"</tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                    lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                    lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", lsFirewallInstall.Replace("add rule","remove rule") + "</tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                    lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                    lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", lsFirewallInstall + "</tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                }
        //            }

        //        }

        //        try
        //        {
        //            writeToLogFile("looking for startmenustructure");
        //            if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "startmenustructure").ToLower() != "")
        //            {
        //                writeToLogFile("found for startmenustructure");
        //                string lsSettingsValue = clsConvertEngine.GetValueFromRequestString(lsRequestString, "startmenustructure");
        //                writeToLogFile("startmenustructure: " + lsSettingsValue);
        //                if (lsSettingsValue.Trim() != "")
        //                {
        //                    string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                    writeToLogFile("oldinstall: " + lsOldInstall);
        //                    string lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<moveshortcuts path=""%ProgramData%\Microsoft\Windows\Start Menu\" + lsSettingsValue + @""" /></tasks>", RegexOptions.IgnoreCase);

        //                    writeToLogFile("newinstall: " + lsNewInstall);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                    lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                    lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<removeshortcuts path=""%ProgramData%\Microsoft\Windows\Start Menu\" + lsSettingsValue + @""" /></tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                    lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                    lsNewInstall = Regex.Replace(lsOldInstall, "</tasks>", @"<moveshortcuts path=""%ProgramData%\Microsoft\Windows\Start Menu\" + lsSettingsValue + @""" /></tasks>", RegexOptions.IgnoreCase);
        //                    lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //                }
        //            }
        //        }
        //        catch { }


        //        if (lsRequestString.ToLower().Contains("constellium"))
        //        {

        //            writeToLogFile("language fix for contellium");

        //            lsDeployXMLContent = lsDeployXMLContent.Replace("<getoslanguage>", @"<getvariablefromregistry var=""customlanguage"" path=""HKU\.DEFAULT\Control Panel\International"" name=""Locale"" />");
        //            lsDeployXMLContent = lsDeployXMLContent.Replace("</getoslanguage>", @"");

        //            lsDeployXMLContent = lsDeployXMLContent.Replace("<languagecodes>", @"");
        //            lsDeployXMLContent = lsDeployXMLContent.Replace("</languagecodes>", @"");

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(@"<languagecode value=""1036"">", @"<checkvariable var=""customlanguage"" value=""0000040C""><exists>");
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(@"<languagecode value=""1033"">", @"<checkvariable var=""customlanguage"" value=""00000409""><exists>");
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(@"<languagecode value=""1031"">", @"<checkvariable var=""customlanguage"" value=""00000407""><exists>");
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(@"<languagecode value=""1029"">", @"<checkvariable var=""customlanguage"" value=""00000405""><exists>");
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(@"<languagecode value=""1040"">", @"<checkvariable var=""customlanguage"" value=""00000410""><exists>");
        //            lsDeployXMLContent = lsDeployXMLContent.Replace(@"<languagecode value=""2055"">", @"<checkvariable var=""customlanguage"" value=""00000807""><exists>");

        //            for (int i = 1000; i < 3000; i++)
        //            {
        //                string lsHex = i.ToString("X");
        //                lsHex = "0000000000".Substring(0, 8 - lsHex.Length) + lsHex;
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(@"<languagecode value=""" + i.ToString() + @""">", @"<checkvariable var=""customlanguage"" value=""" + lsHex + @"""><exists>");
        //            }

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(@"</languagecode>", @"</exists></checkvariable>");

        //        }

        //        //serverusermode


        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopserverusermode") == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains("change.exe"))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                writeToLogFile("oldinstall: " + lsOldInstall);
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "<tasks>", @"<tasks><checkserver><exists><execute path=""change.exe"" windowstyle=""hidden""><parameters><parameter>user /install</parameter></parameters></execute></exists></checkserver>", RegexOptions.IgnoreCase);
        //                lsNewInstall = Regex.Replace(lsNewInstall, "</tasks>", @"<checkserver><exists><execute path=""change.exe"" windowstyle=""hidden""><parameters><parameter>user /execute</parameter></parameters></execute></exists></checkserver></tasks>", RegexOptions.IgnoreCase);
        //                writeToLogFile("newinstall: " + lsNewInstall);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);

        //                lsNewInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsNewInstall, "<tasks>", @"<tasks><checkserver><exists><execute path=""change.exe"" windowstyle=""hidden""><parameters><parameter>user /install</parameter></parameters></execute></exists></checkserver>", RegexOptions.IgnoreCase);
        //                lsNewInstall = Regex.Replace(lsNewInstall, "</tasks>", @"<checkserver><exists><execute path=""change.exe"" windowstyle=""hidden""><parameters><parameter>user /execute</parameter></parameters></execute></exists></checkserver></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);

        //            }
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopscomrefresh") == "enabled")
        //        {
        //            if (!lsDeployXMLContent.Contains("scom.mm.exe"))
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                writeToLogFile("oldinstall: " + lsOldInstall);
        //                string lsNewInstall = Regex.Replace(lsOldInstall, "<tasks>", @"<tasks><checkserver><exists><checkfile path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-s 60</parameter></parameters></execute></exists><notexists><checkfile path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-s 60</parameter></parameters></execute></exists></checkfile></notexists></checkfile></exists></checkserver>", RegexOptions.IgnoreCase);
        //                lsNewInstall = Regex.Replace(lsNewInstall, "</tasks>", @"<checkserver><exists><checkfile path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-d</parameter></parameters></execute></exists><notexists><checkfile path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-d</parameter></parameters></execute></exists></checkfile></notexists></checkfile></exists></checkserver></tasks>", RegexOptions.IgnoreCase);
        //                writeToLogFile("newinstall: " + lsNewInstall);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);

        //                lsNewInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsOldInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsNewInstall = Regex.Replace(lsOldInstall, "<tasks>", @"<tasks><checkserver><exists><checkfile path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-s 60</parameter></parameters></execute></exists><notexists><checkfile path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-s 60</parameter></parameters></execute></exists></checkfile></notexists></checkfile></exists></checkserver>", RegexOptions.IgnoreCase);
        //                lsNewInstall = Regex.Replace(lsNewInstall, "</tasks>", @"<checkserver><exists><checkfile path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-d</parameter></parameters></execute></exists><notexists><checkfile path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><exists><execute path=""%ProgramFiles(x86)%\SCOMMaintenance\scom.mm.exe""><parameters><parameter>-d</parameter></parameters></execute></exists></checkfile></notexists></checkfile></exists></checkserver></tasks>", RegexOptions.IgnoreCase);
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);

        //            }
        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopmsihandling") == "enhanced")
        //        {
        //            if (!lsDeployXMLContent.Contains("1602"))
        //            {
        //                lsDeployXMLContent = lsDeployXMLContent.Replace("</msiexec>", @"<returnvalues><returnvalue value=""1602""><!-- Files are in use --><abort error=""1602"" /></returnvalue></returnvalues></msiexec>");
        //            }
        //        }

        //        writeToLogFile("customertemplate is: [" + lsCustomerTemplate + "]");

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shopinstallreview") == "enabled")
        //        {
        //            writeToLogFile("now replacing:");

        //            string lsNewInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //            string lsOldInstall = lsNewInstall;

        //            writeToLogFile("lsOldInstall:" + lsOldInstall);

        //            string lsTempInstall = "";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<exists>";
        //            lsTempInstall += @"<abort exitcode=""0"" />";
        //            lsTempInstall += @"</exists>";
        //            lsTempInstall += @"</checkregistry>";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<exists>";
        //            lsTempInstall += @"<abort exitcode=""0"" />";
        //            lsTempInstall += @"</exists>";
        //            lsTempInstall += @"</checkregistry>";
        //            lsTempInstall += @"<abort exitcode=""9999"" />";
        //            lsNewInstall = lsNewInstall.Replace("</tasks>", lsTempInstall + "</tasks>");

        //            writeToLogFile("lsNewInstall:" + lsNewInstall);

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);


        //            string lsNewUninstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //            string lsOldUninstall = lsNewUninstall;
        //            lsTempInstall = "";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<exists>";
        //            lsTempInstall += @"<abort exitcode=""9999"" />";
        //            lsTempInstall += @"</exists>";
        //            lsTempInstall += @"</checkregistry>";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<exists>";
        //            lsTempInstall += @"<abort exitcode=""9999"" />";
        //            lsTempInstall += @"</exists>";
        //            lsTempInstall += @"</checkregistry>";

        //            lsNewUninstall = lsNewUninstall.Replace("</tasks>", lsTempInstall + "</tasks>");

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldUninstall, lsNewUninstall);


        //        }

        //        if (lsCustomerTemplate == "kssg")
        //        //if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "deployment").ToLower() == "uem")
        //        {

        //            writeToLogFile("now replacing:");

        //            string lsNewInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //            string lsOldInstall = lsNewInstall;

        //            writeToLogFile("lsOldInstall:" + lsOldInstall);

        //            string lsTempInstall = "";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<exists>";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""DisplayName"" value=""SSC-IT_" + lsResolvedDownloadName + @""" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""UninstallString"" value=""SSC-IT_" + lsResolvedDownloadName + @""" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""Publisher"" value=""SSC-IT"" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""SystemComponent"" value=""1"" type=""REG_DWORD"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""InstallDate"" value=""%DateTime%"" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""InstallSource"" value=""%CurrentFolder%"" type=""REG_SZ"" />";
        //            lsTempInstall += @"</exists>";
        //            lsTempInstall += @"</checkregistry>";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<exists>";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""DisplayName"" value=""SSC-IT_" + lsResolvedDownloadName + @""" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""UninstallString"" value=""SSC-IT_" + lsResolvedDownloadName + @""" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""Publisher"" value=""SSC-IT"" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""SystemComponent"" value=""1"" type=""REG_DWORD"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""InstallDate"" value=""%DateTime%"" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" name=""InstallSource"" value=""%CurrentFolder%"" type=""REG_SZ"" />";
        //            lsTempInstall += @"</exists>";
        //            lsTempInstall += @"</checkregistry>";
        //            lsNewInstall = lsNewInstall.Replace("</tasks>", lsTempInstall + "</tasks>");

        //            writeToLogFile("lsNewInstall:" + lsNewInstall);

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);


        //            string lsNewUninstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //            string lsOldUninstall = lsNewUninstall;
        //            lsTempInstall = "";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<notexists>";
        //            lsTempInstall += @"<removeregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" />";
        //            lsTempInstall += @"</notexists>";
        //            lsTempInstall += @"</checkregistry>";
        //            lsTempInstall += @"<checkregistry path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + lsUninstallKey + @""">";
        //            lsTempInstall += @"<notexists>";
        //            lsTempInstall += @"<removeregkey path=""HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\SSC-IT_" + lsResolvedDownloadName + @""" />";
        //            lsTempInstall += @"</notexists>";
        //            lsTempInstall += @"</checkregistry>";
        //            lsNewUninstall = lsNewUninstall.Replace("</tasks>", lsTempInstall + "</tasks>");

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldUninstall, lsNewUninstall);




        //            //lsNewUninstall = Regex.Replace(lsNewUninstall, "</tasks>", "", RegexOptions.IgnoreCase);


        //            //string lsTempUninstall = "";
        //            //lsTempUninstall += @"<removeregkey path=""HKLM\SOFTWARE\SSC-IT"" name=""" + lsResolvedDownloadName + @""" /> ";

        //            //lsNewUninstall += lsTempUninstall;
        //            //lsNewUninstall += @"</tasks>";

        //            //lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldUninstall, lsNewUninstall);

        //        }

        //        if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "deployment").ToLower() == "sitios")
        //        {
        //            string lsNewInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //            string lsOldInstall = lsNewInstall;
        //            lsNewInstall = Regex.Replace(lsNewInstall, "</tasks>", "", RegexOptions.IgnoreCase);

        //            string lsUGSNumber = "s10000";
        //            string lsUGSRevision = "1.0";
        //            foreach (DataRow loRow in loParameterTable.Rows)
        //            {
        //                if (loRow["name"].ToString().ToLower().Contains("ugsnumber"))
        //                {
        //                    lsUGSNumber = loRow["value"].ToString();
        //                }
        //                if (loRow["name"].ToString().ToLower().Contains("ugsrevision"))
        //                {
        //                    lsUGSRevision = loRow["value"].ToString();
        //                }
        //            }

        //            string lsTempInstall = "";


        //            lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Revision""  value=""" + lsUGSRevision + @""" type=""REG_SZ"" /> ";
        //            lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""ProductCode"" value=""" + lsUninstallKey + @"""  type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Product""  value=""" + lsProduct + @""" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Version""  value=""" + lsReleaseName + @""" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Vendor""  value=""" + lsManufacturer + @""" type=""REG_SZ"" />";
        //            if (lsUninstallKey.StartsWith("{"))
        //            {
        //                lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Uninstall""  value=""msiexec.exe /x " + lsUninstallKey + @""" type=""REG_SZ"" />";
        //                lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Repair""  value=""msiexec.exe /i " + lsUninstallKey + @" REINSTALL=ALL REINSTALLMODE=omus"" type=""REG_SZ"" />";
        //            }
        //            lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Date""  value=""%Date% - %Time%"" type=""REG_SZ"" />";
        //            lsTempInstall += @"<addregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" name=""Language""  value=""" + lsRealLanguages + @""" type=""REG_SZ"" />";

        //            lsNewInstall += lsTempInstall;
        //            lsNewInstall += @"</tasks>";

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);


        //            string lsNewUninstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //            string lsOldUninstall = lsNewUninstall;
        //            lsNewUninstall = Regex.Replace(lsNewUninstall, "</tasks>", "", RegexOptions.IgnoreCase);


        //            string lsTempUninstall = "";
        //            lsTempUninstall += @"<removeregkey path=""HKLM\SYSTEM\UGS\" + lsUGSNumber + @""" /> ";

        //            lsNewUninstall += lsTempUninstall;
        //            lsNewUninstall += @"</tasks>";

        //            lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldUninstall, lsNewUninstall);

        //        }


        //        if (!lsDeployXMLContent.Contains(@"Raiffeisen\_PackageInfo\_Wrapper"))
        //        {
        //            if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "deployment").ToLower() == "sccmadvanced" && lsReleaseId != "")
        //            {
        //                string lsUninstallKeys = FindBetween(lsDeployXMLContent, "SCCMOriginalProductCodes=", "-->").Trim();
        //                if (lsUninstallKeys != "")
        //                {
        //                    writeToLogFile("uninstallkeys2: " + lsUninstallKeys);
        //                }
        //                else
        //                {
        //                    writeToLogFile("error: no uninstall keys");
        //                }

        //                string lsNewInstall = FindBetween(lsDeployXMLContent, "<install>", "</install>");
        //                string lsOldInstall = lsNewInstall;
        //                lsNewInstall = Regex.Replace(lsNewInstall, "</tasks>", "", RegexOptions.IgnoreCase);
        //                bool lbDoneSomething = false;

        //                string lsTempInstall = "";
        //                string lsMyReleaseId = "00000000-0000-0000-0086-0000" + lsRaiffeisenPackageId;
        //                if (lsArchitectures.Contains("64"))
        //                {
        //                    lsMyReleaseId = "00000000-0000-0000-0064-0000" + lsRaiffeisenPackageId;
        //                }

        //                if (lsUninstallKeys != "")
        //                {

        //                    foreach (string lsProductCode in lsUninstallKeys.Split(Convert.ToChar(";")))
        //                    {
        //                        lbDoneSomething = true;
        //                        string lsMyProductCode = lsProductCode.Trim();



        //                        if (lsTempInstall == "")
        //                        {
        //                            lsTempInstall += @"<checkmsiinstallation package=""" + lsMyProductCode + @""">";
        //                            lsTempInstall += @"<exists>";

        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""PackageID""  value=""" + lsRaiffeisenPackageId + @""" type=""REG_SZ"" /> ";
        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""Manufacturer"" value=""" + lsManufacturer + @"""  type=""REG_SZ"" />";
        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductName""  value=""" + lsProduct + @""" type=""REG_SZ"" />";
        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductVersion""  value=""" + lsVersion + @""" type=""REG_SZ"" />";
        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ApplicationName""  value=""" + lsProduct + " - " + lsRaiffeisenPackageId + @""" type=""REG_SZ"" />";
        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductRelease""  value=""" + lsReleaseName + @""" type=""REG_SZ"" />";
        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallDate""  value=""%Date%"" type=""REG_SZ"" />";
        //                            lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallTime""  value=""%Time%"" type=""REG_SZ"" />";

        //                            lsTempInstall += @"</exists>";
        //                            lsTempInstall += @"<notexists></notexists>";
        //                            lsTempInstall += @"</checkmsiinstallation>";
        //                        }
        //                        else
        //                        {
        //                            string lsNewTempInstall = "";

        //                            lsNewTempInstall += @"<checkmsiinstallation package=""" + lsMyProductCode + @""">";
        //                            lsNewTempInstall += @"<exists>";

        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""PackageID""  value=""" + lsRaiffeisenPackageId + @""" type=""REG_SZ"" /> ";
        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""Manufacturer"" value=""" + lsManufacturer + @"""  type=""REG_SZ"" />";
        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductName""  value=""" + lsProduct + @""" type=""REG_SZ"" />";
        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductVersion""  value=""" + lsVersion + @""" type=""REG_SZ"" />";
        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ApplicationName""  value=""" + lsProduct + " - " + lsRaiffeisenPackageId + @""" type=""REG_SZ"" />";
        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductRelease""  value=""" + lsReleaseName + @""" type=""REG_SZ"" />";
        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallDate""  value=""%Date%"" type=""REG_SZ"" />";
        //                            lsNewTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallTime""  value=""%Time%"" type=""REG_SZ"" />";

        //                            lsNewTempInstall += @"</exists>";
        //                            lsNewTempInstall += @"<notexists></notexists>";
        //                            lsNewTempInstall += @"</checkmsiinstallation>";


        //                            lsTempInstall = lsTempInstall.Replace("<notexists></notexists>", "<notexists>" + lsNewTempInstall + "</notexists>");
        //                        }

        //                    }
        //                }
        //                else
        //                {
        //                    if (lsUninstallKey != "")
        //                    {
        //                        lsTempInstall += @"<checkmsiinstallation package=""" + lsUninstallKey + @""">";
        //                        lsTempInstall += @"<exists>";

        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""PackageID""  value=""" + lsRaiffeisenPackageId + @""" type=""REG_SZ"" /> ";
        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""Manufacturer"" value=""" + lsManufacturer + @"""  type=""REG_SZ"" />";
        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductName""  value=""" + lsProduct + @""" type=""REG_SZ"" />";
        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductVersion""  value=""" + lsVersion + @""" type=""REG_SZ"" />";
        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ApplicationName""  value=""" + lsProduct + " - " + lsRaiffeisenPackageId + @""" type=""REG_SZ"" />";
        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductRelease""  value=""" + lsReleaseName + @""" type=""REG_SZ"" />";
        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallDate""  value=""%Date%"" type=""REG_SZ"" />";
        //                        lsTempInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallTime""  value=""%Time%"" type=""REG_SZ"" />";

        //                        lsTempInstall += @"</exists>";
        //                        lsTempInstall += @"<notexists></notexists>";
        //                        lsTempInstall += @"</checkmsiinstallation>";

        //                        lbDoneSomething = true;
        //                    }
        //                }


        //                lsTempInstall = lsTempInstall.Replace("<notexists></notexists>", @"<notexists><removeregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}\"" /><abort exitcode=""9999"" /></notexists>");

        //                lsNewInstall += lsTempInstall;

        //                if (!lbDoneSomething)
        //                {
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""PackageID""  value=""" + lsRaiffeisenPackageId + @""" type=""REG_SZ"" /> ";
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""Manufacturer"" value=""" + lsManufacturer + @"""  type=""REG_SZ"" />";
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ApplicationName""  value=""" + lsProduct + " - " + lsRaiffeisenPackageId + @""" type=""REG_SZ"" />";
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductName""  value=""" + lsProduct + @""" type=""REG_SZ"" />";
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductVersion""  value=""" + lsVersion + @""" type=""REG_SZ"" />";
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""ProductRelease""  value=""" + lsReleaseName + @""" type=""REG_SZ"" />";
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallDate""  value=""%Date%"" type=""REG_SZ"" />";
        //                    lsNewInstall += @"<addregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}"" name=""InstallTime""  value=""%Time%"" type=""REG_SZ"" />";

        //                }
        //                lsNewInstall += @"</tasks>";
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);

        //                lsNewInstall = FindBetween(lsDeployXMLContent, "<uninstall>", "</uninstall>");
        //                lsOldInstall = lsNewInstall;
        //                lsNewInstall = Regex.Replace(lsNewInstall, "</tasks>", "", RegexOptions.IgnoreCase);
        //                lsNewInstall += @"<!-- remove Wrapper GUID -->";
        //                lsNewInstall += @"<removeregkey path=""HKLM\Software\Raiffeisen\_PackageInfo\_Wrapper\PKG_{" + lsMyReleaseId + @"}\"" />";
        //                lsNewInstall += @"</tasks>";
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }
        //        }

        //        if (!lsDeployXMLContent.Contains(@"Raiffeisen\_PackageInfo\_Wrapper"))
        //        {
        //            if (clsConvertEngine.GetValueFromRequestString(lsRequestString, "shoprepair").ToLower() == "disabled")
        //            {
        //                string lsOldInstall = FindBetween(lsDeployXMLContent, "<repair>", "</repair>");
        //                string lsNewInstall = @"<tasks><abort exitcode=""0"" /></tasks>";
        //                lsDeployXMLContent = lsDeployXMLContent.Replace(lsOldInstall, lsNewInstall);
        //            }
        //        }

        //        try
        //        {
        //            if (loParameterTable.Columns.Count > 0)
        //            {
        //                foreach (DataRow loParameterRow in loParameterTable.Select("custom='true'"))
        //                {
        //                    lsDeployXMLContent += @"<!-- " + loParameterRow["name"].ToString() + " = " + loParameterRow["value"] + " -->";
        //                }
        //            }
        //        }
        //        catch { }

        //    }
        //    catch (Exception dummy) { writeToLogFile("error2: " + dummy.Message); }
        //    writeToLogFile("now lsdeployxml is " + lsDeployXMLContent);


        //    try
        //    {
        //        XmlDocument xDocument = new XmlDocument();
        //        xDocument.Load(new XmlTextReader(new StringReader(lsDeployXMLContent)));
        //        lsDeployXMLContent = Beautify(xDocument);
        //        lsDeployXMLContent = lsDeployXMLContent.Replace("<languages></languages>","");
        //    }
        //    catch { }

        //    try
        //    {
        //        XmlDocument xDocument = new XmlDocument();
        //        xDocument.Load(new XmlTextReader(new StringReader(lsDeployXMLContent)));
        //        lsDeployXMLContent = Beautify(xDocument);
        //    }
        //    catch { }

        //    lsDeployXMLContent = lsDeployXMLContent.Replace("utf-16", "utf-8");

        //    return lsDeployXMLContent;

        //}



        //public static string CreateDeliveriesSetupExeConfig2(string lsShopLog, string lsShopLogScriptName, string lsShopLogExeSplit, string lsShopLogExeName, string lsShopActiveSetup, string lsShopLogScriptSplit)
        //{



        //}

        private static string Beautify(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            string lsReturn = sb.ToString();
            lsReturn = lsReturn.Replace(@"""utf-16""", @"""utf-8""");
            return lsReturn;
        }


        public static string GetValueFromRequestString(string lsRequestString, string lsMyName)
        {
            string lsValue = "";

            writeToLogFile("getvaluefromre: " + lsMyName);

            foreach (string lsRequest in lsRequestString.Replace("&amp;", "&").Split(Convert.ToChar("&")))
            {


                try
                {

                    {


                        //settings
                        {
                            if (lsRequest.IndexOf("=") > 0)
                            {
                                string lsSettingsName = "";
                                string lsSettingsValue = "";
                                try
                                {
                                    lsSettingsName = lsRequest.Substring(0, lsRequest.IndexOf("="));
                                    lsSettingsValue = lsRequest.Substring(lsRequest.IndexOf("=") + 1);
                                }
                                catch { }
                                lsSettingsValue = HttpUtility.HtmlDecode(lsSettingsValue);
                                lsSettingsValue = lsSettingsValue.Replace("%5C", @"\");
                                lsSettingsValue = lsSettingsValue.Replace("%20", " ");
                                lsSettingsValue = lsSettingsValue.Replace("%25", "%");
                                lsSettingsValue = lsSettingsValue.Replace("%27", "'");
                                lsSettingsValue = lsSettingsValue.Replace("%5B", "[");
                                lsSettingsValue = lsSettingsValue.Replace("%5D", "]");
                                lsSettingsValue = lsSettingsValue.Replace("%7C", "|");
                                lsSettingsValue = lsSettingsValue.Replace("%7c", "|");
                                lsSettingsValue = lsSettingsValue.Replace("%7B", "{");
                                lsSettingsValue = lsSettingsValue.Replace("%7b", "{");
                                lsSettingsValue = lsSettingsValue.Replace("%7D", "}");
                                lsSettingsValue = lsSettingsValue.Replace("%7d", "}");
                                if (lsSettingsName.ToLower() == lsMyName.ToLower())
                                {
                                    lsValue = lsSettingsValue;
                                    break;
                                }
                                if (lsMyName == "startmenustructure")
                                {
                                    if (lsSettingsName.ToLower().EndsWith("startmenustructure"))
                                    {
                                        lsValue = lsSettingsValue;
                                        break;
                                    }
                                }
                                //if (lsSettingsName.ToLower().Contains(lsMyName.ToLower()))
                                //{
                                //    lsValue = lsSettingsValue;
                                //    break;
                                //}

                            }
                        }
                    }
                }
                catch (Exception ex2) { writeToLogFile("getvaluefromre: " + ex2.Message); }
            }

            writeToLogFile("getvaluefromre dome: " + lsValue);

            return lsValue;
        }


        


        public static string FindBetween(string lsLine, string lsStart, string lsEnd)
        {
            int liCounter = 0;
            int liStartChar = 0;
            string lsReturn = "";
            bool lbInVariable = false;
            if (lsLine.Length > 0)
            {
                do
                {
                    if (lsLine.Substring(liCounter, lsStart.Length) == lsStart)
                    {
                        if (!lbInVariable)
                        {
                            liStartChar = liCounter;
                            lbInVariable = true;
                            liCounter += lsStart.Length;
                        }
                    }
                    if (lsLine.Substring(liCounter, lsEnd.Length) == lsEnd)
                    {
                        if (lbInVariable)
                        {
                            lsReturn = lsLine.Substring(liStartChar + lsStart.Length, liCounter - liStartChar - lsStart.Length);
                            break;
                        }
                        lbInVariable = false;
                    }
                    liCounter += 1;
                } while (liCounter < lsLine.Length - lsStart.Length);
            }
            return lsReturn;
        }


        public static void writeToLogFile(string logMessage)
        {

            string strLogMessage = string.Empty;
            string strLogFile = Environment.ExpandEnvironmentVariables(@"%temp%\convertengine.log");
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


        public static string getAppVFileRoot(string lsRoot)
        {
            string lsReturn = "";
            lsReturn = lsRoot;
            if (lsRoot != "")
            {
                lsReturn = "[{" + lsRoot + "}]";
            }
            return lsReturn;
        }

        public static string getAppVRegistryRoot(string lsRoot)
        {
            string lsReturn = "";
            lsReturn = lsRoot;
            if (lsRoot == "HKCU")
            {
                lsReturn = @"REGISTRY\USER\[{AppVCurrentUserSID}]";
            }
            if (lsRoot == "HKCUC")
            {
                lsReturn = @"REGISTRY\USER\[{AppVCurrentUserSID}]_CLASSES";
            }
            if (lsRoot == "HKLM")
            {
                lsReturn = @"REGISTRY\MACHINE";
            }
            if (lsRoot == "HKU")
            {
                lsReturn = @"REGISTRY\USER";
            }
            if (lsRoot == "")
            {
                lsReturn = @"REGISTRY";
            }
            return lsReturn;
        }


        public static void GetSetupParameters(string lsCommand, string lsParam, ref string lsEXEFile, ref string lsParameters)
        {
            lsEXEFile = lsCommand;
            lsParameters = lsParam;
        }

        public static string CreateVBScripts(string lsType, string lsDeployXML, string lsManufacturer, string lsProduct, string lsVersion, string lsAppVPackageId, string lsAppVVersionId)
        {
            writeToLogFile("createvbscripts");

            writeToLogFile("appvpackageid: " + lsAppVPackageId);

            DataTable loTable = ConvertXMLToDataTable(lsType, lsDeployXML, false);

            writeToLogFile("rows: " + loTable.Rows.Count.ToString());

            StringBuilder loStringBuilder = new StringBuilder();
            if (loTable.Rows.Count > 0)
            {
                loStringBuilder.AppendLine(@"'Manufacturer: " + lsManufacturer);
                loStringBuilder.AppendLine(@"'Product:      " + lsProduct);
                loStringBuilder.AppendLine(@"'Version:      " + lsVersion);
                loStringBuilder.AppendLine(@"'Date:         " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Option Explicit");
                loStringBuilder.AppendLine(@"Dim goFSO, goWshShell, goRegistry, goApplication, goWMI, giOSLang");
                loStringBuilder.AppendLine(@"Set goWshShell = CreateObject(""WScript.Shell"")");
                loStringBuilder.AppendLine(@"Set goApplication = CreateObject(""Shell.Application"")");
                loStringBuilder.AppendLine(@"Set goFSO      = CreateObject(""Scripting.FileSystemObject"")");
                loStringBuilder.AppendLine(@"Set goRegistry = GetObject(""winmgmts:{impersonationLevel=impersonate}!\\.\root\default:StdRegProv"")");
                loStringBuilder.AppendLine(@"Set goWMI = GetObject(""winmgmts:{impersonationLevel=impersonate}!\\.\root\cimv2"")");
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Const HKEY_CURRENT_USER =  &H80000001");
                loStringBuilder.AppendLine(@"Const HKEY_LOCAL_MACHINE = &H80000002");
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Dim gsUserSID, gsUser");
                loStringBuilder.AppendLine(@"gsUser = CreateObject(""WScript.Network"").UserName");
                loStringBuilder.AppendLine(@"gsUserSID = GetSIDFromUser(gsUser)");
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Dim goOperatingSystems, goOperatingSystem");
                loStringBuilder.AppendLine(@"Set goOperatingSystems = goWMI.ExecQuery(""Select * from Win32_OperatingSystem"")");
                loStringBuilder.AppendLine(@"For Each goOperatingSystem in goOperatingSystems");
                loStringBuilder.AppendLine(@"  giOSLang = goOperatingSystem.OSLanguage");
                loStringBuilder.AppendLine(@"Next");
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Dim goFilesInZip");
                loStringBuilder.AppendLine(@"Dim goTextFile, gsTextFile, gsLine");
                loStringBuilder.AppendLine(@"");

                loStringBuilder.AppendLine(@"goWshShell.CurrentDirectory = goFSO.GetParentFolderName(WScript.ScriptFullName)");
                loStringBuilder.AppendLine(@"");

                DataTable loVarTable = new DataTable();
                loVarTable.Columns.Add("name", typeof(string));
                loVarTable.Columns.Add("value", typeof(string));

                foreach (DataRow loRow in loTable.Rows)
                {
                    string lsInherit = "";
                    for (int i = 1; i < Convert.ToInt32(loRow["level"].ToString()); i++)
                    {
                        lsInherit = lsInherit + "  ";
                    }
                    switch (loRow["actiontype"].ToString().ToLower())
                    {
                        case "launchprocess":
                            {
                                string lsExeAndParameters = @"ExpandEnvironmentStringsEx(""""""" + loRow["command"].ToString() + @""""" " + loRow["param"].ToString().Replace(@"""", @"""""") + @""")";
                                if (loRow["param"].ToString() == "")
                                {
                                    lsExeAndParameters = @"ExpandEnvironmentStringsEx(""""""" + loRow["command"].ToString() + @""""""")";
                                }
                                string lsWindowStyle = "1";
                                if (loRow["options"].ToString().ToLower() == "hidden")
                                {
                                    lsWindowStyle = "0";
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.Run " + lsExeAndParameters + ", " + lsWindowStyle + ", true");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            }
                            break;
                        case "launchprocessnowait":
                            {
                                string lsExeAndParameters = @"ExpandEnvironmentStringsEx(""""""" + loRow["command"].ToString() + @""""" " + loRow["param"].ToString().Replace(@"""", @"""""") + @""")";
                                if (loRow["param"].ToString() == "")
                                {
                                    lsExeAndParameters = @"ExpandEnvironmentStringsEx(""""""" + loRow["command"].ToString() + @""""""")";
                                }
                                string lsWindowStyle = "1";
                                if (loRow["options"].ToString().ToLower() == "hidden")
                                {
                                    lsWindowStyle = "0";
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.Run " + lsExeAndParameters + ", " + lsWindowStyle + ", false");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            }
                            break;
                        case "msiexec":
                            if (loRow["command"].ToString().ToLower() == "/i")
                            {
                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.Run ExpandEnvironmentStringsEx(""msiexec /i " + loRow["param"].ToString().Replace(@"""", @"""""") + @"""), 0, true");
                            }
                            else
                            {
                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.Run ExpandEnvironmentStringsEx(""msiexec /x " + loRow["param"].ToString().Replace(@"""", @"""""") + @"""), 0, true");
                            }
                            break;
                        case "killprocess":
                            {
                                string lsCommand = loRow["command"].ToString();
                                if (!lsCommand.ToLower().EndsWith(".exe"))
                                {
                                    lsCommand = lsCommand + ".exe";
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.Run ""TASKKILL /F /IM " + lsCommand + @""",0, true");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            }
                            break;
                        case "checklanguagecode":
                            {
                                string lsCommand = loRow["command"].ToString();
                                loStringBuilder.AppendLine(lsInherit + @"If giOSLang=" + loRow["command"].ToString() + @" Then");
                            }
                            break;
                        case "sleep":
                            int liSeconds = 5;
                            try
                            {
                                liSeconds = Convert.ToInt32(loRow["param"].ToString());
                            }
                            catch { }
                            loStringBuilder.AppendLine(lsInherit + @"WScript.Sleep " + liSeconds.ToString() + "000");
                            break;
                        case "addshortcut":
                            {

                                // lsParam = lsDestination + "|" + lsWorkDir + "|" + lsIconPath + "|" + lsIconIndex + "|" + lsDescription + "|" + lsArguments;

                                string lsPath = "";
                                lsPath = loRow["command"].ToString();
                                string lsDestination = "";
                                try
                                {
                                    lsDestination = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                catch { }
                                string lsWorkDir = "";
                                try
                                {
                                    lsWorkDir = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                string lsDescription = "";
                                try
                                {
                                    lsDescription = loRow["param"].ToString().Split(Convert.ToChar("|"))[4];
                                }
                                catch { }
                                string lsArguments = "";
                                try
                                {
                                    lsArguments = loRow["param"].ToString().Split(Convert.ToChar("|"))[5];
                                }
                                catch { }

                                string lsGuid = Guid.NewGuid().ToString().Replace("-", "");

                                loStringBuilder.AppendLine(lsInherit + @"Dim goShortcut" + lsGuid);
                                loStringBuilder.AppendLine(lsInherit + @"Set goShortcut" + lsGuid + @" = goWshShell.CreateShortcut(ExpandEnvironmentStringsEx(""" + lsPath + @"""))");

                                loStringBuilder.AppendLine(lsInherit + @"   goShortcut" + lsGuid + @".TargetPath = ExpandEnvironmentStringsEx(""" + lsDestination + @""")");
                                if (lsArguments!="")
                                {
                                    loStringBuilder.AppendLine(lsInherit + @"   goShortcut" + lsGuid + @".Arguments = """ + lsArguments + @"""");
                                }
                                if (lsDescription!="")
                                {
                                    loStringBuilder.AppendLine(lsInherit + @"   goShortcut" + lsGuid + @".Description = """ + lsDescription + @"""");
                                }
                                //loStringBuilder.AppendLine(lsInherit + @"   '  goShortcut" + lsGuid + @".HotKey = ""ALT+CTRL+F""");
                                loStringBuilder.AppendLine(lsInherit + @"   '  goShortcut" + lsGuid + @".IconLocation = ""%SystemRoot%\SystemResources\shell32.dll.mun, 94""");
                                loStringBuilder.AppendLine(lsInherit + @"   '  goShortcut" + lsGuid + @".WindowStyle = ""1""");
                                if (lsWorkDir!="")
                                {
                                    loStringBuilder.AppendLine(lsInherit + @"   goShortcut" + lsGuid + @".WorkingDirectory = ExpandEnvironmentStringsEx(""" + lsWorkDir + @""")");
                                }
                                loStringBuilder.AppendLine(lsInherit + @"   goShortcut" + lsGuid + @".Save");
                                loStringBuilder.AppendLine(lsInherit + @"   Set goShortcut" + lsGuid + @" = Nothing");

                                loStringBuilder.AppendLine(lsInherit + @"' addshortcut ");
                                loStringBuilder.AppendLine(lsInherit + @"' goWshShell.Run ExpandEnvironmentStringsEx(""msiexec /x " + loRow["param"].ToString().Replace(@"""", @"""""") + @"""), 0, true");

                            }
                            break;
                        case "setvariable":
                            if (!loStringBuilder.ToString().Contains(@"Dim custom" + loRow["command"].ToString()))
                            {
                                loStringBuilder.AppendLine(lsInherit + @"Dim custom" + loRow["command"].ToString());
                            }
                            loStringBuilder.AppendLine(lsInherit + @"custom" + loRow["command"].ToString() + @"=""" + loRow["param"].ToString() + @"""");

                            if (loVarTable.Select("name='custom" + loRow["command"].ToString() + "'").Length == 0)
                            {
                                DataRow loNewRow = loVarTable.NewRow();
                                loNewRow["name"] = loRow["command"].ToString();
                                loNewRow["value"] = "";
                                loVarTable.Rows.Add(loNewRow);
                            }

                            break;
                        case "getvariablefromregistry":
                            {
                                string lsMyPath = "";
                                string lsMyRootSz = "HKEY_LOCAL_MACHINE";
                                try
                                {
                                    lsMyPath = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                catch { }
                                string lsName = "";
                                try
                                {
                                    lsName = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                if (lsMyPath.ToUpper().StartsWith("HKLM"))
                                {
                                    lsMyPath = lsMyPath.Substring(5);
                                }
                                if (lsMyPath.ToUpper().StartsWith("HKCR"))
                                {
                                    lsMyPath = lsMyPath.Substring(5);
                                    lsMyRootSz = "HKEY_CLASSES_ROOT";
                                }
                                if (lsMyPath.ToUpper().StartsWith("HKCU"))
                                {
                                    lsMyPath = lsMyPath.Substring(5);
                                    lsMyRootSz = "HKEY_CURRENT_USER";
                                }
                                if (!loStringBuilder.ToString().Contains(@"Dim custom" + loRow["command"].ToString()))
                                {
                                    loStringBuilder.AppendLine(lsInherit + @"Dim custom" + loRow["command"].ToString());
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + @"custom" + loRow["command"].ToString() + @" = """"");
                                loStringBuilder.AppendLine(lsInherit + @"custom" + loRow["command"].ToString() + @" = goWshShell.RegRead(""" + lsMyRootSz + @"\" + lsMyPath + @"\" + lsName + @""")");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");

                                if (loVarTable.Select("name='custom" + loRow["command"].ToString() + "'").Length == 0)
                                {
                                    DataRow loNewRow = loVarTable.NewRow();
                                    loNewRow["name"] = loRow["command"].ToString();
                                    loNewRow["value"] = "";
                                    loVarTable.Rows.Add(loNewRow);
                                }
                            }
                            break;
                        case "copyfile":
                            loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                            loStringBuilder.AppendLine(lsInherit + @"goFSO.CopyFile ExpandEnvironmentStringsEx(""" + loRow["command"].ToString() + @"""), ExpandEnvironmentStringsEx(""" + loRow["param"].ToString() + @"""), true");
                            loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            break;
                        case "copyfolder":
                            loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                            loStringBuilder.AppendLine(lsInherit + @"goFSO.CreateFolder ExpandEnvironmentStringsEx(""" + loRow["param"].ToString() + @""")");
                            loStringBuilder.AppendLine(lsInherit + @"goFSO.CopyFolder ExpandEnvironmentStringsEx(""" + loRow["command"].ToString() + @"""), ExpandEnvironmentStringsEx(""" + loRow["param"].ToString() + @"""), true");
                            loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            break;
                        case "expandfolder":
                            loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                            loStringBuilder.AppendLine(lsInherit + @"Set goFilesInZip = goApplication.NameSpace(ExpandEnvironmentStringsEx(""" + loRow["command"].ToString() + @""").Items()");
                            loStringBuilder.AppendLine(lsInherit + @"goApplication.NameSpace(ExpandEnvironmentStringsEx(""" + loRow["param"].ToString() + @""")).CopyHere goFilesInZip, 16");
                            loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            break;
                        case "addfolder":
                            loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                            loStringBuilder.AppendLine(lsInherit + @"goFSO.CreateFolder ExpandEnvironmentStringsEx(""" + loRow["command"].ToString() + @""")");
                            loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            break;
                        case "removefile":
                            loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                            loStringBuilder.AppendLine(lsInherit + @"goFSO.DeleteFile ExpandEnvironmentStringsEx(""" + loRow["command"].ToString() + @"""), true");
                            loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            break;
                        case "removefolder":
                            loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                            loStringBuilder.AppendLine(lsInherit + @"goFSO.DeleteFolder ExpandEnvironmentStringsEx(""" + loRow["command"].ToString() + @"""), true");
                            loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            break;
                        case "addfilepermission":
                            {
                                string lsPath = "";
                                string lsPermission = "";
                                string lsAccount = "";
                                string lsReplace = "";
                                string lsInheritance = "";
                                try
                                {
                                    lsPath = loRow["command"].ToString();
                                }
                                catch { }
                                try
                                {
                                    lsPermission = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                catch { }
                                try
                                {
                                    lsAccount = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                try
                                {
                                    lsReplace = loRow["param"].ToString().Split(Convert.ToChar("|"))[2];
                                }
                                catch { }
                                try
                                {
                                    lsInheritance = loRow["param"].ToString().Split(Convert.ToChar("|"))[3];
                                }
                                catch { }
                                if (lsType == "initialize" && lsAppVPackageId != "")
                                {
                                    lsPath = lsPath.Replace(@"%ProgramData%\", @"%LOCALAPPDATA%\Microsoft\AppV\Client\VFS\" + lsAppVPackageId + @"\Common AppDataS\");
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.Run ExpandEnvironmentStringsEx(""""""icacls.exe"""" """"" + lsPath + @""""" /grant:r """"Jeder"""":(OI)(CI)M /c /t /q""), true");
                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.Run ExpandEnvironmentStringsEx(""""""icacls.exe"""" """"" + lsPath + @""""" /grant:r """"Jeder"""":(OI)(CI)M /c /t /q""), true");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            }
                            break;
                        case "checkvariable":
                            {
                                loStringBuilder.AppendLine(lsInherit + @"If custom" + loRow["command"].ToString() + @"=""" + loRow["param"].ToString() + @""" Then");
                            }
                            break;
                        case "checkregistry":
                            {
                                string lsVariableName = "var" + Guid.NewGuid().ToString().Replace("-", "").ToLower();
                                string lsMyPath = loRow["path"].ToString();
                                string lsMyName = loRow["param"].ToString();
                                string lsMyValue = loRow["command"].ToString();
                                //string lsMyRoot = "&H80000002";
                                string lsMyRootSz = "HKEY_LOCAL_MACHINE";
                                if (lsMyPath.ToUpper().StartsWith("HKLM"))
                                {
                                    lsMyPath = lsMyPath.Substring(5);
                                }
                                if (lsMyPath.ToUpper().StartsWith("HKCR"))
                                {
                                    lsMyPath = lsMyPath.Substring(5);
                                    //lsMyRoot = "&H80000000";
                                    lsMyRootSz = "HKEY_CLASSES_ROOT";
                                }
                                if (lsMyPath.ToUpper().StartsWith("HKCU"))
                                {
                                    lsMyPath = lsMyPath.Substring(5);
                                    //lsMyRoot = "&H80000001";
                                    lsMyRootSz = "HKEY_CURRENT_USER";
                                }
                                loStringBuilder.AppendLine(lsInherit + @"Dim " + lsVariableName);
                                loStringBuilder.AppendLine(lsInherit + lsVariableName + @"=""""");

                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + lsVariableName + @" = goWshShell.RegRead(""" + lsMyRootSz + @"\" + lsMyPath + @"\" + lsMyName + @""")");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");


                                switch (loRow["condition"].ToString())
                                {
                                    case "notexists":
                                        loStringBuilder.AppendLine(lsInherit + @"If lcase(" + lsVariableName + @")="""" Then");
                                        break;
                                    default:
                                        if (lsMyValue == "")
                                        {
                                            loStringBuilder.AppendLine(lsInherit + @"If lcase(" + lsVariableName + @")<>"""" Then");
                                        }
                                        else
                                        {
                                            loStringBuilder.AppendLine(lsInherit + @"If lcase(" + lsVariableName + @")=""" + lsMyValue.ToLower() + @""" Then");
                                        }
                                        break;
                                }
                            }
                            break;
                        case "abort":
                            loStringBuilder.AppendLine(lsInherit + @"WScript.Quit 0");
                            break;
                        case "addregkey":
                            {
                                string lsRoot = "HKEY_LOCAL_MACHINE";
                                string lsPath = loRow["command"].ToString();
                                string lsName = "";
                                string lsMyType = "";
                                string lsValue = "";
                                try
                                {
                                    lsPath = lsPath.Substring(lsPath.IndexOf(@"\") + 1);
                                }
                                catch { }
                                try
                                {
                                    lsPath = lsPath.Substring(0, lsPath.IndexOf(Convert.ToChar("|")));
                                }
                                catch { }
                                try
                                {
                                    lsName = loRow["command"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                if (loRow["command"].ToString().ToUpper().StartsWith("HKCU"))
                                {
                                    lsRoot = "HKEY_CURRENT_USER";
                                }
                                if (loRow["command"].ToString().ToUpper().StartsWith("HKU"))
                                {
                                    lsRoot = "HKEY_USERS";
                                }
                                if (loRow["command"].ToString().ToUpper().StartsWith("HKCR"))
                                {
                                    lsRoot = "HKEY_CLASSES_ROOT";
                                }
                                try
                                {
                                    lsValue = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                try
                                {
                                    lsMyType = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                catch { }

                                bool lbHasVars = false;
                                foreach (DataRow loVarRow in loVarTable.Rows)
                                {
                                    if (lsValue.Contains("%" + loVarRow["name"].ToString() + "%"))
                                    {
                                        lsValue = lsValue.Replace("%" + loVarRow["name"].ToString() + "%", @""" & custom" + loVarRow["name"].ToString() + @" & """);
                                        lbHasVars = true;
                                    }
                                }
                                if (lsValue.StartsWith(@""" & "))
                                {
                                    try
                                    {
                                        lsValue = lsValue.Substring(4);
                                    }
                                    catch { }
                                }
                                if (lsValue.EndsWith(@" & """))
                                {
                                    try
                                    {
                                        lsValue = lsValue.Substring(0, lsValue.Length - 4);
                                    }
                                    catch { }
                                }
                                if (lbHasVars)
                                {
                                    if (lsValue.Contains(@""" & "))
                                    {
                                        if (!lsValue.StartsWith(@""""))
                                        {
                                            lsValue = @"""" + lsValue;
                                        }
                                    }
                                    if (lsValue.Contains(@" & """))
                                    {
                                        if (!lsValue.EndsWith(@""""))
                                        {
                                            lsValue = lsValue + @"""";
                                        }
                                    }
                                }
                                else
                                {
                                    lsValue = lsValue.Replace(@"""", @"""""");
                                    lsValue = @"""" + lsValue + @"""";
                                }

                                if (lsValue.StartsWith(@""""))
                                {
                                    lsValue = " ExpandEnvironmentStringsEx(" + lsValue + ")";
                                }

                                loStringBuilder.AppendLine(lsInherit + "On Error Resume Next");

                                writeToLogFile("check lsPath (" + lsPath + ") for slash ");

                                if (lsPath.Contains("/"))
                                {
                                    switch (lsMyType.ToUpper())
                                    {
                                        default:
                                            if (lsValue.ToLower().Contains("port%"))
                                            {
                                                loStringBuilder.AppendLine(lsInherit + @"goRegistry.SetStringValue " + lsRoot + @",""" + @"SOFTWARE\Microsoft\AppV\Client\Packages\" + lsAppVPackageId + @"\REGISTRY\USER\"" + gsUserSID + ""\" + lsPath + @""",""" + lsName + @""", " + lsValue + @"""");
                                            }
                                            else
                                            {
                                                loStringBuilder.AppendLine(lsInherit + @"goRegistry.SetStringValue " + lsRoot + @",""" + lsPath + @""",""" + lsName + @""", " + lsValue + @"""");
                                            }
                                            break;
                                    }

                                }
                                else
                                {
                                    switch (lsMyType.ToUpper())
                                    {
                                        case "REG_MULTI_SZ":
                                            try
                                            {
                                                string lsMyRoot = "&H80000002";
                                                if (lsRoot == "HKEY_CURRENT_USER")
                                                {
                                                    lsMyRoot = "&H80000001";
                                                }
                                                string lsArrayData = "Array(";
                                                foreach (string lsString in lsValue.Replace("[~]", "~").Split(Convert.ToChar("~")))
                                                {
                                                    lsArrayData += @"""" + lsString + @""",";
                                                }
                                                if (lsArrayData.EndsWith(","))
                                                {
                                                    lsArrayData = lsArrayData.Substring(0, lsArrayData.Length - 1);
                                                }
                                                lsArrayData += ")";
                                                loStringBuilder.AppendLine(lsInherit + @"goRegistry.SetMultiStringValue " + lsMyRoot + @", """ + lsPath + @""", """ + lsName + @""", " + lsArrayData);
                                            }
                                            catch { }
                                            break;
                                        default:
                                            if (lsName != "")
                                            {
                                                if (lsName.ToLower() == "(default)")
                                                {
                                                    loStringBuilder.AppendLine(lsInherit + @"goWshShell.RegWrite """ + lsRoot + @"\" + lsPath + @"\"", " + lsValue + @", """ + lsMyType + @"""");
                                                }
                                                else
                                                {
                                                    loStringBuilder.AppendLine(lsInherit + @"goWshShell.RegWrite """ + lsRoot + @"\" + lsPath + @"\" + lsName + @""", " + lsValue + @", """ + lsMyType + @"""");
                                                }
                                            }
                                            else
                                            {
                                                loStringBuilder.AppendLine(lsInherit + @"goWshShell.RegWrite """ + lsRoot + @"\" + lsPath + @""", " + lsValue + @", """ + lsMyType + @"""");
                                            }
                                            break;
                                    }
                                }

                                loStringBuilder.AppendLine(lsInherit + "On Error Goto 0");
                            }
                            break;
                        case "removeregkey":
                            {
                                string lsRoot = "HKEY_LOCAL_MACHINE";
                                string lsPath = loRow["command"].ToString();
                                string lsName = "";
                                try
                                {
                                    lsPath = lsPath.Substring(lsPath.IndexOf(@"\") + 1);
                                }
                                catch { }
                                try
                                {
                                    lsPath = lsPath.Substring(0, lsPath.IndexOf(Convert.ToChar("|")));
                                }
                                catch { }
                                try
                                {
                                    lsName = loRow["command"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }
                                if (loRow["command"].ToString().ToUpper().StartsWith("HKCU"))
                                {
                                    lsRoot = "HKEY_CURRENT_USER";
                                }
                                if (loRow["command"].ToString().ToUpper().StartsWith("HKU"))
                                {
                                    lsRoot = "HKEY_USERS";
                                }
                                if (loRow["command"].ToString().ToUpper().StartsWith("HKCR"))
                                {
                                    lsRoot = "HKEY_CLASSES_ROOT";
                                }
                                if (!lsPath.EndsWith(@"\"))
                                {
                                    lsPath = lsPath + @"\";
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                if (lsName == "")
                                {
                                    loStringBuilder.AppendLine(lsInherit + @"goWshShell.RegDelete """ + lsRoot + @"\" + lsPath + @"""");
                                }
                                else
                                {
                                    loStringBuilder.AppendLine(lsInherit + @"goWshShell.RegDelete """ + lsRoot + @"\" + lsPath + @"\" + lsName + @"""");
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error goto 0");
                            }
                            break;
                        case "removetext":
                            {
                                string lsText = loRow["param"].ToString();
                                if (loRow["param"].ToString().Split(Convert.ToChar("|")).Length > 0)
                                {
                                    lsText = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + @"gsTextFile = """"");
                                loStringBuilder.AppendLine(lsInherit + @"If goFSO.FileExists(goWshShell.ExpandEnvironmentStrings(""" + loRow["path"].ToString() + @""")) Then");
                                loStringBuilder.AppendLine(lsInherit + @"Set goTextFile = goFSO.OpenTextFile(goWshShell.ExpandEnvironmentStrings(""" + loRow["path"].ToString() + @"""),1)");
                                loStringBuilder.AppendLine(lsInherit + @"gsTextFile = goTextFile.ReadAll");
                                loStringBuilder.AppendLine(lsInherit + @"goTextFile.Close");
                                loStringBuilder.AppendLine(lsInherit + @"Set goTextFile = goFSO.OpenTextFile(goWshShell.ExpandEnvironmentStrings(""" + loRow["path"].ToString() + @"""),2)");
                                loStringBuilder.AppendLine(lsInherit + @"For Each gsLine in Split(gsTextFile,vbcrlf)");
                                loStringBuilder.AppendLine(lsInherit + @"  If Not Instr(1,lcase(gsLine),""" + lsText + @""",1) Then");
                                loStringBuilder.AppendLine(lsInherit + @"    goTextFile.WriteLine(gsLine)");
                                loStringBuilder.AppendLine(lsInherit + @"  End If");
                                loStringBuilder.AppendLine(lsInherit + @"Next");
                                loStringBuilder.AppendLine(lsInherit + @"goTextFile.Close");
                                loStringBuilder.AppendLine(lsInherit + @"End If");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                            }
                            break;
                        case "replacetext":
                            {
                                string lsMyGuid = Guid.NewGuid().ToString().Replace("-", "");
                                string lsSource = "";
                                string lsDestination = "";
                                try
                                {
                                    lsSource = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                                }
                                catch { }
                                try
                                {
                                    lsDestination = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                                }
                                catch { }

                                loStringBuilder.AppendLine(lsInherit + @"Dim gsNextLine" + lsMyGuid + @"");
                                loStringBuilder.AppendLine(lsInherit + @"Dim gsNewFile" + lsMyGuid + @"");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                                loStringBuilder.AppendLine(lsInherit + @"If goFSO.FileExists(goWshShell.ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")) Then");
                                loStringBuilder.AppendLine(lsInherit + @"Set goTextFile = goFSO.OpenTextFile(goWshShell.ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @"""),1)");
                                loStringBuilder.AppendLine(lsInherit + @"Do Until goTextFile.AtEndOfStream");
                                loStringBuilder.AppendLine(lsInherit + @"    gsNextLine" + lsMyGuid + @" = goTextFile.Readline");
                                loStringBuilder.AppendLine(lsInherit + @"	 gsNextLine" + lsMyGuid + @" = Replace(gsNextLine" + lsMyGuid + @", ExpandEnvironmentStringsEx(""" + lsSource + @"""), ExpandEnvironmentStringsEx(""" + lsDestination + @"""))");
                                loStringBuilder.AppendLine(lsInherit + @"    gsNewFile" + lsMyGuid + @" = gsNewFile" + lsMyGuid + @" & gsNextLine" + lsMyGuid + @" & vbCrLf");
                                loStringBuilder.AppendLine(lsInherit + @"Loop");
                                loStringBuilder.AppendLine(lsInherit + @"goTextFile.Close");
                                loStringBuilder.AppendLine(lsInherit + @"Set goTextFile = goFSO.OpenTextFile(ExpandEnvironmentStringsEx(""" + loRow["command"].ToString() + @"""), 2)");
                                loStringBuilder.AppendLine(lsInherit + @"goTextFile.WriteLine gsNewFile" + lsMyGuid + @"");
                                loStringBuilder.AppendLine(lsInherit + @"goTextFile.Close");
                                loStringBuilder.AppendLine(lsInherit + @"Set goTextFile = Nothing");
                                loStringBuilder.AppendLine(lsInherit + @"On Error Goto 0");
                                loStringBuilder.AppendLine(lsInherit + @"End If");
                            }
                            break;
                        case "addtext":

                            break;
                        case "checkfile":
                            loStringBuilder.AppendLine(lsInherit + @"If goFSO.FileExists(ExpandEnvironmentStringsEx(""" + loRow["path"].ToString() + @""")) Then");
                            break;
                        case "checkarchitecture":
                            switch (loRow["condition"].ToString())
                            {
                                case "x86":
                                    loStringBuilder.AppendLine(lsInherit + @"If not goFSO.FolderExists(ExpandEnvironmentStringsEx(""%windir%\syswow64"")) Then");
                                    break;
                                case "x64":
                                    loStringBuilder.AppendLine(lsInherit + @"If goFSO.FolderExists(ExpandEnvironmentStringsEx(""%windir%\syswow64"")) Then");
                                    break;
                            }
                            break;

                        case "endcheck":
                            loStringBuilder.AppendLine(lsInherit + @"End If");
                            break;
                    }
                }

                if (lsAppVPackageId != "")
                {
                    loStringBuilder.AppendLine(@"");
                    loStringBuilder.AppendLine(@"goWshShell.Run ""robocopy """"%LOCALAPPDATA%\Microsoft\AppV\Client\VFS\" + lsAppVPackageId + @"\CommonAppDataS"""" """"%LOCALAPPDATA%\Microsoft\AppV\Client\VFS\" + lsAppVPackageId + @"\CommonAppData"""" /E /XO"",0,true");
                    loStringBuilder.AppendLine(@"goWshShell.Run ""robocopy """"%LOCALAPPDATA%\Microsoft\AppV\Client\VFS\" + lsAppVPackageId + @"\CommonAppData"""" """"%LOCALAPPDATA%\Microsoft\AppV\Client\VFS\" + lsAppVPackageId + @"\CommonAppDataS"""" /E /XO"",0,true");
                    loStringBuilder.AppendLine(@"");
                }



                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Set goWMI         = Nothing");
                loStringBuilder.AppendLine(@"Set goFSO         = Nothing");
                loStringBuilder.AppendLine(@"Set goApplication = Nothing");
                loStringBuilder.AppendLine(@"Set goWshShell    = Nothing");
                loStringBuilder.AppendLine(@"Set goRegistry    = Nothing");

                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Function ExpandEnvironmentStringsEx(lsString)");
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"   Dim loRegKeys, loRegKey, lsValue, lsReturn");
                loStringBuilder.AppendLine(@"   lsReturn = goWshShell.ExpandEnvironmentStrings(lsString)");
                loStringBuilder.AppendLine(@"   If lsReturn = lsString Then");
                loStringBuilder.AppendLine(@"      If Left(lsReturn,1)=""%"" Then");
                loStringBuilder.AppendLine(@"         goRegistry.EnumKey HKEY_CURRENT_USER, ""Volatile Environment"", loRegKeys");
                loStringBuilder.AppendLine(@"         If Ubound(loRegKeys)>=0 Then");
                loStringBuilder.AppendLine(@"         For Each loRegKey in loRegKeys");
                loStringBuilder.AppendLine(@"            lsValue = """"");
                loStringBuilder.AppendLine(@"            On Error Resume Next");
                loStringBuilder.AppendLine(@"            lsValue = goWshShell.RegRead(""HKEY_CURRENT_USER\Volatile Environment\"" + loRegKey + ""\"" + Replace(lsString,""%"",""""))");
                loStringBuilder.AppendLine(@"            On Error Goto 0");
                loStringBuilder.AppendLine(@"            If lsValue <> """" Then");
                loStringBuilder.AppendLine(@"               lsReturn = lsValue");
                loStringBuilder.AppendLine(@"            End If");
                loStringBuilder.AppendLine(@"         Next");
                loStringBuilder.AppendLine(@"         End If");
                loStringBuilder.AppendLine(@"      End If");
                loStringBuilder.AppendLine(@"   End If");
                loStringBuilder.AppendLine(@"   ExpandEnvironmentStringsEx = lsReturn");
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"End Function");
                loStringBuilder.AppendLine(@"");
                loStringBuilder.AppendLine(@"Function GetSIDFromUser(UserName)");
                loStringBuilder.AppendLine(@"  Dim DomainName, Result, WMIUser");
                loStringBuilder.AppendLine(@"  If InStr(UserName, ""\"") > 0 Then");
                loStringBuilder.AppendLine(@"    DomainName = Mid(UserName, 1, InStr(UserName, ""\"") - 1)");
                loStringBuilder.AppendLine(@"    UserName = Mid(UserName, InStr(UserName, ""\"") + 1)");
                loStringBuilder.AppendLine(@"  Else");
                loStringBuilder.AppendLine(@"    DomainName = CreateObject(""WScript.Network"").UserDomain");
                loStringBuilder.AppendLine(@"  End If");
                loStringBuilder.AppendLine(@"  On Error Resume Next");
                loStringBuilder.AppendLine(@"  Set WMIUser = GetObject(""winmgmts:{impersonationlevel=impersonate}!"" _");
                loStringBuilder.AppendLine(@"    & ""/root/cimv2:Win32_UserAccount.Domain='"" & DomainName & ""'"" _");
                loStringBuilder.AppendLine(@"    & "",Name='"" & UserName & ""'"")");
                loStringBuilder.AppendLine(@"  If Err.Number = 0 Then");
                loStringBuilder.AppendLine(@"    Result = WMIUser.SID");
                loStringBuilder.AppendLine(@"  Else");
                loStringBuilder.AppendLine(@"    Result = """"");
                loStringBuilder.AppendLine(@"  End If");
                loStringBuilder.AppendLine(@"  On Error GoTo 0");
                loStringBuilder.AppendLine(@"  GetSIDFromUser = Result");
                loStringBuilder.AppendLine(@"End Function");


            }

            return loStringBuilder.ToString();


        }

        public static string CreatePSScripts(string lsType, string lsDeployXML, string lsManufacturer, string lsProduct, string lsVersion)
        {

            DataTable loVarTable = new DataTable();
            loVarTable.Columns.Add("name", typeof(string));
            loVarTable.Columns.Add("value", typeof(string));


            DataTable loTable = ConvertXMLToDataTable(lsType, lsDeployXML, false);




            StringBuilder loStringBuilder = new StringBuilder();
            loStringBuilder.AppendLine(@"# Manufacturer: " + lsManufacturer);
            loStringBuilder.AppendLine(@"# Product:      " + lsProduct);
            loStringBuilder.AppendLine(@"# Version:      " + lsVersion);
            loStringBuilder.AppendLine(@"# Date:         " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            loStringBuilder.AppendLine(@"");

            //     $expanded = $Path -replace '%([^%]+)%', {
            //        param($m)
            //  $name = $m.Groups[1].Value
            // $val = (Get-Item "env:$name" -ErrorAction SilentlyContinue).Value
            //   if ($null -ne $val) { $val } else { $m.Value }  # keep original if not found
            // }

            //$expanded

            loStringBuilder.AppendLine("function ExpandEnvironmentStrings {");
            loStringBuilder.AppendLine("param(");
            loStringBuilder.AppendLine("[string]$Path");
            loStringBuilder.AppendLine(")");


            loStringBuilder.AppendLine("$result = $Path");
            loStringBuilder.AppendLine(@"foreach ($envVar in Get-ChildItem env:) {");
            loStringBuilder.AppendLine(@"$token = ""%$($envVar.Name)%""");
            loStringBuilder.AppendLine(@"if ($result -like ""*$token*"") {");
            loStringBuilder.AppendLine(@"    $result = $result.Replace($token, $envVar.Value)");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine("}");
            loStringBuilder.AppendLine("$result");

            loStringBuilder.AppendLine("}");



            loStringBuilder.AppendLine(@"$oslanguage = ""1033""");
            loStringBuilder.AppendLine(@"$prefereduilanguage = ""en-US""");
            loStringBuilder.AppendLine(@"try {");
            loStringBuilder.AppendLine(@"$prefereduilanguage = get-systempreferreduilanguage");
            loStringBuilder.AppendLine(@"} catch { }");
            loStringBuilder.AppendLine(@"$prefereduilanguage = $prefereduilanguage.ToLower()");
            loStringBuilder.AppendLine(@"if($prefereduilanguage.StartsWith(""en-""))");
            loStringBuilder.AppendLine(@"{");
            loStringBuilder.AppendLine(@"$oslanguage = ""1033""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"if($prefereduilanguage.StartsWith(""de-""))");
            loStringBuilder.AppendLine(@"{");
            loStringBuilder.AppendLine(@"$oslanguage = ""1031""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"if($prefereduilanguage.StartsWith(""fr-""))");
            loStringBuilder.AppendLine(@"{");
            loStringBuilder.AppendLine(@"$oslanguage = ""1036""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"if($prefereduilanguage.StartsWith(""es-""))");
            loStringBuilder.AppendLine(@"{");
            loStringBuilder.AppendLine(@"$oslanguage = ""1034""");
            loStringBuilder.AppendLine(@"}");
            loStringBuilder.AppendLine(@"if($prefereduilanguage.StartsWith(""it-""))");
            loStringBuilder.AppendLine(@"{");
            loStringBuilder.AppendLine(@"$oslanguage = ""1040""");
            loStringBuilder.AppendLine(@"}");


            foreach (DataRow loRow in loTable.Rows)
            {
                writeToLogFile("createpsscript " + loRow["actiontype"].ToString() + ":" + loRow["param"].ToString());

                string lsInherit = "";
                for (int i = 1; i < Convert.ToInt32(loRow["level"].ToString()); i++)
                {
                    lsInherit = lsInherit + "  ";
                }
                switch (loRow["actiontype"].ToString().ToLower())
                {
                    case "launchprocess":

                        //fix

                        loStringBuilder.AppendLine(@"try {");
                        //loStringBuilder.AppendLine(@"    $codeCmd = ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""") ");

                        loStringBuilder.AppendLine(@"    $codeCmd = ExpandEnvironmentStrings(" + PowerShellQuote(loRow["command"].ToString()) + @")");
                        loStringBuilder.AppendLine(@"    $arguments = ExpandEnvironmentStrings(" + PowerShellQuote(loRow["param"].ToString()) + @")");
                        loStringBuilder.AppendLine(@"");
                        loStringBuilder.AppendLine(@"    $p = Start-Process -FilePath $codeCmd -ArgumentList $arguments -Wait -PassThru -NoNewWindow ");
                        loStringBuilder.AppendLine(@"");
                        loStringBuilder.AppendLine(@"    $exitCode = $p.ExitCode");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch {");
                        loStringBuilder.AppendLine(@"    Write-Host $_.Exception.Message");
                        loStringBuilder.AppendLine(@"}");

                        //loStringBuilder.AppendLine(@"# try {");
                        //loStringBuilder.AppendLine(@"# $ps = new-object System.Diagnostics.Process");
                        //loStringBuilder.AppendLine(@"# $ps.StartInfo.Filename = ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                        //loStringBuilder.AppendLine(@"# $ps.StartInfo.Arguments = ExpandEnvironmentStrings(""" + loRow["param"].ToString() + @""")");
                        //loStringBuilder.AppendLine(@"# $ps.StartInfo.RedirectStandardOutput = $True");
                        //loStringBuilder.AppendLine(@"# $ps.StartInfo.UseShellExecute = $false");
                        //loStringBuilder.AppendLine(@"# $ps.start()  | Out-Null");
                        //loStringBuilder.AppendLine(@"# $ps.WaitForExit()");
                        //loStringBuilder.AppendLine(@"# [string] $Out = $ps.StandardOutput.ReadToEnd();");
                        //loStringBuilder.AppendLine(@"# }");
                        //loStringBuilder.AppendLine(@"# catch { }");
                        break;
                    case "msiexec":
                        {
                            switch (loRow["command"].ToString().ToLower())
                            {
                                case "/i":
                                    loStringBuilder.AppendLine(@"try {");
                                    loStringBuilder.AppendLine(@"$ps = new-object System.Diagnostics.Process");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.Filename = ExpandEnvironmentStrings(""msiexec.exe"")");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.Arguments = ""/i " + loRow["param"].ToString().Replace(@"""", @"'") + @"""");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.RedirectStandardOutput = $True");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.UseShellExecute = $false");
                                    loStringBuilder.AppendLine(@"$ps.start() | Out-Null");
                                    loStringBuilder.AppendLine(@"$ps.WaitForExit()");
                                    loStringBuilder.AppendLine(@"[string] $Out = $ps.StandardOutput.ReadToEnd();");
                                    loStringBuilder.AppendLine(@"}");
                                    loStringBuilder.AppendLine(@"catch { }");
                                    break;
                                default:
                                    loStringBuilder.AppendLine(@"try {");
                                    loStringBuilder.AppendLine(@"$ps = new-object System.Diagnostics.Process");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.Filename = ExpandEnvironmentStrings(""msiexec.exe"")");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.Arguments = ""/x " + loRow["param"].ToString().Replace(@"""", @"'") + @"""");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.RedirectStandardOutput = $True");
                                    loStringBuilder.AppendLine(@"$ps.StartInfo.UseShellExecute = $false");
                                    loStringBuilder.AppendLine(@"$ps.start() | Out-Null");
                                    loStringBuilder.AppendLine(@"$ps.WaitForExit()");
                                    loStringBuilder.AppendLine(@"[string] $Out = $ps.StandardOutput.ReadToEnd();");
                                    loStringBuilder.AppendLine(@"}");
                                    loStringBuilder.AppendLine(@"catch { }");
                                    break;
                            }
                        }

                        break;
                    case "launchprocessnowait":

                        //loStringBuilder.AppendLine(@"try {");
                        //loStringBuilder.AppendLine(@"$ps = new-object System.Diagnostics.Process");
                        //loStringBuilder.AppendLine(@"$ps.StartInfo.Filename = ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                        //loStringBuilder.AppendLine(@"$ps.StartInfo.Arguments = ExpandEnvironmentStrings(""" + loRow["param"].ToString().Replace(@"""", @"'") + @""")");
                        //loStringBuilder.AppendLine(@"$ps.StartInfo.RedirectStandardOutput = $false");
                        //loStringBuilder.AppendLine(@"$ps.StartInfo.UseShellExecute = $false");
                        //loStringBuilder.AppendLine(@"$ps.start() | Out-Null");
                        ////loStringBuilder.AppendLine(@"$ps.WaitForExit()");
                        ////loStringBuilder.AppendLine(@"[string] $Out = $ps.StandardOutput.ReadToEnd();");
                        //loStringBuilder.AppendLine(@"}");
                        //loStringBuilder.AppendLine(@"catch { }");


                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"    $codeCmd = ExpandEnvironmentStrings(" + PowerShellQuote(loRow["command"].ToString()) + @")");
                        loStringBuilder.AppendLine(@"    $arguments = ExpandEnvironmentStrings(" + PowerShellQuote(loRow["param"].ToString()) + @")");
                        loStringBuilder.AppendLine(@"");
                        loStringBuilder.AppendLine(@"    $p = Start-Process -FilePath $codeCmd -ArgumentList $arguments -PassThru -NoNewWindow ");
                        loStringBuilder.AppendLine(@"");
                        loStringBuilder.AppendLine(@"    $exitCode = $p.ExitCode");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch {");
                        loStringBuilder.AppendLine(@"    Write-Host $_.Exception.Message");
                        loStringBuilder.AppendLine(@"}");

                        break;


                    case "sleep":
                        int liSeconds = 5;
                        try
                        {
                            liSeconds = Convert.ToInt32(loRow["param"].ToString());
                        }
                        catch { }
                        loStringBuilder.AppendLine("Start-Sleep -s " + liSeconds);
                        //loStringBuilder.AppendLine(lsInherit + @"WScript.Sleep " + liSeconds.ToString() + "000");
                        break;



                    case "killprocess":
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"stop-process -name '" + loRow["command"].ToString() + @"' -Force");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        break;
                    case "abort":
                        //loStringBuilder.AppendLine(@"exit");
                        break;

                    case "checkvariable":
                        loStringBuilder.AppendLine("# checkvariable command " + loRow["command"].ToString() + "  param " + loRow["param"].ToString());
                        loStringBuilder.AppendLine("if ($custom" + loRow["command"].ToString() + " -eq '" + loRow["param"].ToString() + "') {");
                        break;

                    case "setvariable":
                        loStringBuilder.AppendLine("# setvariable command " + loRow["command"].ToString() + "  param " + loRow["param"].ToString());

                        loStringBuilder.AppendLine(lsInherit + @"$custom" + loRow["command"].ToString() + @"=""" + loRow["param"].ToString() + @"""");

                        if (loVarTable.Select("name='custom" + loRow["command"].ToString() + "'").Length == 0)
                        {
                            DataRow loNewRow = loVarTable.NewRow();
                            loNewRow["name"] = loRow["command"].ToString();
                            loNewRow["value"] = "";
                            loVarTable.Rows.Add(loNewRow);
                        }


                        //if (!loStringBuilder.ToString().Contains(@"Dim custom" + loRow["command"].ToString()))
                        //{
                        //    loStringBuilder.AppendLine(lsInherit + @"Dim custom" + loRow["command"].ToString());
                        //}
                        //loStringBuilder.AppendLine(lsInherit + @"custom" + loRow["command"].ToString() + @"=""" + loRow["param"].ToString() + @"""");

                        //if (loVarTable.Select("name='custom" + loRow["command"].ToString() + "'").Length == 0)
                        //{
                        //    DataRow loNewRow = loVarTable.NewRow();
                        //    loNewRow["name"] = loRow["command"].ToString();
                        //    loNewRow["value"] = "";
                        //    loVarTable.Rows.Add(loNewRow);
                        //}

                        break;

                    case "copyfolder":
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$path = ExpandEnvironmentStrings(""" + System.IO.Path.GetDirectoryName(loRow["param"].ToString()) + @""")");
                        //loStringBuilder.AppendLine(@"$path=""$env:" + System.IO.Path.GetDirectoryName(loRow["param"].ToString()) + @"""");
                        loStringBuilder.AppendLine(@"New-Item -ItemType directory -Path $path -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$source = ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                        loStringBuilder.AppendLine(@"$destination = ExpandEnvironmentStrings(""" + loRow["param"].ToString() + @""")");
                        loStringBuilder.AppendLine(@"New-Item -ItemType Directory -Path $destination -Force -ErrorAction SilentlyContinue | Out-Null");
                        loStringBuilder.AppendLine(@"Copy-Item -Path(Join-Path $source '*') -Destination $destination -Recurse -Force -ErrorAction SilentlyContinue");
                        // loStringBuilder.AppendLine(@"Copy-Item -Path $source -Destination $destination -Recurse -force  -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        break;

                    case "copyfile":
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$path=ExpandEnvironmentStrings(""" + System.IO.Path.GetDirectoryName(loRow["param"].ToString()) + @""")");
                        loStringBuilder.AppendLine(@"New-Item -ItemType directory -Path $path -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$source=ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                        loStringBuilder.AppendLine(@"$destination=ExpandEnvironmentStrings(""" + loRow["param"].ToString() + @""")");
                        loStringBuilder.AppendLine(@"Copy-Item $source -Destination $destination -force  -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        break;
                    case "addfolder":
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$path=ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                        loStringBuilder.AppendLine(@"New-Item -ItemType directory -Path $path  -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        break;
                    case "removefile":
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$path=ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                        loStringBuilder.AppendLine(@"Remove-Item -Path $path -Force  -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        break;
                    case "removefolder":
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$path=ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @"\*"")");
                        loStringBuilder.AppendLine(@"Remove-Item -Path $path -recurse -Force  -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        loStringBuilder.AppendLine(@"try {");
                        loStringBuilder.AppendLine(@"$path=ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                        loStringBuilder.AppendLine(@"Remove-Item -Path $path -Force -ErrorAction SilentlyContinue");
                        loStringBuilder.AppendLine(@"}");
                        loStringBuilder.AppendLine(@"catch { }");
                        break;
                    case "checkregistry":
                        {
                            //string lsVariableName = "var" + Guid.NewGuid().ToString().Replace("-", "").ToLower();
                            string lsMyPath = loRow["path"].ToString();
                            string lsMyValue = loRow["command"].ToString();
                            string lsMyRoot = "HKLM";
                            if (lsMyPath.ToUpper().StartsWith("HKLM"))
                            {
                                lsMyPath = lsMyPath.Substring(5);
                            }
                            if (lsMyPath.ToUpper().StartsWith("HKCR"))
                            {
                                lsMyPath = lsMyPath.Substring(5);
                                lsMyRoot = "HKCR";
                            }
                            if (lsMyPath.ToUpper().StartsWith("HKCU"))
                            {
                                lsMyPath = lsMyPath.Substring(5);
                                lsMyRoot = "HKCU";
                            }
                            loStringBuilder.AppendLine("if (Test-Path '" + lsMyRoot + @":\" + lsMyPath + "')");
                            loStringBuilder.AppendLine("{");

                        }
                        break;
                    case "addregkey":
                        {
                            string lsRoot = "HKLM";
                            string lsPath = loRow["command"].ToString();
                            string lsName = "";
                            string lsMyType = "";
                            string lsValue = "";
                            try
                            {
                                lsPath = lsPath.Substring(lsPath.IndexOf(@"\") + 1);
                            }
                            catch { }
                            try
                            {
                                lsPath = lsPath.Substring(0, lsPath.IndexOf(Convert.ToChar("|")));
                            }
                            catch { }
                            try
                            {
                                lsName = loRow["command"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCU"))
                            {
                                lsRoot = "HKCU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKU"))
                            {
                                lsRoot = "HKU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCR"))
                            {
                                lsRoot = "HKCR";
                            }
                            try
                            {
                                lsValue = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            try
                            {
                                lsMyType = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                            }
                            catch { }

                            switch (lsMyType.ToLower())
                            {
                                case "reg_sz":
                                    lsMyType="String";
                                    break;
                                case "reg_expand_sz":
                                    lsMyType="ExpandString";
                                    break;
                                case "reg_dword":
                                    lsMyType="DWord";
                                    break;
                                case "reg_binary":
                                    lsMyType="Binary";
                                    break;
                                case "reg_multi_sz":
                                    lsMyType="MultiString";
                                    break;
                            }

                            bool lbHasVars = false;
                            foreach (DataRow loVarRow in loVarTable.Rows)
                            {
                                if (lsValue.Contains("%" + loVarRow["name"].ToString() + "%"))
                                {
                                    lsValue = lsValue.Replace("%" + loVarRow["name"].ToString() + "%", @""" + $custom" + loVarRow["name"].ToString() + @" + """);
                                    lbHasVars = true;
                                }
                            }


                            loStringBuilder.AppendLine(@"try {");
                            loStringBuilder.AppendLine(@"$RegKey = """ + lsRoot + @":\" + lsPath + @"""");
                            if (lsMyType=="String")
                            {
                                if (lsValue.Contains(@""""))
                                {
                                    loStringBuilder.AppendLine(@"$RegValue = ExpandEnvironmentStrings(""" + lsValue + @""")");
                                }
                                else
                                {
                                    loStringBuilder.AppendLine(@"$RegValue = ExpandEnvironmentStrings(""" + lsValue + @""")");
                                }
                            }
                            else
                            {
                                if (lsValue.Contains(@""""))
                                {
                                    loStringBuilder.AppendLine(@"$RegValue = '" + lsValue + @"'");
                                }
                                else
                                {
                                    loStringBuilder.AppendLine(@"$RegValue = """ + lsValue + @"""");
                                }
                            }
                            loStringBuilder.AppendLine(@"if (-Not(Test-Path ""$RegKey"")) {");
                            loStringBuilder.AppendLine(@"New-Item -Path ""$($RegKey.TrimEnd($RegKey.Split('\')[-1]))"" -Name ""$($RegKey.Split('\')[-1])"" -Force | Out-Null");
                            loStringBuilder.AppendLine(@"}");
                            loStringBuilder.AppendLine(@"Set-ItemProperty -Path ""$RegKey"" -Name """ + lsName + @""" -Type " + lsMyType + @" -Value ""$RegValue""");
                            loStringBuilder.AppendLine(@"}");
                            loStringBuilder.AppendLine(@"catch { }");
                        }
                        break;


                    case "removeregkey":
                        {
                            string lsRoot = "HKLM";
                            string lsPath = loRow["command"].ToString();
                            string lsName = "";
                            try
                            {
                                lsPath = lsPath.Substring(lsPath.IndexOf(@"\") + 1);
                            }
                            catch { }
                            try
                            {
                                lsPath = lsPath.Substring(0, lsPath.IndexOf(Convert.ToChar("|")));
                            }
                            catch { }
                            try
                            {
                                lsName = loRow["command"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKEY_CURRENT_USER"))
                            {
                                lsRoot = "HKCU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKEY_USERS"))
                            {
                                lsRoot = "HKU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKEY_CLASSES_ROOT"))
                            {
                                lsRoot = "HKCR";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCU"))
                            {
                                lsRoot = "HKCU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKU"))
                            {
                                lsRoot = "HKU";
                            }
                            if (loRow["command"].ToString().ToUpper().StartsWith("HKCR"))
                            {
                                lsRoot = "HKCR";
                            }
                            if (lsPath.EndsWith(@"\"))
                            {
                                try
                                {
                                    lsPath = lsPath.Substring(0, lsPath.Length-1);
                                }
                                catch { }
                            }

                            loStringBuilder.AppendLine(@"# command is [" + loRow["command"].ToString() +"]");

                            if (lsName!="")
                            {
                                loStringBuilder.AppendLine(@"Remove-ItemProperty -Path '"" + lsRoot + @"":\"" + lsPath + @""' -Name '" + lsName + "'  -ErrorAction SilentlyContinue");
                            }
                            else
                            {
                                loStringBuilder.AppendLine(@"Remove-Item -Path '" + lsRoot + @":\" + lsPath + @"' -Recurse -Force  -ErrorAction SilentlyContinue");
                            }

                            //loStringBuilder.AppendLine(lsInherit + @"On Error Resume Next");
                            //if (lsName == "")
                            //{
                            //    loStringBuilder.AppendLine(lsInherit + @"goWshShell.RegDelete """ + lsRoot + @"\" + lsPath + @"""");
                            //}
                            //else
                            //{
                            //    loStringBuilder.AppendLine(lsInherit + @"goWshShell.RegDelete """ + lsRoot + @"\" + lsPath + @"\" + lsName + @"""");
                            //}
                            //loStringBuilder.AppendLine(lsInherit + @"On Error goto 0");
                        }
                        break;
                    case "checklanguagecode":
                        loStringBuilder.AppendLine(@"if ($oslanguage -eq '" + loRow["command"].ToString() + "')");
                        loStringBuilder.AppendLine(@"{");
                        break;


                    case "checkfile":
                        loStringBuilder.AppendLine(@"$testpath=ExpandEnvironmentStrings(""" + loRow["path"].ToString() + @""")");
                        switch (loRow["condition"].ToString())
                        {
                            case "notexists":
                                loStringBuilder.AppendLine(@"if (-not (Test-Path $testpath))");
                                break;
                            default:
                                loStringBuilder.AppendLine(@"if (Test-Path $testpath)");
                                break;
                        }
                        loStringBuilder.AppendLine(@"{");

                        break;

                    case "checkarchitecture":
                        loStringBuilder.AppendLine("$os_type = (Get-WmiObject -Class Win32_ComputerSystem).SystemType -match '(x64)'");
                        switch (loRow["condition"].ToString())
                        {
                            case "x86":
                                loStringBuilder.AppendLine(@"if ($os_type -eq $false) {");

                                break;
                            case "x64":
                                loStringBuilder.AppendLine(@"if ($os_type -eq $true) {");

                                break;
                        }
                        break;

                    case "endcheck":
                        loStringBuilder.AppendLine(lsInherit + @"}");
                        break;

                    case "replacetext":
                        {
                            string lsSource = "";
                            try
                            {
                                lsSource = loRow["param"].ToString().Split(Convert.ToChar("|"))[1];
                            }
                            catch { }
                            string lsDestination = "";
                            try
                            {
                                lsDestination = loRow["param"].ToString().Split(Convert.ToChar("|"))[0];
                            }
                            catch { }
                            loStringBuilder.AppendLine(@"$filename = ExpandEnvironmentStrings(""" + loRow["command"].ToString() + @""")");
                            loStringBuilder.AppendLine("(Get-Content $filename).replace('" + lsSource + "', '" + lsDestination + "') | Set-Content $filename");
                        }
                        break;
                }
            }

            loStringBuilder.AppendLine(@"");
            //loStringBuilder.AppendLine(@"Set goFSO      = Nothing");
            //loStringBuilder.AppendLine(@"Set goWshShell = Nothing");
            //loStringBuilder.AppendLine(@"Set goRegistry = Nothing");

            return loStringBuilder.ToString();

        }


        private static string PowerShellQuote(string value)
        {
            if (value == null)
                return "''";

            return "'" + value.Replace("'", "''") + "'";
        }

    }
}
