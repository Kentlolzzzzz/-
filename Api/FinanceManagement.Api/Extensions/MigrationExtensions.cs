using FinanceManagement.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace FinanceManagement.Api.Extensions
{
    /// <summary>
    /// 数据库迁移扩展方法
    /// </summary>
    public static class MigrationExtensions
    {
        /// <summary>
        /// 配置应用程序使用自动数据库迁移
        /// </summary>
        /// <typeparam name="TContext">数据库上下文类型</typeparam>
        /// <param name="app">Web应用程序</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>Web应用程序</returns>
        public static IHost MigrateDatabase<TContext>(this IHost app, ILogger logger) 
            where TContext : DbContext
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<TContext>();
                    logger.LogInformation("正在执行数据库迁移...");
                    db.Database.Migrate();
                    logger.LogInformation("数据库迁移完成");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "应用数据库迁移时发生错误");
                    throw;
                }
            }
            return app;
        }
    }
} 