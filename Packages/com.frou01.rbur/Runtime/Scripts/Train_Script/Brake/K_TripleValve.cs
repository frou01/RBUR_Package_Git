using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace frou01.RigidBodyTrain
{
    public class K_TripleValve : AbstractBrake
    {
        //寸法等　https://dl.ndl.go.jp/pid/1036111/1/36
        [SerializeField] protected float SupportTankSize = 0.026f;//[m³]
        [SerializeField] protected float CylinderSize = 0.00970964187f;//[m³] 行程による体積変化は無視 203mmシリンダ

        [SerializeField][UdonSynced] protected float SupportPressure;
        [SerializeField][UdonSynced] protected float CylinderPressure;

        [SerializeField][UdonSynced] protected int piston_Position;//0: slowRelease,
                                                                   //1: release,
                                                                   //2: imm_brake_lap,
                                                                   //3: imm_brake,
                                                                   //4: brake_lap,
                                                                   //5: brake,
                                                                   //6: emer,
        protected float piston_Position_float;
        [SerializeField] protected float slowRefill_sensitivity = 0.1f;
        [SerializeField] protected float slowRelease_sensitivity = 0.06f;
        [SerializeField] protected float release_sensitivity = 0.005f;
        [SerializeField] protected float immidiateBrake_lap_sensitivity = 0.00f;
        [SerializeField] protected float immidiateBrake_sensitivity = 0.015f;
        [SerializeField] protected float brake_sensitivity = 0.02f;
        [SerializeField] protected float emer_sensitivity = 0.025f;
        [SerializeField] protected float emer_sensitivity_2 = 0.1f;

        [SerializeField] protected float StaticFriction = 1020f;
        [SerializeField] protected float DynamicFriction = 61f;
        [SerializeField] protected float DynamicFrictionSpeed = 0.5f;

        protected float k_supportDelta;
        protected float k_cylinderDelta;
        protected float k_straightDelta;

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
        }

        protected override void Update()
        {
            base.Update();
            if (isOwnerState)
            {
                SupportPressure += k_supportDelta * DeltaTime;
                CylinderPressure += k_cylinderDelta * DeltaTime;
                straightBrakePressure[0] += k_straightDelta * DeltaTime;

                SupportPressure = Mathf.Max(SupportPressure, 0.1f);
                CylinderPressure = Mathf.Max(CylinderPressure, 0.1f);
            }
            ApplyForceToWheel();
        }
        protected virtual void ApplyForceToWheel()
        {
            brakeFactor[0] = 0;
            if (isOwnerState)
                for (int index = 0; index < wheelBrakes.Length; index++)
                {
                    brakeFactor[0] += wheelBrakes[index][0] = (CylinderPressure - 0.11f);
                    wheelBrakes[index][0] *= wheelMultiplier[index];
                    wheelBrakes[index][0] += Mathf.Lerp(StaticFriction, DynamicFriction, Mathf.Abs(wheelTreadSpeeds[index][0]) / DynamicFrictionSpeed);
                }
            else
                for (int index = 0; index < wheelBrakes.Length; index++)
                {
                    brakeFactor[0] += (CylinderPressure - 0.11f);
                }
        }
        //変化の係数は10³*S/(L*√m)
        //S:断面積
        //L:体積
        //密度定数 m = 11.5075252899[kg/m³*MPa]
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (isOwnerState)
            {
                k_supportDelta = 0;
                k_cylinderDelta = 0;
                k_straightDelta = 0;

                temp_straightBrakePressure = straightBrakePressure[0];
                if (SupportPressure < temp_straightBrakePressure - slowRelease_sensitivity)
                {
                    piston_Position_float = 0;//減速弛め
                }
                else
                if (SupportPressure < temp_straightBrakePressure - release_sensitivity)
                {
                    piston_Position_float = 1;//弛め
                }
                else
                if (SupportPressure > temp_straightBrakePressure + emer_sensitivity)
                {
                    piston_Position_float = 6;//非常
                }
                else
                if (SupportPressure > temp_straightBrakePressure + brake_sensitivity)
                {
                    piston_Position_float += 0.02f;//全制動
                    if (piston_Position_float > 5) piston_Position_float = 5;
                }
                else
                if (piston_Position <= 3 && SupportPressure > temp_straightBrakePressure + immidiateBrake_sensitivity)
                {
                    piston_Position_float += 0.03f;//急制動
                    if (piston_Position_float > 3) piston_Position_float = 3;
                }
                else if (piston_Position == 3 && SupportPressure < temp_straightBrakePressure + immidiateBrake_lap_sensitivity)
                {
                    piston_Position_float = 2;//急制動重なり
                }
                else if (piston_Position == 5)
                {
                    piston_Position_float = 4;//全制動重なり
                }
                piston_Position = Mathf.FloorToInt(piston_Position_float);
                switch (piston_Position)
                {
                    case 0://減速弛め
                           //シリンダ -> 大気
                           //断面積は1.77mm²
                           //10³*1.77/10⁶/(0.0609*√11.5075252899)
                           // = 1000*1.77/1000000/(√11.5075252899)/CylinderSize
                           // = 0.00052177383/CylinderSize
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(0.1f, CylinderPressure);
                        k_cylinderDelta -= 0.0015f / CylinderSize * temp_pressureDiff;

                        temp_cof = temp_straightBrakePressure - SupportPressure;
                        if (temp_cof > slowRefill_sensitivity)
                        {
                            //減速込め
                            //列車管 <-> 補助空気溜
                            //列車管の容積は0.02
                            //断面積は1.08mm²
                            //列車管側係数 = 10³*1.08/10⁶/√11.5075252899/0.02 = 0.01591852379
                            //補助空気溜側係数 = 0.00031837047/SupportAirTankSize = 0.02034611607
                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(SupportPressure, straightBrakePressure[0]);
                            k_supportDelta += 0.00031837047f / SupportTankSize * temp_pressureDiff;
                            k_straightDelta -= 0.01591852379f * temp_pressureDiff;
                        }
                        else if (temp_cof > 0)
                        {
                            //全込め
                            //列車管 <-> 補助空気溜
                            //列車管の容積は0.02
                            //断面積は1.76mm²
                            //列車管側係数 = 0.00051882596/0.02 = 0.025941298
                            //補助空気溜側係数 = 0.00051882596/SupportAirTankSize = 0.02034611607
                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(SupportPressure, straightBrakePressure[0]);
                            k_supportDelta += 0.00051882596f / SupportTankSize * temp_pressureDiff;
                            k_straightDelta -= 0.025941298f * temp_pressureDiff;
                        }
                        break;
                    case 1://全弛め及び全込め
                           //全弛め
                           //シリンダ -> 大気
                           //断面積は9.62mm²
                           //10³*9.62/10⁶/(0.0609*√11.5075252899)
                           // = 10³*9.62/10⁶/√11.5075252899/CylinderSize
                           // = 0.00283585553/CylinderSize
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(0.1f, CylinderPressure);
                        k_cylinderDelta -= 0.0015f / CylinderSize * temp_pressureDiff;
                        temp_cof = temp_straightBrakePressure - SupportPressure;

                        //全込め
                        //列車管 <-> 補助空気溜
                        //列車管の容積は0.02
                        //断面積は1.76mm²
                        //列車管側係数 = 0.00051882596/0.02 = 0.025941298
                        //補助空気溜側係数 = 0.00051882596/SupportAirTankSize = 0.02034611607
                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(SupportPressure, straightBrakePressure[0]);
                        k_straightDelta -= 0.025941298f * temp_pressureDiff;
                        k_supportDelta += 0.00051882596f / SupportTankSize * temp_pressureDiff;
                        break;
                    case 2:
                        //急制動重なり
                        //連絡無し
                        break;
                    case 3:
                        //急制動
                        //急ブレーキ作用
                        //列車管->制動筒
                        //最大3.08mm²
                        //列車管側係数 = 0.04539727154 = 10³*3.08/10⁶/√11.5075252899/0.02
                        //制動筒側係数 = 0.00090794543/CylinderSize = 10³*3.08/10⁶/√11.5075252899/CylinderSize
                        if (CylinderPressure < straightBrakePressure[0])
                        {
                            temp_cof = SupportPressure - temp_straightBrakePressure;
                            temp_cof2 = Mathf.Clamp01(Mathf.Min(temp_cof - immidiateBrake_lap_sensitivity, brake_sensitivity - temp_cof) / (brake_sensitivity - immidiateBrake_lap_sensitivity)) * 2;
                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, straightBrakePressure[0]);
                            k_straightDelta -= temp_cof2 * 0.04539727154f * temp_pressureDiff;
                            k_cylinderDelta += temp_cof2 * 0.00090794543f / CylinderSize * temp_pressureDiff;
                        }


                        //補助空気溜->制動筒
                        //9.62mm²
                        //補助空気溜側係数 = 0.00283585553/SupportAirTankSize = 10³*9.62/10⁶/√11.5075252899/SupportAirTankSize
                        //制動筒側係数 = 0.00283585553/CylinderSize = 10³*9.62/10⁶/√11.5075252899/CylinderSize
                        temp_cof2 = Mathf.Clamp01((brake_sensitivity - temp_cof) / (brake_sensitivity - immidiateBrake_sensitivity));

                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, SupportPressure);
                        k_supportDelta -= temp_cof2 * 0.00283585553f / SupportTankSize * temp_pressureDiff;
                        k_cylinderDelta += temp_cof2 * 0.00283585553f / CylinderSize * temp_pressureDiff;
                        break;
                    case 5:
                        //全制動


                        //補助空気溜->制動筒
                        //9.62mm²
                        //補助空気溜側係数 = 0.00283585553/SupportAirTankSize = 10³*9.62/10⁶/√11.5075252899/SupportAirTankSize
                        //制動筒側係数 = 0.00283585553/CylinderSize = 10³*9.62/10⁶/√11.5075252899/CylinderSize

                        temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, SupportPressure);
                        k_supportDelta -= 0.00283585553f / SupportTankSize * temp_pressureDiff;
                        k_cylinderDelta += 0.00283585553f / CylinderSize * temp_pressureDiff;
                        break;
                    case 6:
                        //非常制動
                        //局部減圧
                        //列車管->制動筒
                        //最大48mm²
                        //列車管側係数 = 0.04539727154 = 10³*48/10⁶/√11.5075252899/0.02
                        //制動筒側係数 = 0.00090794543/CylinderSize = 10³*3.08/10⁶/√11.5075252899/CylinderSize
                        temp_cof = SupportPressure - temp_straightBrakePressure;
                        if (CylinderPressure < straightBrakePressure[0])
                        {
                            temp_cof2 = 1 - Mathf.Clamp01((emer_sensitivity_2 - temp_cof) / (emer_sensitivity_2 - emer_sensitivity));

                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, straightBrakePressure[0]);
                            k_straightDelta -= temp_cof2 * 0.70748994622f * temp_pressureDiff;
                            k_cylinderDelta += temp_cof2 * 0.01414979892f / CylinderSize * temp_pressureDiff;
                        }

                        if (temp_cof2 >= 0.75)
                        {
                            //補助空気溜->制動筒
                            //9.62mm²
                            //補助空気溜側係数 = 0.00283585553/SupportAirTankSize = 10³*9.62/10⁶/√11.5075252899/SupportAirTankSize
                            //制動筒側係数 = 0.00283585553/CylinderSize = 10³*9.62/10⁶/√11.5075252899/CylinderSize

                            temp_pressureDiff = math_sqrt_2_q_Q_div_m(CylinderPressure, SupportPressure);
                            k_supportDelta -= 0.00283585553f / SupportTankSize * temp_pressureDiff;
                            k_cylinderDelta += 0.00283585553f / CylinderSize * temp_pressureDiff;
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
            base.OnDeserialization();
            piston_Position_float = piston_Position;
        }
    }
}
