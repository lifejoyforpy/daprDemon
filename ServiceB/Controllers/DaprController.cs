using System;
using System.Threading.Tasks;
using System.Xml.XPath;
using AppService.Models;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ServiceB.Controllers
{
    [ApiController]
    public class DaprController : ControllerBase
    {
        private readonly ILogger<DaprController> _logger;
        public const string _storeName = "statestore";
        public DaprController(ILogger<DaprController>logger)
        {
            _logger = logger;
        }
       
        
        /// <summary>
        /// Gets the account information as specified by the id.
        /// </summary>
        /// <param name="account">Account information for the id from Dapr state store.</param>
        /// <returns>Account information.</returns>
        [HttpGet("{account}")]
        public ActionResult<Account> Get([FromState(_storeName)] StateEntry<Account> account)
        {
            if (account.Value is null)
            {
                return this.NotFound();
            }

            return account.Value;
        }

        /// <summary>
        /// Method for depositing（存款） to account as specified in transaction.村
        /// </summary>
        /// <param name="transaction">Transaction info.</param>
        /// <param name="daprClient">State client to interact with Dapr runtime.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        ///  "pubsub", the first parameter into the Topic attribute, is name of the default pub/sub configured by the Dapr CLI.
        [Topic("pubsub", "deposit")]
        [HttpPost("deposit")]
        public async Task<ActionResult<Account>> Deposit(Transaction transaction, [FromServices] DaprClient daprClient)
        {
            _logger.LogDebug("Enter deposit");
            var state = await daprClient.GetStateEntryAsync<Account>(_storeName, transaction.Id);
            state.Value ??= new Account() { Id = transaction.Id, };
            state.Value.Balance += transaction.Amount;
            await state.SaveAsync();
            return state.Value;
        }

        /// <summary>
        /// Method for withdrawing（取钱） from account as specified in transaction.
        /// </summary>
        /// <param name="transaction">Transaction info.</param>
        /// <param name="daprClient">State client to interact with Dapr runtime.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        ///  "pubsub", the first parameter into the Topic attribute, is name of the default pub/sub configured by the Dapr CLI.
        [Topic("pubsub", "withdraw")]
        [HttpPost("withdraw")]
        public async Task<ActionResult<Account>> Withdraw(Transaction transaction, [FromServices] DaprClient daprClient)
        {
            _logger.LogDebug("Enter withdraw");
            var state = await daprClient.GetStateEntryAsync<Account>(_storeName, transaction.Id);

            if (state.Value == null)
            {
                return this.NotFound();
            }

            state.Value.Balance -= transaction.Amount;
            await state.SaveAsync();
            return state.Value;
        }

        /// <summary>
        /// Method for returning a BadRequest result which will cause Dapr sidecar to throw an RpcException
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="daprClient"></param>
        /// <returns></returns>
        [HttpPost("throwException")]
        public async Task<ActionResult<Account>> ThrowException(Transaction transaction, [FromServices] DaprClient daprClient)
        {
            Console.WriteLine("Enter ThrowException");
            var task = Task.Delay(10);
            await task;
            return BadRequest(new { statusCode = 400, message = "bad request" });
        }
    }
}
