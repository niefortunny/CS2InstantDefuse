using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using CS2InstantDefuse.Configs;

namespace CS2InstantDefuse
{
    [MinimumApiVersion(228)]
    public class CS2InstantDefuse : BasePlugin, IPluginConfig<CS2InstantDefuseConfig>
    {
        public override string ModuleName => "CS2InstantDefuse";
        public override string ModuleVersion => "1.3.0-unf";
        public override string ModuleAuthor => "LordFetznschaedl | revamp: unfortunate";
        public override string ModuleDescription =>
            "Simple Plugin that allowes the bomb to be instantly defused when no enemy is alive (configurable) and no utility is in use (configurable)";

        public CS2InstantDefuseConfig Config { get; set; } = new CS2InstantDefuseConfig();

        private float _bombPlantedTime = float.NaN;
        private bool _bombTicking = false;
        private int _molotovThreat = 0;
        private int _heThreat = 0;

        private List<int> _infernoThreat = new List<int>();

        public void OnConfigParsed(CS2InstantDefuseConfig config)
        {
            this.Config = config;
        }

        public override void Load(bool hotReload)
        {
            this.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            this.RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
            this.RegisterEventHandler<EventBombBegindefuse>(OnBombBeginDefuse);

            this.RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);

            this.RegisterEventHandler<EventInfernoStartburn>(OnInfernoStartBurn);
            this.RegisterEventHandler<EventInfernoExtinguish>(OnInfernoExtinguish);
            this.RegisterEventHandler<EventInfernoExpire>(OnInfernoExpire);

            this.RegisterEventHandler<EventHegrenadeDetonate>(OnHeGrenadeDetonate);
            this.RegisterEventHandler<EventMolotovDetonate>(OnMolotovDetonate);

            // Comment in if you need to debug the defuse stuff.
            if (this.Config.DebugBombBeep)
            {
                this.RegisterEventHandler<EventBombBeep>(OnBombBeep);
            }
        }

        private HookResult OnBombBeep(EventBombBeep @event, GameEventInfo info)
        {
            var plantedBomb = this.FindPlantedBomb();
            if (plantedBomb == null)
                return HookResult.Continue;
            return HookResult.Continue;
        }

        private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            if (
                @event.Weapon == "smokegrenade"
                || @event.Weapon == "flashbang"
                || @event.Weapon == "decoy"
            )
                return HookResult.Continue;

            if (@event.Weapon == "hegrenade")
                this._heThreat++;

            if (@event.Weapon == "incgrenade" || @event.Weapon == "molotov")
                this._molotovThreat++;

