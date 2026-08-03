
using Cinemachine;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using static Cinemachine.CinemachinePathBase;

namespace frou01.RigidBodyTrain
{
    public class PointLever_Setter : AbstractPointSetter
    {
        public Rail_Script from1;
        public Rail_Script from2;
        public bool changeType1;//true:next  false:prev
        public bool changeType2;//true:next  false:prev
        public Rail_Script to1;
        public Rail_Script to2;

        [Tooltip("Need owner check and sync")][SerializeField]bool OwnerSlaveMode = false;
        [UdonSynced] public bool state;
        [UdonSynced] public bool inprgrs;

        public void SetRoute1()
        {
            if (!OwnerSlaveMode || Networking.IsOwner(gameObject)) owner_SetRoute1();
        }
        public void SetRoute2()
        {
            if (!OwnerSlaveMode || Networking.IsOwner(gameObject)) owner_SetRoute2();
        }
        public void SetInprogress()
        {
            if (!OwnerSlaveMode || Networking.IsOwner(gameObject)) owner_SetInprogress();
        }

        private void owner_SetRoute1()
        {
            //Debug.Log("debug1 " + to1.name);
            state = false;
            inprgrs = false;
            applyChange();
            if(OwnerSlaveMode) RequestSerialization();
        }
        private void owner_SetRoute2()
        {
            //Debug.Log("debug2 " + to2.name);
            state = true;
            inprgrs = false;
            applyChange();
            if (OwnerSlaveMode) RequestSerialization();
        }
        private void owner_SetInprogress()
        {
            //Debug.Log("debug2 " + to2.name);
            inprgrs = true;
            applyChange();
            if (OwnerSlaveMode) RequestSerialization();
        }

        protected override void applyChange()
        {
            Rail_Script target;
            if (inprgrs)
            {
                target = null;
            }
            else if (!state)
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
            base.applyChange();
        }

        public override void OnDeserialization()
        {
            if(OwnerSlaveMode)applyChange();
        }

        public override void set_route_To(int routeIndex)
        {
            if (routeIndex == 0)
            {
                SetRoute1();
            }
            else if (routeIndex == 1)
            {
                SetRoute2();
            }
            else if (routeIndex == -1)
            {
                SetInprogress();
            }
            return;
        }
        public override Rail_Script[] getRoutes()
        {
            return new Rail_Script[] { to1, to2 };
        }
        public override int get_current_To_Index()
        {
            return inprgrs? -1 : (state ? 1 : 0);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            DrawGizmo_From();
            Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
            DrawGizmo_To(to1);
            Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
            DrawGizmo_To(to2);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 1f);
            DrawGizmo_From();
            Gizmos.color = new Color(0f, 1f, 1f, 1f);
            DrawGizmo_To(to1);
            Gizmos.color = new Color(1f, 1f, 0f, 1f);
            DrawGizmo_To(to2);
        }
        public override void DrawGizmo_From()
        {
            Vector3 offset = new Vector3(0, 1, 0);
            if (from1 != null)
            {
                Vector3 changeLinePoint;
                Vector3 changeLineStart;
                getEdgePoint(from1, changeType1, out changeLinePoint, out changeLineStart);
                Gizmos.DrawLine(changeLineStart + offset * 2, changeLinePoint);
            }
            if (from2 != null)
            {
                Vector3 changeLinePoint;
                Vector3 changeLineStart;
                getEdgePoint(from2, changeType2, out changeLinePoint, out changeLineStart);
                Gizmos.DrawLine(changeLineStart + offset * 2, changeLinePoint);
            }
        }
        public override void DrawGizmo_To(Rail_Script targetRail)
        {
            Vector3 offset = new Vector3(0, 1, 0);
            if (from1 != null)
            {

                Vector3 changeLinePoint;
                Vector3 changeLineStart;
                getEdgePoint(from1, changeType1, out changeLinePoint, out changeLineStart);

                CinemachinePathBase toPath;
                Vector3 toClosestPoint;
                float nextClosestUnit;
                if (targetRail)
                {
                    toPath = targetRail.cinemachinePath;
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
                Vector3 changeLinePoint;
                Vector3 changeLineStart;
                getEdgePoint(from1, changeType1, out changeLinePoint, out changeLineStart);

                CinemachinePathBase toPath;
                Vector3 toClosestPoint;
                float nextClosestUnit;
                if (targetRail)
                {
                    toPath = targetRail.cinemachinePath;
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
        public static void getEdgePoint(Rail_Script targetRail, bool GetNextEdge, out Vector3 changeLinePoint, out Vector3 changeLineStart)
        {
            CinemachinePathBase fromPath = targetRail.cinemachinePath;

            float fromChangeUnit;
            float fromChangeLineStart;
            if (GetNextEdge)
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

            changeLinePoint = fromPath.EvaluatePosition(fromChangeUnit);
            changeLineStart = fromPath.EvaluatePosition(fromChangeLineStart);
        }

        [Obsolete]
        public void DrawGizmo(float alpha, bool drawTo1, bool drawTo2)
        {
            Vector3 offset = new Vector3(0, 1, 0);
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
