using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Numerics;
using System.Text;

namespace ShowDamage
{
    class DamageDone
    {
        public float Health { get; set; }
        public float Armor { get; set; }
    }

    public class ShowDamage : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "AbNeR ShowDamage";

        public override string ModuleVersion => "1.1.0";

        public override string ModuleAuthor => "AbNeR_CSS";

        public override string ModuleDescription => "Shows damage dealt to enemies in the center text";

        Dictionary<int, DamageDone> damageDone = new();

        ConVar? ffaEnabledConVar = null;

        public bool FFAEnabled
        {
            get
            {
                if (ffaEnabledConVar is null)
                    return false;

                return ffaEnabledConVar.GetPrimitiveValue<bool>();
            }
        }

        public required Config Config { get; set; }

        public void OnConfigParsed(Config config)
        {
            Config = config;
        }

        public override void Load(bool hotReload)
        {
            Console.WriteLine("Showdamage loaded");
            ffaEnabledConVar = ConVar.Find("mp_teammates_are_enemies");
        }

        Action BuildCallback(int attackerUserId) =>
            () =>
            {
                if (damageDone.ContainsKey(attackerUserId))
                {
                    var player = Utilities.GetPlayerFromUserid(attackerUserId);
                    if (player is not null && player.IsValid)
                    {
                        var dmg = damageDone[attackerUserId];
                        if (dmg is not null)
                        {
                            StringBuilder builder = new();
                            builder.Append($"-{dmg.Health} HP");
                            if (dmg.Armor > 0 && Config.ShowArmorDmg)
                                builder.Append($"\n-{dmg.Armor} Armor");

                            player.PrintToCenter(builder.ToString());
                        }
                    }
                    damageDone.Remove(attackerUserId);
                }
            };

        [GameEventHandler]
        public HookResult EventPlayerHurt(EventPlayerHurt ev, GameEventInfo info)
        {
            if (ev.Attacker is null ||
                ev.Userid is null ||
                !ev.Attacker.IsValid ||
                (ev.Attacker.TeamNum == ev.Userid.TeamNum && !FFAEnabled))
                return HookResult.Continue;

            int attackerUserId = ev.Attacker.UserId!.Value;
            if (!string.IsNullOrEmpty(Config.AdminGroup) && !AdminManager.PlayerInGroup(ev.Attacker, Config.AdminGroup))
                return HookResult.Continue;

            if (Config.HideDamage)
            {
                ev.Attacker.PrintToCenter("*");
                return HookResult.Continue;
            }

            if (damageDone.ContainsKey(attackerUserId))
            {
                DamageDone? dmg = damageDone[attackerUserId];
                if (dmg is not null)
                {
                    dmg.Health += ev.DmgHealth;
                    dmg.Armor += ev.DmgArmor;
                }
            }
            else
            {
                damageDone.Add(attackerUserId, new DamageDone { Armor = ev.DmgArmor, Health = ev.DmgHealth });
                AddTimer(0.1F, BuildCallback(attackerUserId), 0);
            }
            return HookResult.Continue;
        }
    }
}