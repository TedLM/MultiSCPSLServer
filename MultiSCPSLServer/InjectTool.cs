using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyHook;
namespace MultiSCPSLServer
{
    class InjectTool
    {
        private static string InjectLibraryPath = @"D:\code\CSharp\Source\Repos\SCPSLServerHook\SCPSLServerHook\bin\x64\Debug\SCPSLServerHook.dll";

        public static Process RunAndInject(string channel_name, string exe_path, string args, string work_dir)
        {
            Process process = new Process();
            process.StartInfo.Arguments = args;
            process.StartInfo.FileName = exe_path;
            process.StartInfo.WorkingDirectory = work_dir;
            process.Start();
            RemoteHooking.Inject(process.Id, InjectLibraryPath, InjectLibraryPath, channel_name);
            return process;
        }

        public static string CreateIPCServer(ServerHookCallback hookCallback)
        {
            string channel_name = null;
            RemoteHooking.IpcCreateServer<ServerHookCallback>(ref channel_name, System.Runtime.Remoting.WellKnownObjectMode.Singleton, hookCallback);
            return channel_name;
        }

    }
}
