using System.ServiceProcess;

namespace Tecfy.OCR
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Sync()
            };
            ServiceBase.Run(ServicesToRun);
            //OCR.Start();
        }
    }
}
