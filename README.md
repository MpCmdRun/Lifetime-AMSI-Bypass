# Lifetime AMSI Bypass

> This is a C# console tool that searches for running PowerShell instances and patches the AMSI (Anti-Malware Scan Interface) in memory to bypass script scanning.

## ⚠️ Disclaimer

This tool is for **educational purposes only**. Unauthorized use of code like this to bypass security controls may violate laws or organizational policies. Use responsibly and only in environments you own or have permission to test.

---

## 🔧 Features

- Scans all running processes for `powershell.exe` and `pwsh.exe`
- Locates and patches the `AmsiOpenSession` function in memory
- Loops every 500ms to reapply patch to new instances
- Logs actions to the console for debugging

---

## 🛠 Requirements

- .NET Framework 4.8
- Windows OS

---

## 🧪 How it Works

1. Enumerates all system processes.
2. Identifies processes with the name `powershell.exe` or `pwsh.exe`.
3. Reads memory near `AmsiOpenSession` from `amsi.dll` in the target process.
4. Searches for a specific byte pattern.
5. Overwrites the memory to redirect execution and disable AMSI.

---

## 💻 Usage

1. Compile with Visual Studio targeting `.NET Framework 4.8`
2. Run the tool as Administrator
3. Keep the tool running to patch new PowerShell sessions automatically

```bash
AMSI patched successfully in PID 1234
Sleeping 500 ms...
```

---

## 📄 Legal

This code is provided without any warranty or guarantee. Use at your own risk. Only deploy or test this tool in environments where you have explicit permission.

---

## 🔗 References

- [Microsoft AMSI Documentation](https://learn.microsoft.com/en-us/windows/win32/amsi/antimalware-scan-interface-portal)
- Research from security blogs and public malware analysis
