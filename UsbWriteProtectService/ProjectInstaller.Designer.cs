namespace UsbWriteProtectService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.UsbWriteProtectServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.UsbWriteProtectServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // UsbWriteProtectServiceProcessInstaller
            // 
            this.UsbWriteProtectServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.UsbWriteProtectServiceProcessInstaller.Password = null;
            this.UsbWriteProtectServiceProcessInstaller.Username = null;
            // 
            // UsbWriteProtectServiceInstaller
            // 
            this.UsbWriteProtectServiceInstaller.ServiceName = "UsbWriteProtectService";
            this.UsbWriteProtectServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.UsbWriteProtectServiceProcessInstaller,
            this.UsbWriteProtectServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller UsbWriteProtectServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller UsbWriteProtectServiceInstaller;
    }
}