using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using JimBobBennett.JimLib.Xamarin.Network;
using JimBobBennett.RestAndRelaxForPlex.Connection;

namespace JimBobBennett.RestAndRelaxForPlex
{
    public static class ContainerRegistration
    {
        public static void OnInitialize(ContainerBuilder builder, string tmdbApiKey)
        {
            builder.RegisterType<MyPlexConnection>().As<IMyPlexConnection>().SingleInstance();
            builder.RegisterType<ConnectionManager>().As<IConnectionManager>().SingleInstance();
            builder.RegisterType<TheTvdbConnection>().As<ITheTvdbConnection>().SingleInstance();

            builder.RegisterType<TMDbConnection>().As<ITMDbConnection>().WithParameters(new List<Parameter>
            {
                new ResolvedParameter((p, c) => p.Position == 0, (p, c) => c.Resolve<IRestConnection>()),
                new PositionalParameter(1, tmdbApiKey)
            }).SingleInstance();
        }
    }
}
