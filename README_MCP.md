## Grasshopper MCP 服务器

### 概述
这是一个为 Grasshopper 实现的 Model Context Protocol (MCP) 服务器，允许大语言模型通过 MCP 协议与 Grasshopper 进行交互。

### 功能特性

#### 工具 (Tools)
服务器提供以下工具：

1. **add_component** - 在 Grasshopper 画布上添加组件
   - 参数：component_type (字符串), x (数值), y (数值)
   - 支持的组件类型：point, curve, circle, line, panel, slider

2. **connect_components** - 连接两个 Grasshopper 组件
   - 参数：source_id (字符串), target_id (字符串), source_param (可选), target_param (可选)

3. **clear_document** - 清空当前 Grasshopper 文档
   - 无参数

4. **get_document_info** - 获取当前 Grasshopper 文档信息
   - 返回：JSON 格式的文档信息（名称、组件数量、修改状态等）

5. **get_available_patterns** - 获取可用的组件模式
   - 参数：query (查询字符串)
   - 返回：匹配的模式列表

6. **create_pattern** - 根据描述创建组件模式
   - 参数：description (模式描述)

#### 资源 (Resources)
服务器提供以下资源：

1. **grasshopper://status** - 获取 Grasshopper 状态信息
   - 包含文档状态、版本信息等

2. **grasshopper://component_guide** - 获取组件使用指南
   - 详细的组件分类和使用说明

3. **grasshopper://component_library** - 获取组件库信息
   - 可用组件列表和说明

4. **grasshopper://environment** - 获取环境信息
   - 系统信息、版本信息等

### 使用方法

1. 在 Grasshopper 中加载此组件
2. 点击"Start MCP Server"按钮启动服务器
3. 服务器将在指定地址启动（默认：http://localhost:3001/mcp）
4. LLM 客户端可以通过 MCP 协议连接到此服务器

### 技术实现

- 基于 .NET 7.0
- 使用 ModelContextProtocol C# SDK 0.3.0-preview.1
- 支持 HTTP 传输和 Stdio 传输
- 实现了完整的 MCP 协议规范

### 状态指示

组件面板会显示当前服务器状态：
- "Server stopped" - 服务器已停止
- "Server running at http://localhost:3001/mcp" - 服务器正在运行

### 注意事项

- 确保 Grasshopper 文档处于活动状态才能执行相关操作
- 服务器停止时会自动清理资源
- 支持多种组件类型和操作模式
