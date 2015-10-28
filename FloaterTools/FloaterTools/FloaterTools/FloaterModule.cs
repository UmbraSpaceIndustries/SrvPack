using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tac;

namespace FloaterTools
{
    public class FloaterModule : PartModule
    {




        private float inflationTimeTotal = 0f;
        private float inflationTimeLeft = 0f;
        public float pumpSpeed = 0f;

        #region Fields
        [KSPField]
        public String deployAnimationName = "Deploy";

        [KSPField]
        public float deployAnimationSpeed = -2f;

        [KSPField]
        public float retractAnimationSpeed = 2f;

        [KSPField]
        public float instantAnimationSpeed = 1000f;

        [KSPField]
        public float maxBuoyancy = 50f;

        [KSPField]
        public float minBuoyancy = 0f;

        [KSPField]
        public float buoyancyChangeInterval = 10f;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Inflation Status")]
        public float inflationStatus = 0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Buoyancy"), UI_FloatRange(minValue = 0f, maxValue = 50f, stepIncrement = 1f)]
        public float buoyancyWhenDeployed = 25f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Inflation speed %"), UI_FloatRange(minValue = 10f, maxValue = 100f, stepIncrement = 10f)]
        public float inflationSpeed = 100f;

        [KSPField(isPersistant = true)]
        public float buoyancyWhenStowed = 0f;

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        [KSPField]
        public string FireSpitterBuoyancyFieldName = "buoyancyForce";

        [KSPField]
        public string FireSpitterBuoyancyModuleName = "FSbuoyancy";


        #endregion

        #region Method overrides
        public override void OnStart(StartState state)
        {
            DeployAnimation.wrapMode = WrapMode.ClampForever;
            SetDeployedState();
            if (isDeployed)
                PlayDeployAnimation(instantAnimationSpeed);
            base.OnStart(state);
        }


        public void FixedUpdate()
        {
            inflationTimeLeft = Math.Max(0f, inflationTimeLeft - TimeWarp.fixedDeltaTime);

            SyncFireSpitterBuoyancy();
        }

        #endregion

        #region Firespitter helper methods
        private bool SyncFireSpitterBuoyancy()
        {
            var newBuoyancy = isDeployed ? buoyancyWhenDeployed : buoyancyWhenStowed;
            newBuoyancy = newBuoyancy * InflationStatusMultiplier;                      //Set newBuoyancy to scale along with the animation
            if (FireSpitterBuoyancy == newBuoyancy)
                return true;                                                            //Firespitter is already set correctly, no need to update it
            return part.Modules[FireSpitterBuoyancyModuleName]
                .Fields.SetValue(FireSpitterBuoyancyFieldName, newBuoyancy);
        }

        private float FireSpitterBuoyancy
        {
            get { return (float)part.Modules[FireSpitterBuoyancyModuleName]
                    .Fields.GetValue(FireSpitterBuoyancyFieldName); }
        } 
        #endregion


        /// <summary>
        /// Set visibility of Events
        /// </summary>
        private void UpdateEvents()
        {
            part.Modules[FireSpitterBuoyancyModuleName].Fields[FireSpitterBuoyancyFieldName].guiActiveEditor = false;  //Firespitter slider should be disabled everywhere
            part.Modules[FireSpitterBuoyancyModuleName].Fields[FireSpitterBuoyancyFieldName].guiActive = false;

            Events["DeployEvent"].active = !isDeployed;
            Events["RetractEvent"].active = isDeployed;

            Actions["DeployAction"].active = !isDeployed || HighLogic.LoadedSceneIsEditor;
            Actions["RetractAction"].active = isDeployed || HighLogic.LoadedSceneIsEditor;
        }
        

        /// <summary>
        /// Sets deployed state without animation
        /// </summary>
        private void SetDeployedState()
        {
            SyncFireSpitterBuoyancy();
            UpdateEvents();
        }
        
        /// <summary>
        /// Sets a new deployed state, including animation
        /// </summary>
        private void SetDeployedState(bool newState)
        {
            isDeployed = newState;
            PlayDeployAnimation();
            SetDeployedState();
        }


        #region Animation support methods
        /// <summary>
        /// Plays demployment animation at default speed
        /// </summary>
        /// <param name="reverse">If true, animation will play backwards</param>
        private void PlayDeployAnimation()
        {
            var playbackSpeed = isDeployed ? retractAnimationSpeed : deployAnimationSpeed;
            playbackSpeed = playbackSpeed * (inflationSpeed / 100f);
            PlayDeployAnimation(playbackSpeed);
        }

        /// <summary>
        /// Plays deployment animation at a set speed
        /// </summary>
        /// <param name="reverse">If true, animation will play backwards</param>
        private void PlayDeployAnimation(float playbackSpeed)
        {
            pumpSpeed = playbackSpeed;
            DeployAnimation[deployAnimationName].speed = playbackSpeed;

            var length = DeployAnimation[deployAnimationName].length;
            var time = DeployAnimation[deployAnimationName].time;

            inflationTimeTotal = Math.Abs(length / playbackSpeed);    //Needed for the ugly workaround in InflationStatusMultiplier
            inflationTimeLeft = inflationTimeTotal;


            DeployAnimation[deployAnimationName].normalizedTime = Mathf.Clamp01(DeployAnimation[deployAnimationName].normalizedTime);
            DeployAnimation.Play(deployAnimationName);
        }
        
        public Animation DeployAnimation
        {
            get
            {
                return part.FindModelAnimators(deployAnimationName)[0];
            }
        } 

        /// <summary>
        /// Gets current inflation % to allow for gradual inflation/deflation. Big scary note: relies on current animation time.
        /// </summary>
        public float InflationStatusMultiplier
        {
            get
            {
                inflationStatus = (inflationTimeTotal - inflationTimeLeft) / inflationTimeTotal;
                //How much time is left on the inflation timer?
                //Ideally, it would use Animation.IsPlaying and Animation.time, but neiher of those seem to work right when the animation is playing backwards
                if (inflationTimeLeft > 0)
                    return (inflationTimeTotal - inflationTimeLeft) / inflationTimeTotal;
                else
                    return 1f;
            }
        }

        #endregion

        #region Right click menu events
        [KSPEvent(active = false, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, guiName = "Deploy", unfocusedRange = 5.0f)]
        public void DeployEvent()
        {
            SetDeployedState(true);
        }

        [KSPEvent(active = false, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, guiName = "Retract", unfocusedRange = 5.0f)]
        public void RetractEvent()
        {
            SetDeployedState(false);
        }
        #endregion

        #region Bindable actions
        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam param)
        {            
            SetDeployedState(true);
        }

        [KSPAction("Retract")]
        public void RetractAction(KSPActionParam param)
        {
            SetDeployedState(false);
        }
        
        [KSPAction("Toggle")]
        public void ToggleAction(KSPActionParam param)
        {
            SetDeployedState(!isDeployed);
        }

        [KSPAction("More Buoyancy")]
        public void IncreaseBuoyancyAction(KSPActionParam param)
        {
            buoyancyWhenDeployed = Math.Min(maxBuoyancy, buoyancyWhenDeployed + buoyancyChangeInterval);
            SyncFireSpitterBuoyancy();
        }
        [KSPAction("Less Buoyancy")]
        public void DecreaseBuoyancyAction(KSPActionParam param)
        {
            buoyancyWhenDeployed = Math.Max(minBuoyancy, buoyancyWhenDeployed - buoyancyChangeInterval);
            SyncFireSpitterBuoyancy();
        }
        #endregion

    }
}
