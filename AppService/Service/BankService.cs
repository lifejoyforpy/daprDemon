using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AppService.Models;
using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace AppService.Service
{
    public class BankService:IBankService
    {
        private readonly DaprClient _daprClient;
        private ILogger<BankService> _logger;
        public BankService(ILogger<BankService> logger,DaprClient  daprClient )
        {
            _logger = logger;
            _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        }

        public async Task<Account> Deposit(Transaction transaction,CancellationToken cancellationToken)
        {
            var deposit = new Transaction  { Id = "17", Amount = 99m };
            var account = await _daprClient.InvokeMethodAsync<Transaction,Account>( HttpMethod.Post,  "ServiceB", 
                "deposit",transaction, cancellationToken);
            _logger.LogDebug("Returned: id:{0} | Balance:{1}", account?.Id, account?.Balance);
            return account;
        }
        public Task WithDraw(Transaction transaction)
        {
            throw new System.NotImplementedException();
        }
    }
}
