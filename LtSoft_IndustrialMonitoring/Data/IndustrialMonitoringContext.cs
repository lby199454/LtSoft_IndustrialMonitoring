using LtSoft_IndustrialMonitoring.Models;

using Microsoft.EntityFrameworkCore;

namespace LtSoft_IndustrialMonitoring.Data
{
    /// <summary>
    /// 数据库上下文类, 设备监控的数据访问。
    /// </summary>
    public class IndustrialMonitoringContext : DbContext
    {
        public IndustrialMonitoringContext(DbContextOptions<IndustrialMonitoringContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }

        /// <summary>
        /// 配置实体模型
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Device>().ToTable("Devices");

            // 配置Type字段
            modelBuilder.Entity<Device>()
                .Property(d => d.Type)
                .HasMaxLength(255);
        }
    }
}
