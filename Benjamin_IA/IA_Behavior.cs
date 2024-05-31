using AI_BehaviorTree_AIGameUtility;
using IA_BRAIN;
using System.Collections.Generic;
using System.Diagnostics;
using Vector3 = UnityEngine.Vector3;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.UIElements.NavigationMoveEvent;
using static UnityEngine.GraphicsBuffer;
using Windows.UI.Xaml.Media;
using System;

namespace Benjamin_IA
{


    public class IA_Behavior
    {

        public static Vector3 FirstOrderIntercept
(
    Vector3 shooterPosition,
    Vector3 shooterVelocity,
    float shotSpeed,
    Vector3 targetPosition,
    Vector3 targetVelocity
)
        {
            Vector3 targetRelativePosition = targetPosition - shooterPosition;
            Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;
            float t = FirstOrderInterceptTime
            (
                shotSpeed,
                targetRelativePosition,
                targetRelativeVelocity
            );
            return targetPosition + t * (targetRelativeVelocity);
        }
        //first-order intercept using relative target position
        public static float FirstOrderInterceptTime
        (
            float shotSpeed,
            Vector3 targetRelativePosition,
            Vector3 targetRelativeVelocity
        )
        {
            float velocitySquared = targetRelativeVelocity.sqrMagnitude;
            if (velocitySquared < 0.001f)
                return 0f;

            float a = velocitySquared - shotSpeed * shotSpeed;

            //handle similar velocities
            if (Mathf.Abs(a) < 0.001f)
            {
                float t = -targetRelativePosition.sqrMagnitude /
                (
                    2f * Vector3.Dot
                    (
                        targetRelativeVelocity,
                        targetRelativePosition
                    )
                );
                return Mathf.Max(t, 0f); //don't shoot back in time
            }

            float b = 2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition);
            float c = targetRelativePosition.sqrMagnitude;
            float determinant = b * b - 4f * a * c;

