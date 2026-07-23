using Plugin.Maui.Audio;

namespace jeo_ano_ba.Services
{
    public class SfxService
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _clickPlayer;
        private IAudioPlayer? _correctPlayer;
        private IAudioPlayer? _wrongPlayer;
        private IAudioPlayer? _buzzerPlayer;
        private IAudioPlayer? _tickingPlayer;

        public SfxService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
            _ = LoadSoundsAsync();
        }

        private async Task LoadSoundsAsync()
        {
            _clickPlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("click.mp3"));
            _correctPlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("correct.mp3"));
            _wrongPlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("wrong.mp3"));
            _buzzerPlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("buzzer.mp3"));
            _tickingPlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("ticking.mp3"));
            _tickingPlayer.Loop = true;
        }

        public void PlayClick()
        {
            _clickPlayer?.Seek(0);
            _clickPlayer?.Play();
        }

        public void PlayCorrect()
        {
            _correctPlayer?.Seek(0);
            _correctPlayer?.Play();
        }

        public void PlayWrong()
        {
            _wrongPlayer?.Seek(0);
            _wrongPlayer?.Play();
        }
        public void PlayBuzzer()
        {
            _buzzerPlayer?.Seek(0);
            _buzzerPlayer?.Play();
        }
        public void PlayTicking()
        {
            _tickingPlayer?.Seek(0);
            _tickingPlayer?.Play();
        }
        public void StopTicking()
        {
            _tickingPlayer?.Stop();
        }
    }
}