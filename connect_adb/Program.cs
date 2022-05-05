using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace connect_adb
{
    class Program
    {
        static void ToStringDevices(List<string> ids, List<string> ips, List<bool> success)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (i < ips.Count)
                    Console.WriteLine($"{ids[i]}:{(ids[i].Length < 10 ? "\t\t" : "\t")}{ips[i]}\t{(success[i] ? "Connected" : "Failed")}");
                else
                    Console.WriteLine($"{ids[i]}:\tNONE");
            }
            Console.WriteLine();
        }

        static void Connect()
        {
            // disconnect previous connections
            Disconnect();

            List<String> deviceIDs = new List<string>();
            List<String> deviceIPs = new List<string>();
            List<bool> success = new List<bool>();
            string tmpLine;

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "cmd.exe";
            p.Start();

            // get all IDs of devices
            Console.WriteLine("Collecting IDs...");

            p.StandardInput.WriteLine("adb devices");
            p.StandardInput.WriteLine("exit");

            List<string> output = new List<string>(p.StandardOutput.ReadToEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries));

            foreach (string s in output)
            {
                if (Regex.IsMatch(s, @"\t"))
                {
                    deviceIDs.Add(s.Split("\t")[0]);
                    success.Add(false);
                }
            }

            // get all IPs of devices
            Console.WriteLine("Collecting IPs...");

            foreach (string id in deviceIDs)
            {
                p.Start();
                System.Threading.Thread.Sleep(100);

                p.StandardInput.WriteLine($"adb -s {id} shell ip addr show wlan0");
                System.Threading.Thread.Sleep(100);

                p.StandardInput.WriteLine("exit");
                System.Threading.Thread.Sleep(100);

                tmpLine = p.StandardOutput.ReadToEnd();
                if (Regex.IsMatch(tmpLine, @"(([0-9]{1,3}.){3}([0-9]{1,3})\/[0-9]{1,2})"))
                {
                    string ip = Regex.Match(tmpLine, @"(([0-9]{1,3}.){3}([0-9]{1,3})\/[0-9]{1,2})").Value;
                    ip = ip.Split("/")[0];
                    deviceIPs.Add(ip);
                }
            }

            // start adb on port 5555
            Console.WriteLine("Starting ADB...");
            foreach (string id in deviceIDs)
            {
                p.Start();
                System.Threading.Thread.Sleep(100);

                p.StandardInput.WriteLine($"adb -s {id} tcpip 5555");
                System.Threading.Thread.Sleep(100);
                
                p.StandardInput.WriteLine("exit");
                System.Threading.Thread.Sleep(100);
            }

            // connect all devices
            for (int i = 0; i < deviceIPs.Count; i++)
            {
                p.Start();
                System.Threading.Thread.Sleep(100);

                p.StandardInput.WriteLine($"adb connect {deviceIPs[i]}:5555");
                System.Threading.Thread.Sleep(100);
                
                p.StandardInput.WriteLine("exit");
                System.Threading.Thread.Sleep(100);

                tmpLine = p.StandardOutput.ReadToEnd();
                if (tmpLine.Contains("connected")) success[i] = true;
            }

            Console.WriteLine("\nNaprave:");
            ToStringDevices(deviceIDs, deviceIPs, success);

            p.WaitForExit();
            p.Dispose();
        }

        static string Disconnect()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "cmd.exe";
            p.Start();

            p.StandardInput.WriteLine("adb disconnect");
            p.StandardInput.WriteLine("exit");

            string res = p.StandardOutput.ReadToEnd();

            p.WaitForExit();
            p.Dispose();

            return (res.Contains("disconnected everything") ? "Disconnected everything" : "Unexpected error occurred");
        }

        static void Main(string[] args)
        {
            bool ok = false;
            do
            {
                Console.Write("Connect or disconnect [c/d]?\t");

                string read = Console.ReadLine();

                // poveži
                if (read.ToLower() == "c")
                {
                    Console.Clear();
                    Console.WriteLine("Connecting...");
                    Connect();
                    ok = true;
                }
                // razveži
                else if (read.ToLower() == "d")
                {
                    Console.Clear();
                    Console.WriteLine("Disconnecting...");
                    Console.WriteLine(Disconnect());
                    ok = true;
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("Unsupported flag received.");
                }
            } while (!ok);

            Console.ReadKey(true);
        }
    }
}
