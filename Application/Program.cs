using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DistributedSystem;
using Google.Protobuf.Communication;
using Google.Protobuf;
namespace Application
{
    class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        private static async Task MainAsync()
        {
            Console.WriteLine("Hub IP, Hub Listening port, MyIp, Owner, Processess listening ports:");
            string input= Console.ReadLine();

            Init(input);
            Console.ReadLine();

           
            //while(true)
            //{
            //    if(_EventQueue.Count>0)
            //    {
            //        Message m = null;
            //        // Console.WriteLine("Event queue size: {0}", eventQueueSize);
            //        lock (_Locker)
            //        {
            //            m = _EventQueue.Dequeue();
            //        }
            //        ProcessMessage(m);
            //    }
            //    Thread.Sleep(10);
            //}
        }
        private static void Init(string input)
        {
            string[] args = input.Split(' ');
            _HubIP = args[0];
            _HubPort = int.Parse(args[1]);
            _MyIP = args[2];
            _Owner = args[3];

            _Processes = new List<Process>();
            _Threads = new List<Thread>();


            for (int i = 4; i < args.Length; i++)
            {
                _Processes.Add(new Process(_HubIP, _HubPort, _MyIP, _Owner, int.Parse(args[i]), i - 3));
                _Threads.Add(new Thread(new ParameterizedThreadStart(StartProcessLoop)));
                _Threads[i - 4].Start(_Processes[i - 4]);
            }

        }
        private static void StartProcessLoop(object process)
        {
            ((Process)process).Run();
        }
        private static List<Process> _Processes=null;
        private static List<Thread> _Threads;
        //private static Queue<Message> _EventQueue;
        //private static TCPCommunicator _TCPCommunicator;
        private static readonly object _Locker = new object();
        private static string _HubIP;
        private static string _MyIP;
        private static int _HubPort;
        private static string _Owner;
        //private static int _ListeningPort;
        //private static int _Index;
    }
}
