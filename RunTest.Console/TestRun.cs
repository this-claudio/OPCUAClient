using System;
using OpcUaCommon;

namespace System
{
    class TestRun
    {
        static void Main(string[] args)
        {            
            ClassOPCClient client = null;
            try
            {
                client = new ClassOPCClient("opc.tcp://SQO-053.energpower.com.br:4840");
                client.SetNodeValue("ns=3;i=1013", "102");
            }
            catch(Exception Error)
            {
               
                Console.WriteLine(Error.Message);
            }

            var a = client.GetNodeValue("ns=3;i=1009");
            Console.WriteLine(a.ToString());
            
        }
    }
}
