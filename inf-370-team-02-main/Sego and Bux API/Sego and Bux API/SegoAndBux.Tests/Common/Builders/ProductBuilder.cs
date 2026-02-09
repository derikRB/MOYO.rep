using Sego_and__Bux.Models;

namespace SegoAndBux.Tests.Common.Builders
{
    public class ProductBuilder
    {
        private readonly Product _p = new()
        {
            ProductID = 201,
            Name = "Pink Tee",
            Description = "Tee",
            Price = 100.00m,
            StockQuantity = 10,
            ProductTypeID = 1,
            IsDeleted = false
        };

        public ProductBuilder WithPrimaryImage(string imagePath)
        {
            // Keep it minimal to fit any ProductImage model
            _p.PrimaryImage = new ProductImage { ImagePath = imagePath };
            return this;
        }

        public ProductBuilder WithId(int id) { _p.ProductID = id; return this; }
        public ProductBuilder WithName(string name) { _p.Name = name; return this; }
        public ProductBuilder WithPrice(decimal price) { _p.Price = price; return this; }
        public ProductBuilder WithStock(int stock) { _p.StockQuantity = stock; return this; }

        public Product Build() => _p;
    }
}
