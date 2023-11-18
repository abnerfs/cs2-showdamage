using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace ShowDamage
{
    class DamageDone
    {
        public float Health { get; set; }
        public float Armor { get; set; }
    }

    public class ShowDamage : BasePlugin
    {
        public override string ModuleName => "AbNeR ShowDamage";

        public override string ModuleVersion => "1.0.1";

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

        public override void Load(bool hotReload)
        {
            Console.WriteLine("Showdamage loaded");
            
            ffaEnabledConVar = ConVar.Find("mp_teammates_are_enemies");
        }

        [GameEventHandler]
        public HookResult EventPlayerHurt(EventPlayerHurt ev, GameEventInfo info)
        {
            if (ev.Attacker is null ||
                ev.Userid is null ||
                !ev.Attacker.IsValid ||
                (ev.Attacker.TeamNum == ev.Userid.TeamNum && !FFAEnabled))
                return HookResult.Continue;

            int attackerUserId = ev.Attacker.UserId!.Value;
            if (damageDone.ContainsKey(attackerUserId))
            {
                var dmg = damageDone[attackerUserId];
                if(dmg is not null)
                {
                    dmg.Health += ev.DmgHealth;
                    dmg.Armor += ev.DmgArmor;
                }
            }
            else
            {
                damageDone.Add(attackerUserId, new DamageDone { Armor = ev.DmgArmor, Health = ev.DmgHealth });
                Action callback = () =>
                {
                    if (damageDone.ContainsKey(attackerUserId))
                    {
                        var player = Utilities.GetPlayerFromUserid(attackerUserId);
                        if (player is not null && player.IsValid)
                        {
                            var dmg = damageDone[attackerUserId];
                            if(dmg is not null)
                            {
                                StringBuilder builder = new();
                                builder.Append($"-{dmg.Health} HP");
                                if (dmg.Armor > 0)
                                    builder.Append($"\n-{dmg.Armor} Armor");

                                player.PrintToCenter(builder.ToString());
                            }
                        }
                        damageDone.Remove(attackerUserId);
                    }
                };

                NativeAPI.CreateTimer(0.1F, callback, 0);
            }
            return HookResult.Continue;
        }
    }
}