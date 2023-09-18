using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot.Service
{
    public class ShutdownService
    {
        public static Action? OnShutdownSignal;

        public async static void Start()
        {
            var tcs = new TaskCompletionSource();
            var sigintReceived = false;
            Console.CancelKeyPress += (_, ea) =>
            {
                // Tell .NET to not terminate the process
                ea.Cancel = true;
                tcs.SetResult();
                sigintReceived = true;
            };

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                if (!sigintReceived)
                {
                    tcs.SetResult();
                }
                else
                {
                    
                }
            };

            await tcs.Task;
            OnShutdownSignal?.Invoke();
            Log.Info("Killing process!");
            Process.GetCurrentProcess().Kill();
        }
    }
}
