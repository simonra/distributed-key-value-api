using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

public static class OidcAuthSetupService
{
    // public static WebApplicationBuilder
    // public static IServiceCollection
    public static IServiceCollection AddOidcAuth(this IServiceCollection appBuilderServices)
    {
        appBuilderServices.AddAuthentication().AddJwtBearer("OurBearerScheme", options =>
        {
            var backendIdpUrl =
                Environment.GetEnvironmentVariable(
                    OIDC_IDP_ADDRESS_FOR_SERVER); // "http://keycloak:8088/realms/lokalmaskin"
            var clientIdpUrl =
                Environment.GetEnvironmentVariable(
                    OIDC_IDP_ADDRESS_FOR_USERS); // "http://localhost:8088/realms/lokalmaskin"
            options.Configuration = new()
            {
                Issuer = backendIdpUrl,
                AuthorizationEndpoint = $"{clientIdpUrl}/protocol/openid-connect/auth",
                TokenEndpoint = $"{backendIdpUrl}/protocol/openid-connect/token",
                JwksUri = $"{backendIdpUrl}/protocol/openid-connect/certs",
                JsonWebKeySet = FetchJwks(GetHttpClient(), $"{backendIdpUrl}/protocol/openid-connect/certs"),
                EndSessionEndpoint = $"{clientIdpUrl}/protocol/openid-connect/logout",
            };
            Console.WriteLine("Jwks: " + options.Configuration.JsonWebKeySet);
            foreach (var key in options.Configuration.JsonWebKeySet.GetSigningKeys())
            {
                options.Configuration.SigningKeys.Add(key);
                Console.WriteLine("Added SigningKey: " + key.KeyId);
            }

            options.TokenValidationParameters.ValidIssuers = [clientIdpUrl, backendIdpUrl];
            options.TokenValidationParameters.NameClaimType = "name"; // This is what populates @context.User.Identity?.Name
            options.TokenValidationParameters.RoleClaimType = "role";
            options.RequireHttpsMetadata =
                Environment.GetEnvironmentVariable(OIDC_REQUIRE_HTTPS_METADATA) != "false"; // disable only in dev env
            options.MapInboundClaims = true;
            options.Audience = Environment.GetEnvironmentVariable(OIDC_AUDIENCE);
        });
        appBuilderServices.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        return appBuilderServices;
    }

    private static JsonWebKeySet FetchJwks(HttpClient httpClient, string url)
    {
        var result = httpClient.GetAsync(url).Result;
        if (!result.IsSuccessStatusCode || result.Content is null)
        {
            throw new Exception(
                $"Getting token issuers (Keycloaks) JWKS from {url} failed. Status code {result.StatusCode}");
        }

        var jwks = result.Content.ReadAsStringAsync().Result;
        return new JsonWebKeySet(jwks);
    }

    private static HttpClient GetHttpClient()
    {
        if (Environment.GetEnvironmentVariable(OIDC_HTTPCLIENT_VALIDATE_EXTERNAL_CERTIFICATES) == "false")
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            return new HttpClient(handler);
        }
        return new HttpClient();
    }
}
