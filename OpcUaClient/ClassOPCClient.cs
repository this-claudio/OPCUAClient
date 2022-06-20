using Opc.Ua;
using Opc.UaFx;
using Opc.UaFx.Client;
using System;

namespace OpcUaCommon
{
    public class ClassOPCClient
    {
        string sAddress = string.Empty;

        OpcClient oConnection = null;

        public ClassOPCClient(string sConnection)
        {
            sAddress = sConnection;
            oConnection = new OpcClient(sAddress);
            oConnection.Security.EndpointPolicy = new OpcSecurityPolicy(OpcSecurityMode.None);


            try
            {
                oConnection.Connect();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public void SetNodeValue(string sNode, string sValue)
        {
            Checkconnection();

            var Tipo = oConnection.ReadNode(sNode).DataType;
            switch (Tipo)
            {
                case OpcDataType.Boolean:
                    var ValorBool = Convert.ToBoolean(sValue);
                    oConnection.WriteNode(sNode, ValorBool); break;
                case OpcDataType.String:
                    var ValorString = Convert.ToString(sValue);
                    oConnection.WriteNode(sNode, ValorString); break;
                case OpcDataType.Int16:
                    var ValorInt16 = Convert.ToInt16(sValue);
                    oConnection.WriteNode(sNode, ValorInt16); break;
                case OpcDataType.Int32:
                    var ValorInt32 = Convert.ToInt32(sValue);
                    oConnection.WriteNode(sNode, ValorInt32); break;
            }
        }

        public object GetNodeValue(string sNode)
        {
            Checkconnection();
            var Response = oConnection.ReadNode(sNode);
            switch (Response.DataType)
            {
                case OpcDataType.Boolean:
                    return (bool)Response.Value;
                case OpcDataType.String:
                    return Response.Value.ToString();
                case OpcDataType.Int16:
                    return Convert.ToInt32(Response.Value);
                case OpcDataType.Int32:
                    return Convert.ToInt32(Response.Value);
                default: return null;
            }
            
        }

        private void Checkconnection()
        {
            if(!(oConnection.State == OpcClientState.Connected))
            {
                try
                {
                    oConnection.Connect();
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
        }
    }
}
