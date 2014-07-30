using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.Connection;
using JimBobBennett.JimLib.Collections;
using JimBobBennett.JimLib.Extensions;
using JimBobBennett.JimLib.Mvvm;
using JimBobBennett.JimLib.Xml;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace JimBobBennett.RestAndRelaxForPlex.PlexObjects
{
    public class Video : PlexObjectBase<Video>
    {
        private Player _player;
        private string _guid;

        [NotifyPropertyChangeDependency("Key")]
        [NotifyPropertyChangeDependency("HasImdbLink")]
        [NotifyPropertyChangeDependency("HasTvdbLink")]
        [NotifyPropertyChangeDependency("UriSource")]
        [NotifyPropertyChangeDependency("Uri")]
        [NotifyPropertyChangeDependency("SchemeUri")]
        [NotifyPropertyChangeDependency("ImdbId")]
        [NotifyPropertyChangeDependency("TvdbId")]
        public string Guid
        {
            get { return _guid; }
            set
            {
                if (_guid == value) return;

                _guid = value;
                BuildIds();
            }
        }

        private void BuildIds()
        {
            if (Guid.StartsWith("com.plexapp.agents.imdb://", StringComparison.OrdinalIgnoreCase))
            {
                var id = Guid.Replace("com.plexapp.agents.imdb://", "");
                var end = id.IndexOf("?", StringComparison.Ordinal);
                if (end > 1)
                    id = id.Substring(0, end);

                ImdbId = id;
            }
            else
                ImdbId = null;

            if (Guid.StartsWith("com.plexapp.agents.thetvdb://"))
            {
                var bit = Guid.Replace("com.plexapp.agents.thetvdb://", "");
                var bits = bit.Split('/');

                TvdbId = bits[0];
            }
            else
                TvdbId = null;
        }

        public string Title { get; set; }
        public string Summary { get; set; }
        public string Tagline { get; set; }
        public string Art { get; set; }

        [NotifyPropertyChangeDependency("VideoThumb")]
        public string Thumb { get; set; }

        [NotifyPropertyChangeDependency("VideoThumb")]
        public string ParentThumb { get; set; }

        [NotifyPropertyChangeDependency("VideoThumb")]
        public string GrandParentThumb { get; set; }

        public string VideoThumb
        {
            get
            {
                if (!ParentThumb.IsNullOrEmpty())
                    return ParentThumb;
                if (!GrandParentThumb.IsNullOrEmpty())
                    return GrandParentThumb;
                return Thumb;
            }
        }

        [NotifyPropertyChangeDependency("Progress")]
        public double ViewOffset { get; set; }

        [NotifyPropertyChangeDependency("Progress")]
        public double Duration { get; set; }

        public double Progress
        {
            get { return Duration <= 0 ? 0 : ViewOffset/Duration; }
        }

        public string PlayerName { get { return Player.Title; } }
        
        public int Year { get; set; }
        public PlayerState State { get { return Player.State; } }

        public Player Player
        {
            get { return _player; }
            set
            {
                if (Equals(Player, value)) return;

                if (_player != null)
                    _player.PropertyChanged -= PlayerOnPropertyChanged;

                _player = value;

                if (_player != null)
                    _player.PropertyChanged += PlayerOnPropertyChanged;
            }
        }

        private void PlayerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == this.ExtractPropertyInfo(() => _player.State).Name)
                RaisePropertyChanged(() => State);

            if (e.PropertyName == this.ExtractPropertyInfo(() => _player.Title).Name)
                RaisePropertyChanged(() => PlayerName);
        }

        public ObservableCollectionEx<Role> Roles { get; set; }
        public ObservableCollectionEx<Genre> Genres { get; set; }
        public ObservableCollectionEx<Producer> Producers { get; set; }
        public ObservableCollectionEx<Writer> Writers { get; set; }
        public ObservableCollectionEx<Director> Directors { get; set; }

        public string UriSource
        {
            get
            {
                if (HasImdbLink)
                    return "IMDB";

                if (HasTvdbLink)
                    return "TheTVDB";

                return null;
            }
        }

        public Uri Uri
        {
            get
            {
                if (HasImdbLink)
                    return new Uri("http://www.imdb.com/title/" + ImdbId);

                if (HasTvdbLink)
                    return new Uri("http://www.thetvdb.com/?tab=series&id=" + TvdbId);

                return null;
            }
        }

        public string ImdbId { get; set; }
        public string TvdbId { get; set; }

        public bool HasTvdbLink
        {
            get { return !TvdbId.IsNullOrEmpty(); }
        }

        public bool HasImdbLink
        {
            get { return !ImdbId.IsNullOrEmpty(); }
        }

        public Uri SchemeUri
        {
            get { return !HasImdbLink ? null : new Uri(string.Format("imdb:///title/{0}/", ImdbId)); }
        }

        [XmlNameMapping("grandparentTitle")]
        public string Show { get; set; }

        [XmlNameMapping("parentIndex")]
        public int SeasonNumber { get; set; }

        [XmlNameMapping("index")]
        public int EpisodeNumber { get; set; }

        public VideoType Type { get; set; }

        protected override bool OnUpdateFrom(Video newValue, List<string> updatedPropertyNames)
        {
            var isUpdated = UpdateValue(() => Title, newValue, updatedPropertyNames);
            isUpdated = UpdateValue(() => Summary, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => Guid, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => Art, newValue, updatedPropertyNames) | isUpdated;
            
            var thumbUpdated = UpdateValue(() => Thumb, newValue, updatedPropertyNames);
            thumbUpdated = UpdateValue(() => ParentThumb, newValue, updatedPropertyNames) | thumbUpdated;
            thumbUpdated = UpdateValue(() => GrandParentThumb, newValue, updatedPropertyNames) | thumbUpdated;

            if (thumbUpdated)
            {
                isUpdated = true;
                ThumbImageSource = null;
            }

            isUpdated = UpdateValue(() => ViewOffset, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => Duration, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => Show, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => SeasonNumber, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => EpisodeNumber, newValue, updatedPropertyNames) | isUpdated;
            isUpdated = UpdateValue(() => Type, newValue, updatedPropertyNames) | isUpdated;

            isUpdated = Player.UpdateFrom(newValue.Player) | isUpdated;
            isUpdated = Roles.UpdateToMatch(newValue.Roles, r => r.Key, (r1, r2) => r1.UpdateFrom(r2)) | isUpdated;
            isUpdated = Genres.UpdateToMatch(newValue.Genres, r => r.Key, (r1, r2) => r1.UpdateFrom(r2)) | isUpdated;
            isUpdated = Producers.UpdateToMatch(newValue.Producers, r => r.Key, (r1, r2) => r1.UpdateFrom(r2)) | isUpdated;
            isUpdated = Writers.UpdateToMatch(newValue.Writers, r => r.Key, (r1, r2) => r1.UpdateFrom(r2)) | isUpdated;
            isUpdated = Directors.UpdateToMatch(newValue.Directors, r => r.Key, (r1, r2) => r1.UpdateFrom(r2)) | isUpdated;

            return isUpdated;
        }

        public override string Key
        {
            get { return Guid; }
        }

        public ImageSource ThumbImageSource { get; internal set; }

        internal IPlexServerConnection PlexServerConnection { get; set; }
        public string ConnectionUri { get { return PlexServerConnection == null ? null : PlexServerConnection.ConnectionUri; } }

        public async Task PlayAsync()
        {
            if (PlexServerConnection != null)
                await PlexServerConnection.PlayVideoAsync(this);
        }

        public async Task PauseAsync()
        {
            if (PlexServerConnection != null)
                await PlexServerConnection.PauseVideoAsync(this);
        }

        public async Task StopAsync()
        {
            if (PlexServerConnection != null)
                await PlexServerConnection.StopVideoAsync(this);
        }
    }
}
