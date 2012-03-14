// Behavior originally contributed by Raphus.
//
// QUICK DOX:
//      Uses an item on a target while the toon is in combat.   The caller can determine at what point
//      in combat the item will be used:
//          * When the target's health drops below a certain percentage
//          * When the target is casting a particular spell
//          * When the target gains a particular aura
//          * When the toon gains a particular aura
//          * one or more of the above happens
//
//  Parameters (required, then optional--both listed alphabetically):
//      ItemId: Id of the item to use on the targets.
//      MobId1, MobId2, ...MobIdN [Required: 1]: Id of the targets on which to use the item.
//
//      CastingSpellId [Default:none]: waits for the target to be casting this spell before using the item.
//      HasAuraId [Default:none]: waits for the toon to acquire this aura before using the item.
//      MobHasAuraId [Default:none]: waits for the target to acquire this aura before using the item.
//      MobHpPercentLeft [Default:0 percent]: waits for the target's hitpoints to fall below this percentage
//              before using the item.
//
//      NumOfTimes [Default:1]: number of times to use the item on a viable target
//      QuestId [Default:none]:
//      QuestCompleteRequirement [Default:NotComplete]:
//      QuestInLogRequirement [Default:InLog]:
//              A full discussion of how the Quest* attributes operate is described in
//              http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
//      X, Y, Z [Default:Toon's current location]: world-coordinates of the general location where the targets can be found.
//
//  Notes:
//      * One or more of CastingSpellId, HasAuraId, MobHasAuraId, or MobHpPercentLeft must be specified.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;
using System.Threading;
using CommonBehaviors.Actions;
using Styx.Combat.CombatRoutine;


using Styx;


namespace Styx.Bot.Quest_Behaviors
{
    public class TargetFace : CustomForcedBehavior
    {
        public TargetFace(Dictionary<string, string> args)
            : base(args)
        {

            try
            {
               
                
                MobIds = GetNumberedAttributesAsArray<int>("MobId", 1, ConstrainAs.MobId, new[] { "NpcId" });
               
                QuestId = GetAttributeAsNullable<int>("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0;
              
               
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
                IsAttributeProblem = true;
            }
        }


        // Attributes provided by caller
        public int[] MobIds { get; private set; }
        public int QuestId { get; private set; }
        

        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
        private Composite _root;

        // Private properties
        private int Counter { get; set; }
        private LocalPlayer Me { get { return (ObjectManager.Me); } }
        public WoWUnit Mob
        {
            get
            {
                return (ObjectManager.GetObjectsOfType<WoWUnit>()
                                     .Where(u => MobIds.Contains((int)u.Entry) && !u.Dead)
                                     .OrderBy(u => u.Distance).FirstOrDefault());
            }
        }

		

        ~TargetFace()
        {
            Dispose(false);
        }


        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                // NOTE: we should call any Dispose() method for any managed or unmanaged
                // resource, if that resource provides a Dispose() method.

                // Clean up managed resources, if explicit disposal...
                if (isExplicitlyInitiatedDispose)
                {
                    // empty, for now
                }

                // Clean up unmanaged resources (if any) here...
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;

                // Call parent Dispose() (if it exists) here ...
                base.Dispose();
            }

            _isDisposed = true;
        }

        private ulong _lastMobGuid;


        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {

		_isBehaviorDone = true;
		return _root ?? (_root = new PrioritySelector(
			new Sequence(
				new Action(ret => Mob.Target()),
				new Action(ret => StyxWoW.Me.CurrentTarget.Face()),
				new Action(ret => Thread.Sleep(200))
			)
		));
		
        
		
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone );
            }
        }


        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                if (TreeRoot.Current != null && TreeRoot.Current.Root != null && TreeRoot.Current.Root.LastStatus != RunStatus.Running)
                {
                    var currentRoot = TreeRoot.Current.Root;
                    if (currentRoot is GroupComposite)
                    {
                        var root = (GroupComposite)currentRoot;
                        
                    }
                }


                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);

                TreeRoot.GoalText = this.GetType().Name + ": " + ((quest != null) ? quest.Name : "In Progress");
            }
        }

        #endregion
    }
}
