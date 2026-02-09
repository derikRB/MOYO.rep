namespace Sego_and__Bux.Config
{
    public sealed class MaintenanceOptions
    {
        public bool Enabled { get; set; } = false;
        public bool AllowRestore { get; set; } = true; // set false in Production
        public string[] BypassRoles { get; set; } = new[] { "Admin","Employee","Manager" };
        public string? Message { get; set; } = "Down for maintenance";
    }
}
