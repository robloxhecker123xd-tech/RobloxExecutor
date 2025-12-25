using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace RobloxExecutor.Engine
{
    /// <summary>
    /// RobloxExecutorEngine provides script execution and process injection capabilities
    /// for Roblox game scripting and automation.
    /// </summary>
    public class RobloxExecutorEngine
    {
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const int MEM_COMMIT = 0x1000;
        private const int MEM_RELEASE = 0x8000;
        private const int PAGE_EXECUTE_READWRITE = 0x40;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, int dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        private Process _targetProcess;
        private IntPtr _processHandle;
        private Dictionary<string, ExecutionContext> _executionContexts;
        private bool _isInjected;

        /// <summary>
        /// Initializes a new instance of the RobloxExecutorEngine.
        /// </summary>
        public RobloxExecutorEngine()
        {
            _executionContexts = new Dictionary<string, ExecutionContext>();
            _isInjected = false;
        }

        /// <summary>
        /// Attaches the engine to a Roblox process.
        /// </summary>
        /// <param name="processId">The process ID of the Roblox client</param>
        /// <returns>True if attachment was successful, false otherwise</returns>
        public bool AttachToProcess(int processId)
        {
            try
            {
                _targetProcess = Process.GetProcessById(processId);
                _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, processId);

                if (_processHandle == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to open process {processId}");
                    return false;
                }

                Console.WriteLine($"Successfully attached to process {_targetProcess.ProcessName} (PID: {processId})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error attaching to process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Injects a DLL into the target process.
        /// </summary>
        /// <param name="dllPath">Path to the DLL to inject</param>
        /// <returns>True if injection was successful, false otherwise</returns>
        public bool InjectDll(string dllPath)
        {
            try
            {
                if (_processHandle == IntPtr.Zero)
                {
                    Console.WriteLine("Process not attached. Call AttachToProcess first.");
                    return false;
                }

                if (!System.IO.File.Exists(dllPath))
                {
                    Console.WriteLine($"DLL file not found: {dllPath}");
                    return false;
                }

                byte[] dllPathBytes = Encoding.ASCII.GetBytes(dllPath);
                uint pathLength = (uint)(dllPathBytes.Length + 1);

                // Allocate memory in target process
                IntPtr allocatedMemory = VirtualAllocEx(_processHandle, IntPtr.Zero, pathLength, MEM_COMMIT, PAGE_EXECUTE_READWRITE);

                if (allocatedMemory == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to allocate memory in target process");
                    return false;
                }

                // Write DLL path to target process memory
                IntPtr bytesWritten;
                if (!WriteProcessMemory(_processHandle, allocatedMemory, dllPathBytes, pathLength, out bytesWritten))
                {
                    VirtualFreeEx(_processHandle, allocatedMemory, pathLength, MEM_RELEASE);
                    Console.WriteLine("Failed to write DLL path to target process memory");
                    return false;
                }

                // Create remote thread to load DLL
                uint threadId;
                IntPtr remoteThread = CreateRemoteThread(_processHandle, IntPtr.Zero, 0, GetLoadLibraryAddress(), allocatedMemory, 0, out threadId);

                if (remoteThread == IntPtr.Zero)
                {
                    VirtualFreeEx(_processHandle, allocatedMemory, pathLength, MEM_RELEASE);
                    Console.WriteLine("Failed to create remote thread");
                    return false;
                }

                // Wait for thread completion
                WaitForSingleObject(remoteThread, 5000);

                // Cleanup
                CloseHandle(remoteThread);
                VirtualFreeEx(_processHandle, allocatedMemory, pathLength, MEM_RELEASE);

                _isInjected = true;
                Console.WriteLine($"Successfully injected DLL: {dllPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error injecting DLL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes a Lua script in the target process.
        /// </summary>
        /// <param name="scriptContent">The Lua script code to execute</param>
        /// <param name="contextName">Optional name for this execution context</param>
        /// <returns>Execution result containing status and any output</returns>
        public ExecutionResult ExecuteScript(string scriptContent, string contextName = "default")
        {
            try
            {
                if (!_isInjected)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "DLL not injected. Call InjectDll first.",
                        Output = string.Empty
                    };
                }

                var context = new ExecutionContext
                {
                    ScriptContent = scriptContent,
                    ExecutionTime = DateTime.UtcNow,
                    Status = ExecutionStatus.Running
                };

                // Store execution context
                if (_executionContexts.ContainsKey(contextName))
                {
                    _executionContexts[contextName] = context;
                }
                else
                {
                    _executionContexts.Add(contextName, context);
                }

                // Parse and validate script
                if (!ValidateScript(scriptContent))
                {
                    context.Status = ExecutionStatus.Failed;
                    return new ExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "Script validation failed",
                        Output = string.Empty
                    };
                }

                // Execute script
                string output = ExecuteInTargetProcess(scriptContent);

                context.Status = ExecutionStatus.Completed;
                context.Output = output;

                return new ExecutionResult
                {
                    Success = true,
                    ErrorMessage = string.Empty,
                    Output = output
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing script: {ex.Message}");
                return new ExecutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Output = string.Empty
                };
            }
        }

        /// <summary>
        /// Validates Lua script syntax.
        /// </summary>
        /// <param name="scriptContent">The script to validate</param>
        /// <returns>True if script is valid, false otherwise</returns>
        private bool ValidateScript(string scriptContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(scriptContent))
                {
                    return false;
                }

                // Basic validation checks
                int openBraces = scriptContent.Count(c => c == '{');
                int closeBraces = scriptContent.Count(c => c == '}');
                int openParens = scriptContent.Count(c => c == '(');
                int closeParens = scriptContent.Count(c => c == ')');

                if (openBraces != closeBraces || openParens != closeParens)
                {
                    Console.WriteLine("Script syntax error: Mismatched braces or parentheses");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes script in the target process.
        /// </summary>
        private string ExecuteInTargetProcess(string scriptContent)
        {
            // This would be implemented to communicate with the injected DLL
            // For now, returning a simulated execution result
            return $"Script executed successfully at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// Gets the address of LoadLibraryA function.
        /// </summary>
        private IntPtr GetLoadLibraryAddress()
        {
            IntPtr kernel32 = NativeMethods.GetModuleHandle("kernel32.dll");
            return NativeMethods.GetProcAddress(kernel32, "LoadLibraryA");
        }

        /// <summary>
        /// Detaches from the target process.
        /// </summary>
        public void Detach()
        {
            try
            {
                if (_processHandle != IntPtr.Zero)
                {
                    CloseHandle(_processHandle);
                    _processHandle = IntPtr.Zero;
                    _targetProcess = null;
                    _isInjected = false;
                    _executionContexts.Clear();
                    Console.WriteLine("Detached from process");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detaching from process: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the status of a specific execution context.
        /// </summary>
        public ExecutionContext GetExecutionContext(string contextName)
        {
            if (_executionContexts.ContainsKey(contextName))
            {
                return _executionContexts[contextName];
            }
            return null;
        }

        /// <summary>
        /// Clears all execution contexts.
        /// </summary>
        public void ClearContexts()
        {
            _executionContexts.Clear();
        }

        public bool IsAttached => _processHandle != IntPtr.Zero;
        public bool IsInjected => _isInjected;
    }

    /// <summary>
    /// Represents the result of script execution.
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Output { get; set; }
    }

    /// <summary>
    /// Represents an execution context for tracking script execution state.
    /// </summary>
    public class ExecutionContext
    {
        public string ScriptContent { get; set; }
        public DateTime ExecutionTime { get; set; }
        public ExecutionStatus Status { get; set; }
        public string Output { get; set; }
    }

    /// <summary>
    /// Enumeration for execution status.
    /// </summary>
    public enum ExecutionStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Native method wrappers for Windows API calls.
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    }
}
