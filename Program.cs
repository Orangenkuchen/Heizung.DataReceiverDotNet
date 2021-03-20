namespace Heizung.DataRecieverDotNet
{
    using System;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;

    public class Program
    {
        #region Main
        /// <summary>
        /// Die Funktion welche beim Programmstart aufgerufen wird
        /// </summary>
        /// <param name="args">Die Argumente welche an das Programm gegeben werden</param>
        public static void Main(string[] args)
        {
            var logfilePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, 
                "Log.txt");

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(logfilePath)
                .CreateLogger();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleUnhandledException);

            try
            {
                Log.Information("Starte das Programm");
                CreateHostBuilder(args).Build().Run();
            }
            catch(Exception exception)
            {
                Log.Fatal(exception, "Fehler beim Starten des Programms");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        #endregion

        #region CreateHostBuilder
        /// <summary>
        /// Konfiguriert den Webserver
        /// </summary>
        /// <param name="args">Die Argumente welche dem Webserver übergeben werden sollen</param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog() // Überschreibt das Logging mit Serilog
                .UseSystemd()
                .ConfigureServices((service) =>
                {
                    service.AddHostedService<SerialHeaterDataService>();
                }
            );
        }
        #endregion

        #region HandleUnhandledException
        /// <summary>
        /// Wird aufgerufen, wenn eine Exception im Programm nicht abgefangen wird
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception exception = (Exception) args.ExceptionObject;

            Log.Error(exception, "Unhandled exception accourced");
        }
        #endregion
    }
}
