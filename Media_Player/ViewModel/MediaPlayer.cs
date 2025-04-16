using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;


namespace Media_Player.ViewModel
{
    using BaseClass;
    using Media_Player.Model;
    using TagLib;

    public enum PlayMode { None, Video, Playlist };
    public class MediaPlayer : ViewModelBase
    {
        public MediaPlayer()
        {
            MediaElementVM = new MediaElementViewModel();
            addTrackWindow = null;
            playlist = null;
            MediaPlayMode = PlayMode.None;
            defaultCover = new BitmapImage(new Uri(@"/Media_Player;component/Resources/defaultcover.jpg", UriKind.Relative));
        }

        #region Properties

        public MediaElementViewModel MediaElementVM { get; set; }

        AddTrackWindow? addTrackWindow;

        BitmapImage defaultCover;

        private PlayMode playmode;
        public PlayMode MediaPlayMode
        {
            get
            {
                return playmode;
            }
            set
            {
                playmode = value;
                switch (value)
                {
                    case PlayMode.None:
                        MediaElementVM.MediaUri = null;
                        playlist = null;
                        Tracks = null;
                        SelectedTrack = null;
                        PlaylistName = "🎵 Playlista";
                        break;
                    case PlayMode.Video:
                        playlist = null;
                        Tracks = null;
                        SelectedTrack = null;
                        PlaylistName = "🎵 Playlista";
                        break;
                    case PlayMode.Playlist:
                        MediaElementVM.MediaUri = null;
                        PlaylistName = $"🎵 Playlista: {playlist!.Name}";
                        break;
                }
                onPropertyChanged(nameof(PlayMode));
            }
        }


        private string? playlistName;
        public string? PlaylistName
        {
            get
            {
                return playlistName;
            }
            set
            {
                playlistName = value;
                onPropertyChanged(nameof(PlaylistName));
            }
        }

        public Playlist? playlist;

        private ObservableCollection<Track>? tracks = null;
        public ObservableCollection<Track>? Tracks
        {
            get
            {
                return tracks;
            }
            set
            {
                tracks = value;
                onPropertyChanged(nameof(Tracks));
            }
        }

        private Track? selectedTrack;
        public Track? SelectedTrack
        {
            get
            {
                return selectedTrack;
            }
            set
            {
                selectedTrack = value;
                onPropertyChanged(nameof(SelectedTrack));
                if (SelectedTrack != null)
                {
                    MediaElementVM.MediaUri = new Uri(SelectedTrack.FilePath);
                    if (SelectedTrack.Artist != null && SelectedTrack.Artist != String.Empty)
                    {
                        MediaElementVM.MediaName = $"{SelectedTrack.Artist} - {SelectedTrack.TrackName}";
                    }
                    else
                    {
                        MediaElementVM.MediaName = SelectedTrack.TrackName;
                    }

                    if (SelectedTrack.CoverImage != null)
                    {
                        CoverImage = getCoverBitmap(SelectedTrack.CoverImage);
                    }
                    else
                    {

                        CoverImage = defaultCover;
                    }
                }
            }
        }

