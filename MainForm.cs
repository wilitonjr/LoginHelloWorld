﻿using Newtonsoft.Json;
using SQLSafe.Login.Poc;
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
        private IdentityProvider currentProvider = IdentityProvider.Okta;

        public MainForm()
        {
            InitializeComponent();
            InitializeIdentityProviders();
            UpdateLoginStatus(false);
        }

        #region Buttons/ComboBox

        private void cmbIdentityProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentProvider = (IdentityProvider)cmbIdentityProvider.SelectedIndex;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            var authUrl = IdentityProviderConfig.GetAuthUrl(currentProvider);

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            var authCode = await ListenForCallback();

            if (!string.IsNullOrEmpty(authCode))
            {
                var accessToken = await GetAccessToken(authCode);
                var userProfile = await GetUserProfile(accessToken);

                switch (cmbIdentityProvider.SelectedIndex)
                {
                    case 0: // Okta (Auth0)
                        string nick = userProfile.nickname;
                        var nickname = char.ToUpper(nick[0]) + nick.Substring(1);
                        var fullName = $"{userProfile.name}";
                        var email = userProfile.email;

                        lblNickname.Text = $"Welcome, {nickname}";
                        lblName.Text = $"{fullName} ({email})";
                        break;

                    case 1: // Entra ID (Azure AD)
                        var entraFullName = $"{userProfile.displayName}";
                        var entraEmail = userProfile.mail ?? userProfile.userPrincipalName;

                        lblNickname.Text = $"Welcome, {entraFullName.Split(' ')[0]}";
                        lblName.Text = $"{entraFullName} ({entraEmail})";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                UpdateLoginStatus(true);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            using (var client = new HttpClient())
            {
                client.GetAsync(IdentityProviderConfig.RedirectLogoutUrl()).Wait();
            }

            var logoutUrl = IdentityProviderConfig.GetLogoutUrl(currentProvider);

            Process.Start(new ProcessStartInfo(logoutUrl) { UseShellExecute = true });

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
                var tokenUrl = IdentityProviderConfig.GetTokenUrl(currentProvider);

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "code", authCode },
                        { "client_id", IdentityProviderConfig.GetClientId(currentProvider) },
                        { "client_secret", IdentityProviderConfig.GetClientSecret(currentProvider) },
                        { "redirect_uri", IdentityProviderConfig.RedirectUrl(currentProvider) }
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

                var userInfoUrl = IdentityProviderConfig.GetUserInfoUrl(currentProvider);
                var response = await client.GetAsync(userInfoUrl);
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
            cmbIdentityProvider.SelectedIndex = (int)IdentityProvider.Okta;
        }
    }
}