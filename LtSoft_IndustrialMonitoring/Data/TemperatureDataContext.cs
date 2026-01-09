using LtSoft_IndustrialMonitoring.Models;

using Microsoft.EntityFrameworkCore;

namespace LtSoft_IndustrialMonitoring.Data
{
    /// <summary>
    /// 数据库上下文, 温度数据查询
    /// </summary>
    public class TemperatureDataContext : DbContext
    {
        public TemperatureDataContext(DbContextOptions<TemperatureDataContext> options) : base(options)
        {
        }

        // 这里不再需要DbSet，因为使用 原生SQL查询
        // 但为了使用FromSqlRaw，我们需要一个虚拟的DbSet
        public virtual DbSet<TemperatureData> TemperatureData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 由于我们使用原生SQL查询，这里不需要配置模型
        }
    }
}
