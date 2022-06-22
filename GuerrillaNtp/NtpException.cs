// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents errors that occur in SNTP packets or during SNTP operation.
    /// </summary>
    public class NtpException : Exception
    {
        internal NtpException(string message)
            : base(message)
        {
        }
    }
}
