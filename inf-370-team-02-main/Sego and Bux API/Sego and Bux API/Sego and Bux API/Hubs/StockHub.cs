using Microsoft.AspNetCore.SignalR;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Hubs
{
    public class StockHub : Hub
    {
        public async Task SendStockUpdate(ProductStockUpdateDto update)
        {
            // CHANGE THIS LINE - match frontend expectation
            await Clients.All.SendAsync("StockUpdated", update);
        }
    }
}