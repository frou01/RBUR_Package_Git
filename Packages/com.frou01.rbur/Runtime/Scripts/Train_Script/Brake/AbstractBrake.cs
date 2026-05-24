using JetBrains.Annotations;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;


namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class AbstractBrake : TrainConnectionReciever
    {
        //ブレーキ系の標準
        //前後接続、圧力配送
        //手ブレーキ受け入れは拡張クラスでやるのが良さそう
        [Tooltip("this object attached Train. auto assing parent by BuildProcess (no overwrite)")]
        [SerializeField] protected Train train;
        [SerializeField] public float[] straightBrakePressure = new float[1];
        [UdonSynced(UdonSyncMode.Linear)] protected float m_straightBrakePressure;//4byte,[MPa]
        //[SerializeField] protected BrakePipeCocks BPCocks;

        protected float[] MNG_DeltaTime;
        protected float DeltaTime;

        protected float pressure_delta_F;//流量の単位は[kg/s]
        protected float pressure_delta_B;

        protected float[] ConnectedBrakePressure_F;
        protected float[] ConnectedBrakePressure_B;
        protected float connectedPr_F;
        protected float connectedPr_B;

        [SerializeField] private bool UseLegacyPipeState = true;
        [Obsolete]
        public bool BrakeOpenF;//1byte
        [Obsolete]
        public bool BrakeOpenB;//1byte
        protected float maxOverShoot;
        [SerializeField] public Animator indicateAnimator;
        protected bool hasAnimator;
        protected int brakePressureParamaterID;

        [HideInInspector] public float[] brakeFactor = new float[1];

        public virtual void SetUpOnBuildProcess(Train train)
        {
            if(this.train == null) this.train = train;
        }
        public virtual void PostProcessOnBuildProcess()
        {
        }
        public bool NeedReadOpenState()
        {
            return UseLegacyPipeState;
        }
        protected virtual void Start()
        {
            brakePressureParamaterID = Animator.StringToHash("BrakePressure");
            hasAnimator = indicateAnimator != null;
            isOwnerState = Networking.IsOwner(gameObject);
            MNG_DeltaTime = train.trainManager.DeltaTime;
            DeltaTime = MNG_DeltaTime[0];
            maxOverShoot = train.trainManager.maxOverShoot;
            //for (i = 0; i < past.Length; i++)
            //{
            //    past[i] = Time.fixedDeltaTime;
            //}

        }

        //抵抗は無視する。
        //参考 https://kenkidryer.jp/2020/09/03/pressure-flow-rate-bernoullis-principle/
        //気体の性質 https://www.hakko.co.jp/library/qa/qakit/html/h01040.htm
        //温度20度における密度定数 m = 11.5075252899[kg/m³*MPa] 理想気体では無いので近似的な物。

        //大気圧は0.101325[MPa]
        //圧力Q,qは[MPa]とする(Q>q)
        //ある圧力に於ける密度p [kg/m³] は m[kg/m³MPa]*Q[MPa]
        //V = 1000*√(2*(Q-q)[MPa]/(Q*m))[m/s]

        //S[m²]を管径とする
        //V*Sが体積流量[m³/s]である
        //質量流量はV*S*Q*m
        //=s*Q*m*1000*√(2*q[MPa]/Q*m)
        //=s*1000*√(2*q[MPa]*Q*m)

        //質量Mから圧力Qを求める（温度は20度で変わらないことにする）
        //Q = (M[kg]/m[kg/m³*MPa])/L[m³]

        //圧力変化を求めると、
        //ΔQ = 10³ * S/(L*√m)*√(2*(Q-q)*Q)
        //定数×√(2*(Q-q)*Q)になった

        //定数の参考値は
        //S=0.0001m²、BP管容量Lを0.02m³として
        //10³*S/(L*√m) = 1.47 = 1.5
        //Lは実際には各車で違うことが考えられる。

        //実際には流速が音速を下回る状況もある。その場合、以下の計算は甚だ不自然なものということになるが、今は考えないものとする。
        protected virtual void Update()
        {
            //Debug.Log("lowpassed deltaTime" + DeltaTime);
            if (isOwnerState)
            {
                m_straightBrakePressure = straightBrakePressure[0];
                m_straightBrakePressure += pressure_delta_F;
                if(ConnectedBrakePressure_F != null) ConnectedBrakePressure_F[0] -= pressure_delta_F;
                m_straightBrakePressure += pressure_delta_B;
                if (ConnectedBrakePressure_B != null) ConnectedBrakePressure_B[0] -= pressure_delta_B;
            }
            straightBrakePressure[0] = m_straightBrakePressure;
        }
        //音速/60=5.7mほど。このプログラムは波を送るには早いくらいか

        //protected float[] past = new float[5];
        //protected int i = 1;
        //protected float mam_sum;
        //protected virtual float mam_lpf(float _in)//Moving Average Method lowpassfilter
        //{
        //    mam_sum = 0;
        //    for (i = 1; i < past.Length; i++)
        //    {
        //        past[i-1] = past[i];
        //        mam_sum += past[i];
        //    }
        //    mam_sum += _in;

        //    past[i-1] = _in;
        //    return mam_sum/past.Length;
        //}

        protected virtual void LateUpdate()
        {
            if (isOwnerState)
            {
                updateStraightPressure();
            }

            if (hasAnimator)
            {
                indicateAnimator.SetFloat(brakePressureParamaterID, m_straightBrakePressure);
            }

            //ブレーキ圧はLateUpdateでもUpdateでも継承先では一貫した結果になるので、
            //どちらで処理してもよい
        }
        protected virtual void updateStraightPressure()
        {
            m_straightBrakePressure = straightBrakePressure[0];//LateUpdateではm_straightBrakePressureは参照のみ

            DeltaTime = MNG_DeltaTime[0];
            pressure_delta_F = 0;
            pressure_delta_B = 0;
            //低い方へ流す（高圧からは受け入れだけする）

            connectedPr_F = ConnectedBrakePressure_F == null ? 0f : ConnectedBrakePressure_F[0];
            if (connectedPr_F < m_straightBrakePressure)
            {
                pressure_delta_F = -1.5f * Mathf.Sqrt(2 * (m_straightBrakePressure - connectedPr_F) * m_straightBrakePressure) * DeltaTime;
                //pressure_delta_F = -1.5f * Mathf.Clamp(Mathf.Sqrt(2 * pressure_delta_F * m_straightBrakePressure), -pressure_delta_F, pressure_delta_F);
                //Debug.Log("pressure_delta_F " + pressure_delta_F);
            }

            connectedPr_B = ConnectedBrakePressure_B == null ? 0f : ConnectedBrakePressure_B[0];
            if (connectedPr_B < m_straightBrakePressure)
            {
                pressure_delta_B = -1.5f * Mathf.Sqrt(2 * (m_straightBrakePressure - connectedPr_B) * m_straightBrakePressure) * DeltaTime;
                //pressure_delta_B = -1.5f * Mathf.Clamp(Mathf.Sqrt(2 * pressure_delta_B * m_straightBrakePressure),-pressure_delta_B, pressure_delta_B);
                //Debug.Log("pressure_delta_B " + pressure_delta_B);
            }
            pressure_delta_F = Mathf.Clamp(pressure_delta_F, -maxOverShoot - Mathf.Abs(connectedPr_F - m_straightBrakePressure) * DeltaTime, maxOverShoot);
            //Debug.Log(pressure_delta_F + " , clamp to " + (-maxOverShoot - Mathf.Abs(connectedPr_F - m_straightBrakePressure) * DeltaTime));
            pressure_delta_B = Mathf.Clamp(pressure_delta_B, -maxOverShoot - Mathf.Abs(connectedPr_B - m_straightBrakePressure) * DeltaTime, maxOverShoot);
            //Debug.Log(pressure_delta_B + " , clamp to " + (-maxOverShoot - Mathf.Abs(connectedPr_B - m_straightBrakePressure) * DeltaTime));
            //低FPS時はオーバーシュートを減らして振動幅を引き下げる、こうしないとブレーキ弁が振動に反応してしまう
        }

        //math_sqrt_2_q_Q_div_mの結果に係数を掛けて用いる
        //係数は10³*S/(L*√m)
        //S:断面積
        //L:体積
        //密度定数 m = 11.5075252899[kg/m³*MPa]
        protected static float math_sqrt_2_q_Q_div_m(float to, float from)
        {
            return Mathf.Sqrt(2 * Mathf.Abs(from - to) * Mathf.Max(to, from)) * (from > to ? 1 : -1);
        }

        public virtual float[] getStraightPressurePointer(string name)
        {
            if(name == "BP")
            {
                return straightBrakePressure;
            }
            return null;
        }
        public virtual bool setConnectedPressurePointer(string name, float[] newPointer, bool F_B)
        {
            if (name == "BP")
            {
                if (F_B)
                {
                    ConnectedBrakePressure_F = newPointer;
                }
                else
                {
                    ConnectedBrakePressure_B = newPointer;
                }
                if(UseLegacyPipeState)changeLegacyState(F_B);
                return true;
            }
            return false;
        }

        private void changeLegacyState(bool F_B)
        {
            if (F_B)
            {
                BrakeOpenF = ConnectedBrakePressure_F != null ? (ConnectedBrakePressure_F == straightBrakePressure ? false : true) : true;
            }
            else
            {
                BrakeOpenB = ConnectedBrakePressure_B != null ? (ConnectedBrakePressure_B == straightBrakePressure ? false : true) : true;
            }
        }
        protected bool isOwnerState;

        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
            isOwnerState = player == Networking.LocalPlayer;
        }

        [PublicAPI]
        public virtual string ConnectionDebug(bool F_B)
        {
            string val = $"Brake isOwnerState:{isOwnerState} , acctualOwner:{Networking.IsOwner(this.gameObject)}, trainOwner:{Networking.IsOwner(train.gameObject)}" +
                $"<br>BP: ";
            if (F_B)
            {
                val += ConnectedBrakePressure_F != null ? (ConnectedBrakePressure_F == straightBrakePressure ? "Closed" : "Connected") : "Fail";
            }
            else
            {
                val += ConnectedBrakePressure_B != null ? (ConnectedBrakePressure_B == straightBrakePressure ? "Closed" : "Connected") : "Fail";
            }
            val += $"Pressure: {straightBrakePressure[0]}";
            return val;
        }
    }
}