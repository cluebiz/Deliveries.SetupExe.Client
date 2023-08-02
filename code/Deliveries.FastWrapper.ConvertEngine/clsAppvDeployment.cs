using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Deliveries.FastWrapper.Logic
{
    public class clsAppvDeployment
    {

        public static string GetDynamicDeploymentFromManifest(string lsManifestContent, string lsType, bool lbEnableShortcuts, bool lbEnableExtensions, string lsUserInitializeText)
        {

            string lsAppvPackageId = "";
            string lsAppvVersionId = "";
            string lsDisplayName = "";

            try
            {
                XDocument loDoc = XDocument.Parse(lsManifestContent);

                writeToLogFile("parsing " + lsManifestContent);

                writeToLogFile("found " + loDoc.Root.FirstNode.ToString());

                foreach (XElement element in loDoc.Root.Elements())
                {
                    writeToLogFile("nodetype: " + element.NodeType.ToString());
                    switch (element.NodeType)
                    {
                        case XmlNodeType.Element:
                            writeToLogFile("name: " + element.Name);
                            if (element.Name.ToString().EndsWith("Identity"))
                            {
                                writeToLogFile("found identity");
                                foreach (XAttribute loAttribute in element.Attributes())
                                {
                                    writeToLogFile("found attribute: " + loAttribute.Name);
                                    if (loAttribute.Name.ToString().ToLower().EndsWith("packageid"))
                                    {
                                        lsAppvPackageId = loAttribute.Value;
                                    }
                                    if (loAttribute.Name.ToString().ToLower().EndsWith("versionid"))
                                    {
                                        lsAppvVersionId = loAttribute.Value;
                                    }
                                }
                            }
                            if (element.Name.ToString().EndsWith("Properties"))
                            {
                                writeToLogFile("found Properties");
                                foreach (XElement loPropertyNode in element.Elements())
                                {
                                    try
                                    {
                                        writeToLogFile("found node: " + loPropertyNode.Name);
                                        writeToLogFile("found value: " + loPropertyNode.Value);
                                        if (loPropertyNode.Name.ToString().EndsWith("DisplayName"))
                                        {
                                            if (lsDisplayName == "")
                                            {
                                                lsDisplayName = loPropertyNode.Value;
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                            if (element.Name.ToString().EndsWith("Applications"))
                            {
                                writeToLogFile("found Applications");
                                foreach (XElement loPropertyNode in element.Elements())
                                {
                                    try
                                    {
                                        writeToLogFile("found node: " + loPropertyNode.Name);
                                        writeToLogFile("found value: " + loPropertyNode.Value);
                                        if (loPropertyNode.Name.ToString().EndsWith("DisplayName"))
                                        {
                                            lsDisplayName = loPropertyNode.Value;
                                        }
                                    }
                                    catch { }
                                }
                            }
                            break;
                    }

                    }

            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }

            StringBuilder loStringBuilder = new StringBuilder();

            if (lsType.ToLower() == "user")
            {
                loStringBuilder.AppendLine(@"<UserConfiguration PackageId=""" + lsAppvPackageId + @""" DisplayName=""" + lsDisplayName.Trim() + @""" IgnorableNamespaces="""" xmlns=""http://schemas.microsoft.com/appv/2010/userconfiguration"">");
            }
            else
            {
                loStringBuilder.AppendLine(@"<DeploymentConfiguration PackageId=""" + lsAppvPackageId + @""" DisplayName=""" + lsDisplayName.Trim() + @""" IgnorableNamespaces="""" xmlns=""http://schemas.microsoft.com/appv/2010/deploymentconfiguration"">");
                loStringBuilder.AppendLine(@"<UserConfiguration>");
            }



            loStringBuilder.AppendLine(@"    <Subsystems>");

            bool lbHasShortcuts = false;

            try
            {
                XDocument loDoc = XDocument.Parse(lsManifestContent);

                foreach (XElement element in loDoc.Root.Elements())
                {
                    switch (element.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (element.Name.ToString().EndsWith("Extensions"))
                            {
                                foreach (XElement loExtensionNode in element.Elements())
                                {
                                    //writeToLogFile("extensionnode1: " + loExtensionNode.ToString());
                                    if(loExtensionNode.ToString().Contains("AppV.Shortcut"))
                                    {
                                        //writeToLogFile("found shortcuts...");
                                        if (!lbHasShortcuts)
                                        {
                                            if (lbEnableShortcuts)
                                            {
                                                loStringBuilder.AppendLine(@"      <Shortcuts Enabled=""true"">");
                                            }
                                            else
                                            {
                                                loStringBuilder.AppendLine(@"      <Shortcuts Enabled=""false"">");
                                            }
                                            loStringBuilder.AppendLine(@"           <Extensions>");
                                        }
                                        lbHasShortcuts = true;
                                        string lsFile = "";
                                        string lsTarget = "";
                                        string lsIcon = "";
                                        string lsArguments = "";
                                        string lsWorkingDirectory = "";
                                        string lsDescription = "";
                                        string lsShowCommand = "";
                                        string lsApplicationId = "";

                                        foreach (XElement loShortcutNode in loExtensionNode.Elements())
                                        {
                                            try
                                            {
                                                foreach(XElement loShortcutSubNode in loShortcutNode.Elements())
                                                {
                                                    //writeToLogFile(loShortcutSubNode.Name.ToString() + ": " + loShortcutSubNode.Value.ToString());
                                                    if(loShortcutSubNode.Name.ToString().ToLower().EndsWith("file"))
                                                    {
                                                        lsFile = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("target"))
                                                    {
                                                        lsTarget = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("icon"))
                                                    {
                                                        lsIcon = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("arguments"))
                                                    {
                                                        lsArguments = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("workingdirectory"))
                                                    {
                                                        lsWorkingDirectory = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("description"))
                                                    {
                                                        lsDescription = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("showcommand"))
                                                    {
                                                        lsShowCommand = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("applicationid"))
                                                    {
                                                        lsApplicationId = loShortcutSubNode.Value.ToString();
                                                    }
                                                }
                                            }
                                            catch { }
                                        }

                                        if(lsShowCommand=="")
                                        {
                                            lsShowCommand = "1";
                                        }


                                        
                                        loStringBuilder.AppendLine(@"          <Extension Category=""AppV.Shortcut"">");
                                        loStringBuilder.AppendLine(@"            <Shortcut>");
                                        loStringBuilder.AppendLine(@"              <File>" + lsFile + @"</File>");
                                        loStringBuilder.AppendLine(@"              <Target>" + lsTarget + @"</Target>");
                                        loStringBuilder.AppendLine(@"              <Icon>" + lsIcon + @"</Icon>");
                                        if (lsArguments == "")
                                        {
                                            loStringBuilder.AppendLine(@"              <Arguments />");
                                        }
                                        else
                                        {
                                            loStringBuilder.AppendLine(@"              <Arguments>" + lsArguments + @"</Arguments>");
                                        }
                                        loStringBuilder.AppendLine(@"              <WorkingDirectory>" + lsWorkingDirectory + @"</WorkingDirectory>");
                                        loStringBuilder.AppendLine(@"              <Description>" + lsDescription + @"</Description>");
                                        loStringBuilder.AppendLine(@"              <ShowCommand>" + lsShowCommand + "</ShowCommand>");
                                        loStringBuilder.AppendLine(@"              <ApplicationId>" + lsApplicationId + @"</ApplicationId>");
                                        loStringBuilder.AppendLine(@"            </Shortcut>");
                                        loStringBuilder.AppendLine(@"          </Extension>");
                                    }                                   
                                }
                            }
                            break;
                    }

                }

            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }

            if(lbHasShortcuts)
            {
                loStringBuilder.AppendLine(@"           </Extensions>");
                loStringBuilder.AppendLine(@"      </Shortcuts>");
            }




            bool lbHasFileTypeAssociations = false;

            try
            {
                XDocument loDoc = XDocument.Parse(lsManifestContent);

                foreach (XElement element in loDoc.Root.Elements())
                {
                    switch (element.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (element.Name.ToString().EndsWith("Extensions"))
                            {
                                foreach (XElement loAssociationNode in element.Elements())
                                {
                                    //writeToLogFile("extensionnode0: " + loAssociationNode.ToString());
                                    bool lbFoundsomething = false;
                                    if (loAssociationNode.ToString().Contains("AppV.FileTypeAssociation"))
                                    {
                                        lbFoundsomething = true;
                                        //writeToLogFile("found FileTypeAssociation...");
                                        if (!lbHasFileTypeAssociations)
                                        {
                                            if (lbEnableExtensions)
                                            {
                                                loStringBuilder.AppendLine(@" <FileTypeAssociations Enabled=""true"">");
                                            }
                                            else
                                            {
                                                loStringBuilder.AppendLine(@" <FileTypeAssociations Enabled=""false"">");
                                            }
                                            loStringBuilder.AppendLine(@"           <Extensions>");
                                        }
                                        lbHasFileTypeAssociations = true;
                                        string lsName = "";
                                        string lsProgId = "";
                                        string lsContentType = "";

                                        string lsProgIdName = "";
                                        string lsProgIdDescription = "";
                                        string lsProgIdDefaultIcon = "";

                                        loStringBuilder.AppendLine(@"       <Extension Category=""AppV.FileTypeAssociation"">");
                                        loStringBuilder.AppendLine(@"            <FileTypeAssociation>");

                                        foreach (XElement loExtensionNode in loAssociationNode.Elements())
                                        {
                                            foreach(XElement loRealExtensionNode in loExtensionNode.Elements())
                                            {
                                                if (loRealExtensionNode.ToString().Contains("<appv:FileExtension"))
                                                {
                                                    writeToLogFile("now parsing" + loRealExtensionNode.ToString());
                                                    try
                                                    {
                                                        foreach (XElement loShortcutSubNode in loRealExtensionNode.Elements())
                                                        {
                                                            writeToLogFile(loShortcutSubNode.Name.ToString() + ": " + loShortcutSubNode.Value.ToString());
                                                            if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("name"))
                                                            {
                                                                if (lsName == "")
                                                                {
                                                                    lsName = loShortcutSubNode.Value.ToString();
                                                                }
                                                            }
                                                            if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("progid"))
                                                            {
                                                                if (lsProgId == "")
                                                                {
                                                                    lsProgId = loShortcutSubNode.Value.ToString();
                                                                }
                                                            }
                                                            if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("contenttype"))
                                                            {
                                                                if (lsContentType == "")
                                                                {
                                                                    lsContentType = loShortcutSubNode.Value.ToString();
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch { }
                                                }

                                            }
                                        }

                                        if (lsName != "")
                                        {
                                            loStringBuilder.AppendLine(@"              <FileExtension>");
                                            loStringBuilder.AppendLine(@"                <Name>" + lsName + @"</Name>");
                                            loStringBuilder.AppendLine(@"                <ProgId>" + lsProgId + @"</ProgId>");
                                            if (lsContentType != "")
                                            {
                                                loStringBuilder.AppendLine(@"                <ContentType>" + lsContentType + @"</ContentType>");
                                            }
                                            loStringBuilder.AppendLine(@"              </FileExtension>");
                                        }
                                        
                                        foreach (XElement loExtensionNode in loAssociationNode.Elements())
                                        {
                                            foreach (XElement loRealExtensionNode in loExtensionNode.Elements())
                                            {
                                                if (loRealExtensionNode.ToString().Contains("<appv:ProgId"))
                                                {
                                                    writeToLogFile("now parsing" + loRealExtensionNode.ToString());
                                                    try
                                                    {
                                                        foreach (XElement loShortcutSubNode in loRealExtensionNode.Elements())
                                                        {
                                                            writeToLogFile(loShortcutSubNode.Name.ToString() + ": " + loShortcutSubNode.Value.ToString());
                                                            if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("name"))
                                                            {
                                                                if (lsProgIdName == "")
                                                                {
                                                                    if(!loShortcutSubNode.Value.ToString().StartsWith("."))
                                                                    {
                                                                        lsProgIdName = loShortcutSubNode.Value.ToString();
                                                                    }
                                                                }
                                                            }
                                                            if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("description"))
                                                            {
                                                                if (lsProgIdDescription == "")
                                                                {
                                                                    lsProgIdDescription = loShortcutSubNode.Value.ToString();
                                                                }
                                                            }
                                                            if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("defaulticon"))
                                                            {
                                                                if (lsProgIdDefaultIcon == "")
                                                                {
                                                                    lsProgIdDefaultIcon = loShortcutSubNode.Value.ToString();
                                                                }
                                                            }
                                                        }
                                                    }
                                                    catch { }
                                                }

                                            }
                                        }
                                        if (lsProgIdName != "")
                                        {
                                            loStringBuilder.AppendLine(@"              <ProgId>");
                                            loStringBuilder.AppendLine(@"                <Name>" + lsProgIdName + @"</Name>");
                                            loStringBuilder.AppendLine(@"                <Description>" + lsProgIdDescription + @"</Description>");
                                            loStringBuilder.AppendLine(@"                <DefaultIcon>" + lsProgIdDefaultIcon + @"</DefaultIcon>");
                                            foreach (XElement loExtensionNode in loAssociationNode.Elements())
                                            {
                                                foreach (XElement loRealExtensionNode in loExtensionNode.Elements())
                                                {
                                                    if (loRealExtensionNode.ToString().Contains("<appv:ProgId"))
                                                    {
                                                        writeToLogFile("now parsing" + loRealExtensionNode.ToString());
                                                        try
                                                        {
                                                            foreach (XElement loShortcutSubNode in loRealExtensionNode.Elements())
                                                            {
                                                                writeToLogFile(loShortcutSubNode.Name.ToString() + ": " + loShortcutSubNode.Value.ToString());
                                                                if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("shellcommands"))
                                                                {
                                                                    loStringBuilder.AppendLine(@"               <ShellCommands>");
                                                                    
                                                                    foreach (XElement loShellCommand in loShortcutSubNode.Elements())
                                                                    {
                                                                        string lsShellApplicationId = "";
                                                                        string lsShellName = "";
                                                                        string lsShellFriendlyName = "";
                                                                        string lsShellCommandLine = "";
                                                                        foreach (XElement loShellSubCommand in loShellCommand.Elements())
                                                                        {
                                                                            if (loShellSubCommand.Name.ToString().ToLower().EndsWith("applicationid"))
                                                                            {
                                                                                lsShellApplicationId = loShellSubCommand.Value.ToString();
                                                                            }
                                                                            if (loShellSubCommand.Name.ToString().ToLower().EndsWith("name"))
                                                                            {
                                                                                if (lsShellName == "")
                                                                                {
                                                                                    lsShellName = loShellSubCommand.Value.ToString();
                                                                                }
                                                                            }
                                                                            if (loShellSubCommand.Name.ToString().ToLower().EndsWith("friendlyname"))
                                                                            {
                                                                                lsShellFriendlyName = loShellSubCommand.Value.ToString();
                                                                            }
                                                                            if (loShellSubCommand.Name.ToString().ToLower().EndsWith("commandline"))
                                                                            {
                                                                                lsShellCommandLine = loShellSubCommand.Value.ToString();
                                                                            }
                                                                        }
                                                                        if(!lsShellName.ToLower().Contains("&amp;"))
                                                                        {
                                                                            lsShellName = lsShellName.Replace("&", "&amp;");
                                                                        }
                                                                        if(!lsShellFriendlyName.ToLower().Contains("&amp;"))
                                                                        {
                                                                            lsShellFriendlyName = lsShellFriendlyName.Replace("&", "&amp;");
                                                                        }                                                                       
                                                                        if (lsShellName != "")
                                                                        {
                                                                            loStringBuilder.AppendLine(@"                        <ShellCommand>");
                                                                            loStringBuilder.AppendLine(@"							<ApplicationId>" + lsShellApplicationId + @"</ApplicationId>");
                                                                            loStringBuilder.AppendLine(@"							<Name>" + lsShellName + "</Name>");
                                                                            if (lsShellFriendlyName != "")
                                                                            {
                                                                                loStringBuilder.AppendLine(@"							<FriendlyName>" + lsShellFriendlyName + "</FriendlyName>");
                                                                            }
                                                                            loStringBuilder.AppendLine(@"							<CommandLine>" + lsShellCommandLine + "</CommandLine>");
                                                                            loStringBuilder.AppendLine(@"						</ShellCommand>");
                                                                        }
                                                                    }
                                                                    loStringBuilder.AppendLine(@"               </ShellCommands>");

                                                                }
                                                            }
                                                        }
                                                        catch { }
                                                    }

                                                }
                                            }
                                            loStringBuilder.AppendLine(@" </ProgId>");
                                        }
                                        loStringBuilder.AppendLine(@"            </FileTypeAssociation>");
                                        loStringBuilder.AppendLine(@"          </Extension>");

                                      
                                    }
                                    if(lbFoundsomething==false)
                                    {
                                        writeToLogFile("did not find something...");
                                    }
                                }
                            }
                            break;
                    }

                }

            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }

            if (lbHasFileTypeAssociations)
            {
                loStringBuilder.AppendLine(@"           </Extensions>");
                loStringBuilder.AppendLine(@"      </FileTypeAssociations>");
            }




            bool lbHasURLProtocols = false;

            try
            {
                XDocument loDoc = XDocument.Parse(lsManifestContent);

                foreach (XElement element in loDoc.Root.Elements())
                {
                    switch (element.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (element.Name.ToString().EndsWith("Extensions"))
                            {
                                foreach (XElement loExtensionNode in element.Elements())
                                {
                                    
                                    if (loExtensionNode.ToString().Contains("AppV.URLProtocol"))
                                    {
                                        writeToLogFile("extensionnodeurlprotocol: " + loExtensionNode.ToString());
                                        writeToLogFile("found urlprotocol...");
                                        if (!lbHasURLProtocols)
                                        {
                                            loStringBuilder.AppendLine(@"      <URLProtocols Enabled=""true"">");                                            
                                            loStringBuilder.AppendLine(@"           <Extensions>");
                                        }
                                        lbHasURLProtocols = true;

                                        string lsName = "";
                                       

                                        foreach (XElement loURLProtocolNode in loExtensionNode.Elements())
                                        {
                                            try
                                            {
                                                foreach (XElement loURLProtocolSubNode in loURLProtocolNode.Elements())
                                                {
                                                    writeToLogFile(loURLProtocolSubNode.Name.ToString() + ": " + loURLProtocolSubNode.Value.ToString());
                                                    if (loURLProtocolSubNode.Name.ToString().ToLower().EndsWith("name"))
                                                    {
                                                        lsName = loURLProtocolSubNode.Value.ToString();
                                                    }                                                    
                                                }
                                            }
                                            catch { }
                                        }



                                        loStringBuilder.AppendLine(@"          <Extension Category=""AppV.URLProtocol"">");
                                        loStringBuilder.AppendLine(@"            <URLProtocol>");
                                        loStringBuilder.AppendLine(@"              <Name>" + lsName + @"</Name>");
                                        
                                        foreach (XElement loURLProtocolNode in loExtensionNode.Elements())
                                        {
                                            try
                                            {
                                                foreach (XElement loURLProtocolSubNode in loURLProtocolNode.Elements())
                                                {
                                                    //writeToLogFile(loURLProtocolSubNode.Name.ToString() + "X: " + loURLProtocolSubNode.ToString());
                                                    if (loURLProtocolSubNode.Name.ToString().ToLower().EndsWith("applicationurlprotocol"))
                                                    {
                                                        //first run
                                                       // writeToLogFile("X: found applicatoinurlprotocol " + loURLProtocolSubNode.ToString());
                                                        string lsSubDescription = "";
                                                        string lsSubDefaultIcon = "";
                                                        string lsSubFriendlyTypeName = "";
                                                        string lsSubEditFlags = "";
                                                        foreach (XElement loURLProtocolshellcommandnode in loURLProtocolSubNode.Elements())
                                                        {
                                                            //writeToLogFile("X: element is " + loURLProtocolshellcommandnode.Name.ToString());
                                                            if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("shellcommands"))
                                                            {
                                                                if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("description"))
                                                                {
                                                                    lsSubDescription = loURLProtocolshellcommandnode.Value;
                                                                }
                                                                if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("defaulticon"))
                                                                {
                                                                    lsSubDefaultIcon = loURLProtocolshellcommandnode.Value;
                                                                }
                                                                if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("friendlytypename"))
                                                                {
                                                                    lsSubFriendlyTypeName = loURLProtocolshellcommandnode.Value;
                                                                }
                                                                if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("editflags"))
                                                                {
                                                                    lsSubEditFlags = loURLProtocolshellcommandnode.Value;
                                                                }
                                                            }
                                                        }
                                                        foreach (XElement loURLProtocolshellcommandnode in loURLProtocolSubNode.Elements())
                                                        {
                                                            //writeToLogFile("X: element is " + loURLProtocolshellcommandnode.Name.ToString());
                                                            if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("shellcommands"))
                                                            {
                                                                loStringBuilder.AppendLine(@"<ApplicationURLProtocol>");
                                                                if(lsSubDescription!="")
                                                                {
                                                                    loStringBuilder.AppendLine(@"<Description>" + lsSubDescription + @"</Description>");
                                                                }
                                                                if (lsSubDefaultIcon != "")
                                                                {
                                                                    loStringBuilder.AppendLine(@"<DefaultIcon>" + lsSubDefaultIcon + @"</DefaultIcon>");
                                                                }
                                                                if (lsSubFriendlyTypeName != "")
                                                                {
                                                                    loStringBuilder.AppendLine(@"<FriendlyTypeName>" + lsSubFriendlyTypeName + @"</FriendlyTypeName>");
                                                                }
                                                                if (lsSubEditFlags != "")
                                                                {
                                                                    loStringBuilder.AppendLine(@"<EditFlags>" + lsSubEditFlags + @"</EditFlags>");
                                                                }
                                                                loStringBuilder.AppendLine(@"<ShellCommands>");
                                                                string lsSubDefaultCommand = "";
                                                                foreach (XElement loURLProtocolShellCommand in loURLProtocolshellcommandnode.Elements())
                                                                {
                                                                    if (loURLProtocolShellCommand.Name.ToString().ToLower().EndsWith("defaultcommand"))
                                                                    {
                                                                        lsSubDefaultCommand = loURLProtocolShellCommand.Value.ToString();
                                                                    }
                                                                }
                                                                if(lsSubDefaultCommand!="")
                                                                {
                                                                    loStringBuilder.AppendLine("<DefaultCommand>" + lsSubDefaultCommand + "</DefaultCommand>");
                                                                }
                                                                foreach (XElement loURLProtocolShellCommand in loURLProtocolshellcommandnode.Elements())
                                                                {                                                                                                                                  
                                                                    if (loURLProtocolShellCommand.Name.ToString().ToLower().EndsWith("shellcommand"))
                                                                    {
                                                                        string lsShellAppId = "";
                                                                        string lsShellName = "";
                                                                        string lsShellCommandLine = "";
                                                                        foreach (XElement losuburlelement in loURLProtocolShellCommand.Elements())
                                                                        {
                                                                            if (losuburlelement.Name.ToString().ToLower().EndsWith("applicationid"))
                                                                            {
                                                                                lsShellAppId = losuburlelement.Value.ToString();
                                                                            }
                                                                            if (losuburlelement.Name.ToString().ToLower().EndsWith("name"))
                                                                            {
                                                                                lsShellName = losuburlelement.Value.ToString();
                                                                            }
                                                                            if (losuburlelement.Name.ToString().ToLower().EndsWith("commandline"))
                                                                            {
                                                                                lsShellCommandLine = losuburlelement.Value.ToString();
                                                                            }
                                                                        }
                                                                        if (lsShellAppId != "")
                                                                        {
                                                                            loStringBuilder.AppendLine("<ShellCommand>");
                                                                            loStringBuilder.AppendLine("<ApplicationId>" + lsShellAppId + "</ApplicationId>");
                                                                            if(lsShellName!="")
                                                                            {
                                                                                loStringBuilder.AppendLine("<Name>" + lsShellName + "</Name>");
                                                                            }
                                                                            if (lsShellCommandLine != "")
                                                                            {
                                                                                loStringBuilder.AppendLine("<CommandLine>" + lsShellCommandLine + "</CommandLine>");
                                                                            }
                                                                            loStringBuilder.AppendLine("<DdeExec>");
                                                                            loStringBuilder.AppendLine("<DdeCommand />");
                                                                            loStringBuilder.AppendLine("</DdeExec>");
                                                                            loStringBuilder.AppendLine("</ShellCommand>");
                                                                        }

                                                                    }
                                                                }
                                                                loStringBuilder.AppendLine(@"</ShellCommands>");
                                                                loStringBuilder.AppendLine(@"</ApplicationURLProtocol>");
                                                            }

                                                        }
                                                        
                                                    }
                                                }
                                            }
                                            catch { }
                                        }

                                        loStringBuilder.AppendLine(@"            </URLProtocol>");
                                        loStringBuilder.AppendLine(@"          </Extension>");
                                    }
                                }
                            }
                            break;
                    }

                }

            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }

            if (lbHasURLProtocols)
            {
                loStringBuilder.AppendLine(@"           </Extensions>");
                loStringBuilder.AppendLine(@"      </URLProtocols>");
            }

            
            loStringBuilder.AppendLine(@"      <COM Mode=""Isolated"">");
            loStringBuilder.AppendLine(@"        <IntegratedCOMAttributes OutOfProcessEnabled=""true"" InProcessEnabled=""false"" />");
            loStringBuilder.AppendLine(@"      </COM>");
            loStringBuilder.AppendLine(@"      <Objects Enabled=""true"" />");
            loStringBuilder.AppendLine(@"      <Registry Enabled=""true"">");
            loStringBuilder.AppendLine(@"      </Registry>");
            loStringBuilder.AppendLine(@"      <FileSystem Enabled=""true"" />");
            loStringBuilder.AppendLine(@"      <Fonts Enabled=""true"" />");


            bool lbHasEnvironmentVariables = false;
            loStringBuilder.AppendLine(@"      <EnvironmentVariables Enabled=""true"">");
            try
            {
                XDocument loDoc = XDocument.Parse(lsManifestContent);

                foreach (XElement element in loDoc.Root.Elements())
                {
                    switch (element.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (element.Name.ToString().EndsWith("Extensions"))
                            {
                                foreach (XElement loExtensionNode in element.Elements())
                                {

                                    if (loExtensionNode.ToString().Contains("AppV.EnvironmentVariables"))
                                    {
                                        writeToLogFile("environmentvariables: " + loExtensionNode.ToString());
                                        writeToLogFile("found environmentvariables...");

                                        string lsName = "";
                                        string lsValue = "";

                                        loStringBuilder.AppendLine(@"<Include>");

                                        foreach (XElement loURLEnvironmentNodeNode in loExtensionNode.Elements())
                                        {

                                            try
                                            {
                                                foreach (XElement loURLProtocolSubNode in loURLEnvironmentNodeNode.Elements())
                                                {
                                                    foreach (XElement loEnvNode in loURLProtocolSubNode.Elements())
                                                    {
                                                        writeToLogFile("envnode: " + loEnvNode.ToString());

                                                        lbHasEnvironmentVariables = true;

                                                        lsName = FindBetween(loEnvNode.ToString(),@"Name=""", @"""");                                                        
                                                        lsValue = FindBetween(loEnvNode.ToString(), @"Value=""", @"""");
                                                        
                                                        loStringBuilder.AppendLine(@"<Variable Name=""" + lsName + @""" Value=""" + lsValue + @""" />");
                                                    }
                                                }
                                            }
                                            catch { }
                                        }

                                        loStringBuilder.AppendLine(@"</Include>");

                                        //loStringBuilder.AppendLine(@"          <Extension Category=""AppV.URLProtocol"">");
                                        //loStringBuilder.AppendLine(@"            <URLProtocol>");
                                        //loStringBuilder.AppendLine(@"              <Name>" + lsName + @"</Name>");

                                        //foreach (XElement loURLProtocolNode in loExtensionNode.Elements())
                                        //{
                                        //    try
                                        //    {
                                        //        foreach (XElement loURLProtocolSubNode in loURLProtocolNode.Elements())
                                        //        {
                                        //            //writeToLogFile(loURLProtocolSubNode.Name.ToString() + "X: " + loURLProtocolSubNode.ToString());
                                        //            if (loURLProtocolSubNode.Name.ToString().ToLower().EndsWith("applicationurlprotocol"))
                                        //            {
                                        //                //first run
                                        //                // writeToLogFile("X: found applicatoinurlprotocol " + loURLProtocolSubNode.ToString());
                                        //                string lsSubDescription = "";
                                        //                string lsSubDefaultIcon = "";
                                        //                string lsSubFriendlyTypeName = "";
                                        //                string lsSubEditFlags = "";
                                        //                foreach (XElement loURLProtocolshellcommandnode in loURLProtocolSubNode.Elements())
                                        //                {
                                        //                    //writeToLogFile("X: element is " + loURLProtocolshellcommandnode.Name.ToString());
                                        //                    if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("shellcommands"))
                                        //                    {
                                        //                        if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("description"))
                                        //                        {
                                        //                            lsSubDescription = loURLProtocolshellcommandnode.Value;
                                        //                        }
                                        //                        if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("defaulticon"))
                                        //                        {
                                        //                            lsSubDefaultIcon = loURLProtocolshellcommandnode.Value;
                                        //                        }
                                        //                        if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("friendlytypename"))
                                        //                        {
                                        //                            lsSubFriendlyTypeName = loURLProtocolshellcommandnode.Value;
                                        //                        }
                                        //                        if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("editflags"))
                                        //                        {
                                        //                            lsSubEditFlags = loURLProtocolshellcommandnode.Value;
                                        //                        }
                                        //                    }
                                        //                }
                                        //                foreach (XElement loURLProtocolshellcommandnode in loURLProtocolSubNode.Elements())
                                        //                {
                                        //                    //writeToLogFile("X: element is " + loURLProtocolshellcommandnode.Name.ToString());
                                        //                    if (loURLProtocolshellcommandnode.Name.ToString().ToLower().EndsWith("shellcommands"))
                                        //                    {
                                        //                        loStringBuilder.AppendLine(@"<ApplicationURLProtocol>");
                                        //                        if (lsSubDescription != "")
                                        //                        {
                                        //                            loStringBuilder.AppendLine(@"<Description>" + lsSubDescription + @"</Description>");
                                        //                        }
                                        //                        if (lsSubDefaultIcon != "")
                                        //                        {
                                        //                            loStringBuilder.AppendLine(@"<DefaultIcon>" + lsSubDefaultIcon + @"</DefaultIcon>");
                                        //                        }
                                        //                        if (lsSubFriendlyTypeName != "")
                                        //                        {
                                        //                            loStringBuilder.AppendLine(@"<FriendlyTypeName>" + lsSubFriendlyTypeName + @"</FriendlyTypeName>");
                                        //                        }
                                        //                        if (lsSubEditFlags != "")
                                        //                        {
                                        //                            loStringBuilder.AppendLine(@"<EditFlags>" + lsSubEditFlags + @"</EditFlags>");
                                        //                        }
                                        //                        loStringBuilder.AppendLine(@"<ShellCommands>");
                                        //                        string lsSubDefaultCommand = "";
                                        //                        foreach (XElement loURLProtocolShellCommand in loURLProtocolshellcommandnode.Elements())
                                        //                        {
                                        //                            if (loURLProtocolShellCommand.Name.ToString().ToLower().EndsWith("defaultcommand"))
                                        //                            {
                                        //                                lsSubDefaultCommand = loURLProtocolShellCommand.Value.ToString();
                                        //                            }
                                        //                        }
                                        //                        if (lsSubDefaultCommand != "")
                                        //                        {
                                        //                            loStringBuilder.AppendLine("<DefaultCommand>" + lsSubDefaultCommand + "</DefaultCommand>");
                                        //                        }
                                        //                        foreach (XElement loURLProtocolShellCommand in loURLProtocolshellcommandnode.Elements())
                                        //                        {
                                        //                            if (loURLProtocolShellCommand.Name.ToString().ToLower().EndsWith("shellcommand"))
                                        //                            {
                                        //                                string lsShellAppId = "";
                                        //                                string lsShellName = "";
                                        //                                string lsShellCommandLine = "";
                                        //                                foreach (XElement losuburlelement in loURLProtocolShellCommand.Elements())
                                        //                                {
                                        //                                    if (losuburlelement.Name.ToString().ToLower().EndsWith("applicationid"))
                                        //                                    {
                                        //                                        lsShellAppId = losuburlelement.Value.ToString();
                                        //                                    }
                                        //                                    if (losuburlelement.Name.ToString().ToLower().EndsWith("name"))
                                        //                                    {
                                        //                                        lsShellName = losuburlelement.Value.ToString();
                                        //                                    }
                                        //                                    if (losuburlelement.Name.ToString().ToLower().EndsWith("commandline"))
                                        //                                    {
                                        //                                        lsShellCommandLine = losuburlelement.Value.ToString();
                                        //                                    }
                                        //                                }
                                        //                                if (lsShellAppId != "")
                                        //                                {
                                        //                                    loStringBuilder.AppendLine("<ShellCommand>");
                                        //                                    loStringBuilder.AppendLine("<ApplicationId>" + lsShellAppId + "</ApplicationId>");
                                        //                                    if (lsShellName != "")
                                        //                                    {
                                        //                                        loStringBuilder.AppendLine("<Name>" + lsShellName + "</Name>");
                                        //                                    }
                                        //                                    if (lsShellCommandLine != "")
                                        //                                    {
                                        //                                        loStringBuilder.AppendLine("<CommandLine>" + lsShellCommandLine + "</CommandLine>");
                                        //                                    }
                                        //                                    loStringBuilder.AppendLine("<DdeExec>");
                                        //                                    loStringBuilder.AppendLine("<DdeCommand />");
                                        //                                    loStringBuilder.AppendLine("</DdeExec>");
                                        //                                    loStringBuilder.AppendLine("</ShellCommand>");
                                        //                                }

                                        //                            }
                                        //                        }
                                        //                        loStringBuilder.AppendLine(@"</ShellCommands>");
                                        //                        loStringBuilder.AppendLine(@"</ApplicationURLProtocol>");
                                        //                    }

                                        //                }

                                        //            }
                                        //        }
                                        //    }
                                        //    catch { }
                                        //}

                                    }
                                }
                            }
                            break;
                    }

                }

            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }
            loStringBuilder.AppendLine(@"          </EnvironmentVariables>");



            loStringBuilder.AppendLine(@"      <Services Enabled=""true"" />");
            loStringBuilder.AppendLine(@"    </Subsystems>");


            try
            {
                XDocument loDoc = XDocument.Parse(lsManifestContent);

                foreach (XElement element in loDoc.Root.Elements())
                {
                    switch (element.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (element.Name.ToString().EndsWith("Applications"))
                            {
                                loStringBuilder.AppendLine("<Applications>");
                                foreach (XElement loApplicationNode in element.Elements())
                                {
                                    string lsTarget = "";
                                    string lsName = "";
                                    string lsVersion = "";
                                    foreach (XElement loApplicationSubNode in loApplicationNode.Elements())
                                    {
                                        try
                                        {
                                            if (loApplicationSubNode.Name.ToString().EndsWith("Target"))
                                            {
                                                lsTarget = loApplicationSubNode.Value;
                                            }
                                            if (loApplicationSubNode.Name.ToString().EndsWith("VisualElements"))
                                            {
                                                foreach (XElement loVisualNode in loApplicationSubNode.Elements())
                                                {
                                                    if(loVisualNode.Name.ToString().EndsWith("Name"))
                                                    {
                                                        lsName = loVisualNode.Value.ToString();
                                                        if(lsName.Contains("&"))
                                                        {
                                                            if(!lsName.Contains("&quot;"))
                                                            {
                                                                lsName = lsName.Replace("&", "&quot;");
                                                            }
                                                        }
                                                    }
                                                    if (loVisualNode.Name.ToString().EndsWith("Version"))
                                                    {
                                                        lsVersion = loVisualNode.Value.ToString();
                                                    }
                                                }                                                    
                                            }
                                        }
                                        catch { }
                                    }
                                    loStringBuilder.AppendLine(@"<Application Id=""" + lsTarget + @""" Enabled=""true"">");
                                    loStringBuilder.AppendLine(@"        <VisualElements>");
                                    loStringBuilder.AppendLine(@"          <Name>" + lsName + @"</Name>");
                                    loStringBuilder.AppendLine(@"          <Icon />");
                                    loStringBuilder.AppendLine(@"          <Description />");
                                    loStringBuilder.AppendLine(@"        </VisualElements>");
                                    loStringBuilder.AppendLine(@" </Application>");
                                }
                                loStringBuilder.AppendLine("</Applications>");
                            }
                            break;
                    }

                }

            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }

            //if(lsType.ToLower()=="user")
            //if(1==2)
            //{
            //    if(lsUserInitializeText!="")
            //    {
            //        loStringBuilder.AppendLine(@"<UserScripts>");

            //        loStringBuilder.AppendLine(@"<StartVirtualEnvironment RunInVirtualEnvironment=""true"">");
            //        loStringBuilder.AppendLine(@"<Path>wscript.exe</Path>");
            //        loStringBuilder.AppendLine(@"<Arguments>[{AppVPackageRoot}]\..\Scripts\StartVirtualEnvironment.vbs</Arguments>");
            //        loStringBuilder.AppendLine(@"<Wait RollbackOnError=""true"" />");
            //        loStringBuilder.AppendLine(@"</StartVirtualEnvironment>");

            //        loStringBuilder.AppendLine(@" </UserScripts>");

            //    }

            //}



            loStringBuilder.AppendLine(@" </UserConfiguration>");


            if (lsType.ToLower() != "user")
            {
                loStringBuilder.AppendLine(@"<MachineConfiguration>");


                loStringBuilder.AppendLine(@"<ProductSourceURLOptOut Enabled=""true"" />");
                loStringBuilder.AppendLine(@"    <Subsystems>");
                loStringBuilder.AppendLine(@"      <Registry>");
                loStringBuilder.AppendLine(@"      </Registry>");
                loStringBuilder.AppendLine(@"    </Subsystems>");
                loStringBuilder.AppendLine(@"    <TerminateChildProcesses>");
                loStringBuilder.AppendLine(@"    </TerminateChildProcesses>");

                loStringBuilder.AppendLine(@" </MachineConfiguration>");

                loStringBuilder.AppendLine(@"</DeploymentConfiguration>");
            }

            writeToLogFile(loStringBuilder.ToString());

            writeToLogFile("check encoding:");

            try
            {
                try
                {
                    writeToLogFile("encoding is " + Encoding.GetEncoding(loStringBuilder.ToString()).ToString());
                }
                catch (Exception error2) { writeToLogFile(error2.InnerException + " " + error2.InnerException.ToString()); }
            }
            catch { }

            writeToLogFile("returning now...");

            return loStringBuilder.ToString();

        }


        public static void ModifyAppV(string lsAppVFile, string lsExecuteable, string lsReleaseName, string lsManufacturer, string lsProduct, string lsDisplayName, string lsDescription, DataTable loParameterTable, string lsUserInitializeText, string lsInternalRelease, DataTable loExcludedAppVFile)
        {

            StringBuilder loStringBuilder = new StringBuilder();
            loStringBuilder.AppendLine(@";twc;");            

            bool lbIncludeShortcuts = true;
            bool lbNamedObj = false;
            bool lbComObj = false;
            bool lbFullWrite = true;
            bool lbAppvVersion = true;
            string lsInternalReleaseCalculated = "0.0.0.1";
            string lsAppVOS = "";
            foreach (DataRow loRow in loParameterTable.Rows)
            {
                writeToLogFile("parameter for modifyappv: " + loRow["name"].ToString() + "=" + loRow["value"].ToString());
                switch(loRow["name"].ToString().ToLower())
                {
                    case "shopappvinclshortcut":
                        if(loRow["value"].ToString().ToLower()=="disabled")
                        {
                            lbIncludeShortcuts = false;
                        }
                        break;
                    case "shopappvnamedobj":
                        if (loRow["value"].ToString().ToLower() == "enabled")
                        {
                            lbNamedObj = true;
                        }
                        break;
                    case "shopappvcomobj":
                        if (loRow["value"].ToString().ToLower() == "enabled")
                        {
                            lbComObj = true;
                        }
                        break;
                    case "shopappvfullwrite":
                        if (loRow["value"].ToString().ToLower() == "disabled")
                        {
                            lbFullWrite = false;
                        }
                        break;
                    case "shopappvversion":
                        if (loRow["value"].ToString().ToLower() == "disabled")
                        {
                            lbAppvVersion = false;
                        }
                        break;
                    case "shopappvos":
                        if(loRow["value"].ToString() == "dynamic")
                        {
                            lsAppVOS = "";
                        }
                        else
                        {
                            lsAppVOS = loRow["value"].ToString();
                        }
                        break;
                    case "internalrelease":
                        {
                            writeToLogFile("internalrelease is: " + loRow["value"].ToString());
                            try
                            {
                                lsInternalReleaseCalculated = "0.0.0." + Convert.ToInt32(loRow["value"].ToString()).ToString();
                            }
                            catch { }
                        }
                        break;
                }
            }
            

            if (lbAppvVersion)
            {
                try
                {
                    if (lsReleaseName.Split(Convert.ToChar(".")).Length == 4)
                    {
                        loStringBuilder.AppendLine(@"Set-ElementAttribute AppxManifest.xml -xpath ""appx:Identity"" -attributename ""Version"" -attributevalue """ + lsReleaseName + @"""");
                    }
                }
                catch { }
            }

            

            if (lbFullWrite == false)
            {
                loStringBuilder.AppendLine(@"Set-NodeText AppxManifest.xml -xpath ""appx:Properties/appv1.2:FullVFSWriteMode"" -nodetext ""false""");
            }
            else
            {
                loStringBuilder.AppendLine(@"Set-NodeText AppxManifest.xml -xpath ""appx:Properties/appv1.2:FullVFSWriteMode"" -nodetext ""true""");
            }

            //if (lsDescription != "")
            //{
            //    loStringBuilder.AppendLine(@"Set-NodeText AppxManifest.xml -xpath ""appx:Properties/appv:AppVPackageDescription"" -nodetext """ + lsDescription + @"""");
            //}
            //else
            {
                loStringBuilder.AppendLine(@"Set-NodeText AppxManifest.xml -xpath ""appx:Properties/appv:AppVPackageDescription"" -nodetext ""No description entered""");
            }

            if (lsDisplayName != "")
            {
                loStringBuilder.AppendLine(@"Set-NodeText AppxManifest.xml -xpath ""appx:Properties/appx:DisplayName"" -nodetext """ + lsDisplayName.Trim() + @"""");
            }

            //targetos

            loStringBuilder.AppendLine(@"Del-SingleNode AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes/appv:TargetOS""");

            if (lsAppVOS != "")
            {
                if (lsAppVOS.Contains("WC6.1x86"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_6.1_x86"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WC6.1x64"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_6.1_x64"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WC6.2x86"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_6.2_x86"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WC6.2x64"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_6.2_x64"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WC6.3x86"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_6.3_x86"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WC6.3x64"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_6.3_x64"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WC10.0x86"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_10.0_x86"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WC10.0x64"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsClient_10.0_x64"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                

                if (lsAppVOS.Contains("WS6.1x64"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsServer_6.1_x64"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WS6.2x64"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsServer_6.2_x64"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
                if (lsAppVOS.Contains("WS6.3x64"))
                {
                    loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appx:Prerequisites/appv:TargetOSes"" -elementname ""appv:TargetOS"" -elementtext ""WindowsServer_6.3_x64"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                }
            }
            loStringBuilder.AppendLine(@"");

            if (lbNamedObj == true)
            {
                loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appv:Extensions"" -elementname ""appv:Extension"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest""");
                loStringBuilder.AppendLine(@"Del-SingleNode AppxManifest.xml -xpath ""appv:Extensions/appv:Extension[@Category='AppV.Objects']""");
                loStringBuilder.AppendLine(@"Set-ElementAttribute AppxManifest.xml -xpath ""appv:Extensions/appv:Extension[last()]"" -attributename ""Category"" -attributevalue ""AppV.Objects""");
                loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appv:Extensions/appv:Extension[last()]"" -elementname ""appv:Objects"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest"" -createifnotexist");
                loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appv:Extensions/appv:Extension[last()]/appv:Objects[last()]"" -elementname ""appv:NotIsolate"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest"" -createifnotexist");
                loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appv:Extensions/appv:Extension[last()]/appv:Objects[last()]/appv:NotIsolate[last()]"" -elementname ""appv:Object"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest"" -createifnotexist");
                loStringBuilder.AppendLine(@"Set-ElementAttribute AppxManifest.xml -xpath ""appv:Extensions[last()]/appv:Extension[last()]/appv:Objects[last()]/appv:NotIsolate[last()]/appv:Object[last()]"" -attributename ""Name"" -attributevalue ""*""");
                loStringBuilder.AppendLine(@"");
            }
            

            if (lbComObj == true)
            {
                loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath """" -elementname ""appv:ExtensionsConfiguration"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest"" -createifnotexist");
                loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appv:ExtensionsConfiguration"" -elementname ""appv:COM"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest"" -createifnotexist");
                loStringBuilder.AppendLine(@"Set-ElementAttribute AppxManifest.xml -xpath ""appv:ExtensionsConfiguration/appv:COM[last()]"" -attributename ""Mode"" -attributevalue ""Integrated""");
                loStringBuilder.AppendLine(@"New-Element AppxManifest.xml -xpath ""appv:ExtensionsConfiguration/appv:COM[last()]"" -elementname ""appv:IntegratedCOMAttributes"" -namespaceURI ""http://schemas.microsoft.com/appv/2010/manifest"" -createifnotexist");
                loStringBuilder.AppendLine(@"Set-ElementAttribute AppxManifest.xml -xpath ""appv:ExtensionsConfiguration/appv:COM[last()]/appv:IntegratedCOMAttributes[last()]"" -attributename ""InProcessEnabled"" -attributevalue ""false"" ");
                loStringBuilder.AppendLine(@"Set-ElementAttribute AppxManifest.xml -xpath ""appv:ExtensionsConfiguration/appv:COM[last()]/appv:IntegratedCOMAttributes[last()]"" -attributename ""OutOfProcessEnabled"" -attributevalue ""true"" ");
                loStringBuilder.AppendLine(@"");
            }


            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\cluebiz\deliveries_setup\usertasks""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\cluebiz\deliveries_setup\talk""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\cluebiz\deliveries_setup""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\cluebiz""");

            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\Microsoft\ActiveSetup""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\Wow6432Node\Microsoft\ActiveSetup""");

            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SYSTEM\CurrentControlSet\Services\WSearch""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SYSTEM\CurrentControlSet\Control\MUI""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SYSTEM\CurrentControlSet\Services\BITS""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Notifications""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Notifications\Data""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\MACHINE\SOFTWARE\Microsoft\Windows Search""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\USER\[{AppVCurrentUserSID}]\Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\USER\[{AppVCurrentUserSID}]\Software\Microsoft\Windows\CurrentVersion\Internet Settings""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\USER\[{AppVCurrentUserSID}]_Classes\Local Settings\MuiCache""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\USER\.DEFAULT\SOFTWARE\CLASSES\LOCAL SETTINGS\MUICACHE""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubKeys ""REGISTRY\USER\.DEFAULT\SOFTWARE\CLASSES\LOCAL SETTINGS\MrtCache""");

            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubkeys ""REGISTRY\MACHINE\SOFTWARE\Microsoft\Active Setup""");
            loStringBuilder.AppendLine(@"Del-RegistryKeyAndSubkeys ""REGISTRY\MACHINE\SOFTWARE\WOW6432Node\Microsoft\Active Setup""");

            foreach(DataRow loRow in loExcludedAppVFile.Rows)
            {
                loStringBuilder.AppendLine(@"Del-File """ + ReplaceRealPathToAppv(loRow["filename"].ToString()) + @"""");
            }
            loStringBuilder.AppendLine(@"Del-File ""[{ProgramFilesX64}]\Mindjet\MindManager 18\ActiveSetup.vbs""");
            loStringBuilder.AppendLine(@"Del-File ""[{ProgramFilesX86}]\Mindjet\MindManager 18\ActiveSetup.vbs""");
            loStringBuilder.AppendLine(@"Del-File ""[{ProgramFilesX86}]\AudaCity\FirstTime.ini""");

            writeToLogFile("userinitializetext: " + lsUserInitializeText);

            string lsScript = Environment.ExpandEnvironmentVariables(System.IO.Path.GetDirectoryName(lsAppVFile) + @"\startvirtualenvironment.vbs");

            if (lsUserInitializeText != "")
            {
                try
                {
                    System.IO.File.Delete(lsScript);
                }
                catch { }                

                try
                {
                    StreamWriter swLog;
                    swLog = new StreamWriter(lsScript);
                    swLog.WriteLine(lsUserInitializeText);
                    swLog.Close();
                }
                catch { }

                loStringBuilder.AppendLine(@"Del-ScriptFile ""startvirtualenvironment.vbs""");

                //if(1==2)
                //{
                //    loStringBuilder.AppendLine(@"Add-ScriptFile """ + lsScript + @"""");
                //}

            }

            writeToLogFile("appvfile: ");
            writeToLogFile(loStringBuilder.ToString());

            string strLogFile = Environment.ExpandEnvironmentVariables(System.IO.Path.GetDirectoryName(lsAppVFile) + @"\update.twc");
            try
            {
                System.IO.File.Delete(strLogFile);
            }
            catch { }

            try
            {
                writeToLogFile("writing to " + strLogFile);
                StreamWriter swLog;
                swLog = new StreamWriter(strLogFile);
                swLog.WriteLine(loStringBuilder.ToString());
                swLog.Close();
            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }

            writeToLogFile("running batchfileupdate");

            try
            {
                Process loProcess = new Process();
                loProcess.StartInfo.FileName = lsExecuteable;
                loProcess.StartInfo.Arguments = @"/batchfileupdate """ + lsAppVFile + @""" """ + strLogFile + @""" -nofail";
                writeToLogFile(lsExecuteable);
                writeToLogFile(loProcess.StartInfo.Arguments);
                loProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(lsExecuteable);
                loProcess.Start();
                loProcess.WaitForExit();

                System.Threading.Thread.Sleep(1000);

            }
            catch { }

            writeToLogFile("internalreleasecalculated: " + lsInternalReleaseCalculated);

            if (lsInternalReleaseCalculated != "0.0.0.1" || lsInternalReleaseCalculated == "")
            {
                writeToLogFile("fixing versionid");
                writeToLogFile(lsExecuteable + @" ""/update """ + lsAppVFile + @""" /Set-ElementAttribute AppxManifest.xml -xpath appx:Identity -attributename appv:VersionId -attributevalue " + Guid.NewGuid().ToString());
                Process loProcess = new Process();
                loProcess.StartInfo.FileName = lsExecuteable;
                loProcess.StartInfo.Arguments = @"/update """ + lsAppVFile + @""" /Set-ElementAttribute AppxManifest.xml -xpath appx:Identity -attributename appv:VersionId -attributevalue " + Guid.NewGuid().ToString();
                writeToLogFile(lsExecuteable);
                writeToLogFile(loProcess.StartInfo.Arguments);
                loProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(lsExecuteable);
                loProcess.Start();
                loProcess.WaitForExit();
                System.Threading.Thread.Sleep(1000);

                writeToLogFile("fixing version");
                writeToLogFile(lsExecuteable + @" ""/update """ + lsAppVFile + @""" /Set-ElementAttribute AppxManifest.xml -xpath appx:Identity -attributename Version -attributevalue " + lsInternalReleaseCalculated);
                loProcess = new Process();
                loProcess.StartInfo.FileName = lsExecuteable;
                loProcess.StartInfo.Arguments = @"/update """ + lsAppVFile + @""" /Set-ElementAttribute AppxManifest.xml -xpath appx:Identity -attributename Version -attributevalue " + lsInternalReleaseCalculated;
                writeToLogFile(lsExecuteable);
                writeToLogFile(loProcess.StartInfo.Arguments);
                loProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(lsExecuteable);
                loProcess.Start();
                loProcess.WaitForExit();
                System.Threading.Thread.Sleep(1000);

            }


            try
            {
                //System.IO.File.Delete(strLogFile);
            }
            catch { }

            try
            {
                //System.IO.File.Delete(lsScript);
            }
            catch { }

            writeToLogFile("finished batchfileupdate");

        }

        public static string ModifyManifest(string lsManifestContent, string lsDescription)
        {

            writeToLogFile("modifying manifest: ");
            writeToLogFile("manifest before: " + lsManifestContent);
            writeToLogFile("comment is: " + lsDescription);

            string lsReturn = lsManifestContent;

            string lsValue = "";
            lsValue = FindBetween(lsReturn, "<appv:AppVPackageDescription>", "</appv:AppVPackageDescription>");

            writeToLogFile("found value: " + lsValue);

            lsReturn = lsReturn.Replace("<appv:AppVPackageDescription>" + lsValue + "</appv:AppVPackageDescription>", "<appv:AppVPackageDescription>" + lsDescription + "</appv:AppVPackageDescription>");

            writeToLogFile("manifest after: " + lsReturn);

            return lsReturn;
        }

        public static DataTable GetShortcutsFromManifest(string lsManifestContent)
        {

            DataTable loTable = new DataTable();
            loTable.Columns.Add("filename", typeof(string));
            loTable.Columns.Add("target", typeof(string));
            loTable.Columns.Add("arguments", typeof(string));
            loTable.Columns.Add("workdir", typeof(string));
            loTable.Columns.Add("iconfile", typeof(string));
            loTable.Columns.Add("iconindex", typeof(Int32));


            writeToLogFile("getshortcutsfrommanifest:");
            writeToLogFile(lsManifestContent);

            try
            {
                XDocument loDoc = XDocument.Parse(lsManifestContent);

                foreach (XElement element in loDoc.Root.Elements())
                {
                    switch (element.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (element.Name.ToString().EndsWith("Extensions"))
                            {
                                foreach (XElement loExtensionNode in element.Elements())
                                {
                                    if (loExtensionNode.ToString().Contains("AppV.Shortcut"))
                                    {

                                        writeToLogFile("extensionnodeget: " + loExtensionNode.ToString());

                                        string lsFile = "";
                                        string lsTarget = "";
                                        string lsIcon = "";
                                        string lsArguments = "";
                                        string lsWorkingDirectory = "";
                                        string lsDescription = "";
                                        string lsShowCommand = "";
                                        string lsApplicationId = "";

                                        foreach (XElement loShortcutNode in loExtensionNode.Elements())
                                        {
                                            try
                                            {
                                                foreach (XElement loShortcutSubNode in loShortcutNode.Elements())
                                                {
                                                    writeToLogFile("adding shortcut: " + loShortcutSubNode.Name.ToString() + ": " + loShortcutSubNode.Value.ToString());
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("file"))
                                                    {
                                                        lsFile = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("target"))
                                                    {
                                                        lsTarget = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("icon"))
                                                    {
                                                        lsIcon = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("arguments"))
                                                    {
                                                        lsArguments = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("workingdirectory"))
                                                    {
                                                        lsWorkingDirectory = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("description"))
                                                    {
                                                        lsDescription = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("showcommand"))
                                                    {
                                                        lsShowCommand = loShortcutSubNode.Value.ToString();
                                                    }
                                                    if (loShortcutSubNode.Name.ToString().ToLower().EndsWith("applicationid"))
                                                    {
                                                        lsApplicationId = loShortcutSubNode.Value.ToString();
                                                    }
                                                }
                                            }
                                            catch { }
                                        }

                                        writeToLogFile("adding row...");

                                        bool lbValid = true;
                                        if(lsFile.Contains("[{ProgramFiles"))
                                        {
                                            lbValid = false;
                                        }

                                        if (lbValid)
                                        {
                                            try
                                            {
                                                DataRow loRow = loTable.NewRow();
                                                loRow["filename"] = lsFile;
                                                loRow["target"] = lsTarget;
                                                loRow["workdir"] = lsWorkingDirectory;
                                                loRow["arguments"] = lsArguments;
                                                loRow["iconfile"] = lsIcon;
                                                loRow["iconindex"] = 0;
                                                loTable.Rows.Add(loRow);

                                            }
                                            catch (Exception ex) { writeToLogFile("row added error: " + ex.Message); }

                                            writeToLogFile("row added...");
                                        }
                                    }
                                }
                            }
                            break;
                    }

                }

            }
            catch (Exception ex) { writeToLogFile("error: " + ex.Message); }

            writeToLogFile("getshortcutsfrommanifest: found " + loTable.Rows.Count.ToString() + " items") ;

            return loTable;

        }

      
        public static void writeToLogFile(string logMessage)
        {

            string strLogMessage = string.Empty;
            string strLogFile = Environment.ExpandEnvironmentVariables(@"%temp%\appvdeployment.log");
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

        public static string ReplaceRealPathToAppv(string lsPath)
        {
            string lsReturn = lsPath;
            lsReturn = lsReturn.Replace("Root/VFS/ProgramFilesX64", "[{ProgramFilesX64}]");
            lsReturn = lsReturn.Replace("Root/VFS/ProgramFilesX86", "[{ProgramFilesX86}]");
            return lsReturn;
        }

        public static string ReplaceAppVPathToReal(string lsPath)
        {
            string lsReturn = lsPath;
            lsReturn = lsReturn.Replace("[{", "");
            lsReturn = lsReturn.Replace("}]", "");
            return lsReturn;
        }


    }
}
