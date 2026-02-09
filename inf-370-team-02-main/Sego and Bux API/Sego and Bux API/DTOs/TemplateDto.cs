namespace Sego_and__Bux.DTOs
{
    public class TemplateDto
    {
        public int TemplateID { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }

        public int[]? ProductIDs { get; set; }

    }
}
