using StardewModdingAPI;
using System.Net;
using System.Threading.Tasks;

namespace ConcentrationOnFarming
{
    class UpdateChecker
    {
        public static void CheckUpdate(IMonitor monitor)
        {
            Task.Run(() =>
            {
                try
                {
                    string latest_version = new WebClient().DownloadString("https://raw.githubusercontent.com/pomepome/ConcentrationOnFarming/master/current_version.txt");
                    if (!IsLatest(latest_version, ModEntry.VERSION))
                    {
                        monitor.Log(string.Format("New version of ConcentrationOnFarming available! Consider updating version:{0} -> {1}.", ModEntry.VERSION, latest_version), LogLevel.Alert);
                    }
                    else
                    {
                        monitor.Log(string.Format("Your ConcentrationOnFarming(version:{0}) is up to date.",ModEntry.VERSION), LogLevel.Alert);
                    }
                }
                catch(WebException ex)
                {
                    monitor.Log("Update Checker couldn't download latest version info! Message:" + ex.Message,LogLevel.Error);
                }
            }
            );
        }

        private static bool IsLatest(string latest, string current)
        {
            string[] splitLatest = latest.Split(".".ToCharArray());
            string[] splitCurrent = current.Split(".".ToCharArray());

            int major_latest, major_current;
            int minor_latest, minor_current;
            int patch_latest, patch_current;

            try
            {
                major_latest = int.Parse(splitLatest[0]);
                major_current = int.Parse(splitCurrent[0]);
                minor_latest = int.Parse(splitLatest[1]);
                minor_current = int.Parse(splitCurrent[1]);
                patch_latest = int.Parse(splitLatest[2]);
                patch_current = int.Parse(splitCurrent[2]);
            }
            catch
            {
                return false;
            }

            if(major_latest > major_current || minor_latest > minor_current || patch_latest > patch_current)
            {
                return false;
            }
            return true;
        }
    }
}
