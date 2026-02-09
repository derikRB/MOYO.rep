using System;

namespace Sego_and__Bux.DTOs
{
    public record OrderStatusCountDto(string Status, int Count);
    public record SalesPointDto(string Date, decimal Total);
    public record LowStockPointDto(string Label, int Qty, int Threshold);
}
