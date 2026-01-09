using LtSoft_IndustrialMonitoring.Models;

using Microsoft.EntityFrameworkCore;

namespace LtSoft_IndustrialMonitoring.Data
{
    /// <summary>
    /// 筛分计数 数据库上下文
    /// </summary>
    public class CounterDataContext : DbContext
    {
        public CounterDataContext(DbContextOptions<CounterDataContext> options) : base(options)
        {
        }

        // 这里不再需要DbSet，因为使用 原生SQL查询
        // 但为了使用FromSqlRaw，我们需要一个虚拟的DbSet
        public virtual DbSet<CounterData> CounterData { get; set; }

        /// <summary>
        /// 配置模型类
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CounterData>(entity =>
            {
                entity.HasNoKey(); // 设置为无键实体
                entity.ToView("dummy_view"); // 需要指定一个虚拟视图名

                // 配置列映射
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DeviceId).HasColumnName("device_id");
                entity.Property(e => e.DateNow).HasColumnName("date_now");
                entity.Property(e => e.Count).HasColumnName("count");
                entity.Property(e => e.Logs).HasColumnName("logs");
                entity.Property(e => e.Reserved1).HasColumnName("reserved1");
                entity.Property(e => e.Reserved2).HasColumnName("reserved2");
                entity.Property(e => e.Reserved3).HasColumnName("reserved3");
            });
        }
    }
}
