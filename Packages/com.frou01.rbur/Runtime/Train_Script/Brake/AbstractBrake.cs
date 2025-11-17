using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;


namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class AbstractBrake : TrainConnectionReciever
    {
        //ブレーキ系の標準
        //前後接続、圧力配送
        //手ブレーキ受け入れは拡張クラスでやるのが良さそう
        [SerializeField] protected Train train;
        [SerializeField] public float[] straightBrakePressure = new float[1];
        [UdonSynced(UdonSyncMode.Linear)] protected float m_straightBrakePressure;//4byte,[MPa]
        protected float DeltaTime;

        protected float pressure_delta_F;//流量の単位は[kg/s]
        protected float pressure_delta_B;

        protected float[] ConnectedBrakePressure_F;
        protected float[] ConnectedBrakePressure_B;
        protected float connectedPr_F;
        protected float connectedPr_B;

        [UdonSynced] public bool BrakeOpenF;//1byte
        [UdonSynced] public bool BrakeOpenB;//1byte

        [SerializeField] protected Animator indicateAnimator;
        protected bool hasAnimator;
        protected int brakePressureParamaterID;

        [HideInInspector] public float[] brakeFactor = new float[1];

        protected virtual void Start()
        {
            brakePressureParamaterID = Animator.StringToHash("BrakePressure");
            hasAnimator = indicateAnimator != null;
            isOwnerState = Networking.IsOwner(gameObject);
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
            if (isOwnerState)
            {
                m_straightBrakePressure = straightBrakePressure[0];
                DeltaTime = Time.deltaTime;
                m_straightBrakePressure += pressure_delta_F * DeltaTime;
                if (ConnectedBrakePressure_F != null) ConnectedBrakePressure_F[0] -= pressure_delta_F * DeltaTime;
                m_straightBrakePressure += pressure_delta_B * DeltaTime;
                if (ConnectedBrakePressure_B != null) ConnectedBrakePressure_B[0] -= pressure_delta_B * DeltaTime;

            }
            straightBrakePressure[0] = m_straightBrakePressure;
        }

        //音速/60=5.7mほど。波を送るには早いくらいか
        protected virtual void LateUpdate()
        {
            if (isOwnerState)
            {
                m_straightBrakePressure = straightBrakePressure[0];//LateUpdateではm_straightBrakePressureは参照のみ

                pressure_delta_F = 0;
                pressure_delta_B = 0;
                //低い方へ流す（高圧からは受け入れだけする）
                if (BrakeOpenF)
                {
                    connectedPr_F = ConnectedBrakePressure_F == null ? 0f : ConnectedBrakePressure_F[0];
                    if (connectedPr_F < m_straightBrakePressure)
                    {
                        pressure_delta_F = m_straightBrakePressure - connectedPr_F;
                        pressure_delta_F = -1.5f * Mathf.Sqrt(2 * pressure_delta_F * m_straightBrakePressure);
                        //Debug.Log("pressure_delta_F " + pressure_delta_F);
                    }
                }
                if (BrakeOpenB)
                {
                    connectedPr_B = ConnectedBrakePressure_B == null ? 0f : ConnectedBrakePressure_B[0];
                    if (connectedPr_B < m_straightBrakePressure)
                    {
                        pressure_delta_B = m_straightBrakePressure - connectedPr_B;
                        pressure_delta_B = -1.5f * Mathf.Sqrt(2 * pressure_delta_B * m_straightBrakePressure);
                        //Debug.Log("pressure_delta_B " + pressure_delta_B);
                    }
                }
            }

            if (hasAnimator)
            {
                indicateAnimator.SetFloat(brakePressureParamaterID, m_straightBrakePressure);
            }

            //ブレーキ圧はLateUpdateでもUpdateでも継承先では一貫した結果になるので、
            //どちらで処理してもよい
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

        public override void TrainConnectionUpdate(Train connectedTrain, bool F_B)
        {
            if (connectedTrain != null)
            {
                TrainConnectionReciever foundModule = connectedTrain.GetConnectionRecieverByTag("Brake");
                if (foundModule)
                {
                    if (F_B)
                    {
                        if (connectedTrain != null) ConnectedBrakePressure_F = ((AbstractBrake)foundModule).straightBrakePressure;
                        else ConnectedBrakePressure_F = null;

                    }
                    else
                    {
                        if (connectedTrain != null) ConnectedBrakePressure_B = ((AbstractBrake)foundModule).straightBrakePressure;
                        else ConnectedBrakePressure_B = null;
                    }
                }
            }
        }
        protected bool isOwnerState;
        [NetworkCallable]
        public void changeBrakeValve(bool F_B)//空制弁開放/閉鎖
        {
            if (F_B)
            {
                BrakeOpenF = !BrakeOpenF;
            }
            else
            {
                BrakeOpenB = !BrakeOpenB;
            }

            RequestSerialization();
        }
        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
            isOwnerState = player == Networking.LocalPlayer;
        }
    }
}