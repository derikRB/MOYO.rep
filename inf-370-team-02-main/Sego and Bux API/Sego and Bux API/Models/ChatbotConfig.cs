namespace Sego_and__Bux.Models
{
    public class ChatbotConfig
    {
        //whatsapp and email chatbot configuration incase there's any updates
        public int Id { get; set; }
        public string WhatsAppNumber { get; set; } = "";
        public string SupportEmail { get; set; } = "";
    }
}
