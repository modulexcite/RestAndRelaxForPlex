using System.Collections.Generic;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public interface IConnectionManager
    {
        Task<bool> ConnectToMyPlexAsync(string username, string password);
        Task<bool> ConnectToServerAsync(string uri);
        bool IsConnectedToMyPlex { get; }
        Task ConnectAsync();
        Task<Video> RefreshVideo(Video video);

        IEnumerable<Video> NowPlaying { get; }
    }
}