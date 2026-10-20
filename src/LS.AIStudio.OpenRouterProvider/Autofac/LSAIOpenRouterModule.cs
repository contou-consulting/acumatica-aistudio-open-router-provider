using Autofac;
using LS.AIStudio.OpenRouterProvider.ChatClients;
using PX.AIStudio.Services.Core.Connection;

namespace LS.AIStudio.OpenRouterProvider.Autofac
{
    public class LSAIOpenRouterModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LSAIOpenRouterChatClientFactory>().AsSelf().SingleInstance();
            builder.RegisterInstance(LSAIOpenRouterChatClientFactory.Metadata).Keyed<ConnectionMetadata>("direct");
        }
    }
}
