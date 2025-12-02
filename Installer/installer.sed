[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles

[Strings]
InstallPrompt=This will install RamOptimus v2.0.0 on your computer. RamOptimus is an advanced system optimizer for ASUS ROG laptops. Click OK to continue.
DisplayLicense=
FinishMessage=RamOptimus installation complete! You can now launch the application from your Desktop or Start Menu.
TargetName=C:\Users\Jarrod\Desktop\RamOptimus_Setup_v2.0.0.exe
FriendlyName=RamOptimus v2.0.0 Setup
AppLaunched=cmd /c powershell.exe -ExecutionPolicy Bypass -File install.ps1
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0="install.ps1"
FILE1="RamOptimizerUI.exe"
FILE2="Configuration.pdb"
FILE3="HardwareControl.pdb"
FILE4="Logging.pdb"
FILE5="Monitoring.pdb"
FILE6="ProcessManagement.pdb"
FILE7="RamOptimizerUI.pdb"
FILE8="SystemTray.pdb"

[SourceFiles]
SourceFiles0=C:\Users\Jarrod\Desktop\VS Code Projects\Ram optimiser\Installer\
SourceFiles1=C:\Users\Jarrod\Desktop\RamOptimus_Installer\

[SourceFiles0]
%FILE0%=

[SourceFiles1]
%FILE1%=
%FILE2%=
%FILE3%=
%FILE4%=
%FILE5%=
%FILE6%=
%FILE7%=
%FILE8%=
