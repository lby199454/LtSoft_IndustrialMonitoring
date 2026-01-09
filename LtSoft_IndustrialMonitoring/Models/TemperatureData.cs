namespace LtSoft_IndustrialMonitoring.Models
{
    public class TemperatureData
    {
        public int Id { get; set; }
        public int AddressId { get; set; } // 设备地址ID
        public DateTime DateNow { get; set; } // 记录时间
        public double TempValue { get; set; } // 温度值
        public string Logs { get; set; } = string.Empty; // 日志信息
    }

    public class TemperatureDataFilter
    {
        public string TableName { get; set; } = string.Empty; // 表名（地点名）
        public int? AddressId { get; set; } // 设备地址ID筛选
        public DateTime? StartTime { get; set; } // 开始时间
        public DateTime? EndTime { get; set; } // 结束时间
        public int PageNumber { get; set; } = 1; // 页码
        public int PageSize { get; set; } = 100; // 每页数量
    }

    public class TemperatureDataResponse
    {
        public List<TemperatureData> Data { get; set; } = new List<TemperatureData>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class LocationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
