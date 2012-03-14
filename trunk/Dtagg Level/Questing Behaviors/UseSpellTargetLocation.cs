using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Styx.Database;
using Styx.Logic.Combat;
using Styx.Helpers;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles.Quest;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using Styx.Logic.BehaviorTree;
using Action = TreeSharp.Action;

namespace Styx.Bot.Quest_Behaviors
{
    public class SpellLocation : CustomForcedBehavior
	{
		public SpellLocation(Dictionary<string, string> args): base(args){
	            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
             
				SpellLocation = GetAttributeAsNullable<WoWPoint>("", false, ConstrainAs.WoWPointNonEmpty, null) ?? Me.Location;
        		QuestId = GetAttributeAsNullable<int>("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0;
          		ItemId = GetAttributeAsNullable<int>("ItemId", true, ConstrainAs.ItemId, null) ?? 0;

                Counter = 1;
            }
		
            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
                                    + "\nFROM HERE:\n"
                                    + except.StackTrace + "\n");
                
            }
		}    

		public WoWPoint SpellLocation { get; private set; }
		public int QuestId { get; private set; }
		public int ItemId { get; private set; }

		public static LocalPlayer me = ObjectManager.Me;
	
		protected override Composite CreateBehavior()
		{
			return _root ?? (_root = new PrioritySelector(new Decorator(new Action(ret => LegacySpellManager.ClickRemoteLocation(SpellLocation)))));
		}
		public override void OnStart()
        {
			new Action(ret => LegacySpellManager.ClickRemoteLocation(SpellLocation);
		}
	}
}
	

