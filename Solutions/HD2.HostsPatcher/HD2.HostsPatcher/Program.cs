using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace HD2.HostsPatcher
{
    class Program
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern UInt32 DnsFlushResolverCache();

        private static string hostsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        private static GamespyHost[] hosts = new GamespyHost[]
            {
                new GamespyHost("hd2.available.gamespy.com"),
                new GamespyHost("hd2.master.gamespy.com"),
                new GamespyHost("hd2.ms14.gamespy.com"),
            };

        static void Main(string[] args)
        {
            Console.WriteLine("Hidden & Dangerous 2 - Hosts Patcher");
            Console.WriteLine("Copyright (c) 2016 DnA, http://steamcommunity.com/id/dna_uk/. All Rights Reserved.");
            Console.ForegroundColor = ConsoleColor.White;
            
            do
            {
                Console.Write("\nDo you want to patch your hosts file? (Y/N) ");

                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Y:
                        Console.Write("\n\n");
                        Patch();
                        Console.WriteLine("\nPress any key to exit");
                        Console.ReadKey();
                        return;

                    case ConsoleKey.N:
                        return;

                    default:
                        break;
                }
            } while (true);
        }

        private static void Patch()
        {
            try
            {
                // Retrieve host address from remote
                Console.WriteLine("Retrieving host data...");
                WebClient wc = new WebClient();
                byte[] dataBuffer = wc.DownloadData("https://raw.githubusercontent.com/WillWaywell/hd2-hosts-patcher/master/host");
                IPAddress hostAddress = IPAddress.Parse(Encoding.UTF8.GetString(dataBuffer));

                List<string> lines = new List<String>(File.ReadAllLines(hostsFile));
                bool updateHosts = false;

                // Check existing entries
                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i];
                    if (!String.IsNullOrEmpty(line) && !line.StartsWith("#"))
                    {
                        string[] entry = line.Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (GamespyHost host in hosts)
                        {
                            if (entry[1] == host.HostName)
                            {
                                host.Index = i;
                                host.HasValidIP = entry[0] == hostAddress.ToString();
                            }
                        }
                    }
                }

                // Determine if entries need adding or updating
                foreach (GamespyHost host in hosts)
                {
                    Console.WriteLine("Check: {0}", host.HostName);
                    if (host.Index < 0)
                    {
                        lines.Add(String.Format("{0} {1}", hostAddress, host.HostName));
                        updateHosts = true;
                        Console.WriteLine("Append: {0}", host.HostName);
                    }
                    else
                    {
                        if (!host.HasValidIP)
                        {
                            lines[host.Index] = String.Format("{0} {1}", hostAddress, host.HostName);
                            updateHosts = true;
                            Console.WriteLine("Update: {0}", host.HostName);
                        };
                    }
                }

                if (updateHosts)
                {
                    File.WriteAllLines(hostsFile, lines);
                    DnsFlushResolverCache();
                }

                Console.WriteLine("Patching complete");
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }
    }

    class GamespyHost
    {
        public string HostName { get; set; }
        public int Index { get; set; } = -1;
        public bool HasValidIP { get; set; } = false;

        public GamespyHost(string hostName)
        {
            this.HostName = hostName;
        }
    }
}
