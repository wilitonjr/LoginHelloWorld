using System;

namespace SQLSafe.Login.Poc
{
    public enum IdentityProvider
    {
        Okta,
        EntraID
    }

    internal class IdentityProviderConfig
    {
        //Okta
        private static readonly string OKTA_AUTHORITY = "https://dev-0qje6s4wwpdjf1px.us.auth0.com";
        private static readonly string OKTA_CLIENT_ID = "";
        private static readonly string OKTA_CLIENT_SECRET = "";

        //EntraID
        private static readonly string ENTRA_AUTHORITY = "https://login.microsoftonline.com/d053cd9b-57ed-4738-9208-6a2e6600380f/oauth2/v2.0";
        private static readonly string ENTRA_CLIENT_ID = "";
        private static readonly string ENTRA_CLIENT_SECRET = "";

        //Common
        private static readonly string REDIRECT_URI = "http://localhost:5000/callback";
        private static readonly string RESPONSE_TYPE = "code";

        public static string GetAuthority(IdentityProvider provider)
        {
            switch (provider)
            {
                case IdentityProvider.Okta:
                    return OKTA_AUTHORITY;
                case IdentityProvider.EntraID:
                    return ENTRA_AUTHORITY;
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        public static string GetClientId(IdentityProvider provider)
        {
            switch (provider)
            {
                case IdentityProvider.Okta:
                    return OKTA_CLIENT_ID;
                case IdentityProvider.EntraID:
                    return ENTRA_CLIENT_ID;
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        public static string GetClientSecret(IdentityProvider provider)
        {
            switch (provider)
            {
                case IdentityProvider.Okta:
                    return OKTA_CLIENT_SECRET;
                case IdentityProvider.EntraID:
                    return ENTRA_CLIENT_SECRET;
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        public static string GetAuthUrl(IdentityProvider provider)
        {
            return $"{GetAuthority(provider)}/authorize?" +
                $"client_id={Uri.EscapeDataString(GetClientId(provider))}" +
                $"&response_type={Uri.EscapeDataString(RESPONSE_TYPE)}" +
                $"&scope=openid profile email" +
                $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
                $"&state=random_state_value";
        }

        public static string GetLogoutUrl(IdentityProvider provider)
        {
            return $"{GetAuthority(provider)}/v2/logout?" +
                $"client_id={Uri.EscapeDataString(GetClientId(provider))}" +
                $"&returnTo={Uri.EscapeDataString("http://localhost:5000/logout-callback")}";

        }

        public static string GetTokenUrl(IdentityProvider provider)
        {
            switch (provider)
            {
                case IdentityProvider.Okta:
                    return $"{GetAuthority(provider)}/oauth/token";
                case IdentityProvider.EntraID:
                    return $"{GetAuthority(provider)}/token";
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }
        public static string GetUserInfoUrl(IdentityProvider provider)
        {
            switch (provider)
            {
                case IdentityProvider.Okta:
                    return $"{GetAuthority(provider)}/userinfo";
                case IdentityProvider.EntraID:
                    return "https://graph.microsoft.com/v1.0/me";
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        public static string GetRedirectUri(IdentityProvider provider)
        {
            return REDIRECT_URI;
        }
    }
}
