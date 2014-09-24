using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LifeBoat
{
    public class LifeBoat : PartModule
    {
        [KSPField]
        public string deployAnimationName = "DeployPod";
        [KSPField]
        public string hatchAnimationName = "moveHatch";
        [KSPField]
        public string kickAnimationName = "Kickstand";

        [KSPField] 
        public float foodAmount = 1f;

        [KSPField] 
        public float oxyAmount = 776.4f;

        [KSPField]
        public float waterAmount = 5f;

        [KSPField]
        public float monoAmount = 7.5f;

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        [KSPField(isPersistant = true)]
        public bool isUnsealed = false;
        
        [KSPAction("Disconnect")]
        public void DisconnectPod(KSPActionParam param)
        {
            DisconnectLifeboat();
        }
        
        public Animation DeployAnimation
        {
            get
            {
                return part.FindModelAnimators(deployAnimationName)[0];
            }
        }
        public Animation KickAnimation
        {
            get
            {
                return part.FindModelAnimators(kickAnimationName)[0];
            }
        }

        public Animation HatchAnimation
        {
            get
            {
                return part.FindModelAnimators(hatchAnimationName)[0];
            }
        }


        [KSPAction("Evacuate")]
        public void EvacuateShip(KSPActionParam param)
        {
            TryToEvacuate(true);
        }

        private void TryToEvacuate(bool firstTry)
        {
            try
            {
                if (vessel.GetCrewCount() > 0)
                {
                    if (!isDeployed)
                    {
                        InflateLifeboat();
                    }
                    if (part.CrewCapacity == 1 && !part.protoModuleCrew.Any())
                    {
                        print("Evacuating!");
                        var source = vessel.Parts.First(x => x != part
                                                             && !x.Modules.Contains("LifeBoat")
                                                             && x.protoModuleCrew.Count > 0);

                        if (source != null)
                        {
                            print("Attempting to evacuate...");
                            var k = source.protoModuleCrew.First();
                            if (k != null)
                            {
                                print("Evacuating " + k.name);
                                source.RemoveCrewmember(k);
                                k.seat = null;
                                k.rosterStatus = ProtoCrewMember.RosterStatus.Available;

                                //Add Crewmember
                                part.AddCrewmember(k);
                                k.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                if (k.seat != null)
                                    k.seat.SpawnCrew();

                                //Decouple!
                                var p = part.parent;
                                if (p.Modules.OfType<ModuleDecouple>().Any())
                                {
                                    var dpart = p.Modules.OfType<ModuleDecouple>().First();
                                    dpart.Decouple();
                                }
                            }
                            else
                            {
                                print("Problem evacuating crewmember...");
                                if (firstTry)
                                    TryToEvacuate(false);
                            }
                        }
                        else
                        {
                            print("Could not find any crew to move");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                print("ERROR: " + ex.Message);
                if (firstTry)
                    TryToEvacuate(false);
            }
            
        }


        [KSPEvent(guiName = "Inflate Lifeboat", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void InflateLifeboat()
        {
            try
            {
                if (!isDeployed)
                {
                    PlayDeployAnimation();
                }
            }
            catch (Exception ex)
            {
                print("ERR in InflateLifeboat: " + ex.StackTrace);
            }
        }

        private void PlayDeployAnimation(int speed = 1)
        {
            print("Inflating");
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);
            HatchAnimation[hatchAnimationName].speed = speed;
            HatchAnimation.Play(hatchAnimationName);
            isDeployed = true;
            part.CrewCapacity = 1;
            ToggleEvent("InflateLifeboat", false);
            ToggleEvent("DeployKickstand", true);
            if (!isUnsealed)
            {
                AddResources();
                isUnsealed = true;
            }
        }

        public void DeflateLifeboat(int speed)
        {
            print("Deflating");
            DeployAnimation[deployAnimationName].time = DeployAnimation[deployAnimationName].length;
            DeployAnimation[deployAnimationName].speed = speed;
            DeployAnimation.Play(deployAnimationName);
            HatchAnimation[hatchAnimationName].speed = speed;
            HatchAnimation.Play(hatchAnimationName);
            part.CrewCapacity =0;
        }

        [KSPEvent(guiName = "Disconnect", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void DisconnectLifeboat()
        {
            part.decouple(100f);
            ToggleEvent("DisconnectLifeboat", false);
        }


        [KSPEvent(guiName = "Kickstand", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void DeployKickstand()
        {
            if (isDeployed)
            {
                KickAnimation[kickAnimationName].speed = 1;
                KickAnimation.Play(kickAnimationName);
                ToggleEvent("DeployKickstand", false);
            }
        }

        private void ToggleEvent(string eventName, bool state)
        {
            Events[eventName].active = state;
            Events[eventName].externalToEVAOnly = state;
            Events[eventName].guiActive = state;
        }

        public override void OnStart(StartState state)
        {
            DeployAnimation[deployAnimationName].layer = 2;
            HatchAnimation[hatchAnimationName].layer = 3;
            KickAnimation[kickAnimationName].layer = 2;
            if (!isDeployed) DeflateLifeboat(-10);
            else PlayDeployAnimation(10);
            base.OnStart(state);
        }


        private void AddResources()
        {
            var f = part.Resources["Food"];
            var w = part.Resources["Water"];
            var o = part.Resources["Oxygen"];
            var m = part.Resources["MonoPropellant"];

            var massToLose = 0f;

            if (m != null)
            {
                m.amount = monoAmount;
                m.maxAmount = monoAmount;
                massToLose += m.info.density * monoAmount;
            }
            if (f != null)
            {
                f.maxAmount = foodAmount;
                f.amount = foodAmount;
                massToLose += f.info.density * foodAmount;
            }
            if (w != null)
            {
                w.maxAmount = waterAmount;
                w.amount = waterAmount;
                massToLose += w.info.density * waterAmount;
            }
            if (o != null)
            {
                o.maxAmount = oxyAmount;
                o.amount = oxyAmount;
                massToLose += o.info.density * oxyAmount;
            }
            part.mass -= massToLose;
            if (part.mass < 0.05f) part.mass = 0.05f;

        }
    }
}
