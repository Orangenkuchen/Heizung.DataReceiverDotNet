namespace Heizung.DataRecieverDotNet.HostedService.SerialHeaterData
{
    public class SerialHeaterDataServiceConfiguration
    {
        #region ctor
        /// <summary>
        /// Intialisiert die Klasse
        /// </summary>
        /// <param name="portLocation">Der Ort an dem der SerialPort ist (z.B.:COM1, COM2, /dev/ttyUSB0, /dev/ttyUSB1, ...)</param>
        /// <param name="bautRate">Die Bautrate vom Seriellen Port</param>
        public SerialHeaterDataServiceConfiguration(string portLocation, uint bautRate = 57600)
        {
            this.PortLocation = portLocation;
            this.BautRate = bautRate;
        }
        #endregion

        #region PortLocation
        /// <summary>
        /// Der Ort an dem der SerialPort ist
        /// </summary>
        /// <value>z.B.:COM1, COM2, /dev/ttyUSB0, /dev/ttyUSB1, ...</value>
        public string PortLocation { get; set; }
        #endregion

        #region BautRate
        /// <summary>
        /// Die BautRate vom Seriellen Port
        /// </summary>
        /// <value>z.B.: 57600</value>
        public uint BautRate { get; set; }
        #endregion
    }
}