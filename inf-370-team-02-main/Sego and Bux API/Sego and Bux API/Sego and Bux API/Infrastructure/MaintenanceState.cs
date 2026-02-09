using Sego_and__Bux.Config;

namespace Sego_and__Bux.Infrastructure
{
    public sealed class MaintenanceState
    {
        public bool Enabled { get; set; }
        public bool AllowRestore { get; set; }
        public string? Message { get; set; }
        public string[] BypassRoles { get; set; } = new string[0];

        public void InitializeFrom(MaintenanceOptions opts)
        {
            Enabled = opts.Enabled;
            AllowRestore = opts.AllowRestore;
            Message = opts.Message;
            BypassRoles = opts.BypassRoles ?? new string[0];
        }
    }
}