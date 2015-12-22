using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;
using SharpDX;


namespace Volibear
{
    class Program
    {
        public static Spell.Active Q;
        public static Spell.Active E;
        public static Spell.Targeted W;
        public static Spell.Active R;
        public static Menu Menu, SkillMenu, FarmingMenu, MiscMenu;
        public static HitChance MinimumHitChance { get; set; }

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Volibear")
                return;

            Bootstrap.Init(null);

            uint level = (uint)Player.Instance.Level;
            Q = new Spell.Active(SpellSlot.Q, 600);
            R = new Spell.Active(SpellSlot.R, 125);
            E = new Spell.Active(SpellSlot.E, 400);
            W = new Spell.Targeted(SpellSlot.W, 405);
            Menu = MainMenu.AddMenu("Volibear", "hellovolibear");
            Menu.AddSeparator();
            Menu.AddLabel("Created by MyNameIsCool");
            SkillMenu = Menu.AddSubMenu("Skills", "Skills");
            SkillMenu.AddGroupLabel("Skills");
            SkillMenu.AddLabel("Combo");
            SkillMenu.Add("QCombo", new CheckBox("Use Q in Combo"));
            SkillMenu.Add("ECombo", new CheckBox("Use E in Combo"));
            SkillMenu.Add("WCombo", new CheckBox("Use W in Combo"));
            SkillMenu.Add("wslider", new Slider("Min % enemy hp for W use", 100, 0, 100));
            SkillMenu.Add("RCombo", new CheckBox("Use R in Combo"));
            SkillMenu.Add("rslider", new Slider("Num of enemy to Auto Ult 0=Off", 1, 5, 0));
            SkillMenu.AddLabel("Harass");
            SkillMenu.Add("EHarass", new CheckBox("Use E on Harass"));
            FarmingMenu = Menu.AddSubMenu("Farming", "Farming");
            FarmingMenu.AddGroupLabel("Farming");
            FarmingMenu.AddLabel("LastHit");
            FarmingMenu.Add("ELH", new CheckBox("Use E to secure last hits", false));
            FarmingMenu.Add("ELHMana", new Slider("Mana Manager for E", 60, 0, 100));
            FarmingMenu.AddLabel("LaneClear");
            FarmingMenu.Add("ELC", new CheckBox("Use E on LaneClear"));
            FarmingMenu.Add("ELCMana", new Slider("Mana Manager for E", 50, 0, 100));
            FarmingMenu.AddLabel("Jungle");
            FarmingMenu.Add("JCW", new CheckBox("Use W"));
            FarmingMenu.Add("JCE", new CheckBox("Use E"));
            MiscMenu = Menu.AddSubMenu("Misc", "Misc");
            MiscMenu.AddGroupLabel("Misc");
            MiscMenu.AddLabel("KillSteal");
            MiscMenu.Add("Ekill", new CheckBox("Use E to KillSteal"));
            Game.OnTick += Game_OnTick;
            Chat.Print("Cool Addon Loaded -= Volibear =-", System.Drawing.Color.White);
        }
        private static void Game_OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleFarm();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
            KillSteal();
        }
        private static void Combo()
        {
            var useQ = SkillMenu["QCombo"].Cast<CheckBox>().CurrentValue;
            var useR = SkillMenu["RCombo"].Cast<CheckBox>().CurrentValue;
            var useE = SkillMenu["ECombo"].Cast<CheckBox>().CurrentValue;
            var useW = SkillMenu["WCombo"].Cast<CheckBox>().CurrentValue;
            if (Q.IsReady() && useQ)
            {
                var target = TargetSelector.GetTarget(1100, DamageType.Physical);
                {
                    if (
                       ObjectManager.Player.Distance(target) <= Q.Range)
                    {
                        Q.Cast();
                    }
                }
            }
            if (E.IsReady() && useE)
            {
                var target = TargetSelector.GetTarget(1100, DamageType.Physical);
                if (ObjectManager.Player.Distance(target) <= E.Range)
                {
                    E.Cast();
                }
            }
            if (W.IsReady() && useW)
            {
                var target = TargetSelector.GetTarget(1100, DamageType.Physical);
                float health = target.Health;
                float maxhealth = target.MaxHealth;
                float wcount = SkillMenu["wslider"].Cast<Slider>().CurrentValue;
                if (health < ((maxhealth * wcount) / 100))
                {
                if (useW && W.IsReady())
                    {
                       W.Cast(target);
                    }
                }
            }
            if (R.IsReady() && useR)
            {
                var target = TargetSelector.GetTarget(1100, DamageType.Physical);
                if (GetNumberHitByR(target) >= SkillMenu["rslider"].Cast<Slider>().CurrentValue)
                {
                    R.Cast();
                }
            }
        }


        private static int GetNumberHitByR(Obj_AI_Base target)
        {
            int totalHit = 0;
            foreach (AIHeroClient current in ObjectManager.Get<AIHeroClient>())
            {
                if (current.IsEnemy && Vector3.Distance(
                   ObjectManager.Player.ServerPosition, current.ServerPosition) <= R.Range)
                {
                    totalHit = totalHit + 1;
                }
            }
            return totalHit;
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target == null) return;
            var useE = MiscMenu["Ekill"].Cast<CheckBox>().CurrentValue;

            if (E.IsReady() && useE && target.IsValidTarget(E.Range) && !target.IsZombie && target.Health <= _Player.GetSpellDamage(target, SpellSlot.E))
            {
                E.Cast();
            }
        }
        private static void Harass()
        {
            //var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
           // if (target == null) return;
            var useE = SkillMenu["EHarass"].Cast<CheckBox>().CurrentValue;
            var target = TargetSelector.GetTarget(1100, DamageType.Physical);
            if (target == null) return;

            if (useE && ObjectManager.Player.Distance(target) <= E.Range && E.IsReady())
            {
                E.Cast();
            }
        }
        private static void LaneClear()
        {
            var useE = FarmingMenu["ELC"].Cast<CheckBox>().CurrentValue;
            var Wmana = FarmingMenu["ELCMana"].Cast<Slider>().CurrentValue;
            var minions = ObjectManager.Get<Obj_AI_Base>().OrderBy(m => m.Health).Where(m => m.IsMinion && m.IsEnemy && !m.IsDead);
            foreach (var minion in minions)
            {
                if (useE && E.IsReady() && Player.Instance.ManaPercent > Wmana && minion.Health <= _Player.GetSpellDamage(minion, SpellSlot.E))
                {
                    E.Cast();
                }
            }
        }


        private static void JungleFarm()
        {
            var useE = FarmingMenu["JCE"].Cast<CheckBox>().CurrentValue;
            var useW = FarmingMenu["JCW"].Cast<CheckBox>().CurrentValue;
            if (W.IsReady() && useW)
            {
                var target = EntityManager.MinionsAndMonsters.GetJungleMonsters().OrderByDescending(a => a.MaxHealth).FirstOrDefault(b => b.Distance(Player.Instance) < 1300);
                if (target == null || !target.IsValidTarget()) return;
                if (target.IsValidTarget(W.Range))
                {
                    W.Cast(target);
                }
            }

            if (useE && E.IsReady())
            {
                var target = EntityManager.MinionsAndMonsters.GetJungleMonsters().OrderByDescending(a => a.MaxHealth).FirstOrDefault(b => b.Distance(Player.Instance) < 1300);
                if (target == null || !target.IsValidTarget()) return;

                if (target.IsValidTarget(E.Range))
                {
                    E.Cast();
                }
            }
        }

        private static void LastHit()
        {
            return;
        }
    }
}