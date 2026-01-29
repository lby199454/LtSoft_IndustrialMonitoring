namespace LtSoft_IndustrialMonitoring.Models
{
	public class ElecMeterData
	{
		public long Id { get; set; }
		public int MeterNo { get; set; }
		public string MeterAddr { get; set; } = string.Empty;
		public string MeterName { get; set; } = string.Empty;
		public decimal Kwh { get; set; }
		public DateTime CollectTime { get; set; }
		public string SiteName { get; set; } = string.Empty;
		public string CompanyName { get; set; } = string.Empty;
	}

	public class ElecDataFilter
	{
		public string TableName { get; set; } = string.Empty;
		public int? MeterNo { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 100;
	}

	public class ElecDataResponse
	{
		public List<ElecMeterData> Data { get; set; } = new List<ElecMeterData>();
		public int TotalCount { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
	}
}
