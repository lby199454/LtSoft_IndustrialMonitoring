namespace LtSoft_IndustrialMonitoring.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string BaseName { get; set; } = string.Empty;
        public string DeviceIP { get; set; } = string.Empty;
        public int Port { get; set; } = 502;
        public string SqlTableName { get; set; } = string.Empty;
        public int[] DeviceAddresses { get; set; } = Array.Empty<int>();
        public bool IsOnline { get; set; }
        public DateTime LastCommunication { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class DeviceStatus
    {
        public int DeviceId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastChecked { get; set; }
    }
}
