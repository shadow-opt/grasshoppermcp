# 快速入门指南 - VS Code 设置

## 1. 打开项目

有两种方式打开项目：

### 方式一：直接打开文件夹

1. 打开 VS Code
2. 选择 `File` → `Open Folder`
3. 选择 `d:\grasshoppermcp` 文件夹

### 方式二：使用工作区文件（推荐）

1. 双击 `grasshoppermcp.code-workspace` 文件
2. 或者在 VS Code 中选择 `File` → `Open Workspace from File`
3. 选择 `grasshoppermcp.code-workspace` 文件

## 2. 安装必要的扩展

VS Code 会自动提示安装推荐的扩展：

- **C#** - 必须安装，提供 C# 语言支持
- **.NET Runtime** - 必须安装，提供 .NET 运行时支持
- **PowerShell** - 推荐安装，改善 PowerShell 终端体验

## 3. 第一次构建

1. 按 `Ctrl+Shift+P` 打开命令面板
2. 输入 `Tasks: Run Task`
3. 选择 `build`
4. 等待构建完成

或者使用快捷键：

- 按 `Ctrl+Shift+B` 直接构建

## 4. 运行和测试

### 自动启动 Rhino 调试

1. 按 `F5` 开始调试
2. 选择 `Launch Rhino (Debug)`
3. Rhino 会自动启动并加载 Grasshopper
4. 插件会自动加载到 Grasshopper 中

### 手动启动 Rhino 后调试

1. 先手动启动 Rhino 并打开 Grasshopper
2. 在 VS Code 中按 `F5`
3. 选择 `Attach to Rhino`
4. 选择 Rhino 进程

## 5. 开发工作流

1. **编写代码** - 在 VS Code 中编辑 C# 文件
2. **构建** - `Ctrl+Shift+B` 或使用任务面板
3. **调试** - `F5` 启动调试或附加到 Rhino
4. **测试** - 在 Grasshopper 中测试插件功能

## 6. 常用快捷键

- `Ctrl+Shift+B` - 构建项目
- `F5` - 开始调试
- `Ctrl+Shift+P` - 命令面板
- `Ctrl+` - 打开终端
- `Ctrl+Shift+E` - 文件资源管理器
- `Ctrl+Shift+F` - 全局搜索

## 7. 故障排除

### 构建失败

- 检查 .NET SDK 是否安装
- 确保 C# 扩展已启用
- 查看"问题"面板中的错误信息

### 调试无法启动

- 检查 Rhino 路径是否正确（在 `launch.json` 中）
- 确保 Rhino 8 已正确安装
- 检查环境变量设置

### 插件未加载

- 确保构建成功
- 检查 `.gha` 文件是否在正确位置
- 验证环境变量 `RHINO_PACKAGE_DIRS` 设置

## 8. 项目结构说明

```
.vscode/
├── settings.json      # VS Code 工作区设置
├── tasks.json         # 构建任务配置
├── launch.json        # 调试配置
└── extensions.json    # 推荐扩展列表

grasshoppermcp.code-workspace  # VS Code 工作区文件
README.md                      # 详细说明文档
.editorconfig                  # 编码风格配置
```

现在你的项目已经完全适配了 VS Code 开发环境！
