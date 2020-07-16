using GdPicture.Helper;
using System;
using System.IO;
using System.Reflection;

namespace GdPicture
{
    public class Gdpicture
    {
        #region .: Variables :.

        private static GdPictureImaging oGdPictureImaging = new GdPictureImaging();
        private static GdPicturePDF oGdPicturePDF = new GdPicturePDF();
        private static string pathDestinyTemporary = Ready.AppSettings["Path.Destiny.Temporary"].ToString();

        #endregion

        #region .: Constructor :.

        public Gdpicture()
        {
        }

        #endregion

        #region .: OCR :.

        public static string CastToPDF(string pathFile, bool pdfa = true, string idioma = "por", string white = null, string titulo = null, string autor = null, string assunto = null, string palavrasChaves = null, string criador = null, int dpi = 250)//
        {
            string pathFileFinal = "";
            string folderpdf = Path.Combine(pathDestinyTemporary, Guid.NewGuid().ToString());

            try
            {
                oGdPicturePDF.SetLicenseNumber("4118106456693265856441854");
                oGdPictureImaging.SetLicenseNumber("4118106456693265856441854");

                #region .: PDF :.                

                string strpdf = "";

                GdPictureStatus status = oGdPicturePDF.LoadFromFile(pathFile, false);

                if (status == GdPictureStatus.OK)
                {
                    oGdPicturePDF.EnableCompression(true);
                    int ident = 1;
                    int num1 = oGdPicturePDF.GetPageCount();
                    int num4 = 1;
                    string[] mergeArray = new string[num1];
                    Directory.CreateDirectory(folderpdf);
                    if (num1 > 0)
                    {
                        bool flagpdf = true;

                        while (num4 <= num1)
                        {
                            oGdPicturePDF.SelectPage(num4);

                            int numpdf1 = oGdPicturePDF.RenderPageToGdPictureImage(300, true);//here

                            var docuemntoId = Guid.NewGuid();

                            string sstr = Path.Combine(Gdpicture.GetCurrentDirectory(), "GdPicture\\Idiomas");

                            status = oGdPicturePDF.SaveToFile(folderpdf + "\\compressed_pack.pdf", true);
                            oGdPicturePDF.SaveToFile(folderpdf + "\\" + ident + "_" + docuemntoId + ".pdf", true);

                            var id = oGdPictureImaging.PdfOCRStart(folderpdf + "\\" + ident + "_" + docuemntoId + ".pdf", true, "", "", "", "", "");

                            oGdPictureImaging.PdfAddGdPictureImageToPdfOCR(id, numpdf1, "por", sstr, "");
                            oGdPictureImaging.PdfOCRStop(id);

                            mergeArray[num4 - 1] = folderpdf + "\\" + ident + "_" + docuemntoId + ".pdf"; //

                            if (oGdPicturePDF.GetStat() == 0)
                            {
                                num4++;
                                ident++;
                            }
                            else
                            {
                                flagpdf = false;
                                break;
                            }

                            oGdPictureImaging.ReleaseGdPictureImage(numpdf1);
                        }

                        oGdPicturePDF.CloseDocument();

                        if (flagpdf)
                        {
                            var strPdf1 = pathFile.Replace(Path.GetExtension(pathFile), ".pdf");
                            oGdPicturePDF.MergeDocuments(mergeArray, strPdf1);
                            strpdf = strPdf1;
                            oGdPicturePDF.CloseDocument();
                        }

                        oGdPictureImaging.ClearGdPicture();

                        if (File.Exists(pathDestinyTemporary + "\\" + Path.GetFileName(strpdf)))
                        {
                            File.Replace(strpdf, pathDestinyTemporary + "\\" + Path.GetFileName(strpdf), null);
                        }
                        else
                        {
                            File.Move(strpdf, pathDestinyTemporary + "\\" + Path.GetFileName(strpdf));
                        }
                        
                        pathFileFinal = pathDestinyTemporary + "\\" + Path.GetFileName(strpdf);
                        foreach (var item in Directory.GetFiles(folderpdf))
                        {
                            File.Delete(item);
                        }
                        Directory.Delete(folderpdf);
                    }
                    else
                    {
                        oGdPicturePDF.SelectPage(num4);
                        int numpdf = oGdPicturePDF.RenderPageToGdPictureImage(300, true); //here
                        var docuemntoId = Guid.NewGuid();
                        string sstr = string.Concat(Gdpicture.GetCurrentDirectory(), "\\GdPicture\\Idiomas");
                        oGdPictureImaging.SaveAsPDFOCR(numpdf, folderpdf + "\\" + docuemntoId + ".pdf", idioma, sstr, white, pdfa, titulo, autor, assunto, palavrasChaves, criador);

                        var strPdf = pathFile.Replace(Path.GetExtension(pathFile), ".pdf");
                        oGdPictureImaging.ReleaseGdPictureImage(numpdf);

                        oGdPicturePDF.MergeDocuments(System.IO.Directory.GetFiles(folderpdf), strPdf);
                        strpdf = strPdf;

                        oGdPictureImaging.ClearGdPicture();

                        if (File.Exists(pathDestinyTemporary + "\\" + Path.GetFileName(strpdf)))
                        {
                            File.Replace(strpdf, pathDestinyTemporary + "\\" + Path.GetFileName(strpdf), null);
                        }
                        else
                        {
                            File.Move(strpdf, pathDestinyTemporary + "\\" + Path.GetFileName(strpdf));
                        }

                        pathFileFinal = pathDestinyTemporary + "\\" + Path.GetFileName(strpdf);
                        foreach (var item in Directory.GetFiles(folderpdf))
                        {
                            File.Delete(item);
                        }
                        Directory.Delete(folderpdf);
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                if (Directory.Exists(folderpdf))
                {
                    foreach (var item in Directory.GetFiles(folderpdf))
                    {
                        File.Delete(item);
                    }
                    Directory.Delete(folderpdf);
                }

                throw ex;
            }

            return pathFileFinal;
        }

        private static string GetCurrentDirectory()
        {
            string absolutePath = (new Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            string fullName = (new DirectoryInfo(Path.GetDirectoryName(absolutePath))).FullName;
            return Uri.UnescapeDataString(fullName);
        }

        #endregion
    }
}
