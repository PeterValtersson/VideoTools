using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VideoTools.Services
{
    class ProcessHelpers
    {
        internal const int CTRL_C_EVENT = 0;
        [DllImport("kernel32.dll")]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, bool Add);
        // Delegate type to be used as the Handler Routine for SCCH
        delegate bool ConsoleCtrlDelegate(uint CtrlType);
        public static bool StopProcess(Process process)
        {
            var stopped = false;
            if (process is not null)
            {
                if (AttachConsole((uint)process.Id))
                {
                    SetConsoleCtrlHandler(null, true);
                    try
                    {
                        if (GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                        {
                            process.WaitForExit();
                            stopped = true;
                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        SetConsoleCtrlHandler(null, false);
                        FreeConsole();
                    }
                }
            }
            return stopped;
        }
    }
}
