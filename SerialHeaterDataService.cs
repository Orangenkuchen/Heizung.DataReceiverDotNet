namespace Heizung.DataRecieverDotNet
{
    using System.IO.Ports;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Service welcher die Serielle Schnittstelle der Heizung ausliest
    /// </summary>
    public class SerialHeaterDataService : IHostedService
    {
        #region fields
        private SerialPort serialPort;
        #endregion

        #region ctor
        public SerialHeaterDataService()
        {
            var d = ";";
        }
        #endregion

        #region StartAsync
        /// <summary>
        /// Startet den Servicehost 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
        #endregion
        
        #region StopAsync
        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}