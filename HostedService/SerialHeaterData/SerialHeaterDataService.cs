namespace Heizung.DataRecieverDotNet.HostedService.SerialHeaterData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Ports;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service welcher die Serielle Schnittstelle der Heizung ausliest
    /// </summary>
    public class SerialHeaterDataService : IHostedService
    {
        #region fields
        /// <summary>
        /// Der serielle Port welcher verwendet werden soll
        /// </summary>
        private SerialPort serialPort;

        /// <summary>
        /// Service für die Lognachrichten
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Stream für die Daten aus dem Seriellen Port
        /// </summary>
        private BufferedStream serialPortStream;

        /// <summary>
        /// Liste mit Funktion welche beim Dispose aufgerufen weden sollen.
        /// </summary>
        private IList<Action> disposeFunctions;
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert die Klasse
        /// </summary>
        /// <param name="logger">Service für die Lognachrichten</param>
        /// <exception cref="ArgumentNullException">Wird geworfen, wenn serviceConfiguration null ist</exception>
        /// <exception cref="ArgumentException">Wird geworfen, wenn serviceConfiguration.PortLocation null oder whitespace ist</exception>
        public SerialHeaterDataService(ILogger<SerialHeaterDataService> logger, IConfiguration configuration)
        {
            var serialPortLocation = configuration.GetValue<string>("SerialPort:Location", null);
            if (serialPortLocation == null)
            {
                throw new ArgumentNullException("The configuration file must have the location of the serial port set in 'SerialPort.Loction'");
            }

            var serialPortBautRate = configuration.GetValue<int>("SerialPort:BautRate", 0);
            if (serialPortBautRate <= 0)
            {
                throw new ArgumentException("The configuration file must have the bautrate of the serial port set in 'SerialPort.BautRate'. It also needs to be > 0");
            }

            this.disposeFunctions = new List<Action>();
            this.logger = logger;

            this.serialPort = new SerialPort(serialPortLocation, serialPortBautRate);
        }
        #endregion

        #region StartAsync
        /// <summary>
        /// Wird ausgelöst, wenn der Anwendungshost bereit ist, den Dienst zu starten 
        /// </summary>
        /// <param name="cancellationToken">Gibt an, dass der Startprozess abgebrochen wurde</param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                this.logger.LogInformation("SerialHeaterDataService starting...");

                if (cancellationToken.IsCancellationRequested == false)
                {
                    this.Connect();

                    this.logger.LogInformation("SerialHeaterDataService started");
                }
                else
                {
                    this.logger.LogInformation("SerialHeaterDataService start cancled");
                }
            });
        }
        #endregion
        
        #region StopAsync
        /// <summary>
        /// Wird ausgelöst, wenn der Anwendungshost ein ordnungsgemäßes Herunterfahren ausführt
        /// </summary>
        /// <param name="cancellationToken">Gibt an, dass der Vorgang zum Herunterfahren nicht mehr ordnungsgemäß sein sollte</param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => 
            {
                this.logger.LogInformation("SerialHeaterDataService stopping...");
                
                this.Disconnect();

                this.logger.LogInformation("SerialHeaterDataService stopped...");
            });
        }
        #endregion

        #region Connect
        /// <summary>
        /// Stellt die Verbindung zum Seriellen Port her und initialisiert den BufferStream
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Kann verbinden mit dem SerialPort geworfen werden</exception>
        /// <exception cref="ArgumentOutOfRangeException">Kann verbinden mit dem SerialPort geworfen werden</exception>
        /// <exception cref="ArgumentException">Kann verbinden mit dem SerialPort geworfen werden</exception>
        /// <exception cref="System.IO.IOException">Kann verbinden mit dem SerialPort geworfen werden</exception>
        /// <exception cref="InvalidOperationException">Kann verbinden mit dem SerialPort geworfen werden</exception>
        private void Connect()
        {
            this.logger.LogTrace("Connecting to SerialPort ...");
            this.serialPort.Open();
            this.logger.LogTrace("SerialPort connected");
            
            this.serialPort.DataReceived += this.HandleSerialDataReceived;
        }
        #endregion

        #region Disconnect
        /// <summary>
        /// Trennt die Vebindung zum Seriellen Port (wenn verbunden)
        /// </summary>
        private void Disconnect()
        {
            this.logger.LogTrace("Disconnecting from SerialPort ...");

            this.serialPort.DataReceived -= this.HandleSerialDataReceived;

            this.serialPort.Close();
            this.serialPortStream.Close();
            this.serialPortStream.Dispose();

            this.logger.LogTrace("Discconeted from SerialPort");
        }
        #endregion

        #region HandleSerialDataReceived
        /// <summary>
        /// Wird aufgerufen, wenn am seriellen Port neue Daten angekommen sind
        /// </summary>
        /// <param name="sender">Der serielle Port von dem das Event kommt</param>
        /// <param name="eventArgs"></param>
        private void HandleSerialDataReceived(object sender, SerialDataReceivedEventArgs eventArgs)
        {
            var serialPort = (SerialPort)sender;

            if (serialPort.IsOpen)
            {
                var newLine = serialPort.ReadLine();
                
                this.logger.LogTrace(newLine);
            }
        }
        #endregion
    }
}