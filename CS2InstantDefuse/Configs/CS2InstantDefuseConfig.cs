using CounterStrikeSharp.API.Core;

namespace CS2InstantDefuse.Configs
{
    public class CS2InstantDefuseConfig : BasePluginConfig
    {
        public bool DetonateBombIfNotEnoughTimeForDefuse { get; set; } = true;

        public bool EnableMolotovThreatCheck { get; set; } = true;
        public bool EnableHEThreatCheck { get; set; } = true;
        public bool EnableAliveTerroristCheck { get; set; } = true;
        public bool EnableAdditionalKitCheck { get; set; } = true;

        public bool DebugBombBeep { get; set; } = false;


        public CS2InstantDefuseConfig() 
        {
            this.Version = 3;
        }
    }
}
