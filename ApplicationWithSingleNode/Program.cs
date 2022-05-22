using DistributedSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
using Google.Protobuf;
namespace ApplicationWithSingleNode
{
    class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        private static async Task MainAsync()
        {
            Console.WriteLine(" Process listening port:");
            string input = Console.ReadLine();

            Init(input);
            while (true)
            {
               string command= Console.ReadLine();
                CommandToProcess(command);
            }
        }
        private static void CommandToProcess(string command)
        {
            string[] commandParts = command.Split(' ');
            switch (commandParts[0])
            {
                case "initsys":
                    InitSys(commandParts);
                    break;
                case "broadcast":
                    Broadcast(commandParts);
                    break;
                case "write":
                    WriteRegister(commandParts);
                    break;
                case "read":
                    ReadRegister(commandParts);
                    break;
                default:
                    break;
            }
        }

        private static void InitSys(string[] commandParts)
        {
            Message m = new Message();
            m.ToAbstractionId="app";
            m.SystemId = "system1";
            m.Type = Message.Types.Type.ProcInitializeSystem;
            m.ProcInitializeSystem = new ProcInitializeSystem();
            for (int i = 1; i < commandParts.Length; i++)
            {
                ProcessId id = new ProcessId();
                id.Host = "127.0.0.1";
                id.Index = i;
                id.Owner = "admin";
                id.Port =int.Parse( commandParts[i]);
                id.Rank = i;
                m.ProcInitializeSystem.Processes.Add(id);
            }
            _Process.ProcessMessage(m);
        }
        private static void Broadcast(string[] commandParts)
        {

            Message m = new Message();
            m.ToAbstractionId = "app";
            m.SystemId = "system1";
            m.Type = Message.Types.Type.AppBroadcast;
            m.AppBroadcast = new AppBroadcast();
            m.AppBroadcast.Value = new Value();
            m.AppBroadcast.Value.V =int.Parse( commandParts[1]);
            _Process.ProcessMessage(m);
        }
        private static void WriteRegister(string[] commandParts)
        {

            Message m = new Message();
            m.ToAbstractionId = "app";
            m.SystemId = "system1";
            m.Type = Message.Types.Type.AppWrite;
            m.AppWrite = new AppWrite();
            m.AppWrite.Register = commandParts[1];
            m.AppWrite.Value = new Value();
            m.AppWrite.Value.V = int.Parse(commandParts[2]);
            _Process.ProcessMessage(m);
        }
        private static void ReadRegister(string[] commandParts)
        {

            Message m = new Message();
            m.ToAbstractionId = "app";
            m.SystemId = "system1";
            m.Type = Message.Types.Type.AppRead;
            m.AppRead = new AppRead();
            m.AppRead.Register = commandParts[1];
            _Process.ProcessMessage(m);
        }
        private static void Init(string input)
        {
            string[] args = input.Split(' ');
            _HubIP = "127.0.0.1";
            _HubPort =10000;
            _MyIP = "127.0.0.1";
            _Owner = "admin";

            _Process = new Process(_HubIP, _HubPort, _MyIP, _Owner, int.Parse(args[0]), 1,false);
            _Thread = new Thread(new ParameterizedThreadStart(StartProcessLoop));
            _Thread.Start(_Process);

        }
        private static void StartProcessLoop(object process)
        {
            ((Process)process).Run();
        }
        private static Process _Process = null;
        private static Thread _Thread;

        private static readonly object _Locker = new object();
        private static string _HubIP;
        private static string _MyIP;
        private static int _HubPort;
        private static string _Owner;

    }
}
