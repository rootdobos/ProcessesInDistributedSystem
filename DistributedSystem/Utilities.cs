using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
namespace DistributedSystem
{
    public static class Utilities
    {
        public static int GetIndexByPort(List<ProcessId> processes, int port)
        {
            for(int i=0;i<processes.Count;i++)
            {
                if (processes[i].Port == port)
                    return i;
            }
            return -1;
        }
        public static  string GetNameInParantheses(string id)
        {
            int startindex = id.IndexOf('[') + 1;
            int endindex = id.IndexOf(']');
            int length = endindex - startindex;
            string name = id.Substring(startindex, length);
            return name;
        }
        public static string GetRegisterName(string id)
        {
            return GetNameInParantheses(id);
        }
        public static bool IsIntersection(List<ProcessId> list1, List<ProcessId> list2)
        {
            if (Intersection(list1, list2).Count > 0)
                return true;
            else
                return false;
        }
        public static List<ProcessId> Intersection(List<ProcessId> list1, List<ProcessId> list2)
        {
            List<ProcessId> intersection = new List<ProcessId>();
            foreach (ProcessId element1 in list1)
                foreach(ProcessId element2 in list2)
                    if(AreEqualProcesses(element1,element2))
                        intersection.Add(element1);
            return intersection;
        }
        public static bool AreEqualProcesses(ProcessId process1,ProcessId process2)
        {
            if (process1.Host != process2.Host)
                return false;
            if (process1.Port != process2.Port)
                return false;
            return true;
        }
        public static bool CheckIfContainsProcessId(List<ProcessId> list, ProcessId process)
        {
            foreach (ProcessId listProcess in list)
            {
                if (AreEqualProcesses(listProcess, process))
                    return true;
            }
            return false;
        }
        public static List<ProcessId> Clone(List<ProcessId> list)
        {
            List<ProcessId> clone = new List<ProcessId>();
            foreach (ProcessId element in list)
                clone.Add(element);
            return clone;
        }
        public static List<ProcessId> Subtraction(List<ProcessId> list1, List<ProcessId> list2)
        {
            List<ProcessId> result = new List<ProcessId>();
            foreach(ProcessId element in list1)
            {
                if (!list2.Contains(element))
                    result.Add(element);
            }
            return result;
        }
        public static ProcessId MaxRank(List<ProcessId> processes)
        {
            ProcessId maxRank = processes[0];
            for(int i=1; i<processes.Count;i++)
            {
                if (maxRank.Rank < processes[i].Rank)
                    maxRank = processes[i];
            }
            return maxRank;
        }
        public static string RemoveMyAbstractionId(string id, string myid)
        {
            string pattern = "." + myid;
           int index= id.IndexOf(pattern);
            return id.Remove(index, pattern.Length);
        }
        public static bool IsMyMessage(string id, string myid)
        {
            string[] targets = id.Split('.');
            string[] myIdSplit = id.Split('.');
            if (targets[targets.Length - 1] == myIdSplit[myIdSplit.Length-1])
                return true;
            else
                return false;
        }
        public static string AddMyAbstractionId(string id, string myid)
        {
            return id + "." + myid;
        }
        public static bool IsElementInString(string str, List<string> list)
        {
            foreach (string element in list)
                if (str.Contains(element))
                    return true;
            return false;
        }
    }
}
