# Grasshopper MCP 故障排除和测试指南

## 问题诊断

根据您的反馈，`add_component` 和 `create_pattern` 调用失败，出现 "Object reference not set to an instance of an object" 错误。我们已经进行了以下改进：

## 改进内容

### 1. 增强的错误处理

- 添加了详细的空值检查
- 改进了错误消息，提供更多诊断信息
- 添加了堆栈跟踪信息

### 2. 新增测试工具

- `diagnose_environment` - 诊断 Grasshopper 环境状态
- `test_simple_add` - 简单的组件添加测试

### 3. 改进的组件创建

- 修复了 `CreateComponent` 方法的错误处理
- 改进了 `CreateParameterComponent` 方法
- 添加了属性创建的安全检查

## 测试步骤

### 步骤 1：环境诊断

首先运行环境诊断工具：

```json
{
  "method": "tools/call",
  "params": {
    "name": "diagnose_environment",
    "arguments": {}
  }
}
```

预期输出应该显示：

- `GrasshopperInstancesExists: true`
- `ActiveCanvasExists: true`
- `DocumentExists: true`
- `ComponentServerExists: true`

### 步骤 2：简单测试

运行简单测试工具：

```json
{
  "method": "tools/call",
  "params": {
    "name": "test_simple_add",
    "arguments": {}
  }
}
```

这将直接创建一个点参数组件，绕过复杂的组件查找逻辑。

### 步骤 3：基础组件添加

如果简单测试成功，尝试基础组件添加：

```json
{
  "method": "tools/call",
  "params": {
    "name": "add_component",
    "arguments": {
      "component_type": "point",
      "x": 150,
      "y": 150
    }
  }
}
```

### 步骤 4：创建模式

如果基础组件添加成功，尝试创建模式：

```json
{
  "method": "tools/call",
  "params": {
    "name": "create_pattern",
    "arguments": {
      "description": "basic pattern"
    }
  }
}
```

## 支持的组件类型

现在支持以下组件类型，全部作为参数组件创建：

### 基础类型

- `point` / `pt` - 点参数
- `curve` / `crv` - 曲线参数
- `surface` / `srf` - 表面参数
- `mesh` / `m` - 网格参数

### 数值类型

- `number` / `num` - 数值参数
- `integer` / `int` - 整数参数
- `boolean` / `bool` - 布尔参数

### 文本和显示

- `text` / `string` / `panel` - 文本参数
- `color` / `colour` - 颜色参数

### 几何和变换

- `vector` / `v` - 向量参数
- `plane` / `pl` - 平面参数
- `interval` - 区间参数
- `matrix` - 矩阵参数
- `transform` - 变换参数
- `geometry` - 几何参数
- `brep` - Brep 参数

## 常见问题解决

### 1. "ActiveCanvas 为空"

- 确保 Grasshopper 已正确启动
- 确保有一个活动的 Grasshopper 窗口

### 2. "Document 为空"

- 在 Grasshopper 中创建一个新文档
- 确保文档处于活动状态

### 3. "组件创建失败"

- 使用 `diagnose_environment` 检查环境
- 尝试 `test_simple_add` 进行基础测试

### 4. 组件未显示在画布上

- 检查组件是否在可见区域
- 尝试缩放或平移画布视图
- 使用 `get_document_info` 确认组件已添加

## 调试信息

所有工具现在提供更详细的错误信息：

- 具体的错误消息
- 堆栈跟踪信息
- 环境状态检查

这些信息将帮助我们进一步诊断和解决问题。

## 下一步

1. 请先运行 `diagnose_environment` 工具
2. 然后尝试 `test_simple_add` 工具
3. 根据结果，我们可以进一步调整代码

如果问题仍然存在，请提供详细的错误信息和环境诊断结果。
