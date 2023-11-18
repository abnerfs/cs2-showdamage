using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities;
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

        public override string ModuleVersion => "1.0.0";

        public override string ModuleAuthor => "AbNeR_CSS";
        
        public override string ModuleDescription => "Shows damage dealt to enemies in the center text";

        Dictionary<int, DamageDone> damageDone = new();

        public override void Load(bool hotReload)
        {
            Console.WriteLine("Showdamage loaded");
        }

        [GameEventHandler]
        public HookResult EventPlayerHurt(EventPlayerHurt ev, GameEventInfo info)
        {
            if (ev.Attacker is null ||
                ev.Userid is null ||
                ev.Attacker.TeamNum == ev.Userid.TeamNum)
                return HookResult.Continue;

            int attackerUserId = ev.Attacker.UserId!.Value;
            if (damageDone.ContainsKey(attackerUserId))
            {
                var dmg = damageDone[attackerUserId];
                dmg.Health += ev.DmgHealth;
                dmg.Armor += ev.DmgArmor;
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