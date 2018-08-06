using Autofac;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Messaging.Contract;
using System;

namespace Lykke.Cqrs.Light
{
    public static class AutofacExtensions
    {
        [PublicAPI]
        public static void RegisterCqrs(
            this ContainerBuilder containerBuilder,
            IEndpointResolver defaultEndpointResolver,
            params IRegistration[] registrations)
        {
            RegisterCqrs(
                containerBuilder,
                null,
                null,
                defaultEndpointResolver,
                registrations);
        }

        [PublicAPI]
        public static void RegisterCqrs(
            this ContainerBuilder containerBuilder,
            TimeSpan? failedCommandRetryDelay,
            TimeSpan? failedEventRetryDelay,
            IEndpointResolver defaultEndpointResolver,
            params IRegistration[] registrations)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));
            if (defaultEndpointResolver == null)
                throw new ArgumentNullException(nameof(defaultEndpointResolver));

            containerBuilder.Register(ctx =>
                {
                    var messagingEngine = ctx.Resolve<IMessagingEngine>();
                    if (messagingEngine == null)
                        throw new InvalidOperationException($"{nameof(IMessagingEngine)} is not registered in Autofac container");
                    if (ctx.TryResolve<ILogFactory>(out ILogFactory logFactory))
                        return new CqrsEngine(
                            messagingEngine,
                            defaultEndpointResolver,
                            logFactory,
                            failedCommandRetryDelay,
                            failedEventRetryDelay,
                            registrations);
                    var log = ctx.Resolve<ILog>();
                    if (log == null)
                        throw new InvalidOperationException($"Neither {nameof(ILogFactory)}, nor {nameof(ILog)} was registered in Autofac container");
                    return new CqrsEngine(
                        messagingEngine,
                        defaultEndpointResolver,
                        log,
                        failedCommandRetryDelay,
                        failedEventRetryDelay,
                        registrations);
                })
                .As<ICqrsEngine>()
                .As<ICommandSender>()
                .SingleInstance();
        }
    }
}
