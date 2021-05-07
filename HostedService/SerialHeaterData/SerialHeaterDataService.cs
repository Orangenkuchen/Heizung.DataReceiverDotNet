namespace Heizung.DataRecieverDotNet.HostedService.SerialHeaterData
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO.Pipelines;
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
        private Pipe serialPortPipe;

        /// <summary>
        /// Liste mit Funktion welche beim Dispose aufgerufen weden sollen.
        /// </summary>
        private IList<Action> disposeFunctions;

        /// <summary>
        /// Gibt an, ob der Service heruntergefahren werden soll
        /// </summary>
        private CancellationTokenSource shutdownCancellationTokenSource;

        /// <summary>
        /// Gibt an, ob der Service unsauber heruntergefahren werden soll
        /// </summary>
        private CancellationTokenSource killServiceTokenSource;
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
            this.shutdownCancellationTokenSource = new CancellationTokenSource();
            this.killServiceTokenSource = new CancellationTokenSource();

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
            this.shutdownCancellationTokenSource.Cancel();
            cancellationToken.Register(() => 
            {
                this.killServiceTokenSource.Cancel();
            });

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

            this.ProcessLinesAsync(this.serialPort, this.shutdownCancellationTokenSource.Token);

            this.logger.LogTrace("SerialPort connected");
            
            //this.serialPort.DataReceived += this.HandleSerialDataReceived;
            this.serialPort.DataReceived += (sender, e) => 
            {
                var d = "d";
                //this.logger.Verbose(sender);
            };
        }
        #endregion

        #region Disconnect
        /// <summary>
        /// Trennt die Vebindung zum Seriellen Port (wenn verbunden)
        /// </summary>
        private void Disconnect()
        {
            this.logger.LogTrace("Disconnecting from SerialPort ...");

            this.serialPort.Close();

            this.logger.LogTrace("Discconeted from SerialPort");
        }
        #endregion

        #region ProcessLinesAsync
        /// <summary>
        /// Startet das Befüllen und auslesen der Pipe anhand der Daten vom SerialPort. (Läuft bis cancellationToken abgebrochen wird)
        /// </summary>
        /// <param name="serialPort">Der Serielle Port welcher ausgelesen werden soll</param>
        /// <param name="cancellationToken">Anhand dieses Tokens wird die Funktion abgebrochen.</param>
        /// <returns>Wird beenden wenn der Cancellationtoken abgebrochen wird</returns>
        private async Task ProcessLinesAsync(SerialPort serialPort, CancellationToken cancellationToken)
        {
            var pipe = new Pipe();

            Task writing = this.FillPipeAsync(serialPort, pipe.Writer, cancellationToken);
            Task reading = this.ReadPipeAsync(pipe.Reader, cancellationToken);

            await Task.WhenAll(reading, writing);
        }
        #endregion

        #region FillPipeAsync
        /// <summary>
        /// Füllt die den Pipewriter anhand vom SeriellenPort. (Wird erst beenden, wenn cancellationToken abgebrochen wird)
        /// </summary>
        /// <param name="serialPort">Der serielle Port von dem die Daten genommen werden sollen</param>
        /// <param name="writer">Der Writer von der Pipe, welche beschrieben werden soll</param>
        /// <param name="cancellationToken">Anhand dieses Tokens wird die Funktion abgebrochen.</param>
        /// <returns>Wird erst beenden, wenn cancellationToken abgebrochen wird</returns>
        private async Task FillPipeAsync(SerialPort serialPort, PipeWriter writer, CancellationToken cancellationToken)
        {
            const int minimumBufferSize = 750;
            var keepRunning = true;

            cancellationToken.Register(() => keepRunning = false);

            while (keepRunning)
            {
                // Allocate at least 750 bytes from the PipeWriter.
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);

                try
                {
                    int bytesRead = await serialPort.BaseStream.ReadAsync(memory, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    // Tell the PipeWriter how much was read from the Socket.
                    writer.Advance(bytesRead);
                }
                catch (Exception exception)
                {
                    this.logger.LogError(exception, "An exception accoured while writing to the pipline from serialport {0}", serialPort.PortName);
                    break;
                }

                // Make the data available to the PipeReader.
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // By completing PipeWriter, tell the PipeReader that there's no more data coming.
            await writer.CompleteAsync();
        }
        #endregion

        #region ReadPipeAsync
        /// <summary>
        /// Liest die Pipe aus, parsed die Daten und gibt diese weiter an die API. (Wird erst beenden, wenn cancellationToken abgebrochen wird)
        /// </summary>
        /// <param name="reader">Der Reader von der Pipe, welche ausgelesen werden soll</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Wird erst beenden, wenn cancellationToken abgebrochen wird</returns>
        private async Task ReadPipeAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            var keepRunning = true;
            cancellationToken.Register(() => keepRunning = false);

            while (keepRunning)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (this.TryReadMessage(ref buffer, out ReadOnlySequence<byte> line, "\n"))
                {
                    // Process the line.
                    //this.ProcessLine(line);
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
        }
        #endregion

        #region TryReadMessage
        /// <summary>
        /// Versucht die Message im Buffer zu lesen. Dafür wird nach dem Ende der Message gesucht. 
        /// </summary>
        /// <param name="buffer">Der Puffer, welcher durchsucht werden soll</param>
        /// <param name="messageEnd">Der String nach dem gesucht werden soll</param>
        /// <param name="messageLine">Die Message welche aus dem Puffer ermittelt wurde</param>
        /// <returns>Gibt zurück, ob das Message end enthalten ist</returns>
        private bool TryReadMessage (ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messageLine, string messageEnd)
        {
            var result = false;
            // Look for a EOL in the buffer.
            SequencePosition? position = buffer.PositionOf((byte)'\n');
            
            if (buffer.Length > 7)
            {
                var bufferEndSlice = buffer.Slice(buffer.Length - 7 + 1, 7);
                
                if (bufferEndSlice.ToString() == ";22;1;%;")
                {
                    result = true;
                }
            }

            messageLine = new ReadOnlySequence<byte>();

            return result;
        }
        #endregion
    }
}