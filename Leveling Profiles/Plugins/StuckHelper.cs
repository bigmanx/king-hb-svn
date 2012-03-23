/*
 * This plugin includes following features:
 * - Detects when stuck and performs unstuck routine
 * - Restarts bot when stuck or at intervals (stability fix)
 * - Fixes stuck in air being attacked for ArchaeologyBuddy
 * - Fixes stuck in shallow water for PoolFisher bot
 *
 * Author: lofi
 */

using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Inventory.Frames.LootFrame;
using Styx.Logic.POI;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles;
using Styx.Logic;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals;
using Styx;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using System;

namespace lofi
{
    public class StuckHelper : HBPlugin, IDisposable
    {
        // User configurations
        private static double RestartInterval = 0; // Restart bot ever x mins, 0 to disable
        private static int StopStartTime = 10; // Seconds to wait between stop and start during re-start
        private static double StuckDistance = 10; // Reset standing still timer after moving this distance
        private static double RestartStuckMinutes = 3.5; // Restart bot after standing still this long, 0 to disable
        private static double UnstuckRoutineMinutes = 2.0; // Perform unstuck after standing still this long, 0 to disable
        private static double ArchBuddyFixMinutes = 0.3; // Dismounts after flying still and being attacked, 0 to disable
        private static double PoolFisherFixMinutes = 0.3; // Perform unstuck after swimming still this long, 0 to disable
        private static double MountFixMinutes = 0.3; // Perform dismount after mounted still this long, 0 to disable

        public override string Name { get { return "StuckHelper"; } }
        public override string Author { get { return "lofi"; } }
        public override Version Version { get { return new Version(1, 0, 2); } }
        public override bool WantButton { get { return false; } }
        public override string ButtonText { get { return Version.ToString(); } }
        private static LocalPlayer Me { get { return ObjectManager.Me; } }
        private static Stopwatch spamDelay = new Stopwatch();
        private static Stopwatch restartStandingStillTimer = new Stopwatch();
        private static Stopwatch unstuckStandingStillTimer = new Stopwatch();
        private static Stopwatch archBuddyFixTimer = new Stopwatch();
        private static Stopwatch swimFixTimer = new Stopwatch();
        private static Stopwatch mountFixTimer = new Stopwatch();
        private static Thread restartIntervalThread = null;
        private static Thread restartWhenStuckThread = null;
        private static WoWPoint lastPoint = new WoWPoint();
        private static Random random = new Random();

        public override void OnButtonPress()
        {
        }

        public override void Pulse()
        {
            try
            {
                //sanity check and filter-only check
                if (Me == null || !ObjectManager.IsInGame || BotPoi.Current == null ||
                    !TreeRoot.IsRunning || RoutineManager.Current == null)
                {
                    return;
                }

                if (spamDelay.Elapsed.TotalSeconds < 3.0)
                    return; // spam protection
                spamDelay.Reset();
                spamDelay.Start();

                // update game objects
                ObjectManager.Update();

                DetectStuck();
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message + ". See debug log.");
                Logging.WriteDebug("StuckHelper exception:");
                Logging.WriteException(e);
            }
        }

        public override void Initialize()
        {
            if (restartIntervalThread != null && restartIntervalThread.IsAlive)
            {
                restartIntervalThread.Abort();
            }

            if (RestartInterval > 0)
            {
                restartIntervalThread = new Thread(new ThreadStart(RestartIntervalThread));
                restartIntervalThread.Start();
            }

            spamDelay.Start();

            Log("Loaded version " + Version);
        }

        public override void Dispose()
        {
            if (restartIntervalThread != null && restartIntervalThread.IsAlive)
            {
                restartIntervalThread.Abort();
            }
        }

        public static void RestartIntervalThread()
        {
            while (true) // wait for Abort()
            {
                Log("Re-starting HB in " + RestartInterval + " minutes...");
                Thread.Sleep((int)(RestartInterval * 60 * 1000));
                RestartBot();
            }
        }

        public static void RestartWhenStuckThread()
        {
            Log("Detected stuck!! Re-starting bot.");

            if (restartIntervalThread != null && restartIntervalThread.IsAlive)
            {
                restartIntervalThread.Abort();
            }

            RestartBot();

            if (RestartInterval > 0)
            {
                restartIntervalThread = new Thread(new ThreadStart(RestartIntervalThread));
                restartIntervalThread.Start();
            }
        }

