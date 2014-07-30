using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public interface IMyPlexConnection
    {
        PlexUser User { get; }
        ReadOnlyObservableCollection<Device> Devices { get; }
        ReadOnlyObservableCollection<Device> Servers { get; }
        ReadOnlyObservableCollection<Device> Players { get; }
        bool IsConnected { get; }
        Task ConnectAsync(string username, string password);
        Task RefreshContainerAsync();
        event EventHandler DevicesUpdated;
        Task<IEnumerable<IPlexServerConnection>> CreateServerConnectionsAsync();
    }
}