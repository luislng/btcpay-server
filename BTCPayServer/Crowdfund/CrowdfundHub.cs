using System.Threading.Tasks;
using BTCPayServer.Controllers;
using BTCPayServer.Models.AppViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Hubs
{
    public class CrowdfundHub: Hub
    {
        public const string InvoiceCreated = "InvoiceCreated";
        public const string PaymentReceived = "PaymentReceived";
        public const string InfoUpdated = "InfoUpdated";
        private readonly AppsPublicController _AppsPublicController;

        public CrowdfundHub(AppsPublicController appsPublicController)
        {
            _AppsPublicController = appsPublicController;
        }
        public async Task ListenToCrowdfundApp(string appId)
        {
            if (Context.Items.ContainsKey("app"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, Context.Items["app"].ToString());
                Context.Items.Remove("app");
            }
            Context.Items.Add("app", appId);
            await Groups.AddToGroupAsync(Context.ConnectionId, appId);
        }


        public async Task CreateInvoice(ContributeToCrowdfund model)
        {
               model.RedirectToCheckout = false;
               _AppsPublicController.ControllerContext.HttpContext = Context.GetHttpContext();
               var result = await _AppsPublicController.ContributeToCrowdfund(Context.Items["app"].ToString(), model);
               await Clients.Caller.SendCoreAsync(InvoiceCreated, new[] {(result as OkObjectResult)?.Value.ToString()});
        }

    }
}
