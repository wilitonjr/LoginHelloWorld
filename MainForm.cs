using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLSafeLoginPoc
{
    public partial class MainForm : Form
    {
        private string authority = "https://dev-0qje6s4wwpdjf1px.us.auth0.com";
        private string clientId = "";
        private string redirectUri = "http://localhost:5000/callback";
        private string responseType = "code";
        private string clientSecret = "";
        private string clientSecretEntra = "";

        public MainForm()
        {
            InitializeComponent();
            InitializeIdentityProviders();
            UpdateLoginStatus(false);
        }

        #region Buttons/ComboBox

        private void cmbIdentityProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbIdentityProvider.SelectedIndex)
            {
                case 0: // Okta (Auth0)
                    authority = "https://dev-0qje6s4wwpdjf1px.us.auth0.com";
                    clientId = "";
                    clientSecret = "";
                    break;
                case 1: // Entra ID (Azure AD)
                    authority = "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0";
                    clientId = "";
                    clientSecret = "";
                    break;
            }
        }


        private async void btnLogin_Click(object sender, EventArgs e)
        {
            var authUrl = $"{authority}/authorize?" +
                $"client_id={clientId}" +
                $"&response_type={responseType}" +
                $"&scope=openid profile email" +
                $"&redirect_uri={redirectUri}" +
                $"&state=random_state_value";

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            var authCode = await ListenForCallback();

            if (!string.IsNullOrEmpty(authCode))
            {
                var accessToken = await GetAccessToken(authCode);
                var userProfile = await GetUserProfile(accessToken);

                string nick = userProfile.nickname;
                var nickname = char.ToUpper(nick[0]) + nick.Substring(1);
                var fullName = $"{userProfile.name}";
                var email = userProfile.email;

                lblNickname.Text = $"Welcome, {nickname}";
                lblName.Text = $"{fullName} ({email})";

                UpdateLoginStatus(true);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            using (var client = new HttpClient())
            {
                client.GetAsync("http://localhost:5000/logout").Wait();
            }

            var auth0LogoutUrl = $"{authority}/v2/logout?" +
                                 $"client_id={clientId}" +
                                 $"&returnTo={Uri.EscapeDataString("http://localhost:5000/logout-callback")}";

            Process.Start(new ProcessStartInfo(auth0LogoutUrl) { UseShellExecute = true });

            UpdateLoginStatus(false);
        }

        #endregion

        private async Task<string> ListenForCallback()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/callback/");
            listener.Start();

            var context = await listener.GetContextAsync();
            var authCode = context.Request.QueryString["code"];

            using (var response = context.Response)
            {
                var responseString = @"
                <html>
                    <body>
                        <h2>Authentication Successful! You can close this window.</h2>
                        <p>This tab will close in <span id='countdown'>3</span> seconds...</p>
                        <script type='text/javascript'>
                            var countdown = 3;
                            var countdownElement = document.getElementById('countdown');
                        
                            var interval = setInterval(function() {
                                countdown--;
                                countdownElement.innerText = countdown;
                                if (countdown <= 0) {
                                    clearInterval(interval);
                                    window.close();
                                }
                            }, 1000);
                        </script>
                    </body>
                </html>";

                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            listener.Stop();
            return authCode;
        }

        private async Task<string> GetAccessToken(string authCode)
        {
            using (var client = new HttpClient())
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{authority}/oauth/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "client_id", clientId },
                        { "client_secret", clientSecret },
                        { "code", authCode },
                        { "redirect_uri", redirectUri }
                    })
                };

                var response = await client.SendAsync(tokenRequest);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseString);

                return tokenResponse.access_token;
            }
        }

        private async Task<dynamic> GetUserProfile(string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var response = await client.GetAsync($"{authority}/userinfo");
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<dynamic>(responseString);
            }
        }

        private void UpdateLoginStatus(bool isLoggedIn)
        {
            if (isLoggedIn)
            {
                lblLoginStatus.Text = "Logged In";
                lblLoginStatus.ForeColor = System.Drawing.Color.Green;
                lblName.Visible = true;
                btnLogout.Enabled = true;
                btnLogin.Enabled = false;
            }
            else
            {
                lblNickname.Text = "Please LogIn";
                lblName.Visible = false;

                lblLoginStatus.Text = "Not Logged In";
                lblLoginStatus.ForeColor = System.Drawing.Color.Red;
                btnLogout.Enabled = false;
                btnLogin.Enabled = true;
            }
        }

        private void InitializeIdentityProviders()
        {
            cmbIdentityProvider.Items.Add("Okta (auth0)");
            cmbIdentityProvider.Items.Add("Entra ID (Azure AD)");
            cmbIdentityProvider.SelectedIndex = 0;
        }
    }
}