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
        [Obsolete("Use extension without ILog with support of ILogFactory")]
        public static void RegisterCqrs(
            this ContainerBuilder containerBuilder,
            IMessagingEngine messagingEngine,
            ILog log,
            IEndpointResolver defaultEndpointResolver,
            params IRegistration[] registrations)
        {
            RegisterCqrs(
                containerBuilder,
                messagingEngine,
                log,
                null,
                null,
                defaultEndpointResolver,
                registrations);
        }

        [PublicAPI]
        [Obsolete("Use extension without ILog with support of ILogFactory")]
        public static void RegisterCqrs(
            this ContainerBuilder containerBuilder,
            IMessagingEngine messagingEngine,
            ILog log,
            TimeSpan? failedCommandRetryDelay,
            TimeSpan? failedEventRetryDelay,
            IEndpointResolver defaultEndpointResolver,
            params IRegistration[] registrations)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));
            if (messagingEngine == null)
                throw new ArgumentNullException(nameof(messagingEngine));
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            if (defaultEndpointResolver == null)
                throw new ArgumentNullException(nameof(defaultEndpointResolver));

            containerBuilder.Register(ctx =>
                {
                    var engine = new CqrsEngine(
                        messagingEngine,
                        defaultEndpointResolver,
                        log,
                        failedCommandRetryDelay,
                        failedEventRetryDelay,
                        registrations);
                    return engine;
                })
                .As<ICqrsEngine>()
                .As<ICommandSender>()
                .SingleInstance();
        }

        [PublicAPI]
        public static void RegisterCqrs(
            this ContainerBuilder containerBuilder,
            IMessagingEngine messagingEngine,
            IEndpointResolver defaultEndpointResolver,
            params IRegistration[] registrations)
        {
            RegisterCqrs(
                containerBuilder,
                messagingEngine,
                null,
                null,
                defaultEndpointResolver,
                registrations);
        }

        [PublicAPI]
        public static void RegisterCqrs(
            this ContainerBuilder containerBuilder,
            IMessagingEngine messagingEngine,
            TimeSpan? failedCommandRetryDelay,
            TimeSpan? failedEventRetryDelay,
            IEndpointResolver defaultEndpointResolver,
            params IRegistration[] registrations)
        {
            if (containerBuilder == null)
                throw new ArgumentNullException(nameof(containerBuilder));
            if (messagingEngine == null)
                throw new ArgumentNullException(nameof(messagingEngine));
            if (defaultEndpointResolver == null)
                throw new ArgumentNullException(nameof(defaultEndpointResolver));

            containerBuilder.Register(ctx =>
                {
                    var logFactory = ctx.Resolve<ILogFactory>();
                    var engine = new CqrsEngine(
                        messagingEngine,
                        defaultEndpointResolver,
                        logFactory,
                        failedCommandRetryDelay,
                        failedEventRetryDelay,
                        registrations);
                    return engine;
                })
                .As<ICqrsEngine>()
                .As<ICommandSender>()
                .SingleInstance();
        }
    }
}
