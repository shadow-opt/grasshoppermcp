using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using grasshoppermcp.Tools;
using grasshoppermcp.Resources;
using grasshoppermcp.Prompts;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rhino;

// 这个类是你组件中 new McpServer() 所引用的类。
// 所有的修复都在这里。
public class McpServer : IDisposable
{
    private CancellationTokenSource _cancellationTokenSource;
    private readonly HttpListener _httpListener = new();
    private ServiceProvider _serviceProvider;
    private Task _serverTask;

    public bool IsListening => _httpListener.IsListening;

    public void Start(string address)
    {


        if (IsListening)
        {
            Stop();
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        var clientToServerPipe = new Pipe();
        var serverToClientPipe = new Pipe();

        _httpListener.Prefixes.Clear();
        _httpListener.Prefixes.Add(address);
        _httpListener.Start();
        RhinoApp.WriteLine($"MCP Server started at {address}");

        //手动配置，然后注册
        var services = new ServiceCollection();

        // 直接注册不需要 McpServerOptions 和 ServerInfo
        services.AddMcpServer()
            .WithStreamServerTransport(clientToServerPipe.Reader.AsStream(), serverToClientPipe.Writer.AsStream())
            .WithTools<GrasshopperTools>()
            .WithResources<GrasshopperResources>()
            .WithPrompts<GrasshopperPrompts>();

        // 构建服务容器
        _serviceProvider = services.BuildServiceProvider();

        // 启动后台服务
        _serverTask = Task.Run(async () =>
        {
            try
            {
                var mcpServerCore = _serviceProvider.GetRequiredService<IMcpServer>();
                var networkTask = HandleHttpNetworkAsync(_httpListener, serverToClientPipe.Reader, clientToServerPipe.Writer, token);
                var mcpTask = mcpServerCore.RunAsync(token);
                await Task.WhenAny(networkTask, mcpTask);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                RhinoApp.WriteLine($"[FATAL] Server run failed: {ex.Message}");
            }
        }, token);
    }

    /// <summary>
    /// 【超时问题的最终修复】
    /// 这个网络处理方法现在使用 StreamReader 逐行读取请求。
    /// JSON-RPC 消息以换行符分隔，这样可以正确处理每个消息，而不会等待一个永不结束的流。
    /// </summary>
    private async Task HandleHttpNetworkAsync(HttpListener listener, PipeReader responseReader, PipeWriter requestWriter, CancellationToken token)
    {
        while (!token.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext context = null;
            try
            {
                context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                if (request.HttpMethod == "POST" && request.HasEntityBody)
                {
                    using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    {
                        for (string line = await reader.ReadLineAsync(); line != null; line = await reader.ReadLineAsync())
                        {
                            if (string.IsNullOrEmpty(line)) continue;

                            await requestWriter.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"), token);
                            await requestWriter.FlushAsync(token);

                            var result = await responseReader.ReadAsync(token);
                            var buffer = result.Buffer;

                            if (!buffer.IsEmpty)
                            {
                                response.ContentType = "application/json";
                                foreach (var segment in buffer)
                                {
                                    await response.OutputStream.WriteAsync(segment.ToArray(), 0, segment.Length, token);
                                }
                                responseReader.AdvanceTo(buffer.End);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is HttpListenerException || ex is OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"[ERROR] Network handler failed: {ex.Message}");
            }
            finally
            {
                context?.Response.Close();
            }
        }
    }

    /// <summary>
    /// 【Dispose问题的最终修复】
    /// 这是一个同步的 Stop 方法，可以被你的组件直接调用。
    /// 它在内部以一种安全的方式（阻塞等待）来调用异步的清理逻辑。
    /// </summary>
    public void Stop()
    {
        // 触发取消信号
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
        }

        // 关闭监听器
        _httpListener?.Stop();

        try
        {
            // 等待后台任务结束
            _serverTask?.Wait(500);
        }
        catch (AggregateException) { /* 忽略取消任务时抛出的异常 */ }

        // 关键：以同步方式调用异步的 Dispose
        _serviceProvider?.Dispose(); // 对于 IServiceProvider，同步 Dispose 通常是安全的

        RhinoApp.WriteLine("MCP Server stopped.");
    }

    // 实现 IDisposable 接口，确保 Stop 被调用
    public void Dispose()
    {
        Stop();
    }
}