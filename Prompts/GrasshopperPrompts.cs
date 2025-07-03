using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace grasshoppermcp.Prompts
{
    /// <summary>
    /// Grasshopper MCP 提示集
    /// </summary>
    [McpServerPromptType]
    public class GrasshopperPrompts
    {
        /// <summary>
        /// Grasshopper 操作指南提示
        /// </summary>
        [McpServerPrompt(Name = "grasshopper_workflow_guide")]
        [Description("提供 Grasshopper 操作的完整工作流指南")]
        public static Task<string> GetWorkflowGuide(
            [Description("用户的设计意图或目标")] string design_intent = "",
            CancellationToken cancellationToken = default)
        {
            var systemMessage = $@"# Grasshopper MCP 操作指南

## 核心原则
在 Grasshopper 中创建任何设计之前，你必须遵循以下步骤：

### 1. 了解当前状态
- **始终**先调用 `grasshopper://current_document` 资源来查看当前画布上的组件
- 检查已有的组件、它们的位置、参数和连接关系
- 理解现有的设计流程

### 2. 精确的工具使用
- **组件命名**：使用准确的组件类型名称，参考 `grasshopper://component_library` 资源
- **位置规划**：合理规划组件在画布上的位置，避免重叠
- **连接验证**：连接组件前，确认源组件和目标组件的参数名称

### 3. 常见组件类型
- **输入参数**：`slider`（数值滑块）、`panel`（文本面板）、`point`（点）
- **几何体**：`circle`（圆）、`line`（直线）、`curve`（曲线）
- **变换**：`move`（移动）、`rotate`（旋转）、`scale`（缩放）
- **模式**：`voronoi`（泰森多边形）、`delaunay`（德劳内三角剖分）

### 4. 工作流程
1. 检查当前文档状态
2. 规划组件布局
3. 逐步添加组件
4. **设置参数值**
5. **建立连接**
6. 验证结果

### 5. 连接规则
- 滑块通常连接到需要数值输入的组件
- 几何体组件的输出连接到变换或其他几何操作
- 面板用于显示结果或输入文本

## 设计意图
用户的设计意图：{design_intent}

请根据这个意图，制定合适的实现策略。

# 重点
### 重要提示：
- 对于需要平面输入的组件（如 Box、Circle、Rectangle 等），始终使用 “xy plane” 作为平面来源，而不是 Point 组件
- 使用标准化的组件名称，如 xy plane, box, circle, number slider 等
- 坐标值应该是数字，建议使用网格布局（x和y的间隔约为200-300单位）
- 确保先创建所有元件，然后再进行连接
- 对于平面到几何元件的连接，源参数应为 “Plane”，目标参数应为 “Base” 或 “Plane”

";

            return Task.FromResult(systemMessage);
        }

        /// <summary>
        /// 组件连接指南
        /// </summary>
        [McpServerPrompt(Name = "component_connection_guide")]
        [Description("提供组件连接的详细指南")]
        public static Task<string> GetConnectionGuide(
            [Description("源组件类型")] string source_component = "",
            [Description("目标组件类型")] string target_component = "",
            CancellationToken cancellationToken = default)
        {
            var systemMessage = $@"# 组件连接指南

## 连接前的准备工作
1. **读取当前文档**：使用 `grasshopper://current_document` 资源
2. **确认组件存在**：检查源组件和目标组件是否已创建
3. **了解参数名称**：确认准确的输入/输出参数名称

## 常见连接模式

### 数值滑块连接
- 滑块输出：通常是 `Value` 或 `N`
- 常见目标：`Radius`（半径）、`Height`（高度）、`Count`（数量）

### 几何体连接
- 点 → 圆心：`Point` → `Center`
- 线 → 曲线：`Line` → `Curve`
- 圆 → 几何体：`Circle` → `Geometry`

### 变换连接
- 几何体 → 变换对象：`Geometry` → `Geometry`
- 向量 → 移动方向：`Vector` → `Motion`

## 连接步骤
1. 确认源组件ID和输出参数名称
2. 确认目标组件ID和输入参数名称
3. 使用 `connect_components` 工具建立连接
4. 验证连接是否成功

## 当前连接任务
源组件：{source_component}
目标组件：{target_component}

请根据这些组件类型，选择合适的连接策略。";

            return Task.FromResult(systemMessage);
        }

        /// <summary>
        /// 错误排查指南
        /// </summary>
        [McpServerPrompt(Name = "error_troubleshooting")]
        [Description("提供错误排查的详细指南")]
        public static Task<string> GetTroubleshootingGuide(
            [Description("遇到的错误信息")] string error_message = "",
            CancellationToken cancellationToken = default)
        {
            var systemMessage = $@"# 错误排查指南

## 常见错误类型及解决方案

### 1. 组件创建失败
- **错误特征**：`add_component` 返回错误
- **排查步骤**：
  1. 检查 Grasshopper 是否正常运行
  2. 确认组件类型名称正确
  3. 验证坐标参数有效

### 2. 连接失败
- **错误特征**：`connect_components` 返回错误
- **排查步骤**：
  1. 确认源组件和目标组件存在
  2. 检查参数名称是否正确
  3. 验证参数类型是否兼容

### 3. 参数不匹配
- **错误特征**：连接建立但没有数据流
- **排查步骤**：
  1. 检查输出参数类型
  2. 确认输入参数期望的数据类型
  3. 可能需要类型转换组件

### 4. 组件未显示
- **错误特征**：组件创建成功但看不到
- **排查步骤**：
  1. 检查组件位置是否在可见区域
  2. 刷新画布视图
  3. 调整画布缩放级别

## 诊断工具
1. **当前文档状态**：`grasshopper://current_document`
2. **环境信息**：`grasshopper://environment`
3. **组件库**：`grasshopper://component_library`

## 当前错误
错误信息：{error_message}

请根据这个错误信息，提供具体的解决方案。";

            return Task.FromResult(systemMessage);
        }

        /// <summary>
        /// 设计模式指南
        /// </summary>
        [McpServerPrompt(Name = "design_pattern_guide")]
        [Description("提供常见设计模式的实现指南")]
        public static Task<string> GetDesignPatternGuide(
            [Description("设计模式类型，如 voronoi、grid、spiral 等")] string pattern_type = "",
            CancellationToken cancellationToken = default)
        {
            var systemMessage = $@"# 设计模式实现指南

## 模式类型：{pattern_type}

### 实现步骤模板

#### 1. 准备阶段
- 读取当前文档状态
- 规划组件布局
- 确定所需参数

#### 2. 创建输入参数
- 数值滑块：控制大小、数量、间距等
- 点参数：定义起始位置或关键点
- 文本面板：显示信息或输入文本

#### 3. 生成基础几何
- 创建基本形状（点、线、圆等）
- 应用变换（移动、旋转、缩放）
- 建立几何关系

#### 4. 应用模式算法
- 使用特定的模式组件
- 连接输入参数
- 调整参数值

#### 5. 后处理
- 几何体优化
- 视觉效果调整
- 输出设置

## 常见模式实现

### Voronoi 模式
1. 创建点集合
2. 定义边界
3. 应用 Voronoi 组件
4. 调整单元格大小

### 网格模式
1. 定义网格间距
2. 创建点阵列
3. 连接相邻点
4. 应用变换

### 螺旋模式
1. 设置螺旋参数
2. 创建螺旋曲线
3. 沿曲线分布对象
4. 调整密度和方向

请根据选择的模式类型，制定具体的实现计划。";

            return Task.FromResult(systemMessage);
        }

        /// <summary>
        /// 工具选择指南
        /// </summary>
        [McpServerPrompt(Name = "tool_selection_guide")]
        [Description("提供详细的工具选择指南和最佳实践，帮助 AI 选择合适的工具")]
        public static Task<string> GetToolSelectionGuide(
            [Description("用户的具体需求或意图")] string user_intent = "",
            [Description("当前的复杂度级别：simple、intermediate、complex")] string complexity_level = "simple",
            CancellationToken cancellationToken = default)
        {
            var systemMessage = $@"# 🎯 Grasshopper MCP 工具选择指南

## 用户需求：{user_intent}
## 复杂度级别：{complexity_level}

---

## 📋 推荐的工具选择流程

### 1. 基础信息收集
- **始终先调用**: `get_document_info` - 了解当前画布状态
- **检查现有组件**: 避免重复创建，了解已有连接

### 2. 工具选择策略

#### ✅ 简单需求（推荐优先使用）
**适用场景**: 单个组件、基本几何、简单参数控制
- `add_component` - 添加单个组件（滑块、面板、点、圆、线等）
- `connect_components` - 连接两个组件
- `get_document_info` - 查看画布状态

**示例**:
```
需求: 创建一个滑块控制圆的半径
方案: add_component('slider') → add_component('circle') → connect_components()
```

#### ⚠️ 中等复杂度（谨慎使用）
**适用场景**: 多个相关组件、需要特定组合
- 多次使用 `add_component` + `connect_components`
- 如果有预定义模式，先查询再使用

#### 🔍 复杂需求（必须先查询）
**适用场景**: 复杂几何模式、专门的算法模式
- **必须先调用**: `get_available_patterns` - 查看可用的预定义模式
- **然后调用**: `create_pattern` - 创建匹配的模式

**重要**: 绝对不要直接使用 `create_pattern` 而不先查询可用模式！

---

## 🚫 常见错误及避免方法

### 错误1: 直接使用 create_pattern
```
❌ 错误: create_pattern('voronoi cube')  // 不知道是否存在
✅ 正确: get_available_patterns() → 确认模式名称 → create_pattern('确切名称')
```

### 错误2: 过度使用复杂工具
```
❌ 错误: 为简单的圆使用 create_pattern
✅ 正确: 简单需求用 add_component('circle')
```

### 错误3: 跳过状态检查
```
❌ 错误: 直接开始创建组件
✅ 正确: 先 get_document_info 了解现状
```

---

## 🎨 具体场景决策树

### 场景1: 创建基础几何
```
需求: 点、线、圆、面板、滑块
→ 使用: add_component
→ 理由: 基础组件，简单可靠
```

### 场景2: 参数化设计
```
需求: 滑块控制几何参数
→ 使用: add_component('slider') + add_component('geometry') + connect_components
→ 理由: 清晰的组件组合
```

### 场景3: 复杂几何算法
```
需求: 泰森多边形、德劳内三角等
→ 使用: get_available_patterns → create_pattern
→ 理由: 需要专门的算法，必须先确认可用性
```

---

## 💡 最佳实践原则

1. **简单优先**: 能用基础工具解决的，不用复杂工具
2. **查询先行**: 复杂模式必须先查询可用性
3. **状态感知**: 每次操作前了解当前状态
4. **逐步构建**: 一步一步添加组件，再建立连接
5. **验证结果**: 关键步骤后检查结果

---

## 🔧 工具功能对比

| 工具 | 适用场景 | 复杂度 | 可靠性 | 推荐度 |
|------|----------|--------|--------|--------|
| add_component | 基础组件 | 低 | 高 | ⭐⭐⭐⭐⭐ |
| connect_components | 组件连接 | 低 | 高 | ⭐⭐⭐⭐⭐ |
| get_document_info | 状态查询 | 低 | 高 | ⭐⭐⭐⭐⭐ |
| get_available_patterns | 模式查询 | 中 | 高 | ⭐⭐⭐⭐ |
| create_pattern | 复杂模式 | 高 | 中 | ⭐⭐⭐ |

---

## 📌 记住这些关键点

1. **不确定时，选择简单的工具**
2. **create_pattern 前必须先 get_available_patterns**
3. **每个复杂操作前都要 get_document_info**
4. **组件坐标建议间隔 100-200 像素**
5. **连接组件时确认参数名称的准确性**

基于用户需求 '{user_intent}' 和复杂度 '{complexity_level}'，建议优先考虑相应级别的工具选择。
";

            return Task.FromResult(systemMessage);
        }
    }
}
