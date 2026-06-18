
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BrakeConnectorValve : TrainConnectionReciever
    {
        [Header("auto assign on buildprocess (no overwrite)")]
        [SerializeField] protected AbstractBrake brakeModule;//Refered by BuildProcess
        [SerializeField] protected string PipeName = "BP";

        [SerializeField] protected bool F_B;
        [SerializeField][UdonSynced] protected bool OpenState;
        float[] connectingPressurePointer;

        public virtual void SetUpOnBuildProcess(Train train)//Call by BuildProcess
        {
            CouplerObj coupler = transform.parent.gameObject.GetComponent<CouplerObj>();
            if (coupler)
            {
                F_B = coupler.FrontOrBack;
            }
            if (brakeModule == null) brakeModule = (AbstractBrake)train.GetConnectionRecieverByTag("Brake");

            if (brakeModule.NeedReadOpenState())
            {
#pragma warning disable CS0612 // 型またはメンバーが旧型式です
                OpenState = F_B ? brakeModule.BrakeOpenF : brakeModule.BrakeOpenB;
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
                onUpdateConnectState();
                RequestSerialization();
            }

            onUpdateConnectState();
        }

        public virtual void PostProcessOnBuildProcess()
        {

        }

        private void Start()
        {
        }


        public override void TrainConnectionUpdate(Train newConnectedTrain, bool F_B)
        {
            if(this.F_B != F_B) return;

            if (newConnectedTrain)
            {
                connectingPressurePointer = ((AbstractBrake)newConnectedTrain.GetConnectionRecieverByTag("Brake")).getStraightPressurePointer(PipeName);
            }
            else
            {
                connectingPressurePointer = null;
            }
            onUpdateConnectState();
        }
        [PublicAPI]
        public bool isFront()
        {
            return F_B;
        }

        protected virtual void onUpdateConnectState()
        {
            if(OpenState) brakeModule.setConnectedPressurePointer(PipeName,connectingPressurePointer,F_B);
            else brakeModule.setConnectedPressurePointer(PipeName, brakeModule.getStraightPressurePointer(PipeName), F_B);
        }

        public virtual void owner_OpenBrakeValve()
        {
            OpenState = true;
            onUpdateConnectState();
            RequestSerialization();
        }

        public virtual void owner_CloseBrakeValve()
        {
            OpenState = false;
            onUpdateConnectState();
            RequestSerialization();
        }

        [PublicAPI]
        [NetworkCallable]
        public void OpenValve()
        {
            if (Networking.IsOwner(this.gameObject))
            {
                owner_OpenBrakeValve();
            }
            else
            {
                SendCustomNetworkEvent(NetworkEventTarget.Owner,nameof(OpenValve));
            }
        }

        [PublicAPI]
        [NetworkCallable]
        public void CloseValve()
        {
            if (Networking.IsOwner(this.gameObject))
            {
                owner_CloseBrakeValve();
            }
            else
            {
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(CloseValve));
            }
        }

        public override void Interact()
        {
            if (!OpenState)
            {
                OpenValve();
            }
            else
            {
                CloseValve();
            }
        }

        public override void OnDeserialization()
        {
            onUpdateConnectState();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            AbstractBrake _brakemodule = null;
            if (gameObject.GetComponentInParent<Train>())
            {
                _brakemodule = gameObject.GetComponentInParent<Train>().gameObject.GetComponentInChildren<AbstractBrake>();
            }
            CouplerObj coupler = null;
            if (transform.parent && (coupler = transform.parent.gameObject.GetComponent<CouplerObj>()))
            {
                if (coupler.FrontOrBack)
                {
                    Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                }
                else
                {
                    Gizmos.color = new Color(0.2f, 0.2f, 0.8f, 1f);
                }
                Gizmos.DrawSphere(transform.position, 0.1f);
                Gizmos.DrawLine(transform.position, coupler.transform.position);
                if(_brakemodule != null) Gizmos.DrawLine(coupler.transform.position, _brakemodule.transform.position);
            }
            else
            {
                if (F_B)
                {
                    Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                }
                else
                {
                    Gizmos.color = new Color(0.2f, 0.2f, 0.8f, 1f);
                }
                if (_brakemodule != null) Gizmos.DrawLine(transform.position, _brakemodule.transform.position);
            }
        }
#endif
    }
}
