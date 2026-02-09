using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.DTOs;
    public class CustomizationDto
    {
        public int OrderLineID { get; set; }
        public string Template { get; set; }
        public string CustomText { get; set; }
        public string Font { get; set; }
        public int FontSize { get; set; }
        public string Color { get; set; }
        public string UploadedImagePath { get; set; }
    }

   
