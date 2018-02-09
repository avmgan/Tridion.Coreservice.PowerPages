using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.Configuration;
using Tridion.ContentManager.CoreService.Client;
using System.Net;
using System.Xml;

namespace Tridion.CoreServices.PowerPages.Tridion
{
    public class CoreService
    {
        //public static ISessionAwareCoreService GetClient()
        //{
        //    ChannelFactory<ISessionAwareCoreService> factory = new ChannelFactory<ISessionAwareCoreService>("wsHttp_2013");
        //    ISessionAwareCoreService mClient; //= factory.CreateChannel();
        //    Logger.Info("HttpContext.Current.Request.LogonUserIdentity.Name {0}", HttpContext.Current.Request.LogonUserIdentity.Name);
        //    //mClient.Impersonate(HttpContext.Current.Request.LogonUserIdentity.Name);
        //    //mClient.Impersonate("vagrant");  //TO TEST LOCALE
        //    NetworkCredential networkCredential = new NetworkCredential("vagrant", "vagrant", "");
        //    factory.Credentials.Windows.ClientCredential = networkCredential;
        //    mClient = factory.CreateChannel();
        //    //Logger.Info(mClient.GetCurrentUser().Title);
        //    return mClient;
        //}

        private readonly CoreServiceClient _client;
        private const string ServiceUrl = "{0}webservices/CoreService201603.svc/basicHttp";
        private const int MaxMessageSize = 10485760;

        public CoreService(string hostname)
        {
            _client = CreateBasicHttpClient(hostname);
        }

        public CoreService(string hostname, string username, string password, bool IsSecured)
        {
            if (IsSecured)
                _client = CreateBasicHttpsClient(hostname);
            else
                _client = CreateBasicHttpClient(hostname);

            _client.ChannelFactory.Credentials.Windows.ClientCredential =
                            new System.Net.NetworkCredential(username, password);
        }

        public static CoreServiceClient GetClient(string hostname, string username, string password, bool IsSecured)
        {
            var mClient = HttpContext.Current.Session["ClientData"] as CoreServiceClient;
            if (mClient == null)
            {
                if (IsSecured)
                    mClient = CreateBasicHttpsClient(hostname);
                else
                    mClient = CreateBasicHttpClient(hostname);

                mClient.ChannelFactory.Credentials.Windows.ClientCredential =
                       new System.Net.NetworkCredential(username, password);

                HttpContext.Current.Session["ClientData"] = mClient;
            }
            else if (mClient != null && mClient.State == CommunicationState.Faulted)
            {
                mClient.Abort();
                mClient = null;
                HttpContext.Current.Session["ClientData"] = null;

                if (IsSecured)
                    mClient = CreateBasicHttpsClient(hostname);
                else
                    mClient = CreateBasicHttpClient(hostname);

                mClient.ChannelFactory.Credentials.Windows.ClientCredential =
                       new System.Net.NetworkCredential(username, password);

                HttpContext.Current.Session["ClientData"] = mClient;
            }

            return mClient;
        }

        public static CoreServiceClient CreateBasicHttpClient(string hostname)
        {
            var basicHttpBinding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = MaxMessageSize,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxStringContentLength = MaxMessageSize,
                    MaxArrayLength = MaxMessageSize
                },
                Security = new BasicHttpSecurity
                {
                    Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                    Transport = new HttpTransportSecurity
                    {
                        ClientCredentialType = HttpClientCredentialType.Windows
                    }
                }
            };

            hostname = string.Format("{0}{1}{2}", hostname.StartsWith("http") ? "" : "http://", hostname, hostname.EndsWith("/") ? "" : "/");

            var remoteAddress = new EndpointAddress(string.Format(ServiceUrl, hostname));
            return new CoreServiceClient(basicHttpBinding, remoteAddress); ;
        }

        private static CoreServiceClient CreateBasicHttpsClient(string hostname)
        {
            var basicHttpBinding = new BasicHttpsBinding
            {
                MaxReceivedMessageSize = MaxMessageSize,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxStringContentLength = MaxMessageSize,
                    MaxArrayLength = MaxMessageSize
                },
                Security = new BasicHttpsSecurity
                {
                    Mode = BasicHttpsSecurityMode.Transport,
                    Transport = new HttpTransportSecurity
                    {
                        ClientCredentialType = HttpClientCredentialType.Basic
                    }
                }
            };

            hostname = string.Format("{0}{1}{2}", hostname.StartsWith("https") ? "" : "https://", hostname, hostname.EndsWith("/") ? "" : "/");
            var remoteAddress = new EndpointAddress(string.Format(ServiceUrl, hostname));
            return new CoreServiceClient(basicHttpBinding, remoteAddress); ;
        }

        public void Dispose()
        {
            if (_client.State == CommunicationState.Faulted)
            {
                _client.Abort();
            }
            else
            {
                _client.Close();
            }
        }

        #region Core Service calls
        // Calls to the Core Service client library. Extend the Core service calls as needed.

        public UserData GetCurrentUser()
        {
            return _client.GetCurrentUser();
        }

        public PublishTransactionData[] Publish(string[] itemUris, PublishInstructionData publishInstructionData,
            string[] destinationTargetUris, PublishPriority publishPriority, ReadOptions readOptions)
        {
            return _client.Publish(itemUris, publishInstructionData, destinationTargetUris, publishPriority, readOptions);
        }

        public IdentifiableObjectData Read(string id, ReadOptions readOptions)
        {
            return _client.Read(id, readOptions);
        }
        #endregion
    }
}