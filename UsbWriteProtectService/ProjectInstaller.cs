using System.ComponentModel;
using System.Configuration.Install;

namespace UsbWriteProtectService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
