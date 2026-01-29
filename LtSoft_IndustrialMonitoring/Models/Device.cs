namespace LtSoft_IndustrialMonitoring.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = 502;
        public string Type { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
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
