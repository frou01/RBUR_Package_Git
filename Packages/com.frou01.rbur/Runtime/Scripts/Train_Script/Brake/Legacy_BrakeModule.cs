using UnityEngine;

namespace frou01.RigidBodyTrain
{
    public class Legacy_BrakeModule : AbstractBrake
    {

        protected float changedSpeed;
        protected float lastSpeed;
        protected float m_nowSpeed;
        protected Transform chacedTransform;
        protected Rigidbody rigidbody_;
        protected float rigidBodyMass;
        protected float FixedDeltaTime;
        protected float m_brakeFactor;

        [SerializeField] protected float baseBrakePressure = 0.55f;
        [SerializeField] protected float BrakeMultiplier = 70000f;
        protected float currentFriction;
        [SerializeField] protected float friction = 90;
        [SerializeField] protected float static_friction = 2240;

        protected bool[] trainOwnerState = new bool[1];

        protected override void Start()
        {
            base.Start();


            chacedTransform = train.transform;
            rigidbody_ = train.GetComponent<Rigidbody>();
            rigidBodyMass = rigidbody_.mass;
            FixedDeltaTime = Time.fixedDeltaTime;
            trainOwnerState = train.exposedOwnerState;
        }
        protected override void Update()
        {
            base.Update();
        }
        protected override void LateUpdate()
        {
            base.LateUpdate();

            currentFriction = (1 / (1 + Mathf.Abs(localVelocity.z) * 10)) * static_friction + friction;
            m_brakeFactor = (baseBrakePressure - m_straightBrakePressure) / baseBrakePressure * 3.57f;// * 5/(5-((5-1.4)))
            if (m_brakeFactor > 1) m_brakeFactor = 1;
            if (m_brakeFactor < 0) m_brakeFactor = 0;
            brakeFactor[0] = m_brakeFactor / BrakeMultiplier;
            m_brakeFactor *= BrakeMultiplier * (0.5f + 0.5f / (1 + Mathf.Abs(localVelocity.z) / 5));
            m_brakeFactor += currentFriction;
        }

        float FunctionProxy_Float1;

        Vector3 currentVelocity;
        Vector3 localVelocity;
        void FixedUpdate()
        {
            currentVelocity = rigidbody_.velocity;
            localVelocity = Quaternion.Inverse(chacedTransform.rotation) * currentVelocity;
            m_nowSpeed = localVelocity.z;

            changedSpeed = m_nowSpeed - lastSpeed;
            if (trainOwnerState[0])
            {
                if (Mathf.Abs(m_nowSpeed + changedSpeed) * rigidBodyMass > m_brakeFactor * FixedDeltaTime)
                {
                    FunctionProxy_Float1 = m_nowSpeed > 0 ? -m_brakeFactor : m_brakeFactor;
                    //FunctionProxy_Vector1.z = FunctionProxy_Float1;
                    rigidbody_.AddRelativeForce(0, 0, FunctionProxy_Float1);
                    lastSpeed = m_nowSpeed + (m_nowSpeed > 0 ? -m_brakeFactor : m_brakeFactor) / rigidBodyMass * FixedDeltaTime;
                }
                else
                {
                    FunctionProxy_Float1 = -m_nowSpeed - changedSpeed;
                    //FunctionProxy_Vector1.z = FunctionProxy_Float1;
                    rigidbody_.AddRelativeForce(0, 0, FunctionProxy_Float1, ForceMode.VelocityChange);
                    lastSpeed = -changedSpeed;
                }
            }
            else
            {
                lastSpeed = m_nowSpeed;
            }
        }
    }
}
