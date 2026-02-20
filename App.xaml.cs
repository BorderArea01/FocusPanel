using System;
using System.IO;
using System.Linq;
using System.Windows;
using FocusPanel.Data;
using Microsoft.EntityFrameworkCore;

namespace FocusPanel;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Initialize Database
            using (var context = new AppDbContext())
            {
                // Ensure database is created. 
                // Note: If you change the model significantly, you might need to handle migrations 
                // or delete the db file manually during development.
                if (!context.Database.EnsureCreated())
                {
                    // Database exists. Check if schema is valid (simple check: try to query Todos)
                    try 
                    {
                        var count = context.Todos.Count(); 
                    }
                    catch (Exception)
                    {
                        // Schema mismatch likely. Recreate DB.
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database Initialization Error: {ex.Message}\nTry deleting focuspanel.db manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogException(ex);
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        
        // Specific handling for SQLite "no such table" error which might bubble up
        if (e.Exception.Message.Contains("no such table") || e.Exception.InnerException?.Message.Contains("no such table") == true)
        {
             MessageBox.Show("Database schema mismatch detected. The application will restart with a fresh database.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Warning);
             try 
             {
                 File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "focuspanel.db"));
             }
             catch {} // Best effort
             
             // Optionally restart app here, but for now just exit cleanly or let user restart
             System.Diagnostics.Process.Start(ResourceAssembly.Location);
             Current.Shutdown();
             return;
        }

        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex);
            MessageBox.Show($"Critical Error: {ex.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogException(Exception ex)
    {
        try
        {
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
            string message = $"[{DateTime.Now}] {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logFile, message);
        }
        catch { }
    }
}
