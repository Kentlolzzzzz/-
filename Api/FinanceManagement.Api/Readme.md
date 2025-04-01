# 财务管理系统 API

这是财务管理系统的后端 API 项目，基于 .NET 9.0 和 Entity Framework Core 开发。

## 主要功能

- 用户认证和授权（JWT）
- 项目管理
- 交易记录管理（收入/支出）
- 账户管理
- 合同管理
- 发票管理
- 员工和工资管理
- 客户和供应商管理

## 技术栈

- .NET 9.0
- Entity Framework Core + SQLite
- JWT 认证
- Swagger API 文档

## 项目结构

```
FinanceManagement.Api/
├── Controllers/        # API 控制器
├── Data/               # 数据上下文和数据访问
├── Models/             # 数据模型
│   ├── Entities/       # 实体类
│   ├── DTOs/           # 数据传输对象
│   └── ViewModels/     # 视图模型
├── Services/           # 业务逻辑服务
│   ├── Interfaces/     # 服务接口
│   └── Implementations/# 服务实现
├── Repositories/       # 仓储模式
│   ├── Interfaces/     # 仓储接口
│   └── Implementations/# 仓储实现
├── Configurations/     # 配置类
├── Extensions/         # 扩展方法
├── Middlewares/        # 中间件
└── Utilities/          # 工具类
```

## 如何运行

1. 确保安装了 .NET 9.0 SDK
2. 克隆本仓库
3. 导航到项目目录: `cd FinanceManagement.Api`
4. 运行应用: `dotnet run`
5. 访问 Swagger UI: `https://localhost:5001/swagger`

## API 端点

- `/api/Auth` - 用户认证
- `/api/Transactions` - 交易记录管理
- `/api/Projects` - 项目管理
- `/api/Accounts` - 账户管理
- `/api/Contracts` - 合同管理
- `/api/Invoices` - 发票管理
- `/api/Employees` - 员工管理
- `/api/Salaries` - 工资管理
- `/api/Customers` - 客户管理
- `/api/Suppliers` - 供应商管理

## 开发人员

请联系项目维护者了解更多信息。 