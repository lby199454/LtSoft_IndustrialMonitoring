using System.ComponentModel.DataAnnotations.Schema;

namespace LtSoft_IndustrialMonitoring.Models
{
    /// <summary>
    /// 计数器数据模型
    /// </summary>
    public class CounterData
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("device_id")]
        public int DeviceId { get; set; } // 设备ID

        [Column("date_now")]
        public DateTime DateNow { get; set; } // 记录时间

        [Column("count")]
        public int Count { get; set; } // 计数值

        [Column("logs")]
        public string Logs { get; set; } = string.Empty; // 日志信息

        [Column("reserved1")]
        public string Reserved1 { get; set; } = string.Empty;

        [Column("reserved2")]
        public string Reserved2 { get; set; } = string.Empty;

        [Column("reserved3")]
        public string Reserved3 { get; set; } = string.Empty;
    }

    /// <summary>
    /// 筛选器
    /// </summary>
    public class CounterDataFilter
    {
        public string TableName { get; set; } = string.Empty; // 表名（站点名）
        public int? DeviceId { get; set; }       // 设备ID筛选
        public DateTime? StartTime { get; set; } // 开始时间
        public DateTime? EndTime { get; set; }   // 结束时间
        public int PageNumber { get; set; } = 1; // 页码
        public int PageSize { get; set; } = 100; // 每页数量
    }

    /// <summary>
    /// 响应数据
    /// </summary>
    public class CounterDataResponse
    {
        public List<CounterData> Data { get; set; } = new List<CounterData>();
        public int TotalCount { get; set; } // 总记录数
        public int SumCount { get; set; }   // count字段累加值
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// 统计信息
    /// </summary>
    public class CounterSummary
    {
        public string LocationName { get; set; } = string.Empty; // 站点名称
        public int TotalRecords { get; set; } // 总记录数
        public int TotalCount { get; set; } // count字段累加值
        public DateTime? FirstRecordTime { get; set; } // 最早记录时间
        public DateTime? LastRecordTime { get; set; } // 最新记录时间
    }

    /// <summary>
    /// 新增 日统计返回结构
    /// </summary>
    public class CounterDayStats
    {
        public string TableName { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int TotalRecords { get; set; } // 总记录数（COUNT）意思为数据表里面一共有多少条数据
		public int TotalCount { get; set; }   // 累加值（SUM）    意思为数据表里面所有count1累加值   因为count为1所以记录数和累加值一致
	}

    /// <summary>
    /// 导出所有站点数据请求
    /// </summary>
    public class ExportAllSitesRequest
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class AllSiteStatRow
    { 
        public string SiteName { get; set; }
        public string DeviceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalCount { get; set; }
}
}
