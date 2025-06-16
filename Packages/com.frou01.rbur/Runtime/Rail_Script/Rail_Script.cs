using Cinemachine;
using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    public class Rail_Script : UdonSharpBehaviour
    {

        public CinemachinePathBase cinemachinePath;

        //[System.NonSerialized]public Vector3 A;
        //[System.NonSerialized]public Vector3 B;
        //[System.NonSerialized]public Vector3 C;
        //[System.NonSerialized]public Vector3 D;
        //public Transform startS;
        //public Transform startE;
        //public Transform endS;
        //public Transform endE;

        //public bool TCurve_FStraight;

        public bool moveableRail = false;//転車台等動くレールであるか

        //private int precision = 25;

        public Rail_Script next;
        public Rail_Script prev;

        //public float length;

        [System.NonSerialized] public int RailID;

        //public void Start()
        //{
            //if (TCurve_FStraight)
            //{
            //    Vector3 startVectorS;
            //    Vector3 startVectorE;
            //    Vector3 endVectorS;
            //    Vector3 endVectorE;
            //
            //    startVectorS = startS.position;
            //    startVectorE = startE.position;
            //    endVectorS = endS.position;
            //    endVectorE = endE.position;
            //    A = -startVectorS + 3 * startVectorE - 3 * endVectorS + endVectorE;
            //    B = 3 * (startVectorS - 2 * startVectorE + endVectorS);
            //    C = 3 * (-startVectorS + startVectorE);
            //    D = startVectorS;
            //    cachedPoints = new Vector3[precision + 1];
            //    cachedPoints[0] = startVectorS;
            //
            //
            //    //double decLength = 0;
            //    //for (int a = 0;a < precision-1;a++)
            //    //{
            //    //    int b = a + 1;
            //    //
            //    //    float tA = (float)a / precision;
            //    //    float tB = (float)b / precision;
            //    //
            //    //    float fA = GetVector(tA).magnitude;
            //    //    float fB = GetVector(tB).magnitude;
            //    //
            //    //    decLength += fA + (4 * GetVector((a + b) / (2 * precision)).magnitude) + fB;
            //    //
            //    //
            //    //
            //    //}
            //    //
            //    //length = (float)(decLength / (6 * precision));
            //
            //    //Debug.Log(0 + " , " + cachedPoints[0]);
            //    for (int i = 1; i < precision; i++)
            //    {
            //        //Debug.Log(GetPoint(i / 20f));
            //        cachedPoints[i] = GetPoint(i / (float)precision);
            //        //Debug.Log(i + " , " + cachedPoints[i]);
            //    }
            //    cachedPoints[precision] = endVectorE;
            //    //Debug.Log(20 + " , " + cachedPoints[20]);
            //}
            //else
            //{
            //    //length = (startS.position - endE.position).magnitude;
            //}

        //}

        public Vector3 GetStartPoint()
        {
            return cinemachinePath.EvaluatePosition(0);
        }

        public Vector3 GetEndPoint()
        {
            return cinemachinePath.EvaluatePosition(cinemachinePath.MaxPos);
        }
        private Vector3 GetStartTangent()
        {
            return cinemachinePath.EvaluateTangent(0).normalized;
        }

        private Vector3 GetEndTangent()
        {
            return cinemachinePath.EvaluateTangent(cinemachinePath.MaxPos).normalized;
        }

        public Vector3 GetPosition(float t)
        {
            return cinemachinePath.EvaluatePosition(t);

        }



        public float GetF(Vector3 Point)
        {
            return cinemachinePath.FindClosestPoint(Point, 0, -1, 10);

        }
        void OnDrawGizmos()
        {
            if (cinemachinePath)
            {
                Gizmos.color = new Color(1f, 0, 0f, 0.1f);
                if (next != null)
                {
                    CinemachinePathBase nextPath = next.cinemachinePath;
                    Vector3 nextClosestPoint = nextPath.EvaluatePosition(nextPath.FindClosestPoint(GetEndPoint() + GetEndTangent(), 0, -1, 2)) + new Vector3(0, 1, 0);
                    float edgeDist = Vector3.Distance(GetEndPoint(), nextPath.EvaluatePosition(nextPath.FindClosestPoint(GetEndPoint(), 0, -1, 2)));
                    Gizmos.color = new Color(1f, 0, 0f, 0.3f + edgeDist);
                    Gizmos.DrawLine(GetEndPoint(), nextClosestPoint);
                    Gizmos.DrawSphere(GetEndPoint(), edgeDist);
                }
                else
                {
                    Gizmos.color = new Color(1f, 0, 0f, 1f);
                    Gizmos.DrawLine(GetEndPoint(), GetEndPoint() - GetEndTangent());
                    Gizmos.DrawSphere(GetEndPoint() - GetEndTangent(), 0.3f);
                }
                Gizmos.color = new Color(0f, 0, 1f, 0.1f);
                if (prev != null)
                {
                    CinemachinePathBase prevPath = prev.cinemachinePath;
                    Vector3 prevClosestPoint = prevPath.EvaluatePosition(prevPath.FindClosestPoint(GetStartPoint() - GetStartTangent(), 0, -1, 2)) + new Vector3(0, 1, 0);
                    float edgeDist = Vector3.Distance(GetStartPoint(), prevPath.EvaluatePosition(prevPath.FindClosestPoint(GetStartPoint(), 0, -1, 2)));
                    Gizmos.color = new Color(0, 0, 1f, 0.3f + edgeDist);
                    Gizmos.DrawLine(GetStartPoint(), prevClosestPoint);
                    Gizmos.DrawSphere(GetStartPoint(), edgeDist);
                }
                else
                {
                    Gizmos.color = new Color(0f, 0, 1f, 1f);
                    Gizmos.DrawLine(GetStartPoint(), GetStartPoint() + GetStartTangent());
                    Gizmos.DrawSphere(GetStartPoint() + GetStartTangent(), 0.3f);
                }
            }
        }
    }
}
