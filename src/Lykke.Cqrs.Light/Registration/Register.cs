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
            var details = new DefaultRoutingDetails();
            detailsSetter.Invoke(details);
            return new DefaultRoutingRegistration(details);
        }

        public static IRegistration Context(string contextName, Action<ContextDetails> detailsSetter)
        {
            var details = new ContextDetails(contextName);
            detailsSetter.Invoke(details);
            return new ContextRegistration(details);
        }

        public static IRegistration Saga<T>(string contextName, Action<SagaDetails> detailsSetter)
        {
            var details = new SagaDetails(contextName, typeof(T));
            detailsSetter.Invoke(details);
            return new SagaRegistration(details);
        }

        public static IRegistration Saga<T>(IEventHandler<T> saga, string contextName, Action<SagaDetails> detailsSetter)
        {
            var details = new SagaDetails(contextName, saga);
            detailsSetter.Invoke(details);
            return new SagaRegistration(details);
        }
    }
}
