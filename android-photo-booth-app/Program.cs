namespace MagnusAkselvoll.AndroidPhotoBooth.App;

using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Set up dependency injection container
        var services = new ServiceCollection();
        ConfigureServices(services);
        using var serviceProvider = services.BuildServiceProvider();

        // Get MainForm from DI and run
        var mainForm = serviceProvider.GetRequiredService<MainForm>();
        Application.Run(mainForm);
    }

    /// <summary>
    /// Configures dependency injection services.
    /// </summary>
    static void ConfigureServices(IServiceCollection services)
    {
        // Register forms
        services.AddScoped<MainForm>();
        services.AddScoped<PictureForm>();

        // Register services (to be added in Phase 3)
        // services.AddScoped<ISettingsService, SettingsService>();
    }
}
