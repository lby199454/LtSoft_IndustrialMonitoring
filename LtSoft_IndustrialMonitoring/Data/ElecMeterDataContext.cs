using LtSoft_IndustrialMonitoring.Models;
using Microsoft.EntityFrameworkCore;

namespace LtSoft_IndustrialMonitoring.Data
{
	/// <summary>
	/// 电表数据 数据库上下文
	/// </summary>
	public class ElecMeterDataContext : DbContext
	{
		public ElecMeterDataContext(DbContextOptions<ElecMeterDataContext> options) : base(options)
		{
		}

		/// <summary>
		/// 若需要FromSqlRaw映射实体，可以添加虚拟DbSet
		/// </summary>
		public virtual DbSet<ElecMeterData> ElecMeterData { get; set; }

		/// <summary>
		/// 配置实体映射
		/// </summary>
		/// <param name="modelBuilder"></param>
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<ElecMeterData>(entity =>
			{
				entity.HasNoKey();
				entity.ToView("dummy_view");

				entity.Property(e => e.Id).HasColumnName("id");
				entity.Property(e => e.MeterNo).HasColumnName("meter_no");
				entity.Property(e => e.MeterAddr).HasColumnName("meter_addr");
				entity.Property(e => e.MeterName).HasColumnName("meter_name");
				entity.Property(e => e.Kwh).HasColumnName("kwh");
				entity.Property(e => e.CollectTime).HasColumnName("collect_time");
				entity.Property(e => e.SiteName).HasColumnName("site_name");
				entity.Property(e => e.CompanyName).HasColumnName("company_name");
			});
		}
	}
}
