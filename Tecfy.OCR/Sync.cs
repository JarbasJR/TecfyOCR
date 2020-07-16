using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;

namespace Tecfy.OCR
{
    partial class Sync : ServiceBase
    {      
        #region [ DI ]

        public Sync()
        {
            InitializeComponent();

            CultureInfo before = System.Threading.Thread.CurrentThread.CurrentCulture;

            System.Threading.Tasks.Task objTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.Sleep(20000);
                RunService();
            });
        }

        #endregion

        void RunService()
        {
            OCR.SetEventLog(EventLog);
            OCR.Start();
        }

        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry("Serviço Inicializado.", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry("Serviço Parado.", EventLogEntryType.Information);
        }
    }
}
