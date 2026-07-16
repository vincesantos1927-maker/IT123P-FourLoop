using Plugin.Maui.Audio;

namespace jeo_ano_ba.Services {
    public class BgmService {
        private const string BgmFileName = "BgmEnabled";
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _player;

        public BgmService(IAudioManager audioManager) {
            _audioManager = audioManager;
        }

        public bool IsEnabled => Preferences.Get(BgmFileName, true);

        public void SetEnabled(bool enabled) {
            Preferences.Set(BgmFileName, enabled);

            if (enabled)
                Play();
            else
                Pause();
        }

        public bool Toggle() {
            bool enabled = !IsEnabled;
            SetEnabled(enabled);
            return enabled;
        }

        public async Task InitializeAsync() {
            if (_player != null) return;

            var stream = await FileSystem.OpenAppPackageFileAsync("bgm.mp3");
            _player = _audioManager.CreatePlayer(stream);
            _player.Loop = true;

            if (IsEnabled)
                _player.Play();
        }

        public void Play() {
            _player?.Play();
        }

        public void Pause() {
            _player?.Pause();
        }
    }
}