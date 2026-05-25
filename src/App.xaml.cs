using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Autodesk.DataExchange;

namespace SampleConnector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string SingleInstanceMutexName = "Global\\Autodesk.SampleConnector.SingleInstance";
        private static Mutex singleInstanceMutex;
        private SampleHostWindow hostWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "Sample Connector is already running.\n\nClose the existing host window and Connector UI before starting another instance.",
                    "Sample Connector",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                this.Shutdown();
                return;
            }

            this.DispatcherUnhandledException += this.Application_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += this.TaskScheduler_UnobservedTaskException;

            this.hostWindow = new SampleHostWindow();
            this.hostWindow.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            this.hostWindow?.Destroy();
            singleInstanceMutex?.ReleaseMutex();
            singleInstanceMutex?.Dispose();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}",
                "Sample Connector",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
        }
    }
}
