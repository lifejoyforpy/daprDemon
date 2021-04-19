using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AppService.Service;
using Dapr;
using Dapr.Client;
using GrpcService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Account=AppService.Models.Account;
using Transaction=AppService.Models.Transaction;

namespace ServiceA.Controllers
{
    [ApiController]
   
    public class DaprController : Controller
    {
        private readonly IBankService _bankService;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ILogger<DaprClient>  _logger;
        
        public DaprController(IBankService bankService,ILogger<DaprClient> logger)
        {
            _bankService = bankService;
            _logger = logger;
        }

        #region  invoke service   Invoke HTTP services using DaprClient
        /// <summary>
        /// get invoke  
        /// </summary>
        /// <returns></returns>
        [HttpGet("invoke/get")]
        public async Task<IEnumerable<WeatherForecast>> DaprGetInvoke()
        {
            var daprClient = new DaprClientBuilder().Build();
            var response =await daprClient.InvokeMethodAsync<IEnumerable<WeatherForecast>>
                (HttpMethod.Get, "ServiceB", "WeatherForecast");
            return response;
        }
        /// <summary>
        /// get invoke  
        /// </summary>
        /// <returns></returns>
        [HttpPost("invoke/post")]
        public async Task<Account> Deposit() 
        {
            var deposit = new Transaction  { Id = "17", Amount = 99m };
            var  result=await _bankService.Deposit(deposit,_cts.Token);
            return result;
        }
        
        #endregion

        #region Invoke Grpc services using DaprClient
        /// <summary>
        /// grpc invoke
        /// </summary>
        /// <param name="daprClient"></param>
        /// <returns></returns>
        [HttpGet("grpc/getaccount")]
        public async Task<ActionResult<Account>> GrpcInvoke([FromServices] DaprClient daprClient)
        {
           
           _logger.LogDebug("Invoking grpc balance");
            var request = new GetAccountRequest() { Id = "17", };
            var account = await daprClient.InvokeMethodGrpcAsync<GetAccountRequest, GrpcService.Account>
                ("grpcsample", "getaccount", request, _cts.Token);
            _logger.LogDebug($"Received grpc balance {account.Balance}");
            return new JsonResult(new Account{Id = account.Id,Balance = account.Balance});
        }

        #endregion
        #region state management
        private static readonly string stateKeyName = "account";
        private static readonly string storeName = "statestore";
        /// <summary>
        /// statestore
        /// </summary>
        /// <param name="daprClient"></param>
        /// <returns></returns>
        [HttpGet("statestore/example")]
        public async Task<OkResult> StateStoreExample([FromServices] DaprClient daprClient)
        {
            // Save state which will create a new etag
             await daprClient.SaveStateAsync<Account>(storeName, stateKeyName,  
                 new Account() { Id = "zhangsan", Balance = 1000, }, cancellationToken: _cts.Token);
             _logger.LogDebug("Saved state which created a new entry with an initial etag");
            //
            // Read the state and etag
            var  account= await daprClient.GetStateAsync<Account>(storeName, stateKeyName, cancellationToken: _cts.Token);
            _logger.LogDebug($"Retrieved state: {account.Id}  {account.Balance}");
            await daprClient.DeleteStateAsync(storeName, stateKeyName, cancellationToken: _cts.Token);
            return Ok();
            
            // update 
        }

        #endregion

        #region pub/sub
        private static readonly string pubsubName = "pubsub";
        /// <summary>
        /// pub sub 
        /// </summary>
        /// <param name="daprClient"></param>
        /// <returns></returns>
        [HttpGet("pub/deposit")]
        public async Task<ActionResult> Deposit([FromServices] DaprClient daprClient)
        {
            var eventData = new { Id = "18", Amount = 10m, };
            await daprClient.PublishEventAsync(pubsubName, "deposit", eventData, _cts.Token);
            _logger.LogDebug("Published deposit event!");
            return Ok();
        }
        /// <summary>
        /// pub withdraw
        /// </summary>
        /// <param name="daprClient"></param>
        /// <returns></returns>
       [HttpGet("pub/withdraw")]
        public async Task<ActionResult> WithDraw([FromServices] DaprClient daprClient)
        {
            var eventData = new { Id = "18", Amount = 10m, };
            await daprClient.PublishEventAsync(pubsubName, "withdraw", eventData, _cts.Token);
            _logger.LogDebug("Published withdraw event!");
            return Ok();
        }

        #endregion pub/subscribe

        #region secretStore configure
        /// <summary>
        /// SecretStoreConfigure
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        [HttpGet("secretStore/configure")]
        public async Task<IActionResult> SecretStoreConfigure([FromServices] DaprClient daprClient)
        {
            var secretValue=await daprClient.GetSecretAsync("demosecrets", "super-secret");
            // var secretValue = configuration["super-secret"];
            return await  Task.FromResult(new JsonResult(secretValue));
        }


        #endregion


        #region binding
         
       
        [HttpGet("trigger/http/binding")]
        public async Task<ActionResult> HttpBinding([FromServices] DaprClient daprClient)
        {
            var request = new BindingRequest("httpbinding","get");
            var result= await daprClient.InvokeBindingAsync(request,cancellationToken: _cts.Token);
            return Ok();
        }
        
      
        [HttpGet("test")]
        public async Task<string> Test()
        {
           _logger.LogDebug("bingding trigger");
            return await Task.FromResult("test");
        }
        #endregion
    }
}
