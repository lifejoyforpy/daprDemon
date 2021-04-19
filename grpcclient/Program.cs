using System;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using GrpcServiceSample.Generated;
namespace grpcclient
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var cts= new CancellationTokenSource();
            await RunAsync(cts.Token);
            Console.WriteLine("Hello World!");
            return 0;
        }
        public  async static Task RunAsync(CancellationToken cancellationToken)
        {
            using var client = new DaprClientBuilder().Build();

            Console.WriteLine("Invoking grpc deposit");
            var deposit = new GrpcServiceSample.Generated.Transaction() { Id = "17", Amount = 99 };
            var account = await client.InvokeMethodGrpcAsync<GrpcServiceSample.Generated.Transaction, Account>("grpcsample", "deposit", deposit, cancellationToken);
            Console.WriteLine("Returned: id:{0} | Balance:{1}", account.Id, account.Balance);
            Console.WriteLine("Completed grpc deposit");

            Console.WriteLine("Invoking grpc withdraw");
            var withdraw = new GrpcServiceSample.Generated.Transaction() { Id = "17", Amount = 10, };
            await client.InvokeMethodGrpcAsync("grpcsample", "withdraw", withdraw, cancellationToken);
            Console.WriteLine("Completed grpc withdraw");

            Console.WriteLine("Invoking grpc balance");
            var request = new GetAccountRequest() { Id = "17", };
            account = await client.InvokeMethodGrpcAsync<GetAccountRequest, Account>("grpcsample", "getaccount", request, cancellationToken);
            Console.WriteLine($"Received grpc balance {account.Balance}");
        }
    }
    
    
    
    
    
}
