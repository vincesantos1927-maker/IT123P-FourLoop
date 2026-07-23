using Plugin.Maui.Audio;

namespace jeo_ano_ba.Services {
    public class BgmService {
        //for saving the bgm setting
        private const string BgmFileName = "BgmEnabled";
        //for playing the bgm
        private readonly IAudioManager _audioManager;
        private readonly SfxService _sfxService;
        private IAudioPlayer? _player;
        
        //constructor for the BgmService, takes in an IAudioManager to manage audio playback
        public BgmService(IAudioManager audioManager, SfxService sfxService) {
            _audioManager = audioManager;
            _sfxService = sfxService;
        }
        //gets whether the bgm is enabled or not, by checking the Preferences for the BgmFileName key, defaulting to true if not found
        public bool IsEnabled => Preferences.Get(BgmFileName, true);

        public void SetEnabled(bool enabled) {
            _sfxService.PlayClick();

            //saves the setting to the Preferences, and plays or pauses the music based on the enabled state
            Preferences.Set(BgmFileName, enabled);

            if (enabled)
                Play();
            else
                Pause();
        }
        //toggles the bgm setting, and returns the new state, opposite of the enavled
        public bool Toggle() {
            bool enabled = !IsEnabled;
            SetEnabled(enabled);
            return enabled;
        }

        public async Task InitializeAsync() {
            //if the player is already initialized, return
            if (_player != null) return;
            //opens the bgm.mp3 file from the app package, creates an audio player with it, and sets it to loop
            var stream = await FileSystem.OpenAppPackageFileAsync("bgm.mp3");
            _player = _audioManager.CreatePlayer(stream);
            _player.Loop = true;
            //if the bgm is enabled, start playing the music
            if (IsEnabled)
                _player.Play();
        }
        //starts the music playback if the player is initialized
        public void Play() {
            _player?.Play();
        }
        //pauses the music playback if the player is initialized
        public void Pause() {
            _player?.Pause();
        }
    }
}