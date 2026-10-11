using System.Net.Http.Headers;

namespace FUNews.Client.DataAccess.Clients;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenProvider? _tokenProvider;

    public AuthHeaderHandler(ITokenProvider? tokenProvider = null)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_tokenProvider != null)
        {
            var token = _tokenProvider.GetToken();
            if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization == null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
