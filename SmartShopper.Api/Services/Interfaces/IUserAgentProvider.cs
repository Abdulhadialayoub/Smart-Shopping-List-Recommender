namespace SmartShopper.Api.Services;

public interface IUserAgentProvider
{
    string GetRandomUserAgent();
    string GetSecChUaHeader(string userAgent);
}
