namespace Sego_and__Bux.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }

        public ICollection<ProductType> ProductTypes { get; set; } = new List<ProductType>();
    }

}
