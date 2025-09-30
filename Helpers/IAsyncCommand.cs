using System.Threading.Tasks;
using System.Windows.Input;

namespace Mechanical_Keyboard.Helpers
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
    }
}