        private static void UnstuckRoutine()
        {
            Log("Performing unstuck routine!");

            Mount.Dismount();

            // swim jumps
            int numJumps = random.Next(2, 4);
            for (int i = 0; i < numJumps; i++)
            {
                Styx.Helpers.KeyboardManager.PressKey((char)Keys.Space);
                Thread.Sleep(random.Next(2000, 3000));
                Styx.Helpers.KeyboardManager.ReleaseKey((char)Keys.Space);
                Thread.Sleep(random.Next(250, 750));
            }

            // long jumps
            numJumps = random.Next(1, 3);
            for (int i = 0; i < numJumps; i++)
            {
                Styx.Helpers.KeyboardManager.PressKey((char)Keys.Up);
                Thread.Sleep(random.Next(30, 50));
                Styx.Helpers.KeyboardManager.PressKey((char)Keys.Space);
                Thread.Sleep(random.Next(500, 750));
                Styx.Helpers.KeyboardManager.ReleaseKey((char)Keys.Up);
                Styx.Helpers.KeyboardManager.ReleaseKey((char)Keys.Space);
                Thread.Sleep(random.Next(250, 750));
            }

            // short jumps
            numJumps = random.Next(1, 3);
            for (int i = 0; i < numJumps; i++)
            {
                Styx.Helpers.KeyboardManager.PressKey((char)Keys.Space);
                Thread.Sleep(random.Next(30, 50));
                Styx.Helpers.KeyboardManager.PressKey((char)Keys.Up);
                Thread.Sleep(random.Next(500, 750));
                Styx.Helpers.KeyboardManager.ReleaseKey((char)Keys.Up);
                Styx.Helpers.KeyboardManager.ReleaseKey((char)Keys.Space);
                Thread.Sleep(random.Next(250, 750));
            }

            lastPoint = new WoWPoint(Me.X, Me.Y, Me.Z);
        }

        private static void RestartBot()
        {
            if (Me.IsInInstance || Battlegrounds.IsInsideBattleground)
            {
                Log("Inside an instance or a BG! Skipped HB re-start.");
            }
            else
            {
                spamDelay.Reset();
                restartStandingStillTimer.Reset();
                unstuckStandingStillTimer.Reset();
                archBuddyFixTimer.Reset();
                swimFixTimer.Reset();
                mountFixTimer.Reset();

                Log("Re-starting HB...");
                Styx.Logic.BehaviorTree.TreeRoot.Stop();

                Log("Waiting " + StopStartTime + " seconds...");
                Thread.Sleep(StopStartTime * 1000);

                Log("Starting HB...");
                Styx.Logic.BehaviorTree.TreeRoot.Start();
            }
        }

        private static bool DetectStuck()
        {
            WoWPoint myPoint = new WoWPoint(Me.X, Me.Y, Me.Z);

            if (myPoint.Distance(lastPoint) > StuckDistance)
            {
                lastPoint = myPoint;
                restartStandingStillTimer.Reset();
                unstuckStandingStillTimer.Reset();
                return false;
            }

            if (!restartStandingStillTimer.IsRunning)
                restartStandingStillTimer.Start();

            if (!unstuckStandingStillTimer.IsRunning)
                unstuckStandingStillTimer.Start();

            if (!archBuddyFixTimer.IsRunning && Me.IsFlying && Me.Combat)
                archBuddyFixTimer.Start();
            else if (archBuddyFixTimer.IsRunning && (!Me.IsFlying || !Me.Combat))
                archBuddyFixTimer.Reset();

            if (!swimFixTimer.IsRunning && Me.IsSwimming)
                swimFixTimer.Start();
            else if (swimFixTimer.IsRunning && !Me.IsSwimming)
                swimFixTimer.Reset();

            if (!mountFixTimer.IsRunning && Me.Mounted && !Me.IsFlying && !Me.HasAura("Preparation"))
                mountFixTimer.Start();
            else if (mountFixTimer.IsRunning && (!Me.Mounted || Me.IsFlying || Me.HasAura("Preparation")))
                mountFixTimer.Reset();

            if (RestartStuckMinutes != 0 && restartStandingStillTimer.Elapsed.TotalMinutes > RestartStuckMinutes)
            {
                restartStandingStillTimer.Reset();

                if (restartIntervalThread != null && restartIntervalThread.IsAlive)
                {
                    restartIntervalThread.Abort();
                }
                if (restartWhenStuckThread != null && restartWhenStuckThread.IsAlive)
                {
                    restartWhenStuckThread.Abort();
                }

                restartWhenStuckThread = new Thread(new ThreadStart(RestartWhenStuckThread));
                restartWhenStuckThread.Start();
                return true;
            }

            if (UnstuckRoutineMinutes > 0 && unstuckStandingStillTimer.Elapsed.TotalMinutes > UnstuckRoutineMinutes)
            {
                swimFixTimer.Reset();
                unstuckStandingStillTimer.Reset();
                UnstuckRoutine();
                return true;
            }

            if (PoolFisherFixMinutes != 0 && swimFixTimer.Elapsed.TotalMinutes > PoolFisherFixMinutes)
            {
                swimFixTimer.Reset();
                unstuckStandingStillTimer.Reset();
                UnstuckRoutine();
                return true;
            }

            if (ArchBuddyFixMinutes != 0 && archBuddyFixTimer.Elapsed.TotalMinutes > ArchBuddyFixMinutes)
            {
                Log("Dismounting while flying");
                archBuddyFixTimer.Reset();
                Mount.Dismount();
                return true;
            }

            if (MountFixMinutes != 0 && mountFixTimer.Elapsed.TotalMinutes > MountFixMinutes)
            {
                Log("Dismounting to unstuck");
                mountFixTimer.Reset();
                Mount.Dismount();
                return true;
            }

            return false;
        }

        private static void Log(string format, params object[] args)
        {
            Logging.Write(Color.DarkRed, "[StuckHelper] " + format, args);
        }
    }
}

