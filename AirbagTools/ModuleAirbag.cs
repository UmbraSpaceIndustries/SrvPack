using System;
using UnityEngine;

namespace AirbagTools
{
    public class ModuleAirbag : PartModule
    {
        [KSPField] 
        public float dampenFactor = .75f;
        [KSPField] 
        public float dampenSpeed = 15f;
        [KSPField]
        public String deployAnimationName = "Deploy";

        [KSPField(isPersistant = true)]
        public bool isCharged = true;

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        private StartState _state;

        [KSPAction("Inflate")]
        public void InflateAction(KSPActionParam param)
        {
            InflateAirbags();
        }

        [KSPAction("Deflate")]
        public void DeflateAction(KSPActionParam param)
        {
            DeflateAirbags();
        }

        [KSPAction("Disconnect")]
        public void DisconnectAction(KSPActionParam param)
        {
            DisconnectAirbags();
        }
        public void DisconnectAirbags()
        {
            part.decouple(200f);
        }


        [KSPEvent(guiName = "Inflate Airbags", guiActiveEditor=true)]
        public void InflateAirbags()
        {
            if (!isDeployed)
            {
                if (isCharged || _state == StartState.Editor)
                {
                    isCharged = false;
                    isDeployed = true;
                    DeployAnimation[deployAnimationName].speed = 1;
                    DeployAnimation.Play(deployAnimationName);
                    ToggleEvent("DeflateAirbags", true);
                    ToggleEvent("InflateAirbags", false);
                }
            }
        }

        [KSPEvent(guiName = "Deflate Airbags", guiActiveEditor = false)]
        public void DeflateAirbags()
        {
            if (isDeployed)
            {
                isDeployed = false;
                DeployAnimation[deployAnimationName].speed = -.25f;
                DeployAnimation[deployAnimationName].time = DeployAnimation[deployAnimationName].length;
                DeployAnimation.Play(deployAnimationName);
                ToggleEvent("DeflateAirbags", false);
                Events["RechargeAirbags"].active = true;
            }
        }
        
        [KSPEvent(guiName = "Recharge Airbags", guiActive = false, externalToEVAOnly = true, guiActiveEditor = true, active=false, guiActiveUnfocused=true, unfocusedRange=3.0f)]
        public void RechargeAirbags()
        {
            isCharged = true;
            Events["RechargeAirbags"].active = false;
            ToggleEvent("InflateAirbags", true);
        }
        private void ToggleEvent(string eventName, bool state)
        {
            Events[eventName].active = state;
            Events[eventName].externalToEVAOnly = state;
            Events[eventName].guiActiveEditor = state;
            Events[eventName].guiActive = state;
        }

        public Animation DeployAnimation
        {
            get
            {
                return part.FindModelAnimators(deployAnimationName)[0];
            }
        }


        public void OnFixedUpdate()
        {
            try
            {
                if (part.checkLanded())
                {
                    Dampen();
                }
                if (part.Landed)
                {
                    Dampen();
                }
            }
            catch (Exception ex)
            {
                print("[AB] Error in OnFixedUpdate - " + ex.Message);
            }
        }

        private void Dampen()
        {
            if (vessel.srfSpeed > dampenSpeed
                || vessel.horizontalSrfSpeed > dampenSpeed)
            {
                //print("Dampening...");
                foreach (var p in vessel.parts)
                {
                    p.Rigidbody.angularVelocity *= dampenFactor;
                    p.Rigidbody.velocity *= dampenFactor;
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            Setup();
        }

        public override void OnStart(StartState state)
        {
            _state = state;
            Setup();
        }

        private void Setup()
        {
            print("Deployed: " + isDeployed);
            print("Charged: " + isCharged);
            if (vessel != null)
            {
                part.force_activate();
                if (isDeployed)
                {
                    ToggleEvent("DeflateAirbags", true);
                    ToggleEvent("InflateAirbags", false);
                }
                else
                {
                    ToggleEvent("DeflateAirbags", false);
                    ToggleEvent("InflateAirbags", true);
                }
            }
        }
    }
}
