using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web.Script.Serialization;

namespace KarotzWifiConfigurator
{
    class Program
    {
        static void Main(string[] args)
        {
            DoIt();
        }

        private class WifiSettings
        {
            public string cmd { get; set; }
            public string encryption { get; set; }
            public string ssid { get; set; }
            public string key { get; set; }
            public WifiSettingsSettings settings { get; set; }

            public WifiSettings() // constructor
            {
                cmd = "set_wifi";
                encryption = "";
                ssid = "";
                key = "";
                settings = new WifiSettingsSettings();
            }

            public void SetSSID(string newssid)
            {
                ssid = newssid;
                settings.SetSSID(newssid);
            }

            public void SetPwd(string newpwd)
            {
                key = newpwd;
                settings.SetPwd(newpwd);
            }

            public void SetEncryption(string newencryption)
            {
                encryption = newencryption.ToLower();
                settings.SetProto(newencryption);
            }
        }

        private class WifiSettingsSettings
        {
            public string ssid { get; set; }
            public int scan_ssid { get; set; }
            public string proto { get; set; }
            public string key_mgmt { get; set; }
            public string psk { get; set; }

            public WifiSettingsSettings() // constructor
            {
                ssid = "";
                scan_ssid = 1;
                proto = "";
                key_mgmt = "WPA-PSK";
                psk = "";
            }

            public void SetSSID(string newssid)
            {
                ssid = WrapInQuotes(newssid);
            }

            public void SetPwd(string newpwd)
            {
                psk = WrapInQuotes(newpwd);
            }

            public void SetProto(string newproto)
            {
                proto = newproto.ToUpper();
            }

            private string WrapInQuotes(string text)
            {
                return string.Format("\"{0}\"", text);
            }        
        
        }

        private class NetworkSettings
        {
            public string nameserver { get; set; }
            public string cmd { get; set; }
            public string gateway { get; set; }
            public bool dhcp { get; set; }
            public string @interface { get; set; }
            public string netmask { get; set; }
            public string ip { get; set; }

            public NetworkSettings() // constructor
            {
                nameserver = "";
                cmd = "set_ip";
                gateway = "";
                dhcp = false;
                @interface = "wlan0";
                netmask = "";
                ip = "";
            }
        }

        static void DoIt()
        {
            Message("This program will create 'network.conf' for your Karotz.\nPlease enter information needed to connect to WIFI.");
            var WifiJson = ToJson(GetWifiSettings());
            Message("Please enter information needed to create network configuration.");
            var NetworkJson = ToJson(GetNetworkSettings(GetLocalIp()));
            string config = string.Concat(WifiJson, "\n", NetworkJson);
            Message(string.Format("Your configuration:\n{0}", config));
            SaveConfiguration(config);
            Message("Configuration is saved. Press a key to exit");
            Console.ReadLine();
        }

        private static WifiSettings GetWifiSettings()
        {
            var wifi = new WifiSettings();
            wifi.SetSSID(AskQuestion(Questions.SSID, "MYSSID"));
            wifi.SetPwd(AskQuestion(Questions.Pwd, "password"));
            wifi.SetEncryption(AskQuestion(Questions.Encryption, "wpa2"));
            return wifi;
        }

        private static NetworkSettings GetNetworkSettings(string localip)
        {
            var settings = new NetworkSettings();
            settings.dhcp = (AskQuestion(Questions.DHCP, "n").ToLower().StartsWith("y"));
            if (!settings.dhcp)
            {
                settings.nameserver = AskQuestion(Questions.NameServer, IpToNewIp(localip, "1"));
                settings.gateway = AskQuestion(Questions.Gateway, IpToNewIp(localip, "1"));
                settings.netmask = AskQuestion(Questions.Netmask, "255.255.255.0");
                settings.ip = AskQuestion(Questions.StaticIp, IpToNewIp(localip, "130"));
            }
            return settings;
        }

        private static string AskQuestion(string question, string defaultanswer)
        {
            string answer = "";
            string enterinfo = (!string.IsNullOrWhiteSpace(defaultanswer)) ? string.Format(" [Enter = {0}]", defaultanswer) : "";
            Console.Write(string.Format("{0}{1}: ", question, enterinfo));
            answer = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(answer)) answer = defaultanswer;
            return answer;
        }

        private static void Message(string message)
        {
            const string line = "------------------------------------------------------------------------------";
            Console.WriteLine(string.Format("{0}\n{1}\n{2}", line, message, line));
        }
        
        private static string ToJson(object obj)
        {
            return new JavaScriptSerializer().Serialize(obj);
        }

        private static string GetLocalIp()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            string address = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            return (address != null) ? address : "192.168.1.1";
        }

        private static string IpToNewIp(string ip, string segment)
        {
            var result = ip;
            var splittedIp = ip.Split('.');
            if (splittedIp.Length == 4)
            {
                splittedIp[3] = segment;
                result = string.Join(".", splittedIp);
            }
            return result;
        }

        private static void SaveConfiguration(string config)
        {
            string configFileName = "network.conf";
            try
            {
                System.IO.File.WriteAllText(configFileName, config);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        public static class Questions
        {
            public const string SSID = "SSID, your WIFI network name";
            public const string Pwd = "Password, your WIFI password";
            public const string Encryption = "WIFI Encryption ('open', 'wep', 'wpa' or 'wpa2')";
            public const string Gateway = "Gateway, often your Router-IP";
            public const string NameServer = "DNS (nameserver) often your Router-IP";
            public const string DHCP = "DHCP (y or n)";
            public const string Netmask = "Netmask";
            public const string StaticIp = "Static IP, the fixed IP-address for your Karotz";
        }


    }
}
