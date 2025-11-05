
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
        }

        Vector3 tempForceVector;
        private void FixedUpdate()
        {
            WheelTreadSpeed[index] = trainTransform.InverseTransformVector(wheel.angularVelocity).x * wheelRadius;

            tempForceVector = trainTransform.up;

            wheel.AddForce(-tempForceVector * Vector3.Dot(trainTransform.up, Vector3.up) * WheelPressure[0]);
            wheel.AddRelativeTorque(MortorForce[0] * wheelRadius, 0, 0, ForceMode.Force);
            brake.AddForce(-tempForceVector * BrakeForce[0]/ brakeFriction * wheelRadius, ForceMode.Force);

            if (overrideFriction)
            {
                wheelMaterial.staticFriction = Friction[0];
                wheelMaterial.dynamicFriction = Friction[1];
            }
        }

        int checkInterval;
        int checkCounter;
        private void Update()
        {
            checkCounter += 1;
            if(checkCounter < checkInterval)
            {
                CheckWheelTransform();
                checkCounter = 0;
                checkInterval = Random.Range(0, 60);
            }
        }
        private void CheckWheelTransform()
        {
            if (BrakeTransform.localPosition.y - WheelTransform.localPosition.y < wheelRadius)
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