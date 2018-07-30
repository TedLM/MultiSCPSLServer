using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyHook;
namespace MultiSCPSLServer
{
    class Program
    {
        static void Main(string[] args)
        {
            SCPSLServer server = new SCPSLServer(
                "test",
                @"D:\Program Files (x86)\Steam\steamapps\common\SCPSL_Server\SCPSL.exe",
                "D:\\test");
            Process.GetCurrentProcess().Exited += new EventHandler((object sender, EventArgs e) =>
            {
                server.forceStopServer();
            });
            server.StartRServer();
            server.messageComingListener += new SCPSLServer.MessageComingHandler((int inMessageID, string message) =>
            {
                Console.WriteLine(message);
            });
            for (; ; )
            {
                server.SendMessage(Console.ReadLine());
                Thread.Sleep(500);
            }
        }
    }
}