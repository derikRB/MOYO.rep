using System.Collections.Generic;

namespace Sego_and__Bux.Models
{
    public class Template
    {
        public int TemplateID { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }

        // ← NEW: many‐to‐many to Products
        public ICollection<ProductTemplate> ProductTemplates { get; set; }
            = new List<ProductTemplate>();
    }
}