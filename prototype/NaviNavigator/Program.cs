using System.Windows.Forms;

namespace NaviNavigator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new BrowserForm());
    }
}
