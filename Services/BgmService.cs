using System;
using System.Collections.Generic;
using System.Text;

namespace jeo_ano_ba.Services
{
    public class BgmService
    {
        private const string BgmFileName = "BgmEnabled";
        //checks if music is turned on
        public bool IsEnabled => Preferences.Get(BgmFileName, true);
        //permanently sets the music to on or off
        public void SetEnabled(bool enabled)
        {
            Preferences.Set(BgmFileName, enabled);
        }
        //switch obvious naman to
        public bool Toggle()
        {
            bool enabled = !IsEnabled;
            SetEnabled(enabled);
            return enabled;
        }
    }
}
