using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Deliveries.SetupExe.Logic
{
    public class Logging
    {

       
       

        public void Info(string logMessage, string section)
        {
            WriteLog(logMessage, "INFO   ",section);    
        }

        public void Warn(string logMessage, string section)
        {
            WriteLog(logMessage, "WARNING",section);    
        }

        public void Fatal(string logMessage, string section, Exception ex)
        {
            WriteLog(logMessage, "FATAL  ",section);    
        }

        public void Error(string logMessage,string section)
        {
            WriteLog(logMessage, "ERROR  ",section);    
        }

        public void WritetoEventLog(string logMessage, string type, string section)
        {
            string eventlogsource = "Deliveries.Setup";
            //EventLog myEventLogger = new EventLog();

            try
            {
                if (!EventLog.SourceExists(eventlogsource))
                {
                    //myEventLogger.CreateEventSource(eventlogsource, "Application");
                    EventLog.CreateEventSource(eventlogsource, "Application");
                }
                switch (type)
                {
                    case "INFO   ":
                    case "INFO":
                        EventLog.WriteEntry(eventlogsource, logMessage, EventLogEntryType.Information);
                        break;
                    case "WARNING":
                        EventLog.WriteEntry(eventlogsource, logMessage, EventLogEntryType.Warning);
                        break;
                    case "ERROR  ":
                    case "ERROR":
                    case "FATAL  ":
                    case "FATAL":
                        EventLog.WriteEntry(eventlogsource, logMessage, EventLogEntryType.Error);
                        break;
                }
            }
            catch (Exception)
            {
                //error: do sth?
            }

            
        }

        private void WriteLog(string logMessage, string type, string section)
        {
            string logFile;
            logFile = GlobalClass.LogFileProductCode;

            try
            {
                TextWriter tw = new StreamWriter(logFile, true);
                tw.WriteLine(DateTime.Now.ToLongDateString() + " - " + DateTime.Now.ToLongTimeString() + " | " + type + " | - " + logMessage);
                tw.Close();
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message);
                //error: do sth?
            }

            
            //file security
            try
            {
                string SID = "S-1-1-0"; //everyone
                SecurityIdentifier sid = new SecurityIdentifier(SID);
                
                FileInfo fileinfo = new FileInfo(logFile);
                FileSecurity filesecurity = fileinfo.GetAccessControl();
                filesecurity.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.FullControl,AccessControlType.Allow));
                fileinfo.SetAccessControl(filesecurity);
            }
            catch (Exception)
            {
                //to nothing
                //MessageBox.Show(ex.Message);
            }

            try
            {
                //using (MessageReceiver.MessageReceiverClient client = new MessageReceiver.MessageReceiverClient())
                //{
                //    client.GetMessage(Environment.ExpandEnvironmentVariables("%COMPUTERNAME%"), logMessage, type, section);
                //}
            }
            catch { }


            }
    }
}
