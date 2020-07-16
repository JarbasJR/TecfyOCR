using System.IO;

namespace Tecfy.OCR
{
    partial class FileLocked
    {
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                FileStream fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                fs.Close();

                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
