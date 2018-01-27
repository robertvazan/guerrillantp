// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
namespace GuerrillaNtp
{
    /// <summary>
    /// A set of values indicating which, if any, warning should be sent due to an impending leap second
    /// </summary>
    public enum NtpLeapIndicator
    {
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
        /// Server clock is unsynchronized
        /// </summary>
        AlarmCondition
    }
}