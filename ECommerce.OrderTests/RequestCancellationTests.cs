using ECommerce.API.Middlewares;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ECommerce.OrderTests;

public sealed class RequestCancellationTests
{
    [Fact]
    public async Task Request_aborted_operation_is_not_converted_to_http_500_or_written_to()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var context = new DefaultHttpContext
        {
            RequestAborted = cancellation.Token
        };
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(cancellation.Token),
            loggerFactory.CreateLogger<GlobalExceptionHandlingMiddleware>(),
            new TestHostEnvironment());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task Non_request_cancellation_is_deliberately_mapped_to_408()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var context = new DefaultHttpContext
        {
            RequestAborted = CancellationToken.None
        };
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException("database timeout"),
            loggerFactory.CreateLogger<GlobalExceptionHandlingMiddleware>(),
            new TestHostEnvironment());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status408RequestTimeout, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("408", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Aborted_request_does_not_write_a_generic_error_response()
    {
        using var loggerFactory = LoggerFactory.Create(_ => { });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var context = new DefaultHttpContext
        {
            RequestAborted = cancellation.Token
        };
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("client disconnected"),
            loggerFactory.CreateLogger<GlobalExceptionHandlingMiddleware>(),
            new TestHostEnvironment());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "ECommerce.OrderTests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
