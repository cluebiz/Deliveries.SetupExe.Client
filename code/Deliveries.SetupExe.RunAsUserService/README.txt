HKEY_LOCAL_MACHINE\SOFTWARE\cluebiz\deliveries_setup\usertasks
{"cmd":"notepad.exe", "param":"test.txt","workdir":"d:\temp","waitproc":"true","enddate":"2019-06-10T14:30:00"}


Tasks werden nach name sortiert ausgeführt!!!!



install service:
(RUN AS ADMIN) From the Start menu, select the Visual Studio <version> directory, then select Developer Command Prompt for VS <version>. 

The Developer Command Prompt for Visual Studio appears.
Access the directory where your project's compiled executable file is located.
Run InstallUtil.exe from the command prompt with your project's executable as a parameter:
installutil <yourproject>.exe

remove service:
(stop service+task)
sc delete CluebizRunAsUserService