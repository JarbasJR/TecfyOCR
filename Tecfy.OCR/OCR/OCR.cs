using GdPicture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace Tecfy.OCR
{
    partial class OCR
    {
        #region .: Variables :.

        private static string pathInput = Ready.AppSettings["Path.Input"].ToString();
        private static string pathInputTemporary = Ready.AppSettings["Path.Input.Temporary"].ToString();
        private static string pathDestiny = Ready.AppSettings["Path.Destiny.OCR"].ToString();
        private static string pathDestinyRefused = Ready.AppSettings["Path.Destiny.Refused"].ToString();
        private static string separator = Ready.AppSettings["Separator"].ToString();
        private static FileSystemWatcher watcher;
        private static List<string> runningFiles = new List<string>();
        private static EventLog EventLog = null;
        private static System.Timers.Timer timer = new System.Timers.Timer();
        private static DateTime lastExecution;

        #endregion

        #region .: Constructor :.

        public OCR()
        {
        }

        internal static void SetEventLog(EventLog eventLog)
        {
            EventLog = eventLog;
        }

        #endregion

        #region .: OCR :.

        public static void Start()
        {
            try
            {
                CreateFolder();
                int processes = Convert.ToInt32(Ready.AppSettings["Processes"]);
                ThreadPool.GetMaxThreads(out int maxWorker, out int maxIOC);
                ThreadPool.SetMaxThreads(processes, maxIOC);

                ProcessCurrentFiles();

                InitFileSystemWatcher(1);

                timer.Elapsed += new System.Timers.ElapsedEventHandler(Restart);
                timer.Interval = Convert.ToInt32(Ready.AppSettings["Interval.Restart"]);
                timer.Enabled = true;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(string.Format("Método Start, Erro: {0}", ex.Message), EventLogEntryType.Error);
            }
        }

        public static void Restart(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (Directory.GetFiles(pathInput).Length > 0)
                {
                    if (lastExecution.AddMilliseconds(240000) < DateTime.Now)
                    {
                        watcher.EnableRaisingEvents = false;
                        watcher.Dispose();
                        watcher = null;
                        ProcessCurrentFiles();
                        InitFileSystemWatcher(1);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(string.Format("Método Restart, Erro: {0}", ex.Message), EventLogEntryType.Error);
            }
        }

        private static void Run(object state)
        {
            var item = (string)state;

            var fileNameArray = Path.GetFileName(item).ToString().Split(new char[] { Convert.ToChar(separator) });
            var folderMain = fileNameArray.Length > 1 ? fileNameArray[0] : "";
            var fileName = Path.GetFileName(item);
            var pathFileName = Path.Combine(pathInputTemporary, fileName);
            var pathFileNameRefused = Path.Combine(pathDestinyRefused, Path.GetFileName(item));
            try
            {
                if (Path.GetExtension(item).ToUpper() == ".PDF")
                {
                    MoveFile(item, pathFileName, 1);
                }

                if (Path.GetExtension(pathFileName).ToUpper() == ".PDF")
                {
                    try
                    {
                        RunOCR(fileName, folderMain);
                    }
                    catch (Exception ex)
                    {
                        EventLog.WriteEntry(string.Format("Método Run, Arquivo: {0} Erro: {1}", pathFileName, ex.Message), EventLogEntryType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MoveFile(pathFileName, pathFileNameRefused, 1);

                EventLog.WriteEntry(string.Format("Método Run, Erro: {0}", ex.Message), EventLogEntryType.Error);
            }
            finally
            {
                lock (runningFiles)
                {
                    runningFiles.Remove(item);
                }
            }
        }

        private static void RunOCR(string fileName, string folderMain)
        {
            var pathFileName = Path.Combine(pathInputTemporary, fileName);
            var pathFileNameFinal = string.IsNullOrEmpty(folderMain) ? Path.Combine(pathDestiny, fileName) : Path.Combine(pathDestiny, folderMain, fileName);
            var pathFileNameRefused = Path.Combine(pathDestinyRefused, fileName);
            try
            {
                FileInfo fileInfo = new FileInfo(pathFileName);
                // validates 0kb files
                if (fileInfo.Length != 0)
                {
                    // Run OCR
                    string pathFileNameOCR = CastToPDFByProccessCommand(pathFileName);
                    // Run RTF
                    RTF.CastToRTF(pathFileNameOCR, folderMain);
                    // Run Send Email
                    SendEmail(pathFileNameOCR, pathFileNameRefused, 1);
                    // Run Destination Folder
                    GetDestinationFolder(pathFileNameOCR, pathFileNameFinal, folderMain);

                    if (File.Exists(pathFileName))
                    {
                        File.Delete(pathFileName);
                    }
                    if (File.Exists(pathFileNameOCR))
                    {
                        File.Delete(pathFileNameOCR);
                    }
                }
                else
                {
                    MoveFile(pathFileName, pathFileNameRefused, 1);
                }
            }
            catch (Exception ex)
            {
                MoveFile(pathFileName, pathFileNameRefused, 1);
                EventLog.WriteEntry(string.Format("Método RunOCR, Erro: {0}", ex.Message), EventLogEntryType.Error);
            }
        }

        private static string CastToPDFByProccessCommand(string fileName)
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo("GdPicture.exe", "\"" + fileName + "\"");

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            procStartInfo.StandardOutputEncoding = Encoding.UTF8;

            // wrap IDisposable into using (in order to release hProcess) 
            using (Process process = new Process())
            {
                process.StartInfo = procStartInfo;
                process.Start();

                // Add this: wait until process does its work
                process.WaitForExit();

                // and only then read the result
                string result = process.StandardOutput.ReadToEnd();
                if (process.ExitCode == 0)
                {
                    return result;
                }
                throw new Exception(result);
            }
        }

        #endregion

        #region .: Helper :.

        private static void CreateFolder()
        {
            var folders = new string[] { "Path.Input", "Path.Input.Temporary", "Path.Destiny.Temporary", "Path.Destiny.Refused", "Path.Destiny.OCR" };

            foreach (var item in folders)
            {
                var path = Ready.AppSettings[item].ToString();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        private static void GetDestinationFolder(string pathFileNameOCR, string pathFileNameFinal, string folderMain)
        {
            bool sendEmail = Convert.ToBoolean(Ready.AppSettings["Send.Email"]);

            if (sendEmail != true)
            {
                try
                {
                    string path = string.IsNullOrEmpty(folderMain) ? pathDestiny : Path.Combine(pathDestiny, folderMain);

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    if (File.Exists(pathFileNameFinal))
                    {
                        File.Delete(pathFileNameFinal);

                        File.Move(pathFileNameOCR, pathFileNameFinal);
                    }
                    else
                    {
                        File.Move(pathFileNameOCR, pathFileNameFinal);
                    }
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(string.Format("Método GetDestinationFolder, Erro: {0}", ex.Message), EventLogEntryType.Error);
                }
            }
        }

        private static void MoveFile(string sourceFileName, string destFileName, int exec)
        {
            if (File.Exists(sourceFileName))
            {
                if (!FileLocked.IsFileLocked(sourceFileName))
                {
                    if (File.Exists(destFileName))
                    {
                        File.Delete(destFileName);
                    }
                    File.Move(sourceFileName, destFileName);
                }
                else
                {
                    if (exec <= 5)
                    {
                        exec++;
                        Thread.Sleep(3000);
                        MoveFile(sourceFileName, destFileName, exec);
                    }
                }
            }
        }

        private static void SendEmail(string pathFileNameOCR, string pathFileNameRefused, int exec)
        {
            Boolean sendEmail = Convert.ToBoolean(Ready.AppSettings["Send.Email"]);

            if (sendEmail == true)
            {
                Attachment anexo = new Attachment(pathFileNameOCR);
                try
                {
                    string strpdf = "";

                    string[] strArrayspdf = Path.GetFileName(pathFileNameOCR).ToString().Split(new char[] { '^' });
                    if (strArrayspdf.Length != 0)
                    {
                        strpdf = strArrayspdf[0];
                    }

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        MailMessage mail = new MailMessage();

                        var Host = Ready.AppSettings["Host"];
                        var Porta = Ready.AppSettings["Porta"];
                        var Email = Ready.AppSettings["Email"];
                        var Senha = Ready.AppSettings["Senha"];

                        smtp.Host = Host;
                        smtp.Port = Convert.ToInt32(Porta);
                        smtp.EnableSsl = true;
                        smtp.UseDefaultCredentials = false;

                        smtp.Credentials = new System.Net.NetworkCredential(Email, Senha);
                        mail.From = new MailAddress(Email);
                        mail.Attachments.Add(anexo);
                        mail.Subject = "Digitalização OCR";
                        mail.Body = "Documento Digitalizado ";
                        mail.To.Add(new MailAddress(strpdf));
                        smtp.Send(mail);
                    }
                }
                catch (Exception ex)
                {
                    anexo.Dispose();
                    if (exec <= 5)
                    {
                        exec++;
                        Thread.Sleep(3000);
                        SendEmail(pathFileNameOCR, pathFileNameRefused, exec);
                    }
                    else
                    {
                        MoveFile(pathFileNameOCR, pathFileNameRefused, 1);
                        EventLog.WriteEntry(string.Format("Método SendEmail, Erro: {0}", ex.Message), EventLogEntryType.Error);
                    }
                }
                finally
                {
                    anexo.Dispose();
                }
            }
        }

        private static void WatcherError(object sender, ErrorEventArgs e)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            watcher = null;
            ProcessCurrentFiles();
            InitFileSystemWatcher(1);
        }

        private static void WatcherOnChanged(object source, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                    FileCreationVerification.AddFileToCreatedFileList(e.FullPath);
                }
                if (FileCreationVerification.FileCreatedIsCompletedWrited(e.FullPath))
                {
                    QueueToProcess(e.FullPath);
                }
            }
        }

        private static void ProcessCurrentFiles()
        {
            foreach (var item in Directory.GetFiles(pathInput))
            {
                QueueToProcess(item);
            }
        }

        private static void QueueToProcess(string item)
        {
            lock (runningFiles)
            {
                if (!runningFiles.Contains(item))
                {
                    lastExecution = DateTime.Now;
                    runningFiles.Add(item);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Run), item);
                    //Run(item);
                }
            }
        }

        private static void InitFileSystemWatcher(int exec)
        {
            try
            {
                watcher = new FileSystemWatcher();
                watcher.InternalBufferSize = 65536;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.pdf";
                watcher.Created += WatcherOnChanged;
                watcher.Changed += WatcherOnChanged;
                watcher.Error += WatcherError;
                watcher.Path = pathInput;
                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception)
            {
                if (exec <= 5)
                {
                    exec++;
                    Thread.Sleep(3000);
                    InitFileSystemWatcher(exec);
                }
            }
        }

        class FileCreationVerification
        {
            private static List<string> createdFileList = new List<string>();
            public static void AddFileToCreatedFileList(string filepath)
            {
                lock (createdFileList)
                {
                    if (!createdFileList.Contains(filepath))
                        createdFileList.Add(filepath);
                }
            }

            public static bool FileCreatedIsCompletedWrited(string filepath)
            {
                lock (createdFileList)
                {
                    if (!createdFileList.Contains(filepath))
                        return false;
                    if (!IsFileReady(filepath))
                        return false;
                    createdFileList.Remove(filepath);
                    return true;
                }
            }

            private static bool IsFileReady(string filepath)
            {
                try
                {
                    using (var inputStream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        #endregion
    }
}
