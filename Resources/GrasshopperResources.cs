using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace grasshoppermcp.Resources
{
    /// <summary>
    /// Grasshopper MCP 资源集
    /// </summary>
    [McpServerResourceType]
    public class GrasshopperResources
    {
        /// <summary>
        /// 获取 Grasshopper 状态信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>状态信息</returns>
        [McpServerResource(UriTemplate = "grasshopper://status")]
        [Description("获取当前 Grasshopper 状态")]
        public static Task<string> GetStatus(CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                var status = new
                {
                    IsDocumentActive = document != null,
                    DocumentName = document?.DisplayName ?? "无文档",
                    ComponentCount = document?.ObjectCount ?? 0,
                    GrasshopperVersion = Grasshopper.Versioning.Version,
                    RhinoVersion = Rhino.RhinoApp.Version
                };

                return Task.FromResult(JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取组件指南
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>组件指南</returns>
        [McpServerResource(UriTemplate = "grasshopper://component_guide")]
        [Description("获取 Grasshopper 组件使用指南")]
        public static Task<string> GetComponentGuide(CancellationToken cancellationToken = default)
        {
            try
            {
                var guide = new
                {
                    Title = "Grasshopper 组件使用指南",
                    Categories = new[]
                    {
                        new { Name = "参数", Description = "输入参数和滑块" },
                        new { Name = "几何", Description = "点、线、面、体等基本几何体" },
                        new { Name = "曲线", Description = "曲线创建和操作" },
                        new { Name = "表面", Description = "表面生成和修改" },
                        new { Name = "网格", Description = "网格处理和操作" },
                        new { Name = "变换", Description = "移动、旋转、缩放等变换" },
                        new { Name = "数学", Description = "数学运算和函数" },
                        new { Name = "逻辑", Description = "条件判断和流程控制" }
                    },
                    Usage = "使用 add_component 工具添加组件，使用 connect_components 工具连接组件"
                };

                return Task.FromResult(JsonSerializer.Serialize(guide, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取组件库
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>组件库信息</returns>
        [McpServerResource(UriTemplate = "grasshopper://component_library")]
        [Description("获取可用的 Grasshopper 组件库")]
        public static Task<string> GetComponentLibrary(CancellationToken cancellationToken = default)
        {
            try
            {
                var library = new
                {
                    Title = "Grasshopper 组件库",
                    BasicComponents = new[]
                    {
                        new { Name = "Point", Type = "point", Description = "创建点" },
                        new { Name = "Line", Type = "line", Description = "创建直线" },
                        new { Name = "Circle", Type = "circle", Description = "创建圆" },
                        new { Name = "Curve", Type = "curve", Description = "创建曲线" },
                        new { Name = "Panel", Type = "panel", Description = "文本面板" },
                        new { Name = "Slider", Type = "slider", Description = "数值滑块" }
                    },
                    AdvancedComponents = new[]
                    {
                        new { Name = "Voronoi", Type = "voronoi", Description = "Voronoi 图" },
                        new { Name = "Delaunay", Type = "delaunay", Description = "Delaunay 三角剖分" },
                        new { Name = "Mesh", Type = "mesh", Description = "网格处理" },
                        new { Name = "Surface", Type = "surface", Description = "表面生成" }
                    },
                    Usage = "使用组件的 'type' 值作为 add_component 工具的 component_type 参数"
                };

                return Task.FromResult(JsonSerializer.Serialize(library, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取环境信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>环境信息</returns>
        [McpServerResource(UriTemplate = "grasshopper://environment")]
        [Description("获取 Grasshopper 环境信息")]
        public static Task<string> GetEnvironment(CancellationToken cancellationToken = default)
        {
            try
            {
                var environment = new
                {
                    Title = "Grasshopper 环境信息",
                    RhinoVersion = Rhino.RhinoApp.Version.ToString(),
                    GrasshopperVersion = Grasshopper.Versioning.Version.ToString(),
                    Platform = Environment.OSVersion.Platform.ToString(),
                    MachineName = Environment.MachineName,
                    UserName = Environment.UserName,
                    WorkingDirectory = Environment.CurrentDirectory,
                    SystemInfo = new
                    {
                        ProcessorCount = Environment.ProcessorCount,
                        SystemPageSize = Environment.SystemPageSize,
                        WorkingSet = Environment.WorkingSet,
                        Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                        Is64BitProcess = Environment.Is64BitProcess
                    }
                };

                return Task.FromResult(JsonSerializer.Serialize(environment, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前 Grasshopper 文档的详细信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>当前文档的详细信息</returns>
        [McpServerResource(UriTemplate = "grasshopper://current_document")]
        [Description("获取当前 Grasshopper 文档的详细信息，包括所有组件、位置、参数和连接")]
        public static Task<string> GetCurrentDocument(CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult(JsonSerializer.Serialize(new
                    {
                        error = "没有活动的 Grasshopper 文档",
                        components = new object[0],
                        connections = new object[0]
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }

                var components = new List<object>();
                var connections = new List<object>();

                // 遍历所有组件
                foreach (var obj in document.Objects)
                {
                    if (obj is IGH_Component component)
                    {
                        var inputs = new List<object>();
                        var outputs = new List<object>();

                        // 收集输入参数
                        foreach (var param in component.Params.Input)
                        {
                            inputs.Add(new
                            {
                                name = param.Name,
                                nickname = param.NickName,
                                type = param.Type.Name,
                                isConnected = param.Sources.Count > 0,
                                sourceCount = param.Sources.Count,
                                description = param.Description
                            });
                        }

                        // 收集输出参数
                        foreach (var param in component.Params.Output)
                        {
                            outputs.Add(new
                            {
                                name = param.Name,
                                nickname = param.NickName,
                                type = param.Type.Name,
                                isConnected = param.Recipients.Count > 0,
                                recipientCount = param.Recipients.Count,
                                description = param.Description
                            });
                        }

                        components.Add(new
                        {
                            id = component.InstanceGuid.ToString(),
                            name = component.Name,
                            nickname = component.NickName,
                            category = component.Category,
                            subcategory = component.SubCategory,
                            position = new
                            {
                                x = component.Attributes.Pivot.X,
                                y = component.Attributes.Pivot.Y
                            },
                            inputs,
                            outputs,
                            description = component.Description
                        });

                        // 收集连接信息
                        foreach (var output in component.Params.Output)
                        {
                            foreach (var recipient in output.Recipients)
                            {
                                connections.Add(new
                                {
                                    sourceId = component.InstanceGuid.ToString(),
                                    sourceParam = output.Name,
                                    sourceParamNickname = output.NickName,
                                    targetId = recipient.Attributes.GetTopLevel.DocObject.InstanceGuid.ToString(),
                                    targetParam = recipient.Name,
                                    targetParamNickname = recipient.NickName
                                });
                            }
                        }
                    }
                    else if (obj is IGH_Param param)
                    {
                        // 处理参数组件（如滑块、面板等）
                        var outputs = new List<object>();

                        // 对于参数，它们通常只有一个输出
                        outputs.Add(new
                        {
                            name = param.Name,
                            nickname = param.NickName,
                            type = param.Type.Name,
                            isConnected = param.Recipients.Count > 0,
                            recipientCount = param.Recipients.Count,
                            description = param.Description
                        });

                        components.Add(new
                        {
                            id = param.InstanceGuid.ToString(),
                            name = param.Name,
                            nickname = param.NickName,
                            category = param.Category,
                            subcategory = param.SubCategory,
                            position = new
                            {
                                x = param.Attributes.Pivot.X,
                                y = param.Attributes.Pivot.Y
                            },
                            inputs = new object[0],
                            outputs,
                            description = param.Description,
                            isParameter = true
                        });

                        // 收集参数的连接信息
                        foreach (var recipient in param.Recipients)
                        {
                            connections.Add(new
                            {
                                sourceId = param.InstanceGuid.ToString(),
                                sourceParam = param.Name,
                                sourceParamNickname = param.NickName,
                                targetId = recipient.Attributes.GetTopLevel.DocObject.InstanceGuid.ToString(),
                                targetParam = recipient.Name,
                                targetParamNickname = recipient.NickName
                            });
                        }
                    }
                }

                var documentInfo = new
                {
                    documentName = document.DisplayName,
                    componentCount = components.Count,
                    connectionCount = connections.Count,
                    components,
                    connections,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                return Task.FromResult(JsonSerializer.Serialize(documentInfo, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取故障排除指南
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>故障排除指南</returns>
        [McpServerResource(UriTemplate = "grasshopper://troubleshooting")]
        [Description("获取 Grasshopper MCP 故障排除指南")]
        public static Task<string> GetTroubleshootingGuide(CancellationToken cancellationToken = default)
        {
            try
            {
                var guide = new
                {
                    Title = "Grasshopper MCP 故障排除指南",
                    CommonIssues = new[]
                    {
                        new {
                            Issue = "add_component 失败",
                            Solution = "请先运行 diagnose_environment 检查环境，然后尝试 test_simple_add"
                        },
                        new {
                            Issue = "Object reference not set to an instance of an object",
                            Solution = "确保 Grasshopper 已启动且有活动文档，检查 ActiveCanvas 和 Document 状态"
                        },
                        new {
                            Issue = "组件未显示在画布上",
                            Solution = "检查组件位置是否在可见区域，尝试缩放画布或使用 get_document_info 确认组件已添加"
                        },
                        new {
                            Issue = "连接组件失败",
                            Solution = "确保源组件和目标组件都存在，使用正确的组件ID或参数名称"
                        }
                    },
                    TestingSteps = new[]
                    {
                        "1. 运行 diagnose_environment 检查环境",
                        "2. 运行 test_simple_add 进行基础测试",
                        "3. 尝试 add_component 添加简单组件",
                        "4. 使用 get_document_info 确认组件已添加",
                        "5. 尝试 create_pattern 创建复杂模式"
                    },
                    SupportedComponents = new[]
                    {
                        "point", "curve", "surface", "mesh", "number", "integer",
                        "boolean", "text", "panel", "slider", "vector", "plane",
                        "color", "interval", "matrix", "transform", "geometry", "brep"
                    }
                };

                return Task.FromResult(JsonSerializer.Serialize(guide, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }
    }
}