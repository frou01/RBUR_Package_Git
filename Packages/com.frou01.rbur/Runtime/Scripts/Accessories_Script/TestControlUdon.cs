using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace frou01.RigidBodyTrain
{
    public class TestControlUdon : UdonSharpBehaviour
    {
        [SerializeField] Toggle Accel_speed_Toggle;
        [SerializeField] TMP_InputField ID_InputField;
        [SerializeField] TMP_InputField Mul_InputField;
        [SerializeField] Slider Throttle_Slider;

        bool IsAccelMode;
        int TargetID;
        [SerializeField][HideInInspector] public TrainManager TrainManager;
        Train targetTrain;
        float ControlMultiplier;
        float Throttle;
        public void ChangeControlMode()
        {
            IsAccelMode = Accel_speed_Toggle.isOn;
        }
        public void ChangeTargetID()
        {
            if(targetTrain != null)
            {
                targetTrain.GetComponent<ConstantForce>().relativeForce = Vector3.zero;
            }
            TargetID = -1;
            int.TryParse(ID_InputField.text,out TargetID);
            if(0 <=  TargetID && TargetID < TrainManager.Trains.Length)
            {
                targetTrain = TrainManager.Trains[TargetID];

                Networking.SetOwner(Networking.LocalPlayer, targetTrain.gameObject);
            }
            else
            {
                targetTrain = null;
            }
            ApplyControl();
        }
        public void ChangeMultiplier()
        {
            float.TryParse(Mul_InputField.text, out ControlMultiplier);
            ApplyControl();
        }
        public void ThrottleChanged()
        {
            Throttle = Throttle_Slider.value;
            ApplyControl();
        }

        void FixedUpdate()
        {
            ApplyControl();
        }

        private void ApplyControl()
        {
            if (targetTrain)
            {
                if (Mathf.Abs(Throttle) < 0.1) Throttle = 0;
                if (IsAccelMode)
                {
                    targetTrain.GetComponent<ConstantForce>().relativeForce = new Vector3(0, 0, Throttle * ControlMultiplier);
                }
                else
                {
                    targetTrain.GetComponent<Rigidbody>().velocity = targetTrain.transform.rotation * new Vector3(0, 0, Throttle * ControlMultiplier);
                }
            }
        }
    }
}