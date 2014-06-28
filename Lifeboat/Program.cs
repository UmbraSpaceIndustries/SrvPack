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
            if (vessel.GetCrewCount() > 0)
            {
                if (!isDeployed)
                {
                    InflateLifeboat();
                }
                if (part.CrewCapacity == 1 && part.protoModuleCrew.Count == 0)
                {
                    ProtoCrewMember k = null;
                    print("Evacuating!");
                    //Remove Crewmember
                    foreach (var p in vessel.parts)
                    {
                        if (p.protoModuleCrew.Count > 0)
                        {
                            k = p.protoModuleCrew.First();
                            p.RemoveCrewmember(k);
                            k.seat = null;
                            k.rosterStatus = ProtoCrewMember.RosterStatus.AVAILABLE;
                        }
                    }
                    //Add Crewmember
                    part.AddCrewmember(k);
                    k.rosterStatus = ProtoCrewMember.RosterStatus.ASSIGNED;
                    if(k.seat != null)
                        k.seat.SpawnCrew();
                    //Decouple
                    part.decouple(200f);
                }
            }
        }

        [KSPEvent(guiName = "Inflate Lifeboat", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void InflateLifeboat()
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
