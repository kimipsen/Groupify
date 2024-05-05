using ReactiveUI;

namespace Groupify.App.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public static void DoStuff() => Console.WriteLine("DoStuff!");
}
