using System.Threading;
using System.Threading.Tasks;
using AppService.Models;

namespace AppService.Service
{
    public interface IBankService
    {
        Task<Account> Deposit(Transaction transaction,CancellationToken cancellationToken);
        // Task WithDraw(Transaction transaction);
    }
}
