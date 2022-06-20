// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GuerrillaNtp {
    internal static partial class TaskExtensions {

        public static ConfiguredValueTaskAwaitable DefaultAwait(this ValueTask This) {
            return This.ConfigureAwait(__ConfigureAwait);
        }

        public static ConfiguredValueTaskAwaitable<T> DefaultAwait<T>(this ValueTask<T> This) {
            return This.ConfigureAwait(__ConfigureAwait);
        }
    }
}
