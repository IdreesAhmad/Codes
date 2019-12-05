using Microsoft.Xrm.Sdk;
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Ford.Plugin.CustomerDetailsOnPhoneCall
{
    public class CustomerWebCall : IPlugin
    {
        [DataContract]
        public class profile
        {
            [DataMember]
            public string firstName { get; set; }
            [DataMember]
            public string lastName { get; set; }
            [DataMember]
            public string primaryPhone { get; set; }
            [DataMember]
            public string mobilePhone { get; set; }
            [DataMember]

            public string secondEmail { get; set; }
            [DataMember]
            public string userType { get; set; }
            [DataMember]
            public string login { get; set; }
            [DataMember]
            public string email { get; set; }
        }
        [DataContract]
        public class User
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string status { get; set; }
            [DataMember]
            public string created { get; set; }
            [DataMember]
            public string activated { get; set; }
            [DataMember]
            public string statusChanged { get; set; }
            [DataMember]
            public string lastLogin { get; set; }
            [DataMember]
            public string lastUpdated { get; set; }
            [DataMember]
            public string passwordChanged { get; set; }

            [DataMember]
            public profile profile { get; set; }
        }
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public CustomerWebCall(string unsecureConfig, string secureConfig)
        {
            _secureConfig = secureConfig;
            _unsecureConfig = unsecureConfig;
        }
        #endregion
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;

                Entity entity = (Entity)context.InputParameters["Target"];
                string phoneNumber = entity.GetAttributeValue<string>("phonenumber");
                string URL = "https://dev-527337.okta.com/api/v1/users?search=profile.primaryPhone eq \"" + phoneNumber + "\"";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.Method = "GET";
                request.Accept = "application/json";
                request.ContentType = "application /json";
                request.Headers.Add("Authorization", "SSWS 00A_1QM_I4MlYaxMLzNtuSbfVrW-bpyIZaf4bkLLwD"); //=

                try
                {
                    using (WebResponse webResponse = request.GetResponse())
                    using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                    using (StreamReader responseReader = new StreamReader(webStream))
                    {
                        var user = new User();
                        string response = responseReader.ReadToEnd();
                        if(response.Length > 50)
                        {
                            response = response.Substring(1, response.Length - 2);
                            var ms = new MemoryStream(Encoding.UTF8.GetBytes(response));
                            var ser = new DataContractJsonSerializer(user.GetType());
                            user = ser.ReadObject(ms) as User;
                            entity["avcc_customername"] = user.profile.firstName + " " + user.profile.lastName;
                            entity["avcc_customerid"] = user.id;
                            service.Update(entity);
                        }
                        

                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException(ex.Message);
                }

                //TODO: Do stuff
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}
        