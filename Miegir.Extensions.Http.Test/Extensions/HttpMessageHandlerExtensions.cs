namespace Microsoft.Extensions.Http.Extensions;

internal static class HttpMessageHandlerExtensions
{
    public static THandler GetInnerHandlerOfType<THandler>(this HttpMessageHandler handler) where THandler: HttpMessageHandler
    {
        while (true)
        {
            switch (handler)
            {
                case THandler result:
                    return result;

                case DelegatingHandler { InnerHandler: HttpMessageHandler inner }:
                    handler = inner;
                    continue;

                default:
                    throw new AssertFailedException($"HttpMessageHandler expected to contain inner handler of type {typeof(THandler).Name}.");
            }
        }
    }
}
