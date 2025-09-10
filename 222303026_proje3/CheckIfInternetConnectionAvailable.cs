using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace _222303026_proje3
{
    public class CheckIfInternetConnectionAvailable
    {
        public static bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("info.cern.ch"); // Check internet connectivity by pinging the first website ever
                    // If the ping is successful, we have internet access
                    // Fun fact: info.cern.ch is the first website ever created (1991) by Tim Berners-Lee
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
        public static bool IsServerUp()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("generativelanguage.googleapis.com"); // Check server status by pinging the Google API server
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
