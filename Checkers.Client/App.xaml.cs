using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace Checkers.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText("crash.log", e.ExceptionObject.ToString());
            MessageBox.Show("Критическая ошибка: " + e.ExceptionObject);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText("ui_crash.log", e.Exception.ToString());
            MessageBox.Show("Ошибка UI: " + e.Exception.Message);
            e.Handled = true;
        }

    }



}
