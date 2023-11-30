
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TurnTable_Controller : UdonSharpBehaviour
    {
        Vector3 rollingInput;

        public GameObject targetTable;
        Transform tableTransform;

        public Rail_Script mine;
        public Rail_Script[] targets;

        Quaternion initialRotation;
        private float prevTableRotation;
        float prevSyncedTableRotation;
        [SerializeField] Animator animator;
        int mortorTorqueParam;
        [UdonSynced] public float syncedTableRotation;
        float localTableRotation;
        [UdonSynced] bool Active;

        void Start()
        {
            initialRotation = targetTable.transform.localRotation;
            tableTransform = targetTable.transform;
            tableTransform.localRotation = initialRotation;
            tableTransform.Rotate(0, syncedTableRotation, 0);
            mortorTorqueParam = Animator.StringToHash("mortorTorque");
            updateRail();
        }
        public override void Interact()
        {
            //Debug.Log("mine " + mine);
            //if (mine.next != null) Debug.Log("mine.next " + mine.next);
            //if (mine.prev != null) Debug.Log("mine.prev " + mine.prev);
            if (Networking.LocalPlayer != null) Networking.LocalPlayer.UseAttachedStation();
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            Active = true;
        }

        Quaternion prevTableTransformRotation;

        float timeFromSync;
        public void Update()
        {
            bool isowner = Networking.IsOwner(this.gameObject);
            if (!isowner)
            {
                timeFromSync += Time.deltaTime;
            }
            if (Active)
            {
                if (isowner)
                {
                    syncedTableRotation += animator.GetFloat(mortorTorqueParam) * 2.4f * Time.deltaTime;
                    prevSyncedTableRotation = localTableRotation = syncedTableRotation;
                }
                else
                {
                    localTableRotation = Mathf.Lerp(prevSyncedTableRotation, syncedTableRotation, timeFromSync / 0.2f);
                }
            }
            else
            {
                this.enabled = false;
            }
            if (isowner)
            {
                timeFromSync += Time.deltaTime;
                if (timeFromSync > 0.2f)
                {
                    syncedTableRotation = wrapAngleTo180(syncedTableRotation);
                    timeFromSync = 0;
                    RequestSerialization();
                }
            }

            if (localTableRotation != prevTableRotation)
            {
                updateRail();
            }

            //if (Active || tableTransform.rotation != prevTableTransformRotation)
            //{
            //    
            //}
            //prevTableTransformRotation = tableTransform.localRotation;
        }
        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
            timeFromSync = 0;
            localTableRotation = prevSyncedTableRotation = syncedTableRotation;
        }

                private float wrapAngleTo180(float controllerAngle)
        {
            controllerAngle %= 360;
            controllerAngle = controllerAngle > 180 ? controllerAngle - 360 : controllerAngle;
            controllerAngle = controllerAngle < -180 ? controllerAngle + 360 : controllerAngle;
            return controllerAngle;
        }
        void updateRail()
        {
            tableTransform.localRotation = initialRotation;
            tableTransform.Rotate(0, localTableRotation, 0);
            Vector3 currentStart = mine.GetStartPoint();
            Vector3 currentEnd = mine.GetEndPoint();
            mine.prev = null;
            mine.next = null;

            float Sdistance = ((currentStart - currentEnd) / 2).sqrMagnitude;
            float Edistance = ((currentStart - currentEnd) / 2).sqrMagnitude;
            foreach (Rail_Script target in targets)
            {
                if (Sdistance > (target.GetStartPoint() - currentStart).sqrMagnitude)
                {
                    Sdistance = (target.GetStartPoint() - currentStart).sqrMagnitude;
                    mine.prev = target;
                }
                else if (Sdistance > (target.GetEndPoint() - currentStart).sqrMagnitude)
                {
                    Sdistance = (target.GetEndPoint() - currentStart).sqrMagnitude;
                    mine.prev = target;
                }
                if (Edistance > (target.GetStartPoint() - currentEnd).sqrMagnitude)
                {
                    Edistance = (target.GetStartPoint() - currentEnd).sqrMagnitude;
                    mine.next = target;
                }
                else if (Edistance > (target.GetEndPoint() - currentEnd).sqrMagnitude)
                {
                    Edistance = (target.GetEndPoint() - currentEnd).sqrMagnitude;
                    mine.next = target;
                }
            }
            prevTableRotation = localTableRotation;
        }


        public override void OnStationEntered(VRC.SDKBase.VRCPlayerApi player)
        {
            Active = true;
            this.enabled = true;
        }
        public override void OnStationExited(VRC.SDKBase.VRCPlayerApi player)
        {
            Active = false;
        }
        //public override void InputLookHorizontal(float value, VRC.Udon.Common.UdonInputEventArgs args)
        //{
        //
        //    if (Networking.LocalPlayer != null && !Networking.LocalPlayer.IsUserInVR())
        //    {
        //        rollingInput.x += args.floatValue;
        //        if (rollingInput.x > 2.4f) rollingInput.x = 2.4f;
        //        else if (rollingInput.x < -2.4f) rollingInput.x = -2.4f;
        //    }
        //    else
        //    {
        //        rollingInput.x = args.floatValue * 2.4f;
        //    }
        //}

        public override void OnDeserialization()
        {
            prevSyncedTableRotation = localTableRotation;

            float delta = wrapAngleTo180(syncedTableRotation - prevSyncedTableRotation);
            syncedTableRotation = prevSyncedTableRotation + delta;

            if (Active) this.enabled = true;
            timeFromSync = 0;
        }
    }
}
