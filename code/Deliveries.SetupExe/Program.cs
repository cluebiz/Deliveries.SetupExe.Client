using System;
using System.Windows.Forms;
using System.Threading;
using Deliveries.SetupExe.Logic;

namespace SetupExe
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {




            //Culture
            //Creating a Global culture specific to our application
            System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("en-US");
            //Assigning the culture to the application
            Application.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            
            // Creates an instance of the methods that will handle the exception.
            CustomExceptionHandler eh = new CustomExceptionHandler();

            // Adds the event handler to the event.
            Application.ThreadException += new ThreadExceptionEventHandler(eh.OnThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(eh.CurrentDomain_UnhandledException);

            // Runs the application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new formMain(args));

            return GlobalClass.ExitCode;
        }


    }

    internal class CustomExceptionHandler
    {
        // Handles the exception event.
        public void OnThreadException(object sender, ThreadExceptionEventArgs t)
        {
            DialogResult result = DialogResult.Cancel;
            try
            {
                result = this.ShowThreadExceptionDialog(t.Exception);
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal Error", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }

            // Exits the program when the user clicks Abort.
            if (result == DialogResult.Abort)
            {
                Application.Exit();
            }
        }

        public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            DialogResult result = DialogResult.Cancel;
            try
            {
                result = this.ShowThreadExceptionDialog(ex.ExceptionObject as Exception);
            }
            catch
            {
                try
                {
                    MessageBox.Show((ex.ExceptionObject as Exception).Message, "Unhandled UI Exception", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }

            }
            
        }

       
        // Creates the error message and displays it.
        private DialogResult ShowThreadExceptionDialog(Exception e)
        {
            string errorMsg = "An error occurred please contact the adminstrator with the following information:\n\n";
            errorMsg = errorMsg + e.Message + "\n\nStack Trace:\n" + e.StackTrace;
            return MessageBox.Show(errorMsg, "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
   
    }
}
