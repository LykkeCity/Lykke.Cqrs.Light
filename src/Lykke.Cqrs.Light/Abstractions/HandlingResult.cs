using JetBrains.Annotations;
using System;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public class HandlingResult
    {
        internal bool Retry { get; }
        internal TimeSpan? RetryDelay { get; }

        internal HandlingResult(bool retry, TimeSpan? retryDelay = null)
        {
            Retry = retry;
            RetryDelay = retryDelay;
        }

        public static HandlingResult Ok()
        {
            return new HandlingResult(false);
        }

        public static HandlingResult Fail(TimeSpan? retryDelay = null)
        {
            return new HandlingResult(true, retryDelay);
        }
    }
}
