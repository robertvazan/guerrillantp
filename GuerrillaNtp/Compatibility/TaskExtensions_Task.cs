// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GuerrillaNtp
{
    internal static partial class TaskExtensions
    {
        const bool __ConfigureAwait = false;

        public static ConfiguredTaskAwaitable DefaultAwait(this Task This)
        {
            return This.ConfigureAwait(__ConfigureAwait);
        }

        public static ConfiguredTaskAwaitable<T> DefaultAwait<T>(this Task<T> This)
        {
            return This.ConfigureAwait(__ConfigureAwait);
        }

    }
}
