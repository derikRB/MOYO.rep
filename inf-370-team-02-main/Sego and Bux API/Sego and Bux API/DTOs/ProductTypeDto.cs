using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.DTOs;
public class ProductTypeDto
{
    public int ProductTypeID { get; set; }
    public string ProductTypeName { get; set; }
    public string? Description { get; set; }
    public int CategoryID { get; set; }
}
