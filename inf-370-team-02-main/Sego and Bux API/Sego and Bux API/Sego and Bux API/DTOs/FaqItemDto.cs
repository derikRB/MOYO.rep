namespace Sego_and__Bux.DTOs
{
    public class FaqItemDto
    {
        public int FaqId { get; set; }
        public string Category { get; set; } = "";
        public string QuestionVariant { get; set; } = "";
        public string Answer { get; set; } = "";
        public int SortOrder { get; set; }
    }
}
