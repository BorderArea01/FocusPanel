using System.Windows;
using System.Windows.Input;

namespace FocusPanel.Views
{
    public partial class TaskDetailWindow : Window
    {
        public TaskDetailWindow()
        {
            InitializeComponent();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}