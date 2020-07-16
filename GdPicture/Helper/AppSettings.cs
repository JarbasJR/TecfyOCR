namespace GdPicture.Helper
{
    public class AppSettings
    {
        public string this[string key]
        {
            get
            {
                return System.Configuration.ConfigurationManager.AppSettings[key];
            }
        }
    }
}
