
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
        [SerializeField] private bool overrideFriction = false;
        [SerializeField] public float[] Friction = new float[] { 0.5f, 0.2f };//Static/Dynamic
        [SerializeField] Rigidbody rb;
        private Transform trainTransform;
        [SerializeField] Rigidbody wheel;
        PhysicMaterial wheelMaterial;
        [SerializeField] Rigidbody brake;
        [SerializeField] float[] WheelPressure = new float[1];
        [SerializeField] float RBTorqueBrakeStartSpeed = 5;
        [SerializeField] float RBTorqueBrakeFullSpeed = 10;
        [SerializeField] float RBTorqueBrakePressShoeForce = 100;
        float wheelRadius = 0;
        float brakeFriction = 1;
        Transform WheelTransform;
        Transform BrakeTransform;
        Vector3 wheelInitialPos;
        Vector3 BrakeInitialPos;
        private void Start()
        {
            wheel.maxAngularVelocity = 1000;
            wheel.isKinematic = false;
            wheelRadius = wheel.GetComponent<SphereCollider>().radius;
            wheelMaterial = wheel.GetComponent<SphereCollider>().material;
            trainTransform = rb.transform;
            brakeFriction = brake.GetComponent<CapsuleCollider>().material.dynamicFriction;
            brake.isKinematic = false;

            WheelTransform = wheel.transform;
            BrakeTransform = brake.transform;
            wheelInitialPos = WheelTransform.localPosition;
            BrakeInitialPos = BrakeTransform.localPosition;

            brakeTorque_transitionRange = RBTorqueBrakeFullSpeed - RBTorqueBrakeStartSpeed;
        }

        Vector3 tempForceVector;
        float treadSpeed = 0;
        float brakeTorque_transitionRange;
        float brakeTorque_interpolation;
        float MortorBrakeTorque;
        float ShoeBrakeTorque;
        private void FixedUpdate()
        {
            treadSpeed = WheelTreadSpeed[index] = trainTransform.InverseTransformVector(wheel.angularVelocity).x * wheelRadius;

            tempForceVector = trainTransform.up;
            if(BrakeForce[0] > RBTorqueBrakePressShoeForce)
            {
                if (treadSpeed > RBTorqueBrakeStartSpeed)
                {
                    brakeTorque_interpolation = (treadSpeed - RBTorqueBrakeStartSpeed) / brakeTorque_transitionRange;

                    MortorBrakeTorque = Mathf.Lerp(0 , BrakeForce[0] - RBTorqueBrakePressShoeForce, brakeTorque_interpolation);
                    ShoeBrakeTorque = (BrakeForce[0] - MortorBrakeTorque) / brakeFriction * wheelRadius;

                    brake.AddForce(-tempForceVector * ShoeBrakeTorque / brakeFriction * wheelRadius, ForceMode.Force);
                    wheel.AddRelativeTorque(-MortorBrakeTorque * wheelRadius, 0, 0, ForceMode.Force);
                    wheel.AddForce(-tempForceVector * Vector3.Dot(trainTransform.up, Vector3.up) * (WheelPressure[0] - ShoeBrakeTorque));

                }
                else if (treadSpeed < -RBTorqueBrakeStartSpeed)
                {
                    brakeTorque_interpolation = (-treadSpeed - RBTorqueBrakeStartSpeed) / brakeTorque_transitionRange;

                    MortorBrakeTorque = Mathf.Lerp(0, BrakeForce[0] - RBTorqueBrakePressShoeForce, brakeTorque_interpolation);
                    ShoeBrakeTorque = (BrakeForce[0] - MortorBrakeTorque) / brakeFriction * wheelRadius;

                    brake.AddForce(-tempForceVector * ShoeBrakeTorque , ForceMode.Force);
                    wheel.AddRelativeTorque(MortorBrakeTorque * wheelRadius, 0, 0, ForceMode.Force);
                    wheel.AddForce(-tempForceVector * Vector3.Dot(trainTransform.up, Vector3.up) * (WheelPressure[0] - ShoeBrakeTorque));
                }
                else
                {
                    brake.AddForce(-tempForceVector * BrakeForce[0] / brakeFriction * wheelRadius, ForceMode.Force);
                    wheel.AddForce(-tempForceVector * Vector3.Dot(trainTransform.up, Vector3.up) * (WheelPressure[0] - BrakeForce[0] / brakeFriction * wheelRadius));
                }
            }
            else
            {
                brake.AddForce(-tempForceVector * BrakeForce[0] / brakeFriction * wheelRadius, ForceMode.Force);
                wheel.AddForce(-tempForceVector * Vector3.Dot(trainTransform.up, Vector3.up) * (WheelPressure[0] - BrakeForce[0] / brakeFriction * wheelRadius));
            }
            if (MortorForce[0] != 0) wheel.AddRelativeTorque(MortorForce[0] * wheelRadius, 0, 0, ForceMode.Force);

            if (overrideFriction)
            {
                wheelMaterial.staticFriction = Friction[0];
                wheelMaterial.dynamicFriction = Friction[1];
            }
        }

        private static void BrakeTorque_Interpolation()
        {
            
        }

        int checkInterval;
        int checkCounter;
        private void Update()
        {
            checkCounter += 1;
            if(checkCounter > checkInterval) // fix https://github.com/frou01/RBUR_Package_Git/issues/42#issuecomment-4231819970
            {
                CheckWheelTransform();
                checkCounter = 0;
                checkInterval = Random.Range(0, 60);
            }
        }
        private void CheckWheelTransform()
        {
            if (BrakeTransform.localPosition.y - WheelTransform.localPosition.y < wheelRadius
                || Vector3.Distance(WheelTransform.localPosition, wheelInitialPos) > wheelRadius
                || BrakeInitialPos.y - BrakeTransform.localPosition.y > wheelRadius/ 10 /*from: https://github.com/frou01/RBUR_Package_Git/issues/42#issuecomment-4071492894 */)
            {
                Debug.Log("Object penetration : " + this.name);
                Debug.Log("WheelBody pos " + WheelTransform.localPosition);
                Debug.Log("BrakeBody pos " + BrakeTransform.localPosition);
                WheelTransform.localPosition = wheelInitialPos;
                BrakeTransform.localPosition = BrakeInitialPos;
                Physics.SyncTransforms();
            }
        }
    }
}