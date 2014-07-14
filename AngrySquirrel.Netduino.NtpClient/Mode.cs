namespace AngrySquirrel.Netduino.NtpClient
{
    /// <summary>
    /// A set of values indicating how an NTP server can be associated with another
    /// </summary>
    public enum Mode
    {
        /// <summary>
        /// The NTP host is configured as a client
        /// </summary>
        Client = 3, 

        /// <summary>
        /// The NTP host is configured as a server
        /// </summary>
        Server = 4, 
    }
}