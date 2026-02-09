// Sego_and__Bux/DTOs/FeatureAccessDto.cs
namespace Sego_and__Bux.DTOs
{
    public class FeatureAccessDto
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string[] Roles { get; set; } = System.Array.Empty<string>();
    }
}
