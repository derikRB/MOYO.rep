// Sego_and__Bux/Hubs/StockHub.cs
using Microsoft.AspNetCore.SignalR;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Hubs
{
    public class StockHub : Hub
    {
        public async Task SendStockUpdate(ProductStockUpdateDto update)
        {
            await Clients.All.SendAsync("ReceiveStockUpdate", update);
        }
    }
}
//sdhfgh