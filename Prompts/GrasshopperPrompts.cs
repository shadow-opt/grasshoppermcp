using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace grasshoppermcp.Prompts
{
    /// <summary>
    /// Grasshopper MCP 提示集 - 用户可选择的工作流
    /// </summary>
    [McpServerPromptType]
    public class GrasshopperPrompts
    {
        /// <summary>
        /// 创建3D几何图案工作流
        /// </summary>
        [McpServerPrompt(Name = "create_3d_pattern")]
        [Description("启动创建3D几何图案的工作流程")]
        public static Task<string> Create3DPattern(
            [Description("图案类型（如：voronoi、delaunay、grid、spiral等）")] string pattern_type = "",
            [Description("图案规模（small、medium、large）")] string scale = "medium",
            CancellationToken cancellationToken = default)
        {
            var prompt = $@"我想创建一个{pattern_type}类型的3D几何图案，规模为{scale}。

请帮我：
1. 分析当前Grasshopper环境
2. 设计合适的组件布局
3. 逐步创建所需组件
4. 建立正确的连接关系
5. 调整参数以获得理想效果

开始创建这个图案吧！";

            return Task.FromResult(prompt);
        }

        /// <summary>
        /// 分析当前设计工作流
        /// </summary>
        [McpServerPrompt(Name = "analyze_design")]
        [Description("分析当前Grasshopper设计的结构和优化建议")]
        public static Task<string> AnalyzeDesign(
            [Description("分析重点（structure、performance、optimization、workflow）")] string focus = "structure",
            CancellationToken cancellationToken = default)
        {
            var prompt = $@"请分析我当前的Grasshopper设计，重点关注{focus}方面。

请帮我：
1. 检查当前文档状态
2. 分析组件布局和连接关系
3. 识别潜在问题或改进点
4. 提供具体的优化建议
5. 如果需要，帮助实施改进

开始分析吧！";

            return Task.FromResult(prompt);
        }


        /// <summary>
        /// 创建参数化设计
        /// </summary>
        [McpServerPrompt(Name = "create_parametric_design")]
        [Description("创建参数化设计，可通过滑块动态调整")]
        public static Task<string> CreateParametricDesign(
            [Description("设计目标（如：建筑立面、结构体系、装饰图案等）")] string design_goal = "",
            [Description("关键参数（如：尺寸、角度、密度、数量等）")] string key_parameters = "",
            CancellationToken cancellationToken = default)
        {
            var prompt = $@"我想创建一个参数化设计，目标是{design_goal}。

关键参数包括：{key_parameters}

请帮我：
1. 设计参数化逻辑
2. 创建控制滑块
3. 构建几何生成流程
4. 建立参数与几何的关联
5. 测试参数变化的效果

开始创建参数化设计吧！";

            return Task.FromResult(prompt);
        }
    }
}
