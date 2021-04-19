using System;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.AppCallback.Autogen.Grpc.v1;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcServiceSample.Generated;
using Microsoft.Extensions.Logging;
namespace GrpcService.Services
{
    public class BankingGrpcService: AppCallback.AppCallbackBase
    {
        /// <summary>
        /// State store name.
        /// </summary>
        public const string StoreName = "statestore";

        private readonly ILogger<BankingGrpcService> _logger;
        private readonly DaprClient _daprClient;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="daprClient"></param>
        /// <param name="logger"></param>
        public BankingGrpcService(DaprClient daprClient, ILogger<BankingGrpcService> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// implement OnIvoke to support getaccount, deposit and withdraw
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<InvokeResponse> OnInvoke(InvokeRequest request, ServerCallContext context)
        {
            var response = new InvokeResponse();
            switch (request.Method)
            {
                case "getaccount":                
                    var input = request.Data.Unpack<GrpcServiceSample.Generated.GetAccountRequest>();
                    var output = await GetAccount(input, context);
                    response.Data = Any.Pack(output);
                    break;
                case "deposit":
                case "withdraw":
                    var transaction = request.Data.Unpack<GrpcServiceSample.Generated.Transaction>();
                    var account = request.Method == "deposit" ?
                        await Deposit(transaction, context) :
                        await Withdraw(transaction, context);
                    response.Data = Any.Pack(account);
                    break;
                default:
                    break;
            }
            return response;
        }

        /// <summary>
        /// implement ListTopicSubscriptions to register deposit and withdraw subscriber
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ListTopicSubscriptionsResponse> ListTopicSubscriptions(Empty request, ServerCallContext context)
        {
            var result = new ListTopicSubscriptionsResponse();
            result.Subscriptions.Add(new TopicSubscription
            {
                PubsubName = "pubsub",
                Topic = "deposit"
            });
            result.Subscriptions.Add(new TopicSubscription
            {
                PubsubName = "pubsub",
                Topic = "withdraw"
            });
            return Task.FromResult(result);
        }

        /// <summary>
        /// implement OnTopicEvent to handle deposit and withdraw event
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<TopicEventResponse> OnTopicEvent(TopicEventRequest request, ServerCallContext context)
        {
            if (request.PubsubName == "pubsub")
            {
                var input = JsonSerializer.Deserialize<Models.Transaction>(request.Data.ToStringUtf8(), this.jsonOptions);
                var transaction = new GrpcServiceSample.Generated.Transaction() { Id = input.Id, Amount = (int)input.Amount, };
                if (request.Topic == "deposit")
                {
                    await Deposit(transaction, context);
                }
                else
                {
                    await Withdraw(transaction, context);
                }
            }

            return await Task.FromResult(default(TopicEventResponse));
        }

        /// <summary>
        /// GetAccount
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<GrpcServiceSample.Generated.Account> GetAccount(GetAccountRequest input, ServerCallContext context)
        {
            var state = await _daprClient.GetStateEntryAsync<Models.Account>(StoreName, input.Id);
            return new GrpcServiceSample.Generated.Account() { Id = state.Value.Id, Balance = (int)state.Value.Balance, }; 
        }

        /// <summary>
        /// Deposit
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<GrpcServiceSample.Generated.Account> Deposit(GrpcServiceSample.Generated.Transaction transaction, ServerCallContext context)
        {
            _logger.LogDebug("Enter deposit");
            var state = await _daprClient.GetStateEntryAsync<Models.Account>(StoreName, transaction.Id);
            state.Value ??= new Models.Account() { Id = transaction.Id, };
            state.Value.Balance += transaction.Amount;
            await state.SaveAsync();
            return new GrpcServiceSample.Generated.Account() { Id = state.Value.Id, Balance = (int)state.Value.Balance, }; 
        }

        /// <summary>
        /// Withdraw
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<GrpcServiceSample.Generated.Account> Withdraw(GrpcServiceSample.Generated.Transaction transaction, ServerCallContext context)
        {
            _logger.LogDebug("Enter withdraw");
            var state = await _daprClient.GetStateEntryAsync<Models.Account>(StoreName, transaction.Id);

            if (state.Value == null)
            {
                throw new Exception($"NotFound: {transaction.Id}");
            }

            state.Value.Balance -= transaction.Amount;
            await state.SaveAsync();
            return new GrpcServiceSample.Generated.Account() { Id = state.Value.Id, Balance = (int)state.Value.Balance, };
        }
    }
}
