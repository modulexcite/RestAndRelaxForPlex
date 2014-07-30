using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.Connection;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.JimLib.Xamarin.Net45.Images;
using JimBobBennett.JimLib.Xamarin.Network;
using JimBobBennett.JimLib.Xamarin.Timers;
using JimBobBennett.JimLib.Xamarin.Win.Network;

namespace ConsoleTest
{
    static class Program
    {
        static void Main()
        {
            var restConnection = new RestConnection();

            var connectionManager = new ConnectionManager(new Timer(), new LocalServerDiscovery(),
                restConnection, new MyPlexConnection(restConnection), new TheTvdbConnection(restConnection),
                new ImageHelper(restConnection));

            Task.Factory.StartNew(async () =>
                {
                    ((INotifyCollectionChanged) connectionManager.NowPlaying).CollectionChanged += (s, e) =>
                        {
                            foreach (var video in connectionManager.NowPlaying)
                                WriteVideo(video);
                        };

                    Console.WriteLine("Connecting to MyPlex...");
                    await connectionManager.ConnectToMyPlexAsync(TestConstants.MyPlexUserName, TestConstants.MyPlexPassword);

                    Console.WriteLine("Connecting...");
                    await connectionManager.ConnectAsync();

                    Console.WriteLine("Waiting for videos...");
                });

            Console.WriteLine("Press any key to exit");

            Console.ReadKey();
        }

        private static void WriteVideo(Video video)
        {
            Console.WriteLine(video.Title);

            if (video.Type == VideoType.Episode)
            {
                Console.WriteLine("Show name: " + video.Show);
                Console.WriteLine("Season: " + video.SeasonNumber + ", Episode: " + video.EpisodeNumber);
            }
            else
                Console.WriteLine("Type: " + video.Type);

            Console.WriteLine("Playing on " + video.Player.Title);
            Console.WriteLine("Thumb: " + video.VideoThumb);
            Console.WriteLine("Links:");
            Console.WriteLine(video.Uri);
            Console.WriteLine(video.SchemeUri);
            Console.WriteLine("IMDB Id: " + video.ImdbId);
            Console.WriteLine("Tvdbv Id: " + video.TvdbId);
            Console.WriteLine(video.Player.State);

            if (video.Player.State == PlayerState.Playing)
                Console.WriteLine("Position: " + video.Progress);

            Console.WriteLine("Cast:");
            foreach (var role in video.Roles)
                Console.WriteLine(role.RoleName + ": " + role.Tag);

            //Console.WriteLine("Directors");
            //foreach (var director in video.Directors)
            //    Console.WriteLine(director.tag);
        }
    }
}
