using EVotingSystem.Data;
using EVotingSystem.Forms;

namespace EVotingSystem;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        var databaseFactory = new DatabaseFactory();
        databaseFactory.Initialize();

        ApplicationConfiguration.Initialize();

        var context = new ApplicationContext();
        var navigation = new FormNavigationService(context);
        var mainForm = new MainForm(databaseFactory, navigation);
        navigation.Start(mainForm);

        Application.Run(context);
    }
}
