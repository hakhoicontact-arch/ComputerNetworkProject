using System.Diagnostics;

namespace RCS.Agent.Services.Windows
{
    public class SystemControl
    {
        public void Shutdown()
        {
            RunCommand("shutdown", "/s /t 0");
        }

        public void Restart()
        {
            RunCommand("shutdown", "/r /t 0");
        }

        private void RunCommand(string fileName, string args)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
    }
}