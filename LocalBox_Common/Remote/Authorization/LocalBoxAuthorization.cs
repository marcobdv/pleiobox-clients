using System;
using System.Text;
using System.Net.Http;
using System.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using Xamarin;
using System.Collections.Generic;

using XLabs.Platform.Services;

namespace LocalBox_Common.Remote.Authorization
{
    public class LocalBoxAuthorization
    {
        private string client_key;
        private string client_secret;
        private string localBoxBaseUrl;

		private LocalBox _localBox;

		private SecureStorage storage = new SecureStorage();
		private ASCIIEncoding encoding = new ASCIIEncoding();

        private string _accessToken = null;
        public string AccessToken { 
            get { return _accessToken; }
        }

        private string _refreshToken;
        public string RefreshToken {
            get { return _refreshToken; }
        }

        private DateTime _expiry;
        public DateTime Expiry {
            get { return _expiry; }
        }

        public LocalBoxAuthorization(LocalBox localBox)
		{			
			_localBox = localBox;

			client_key = encoding.GetString(storage.Retrieve("client_key"));
			client_secret = encoding.GetString(storage.Retrieve ("client_secret"));

			_accessToken = encoding.GetString (storage.Retrieve ("access_token"));
			_refreshToken = encoding.GetString (storage.Retrieve ("refresh_token"));

			var expires = encoding.GetString (storage.Retrieve ("expires"));
			DateTime.TryParse (expires, out _expiry);

			localBoxBaseUrl = localBox.BaseUrl;
        }

		public bool IsAuthorized()
		{
			if (_expiry.Equals (new DateTime (1,1,1))) {
				// Not nooit geautoriseerd:
				return true;
			}
				
			if (_expiry.ToLocalTime() > DateTime.Now.ToLocalTime ()) {
				return true;
			}
			// Of nog niet geauthorizeer: doit
			// of expired: doit.
			return false;
		}


		public bool RefreshAccessToken()
        {
            StringBuilder localBoxUrl = new StringBuilder();
            localBoxUrl.Append(localBoxBaseUrl);
			localBoxUrl.Append("oauth/v2/token");

			if (_refreshToken == null) {
				throw new InvalidOperationException ("Refreshtoken is leeg!");
			}
            
			var data = new List<KeyValuePair<string, string>> ();
			data.Add (new KeyValuePair<string, string> ("client_id", client_key));
			data.Add (new KeyValuePair<string, string> ("client_secret", client_secret));
			data.Add (new KeyValuePair<string, string> ("grant_type", "refresh_token"));
			data.Add (new KeyValuePair<string, string> ("refresh_token", _refreshToken));

			HttpContent content = new FormUrlEncodedContent (data);

			return RequestAccessToken(new Uri(localBoxUrl.ToString()), content);
           
        }

		private bool RequestAccessToken(Uri uri, HttpContent data) {
			bool result = false;

			var handler = new HttpClientHandler {
				Proxy = CoreFoundation.CFNetwork.GetDefaultProxy (),
				UseProxy = true,
			};

			using (var httpClient = new HttpClient (handler)) {
				httpClient.MaxResponseContentBufferSize = int.MaxValue;
				httpClient.DefaultRequestHeaders.ExpectContinue = false;
				httpClient.DefaultRequestHeaders.Add ("x-li-format", "json");

				try {
					var response = httpClient.PostAsync (uri, data).Result;
					if (response.IsSuccessStatusCode) {
						string content = response.Content.ReadAsStringAsync().Result;
						var jsonObject = JsonValue.Parse (content);

						_accessToken = jsonObject ["access_token"];

						if (!string.IsNullOrEmpty (jsonObject ["refresh_token"])) {

							_refreshToken = jsonObject ["refresh_token"];

							int expiry = 0;
							if (jsonObject ["expires_in"].JsonType == JsonType.Number) {
								expiry = (int)jsonObject ["expires_in"];
								// We laten de key al vervallen als al 90% van de tijd is verstreken
								_expiry = DateTime.UtcNow.AddSeconds (expiry * 0.9).ToLocalTime();

								result = true;
							}
						}
							
						storage.Store("access_token", encoding.GetBytes(_accessToken));
						storage.Store("refresh_token", encoding.GetBytes(_refreshToken));
						storage.Store("expires", encoding.GetBytes(_expiry.ToString()));

					} else {
						Debug.WriteLine ("Fout in requestaccesstoken: " + response.Headers); 
					}
				} catch (Exception ex) {
					Insights.Report (ex);
					return false;
				}
			} 

			return result;
        }
    }
}

