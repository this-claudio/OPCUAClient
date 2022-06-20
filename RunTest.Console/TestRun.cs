
using OpcUaCommon;
using System;

namespace MyHomework
{
    class Program
    {
        static void Main(string[] args)
        {

            var Cliente = new ClassOPCClient("your-ip", "4840","abcd","1234");
            Cliente.Conectar();
            string Connected = Cliente.oSession.Connected.ToString();


            Console.WriteLine(Connected);

            var NodeValue = (bool)Cliente.GetNodeValue("ns=3;i=1015");
            Console.WriteLine("Leitura do nó: "+ NodeValue.ToString());

            Cliente.SetNodeValue("ns=3;i=1015", true);

            Console.ReadLine();
            
        }

       
    }
}