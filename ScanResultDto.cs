namespace Syngenta.API.Models;

public class ScanResultDto
{
    public DateTime timestamp { get; set; }
    public string cameraId { get; set; }
    public string qrCode { get; set; }
    public string status { get; set; }
}