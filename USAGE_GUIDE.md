# Grasshopper MCP 服务器使用指南

## 概述

这个 Grasshopper MCP 服务器现在已经优化，能够更好地与 AI 模型协作。主要改进包括：

### 1. 增强的 Resources（资源）

#### `grasshopper://current_document`

- **用途**: 提供当前 Grasshopper 画布的完整状态
- **包含信息**:
  - 所有组件的 ID、名称、位置
  - 每个组件的输入/输出参数详情
  - 组件之间的连接关系
- **使用时机**: 在执行任何操作前，AI 应该先读取这个资源

#### 其他资源

- `grasshopper://component_library`: 可用组件库
- `grasshopper://environment`: 环境信息
- `grasshopper://troubleshooting`: 故障排除指南

### 2. 智能 Prompts（提示）

#### `grasshopper_workflow_guide`

- 提供完整的工作流指南
- 强调先读取文档状态的重要性
- 包含组件连接的最佳实践

#### `component_connection_guide`

- 详细的组件连接指南
- 常见连接模式说明
- 参数命名规范

#### `error_troubleshooting`

- 错误诊断和解决方案
- 常见问题的排查步骤

#### `design_pattern_guide`

- 设计模式实现指南
- 如 Voronoi、网格、螺旋等模式

### 3. 优化的 Tools（工具）

#### `add_component`

- **改进**: 详细的组件类型说明和参数描述
- **新特性**: 建议先读取当前文档状态

#### `connect_components`

- **改进**: 详细的参数说明，包含常见连接模式
- **新特性**: 强调参数名称的重要性

#### `get_document_info`

- **改进**: 明确说明与 resources 的区别

## 使用流程

### 理想的 AI 工作流程

1. **了解现状**

   ```
   读取 grasshopper://current_document 资源
   ```

2. **规划设计**

   ```
   使用 grasshopper_workflow_guide 提示
   或使用 design_pattern_guide 提示（如果有特定模式需求）
   ```

3. **执行操作**

   ```
   使用 add_component 工具添加组件
   使用 connect_components 工具连接组件
   ```

4. **验证结果**
   ```
   再次读取 grasshopper://current_document 资源确认结果
   ```

### 示例：创建一个带半径控制的圆

#### 理想的 AI 操作序列：

1. **读取当前状态**

   ```
   访问 grasshopper://current_document 资源
   ```

2. **添加滑块**

   ```
   add_component(component_type="slider", x=100, y=200)
   返回: slider_id = "abc-123"
   ```

3. **添加圆组件**

   ```
   add_component(component_type="circle", x=300, y=200)
   返回: circle_id = "def-456"
   ```

4. **连接滑块到圆的半径**

   ```
   connect_components(
     source_id="abc-123",
     target_id="def-456",
     source_param="Value",
     target_param="Radius"
   )
   ```

5. **验证结果**
   ```
   再次访问 grasshopper://current_document 资源
   确认连接已建立
   ```

## 关键改进点

### 1. 解决"AI 乱用工具"的问题

- 通过详细的工具描述，AI 现在知道每个工具的确切用途
- 通过 Prompts 引导，AI 学会了正确的工作流程

### 2. 解决"胡乱连接"的问题

- `grasshopper://current_document` 资源让 AI 能"看见"当前状态
- 详细的参数信息让 AI 知道如何正确连接组件
- 连接指南提供了常见连接模式的参考

### 3. 提供上下文感知

- AI 现在会在操作前先了解当前状态
- 可以基于现有组件做出智能决策
- 避免重复创建组件或错误连接

## 测试建议

1. **基础测试**: 要求 AI 创建一个简单的滑块控制的圆
2. **复杂测试**: 要求 AI 创建一个 Voronoi 图案
3. **错误恢复测试**: 故意给出错误信息，看 AI 是否能使用故障排除资源

通过这些改进，AI 现在应该能够更智能、更准确地操作 Grasshopper，而不是盲目地使用工具。
