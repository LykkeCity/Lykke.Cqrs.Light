using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using System;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public static class Register
    {
        public static IRegistration DefaultRouting(Action<DefaultRoutingDetails> detailsSetter)
        {
            if (detailsSetter == null)
                throw new ArgumentNullException();

            var details = new DefaultRoutingDetails();
            detailsSetter.Invoke(details);
            return new DefaultRoutingRegistration(details);
        }

        public static IRegistration Context(string contextName, Action<ContextDetails> detailsSetter)
        {
            if (contextName == null)
                throw new ArgumentNullException(nameof(contextName));
            if (detailsSetter == null)
                throw new ArgumentNullException(nameof(detailsSetter));

            var details = new ContextDetails(contextName);
            detailsSetter.Invoke(details);
            return new ContextRegistration(details);
        }

        public static IRegistration Saga<T>(string contextName, Action<SagaDetails> detailsSetter)
        {
            if (contextName == null)
                throw new ArgumentNullException(nameof(contextName));
            if (detailsSetter == null)
                throw new ArgumentNullException(nameof(detailsSetter));

            var details = new SagaDetails(contextName, typeof(T));
            detailsSetter.Invoke(details);
            return new SagaRegistration(details);
        }

        public static IRegistration Saga(
            object saga,
            string contextName,
            Action<SagaDetails> detailsSetter)
        {
            if (saga == null)
                throw new ArgumentNullException(nameof(saga));
            if (contextName == null)
                throw new ArgumentNullException(nameof(contextName));
            if (detailsSetter == null)
                throw new ArgumentNullException(nameof(detailsSetter));

            var details = new SagaDetails(contextName, saga);
            detailsSetter.Invoke(details);
            return new SagaRegistration(details);
        }
    }
}
