using System;
using System.Security.Permissions;

namespace GdPicture
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string pathFile = args[0];
            try
            {
                Run(pathFile);
                Environment.Exit(0);
            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
                Environment.Exit(1);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private static void Run(string pathFile)
        {
            string returnFile = GdPicture.Gdpicture.CastToPDF(pathFile);
            Console.Write(returnFile);
        }
    }
}
