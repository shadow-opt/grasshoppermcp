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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Rhino;

// 这个类是你组件中 new McpServer() 所引用的类。
// 所有的修复都在这里。
public class McpServer : IDisposable, IAsyncDisposable
{
    private CancellationTokenSource _cancellationTokenSource;
    private readonly HttpListener _httpListener = new();
    private ServiceProvider _serviceProvider;
    private Task _serverTask;
    private readonly SemaphoreSlim _requestSemaphore = new SemaphoreSlim(1, 1); // 添加信号量来控制并发
    private bool _disposed = false;

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
    /// 【并发处理修复】
    /// 使用信号量来确保请求按顺序处理，避免管道冲突。
    /// </summary>
    private async Task HandleHttpNetworkAsync(HttpListener listener, PipeReader responseReader, PipeWriter requestWriter, CancellationToken token)
    {
        while (!token.IsCancellationRequested && listener.IsListening)
        {
            try
            {
                var context = await listener.GetContextAsync();

                // 为每个请求创建独立的处理任务
                _ = Task.Run(async () => await HandleSingleRequestAsync(context, responseReader, requestWriter, token), token);
            }
            catch (Exception ex) when (ex is HttpListenerException || ex is OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"[ERROR] Network handler failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处理单个HTTP请求（使用信号量确保顺序处理）
    /// </summary>
    private async Task HandleSingleRequestAsync(HttpListenerContext context, PipeReader responseReader, PipeWriter requestWriter, CancellationToken token)
    {
        // 使用信号量确保同时只有一个请求在处理
        await _requestSemaphore.WaitAsync(token);

        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST" && request.HasEntityBody)
            {
                // 读取整个请求体
                string requestBody;
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (!string.IsNullOrEmpty(requestBody))
                {
                    // 将请求写入管道
                    var requestBytes = Encoding.UTF8.GetBytes(requestBody + "\n");
                    await requestWriter.WriteAsync(requestBytes, token);
                    await requestWriter.FlushAsync(token);

                    // 使用超时机制读取响应
                    using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token))
                    {
                        try
                        {
                            var result = await responseReader.ReadAsync(combinedCts.Token);
                            var buffer = result.Buffer;

                            if (!buffer.IsEmpty)
                            {
                                response.ContentType = "application/json";
                                response.StatusCode = 200;

                                // 写入响应数据
                                foreach (var segment in buffer)
                                {
                                    await response.OutputStream.WriteAsync(segment.ToArray(), 0, segment.Length, combinedCts.Token);
                                }

                                responseReader.AdvanceTo(buffer.End);
                            }
                            else
                            {
                                // 如果没有响应数据，返回空的JSON-RPC响应
                                await WriteErrorResponse(response, -32603, "No response received", null);
                            }
                        }
                        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                        {
                            // 处理超时
                            await WriteErrorResponse(response, -32603, "Request timeout", null);
                        }
                    }
                }
            }
            else
            {
                // 非POST请求或没有请求体
                response.StatusCode = 400;
                await WriteErrorResponse(response, -32600, "Invalid request", null);
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[ERROR] Single request handler failed: {ex.Message}");

            // 确保返回错误响应
            try
            {
                await WriteErrorResponse(context.Response, -32603, "Internal error", null);
            }
            catch
            {
                // 忽略在发送错误响应时的异常
            }
        }
        finally
        {
            _requestSemaphore.Release();

            try
            {
                context.Response.Close();
            }
            catch
            {
                // 忽略关闭响应时的异常
            }
        }
    }

    /// <summary>
    /// 写入错误响应
    /// </summary>
    private async Task WriteErrorResponse(HttpListenerResponse response, int code, string message, object id)
    {
        var errorResponse = new
        {
            jsonrpc = "2.0",
            error = new { code = code, message = message },
            id = id
        };

        var json = JsonSerializer.Serialize(errorResponse);
        var bytes = Encoding.UTF8.GetBytes(json);

        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// 【异步释放资源的修复】
    /// 异步释放方法，正确处理 IAsyncDisposable 资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            // 触发取消信号
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            // 关闭监听器
            _httpListener?.Stop();

            // 等待后台任务结束
            if (_serverTask != null)
            {
                try
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(1));
                }
                catch (TimeoutException)
                {
                    // 如果等待超时，强制取消
                    RhinoApp.WriteLine("MCP Server task did not complete within timeout, forcing cancellation.");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Error waiting for server task: {ex.Message}");
                }
            }

            // 异步释放服务提供者
            if (_serviceProvider != null)
            {
                if (_serviceProvider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    _serviceProvider.Dispose();
                }
            }

            // 释放信号量
            _requestSemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();

            RhinoApp.WriteLine("MCP Server stopped.");
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error during async disposal: {ex.Message}");
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// 【异步停止方法】
    /// 异步停止服务器，避免阻塞主线程
    /// </summary>
    public async Task StopAsync()
    {
        if (_disposed)
            return;

        try
        {
            await DisposeAsync();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error during async stop: {ex.Message}");
        }
    }

    /// <summary>
    /// 【同步释放方法修复】
    /// 同步的 Stop 方法，内部调用异步释放
    /// </summary>
    public void Stop()
    {
        if (_disposed)
            return;

        try
        {
            // 使用 GetAwaiter().GetResult() 来同步等待异步操作
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error during synchronous stop: {ex.Message}");
        }
    }

    /// <summary>
    /// 同步 Dispose 方法
    /// </summary>
    public void Dispose()
    {
        Stop();
    }
}