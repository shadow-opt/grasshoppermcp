using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rhino;


public class McpServer: IDisposable
{
    private readonly HttpListener _httpListener = new();
    private CancellationTokenSource _cancellationTokenSource;
    public bool IsListening => _httpListener.IsListening;

    public Pipe ClientToServerPipe { get; } = new();
    public Pipe ServerToClientPipe { get; } = new();

    public void Start(string endpoint)
    {
        _cancellationTokenSource = new();
        var token = _cancellationTokenSource.Token;

        //var endpoint = $"http://localhost:{port}/mcp";

        _httpListener.Prefixes.Clear();
        _httpListener.Prefixes.Add(endpoint);
        _httpListener.Start();
        RhinoApp.WriteLine($"MCP Server started at {endpoint}");
        // 启动两个并行的后台任务
        // 1. HTTP 请求处理循环
        Task.Run(() => HandleHttpRequestsAsync(token), token);
        // 2. 内部逻辑处理循环 (目前是模拟的回声逻辑)
        Task.Run(() => StartProcessingLoop(token), token);
    }
    /// <summary>
    /// 模拟的内部逻辑处理循环。
    /// </summary>
    private async Task StartProcessingLoop(CancellationToken token)
    {
        RhinoApp.WriteLine("Internal processing loop started.");
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 1. 从“客户端到服务器”的管道中读取数据
                ReadResult result = await ClientToServerPipe.Reader.ReadAsync(token);
                var buffer = result.Buffer;

                if (buffer.IsEmpty && result.IsCompleted)
                {
                    break;
                }

                // 2. 【修复点】遍历 buffer 中的每一个内存段
                if (!buffer.IsEmpty)
                {
                    foreach (var segment in buffer)
                    {
                        // 将每一个连续的内存段 (segment) 分别写入目标管道
                        await ServerToClientPipe.Writer.WriteAsync(segment, token);
                    }
                }

                // 3. 告知管道，我们已经处理了这部分数据
                ClientToServerPipe.Reader.AdvanceTo(buffer.End);
            }
        }
        catch (OperationCanceledException)
        {
            RhinoApp.WriteLine("Processing loop was cancelled.");
        }
        finally
        {
            await ClientToServerPipe.Writer.CompleteAsync();
            await ServerToClientPipe.Writer.CompleteAsync();
            RhinoApp.WriteLine("Internal processing loop stopped.");
        }
    }
    /// <summary>
    /// 处理外部 HTTP 请求的循环。
    /// </summary>
    //private async Task HandleHttpRequestsAsync(CancellationToken token)
    //{
    //    try
    //    {
    //        while (!token.IsCancellationRequested)
    //        {
    //            var context = await _httpListener.GetContextAsync();
    //            var request = context.Request;
    //            var response = context.Response;

    //            if (request.HttpMethod == "POST")
    //            {
    //                // a. 从请求流中读取数据，并写入 ClientToServerPipe
    //                //    这里我们直接将请求的输入流“泵”入管道，效率更高
    //                await request.InputStream.CopyToAsync(ClientToServerPipe.Writer, token);
    //                RhinoApp.WriteLine($"Received {request.ContentLength64} bytes from client.");

    //                // b. 从 ServerToClientPipe 中读取处理结果
    //                ReadResult result = await ServerToClientPipe.Reader.ReadAsync(token);
    //                var responseBuffer = result.Buffer;

    //                // c. 将处理结果写回给客户端
    //                response.ContentType = "application/json";
    //                response.ContentLength64 = responseBuffer.Length;

    //                // 将 Buffer 中的数据写到 HTTP 响应流中
    //                foreach (var segment in responseBuffer)
    //                {
    //                    await response.OutputStream.WriteAsync(segment.ToArray(), 0, segment.Length, token);
    //                }

    //                // 告知管道数据已被消费
    //                ServerToClientPipe.Reader.AdvanceTo(responseBuffer.End);
    //            }
    //            else
    //            {
    //                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
    //            }

    //            response.Close();
    //        }
    //    }
    //    catch (HttpListenerException ex) when (ex.ErrorCode == 995)
    //    {
    //        RhinoApp.WriteLine("HTTP listener was gracefully stopped.");
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        RhinoApp.WriteLine("Request handling was gracefully cancelled.");
    //    }
    //    catch (Exception ex)
    //    {
    //        RhinoApp.WriteLine($"[ERROR] An error occurred in request handler: {ex.Message}");
    //    }
    //}
    public void Stop()
    {
        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
        {
            // 发出取消信号，这将使 HandleHttpRequestsAsync 循环停止
            _cancellationTokenSource.Cancel();
            // 关闭 HttpListener 以立即停止监听新请求
            _httpListener.Stop();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            RhinoApp.WriteLine("MCP Server stopped.");
        }
    }



    private async Task HandleHttpRequestsAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 【修复点】使用 Task.WhenAny 实现可取消的等待
                var getContextTask = _httpListener.GetContextAsync();
                var cancellationTask = Task.Delay(Timeout.Infinite, token);

                // 等待 GetContextAsync 或者取消信号，看谁先来
                var completedTask = await Task.WhenAny(getContextTask, cancellationTask);

                // 如果是取消信号先来，就抛出异常终止循环
                if (completedTask == cancellationTask)
                {
                    throw new OperationCanceledException();
                }

                // 如果是 GetContextAsync 先完成，就安全地获取结果
                var context = await getContextTask;
                var request = context.Request;
                var response = context.Response;

                if (request.HttpMethod == "POST" && request.HasEntityBody)
                {
                    // 1. 根据 Content-Length 创建一个缓冲区来接收数据
                    var buffer = new byte[(int)request.ContentLength64];
                    // 2. 从请求流中精确读取所有数据
                    await request.InputStream.ReadAsync(buffer, 0, buffer.Length, token);

                    // 3. 将接收到的数据写入管道
                    await ClientToServerPipe.Writer.WriteAsync(buffer, token);
                    await ClientToServerPipe.Writer.FlushAsync(token); // 确保数据立即可用

                    RhinoApp.WriteLine($"Received {request.ContentLength64} bytes from client.");

                    // 4. 等待内部逻辑处理完成，并从响应管道中读取结果
                    ReadResult result = await ServerToClientPipe.Reader.ReadAsync(token);
                    var responseBuffer = result.Buffer;

                    // 5. 将结果作为响应发送回客户端
                    response.ContentType = "application/json";
                    response.ContentLength64 = responseBuffer.Length;
                    foreach (var segment in responseBuffer)
                    {
                        await response.OutputStream.WriteAsync(segment.ToArray(), 0, segment.Length, token);
                    }

                    // 6. 告知管道，响应数据已被消费
                    ServerToClientPipe.Reader.AdvanceTo(responseBuffer.End);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }

                response.Close();
            }
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 995)
        {
            RhinoApp.WriteLine("HTTP listener was gracefully stopped.");
        }
        catch (OperationCanceledException)
        {
            // 这是我们期望的、当服务器停止时的正常退出路径
            RhinoApp.WriteLine("Request handling was gracefully cancelled.");
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[ERROR] An error occurred in request handler: {ex.Message}");
        }
    }

    // 实现 IDisposable 接口，确保资源被正确释放
    public void Dispose()
    {
        Stop();
    }
}