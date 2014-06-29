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
        public float foodAmount = 5;

        [KSPField] 
        public float oxyAmount = 15;

        [KSPField]
        public float waterAmount = 15;

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

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
                                k.rosterStatus = ProtoCrewMember.RosterStatus.AVAILABLE;

                                //Add Crewmember
                                part.AddCrewmember(k);
                                k.rosterStatus = ProtoCrewMember.RosterStatus.ASSIGNED;
                                if (k.seat != null)
                                    k.seat.SpawnCrew();
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
                    isDeployed = true;
                    DeployAnimation[deployAnimationName].speed = 1;
                    DeployAnimation.Play(deployAnimationName);
                    HatchAnimation[hatchAnimationName].speed = 1;
                    HatchAnimation.Play(hatchAnimationName);
                    part.CrewCapacity = 1;
                    ToggleEvent("InflateLifeboat", false);
                    ToggleEvent("DeployKickstand", true);
                    AddResources();
                }
            }
            catch (Exception ex)
            {
                print("ERR in InflateLifeboat: " + ex.Message);
            }
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
            base.OnStart(state);
        }

        private void AddResources()
        {
            var f = part.Resources["Food"];
            var w = part.Resources["Water"];
            var o = part.Resources["Oxygen"];

            f.maxAmount = foodAmount;
            f.amount = foodAmount;
            w.maxAmount = waterAmount;
            w.amount = waterAmount;
            o.maxAmount = oxyAmount;
            o.amount = oxyAmount;

        }
    }
}
