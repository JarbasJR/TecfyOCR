using Aspose.Pdf;
using System;
using System.IO;
using System.Reflection;

namespace Tecfy.OCR
{
    partial class RTF
    {
        #region .: Variables :.

        private static string pathDestiny = Ready.AppSettings["Path.Destiny.OCR"].ToString();

        #endregion

        #region .: Constructor :.

        public RTF()
        {
        }

        #endregion

        #region .: RTF :.

        public static void CastToRTF(string pathFileNameOCR, string folderMain)
        {
            bool lockRTF = Convert.ToBoolean(Ready.AppSettings["Lock.RTF"]);

            if (lockRTF == true)
            {
                string path = string.IsNullOrEmpty(folderMain) ? pathDestiny : Path.Combine(pathDestiny, folderMain);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string AsposeWords = Path.Combine(GetCurrentDirectory(), "Aspose/Aspose.Words.lic");
                string AsposePdf = Path.Combine(GetCurrentDirectory(), "Aspose/Aspose.Pdf.lic");

                using (FileStream fileStream = File.Open(AsposeWords, FileMode.Open))
                {
                    (new Aspose.Words.License()).SetLicense(fileStream);
                }
                using (FileStream fileStream1 = File.Open(AsposePdf, FileMode.Open))
                {
                    (new License()).SetLicense(fileStream1);
                }

                string strDoc = Path.Combine(path, Path.GetFileNameWithoutExtension(pathFileNameOCR) + ".doc");
                Document document = new Document(pathFileNameOCR);

                DocSaveOptions docSaveOption = new DocSaveOptions
                {
                    Mode = DocSaveOptions.RecognitionMode.Flow,
                    RelativeHorizontalProximity = 2.5f,
                    RecognizeBullets = true
                };

                Aspose.Words.Document document1 = new Aspose.Words.Document(strDoc);
                string strRTF = Path.Combine(path, Path.GetFileNameWithoutExtension(pathFileNameOCR) + ".rtf");
                document1.Save(strRTF);
            }
        }

        #endregion

        #region .: Helper :.

        private static string GetCurrentDirectory()
        {
            string absolutePath = (new Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            string fullName = (new DirectoryInfo(Path.GetDirectoryName(absolutePath))).FullName;
            return Uri.UnescapeDataString(fullName);
        }

        #endregion
    }
}
