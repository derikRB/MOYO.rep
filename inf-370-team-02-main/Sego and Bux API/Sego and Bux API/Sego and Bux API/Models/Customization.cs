using Sego_and__Bux.Models;

public class Customization
{
    public int CustomizationID { get; set; }
    public int OrderLineID { get; set; }
    public OrderLine OrderLine { get; set; }

    public string? Template { get; set; }
    public string? CustomText { get; set; }
    public string? Font { get; set; }
    public int? FontSize { get; set; }
    public string? Color { get; set; }
    public string? UploadedImagePath { get; set; }
    public string? SnapshotPath { get; set; } // ← NEW for Konva/canvas snapshots
}
