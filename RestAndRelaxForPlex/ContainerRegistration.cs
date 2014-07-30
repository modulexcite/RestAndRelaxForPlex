using Autofac;
using JimBobBennett.RestAndRelaxForPlex.Connection;

namespace JimBobBennett.RestAndRelaxForPlex
{
    public static class ContainerRegistration
    {
        public static void OnInitialize(ContainerBuilder builder)
        {
            builder.RegisterType<MyPlexConnection>().As<IMyPlexConnection>().SingleInstance();
            builder.RegisterType<ConnectionManager>().As<IConnectionManager>().SingleInstance();
            builder.RegisterType<TheTvdbConnection>().As<ITheTvdbConnection>().SingleInstance();
        }
    }
}
