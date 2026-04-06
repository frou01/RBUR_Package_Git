
using Cinemachine;
using System.IO;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using static Cinemachine.CinemachinePathBase;

namespace frou01.RigidBodyTrain
{
    public class PointLever_Setter : UdonSharpBehaviour
    {
        public Rail_Script from1;
        public Rail_Script from2;
        public bool changeType1;//true:next  false:prev
        public bool changeType2;//true:next  false:prev
        public Rail_Script to1;
        public Rail_Script to2;
        public UdonSharpBehaviour[] callbackUdons = new UdonSharpBehaviour[0]; 

        [UdonSynced] public bool state;

        void Start()
        {
            applyChange();
        }

        public void SetRoute1()
        {
            if (Networking.IsOwner(gameObject)) owner_SetRoute1();
        }
        public void SetRoute2()
        {
            if (Networking.IsOwner(gameObject)) owner_SetRoute2();
        }

        private void owner_SetRoute1()
        {
            //Debug.Log("debug1 " + to1.name);
            state = false;
            applyChange();
            RequestSerialization();
        }
        private void owner_SetRoute2()
        {
            //Debug.Log("debug2 " + to2.name);
            state = true;
            applyChange();
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            applyChange();
        }

        private void applyChange()
        {
            Rail_Script target;
            if (!state)
            {
                //Debug.Log("debug1 " + to1.name);
                target = to1;
            }
            else
            {
                //Debug.Log("debug2 " + to2.name);
                target = to2;
            }
            if (from1 != null)
            {
                if (changeType1) from1.next = target;
                else from1.prev = target;
            }
            if (from2 != null)
            {
                if (changeType2) from2.next = target;
                else from2.prev = target;
            }
            foreach (UdonSharpBehaviour udon in callbackUdons)
            {
                udon.SendCustomEvent("PointUpdate");
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        Vector3 offset = new Vector3(0, 1, 0);
        void OnDrawGizmos()
        {
            DrawGizmo(0.1f,true, true);
        }
        void OnDrawGizmosSelected()
        {
            DrawGizmo(1f,true,true);
        }

        public void DrawGizmo(float alpha,bool drawTo1,bool drawTo2)
        {
            if (from1 != null)
            {
                CinemachinePathBase fromPath = from1.cinemachinePath;

                float fromChangeUnit;
                float fromChangeLineStart;
                if (changeType1)
                {
                    fromChangeUnit = fromPath.MaxPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart -= 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }
                else
                {
                    fromChangeUnit = fromPath.MinPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart += 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }

                Vector3 changeLinePoint = fromPath.EvaluatePosition(fromChangeUnit);
                Vector3 changeLineStart = fromPath.EvaluatePosition(fromChangeLineStart);
                Gizmos.color = new Color(0f, 1f, 0f, alpha);
                Gizmos.DrawLine(changeLineStart + offset * 2, changeLinePoint);

                CinemachinePathBase toPath;
                Vector3 toClosestPoint;
                float nextClosestUnit;
                if (drawTo1)
                {
                    Gizmos.color = new Color(0f, 1f, 1f, alpha);
                    toPath = to1.cinemachinePath;
                    nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                    nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                    if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                    else nextClosestUnit += 10;
                    nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                    toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                    Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);
                }
                if (drawTo2)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, alpha);
                    toPath = to2.cinemachinePath;
                    nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                    nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                    if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                    else nextClosestUnit += 10;
                    nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                    toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                    Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);
                }
            }
            if (from2 != null)
            {
                CinemachinePathBase fromPath = from2.cinemachinePath;

                float fromChangeUnit;
                float fromChangeLineStart;
                if (changeType2)
                {
                    fromChangeUnit = fromPath.MaxPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart -= 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }
                else
                {
                    fromChangeUnit = fromPath.MinPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart += 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }

                Vector3 changeLinePoint = fromPath.EvaluatePosition(fromChangeUnit);
                Vector3 changeLineStart = fromPath.EvaluatePosition(fromChangeLineStart);
                Gizmos.color = new Color(0f, 1f, 0f, alpha);
                Gizmos.DrawLine(changeLineStart + offset * 2, changeLinePoint);

                CinemachinePathBase toPath;
                Vector3 toClosestPoint;
                float nextClosestUnit;
                if (drawTo1)
                {
                    Gizmos.color = new Color(0f, 1f, 1f, alpha);
                    toPath = to1.cinemachinePath;
                    nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                    nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                    if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                    else nextClosestUnit += 10;
                    nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                    toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                    Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);
                }
                if (drawTo2)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, alpha);
                    toPath = to2.cinemachinePath;
                    nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                    nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                    if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                    else nextClosestUnit += 10;
                    nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                    toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                    Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);
                }
            }
        }
#endif
    }
}
