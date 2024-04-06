using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace LogAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var arguments = ParseArguments(args);

                var logs = ReadLogs(arguments["--file-log"]);

                var filteredLogs = FilterLogs(logs, arguments);

                var ipCounts = CountIPAddresses(filteredLogs);

                WriteResults(arguments["--file-output"], ipCounts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Dictionary<string, string> ParseArguments(string[] arg)
            {
                var arguments = new Dictionary<string, string>();

                for (int i = 0; i < arg.Length; i += 2)
                {
                    arguments[arg[i]] = arg[i + 1];
                }

                return arguments;
            }

            List<(string, DateTime)> ReadLogs(string filePath)
            {
                var logs = new List<(string, DateTime)>();

                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(':');
                        var ipAddress = parts[0];
                        var timestamp = DateTime.Parse(parts[1]);
                        logs.Add((ipAddress, timestamp));
                    }
                }

                return logs;
            }

            List<(string, DateTime)> FilterLogs(List<(string, DateTime)> logs, Dictionary<string, string> arguments)
            {
                var filteredLogs = logs;

                if (arguments.ContainsKey("--address-start"))
                {
                    var addressStart = arguments["--address-start"];
                    filteredLogs = filteredLogs.Where(log => log.Item1.StartsWith(addressStart)).ToList();
                }

                if (arguments.ContainsKey("--address-mask"))
                {
                    var mask = IPAddress.Parse(arguments["--address-mask"]);
                    filteredLogs = filteredLogs.Where(log => IsInSameSubnet(IPAddress.Parse(log.Item1), mask)).ToList();
                }

                if (arguments.ContainsKey("--time-start"))
                {
                    var timeStart = DateTime.ParseExact(arguments["--time-start"], "dd.MM.yyyy", null);
                    filteredLogs = filteredLogs.Where(log => log.Item2 >= timeStart).ToList();
                }

                if (arguments.ContainsKey("--time-end"))
                {
                    var timeEnd = DateTime.ParseExact(arguments["--time-end"], "dd.MM.yyyy", null);
                    filteredLogs = filteredLogs.Where(log => log.Item2 <= timeEnd).ToList();
                }

                return filteredLogs;
            }

            bool IsInSameSubnet( IPAddress address, IPAddress subnetMask)
            {
                if (address.AddressFamily != subnetMask.AddressFamily)
                    return false;

                byte[] addressBytes = address.GetAddressBytes();
                byte[] maskBytes = subnetMask.GetAddressBytes();

                if (addressBytes.Length != maskBytes.Length)
                    return false;

                for (int i = 0; i < addressBytes.Length; i++)
                {
                    if ((addressBytes[i] & maskBytes[i]) != (maskBytes[i] & maskBytes[i]))
                        return false;
                }

                return true;
            }

            Dictionary<string, int> CountIPAddresses(List<(string, DateTime)> logs)
            {
                var ipCounts = new Dictionary<string, int>();

                foreach (var log in logs)
                {
                    var ipAddress = log.Item1;
                    if (ipCounts.ContainsKey(ipAddress))
                    {
                        ipCounts[ipAddress]++;
                    }
                    else
                    {
                        ipCounts[ipAddress] = 1;
                    }
                }

                return ipCounts;
            }

            void WriteResults(string filePath, Dictionary<string, int> ipCounts)
            {
                using (var writer = new StreamWriter(filePath))
                {
                    foreach (var entry in ipCounts)
                    {
                        writer.WriteLine($"{entry.Key}: {entry.Value}");
                    }
                }
            }
        }
    }
}
