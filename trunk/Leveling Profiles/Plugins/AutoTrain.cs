using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Styx;
using Styx.Plugins;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace AutoTrain
{
    public class AutoTrain:HBPlugin
    {
        internal static uint SpellBookNbSpells = 0xB31E9C;
        internal static uint KnownSpell = 0xB31EA0;
        internal static bool HasGoneToTrainer = false;
        public int AvailableSpells
        {
            get
            {
                int number = 0;
                uint nbSpells = ObjectManager.Wow.Read<uint>((uint)ObjectManager.WoWProcess.MainModule.BaseAddress + SpellBookNbSpells);
                uint SpellBookInfoPtr = ObjectManager.Wow.Read<uint>((uint)ObjectManager.WoWProcess.MainModule.BaseAddress + KnownSpell);

                for (uint i = 0; i < nbSpells; i++)
                {
                    uint Struct = ObjectManager.Wow.Read<uint>(SpellBookInfoPtr + i * 0x4);
                    bool IsAvailable = ObjectManager.Wow.Read<int>(Struct + 0x8) == 3;
                    if (IsAvailable)
                    {
                        number++;
                    }
                }
                return number;
            }
        }
        public List<Styx.Logic.Combat.WoWSpell> AvailSpells
        {
            get
            {
                List<Styx.Logic.Combat.WoWSpell> temp = new List<Styx.Logic.Combat.WoWSpell>();
                uint nbSpells = ObjectManager.Wow.Read<uint>((uint)ObjectManager.WoWProcess.MainModule.BaseAddress + SpellBookNbSpells);
                uint SpellBookInfoPtr = ObjectManager.Wow.Read<uint>((uint)ObjectManager.WoWProcess.MainModule.BaseAddress + KnownSpell);

                for (uint i = 0; i < nbSpells; i++)
                {
                    uint Struct = ObjectManager.Wow.Read<uint>(SpellBookInfoPtr + i * 0x4);
                    bool IsAvailable = ObjectManager.Wow.Read<int>(Struct + 0x8) == 3;
                    int SpellId = ObjectManager.Wow.Read<int>(Struct + 0x4);
                    if (IsAvailable)
                    {
                        Styx.Logic.Combat.WoWSpell spell = Styx.Logic.Combat.WoWSpell.FromId(SpellId);
                        temp.Add(spell);
                    }
                }
                return temp;
            }
        }
        public override string Author
        {
            get { return "Shak"; }
        }
        public AutoTrain()
        {
        }
        public override void Initialize()
        {
            
        }
        public override string ButtonText
        {
            get
            {
                return "Settings";
            }
        }
        public override string Name
        {
            get { return "AutoTrain"; }
        }
        public override bool WantButton
        {
            get
            {
                return false;
            }
        }
        public override Version Version
        {
            get { return new Version(1, 0, 0); }
        }
        public override void Pulse()
        {
            if (Styx.Helpers.CharacterSettings.Instance.TrainNewSkills && AvailableSpells > 0 && !HasGoneToTrainer)
            {
                foreach(Styx.Logic.Combat.WoWSpell spell in AvailSpells)
                {
                    if (spell.Mechanic != Styx.Logic.Combat.WoWSpellMechanic.Mounted)
                    {
                        Styx.Logic.Vendors.ForceTrainer = true;
                        HasGoneToTrainer = true;
                        break;
                    }
                }
            }
            if (Styx.Helpers.CharacterSettings.Instance.TrainNewSkills && AvailableSpells == 0 && HasGoneToTrainer)
            {
                HasGoneToTrainer = false;
            }
        }
    }
}
