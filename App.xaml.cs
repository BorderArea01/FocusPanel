using System.Windows;
using FocusPanel.Data;
using Microsoft.EntityFrameworkCore;

namespace FocusPanel
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            using (var context = new AppDbContext())
            {
                context.Database.EnsureCreated();
            }
        }
    }
}
