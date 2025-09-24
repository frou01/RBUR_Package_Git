
using UdonSharp;
using UnityEngine;

namespace frou01.RigidBodyTrain
{
    public class MortorAndWheel : UdonSharpBehaviour
    {
        /*[HideInInspector]*/
        [SerializeField] public float[] WheelTreadSpeed = new float[4];//初期化前にUpdateが走っても死なないようにしておく。車輪数が多い場合はそのようにインスペクターで変更のこと
        /*[HideInInspector]*/
        [SerializeField] public int index;
        /*[HideInInspector]*/
        [SerializeField] public float[] MortorForce = new float[1];//基本的に制御側から参照を渡される想定をしている。もちろん制御側が参照を貰ってもいい。
        /*[HideInInspector]*/
        [SerializeField] public float[] BrakeForce = new float[1];
        [SerializeField] WheelCollider wheel;
        [SerializeField] bool override_wheelFriction;
        [SerializeField] Rigidbody rb;
        float wheelRadius = 0;
        private void Start()
        {
            wheelRadius = wheel.radius;
            wheel.motorTorque = 0;
            wheel.brakeTorque = 0;

            friction = wheel.forwardFriction;
        }

        float currentTorque;
        float currentBrake;
        float TreadSpeed = 0;
        WheelFrictionCurve friction;
        private void FixedUpdate()
        {
            WheelTreadSpeed[index] = TreadSpeed = wheel.rotationSpeed * Mathf.Deg2Rad * wheelRadius;
            if (MortorForce[0] != currentTorque)
            {
                wheel.motorTorque = currentTorque = MortorForce[0] * wheelRadius;
            }

            if (BrakeForce[0] != currentBrake)
            {
                wheel.brakeTorque = currentBrake = BrakeForce[0] * wheelRadius; ;
            }
            if (override_wheelFriction)
            {
#if UNITY_EDITOR
                friction = wheel.forwardFriction;//パラメーター調整用。Runtimeでは動かない。
#endif
                friction.stiffness = 0.4f * (1 / (1 + Mathf.Abs(TreadSpeed - (Quaternion.Inverse(rb.rotation) * rb.velocity).z) * 4));//WheelCollider純正では粘着の計算がタイヤ仕様なので鉄輪化する
                wheel.forwardFriction = friction;
            }
        }
    }
}