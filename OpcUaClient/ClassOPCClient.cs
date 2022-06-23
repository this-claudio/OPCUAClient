/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 *
 * 
 * 
 * This code was made basead on this sample repository: 
 * https://github.com/OPCFoundation/UA-.NETStandard-Samples/tree/master/Samples/NetCoreConsoleClient
 * 
 * 
 * 
 * ======================================================================*/


using Opc.Ua;   
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;

namespace OpcUaCommon
{
    public class ClassOPCClient
    {
        private SessionReconnectHandler reconnectHandler;

        public string sEndereco { get; set; }
        public int nPorta { get; set; }
        public string sUser { get; set; }
        public string sPassWord { get; set; }
        public Session oSession { get; set; }

        public EstadoConexao SessioStatus { get; set; }
        public int ReconnectPeriod { get; private set; }

        public ClassOPCClient(string sEndereco, string sPorta, string sUser = "", string sPassWord = "")
        {
            this.sEndereco = sEndereco;
            this.nPorta = Convert.ToInt32(sPorta);
            this.sUser = sUser;
            this.sPassWord = sPassWord;
            ReconnectPeriod = 10;
            this.SessioStatus = new EstadoConexao();
            this.SessioStatus = EstadoConexao.Desconectado;
        }


        public void Conectar()
        {
            Console.WriteLine("Step 1 - Create application configuration and certificate.");
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "MyOPCUAClass",
                ApplicationUri = Utils.Format(@"urn:{0}:MyOPCUAClass", System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = "MyOPCUAClass" },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };
            config.Validate(ApplicationType.Client).GetAwaiter().GetResult();
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            }

            var application = new ApplicationInstance
            {
                ApplicationName = "MyOPCUAClass",
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config
            };
            application.CheckApplicationInstanceCertificate(false, 2048).GetAwaiter().GetResult();
            //var servers = CoreClientUtils.DiscoverServers(config);

            bool UserSeguranca = (string.IsNullOrEmpty(sUser) || string.IsNullOrEmpty(sPassWord)) ? false : true;

            EndpointDescription oSelectedEndpoint = CoreClientUtils.SelectEndpoint("opc.tcp://SQO-053.mshome.net:4840", useSecurity: UserSeguranca);
            UserIdentity oNewUser = (string.IsNullOrEmpty(sUser) || string.IsNullOrEmpty(sPassWord)) ? null : new UserIdentity(this.sUser,this.sPassWord);

            Console.WriteLine($"Step 2 - Create a session with your server: {oSelectedEndpoint.EndpointUrl} ");
            this.oSession = Session.Create(config, new ConfiguredEndpoint(null, oSelectedEndpoint, EndpointConfiguration.Create(config)), false, "", 60000, oNewUser, null).GetAwaiter().GetResult();
            this.oSession.KeepAlive += ClientKeepAlive;

            if (this.oSession.Connected) this.SessioStatus = EstadoConexao.Conectado;
            else this.SessioStatus = EstadoConexao.Desconectado;


        }

        private void ClientKeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                Console.WriteLine("{0} {1}/{2}", e.Status, sender.OutstandingRequestCount, sender.DefunctRequestCount);

                if (reconnectHandler == null)
                {
                    this.SessioStatus = EstadoConexao.Reconectando;
                    Console.WriteLine("--- RECONNECTING ---");
                    reconnectHandler = new SessionReconnectHandler();
                    reconnectHandler.BeginReconnect(sender, ReconnectPeriod * 1000, Client_ReconnectComplete);
                }
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, reconnectHandler))
            {
                return;
            }

            this.oSession = reconnectHandler.Session;
            reconnectHandler.Dispose();
            reconnectHandler = null;
            this.SessioStatus = EstadoConexao.Conectado;
            Console.WriteLine("--- RECONNECTED ---");
        }


        public void SetNodeValue(string sNode, object oValue)
        {
            WriteValue Node = new WriteValue();
            Node.NodeId = new NodeId(sNode);
            Node.AttributeId = Attributes.Value;
            Node.Value = new DataValue();
            Node.Value.Value = oValue;

            WriteValueCollection WriteNode = new WriteValueCollection();
            WriteNode.Add(Node);

            StatusCodeCollection ResultWrite = new StatusCodeCollection();
            DiagnosticInfoCollection Diagnosticwrite = new DiagnosticInfoCollection();

            try
            {
                this.oSession.Write(null, WriteNode, out ResultWrite, out Diagnosticwrite);

                if (ResultWrite.Count <= 0)
                {
                    throw new Exception("Não foi possivel escrever o valor");
                }
            }
            catch (Exception e)
            { throw new Exception("Não foi possivel ler o sinal, devido a: " + e.ToString()); }


        }

        public object GetNodeValue(string sNode)
        {

            ReadValueId node = new ReadValueId();
            node.NodeId = new NodeId(sNode);
            node.AttributeId = Attributes.Value;

            ReadValueIdCollection readnodes = new ReadValueIdCollection();
            readnodes.Add(node);

            DataValueCollection ResultRead = new DataValueCollection();
            DiagnosticInfoCollection Diagnostic = new DiagnosticInfoCollection();

            try
            {
                this.oSession.Read(null, 10.0, new TimestampsToReturn(), readnodes, out ResultRead, out Diagnostic);

                if (ResultRead.Count > 0)
                {
                    return ResultRead[0].Value;
                }
                else return null;
            }
            catch(Exception e) 
            { throw new Exception("Não foi possivel ler o sinal, devido a: " + e.ToString()); }

            

        }

        public TypeInfo GetNodeValueType(string sNode)
        {

            ReadValueId node = new ReadValueId();
            node.NodeId = new NodeId(sNode);
            node.AttributeId = Attributes.Value;

            ReadValueIdCollection readnodes = new ReadValueIdCollection();
            readnodes.Add(node);

            DataValueCollection ResultRead = new DataValueCollection();
            DiagnosticInfoCollection Diagnostic = new DiagnosticInfoCollection();

            try
            { 
                this.oSession.Read(null, 10.0, new TimestampsToReturn(), readnodes, out ResultRead, out Diagnostic);

                if (ResultRead.Count > 0)
                {
                    return ResultRead[0].WrappedValue.TypeInfo;
                }
                else return null;

            }
            catch(Exception e) 
            { throw new Exception("Não foi possivel ler o sinal, devido a: " + e.ToString());}

        }

        public enum EstadoConexao
        { 
            Conectado,
            Desconectado,
            Reconectando,
            Desconectando,
            Indefinido
        }



    }
}
