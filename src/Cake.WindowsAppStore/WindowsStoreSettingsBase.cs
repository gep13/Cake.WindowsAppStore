namespace Cake.WindowsAppStore
{
    public abstract class WindowsStoreSettingsBase
    {
        private const string DefaultServiceUrl = "https://manage.devcenter.microsoft.com";
        private const string DefaultTokenEndpoint = "https://login.microsoftonline.com/<tenantid>/oauth2/token";
        private const string DefaultScope = "https://manage.devcenter.microsoft.com";

        private string _serviceUrl;
        private string _tokenEndpoint;
        private string _scope;

        /// <summary>
        /// Client Id of your AAD app.
        /// Example" ba3c223b-03ab-4a44-aa32-38aa10c27e32
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret of your AAD app.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Tenant Id of your AAD app.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Service root endpoint.
        /// </summary>
        public string ServiceUrl
        {
            get { return _serviceUrl ?? DefaultServiceUrl; }
            set { _serviceUrl = value; }
        }

        /// <summary>
        /// Token endpoint to which the request is to be made. Specific to your AAD app
        /// Example: https://login.windows.net/d454d300-128e-2d81-334a-27d9b2baf002/oauth2/token
        /// </summary>
        public string TokenEndpoint
        {
            get { return _tokenEndpoint ?? DefaultTokenEndpoint.Replace("<tenantid>", TenantId); }
            set { _tokenEndpoint = value; }
        }

        /// <summary>
        /// Resource scope. If not provided (set to null), default one is used for the production API
        /// endpoint ("https://manage.devcenter.microsoft.com")
        /// </summary>
        public string Scope
        {
            get { return _scope ?? DefaultScope; }
            set { _scope = value; }
        }

        /// <summary>
        /// Application ID.
        /// Example: 9WZANCRD4AMD
        /// </summary>
        public string ApplicationId { get; set; }

        ///// <summary>
        ///// In-app-product ID;
        ///// Example: 9WZBMAAD4VVV
        ///// </summary>
        //public string InAppProductId { get; set; }

        ///// <summary>
        ///// Flight Id
        ///// Example: 62211033-c2fa-3934-9b03-d72a6b2a171d
        ///// </summary>
        //public string FlightId { get; set; }
    }
}