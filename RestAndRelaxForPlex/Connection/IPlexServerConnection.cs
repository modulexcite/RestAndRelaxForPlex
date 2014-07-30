using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public interface IPlexServerConnection : INotifyPropertyChanged
    {
        Device Device { get; }
        string ConnectionUri { get; }
        bool IsOnLine { get; }
        string Platform { get; }
        string MachineIdentifier { get; }
        string Name { get; }

        ReadOnlyObservableCollection<Video> NowPlaying { get; }
        ReadOnlyObservableCollection<Server> Clients { get; }
        PlexUser User { get; }

        Task PauseVideoAsync(Video video);
        Task PlayVideoAsync(Video video);
        Task StopVideoAsync(Video video);
    }
}