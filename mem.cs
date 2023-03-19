using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

class Program
{
    const int PROCESS_VM_READ = 0x0010;

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    static void Main(string[] args)
    {
        Console.WriteLine("Enter process name or process ID:");
        string processNameOrID = Console.ReadLine();

        int processID;
        if (!int.TryParse(processNameOrID, out processID))
        {
            Process[] processes = Process.GetProcessesByName(processNameOrID);
            if (processes.Length == 0)
            {
                Console.WriteLine("Could not find process with name {0}", processNameOrID);
                return;
            }
            processID = processes[0].Id;
        }

        Console.WriteLine("Opening process {0}...", processID);

        IntPtr hProcess = OpenProcess(PROCESS_VM_READ, false, processID);
        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("Failed to open process.");
            return;
        }

        List<long> matchingAddresses = new List<long>();

        while (matchingAddresses.Count < 5)
        {
            Console.WriteLine("Enter value to search for:");
            string input = Console.ReadLine();

            int intValue;
            bool isNumeric = int.TryParse(input, out intValue);

            byte[] buffer = new byte[1024];
            int bytesRead;

            for (long address = 0; ; address += buffer.Length)
            {
                if (!ReadProcessMemory(hProcess, new IntPtr(address), buffer, buffer.Length, out bytesRead))
                {
                    Console.WriteLine("Failed to read process memory.");
                    return;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                for (int i = 0; i < bytesRead - (isNumeric ? 3 : input.Length); i++)
                {
                    bool matches = true;
                    for (int j = 0; j < input.Length; j++)
                    {
                        if ((char)buffer[i + j] != input[j])
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches || (isNumeric && BitConverter.ToInt32(buffer, i) == intValue))
                    {
                        matchingAddresses.Add(address + i);
                        Console.WriteLine("Match found at address 0x{0:X}", address + i);
                        break;
                    }
                }

                if (matchingAddresses.Count >= 5)
                {
                    break;
                }
            }

            if (matchingAddresses.Count == 0)
            {
                Console.WriteLine("No matches found.");
            }
        }
    }
}
