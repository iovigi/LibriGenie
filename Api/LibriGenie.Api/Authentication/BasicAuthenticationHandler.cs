using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System.Net.Http.Headers;
using System.Net;
using LibriGenie.Api.Configuration;

namespace LibriGenie.Api.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppSettings settings;

    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, AppSettings settings)
        : base(options, logger, encoder)
    {
        this.settings = settings;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.ContainsKey("Authorization"))
        {
            Logger.LogInformation("Auth header is missing");

            return AuthenticateResult.Fail(HttpStatusCode.Unauthorized.ToString());
        }

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(Context.Request.Headers["Authorization"]);
            if (authHeader.Scheme != "Basic")
            {
                return AuthenticateResult.Fail(HttpStatusCode.Unauthorized.ToString());
            }

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter!);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
            var username = credentials[0];
            var password = credentials[1];

            if (username != settings.BasicAuthentication.Username
                || password != settings.BasicAuthentication.Password)
            {
                return AuthenticateResult.Fail(HttpStatusCode.Unauthorized.ToString());
            }

            var claims = new Claim[]
            {
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", username),
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            await Task.CompletedTask;
            return AuthenticateResult.Success(ticket);
        }
        catch
        {
            return AuthenticateResult.Fail(HttpStatusCode.Unauthorized.ToString());
        }
    }
}
