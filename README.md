# Grasshopper MCP Plugin

这是一个用于 Grasshopper 的 Model Context Protocol (MCP) 插件项目。

## 开发环境设置

### 必需的软件

1. **Visual Studio Code** - 推荐的开发环境
2. **.NET SDK** - 需要 .NET 7.0 或更高版本
3. **Rhino 8** - 用于测试插件
4. **C# 扩展** - VS Code 的 C# 开发支持

### 推荐的 VS Code 扩展

项目已配置了推荐的扩展列表，在打开项目时 VS Code 会提示安装：

- C# (ms-dotnettools.csharp)
- .NET Runtime (ms-dotnettools.vscode-dotnet-runtime)
- PowerShell (ms-vscode.powershell)
- .NET Interactive (ms-dotnettools.dotnet-interactive-vscode)

## 项目结构

```
grasshoppermcp/
├── .vscode/                    # VS Code 配置文件
│   ├── settings.json          # 工作区设置
│   ├── tasks.json             # 构建任务
│   ├── launch.json            # 调试配置
│   └── extensions.json        # 推荐扩展
├── bin/                       # 编译输出
├── obj/                       # 中间文件
├── Properties/                # 项目属性
├── server/                    # MCP 服务器代码
├── grasshoppermcp.csproj      # 项目文件
├── grasshoppermcp.sln         # 解决方案文件
├── grasshoppermcpComponent.cs # 主要组件
└── grasshoppermcpInfo.cs      # 插件信息
```

## 如何使用

### 1. 构建项目

在 VS Code 中使用以下方式构建项目：

- 按 `Ctrl+Shift+P` 打开命令面板
- 输入 "Tasks: Run Task"
- 选择 "build"

或者使用快捷键：

- `Ctrl+Shift+B` 快速构建

### 2. 运行和调试

#### 方法一：使用调试器启动 Rhino

1. 设置断点（如果需要）
2. 按 `F5` 或点击调试按钮
3. 选择 "Launch Rhino (Debug)"

#### 方法二：手动启动 Rhino 后附加调试器

1. 手动启动 Rhino 并加载 Grasshopper
2. 在 VS Code 中按 `F5`
3. 选择 "Attach to Rhino"
4. 选择 Rhino 进程

#### 方法三：直接运行 Rhino

- 使用任务："Tasks: Run Task" -> "run-rhino"

### 3. 编译后的文件位置

插件编译后的 `.gha` 文件位于：

```
bin/Debug/net7.0/grasshoppermcp.gha
```

### 4. 环境变量

项目配置了 `RHINO_PACKAGE_DIRS` 环境变量，Rhino 会自动从编译输出目录加载插件。

## 开发技巧

### 1. IntelliSense 和代码补全

VS Code 配置了完整的 C# 开发支持，包括：

- 语法高亮
- 代码补全
- 错误检查
- 重构支持

### 2. 调试

- 可以在 C# 代码中设置断点
- 支持变量查看和调用堆栈
- 可以在 Rhino 运行时附加调试器

### 3. 格式化

- 保存时自动格式化代码
- 遵循 C# 编码规范

## 故障排除

### 常见问题

1. **构建失败**

   - 确保安装了正确版本的 .NET SDK
   - 检查 Rhino 和 Grasshopper 引用路径

2. **调试无法启动**

   - 检查 `launch.json` 中的 Rhino 路径是否正确
   - 确保 Rhino 8 已正确安装

3. **插件未加载**
   - 检查 `RHINO_PACKAGE_DIRS` 环境变量
   - 确保 `.gha` 文件已正确编译

### 日志和调试输出

在 VS Code 的"调试控制台"中可以查看调试输出。

## 贡献

欢迎提交 Pull Request 和 Issue！

## 许可证

[在此添加许可证信息]
