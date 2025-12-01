using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace frou01.RigidBodyTrain
{

    public class AV_Brake : AbstractBrake
    {
        //参考
        //https://dl.ndl.go.jp/pid/1228874/1/53
        //https://dl.ndl.go.jp/pid/1036111/1/14
        [SerializeField] protected float SupportTankSize = 0.050f;//[m³]
        [SerializeField] protected float AdditionalTankSize = 0.140f;//[m³]
        [SerializeField] protected float EmerTankSize = 0.0035f;//[m³]
        [SerializeField] protected float CylinderSize = 0.02191849924f;//[m³] 行程による体積変化は無視 305mmシリンダ

        [SerializeField] protected float release_constriction = 8.0f;//mm²
        [SerializeField] protected float refill_constriction = 5.0f;
        [SerializeField] protected float brake_constriction = 8.04f;

        protected float cylinder_release_coefficient;
        protected float additional_refill_coefficient;
        protected float support_refill_coefficient;
        protected float cylinder_brake_coefficient;
        protected float support_brake_coefficient;

        [SerializeField][UdonSynced] protected float SupportPressure;
        [SerializeField][UdonSynced] protected float AdditionalPressure;
        [SerializeField][UdonSynced] protected float EmerPressure;
        [SerializeField][UdonSynced] protected float CylinderPressure;

        [SerializeField][UdonSynced] protected int piston_Position;//0: refill,
                                                                   //1: release
                                                                   //2: imm_lap,
                                                                   //3: imm_brake,
                                                                   //4: brake_lap
                                                                   //5: brake,
                                                                   //6: emer,
        protected float piston_Position_float;
        protected float Emer_piston_Position_Float;
        [SerializeField] protected float refill_sensitivity = 0.01f;
        [SerializeField] protected float release_sensitivity = 0.005f;
        [SerializeField] protected float releaseLap_sensitivity = 0.005f;
        [SerializeField] protected float immiBrakeLap_sensitivity = 0.01f;
        [SerializeField] protected float immiBrake_sensitivity = 0.02f;
        [SerializeField] protected float brake_sensitivity = 0.025f;
        [SerializeField] protected float emer_refill_sensitivity = 0.005f;
        [SerializeField] protected float emer_release_sensitivity = 0.005f;
        [SerializeField] protected float emer_sensitivity = 0.010f;
        [SerializeField] protected float emer_sensitivity_2 = 0.1f;

        [SerializeField] protected float StaticFriction = 1020f;
        [SerializeField] protected float DynamicFriction = 61f;
        [SerializeField] protected float DynamicFrictionSpeed = 0.5f;
        [HideInInspector][SerializeField] public float[] ExposedDeltaTime = new float[1];

        protected float A_supportDelta;
        protected float A_additionalDelta;
        protected float A_emerDelta;
        protected float A_cylinderDelta;
        protected float A_straightDelta;

        protected float temp_pressureDiff;
        protected float temp_cof;
        protected float temp_cof2;
        protected float temp_straightBrakePressure;

        [SerializeField] protected MortorAndWheel[] controlledWheel;
        [HideInInspector][SerializeField] protected float[][] wheelBrakes = new float[0][];
        [HideInInspector][SerializeField] protected float[][] wheelTreadSpeeds = new float[0][];
        [SerializeField] protected float[] wheelMultiplier = new float[0];

        protected override void Start()
        {
            base.Start();
            wheelBrakes = new float[controlledWheel.Length][];
            wheelTreadSpeeds = new float[controlledWheel.Length][];
            for (int index = 0; index < wheelBrakes.Length; index++)
            {
                wheelBrakes[index] = controlledWheel[index].BrakeForce;
                wheelTreadSpeeds[index] = controlledWheel[index].WheelTreadSpeed;
            }

            cylinder_release_coefficient    = release_constriction  * 0.00029478747f / CylinderSize;
            additional_refill_coefficient   = refill_constriction   * 0.00029478747f / AdditionalTankSize;
            support_refill_coefficient      = refill_constriction   * 0.00029478747f / SupportTankSize;
            cylinder_brake_coefficient      = brake_constriction    * 0.00029478747f / CylinderSize;
            support_brake_coefficient       = brake_constriction    * 0.00029478747f / SupportTankSize;
        }

        protected override void Update()
        {
            base.Update();
            if (isOwnerState)
            {
                SupportPressure += A_supportDelta * DeltaTime;
                AdditionalPressure += A_additionalDelta * DeltaTime;
                EmerPressure += A_emerDelta * DeltaTime;
                CylinderPressure += A_cylinderDelta * DeltaTime;
                straightBrakePressure[0] += A_straightDelta * DeltaTime;

                SupportPressure = Mathf.Max(SupportPressure, 0.1f);
                AdditionalPressure = Mathf.Max(AdditionalPressure, 0.1f);
                EmerPressure = Mathf.Max(EmerPressure, 0.1f);
                CylinderPressure = Mathf.Max(CylinderPressure, 0.1f);
            }
            ExposedDeltaTime[0] = DeltaTime;
            ApplyForceToWheel();
        }

        protected virtual void ApplyForceToWheel()
        {
            brakeFactor[0] = 0;
            if (isOwnerState)
                for (int index = 0; index < wheelBrakes.Length; index++)
                {
                    brakeFactor[0] += wheelBrakes[index][0] = (CylinderPressure - 0.1f);
                    wheelBrakes[index][0] *= wheelMultiplier[index];
                    wheelBrakes[index][0] += Mathf.Lerp(StaticFriction, DynamicFriction, Mathf.Abs(wheelTreadSpeeds[index][0]) / DynamicFrictionSpeed);
                }
            else
                for (int index = 0; index < wheelBrakes.Length; index++)
                {
                    brakeFactor[0] += (CylinderPressure - 0.1f);
                }
        }
        //変化の係数は10³*S/(L*√m)
        //S:断面積[m²]
        //L:体積[m³]
        //密度定数 m = 11.5075252899[kg/m³*MPa]
        //S/10³/√11.5075252899/L
        //S*294.787477595/L
        //s[mm²]の場合は
        //s*0.00029478747/L
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (isOwnerState)
            {
                A_supportDelta = 0;
                A_additionalDelta = 0;
                A_emerDelta = 0;
                A_cylinderDelta = 0;
                A_straightDelta = 0;

                temp_straightBrakePressure = straightBrakePressure[0];

                if (SupportPressure - temp_straightBrakePressure < -refill_sensitivity)
                {
                    piston_Position_float = 0;//込め
                } else if (piston_Position != 0 && SupportPressure - temp_straightBrakePressure < -release_sensitivity)
                {
                    if (piston_Position_float > 1) piston_Position_float = 1;//弛め
                    else piston_Position_float += 0.2f;
                }
                else
                if (SupportPressure - temp_straightBrakePressure > brake_sensitivity)
                {
                    piston_Position_float += 0.9f;
                    if (piston_Position_float > 5) piston_Position_float = 5;//全制動
                }
                else
                if (piston_Position <= 3 && SupportPressure - temp_straightBrakePressure > immiBrake_sensitivity)
                {
                    piston_Position_float += 0.6f;
                    if (piston_Position_float > 3) piston_Position_float = 3;//急制動
                }
                else if (piston_Position == 5)
                {
                    piston_Position_float = 4;//全制動重なり
                } else if ((piston_Position >= 2 && SupportPressure - temp_straightBrakePressure < immiBrakeLap_sensitivity) || SupportPressure - temp_straightBrakePressure > releaseLap_sensitivity)
                {
                    if (piston_Position_float > 2) piston_Position_float = 2;//急制動重なり or 弛め重なり
                    else piston_Position_float += 0.4f;
                    //※本来は急制動重なりと弛め重なりは滑り弁の位置が異なる。
                }
                //非常部は独立給排気
                if (piston_Position != 6)
                {
                    if (EmerPressure - temp_straightBrakePressure > emer_release_sensitivity)
                    {
                        //急動空気溜め -> 大気        ┌1.37mm²┐>（閉塞条件：急動空気溜 < 列車管 or 急動空気溜めが列車管より十分高圧）
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(0.1f, EmerPressure);
                        A_emerDelta -= 0.00040385883f / EmerTankSize * temp_pressureDiff;
                    }
                    else if (EmerPressure - temp_straightBrakePressure < -emer_refill_sensitivity)
                    {
                        //列車管  -> 急動空気溜       64mm²
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(EmerPressure, temp_straightBrakePressure);
                        A_emerDelta += 0.01886639808f / EmerTankSize * temp_pressureDiff;
                        A_straightDelta -= 0.01886639808f / 0.02f * temp_pressureDiff;
                    }
                }
                piston_Position = Mathf.FloorToInt(piston_Position_float);
                if (EmerPressure - temp_straightBrakePressure > emer_sensitivity)
                {
                    Emer_piston_Position_Float += 0.4f;//deltatimeではなくフレーム単位
                }
                else
                {
                    Emer_piston_Position_Float = 0;
                }

                if (Emer_piston_Position_Float > 2)
                {
                    Emer_piston_Position_Float = 2;
                    piston_Position = 6;//非常
                }
                else if(Emer_piston_Position_Float < 0)
                {
                    Emer_piston_Position_Float = 0;
                }

                switch (piston_Position)
                {
                    case 0:
                        //弛め
                        //シリンダ -> 大気        8.0mm²   ┐
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(0.1f, CylinderPressure);
                        A_cylinderDelta -= cylinder_release_coefficient * temp_pressureDiff;
                        //補助空気溜 <-> 付加空気溜   5.0mm²   -
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(AdditionalPressure, SupportPressure);
                        A_additionalDelta += additional_refill_coefficient * temp_pressureDiff;
                        A_supportDelta -= support_refill_coefficient * temp_pressureDiff;

                        //込め
                        //列車管 <-> 補助空気溜       1.97mm²  ┐
                        //列車管  -> 付加空気溜       24.34mm² ＼
                        //列車管の容積は0.02m³
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(SupportPressure , temp_straightBrakePressure);
                        A_supportDelta += 0.00058073131f / SupportTankSize * temp_pressureDiff;
                        A_straightDelta -= 0.00058073131f / 0.02f * temp_pressureDiff;

                        if(AdditionalPressure < temp_straightBrakePressure)
                        {
                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(AdditionalPressure, temp_straightBrakePressure);
                            A_additionalDelta += 0.00717512701f / AdditionalTankSize * temp_pressureDiff;
                            A_straightDelta -= 0.00717512701f / 0.02f * temp_pressureDiff;
                        }
                        break;
                    case 1:
                        //弛め
                        //シリンダ -> 大気        8.0mm²   ┐
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(0.1f, CylinderPressure);
                        A_cylinderDelta -= 0.00235829976f / CylinderSize * temp_pressureDiff;

                        //補助空気溜 <-> 付加空気溜   5.0mm²   ┐
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(AdditionalPressure, SupportPressure);
                        A_additionalDelta += 0.00147393735f / AdditionalTankSize * temp_pressureDiff;
                        A_supportDelta -= 0.00147393735f / SupportTankSize * temp_pressureDiff;
                        break;
                    case 2:
                    case 4:
                        //重なり
                        //連絡無し
                        break;
                    case 3:
                        //急制動
                        //列車管  -> 制動筒           ／7.1mm²＼
                        //補助空気溜め -> 制動筒        /8.04mm²-
                        //ずらすのは判定面倒だし再現した所でQ現象の元なのでやめとこう
                        temp_cof = SupportPressure - temp_straightBrakePressure;
                        temp_cof2 = Mathf.Clamp01(Mathf.Min(temp_cof - immiBrake_sensitivity, brake_sensitivity - temp_cof) / (brake_sensitivity - immiBrake_sensitivity));

                        if(temp_straightBrakePressure > CylinderPressure)
                        {
                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, temp_straightBrakePressure);
                            A_cylinderDelta += temp_cof2 * 0.00209299103f / CylinderSize * temp_pressureDiff;
                            A_straightDelta -= temp_cof2 * 0.00209299103f / 0.02f * temp_pressureDiff;
                        }

                        temp_cof2 = Mathf.Clamp01((temp_cof - immiBrake_sensitivity) / (brake_sensitivity - immiBrake_sensitivity) * 3);
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, SupportPressure);
                        A_cylinderDelta += temp_cof2 * cylinder_brake_coefficient * temp_pressureDiff;
                        A_supportDelta -= temp_cof2 * support_brake_coefficient * temp_pressureDiff;

                        break;
                    case 5:
                        //全制動
                        //補助空気溜め -> 制動筒        -8.04mm²-
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, SupportPressure);
                        A_cylinderDelta += cylinder_brake_coefficient * temp_pressureDiff;
                        A_supportDelta -= support_brake_coefficient * temp_pressureDiff;

                        break;
                    case 6:
                        //非常制動
                        //列車管  -> 大気            7.1mm² > (閉塞条件：急動空気溜め < 1.5[MPa]（仮）)
                        //急動空気溜め -> 大気       ?mm² > (8-12秒で放出)
                        //付加空気溜め -> 制動筒     ／3.14mm² > (解放条件：制動管圧 < 4[MPa]（仮）)
                        if(EmerPressure > 0.15f)
                        {
                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(0.1f, temp_straightBrakePressure);
                            A_straightDelta -= 0.00209299103f / 0.02f * temp_pressureDiff;
                        }
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(0.1f, EmerPressure);
                        A_emerDelta -= 0.0015f / 0.02f * temp_pressureDiff;

                        if(CylinderPressure < AdditionalPressure)
                        {
                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, AdditionalPressure);
                            A_cylinderDelta += 0.00092563265f / CylinderSize * temp_pressureDiff;
                            A_additionalDelta -= 0.00092563265f / AdditionalTankSize * temp_pressureDiff;
                        }
                        break;
                }
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            base.OnOwnershipTransferred(player);
            for (int index = 0; index < wheelBrakes.Length; index++)
            {
                wheelBrakes[index][0] = 0;
            }
        }

        public override void OnDeserialization()
        {
            piston_Position_float = piston_Position;
            Emer_piston_Position_Float = 1;
        }
    }
}