            if (determinant > 0f)
            { //determinant > 0; two intercept paths (most common)
                float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a),
                        t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
                if (t1 > 0f)
                {
                    if (t2 > 0f)
                        return Mathf.Min(t1, t2); //both are positive
                    else
                        return t1; //only t1 is positive
                }
                else
                    return Mathf.Max(t2, 0f); //don't shoot back in time
            }
            else if (determinant < 0f) //determinant < 0; no intercept path
                return 0f;
            else //determinant = 0; one intercept path, pretty much never happens
                return Mathf.Max(-b / (2f * a), 0f); //don't shoot back in time
        }

        public static bool CalculateTrajectory(float TargetDistance, float ProjectileVelocity, out float CalculatedAngle)
        {
            CalculatedAngle = 0.5f * (Mathf.Asin((-Physics.gravity.y * TargetDistance) / (ProjectileVelocity * ProjectileVelocity)) * Mathf.Rad2Deg);
            if (float.IsNaN(CalculatedAngle))
            {
                CalculatedAngle = 0;
                return false;
            }
            return true;
        }



        static Vector3 lastPosition = Vector3.negativeInfinity;
        static Vector3 oldPosition = Vector3.negativeInfinity;
        static Vector3 directorSpeed;
        static Vector3 directionLook;
        static List<PlayerInformations> playerInfos;
        static List<BonusInformations> Bonus;
        static List<AIAction> lstActions;
        static int LastTarget;
       
        public PlayerInformations FindNearestPlayer(List<PlayerInformations> list, PlayerInformations player)
        {
            float minDistance = float.MaxValue;
            int idxRayCast = -1;
            int idx = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == player)
                {
                    continue;
                }

                float tmp = Vector3.Distance(player.Transform.Position, list[i].Transform.Position);
                if (tmp < minDistance){
                    Vector3 dir = (player.Transform.Position - list[i].Transform.Position);
                   
                    if (!Physics.Raycast(player.Transform.Position, dir.normalized, dir.magnitude, 0))
                    {
                        idxRayCast = i;
                    }
                    idx = i;
                    minDistance = tmp;
                }


            }

            if(idxRayCast > 0)
            {
                return list[idxRayCast];
            }

            return list[idx];
        }
        public Vector3 findBarycenter(List<PlayerInformations> PlayerInfos, PlayerInformations player)
        {
            Vector3 baryCenter = new Vector3();
            float size = 0;
            for (int i = 0;i < PlayerInfos.Count; i++)
            {
                if (PlayerInfos[i] == player)
                    continue;

                baryCenter += PlayerInfos[i].Transform.Position;
                size++;
            }

            return baryCenter / size;
        }

        public int FindNearestActivableBonus(List<BonusInformations> BonusInfos, PlayerInformations playerInfos)
        {
            int idx = -1;
            float distance = float.MaxValue;
            for (int i = 0;  i < BonusInfos.Count; i++)
            {
                float tmp = Vector3.Distance(BonusInfos[i].Position, playerInfos.Transform.Position);
                if ( tmp < distance)
                {
                    idx = i;
                    distance = tmp;
                }
            }

            return idx;
        }


        public int FindNearestHeathBonus(List<BonusInformations> BonusInfos, PlayerInformations playerInfos)
        {
            int idx = -1;
            float distance = float.MaxValue;
            for (int i = 0; i < BonusInfos.Count; i++)
            {
                float tmp = Vector3.Distance(BonusInfos[i].Position, playerInfos.Transform.Position);
                if (tmp < distance && BonusInfos[i].Type == EBonusType.Health)
                {
                    idx = i;
                    distance = tmp;
                }
            }

            return idx;
        }

        public int FindNearestBonusStrong(List<BonusInformations> BonusInfos, PlayerInformations playerInfos)
        {
            int idx = -1;
            float distance = float.MaxValue;
            for (int i = 0; i < BonusInfos.Count; i++)
            {
                float tmp = Vector3.Distance(BonusInfos[i].Position, playerInfos.Transform.Position);
                if (tmp < distance)
                {
                    idx = i;
                    distance = tmp;
                }
            }

            return idx;
        }

        public int FindWeaker(List<PlayerInformations> playerInfos, PlayerInformations player)
        {
          
            int idx = -1;
            for (int i = 0; i < playerInfos.Count; i++)
            {
                if (playerInfos[i] == player)
                    continue;
                PlayerInformations target = playerInfos[i];
                bool has_no_bonus = playerInfos[i].BonusOnPlayer[EBonusType.Damage] < player.BonusOnPlayer[EBonusType.Damage] && target.BonusOnPlayer[EBonusType.Invulnerability] <=0;

                if (playerInfos[i].IsActive && has_no_bonus)
                {
                    idx = i;
                }
            }

            return idx;
        }


        public List<AIAction> Actions(GameWorldUtils AIGameWorldUtils, PlayerInformations myPlayerInfos)
        {
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            List<BonusInformations> Bonus = AIGameWorldUtils.GetBonusInfosList();
            List<ProjectileInformations> Projectils = AIGameWorldUtils.GetProjectileInfosList();
            List<AIAction> lstActions = new List<AIAction>();
            PlayerInformations target = FindNearestPlayer(playerInfos, myPlayerInfos);

            Vector3 deplacment_Direction = new Vector3();

            if (oldPosition.x <= float.NegativeInfinity)
            {
                oldPosition = target.Transform.Position;
            }

            if(!myPlayerInfos.IsActive)
                return lstActions;

            // Nodes

            // All nodes
            Node Shot_Nearest_Player = new Node();
            Shot_Nearest_Player.SetActionFunc(() => {
                Vector3 dir = target.Transform.Position - oldPosition;

                lstActions.Add(new AIActionLookAtPosition(target.Transform.Position+dir.normalized*8.0f));
                if ((target.Transform.Position - myPlayerInfos.Transform.Position).magnitude < 70.0f)
                {
                    
                    lstActions.Add(new AIActionFire());
                }
                return State.Failure;
            });

            Node Shot_Find_Bonus = new Node();
            Shot_Find_Bonus.SetActionFunc(() =>
            {
                int idx = FindNearestActivableBonus(Bonus, myPlayerInfos);
                if(idx >= 0)
                {
                    lstActions.Add(new AIActionMoveToDestination(Bonus[idx].Position));
                    return State.Success;
                }

                return State.Failure;
            });


            Node Shot_Find_Heal = new Node();
            Shot_Find_Heal.SetActionFunc(() =>
            {
                int idx = FindNearestHeathBonus(Bonus, myPlayerInfos);
                if (idx >= 0)
                {
                    lstActions.Add(new AIActionMoveToDestination(Bonus[idx].Position));
                    return State.Success;
                }

                return State.Failure;
            });



            Node CheckLife = new Node();
            CheckLife.SetActionFunc(() =>
            {
                if(myPlayerInfos.CurrentHealth < 4 && myPlayerInfos.BonusOnPlayer[EBonusType.Invulnerability] <= 0)
                {
                    return State.Success;
                }

                return State.Failure;
            });

            Node CheckLife_Chase = new Node();
            CheckLife_Chase.SetActionFunc(() =>
            {
                //bool has_no_bonus = target.BonusOnPlayer[EBonusType.CooldownReduction] <=0 && 
                //target.BonusOnPlayer[EBonusType.Damage] <=0 && target.BonusOnPlayer[EBonusType.Speed] <=0 && target.BonusOnPlayer[EBonusType.Invulnerability] <=0;

                if (myPlayerInfos.CurrentHealth < 4 || myPlayerInfos.BonusOnPlayer[EBonusType.Invulnerability] > 0)
                {
                    return State.Success;
                }

                return State.Failure;
            });


            Node Run = new Node();
            Run.SetActionFunc(() =>
            {
                Vector3 direction = Vector3.ProjectOnPlane( UnityEngine.Random.insideUnitSphere,Vector3.up);
                lstActions.Add(new AIActionMoveToDestination(myPlayerInfos.Transform.Position + direction*100.0f));
                lstActions.Add(new AIActionDash(direction));
                return State.Success;
            });


            Sequence Flee_Ennemie = new Sequence();
   
            Flee_Ennemie.Add(Run);

            Node FindWeak = new Node();
            FindWeak.SetActionFunc(() =>
            {
                int idx = FindWeaker(playerInfos,myPlayerInfos);
                if(idx > 0)
                {
                    Vector3 direction = (myPlayerInfos.Transform.Position - playerInfos[idx].Transform.Position).normalized;
                    lstActions.Add(new AIActionMoveToDestination(playerInfos[idx].Transform.Position));
                    lstActions.Add(new AIActionDash(direction));
                    return State.Success;
                }

                return State.Failure;    
            });

            Sequence Find_Life= new Sequence();
            Find_Life.Add(CheckLife);
            Find_Life.Add(Shot_Find_Heal);


            Sequence Chase = new Sequence();
            Chase.Add(CheckLife_Chase);
            Chase.Add(FindWeak);

            Selector Behavior_tree = new Selector();
            Behavior_tree.Add(Shot_Nearest_Player);

            Behavior_tree.Add(Find_Life);
            Behavior_tree.Add(Chase);
            Behavior_tree.Add(Shot_Find_Bonus);
            Behavior_tree.Add(Flee_Ennemie);
            

            Behavior_tree.Compute_Node();
            
            oldPosition = target.Transform.Position;
            return lstActions;
        }

    }
}
