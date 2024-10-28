options.Events = new OpenIdConnectEvents
{
    OnTokenValidated = async context =>
    {
        var authProperties = context.Properties;
        var refreshToken = authProperties.GetTokenValue("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
        {
            // Handle case where no refresh token is available
            return;
        }

        // Check if access token is near expiration
        var expiresAt = authProperties.GetTokenValue("expires_at");
        if (DateTime.TryParse(expiresAt, out DateTime expiry) && expiry < DateTime.UtcNow.AddMinutes(-5))
        {
            var tokenClient = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
            
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{options.Authority}/token");
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", options.ClientId },
                { "client_secret", options.ClientSecret },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            });

            var response = await tokenClient.SendAsync(tokenRequest);
            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

                if (tokenResponse.TryGetValue("access_token", out var newAccessToken))
                {
                    authProperties.UpdateTokenValue("access_token", newAccessToken);
                    authProperties.UpdateTokenValue("expires_at", DateTime.UtcNow.AddSeconds(int.Parse(tokenResponse["expires_in"])).ToString("o"));

                    if (tokenResponse.TryGetValue("refresh_token", out var newRefreshToken))
                    {
                        authProperties.UpdateTokenValue("refresh_token", newRefreshToken);
                    }
                }
            }
            else
            {
                // Handle failure to refresh token (e.g., log and redirect to login)
            }
        }
    }
};
