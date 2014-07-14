namespace AngrySquirrel.Netduino.NtpClient
{
    /// <summary>
    /// A set of values indicating which, if any, warning should be sent due to an impending leap second
    /// </summary>
    public enum LeapIndicator
    {
        /// <summary>
        /// The lack of a leap second warning indicator
        /// </summary>
        None = -1, 

        /// <summary>
        /// No warning should be sent
        /// </summary>
        NoWarning, 

        /// <summary>
        /// The last minute of the month has 61 seconds
        /// </summary>
        LastMinuteHas61Seconds, 

        /// <summary>
        /// The last minute of the month has 59 seconds
        /// </summary>
        LastMinuteHas59Seconds, 

        /// <summary>
        /// The clock is unsynchronized so it is unknown if there exists an impending leap second
        /// </summary>
        Unknown
    }
}