            return HookResult.Continue;
        }

        private HookResult OnInfernoStartBurn(EventInfernoStartburn @event, GameEventInfo info)
        {
            var infernoPosVector = new Vector3(@event.X, @event.Y, @event.Z);

            var plantedBomb = this.FindPlantedBomb();
            if (plantedBomb == null)
                return HookResult.Continue;

            var plantedBombVector = plantedBomb.CBodyComponent?.SceneNode?.AbsOrigin ?? null;
            if (plantedBombVector == null)
                return HookResult.Continue;

            var plantedBombVector3 = new Vector3(
                plantedBombVector.X,
                plantedBombVector.Y,
                plantedBombVector.Z
            );

            var distance = Vector3.Distance(infernoPosVector, plantedBombVector3);

            if (distance > 250)
                return HookResult.Continue;

            this._infernoThreat.Add(@event.Entityid);

            return HookResult.Continue;
        }

        private HookResult OnInfernoExtinguish(EventInfernoExtinguish @event, GameEventInfo info)
        {
            this._infernoThreat.Remove(@event.Entityid);

            return HookResult.Continue;
        }

        private HookResult OnInfernoExpire(EventInfernoExpire @event, GameEventInfo info)
        {
            this._infernoThreat.Remove(@event.Entityid);
            return HookResult.Continue;
        }

        private HookResult OnHeGrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info)
        {
            if (this._heThreat > 0)
                this._heThreat--;

            return HookResult.Continue;
        }

        private HookResult OnMolotovDetonate(EventMolotovDetonate @event, GameEventInfo info)
        {
            if (this._molotovThreat > 0)
                this._molotovThreat--;

            return HookResult.Continue;
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            this._bombPlantedTime = float.NaN;
            this._bombTicking = false;

            this._heThreat = 0;
            this._molotovThreat = 0;
            this._infernoThreat = new List<int>();

            return HookResult.Continue;
        }

        private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            this._bombPlantedTime = Server.CurrentTime;
            this._bombTicking = true;

            return HookResult.Continue;
        }

        private HookResult OnBombBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            if (@event.Userid == null)
                return HookResult.Continue;

            if (!@event.Userid.IsValid)
                return HookResult.Continue;

            this.TryInstantDefuse(@event.Userid);

            return HookResult.Continue;
        }

        private bool TryInstantDefuse(CCSPlayerController player)
        {
            if (!this._bombTicking)
                return false;

            if (this.Config.EnableHEThreatCheck && this._heThreat > 0)
                return false;
            if (
                this.Config.EnableMolotovThreatCheck
                && (this._molotovThreat > 0 || this._infernoThreat.Any())
            )
                return false;

            var plantedBomb = this.FindPlantedBomb();
            if (plantedBomb == null)
                return false;

            if (plantedBomb.CannotBeDefused)
                return false;

            if (this.Config.EnableAliveTerroristCheck && this.TeamHasAlivePlayers(CsTeam.Terrorist))
                return false;

            var bombTimeUntilDetonation =
                plantedBomb.TimerLength - (Server.CurrentTime - this._bombPlantedTime);

            var defuseLength = plantedBomb.DefuseLength;
            if (defuseLength != 5 && defuseLength != 10)
                defuseLength = player.PawnHasDefuser ? 5 : 10;

            bool bombCanBeDefusedInTime = (bombTimeUntilDetonation - defuseLength) >= 0.0f;
            bool bombCanBeDefusedInTimeWithKit = (bombTimeUntilDetonation - 5f) >= 0.0f;

            if (
                this.Config.EnableAdditionalKitCheck
                && !bombCanBeDefusedInTime
                && bombCanBeDefusedInTimeWithKit
                && defuseLength == 10
            )
            {
                foreach (var ctPlayer in this.GetPlayerControllersOfTeam(CsTeam.CounterTerrorist))
                {
                    if (!ctPlayer.PawnIsAlive)
                        continue;

                    if (ctPlayer?.PlayerPawn?.Value?.ItemServices == null)
                        continue;

                    var itemService = new CCSPlayer_ItemServices(
                        ctPlayer.PlayerPawn.Value.ItemServices.Handle
                    );

                    if (itemService.HasDefuser)
                        return false;
                }
            }

            if (!bombCanBeDefusedInTime)
            {
                if (this.Config.DetonateBombIfNotEnoughTimeForDefuse)
                {
                    Server.NextFrame(() =>
                    {
                        plantedBomb.C4Blow = 1.0f;
                    });
                }

                return false;
            }

            Server.NextFrame(() =>
            {
                plantedBomb.DefuseCountDown = 0;
            });

            return true;
        }

        private bool TeamHasAlivePlayers(CsTeam team)
        {
            var playerList = this.GetPlayerControllersOfTeam(team);

            if (!playerList.Any())
                throw new Exception("No player entities have been found!");

            return playerList
                .Where(player =>
                    player.IsValid && player.TeamNum == (byte)team && player.PawnIsAlive
                )
                .Any();
        }

        private CPlantedC4? FindPlantedBomb()
        {
            var plantedBombList = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");

            if (!plantedBombList.Any())
                return null;

            return plantedBombList.FirstOrDefault();
        }

        private List<CCSPlayerController> GetPlayerControllersOfTeam(CsTeam team)
        {
            var playerList = Utilities.GetPlayers();

            //Valid players
            playerList = playerList.FindAll(x =>
                x != null
                && x.IsValid
                && x.PlayerPawn != null
                && x.PlayerPawn.IsValid
                && x.PlayerPawn.Value != null
                && x.PlayerPawn.Value.IsValid
            );

            //Team specific players
            playerList = playerList.FindAll(x => x.TeamNum == (int)team);

            return playerList ?? new List<CCSPlayerController>();
        }
    }
}
