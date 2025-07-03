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
using Grasshopper.Kernel.Special;
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
        [Description("基础工具 - 推荐优先使用。在 Grasshopper 画布上添加单个组件。这是最可靠的方法来创建组件。支持的组件类型：slider（数值滑块）、panel（文本面板）、point（点）、circle（圆）、line（直线）、curve（曲线）、rectangle（矩形）、box（长方体）等。成功后返回组件ID用于连接。对于复杂模式，建议多次调用此方法逐个添加组件，然后使用 connect_components 连接它们。")]
        public static Task<string> AddComponent(
            [Description("组件类型。常用类型：slider（数值滑块，用于提供数值输入）、panel（文本面板，用于显示信息）、point（点几何）、circle（圆几何）、line（直线几何）、curve（曲线几何）、voronoi（泰森多边形）、delaunay（德劳内三角剖分）")] string component_type,
            [Description("组件在画布上的X坐标位置（像素单位）")] double x,
            [Description("组件在画布上的Y坐标位置（像素单位）")] double y,
            [Description("（可选）为组件设置初始值。对于'slider'，是数字；对于'panel'，是字符串；对于'point'，是JSON '''{\"X\":10,\"Y\":20,\"Z\":0}'''；对于'plane'，是JSON '''{\"Origin\":{\"X\":0,\"Y\":0,\"Z\":0},\"Normal\":{\"X\":0,\"Y\":0,\"Z\":1}}'''。")] string value = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 详细检查 Grasshopper 环境
                if (Grasshopper.Instances.ActiveCanvas == null)
                {
                    return Task.FromResult("错误：Grasshopper 画布未初始化。请确保 Grasshopper 已正确启动。");
                }

                // 获取当前的 Grasshopper 文档
                var document = Grasshopper.Instances.ActiveCanvas.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档。请在 Grasshopper 中创建一个新文档。");
                }

                // 根据组件类型创建相应的组件
                IGH_DocumentObject component = CreateComponentByType(component_type);
                if (component == null)
                {
                    return Task.FromResult($"错误：不支持的组件类型 '{component_type}'。支持的类型包括：point, curve, circle, line, panel, slider, rectangle, surface, mesh, vector, integer, boolean, color, plane, box, sphere, cylinder");
                }

                // 确保组件有有效的属性
                if (component.Attributes == null)
                {
                    component.CreateAttributes();
                }

                // 设置组件位置
                component.Attributes.Pivot = new System.Drawing.PointF((float)x, (float)y);

                // 添加到文档
                document.AddObject(component, false);

                // 如果有初始值，则设置它
                if (!string.IsNullOrEmpty(value))
                {
                    SetComponentValue(component, value);
                }

                // 生成唯一 ID 并存储映射
                string componentId = Guid.NewGuid().ToString();
                _componentMap[componentId] = component.InstanceGuid;

                // 刷新画布
                Grasshopper.Instances.ActiveCanvas.Refresh();

                return Task.FromResult($"成功：在位置 ({x}, {y}) 添加了 {component_type} 组件，ID: {componentId}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}\n详细信息：{ex.StackTrace}");
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
                // 检查组件服务器是否可用
                var componentServer = Grasshopper.Instances.ComponentServer;
                if (componentServer == null)
                {
                    System.Diagnostics.Debug.WriteLine("组件服务器不可用");
                    return CreateParameterComponent(name, nickname);
                }

                // 尝试从组件服务器获取组件
                var componentProxy = componentServer.ObjectProxies.FirstOrDefault(p =>
                    p.Desc.Category == category &&
                    p.Desc.SubCategory == subcategory &&
                    (p.Desc.Name == name || p.Desc.NickName == nickname));

                if (componentProxy != null)
                {
                    var instance = componentProxy.CreateInstance();
                    if (instance != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"成功创建组件: {name}");
                        return instance;
                    }
                }

                // 如果找不到，尝试创建基础参数组件
                System.Diagnostics.Debug.WriteLine($"未找到组件 {category}/{subcategory}/{name}，创建参数组件");
                return CreateParameterComponent(name, nickname);
            }
            catch (Exception ex)
            {
                // 记录错误并返回参数组件
                System.Diagnostics.Debug.WriteLine($"CreateComponent 错误: {ex.Message}");
                return CreateParameterComponent(name, nickname);
            }
        }

        /// <summary>
        /// 设置组件的值
        /// </summary>
        private static void SetComponentValue(IGH_DocumentObject component, string value)
        {
            if (component is IGH_Param param)
            {
                // 对于参数类型的组件
                if (param is Param_Point pointParam)
                {
                    try
                    {
                        var pointCoords = JsonSerializer.Deserialize<PointCoordinates>(value);
                        if (pointCoords != null)
                        {
                            var ghPoint = new GH_Point(new Point3d(pointCoords.X, pointCoords.Y, pointCoords.Z));
                            pointParam.PersistentData.Clear();
                            pointParam.PersistentData.Append(ghPoint);
                        }
                    }
                    catch { /* 忽略反序列化错误 */ }
                }
                else if (param is Param_String panelParam)
                {
                    panelParam.PersistentData.Clear();
                    panelParam.PersistentData.Append(new GH_String(value));
                }
                // 可以为其他参数类型添加更多逻辑
            }
            else if (component is GH_NumberSlider slider)
            {
                // 对于滑块组件
                if (decimal.TryParse(value, out decimal sliderValue))
                {
                    slider.SetSliderValue(sliderValue);
                }
            }
        }

        // 用于反序列化点坐标的辅助类
        private class PointCoordinates
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }

        /// <summary>
        /// 创建基础参数组件
        /// </summary>
        private static IGH_Param CreateParameterComponent(string name, string nickname)
        {
            try
            {
                IGH_Param param = null;

                switch (name.ToLowerInvariant())
                {
                    case "point":
                    case "pt":
                        param = new Param_Point();
                        break;
                    case "curve":
                    case "crv":
                        param = new Param_Curve();
                        break;
                    case "surface":
                    case "srf":
                        param = new Param_Surface();
                        break;
                    case "mesh":
                    case "m":
                        param = new Param_Mesh();
                        break;
                    case "number":
                    case "num":
                        param = new Param_Number();
                        break;
                    case "integer":
                    case "int":
                        param = new Param_Integer();
                        break;
                    case "text":
                    case "string":
                    case "panel":
                        param = new Param_String();
                        break;
                    case "boolean":
                    case "bool":
                        param = new Param_Boolean();
                        break;
                    case "vector":
                    case "v":
                        param = new Param_Vector();
                        break;
                    case "plane":
                    case "pl":
                        param = new Param_Plane();
                        break;
                    case "color":
                    case "colour":
                        param = new Param_Colour();
                        break;
                    case "interval":
                        param = new Param_Interval();
                        break;
                    case "matrix":
                        param = new Param_Matrix();
                        break;
                    case "transform":
                        param = new Param_Transform();
                        break;
                    case "geometry":
                        param = new Param_Geometry();
                        break;
                    case "brep":
                        param = new Param_Brep();
                        break;
                    default:
                        param = new Param_GenericObject();
                        break;
                }

                if (param != null)
                {
                    param.Name = name;
                    param.NickName = nickname ?? name;
                    System.Diagnostics.Debug.WriteLine($"成功创建参数组件: {name}");
                }

                return param;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateParameterComponent 错误: {ex.Message}");
                return new Param_GenericObject() { Name = name, NickName = nickname ?? name };
            }
        }

        /// <summary>
        /// 设置一个已存在组件的值或属性。
        /// </summary>
        /// <param name="component_id">要修改的组件的唯一ID。</param>
        /// <param name="value">要设置的值。对于'slider'，这是一个数字；对于'panel'，这是一个字符串；对于'point'，这是一个JSON字符串，格式为'{\"X\":10,\"Y\":20,\"Z\":0}'。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>操作结果。</returns>
        [McpServerTool(Name = "set_component_value")]
        [Description("设置画布上一个已有组件的值。可用于设定点(point)的具体坐标、滑块(slider)的数值、面板(panel)的文本等。")]
        public static Task<string> SetComponentValue(
            [Description("要修改的组件的唯一ID。")] string component_id,
            [Description("要设置的值。对于'slider'，这是一个数字；对于'panel'，这是一个字符串；对于'point'，这是一个JSON字符串，格式为'{\"X\":10,\"Y\":20,\"Z\":0}'。")] string value,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档。");
                }

                var component = FindComponentById(component_id, document);
                if (component == null)
                {
                    return Task.FromResult($"错误：找不到ID为 '{component_id}' 的组件。");
                }

                SetComponentValue(component, value);

                // 刷新画布以显示更改
                component.ExpireSolution(true);
                Grasshopper.Instances.ActiveCanvas?.Refresh();

                return Task.FromResult($"成功：已设置组件 {component_id} 的值。");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：{ex.Message}");
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
        [Description("连接两个 Grasshopper 组件的输出和输入参数。使用前必须先读取 grasshopper://current_document 资源来确认组件存在并获取准确的参数名称。常见连接：滑块的 'Value' 输出 → 圆的 'Radius' 输入；点的 'Point' 输出 → 圆的 'Center' 输入；几何体的 'Geometry' 输出 → 变换的 'Geometry' 输入。")]
        public static Task<string> ConnectComponents(
            [Description("源组件的唯一ID（从 add_component 工具获得或从 grasshopper://current_document 资源中查找）")] string source_id,
            [Description("目标组件的唯一ID（从 add_component 工具获得或从 grasshopper://current_document 资源中查找）")] string target_id,
            [Description("源组件的输出参数名称。常见名称：Value（滑块输出）、Point（点输出）、Geometry（几何体输出）、Circle（圆输出）。如不确定请先读取当前文档")] string source_param = null,
            [Description("目标组件的输入参数名称。常见名称：Radius（半径输入）、Center（圆心输入）、Geometry（几何体输入）。如不确定请先读取当前文档")] string target_param = null,
            [Description("源参数索引（可选，当无法通过名称匹配时使用）")] int? source_param_index = null,
            [Description("目标参数索引（可选，当无法通过名称匹配时使用）")] int? target_param_index = null,
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
                    var availableOutputs = GetAvailableOutputs(sourceComponent);
                    return Task.FromResult($"错误：找不到有效的源输出参数。可用的输出参数：{availableOutputs}");
                }

                // 获取目标输入参数
                IGH_Param targetInput = GetInputParameter(targetComponent, target_param, target_param_index);
                if (targetInput == null)
                {
                    var availableInputs = GetAvailableInputs(targetComponent);
                    return Task.FromResult($"错误：找不到有效的目标输入参数。可用的输入参数：{availableInputs}");
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


        // //clear_document暂时废弃

        // /// <summary>
        // /// 清空 Grasshopper 文档
        // /// </summary>
        // /// <param name="cancellationToken">取消令牌</param>
        // /// <returns>操作结果</returns>
        // [McpServerTool(Name = "clear_document")]
        // [Description("清空当前 Grasshopper 文档")]
        // public static Task<string> ClearDocument(CancellationToken cancellationToken = default)
        // {
        //     try
        //     {
        //         var document = Grasshopper.Instances.ActiveCanvas?.Document;
        //         if (document == null)
        //         {
        //             return Task.FromResult("错误：没有活动的 Grasshopper 文档");
        //         }

        //         // 获取所有对象并移除
        //         var allObjects = document.Objects.ToList();
        //         foreach (var obj in allObjects)
        //         {
        //             document.RemoveObject(obj, false);
        //         }

        //         // 清空组件映射
        //         _componentMap.Clear();

        //         // 刷新画布
        //         Grasshopper.Instances.ActiveCanvas?.Refresh();

        //         return Task.FromResult($"成功：已清空 Grasshopper 文档，移除了 {allObjects.Count} 个对象");
        //     }
        //     catch (Exception ex)
        //     {
        //         return Task.FromResult($"错误：{ex.Message}");
        //     }
        // }

        /// <summary>
        /// 获取 Grasshopper 文档信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文档信息</returns>
        [McpServerTool(Name = "get_document_info")]
        [Description("获取当前 Grasshopper 文档的基本信息，包括组件数量和组件列表。这是一个简化版本，如需要详细的组件参数和连接信息，请使用 grasshopper://current_document 资源。")]
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
                    Objects = document.Objects.Select(obj => new
                    {
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
        [Description("**必须首先调用** - 获取可用的 Grasshopper 组件模式列表。在使用 create_pattern 之前，请先调用此方法了解有哪些预定义模式可用。这样可以避免创建不支持的模式并获得更好的结果。")]
        public static Task<string> GetAvailablePatterns(
            [Description("（可选）搜索关键词，用于过滤特定类型的模式。例如：'point'、'line'、'3d' 等")] string query = "",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var usage_guide = @"
推荐的工具使用顺序：
1. add_component - 添加单个组件（最可靠）
2. connect_components - 连接组件
3. create_pattern - 仅用于预定义的复杂模式

最佳实践：
- 简单需求：使用 add_component 逐个添加组件
- 复杂设计：先查看下面的预定义模式，确认支持后再使用 create_pattern
";

                var patterns = new List<object>
                {
                    new {
                        Name = "Point Grid",
                        Description = "创建点阵模式（包含X、Y数量滑块）",
                        Usage = "适用于需要规律排列点的场景"
                    },
                    new {
                        Name = "Line Segment",
                        Description = "创建线段模式（包含起点、终点）",
                        Usage = "适用于需要定义线段的场景"
                    },
                    new {
                        Name = "Circle",
                        Description = "创建圆形模式（包含中心点、半径滑块）",
                        Usage = "适用于需要定义圆的场景"
                    },
                    new {
                        Name = "Box",
                        Description = "创建3D长方体模式（包含起点、尺寸滑块）",
                        Usage = "适用于需要定义3D盒子的场景"
                    },
                    new {
                        Name = "Voronoi Pattern",
                        Description = "创建Voronoi图案模式（包含点集、边界）",
                        Usage = "适用于需要Voronoi分割的场景"
                    }
                };

                if (!string.IsNullOrEmpty(query))
                {
                    patterns = patterns.Where(p =>
                    {
                        var pattern = (dynamic)p;
                        string name = pattern.Name?.ToString() ?? "";
                        string desc = pattern.Description?.ToString() ?? "";
                        return name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               desc.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                    }).ToList();
                }

                var result = new
                {
                    Usage_Guide = usage_guide,
                    Available_Patterns = patterns,
                    Note = "对于简单需求，建议直接使用 add_component 而不是 create_pattern"
                };

                return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
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
        [Description("高级功能 - 仅在调用 get_available_patterns 确认支持后使用。根据描述创建复杂的 Grasshopper 组件模式。对于简单单个组件，建议使用 add_component。对于复杂设计，请先调用 get_available_patterns 查看预定义模式。")]
        public static Task<string> CreatePattern(
            [Description("必须是 get_available_patterns 返回的模式名称之一，或者是明确的几何描述（如：'Point Grid'、'Voronoi Pattern'、'Box'）。不要使用模糊描述。")] string description,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var document = Grasshopper.Instances.ActiveCanvas?.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：没有活动的 Grasshopper 文档");
                }

                // 首先检查是否是支持的模式
                var supportedPatterns = new[]
                {
                    "basic point", "point grid", "line segment", "circle", "rectangle",
                    "box", "number slider", "panel", "curve division", "voronoi pattern"
                };

                var desc = description.ToLowerInvariant().Trim();

                // 检查是否是已知模式
                var isKnownPattern = supportedPatterns.Any(pattern =>
                    desc.Contains(pattern) || pattern.Contains(desc));

                if (!isKnownPattern && desc.Length < 5)
                {
                    return Task.FromResult($@"不建议使用模糊描述 '{description}'。

建议的使用方法：
1. 首先调用 get_available_patterns 查看支持的模式
2. 然后使用返回的具体模式名称调用 create_pattern

支持的模式包括：{string.Join("、", supportedPatterns)}

或者使用 add_component 添加单个组件。");
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
            if (document == null)
            {
                return "错误：文档为空";
            }

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
                else if (desc.Contains("box") || desc.Contains("rectangular"))
                {
                    // 创建长方体模式
                    return CreateBoxPattern(document);
                }
                else
                {
                    // 默认创建基础模式
                    return CreateBasicPattern(document);
                }
            }
            catch (Exception ex)
            {
                return $"创建模式时出错：{ex.Message}\n详细信息：{ex.StackTrace}";
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
        /// 创建长方体模式
        /// </summary>
        private static string CreateBoxPattern(GH_Document document)
        {
            var components = new List<string>();

            try
            {
                // 创建起始点参数
                var originPoint = CreateParameterComponent("point", "Origin");
                if (originPoint != null)
                {
                    if (originPoint.Attributes == null)
                        originPoint.CreateAttributes();
                    originPoint.Attributes.Pivot = new System.Drawing.PointF(50, 100);
                    document.AddObject(originPoint, false);
                    components.Add("起始点");
                }

                // 创建X尺寸滑块
                var xSlider = CreateParameterComponent("number", "X Size");
                if (xSlider != null)
                {
                    if (xSlider.Attributes == null)
                        xSlider.CreateAttributes();
                    xSlider.Attributes.Pivot = new System.Drawing.PointF(50, 150);
                    document.AddObject(xSlider, false);
                    components.Add("X尺寸滑块");
                }

                // 创建Y尺寸滑块
                var ySlider = CreateParameterComponent("number", "Y Size");
                if (ySlider != null)
                {
                    if (ySlider.Attributes == null)
                        ySlider.CreateAttributes();
                    ySlider.Attributes.Pivot = new System.Drawing.PointF(50, 200);
                    document.AddObject(ySlider, false);
                    components.Add("Y尺寸滑块");
                }

                // 创建Z尺寸滑块
                var zSlider = CreateParameterComponent("number", "Z Size");
                if (zSlider != null)
                {
                    if (zSlider.Attributes == null)
                        zSlider.CreateAttributes();
                    zSlider.Attributes.Pivot = new System.Drawing.PointF(50, 250);
                    document.AddObject(zSlider, false);
                    components.Add("Z尺寸滑块");
                }

                return $"成功创建长方体模式，包含组件：{string.Join(", ", components)}";
            }
            catch (Exception ex)
            {
                return $"创建长方体模式时出错：{ex.Message}";
            }
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

        /// <summary>
        /// 诊断 Grasshopper 环境状态
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>诊断信息</returns>
        [McpServerTool(Name = "diagnose_environment")]
        [Description("诊断 Grasshopper 环境状态")]
        public static Task<string> DiagnoseEnvironment(CancellationToken cancellationToken = default)
        {
            try
            {
                var diagnostics = new
                {
                    GrasshopperInstancesExists = typeof(Grasshopper.Instances) != null,
                    ActiveCanvasExists = Grasshopper.Instances.ActiveCanvas != null,
                    DocumentExists = Grasshopper.Instances.ActiveCanvas?.Document != null,
                    ComponentServerExists = Grasshopper.Instances.ComponentServer != null,
                    RhinoDocExists = Rhino.RhinoDoc.ActiveDoc != null,
                    GrasshopperVersion = Grasshopper.Versioning.Version.ToString(),
                    RhinoVersion = Rhino.RhinoApp.Version.ToString(),
                    CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return Task.FromResult(JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"诊断失败：{ex.Message}\n堆栈跟踪：{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 简单测试工具，直接创建参数组件
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>测试结果</returns>
        [McpServerTool(Name = "test_simple_add")]
        [Description("简单测试工具，直接添加参数组件")]
        public static Task<string> TestSimpleAdd(CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查基本环境
                if (Grasshopper.Instances.ActiveCanvas == null)
                {
                    return Task.FromResult("错误：ActiveCanvas 为空");
                }

                var document = Grasshopper.Instances.ActiveCanvas.Document;
                if (document == null)
                {
                    return Task.FromResult("错误：Document 为空");
                }

                // 直接创建一个简单的参数组件
                var param = new Param_Point();
                param.Name = "Test Point";
                param.NickName = "Test";

                // 创建属性
                param.CreateAttributes();

                // 设置位置
                param.Attributes.Pivot = new System.Drawing.PointF(200, 200);

                // 添加到文档
                document.AddObject(param, false);

                // 刷新画布
                Grasshopper.Instances.ActiveCanvas.Refresh();

                return Task.FromResult($"成功：添加了测试点参数，ID: {param.InstanceGuid}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"测试失败：{ex.Message}\n详细信息：{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取组件的可用输出参数列表
        /// </summary>
        private static string GetAvailableOutputs(IGH_DocumentObject component)
        {
            var outputs = new List<string>();

            if (component is IGH_Component comp)
            {
                foreach (var param in comp.Params.Output)
                {
                    outputs.Add($"'{param.Name}' ({param.NickName})");
                }
            }
            else if (component is IGH_Param param)
            {
                outputs.Add($"'{param.Name}' ({param.NickName})");
            }

            return outputs.Count > 0 ? string.Join(", ", outputs) : "无可用输出参数";
        }

        /// <summary>
        /// 获取组件的可用输入参数列表
        /// </summary>
        private static string GetAvailableInputs(IGH_DocumentObject component)
        {
            var inputs = new List<string>();

            if (component is IGH_Component comp)
            {
                foreach (var param in comp.Params.Input)
                {
                    inputs.Add($"'{param.Name}' ({param.NickName})");
                }
            }

            return inputs.Count > 0 ? string.Join(", ", inputs) : "无可用输入参数";
        }
    }
}
