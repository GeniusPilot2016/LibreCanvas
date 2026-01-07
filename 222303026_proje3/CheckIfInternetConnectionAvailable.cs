// LibreCanvas - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Net.NetworkInformation;

namespace Carpathia
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