        private int? selectedIndex;
        public int? SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                onPropertyChanged(nameof(SelectedIndex));
            }
        }

        private BitmapImage? coverImage;
        public BitmapImage? CoverImage
        {
            get { return coverImage; }
            set
            {
                coverImage = value;
                onPropertyChanged(nameof(CoverImage));
            }
        }

        public EventHandler? GoToEndOfVideo;
        public EventHandler? GoToBeginningOfVideo;
        #endregion

        #region Methods
        private void openVideo()
        {
            try
            {
                MediaElementVM.OpenVideoFile();
                CoverImage = null;
                MediaPlayMode = PlayMode.Video;
            }
            catch (Exception ex)
            {
                MediaPlayMode = PlayMode.None;
                MessageBox.Show($"Błąd podczas otwierania wideo:\n{ex.Message}", "Błąd zapisu!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void createPlaylistFile()
        {
            try
            {
                Playlist? newPlaylist = PlaylistFileDialog.createFile();
                if (newPlaylist != null)
                {
                    playlist = newPlaylist;
                    playlist.loadFromFile();
                    Tracks = playlist.Tracks;
                    MediaPlayMode = PlayMode.Playlist;
                    MessageBox.Show("Pomyślnie utworzono nową playliste.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                playlist = null;
                PlaylistName = null;
                Tracks = null;
                MediaPlayMode = PlayMode.None;
                MessageBox.Show($"Błąd podczas tworzenia playlisty:\n{ex.Message}", "Błąd zapisu!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void openPlaylistFile()
        {
            try
            {
                Playlist? newPlaylist = PlaylistFileDialog.openFile();
                if (newPlaylist != null)
                {
                    playlist = newPlaylist;
                    playlist.loadFromFile();
                    Tracks = playlist.Tracks;
                    MediaPlayMode = PlayMode.Playlist;
                    MessageBox.Show("Pomyślnie otworzno playliste.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                playlist = null;
                PlaylistName = null;
                Tracks = null;
                MediaPlayMode = PlayMode.None;
                MessageBox.Show($"Błąd podczas otwierania playlisty:\n{ex.Message}", "Błąd odczytu!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void savePlaylist()
        {
            try
            {
                playlist!.save();
                MessageBox.Show("Pomyślnie zapisano playliste.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania playlisty:\n{ex.Message}", "Błąd zapisu!", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }
        private void openAddTrackWindow()
        {
            WindowContext.playlistShare = playlist;
            addTrackWindow = new AddTrackWindow("dodaj");
            addTrackWindow.ShowDialog();
            Tracks = playlist?.Tracks;
            WindowContext.playlistShare = null;
        }

        private void openEditTrackWindow()
        {
            WindowContext.playlistShare = playlist;
            WindowContext.selectedTrackShare = SelectedTrack;
            addTrackWindow = new AddTrackWindow("edytuj");
            addTrackWindow.ShowDialog();
            SelectedTrack = WindowContext.selectedTrackShare;
            Tracks = null;
            Tracks = playlist?.Tracks;
            WindowContext.playlistShare = null;
            WindowContext.selectedTrackShare = null;
        }

        private void deleteSelectedTrack()
        {
            var result = MessageBox.Show("Czy napewno chcesz usunąć utwór?", "Usuń utwór", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                playlist!.removeTrack(SelectedTrack!.Id);
                SelectedTrack = null;
            }

        }

        private void goToNextTrack()
        {
            if (MediaPlayMode == PlayMode.Playlist && playlist != null)
            {
                if ((SelectedIndex + 1) < playlist.Tracks.Count)
                {
                    SelectedIndex++;
                }
                else
                {
                    SelectedIndex = 0;
                }
            }
            else if (MediaPlayMode == PlayMode.Video)
            {
                if (GoToEndOfVideo != null)
                {
                    GoToEndOfVideo(this, EventArgs.Empty);
                }
            }
        }

        private void goToPreviousTrack()
        {
            if (MediaPlayMode == PlayMode.Playlist && playlist != null)
            {
                if ((SelectedIndex - 1) >= 0)
                {
                    SelectedIndex--;
                }
                else
                {
                    SelectedIndex = playlist.Tracks.Count - 1;
                }
            }
            else if (MediaPlayMode == PlayMode.Video)
            {
                if (GoToBeginningOfVideo != null)
                {
                    GoToBeginningOfVideo(this, EventArgs.Empty);
                }
            }
        }

        public void TrackEnded()
        {
            if (MediaPlayMode == PlayMode.Playlist && playlist != null)
            {
                goToNextTrack();
            }
        }

        public async void FetchAndDisplayLyricsForSelectedTrack()
        {
            if (SelectedTrack != null)
            {
                try
                {
                    var lyricsService = new LyricsService();
                    var lyrics = await lyricsService.FetchLyricsAsync(SelectedTrack!.Artist!, SelectedTrack!.TrackName!);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var lyricsWindow = new LyricsWindow(SelectedTrack!.Artist!, SelectedTrack!.TrackName!, lyrics);
                        lyricsWindow.Show();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas wczytywania tekstu utworu:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Proszę wybrać utwór.", "Nie wybrano utworu", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void shuffleTracks()
        {
            Random shuffle = new Random();
            int n = Tracks!.Count;
            while (n > 1)
            {
                n--;
                int k = shuffle.Next(n + 1);
                var value = Tracks[k];
                Tracks[k] = Tracks[n];
                Tracks[n] = value;
            }
            Tracks = null;
            Tracks = playlist!.Tracks;
        }

        private BitmapImage getCoverBitmap(IPicture picture)
        {
            MemoryStream ms = new MemoryStream(picture.Data.Data);
            ms.Seek(0, SeekOrigin.Begin);
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.EndInit();

            return bitmap;
        }

        private void showEqualizerWindow()
        {
            EqualizerWindow EqWindow = new EqualizerWindow(SelectedTrack.FilePath);
            EqWindow.Show();
        }

        #endregion

        #region Commands
        public ICommand OpenVideoFile
        {
            get
            {
                return new RelayCommand(execute => openVideo(), canExecute => true);
            }
        }

        public ICommand CreatePlaylist
        {
            get
            {
                return new RelayCommand(execute => createPlaylistFile(), canExecute => true);
            }
        }

        public ICommand OpenPlaylist
        {
            get
            {
                return new RelayCommand(execute => openPlaylistFile(), canExecute => true);
            }
        }

        public ICommand SavePlaylist
        {
            get
            {
                return new RelayCommand(execute => savePlaylist(), canExecute => (playlist != null));
            }
        }

        public ICommand AddNewTrack
        {
            get
            {
                return new RelayCommand(execute => openAddTrackWindow(), canExecute => (playlist != null));
            }
        }

        public ICommand EditTrack
        {
            get
            {
                return new RelayCommand(execute => openEditTrackWindow(), canExecute => (playlist != null && SelectedTrack != null));
            }
        }

        public ICommand DeleteTrack
        {
            get
            {
                return new RelayCommand(execute => deleteSelectedTrack(), canExecute => (playlist != null && SelectedTrack != null));
            }
        }

        public ICommand GoToNextTrack
        {
            get
            {
                return new RelayCommand(execute => goToNextTrack(), canExecute => (MediaPlayMode == PlayMode.Playlist || MediaPlayMode == PlayMode.Video));
            }
        }

        public ICommand GoToPreviousTrack
        {
            get
            {
                return new RelayCommand(execute => goToPreviousTrack(), canExecute => (MediaPlayMode == PlayMode.Playlist || MediaPlayMode == PlayMode.Video));
            }
        }

        public ICommand ShufflePlaylist
        {
            get
            {
                return new RelayCommand(execute => shuffleTracks(), canExecute => (playlist != null && Tracks != null && Tracks!.Count > 0));
            }
        }

        public ICommand ShowLyrics
        {
            get
            {
                return new RelayCommand(execute => FetchAndDisplayLyricsForSelectedTrack(), canExecute => (playlist != null && SelectedTrack != null));
            }
        }

        public ICommand ShowEqualizer
        {
            get
            {
                return new RelayCommand(execute => showEqualizerWindow(),
                    canExecute => (SelectedTrack != null ));
            }
        }
        #endregion
    }
}
