using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace grasshoppermcp.Tools
{
    /// <summary>
    /// Grasshopper MCP 工具集
    /// </summary>
    [McpServerToolType]
    public class GrasshopperTools
    {
        // 存储组件 ID 映射
        private static readonly Dictionary<string, Guid> _componentMap = new Dictionary<string, Guid>();
        
        /// <summary>
        /// 在 Grasshopper 画布上添加组件
        /// </summary>
        /// <param name="component_type">组件类型</param>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        [McpServerTool(Name = "add_component")]
        [Description("在 Grasshopper 画布上添加组件")]
        public static Task<string> AddComponent(
            [Description("组件类型（point, curve, circle, line, panel, slider）")] string component_type,
            [Description("X 坐标")] double x,
            [Description("Y 坐标")] double y,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 获取当前的 Grasshopper 文档
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档");
                }

                // 根据组件类型创建相应的组件
                IGH_DocumentObject component = CreateComponentByType(component_type);
                if (component == null)
                {
                    return Task.FromResult($"错误：不支持的组件类型 '{component_type}'");
                }

                // 设置组件位置
                component.Attributes.Pivot = new System.Drawing.PointF((float)x, (float)y);

                // 添加到文档
                document.AddObject(component, false);
                
                // 生成唯一 ID 并存储映射
                string componentId = Guid.NewGuid().ToString();
                _componentMap[componentId] = component.InstanceGuid;

                // 刷新画布
                Grasshopper.Instances.ActiveCanvas?.Refresh();

                return Task.FromResult($"成功：在位置 ({x}, {y}) 添加了 {component_type} 组件，ID: {componentId}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据组件类型创建组件实例
        /// </summary>
        private static IGH_DocumentObject CreateComponentByType(string componentType)
        {
            switch (componentType.ToLowerInvariant())
            {
                case "point":
                    return CreateComponent("Parameters", "Geometry", "Point", "Pt");
                case "curve":
                    return CreateComponent("Parameters", "Geometry", "Curve", "Crv");
                case "circle":
                    return CreateComponent("Curve", "Primitive", "Circle", "Cir");
                case "line":
                    return CreateComponent("Curve", "Primitive", "Line", "Ln");
                case "panel":
                    return CreateComponent("Params", "Input", "Panel", "Panel");
                case "slider":
                    return CreateComponent("Params", "Input", "Number Slider", "Slider");
                // 添加更多组件类型
                case "rectangle":
                    return CreateComponent("Curve", "Primitive", "Rectangle", "Rec");
                case "surface":
                    return CreateComponent("Parameters", "Geometry", "Surface", "Srf");
                case "mesh":
                    return CreateComponent("Parameters", "Geometry", "Mesh", "M");
                case "vector":
                    return CreateComponent("Parameters", "Geometry", "Vector", "V");
                case "integer":
                    return CreateComponent("Params", "Input", "Integer", "I");
                case "boolean":
                    return CreateComponent("Params", "Input", "Boolean", "B");
                case "color":
                    return CreateComponent("Params", "Input", "Colour", "C");
                case "domain":
                    return CreateComponent("Params", "Input", "Domain", "D");
                case "plane":
                    return CreateComponent("Parameters", "Geometry", "Plane", "Pl");
                case "box":
                    return CreateComponent("Surface", "Primitive", "Box", "Box");
                case "sphere":
                    return CreateComponent("Surface", "Primitive", "Sphere", "Sph");
                case "cylinder":
                    return CreateComponent("Surface", "Primitive", "Cylinder", "Cyl");
                default:
                    return null;
            }
        }

        /// <summary>
        /// 通过类别和子类别创建组件
        /// </summary>
        private static IGH_DocumentObject CreateComponent(string category, string subcategory, string name, string nickname)
        {
            try
            {
                // 尝试从组件服务器获取组件
                var componentServer = Grasshopper.Instances.ComponentServer;
                var componentProxy = componentServer.ObjectProxies.FirstOrDefault(p => 
                    p.Desc.Category == category && 
                    p.Desc.SubCategory == subcategory && 
                    (p.Desc.Name == name || p.Desc.NickName == nickname));

                if (componentProxy != null)
                {
                    return componentProxy.CreateInstance();
                }

                // 如果找不到，尝试创建基础参数组件
                return CreateParameterComponent(name, nickname);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 创建基础参数组件
        /// </summary>
        private static IGH_Param CreateParameterComponent(string name, string nickname)
        {
            switch (name.ToLowerInvariant())
            {
                case "point":
                    return new Param_Point() { Name = name, NickName = nickname };
                case "curve":
                    return new Param_Curve() { Name = name, NickName = nickname };
                case "surface":
                    return new Param_Surface() { Name = name, NickName = nickname };
                case "mesh":
                    return new Param_Mesh() { Name = name, NickName = nickname };
                case "number":
                    return new Param_Number() { Name = name, NickName = nickname };
                case "integer":
                    return new Param_Integer() { Name = name, NickName = nickname };
                case "text":
                case "string":
                    return new Param_String() { Name = name, NickName = nickname };
                case "boolean":
                    return new Param_Boolean() { Name = name, NickName = nickname };
                case "vector":
                    return new Param_Vector() { Name = name, NickName = nickname };
                case "plane":
                    return new Param_Plane() { Name = name, NickName = nickname };
                case "color":
                case "colour":
                    return new Param_Colour() { Name = name, NickName = nickname };
                case "domain":
                    return new Param_Interval() { Name = name, NickName = nickname };
                case "interval":
                    return new Param_Interval() { Name = name, NickName = nickname };
                case "matrix":
                    return new Param_Matrix() { Name = name, NickName = nickname };
                case "transform":
                    return new Param_Transform() { Name = name, NickName = nickname };
                case "geometry":
                    return new Param_Geometry() { Name = name, NickName = nickname };
                case "brep":
                    return new Param_Brep() { Name = name, NickName = nickname };
                default:
                    return new Param_GenericObject() { Name = name, NickName = nickname };
            }
        }

        /// <summary>
        /// 连接两个组件
        /// </summary>
        /// <param name="source_id">源组件ID</param>
        /// <param name="target_id">目标组件ID</param>
        /// <param name="source_param">源参数名称</param>
        /// <param name="target_param">目标参数名称</param>
        /// <param name="source_param_index">源参数索引</param>
        /// <param name="target_param_index">目标参数索引</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        [McpServerTool(Name = "connect_components")]
        [Description("连接两个 Grasshopper 组件")]
        public static Task<string> ConnectComponents(
            [Description("源组件ID")] string source_id,
            [Description("目标组件ID")] string target_id,
            [Description("源参数名称（可选）")] string source_param = null,
            [Description("目标参数名称（可选）")] string target_param = null,
            [Description("源参数索引（可选）")] int? source_param_index = null,
            [Description("目标参数索引（可选）")] int? target_param_index = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档");
                }

                // 查找源组件和目标组件
                var sourceComponent = FindComponentById(source_id, document);
                var targetComponent = FindComponentById(target_id, document);

                if (sourceComponent == null)
                {
                    return Task.FromResult($"错误：找不到源组件 {source_id}");
                }

                if (targetComponent == null)
                {
                    return Task.FromResult($"错误：找不到目标组件 {target_id}");
                }

                // 获取源输出参数
                IGH_Param sourceOutput = GetOutputParameter(sourceComponent, source_param, source_param_index);
                if (sourceOutput == null)
                {
                    return Task.FromResult("错误：找不到有效的源输出参数");
                }

                // 获取目标输入参数
                IGH_Param targetInput = GetInputParameter(targetComponent, target_param, target_param_index);
                if (targetInput == null)
                {
                    return Task.FromResult("错误：找不到有效的目标输入参数");
                }

                // 建立连接
                targetInput.AddSource(sourceOutput);

                // 刷新画布
                Grasshopper.Instances.ActiveCanvas?.Refresh();

                return Task.FromResult($"成功：连接了组件 {source_id} 和 {target_id}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据 ID 查找组件
        /// </summary>
        private static IGH_DocumentObject FindComponentById(string componentId, GH_Document document)
        {
            // 如果是我们存储的 ID，先查找映射
            if (_componentMap.TryGetValue(componentId, out Guid instanceGuid))
            {
                return document.FindObject(instanceGuid, true);
            }

            // 尝试直接解析 GUID
            if (Guid.TryParse(componentId, out Guid guid))
            {
                return document.FindObject(guid, true);
            }

            return null;
        }

        /// <summary>
        /// 获取输出参数
        /// </summary>
        private static IGH_Param GetOutputParameter(IGH_DocumentObject component, string paramName, int? paramIndex)
        {
            if (component is IGH_Component ghComponent)
            {
                if (!string.IsNullOrEmpty(paramName))
                {
                    // 按名称查找
                    return ghComponent.Params.Output.FirstOrDefault(p => 
                        p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase) ||
                        p.NickName.Equals(paramName, StringComparison.OrdinalIgnoreCase));
                }
                else if (paramIndex.HasValue && paramIndex.Value >= 0 && paramIndex.Value < ghComponent.Params.Output.Count)
                {
                    // 按索引查找
                    return ghComponent.Params.Output[paramIndex.Value];
                }
                else if (ghComponent.Params.Output.Count > 0)
                {
                    // 默认使用第一个输出
                    return ghComponent.Params.Output[0];
                }
            }
            else if (component is IGH_Param param)
            {
                // 如果组件本身就是参数，返回它自己
                return param;
            }

            return null;
        }

        /// <summary>
        /// 获取输入参数
        /// </summary>
        private static IGH_Param GetInputParameter(IGH_DocumentObject component, string paramName, int? paramIndex)
        {
            if (component is IGH_Component ghComponent)
            {
                if (!string.IsNullOrEmpty(paramName))
                {
                    // 按名称查找
                    return ghComponent.Params.Input.FirstOrDefault(p => 
                        p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase) ||
                        p.NickName.Equals(paramName, StringComparison.OrdinalIgnoreCase));
                }
                else if (paramIndex.HasValue && paramIndex.Value >= 0 && paramIndex.Value < ghComponent.Params.Input.Count)
                {
                    // 按索引查找
                    return ghComponent.Params.Input[paramIndex.Value];
                }
                else if (ghComponent.Params.Input.Count > 0)
                {
                    // 默认使用第一个输入
                    return ghComponent.Params.Input[0];
                }
            }
            else if (component is IGH_Param param)
            {
                // 如果组件本身就是参数，返回它自己
                return param;
            }

            return null;
        }

        /// <summary>
        /// 清空 Grasshopper 文档
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        [McpServerTool(Name = "clear_document")]
        [Description("清空当前 Grasshopper 文档")]
        public static Task<string> ClearDocument(CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档");
                }

                // 获取所有对象并移除
                var allObjects = document.Objects.ToList();
                foreach (var obj in allObjects)
                {
                    document.RemoveObject(obj, false);
                }
                
                // 清空组件映射
                _componentMap.Clear();

                // 刷新画布
                Grasshopper.Instances.ActiveCanvas?.Refresh();

                return Task.FromResult($"成功：已清空 Grasshopper 文档，移除了 {allObjects.Count} 个对象");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取 Grasshopper 文档信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档信息</returns>
        [McpServerTool(Name = "get_document_info")]
        [Description("获取当前 Grasshopper 文档信息")]
        public static Task<string> GetDocumentInfo(CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档");
                }

                var info = new
                {
                    IsDocumentActive = document != null,
                    DocumentName = document.DisplayName ?? "未命名文档",
                    ComponentCount = document.ObjectCount,
                    Objects = document.Objects.Select(obj => new {
                        Id = obj.InstanceGuid.ToString(),
                        Name = obj.Name,
                        NickName = obj.NickName,
                        Type = obj.GetType().Name,
                        Category = obj.Category,
                        SubCategory = obj.SubCategory,
                        Position = obj.Attributes?.Pivot != null ? 
                            new { X = obj.Attributes.Pivot.X, Y = obj.Attributes.Pivot.Y } : null
                    }).ToList(),
                    GrasshopperVersion = typeof(Grasshopper.Instances).Assembly.GetName().Version?.ToString(),
                    RhinoVersion = typeof(Rhino.RhinoApp).Assembly.GetName().Version?.ToString()
                };

                return Task.FromResult(JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取可用的组件模式
        /// </summary>
        /// <param name="query">查询字符串</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>可用模式列表</returns>
        [McpServerTool(Name = "get_available_patterns")]
        [Description("获取可用的 Grasshopper 组件模式")]
        public static Task<string> GetAvailablePatterns(
            [Description("模式查询字符串")] string query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var patterns = new List<object>
                {
                    new { Name = "Basic Point", Description = "创建基本点组件" },
                    new { Name = "Line Segment", Description = "创建线段组件" },
                    new { Name = "Circle", Description = "创建圆形组件" },
                    new { Name = "Rectangle", Description = "创建矩形组件" },
                    new { Name = "Number Slider", Description = "创建数值滑块" },
                    new { Name = "Panel", Description = "创建文本面板" },
                    new { Name = "Point Grid", Description = "创建点阵模式" },
                    new { Name = "Curve Division", Description = "创建曲线分割模式" },
                    new { Name = "Voronoi Pattern", Description = "创建 Voronoi 图案" }
                };

                if (!string.IsNullOrEmpty(query))
                {
                    patterns = patterns.Where(p => 
                        ((dynamic)p).Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        ((dynamic)p).Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                return Task.FromResult(JsonSerializer.Serialize(patterns, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据描述创建组件模式
        /// </summary>
        /// <param name="description">模式描述</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建结果</returns>
        [McpServerTool(Name = "create_pattern")]
        [Description("根据高级描述创建 Grasshopper 组件模式")]
        public static Task<string> CreatePattern(
            [Description("模式的高级描述（如：'3D voronoi cube'）")] string description,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档");
                }

                // 根据描述分析并创建相应的模式
                var result = CreatePatternByDescription(description, document);
                
                // 刷新画布
                Grasshopper.Instances.ActiveCanvas?.Refresh();

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据描述创建模式
        /// </summary>
        private static string CreatePatternByDescription(string description, GH_Document document)
        {
            var desc = description.ToLowerInvariant();
            var createdComponents = new List<string>();

            try
            {
                if (desc.Contains("point") && desc.Contains("grid"))
                {
                    // 创建点阵模式
                    return CreatePointGridPattern(document);
                }
                else if (desc.Contains("line") || desc.Contains("连线"))
                {
                    // 创建线段模式
                    return CreateLinePattern(document);
                }
                else if (desc.Contains("circle") || desc.Contains("圆"))
                {
                    // 创建圆形模式
                    return CreateCirclePattern(document);
                }
                else if (desc.Contains("voronoi"))
                {
                    // 创建 Voronoi 模式
                    return CreateVoronoiPattern(document);
                }
                else
                {
                    // 默认创建基础模式
                    return CreateBasicPattern(document);
                }
            }
            catch (Exception ex)
            {
                return $"创建模式时出错：{ex.Message}";
            }
        }

        /// <summary>
        /// 创建点阵模式
        /// </summary>
        private static string CreatePointGridPattern(GH_Document document)
        {
            var components = new List<string>();

            // 创建数值滑块 (X 数量)
            var xSlider = CreateParameterComponent("number", "X Count");
            if (xSlider != null)
            {
                xSlider.Attributes.Pivot = new System.Drawing.PointF(50, 100);
                document.AddObject(xSlider, false);
                components.Add("X数量滑块");
            }

            // 创建数值滑块 (Y 数量)
            var ySlider = CreateParameterComponent("number", "Y Count");
            if (ySlider != null)
            {
                ySlider.Attributes.Pivot = new System.Drawing.PointF(50, 150);
                document.AddObject(ySlider, false);
                components.Add("Y数量滑块");
            }

            return $"成功创建点阵模式，包含组件：{string.Join(", ", components)}";
        }

        /// <summary>
        /// 创建线段模式
        /// </summary>
        private static string CreateLinePattern(GH_Document document)
        {
            var components = new List<string>();

            // 创建起点参数
            var startPoint = CreateParameterComponent("point", "Start");
            if (startPoint != null)
            {
                startPoint.Attributes.Pivot = new System.Drawing.PointF(50, 100);
                document.AddObject(startPoint, false);
                components.Add("起点");
            }

            // 创建终点参数
            var endPoint = CreateParameterComponent("point", "End");
            if (endPoint != null)
            {
                endPoint.Attributes.Pivot = new System.Drawing.PointF(50, 150);
                document.AddObject(endPoint, false);
                components.Add("终点");
            }

            return $"成功创建线段模式，包含组件：{string.Join(", ", components)}";
        }

        /// <summary>
        /// 创建圆形模式
        /// </summary>
        private static string CreateCirclePattern(GH_Document document)
        {
            var components = new List<string>();

            // 创建中心点参数
            var centerPoint = CreateParameterComponent("point", "Center");
            if (centerPoint != null)
            {
                centerPoint.Attributes.Pivot = new System.Drawing.PointF(50, 100);
                document.AddObject(centerPoint, false);
                components.Add("中心点");
            }

            // 创建半径滑块
            var radiusSlider = CreateParameterComponent("number", "Radius");
            if (radiusSlider != null)
            {
                radiusSlider.Attributes.Pivot = new System.Drawing.PointF(50, 150);
                document.AddObject(radiusSlider, false);
                components.Add("半径滑块");
            }

            return $"成功创建圆形模式，包含组件：{string.Join(", ", components)}";
        }

        /// <summary>
        /// 创建 Voronoi 模式
        /// </summary>
        private static string CreateVoronoiPattern(GH_Document document)
        {
            var components = new List<string>();

            // 创建点集参数
            var points = CreateParameterComponent("point", "Points");
            if (points != null)
            {
                points.Attributes.Pivot = new System.Drawing.PointF(50, 100);
                document.AddObject(points, false);
                components.Add("点集");
            }

            // 创建边界参数
            var boundary = CreateParameterComponent("curve", "Boundary");
            if (boundary != null)
            {
                boundary.Attributes.Pivot = new System.Drawing.PointF(50, 150);
                document.AddObject(boundary, false);
                components.Add("边界");
            }

            return $"成功创建 Voronoi 模式，包含组件：{string.Join(", ", components)}";
        }

        /// <summary>
        /// 创建基础模式
        /// </summary>
        private static string CreateBasicPattern(GH_Document document)
        {
            var components = new List<string>();

            // 创建基础参数组件
            var param = CreateParameterComponent("point", "Input");
            if (param != null)
            {
                param.Attributes.Pivot = new System.Drawing.PointF(100, 100);
                document.AddObject(param, false);
                components.Add("输入参数");
            }

            // 创建面板
            var panel = CreateParameterComponent("text", "Output");
            if (panel != null)
            {
                panel.Attributes.Pivot = new System.Drawing.PointF(300, 100);
                document.AddObject(panel, false);
                components.Add("输出面板");
            }

            return $"成功创建基础模式，包含组件：{string.Join(", ", components)}";
        }
    }
}
