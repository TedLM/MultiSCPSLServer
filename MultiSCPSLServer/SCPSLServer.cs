using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyHook;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MultiSCPSLServer
{
    class SCPSLServer : ServerHookCallback
    {
        private string server_name;
        private string scpsl_exe_path;
        private string server_configs_dir;
        private string server_work_dir;
        private Random random = new Random(DateTime.Now.Millisecond);
        private string sm_plugins_dir;
        private string config_dir;
        private string logs_dir;
        private int session;
        private int outMessageID;
        private int inMessageID;
        private string channel_name;
        private static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory";

        private Process serverProcess;

        private bool runningFlag;

        public string Server_name { get => server_name; }
        public string Scpsl_exe_path { get => scpsl_exe_path; }
        public string Server_configs_dir { get => server_configs_dir; }
        public string Sm_plugins_dir { get => sm_plugins_dir; }
        public string Config_dir { get => config_dir; }
        public string Logs_dir { get => logs_dir; }
        public string Server_work_dir { get => server_work_dir; }
        public int Session { get => session; }

        public delegate void MessageComingHandler(int inMessageID, string message);

        public event MessageComingHandler messageComingListener;

        public SCPSLServer(string server_name, string scpsl_exe_path, string server_configs_dir)
        {
            this.server_name = server_name;
            this.scpsl_exe_path = scpsl_exe_path;
            this.server_configs_dir = server_configs_dir;
            if (this.server_configs_dir.EndsWith("" + Path.DirectorySeparatorChar))
            {
                this.server_configs_dir = this.server_configs_dir.Substring(0, this.server_configs_dir.Length - 2);
            }
            sm_plugins_dir = server_configs_dir + Path.DirectorySeparatorChar + "sm_plugins";
            config_dir = server_configs_dir + Path.DirectorySeparatorChar + "config";
            logs_dir = server_configs_dir + Path.DirectorySeparatorChar + "logs";
            server_work_dir = scpsl_exe_path.Substring(0, scpsl_exe_path.LastIndexOf(Path.DirectorySeparatorChar));
            serverProcess = null;
            session = 0;
            outMessageID = 0;
            inMessageID = 0;
            channel_name = InjectTool.CreateIPCServer(this);
            runningFlag = false;
            InitDirs();
            RunDaemonThread();
        }

        private void RunDaemonThread()
        {
            Thread thread = new Thread(() =>
            {
                string dir = server_work_dir + Path.DirectorySeparatorChar + "SCPSL_Data/Dedicated/" + session;
                for (; ; )
                {
                    if (!runningFlag)
                    {
                        if (serverProcess != null && !serverProcess.HasExited)
                        {
                            serverProcess.Kill();
                        }
                    }
                    else
                    {
                        //检查是否崩溃
                        if (serverProcess == null || serverProcess.HasExited)
                        {
                            StartServerProcess();
                            dir = server_work_dir + Path.DirectorySeparatorChar + "SCPSL_Data/Dedicated/" + session;
                        }
                        //检查是否有新的消息
                        try
                        {
                            string[] array = Directory.GetFiles(dir, "sl*.mapi", SearchOption.TopDirectoryOnly);
                            foreach (string messageFile in array)
                            {
                                StreamReader streamReader = new StreamReader(messageFile);
                                string message = streamReader.ReadToEnd();
                                streamReader.Close();
                                File.Delete(messageFile);
                                messageComingListener(inMessageID++, message);
                            }
                        }
                        catch
                        {

                        }
                    }
                    Thread.Sleep(200);
                }

            });
            thread.Start();
        }

        private void StartServerProcess()
        {
            if (serverProcess != null)
            {
                if (!serverProcess.HasExited)
                    serverProcess.Kill();
                serverProcess = null;
            }
            inMessageID = 0;
            outMessageID = 0;
            string args = GetArgs();
            serverProcess = InjectTool.RunAndInject(channel_name, scpsl_exe_path, args, server_work_dir);
        }

        public void StartRServer()
        {
            runningFlag = true;
        }

        public void forceStopServer()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                serverProcess.Kill();
            }
            runningFlag = false;
        }

        public void stopServer()
        {
            runningFlag = false;
        }

        /**
         * 初始化服务端需要的目录
         * sm_plugins SMod2的目录
         * config 配置文件目录
         * logs 日志文件目录
         * */
        private void InitDirs()
        {
            if (!Directory.Exists(server_configs_dir))
            {
                Directory.CreateDirectory(server_configs_dir);
            }
            if (!Directory.Exists(sm_plugins_dir))
            {
                Directory.CreateDirectory(sm_plugins_dir);
            }
            if (!Directory.Exists(config_dir))
            {
                Directory.CreateDirectory(config_dir);
            }
            if (!Directory.Exists(logs_dir))
            {
                Directory.CreateDirectory(logs_dir);
            }
            if (!Directory.Exists(logs_dir + Path.DirectorySeparatorChar + "ServerLogs"))
            {
                Directory.CreateDirectory(logs_dir + Path.DirectorySeparatorChar + "ServerLogs");
            }
        }

        private string getLogFilePath()
        {
            return logs_dir + Path.DirectorySeparatorChar + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second + ".txt";
        }

        public string GetArgs()
        {
            string logFile = getLogFilePath();
            session = random.Next();
            logFile = session + " " + logFile;
            Directory.CreateDirectory(server_work_dir + Path.DirectorySeparatorChar + "SCPSL_Data" + Path.DirectorySeparatorChar + "Dedicated" + Path.DirectorySeparatorChar + session);
            return "-batchmode -nographics -key" + session + " -silent-crashes -id" + NativeAPI.GetCurrentProcessId() + " -logFile " + '"' + logFile + '"';
        }

        public void SendMessage(string message)
        {
            StreamWriter streamWriter = new StreamWriter(server_work_dir + Path.DirectorySeparatorChar + "SCPSL_Data/Dedicated/" + session + "/cs" + (outMessageID++) + ".mapi");
            streamWriter.WriteLine(message + "terminator");
            streamWriter.Close();
        }

        public override string OnCreateFile(string fileName)
        {

            if (fileName.Contains("sm_plugins"))
            {
                //D:/Program Files (x86)/Steam/steamapps/common/SCPSL_Server/SCPSL_Data/../sm_plugins/AdminToolbox.dll
                //string newFileName = fileName.Replace(AppDataPath + "/sm_plugins", sm_plugins_dir);
                string newFileName = sm_plugins_dir + "/";
                newFileName += fileName.Substring(fileName.IndexOf("sm_plugins") + 11);
                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine(fileName);
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine(newFileName);
                //Console.ForegroundColor = ConsoleColor.White;
                return newFileName;
            }

            if (fileName.Contains(AppDataPath + "/ServerLogs"))
            {
                //Console.WriteLine("true: " + fileName);
                string newFileName = fileName.Replace(AppDataPath + "/ServerLogs", logs_dir + Path.DirectorySeparatorChar + "ServerLogs");
                //Console.WriteLine("replace:" + fileName + " as " + newFileName);
                string logsParentDir = newFileName.Substring(0, newFileName.LastIndexOf(@"/"));
                if (!Directory.Exists(logsParentDir))
                {
                    Directory.CreateDirectory(logsParentDir);
                }
                return newFileName;
            }
            if (fileName.Contains(AppDataPath))
            {
                //Console.WriteLine("true: " + fileName);
                string newFileName = fileName.Replace(AppDataPath, config_dir);
                //Console.WriteLine("replace:" + fileName + " as " + newFileName);
                return newFileName;
            }

            return fileName;
        }

        public override string OnFindFirstFile(string fileName)
        {
            //Console.ForegroundColor = ConsoleColor.DarkGreen;
            //Console.WriteLine(fileName);
            //Console.ForegroundColor = ConsoleColor.White;
            if (fileName.Contains("sm_plugins"))
            {
                string newFileName = sm_plugins_dir + @"\*";
                //Console.ForegroundColor = ConsoleColor.DarkYellow;
                //Console.WriteLine(newFileName);
                //Console.ForegroundColor = ConsoleColor.White;
                return newFileName;
            }
            return fileName;
        }
    }
}
