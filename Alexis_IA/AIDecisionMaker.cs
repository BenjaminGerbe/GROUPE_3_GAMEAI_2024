using AI_BehaviorTree_AIGameUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using IA_BRAIN;

namespace AI_BehaviorTree_AIImplementation
{
    public class AIDecisionMaker
    {

        public PlayerInformations target = null;
        public static Vector3 lastTargetPos = Vector3.zero;
        public static Vector3 lastdir = Vector3.right;
        public static float distBonus = 99999;
        public static float distTarget;

        /// <summary>
        /// Ne pas supprimer des fonctions, ou changer leur signature sinon la DLL ne fonctionnera plus
        /// Vous pouvez unitquement modifier l'intérieur des fonctions si nécessaire (par exemple le nom)
        /// ComputeAIDecision en fait partit
        /// </summary>
        private int AIId = -1;
        public GameWorldUtils AIGameWorldUtils = new GameWorldUtils();

        // Ne pas utiliser cette fonction, elle n'est utile que pour le jeu qui vous Set votre Id, si vous voulez votre Id utilisez AIId
        public void SetAIId(int parAIId) { AIId = parAIId; }

        // Vous pouvez modifier le contenu de cette fonction pour modifier votre nom en jeu
        public string GetName() { return "Shouei Barou"; }

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }

        public void OnMyAIDeath() { }

        //Fin du bloc de fonction nécessaire (Attention ComputeAIDecision en fait aussi partit)

        public List<AIAction> ComputeAIDecision()
        {
            List<AIAction> actionList = new List<AIAction>();
            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            BonusInformations nearestBonus = GetNearestBonus(myPlayerInfos);
            this.target = GetTarget(myPlayerInfos, playerInfos);

            Selector start = new Selector();

            Sequence combat = new Sequence();

            Node predicfire = new Node();
            predicfire.SetActionFunc(() =>
            {
                if (this.target == null)
                    return State.Failure;
                if(myPlayerInfos.BonusOnPlayer.ContainsKey(EBonusType.BulletSpeed)|| myPlayerInfos.BonusOnPlayer.ContainsKey(EBonusType.CooldownReduction))
                    actionList.Add(new AIActionLookAtPosition(this.target.Transform.Position ));
                else
                    actionList.Add(new AIActionLookAtPosition(this.target.Transform.Position + getTargetDirection(this.target).normalized * 4));
                return State.Success;
            });

            Node shoot = new Node();
            shoot.SetActionFunc(() => {

                
                if (isTargetOnVision(myPlayerInfos) && !Physics.Raycast(myPlayerInfos.Transform.Position, myPlayerInfos.Transform.Rotation * Vector3.forward, 20f))
                {
                    actionList.Add(new AIActionFire());
                    return State.Success;
                }
                    
                return State.Failure;
            }
            );

            combat.Add(predicfire);
            combat.Add(shoot);

            Sequence movement = new Sequence();

            Node move = new Node();
            move.SetActionFunc(() =>
            {
                if (distTarget < distBonus)
                    actionList.Add(new AIActionMoveToDestination(this.target.Transform.Position));
                else
                    actionList.Add(new AIActionMoveToDestination(GetNearestBonus(myPlayerInfos).Position));
                return State.Success;
            });

            movement.Add(move);

            Selector dash = new Selector();
            Node dastToBonus = new Node();
            dastToBonus.SetActionFunc(() =>
            {
                if (!myPlayerInfos.IsDashAvailable)
                    return State.Failure;
                else
                {
                    if (nearestBonus == null)
                        return State.Failure;
                    if(Vector3.Distance(myPlayerInfos.Transform.Position, nearestBonus.Position) < 20)
                    {
                        actionList.Add(new AIActionDash((nearestBonus.Position - myPlayerInfos.Transform.Position)));
                        return State.Success;
                    }
                    return State.Failure;
                }
               
            });

            dash.Add(dastToBonus);

            Node dastToTarget = new Node();
            dastToTarget.SetActionFunc(() =>
            {
                if (!myPlayerInfos.IsDashAvailable)
                    return State.Failure;
                else
                {
                    if (target == null)
                        return State.Failure;
                    if (Vector3.Distance(myPlayerInfos.Transform.Position, target.Transform.Position) > 20)
                    {
                        actionList.Add(new AIActionDash((target.Transform.Position - myPlayerInfos.Transform.Position)));
                        return State.Success;
                    }
                    else
                    {
                        if(Vector3.Distance(myPlayerInfos.Transform.Position, target.Transform.Position) < 5)
                        {
                            actionList.Add(new AIActionDash((myPlayerInfos.Transform.Position - target.Transform.Position)));
                            return State.Success;
                        }
                    }
                    return State.Failure;
                }

            });

            dash.Add(dastToTarget);

            start.Add(combat);
            start.Add(movement);
            start.Add(dash);
            start.Compute_Node();

            lastTargetPos = target.Transform.Position;
            lastdir *= -1f;

            return actionList;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        

        public PlayerInformations GetPlayerInfos(int parPlayerId, List<PlayerInformations> parPlayerInfosList)
        {
            foreach (PlayerInformations playerInfo in parPlayerInfosList)
            {
                if (playerInfo.PlayerId == parPlayerId)
                    return playerInfo;
            }

            Assert.IsTrue(false, "GetPlayerInfos : PlayerId not Found");
            return null;
        }
        public PlayerInformations GetTarget(PlayerInformations me, List<PlayerInformations> others)
        {
            PlayerInformations target = null;

            float dist = 9999;
            if(others.Count >0)
            {
                foreach(PlayerInformations p in others)
                {
                    if (p == me)
                        continue;

                    if (!p.IsActive)
                        continue;

                    if(Vector3.Distance(me.Transform.Position, p.Transform.Position) < dist)
                    {
                        target = p;
                        dist = Vector3.Distance(me.Transform.Position, p.Transform.Position);
                    }
                }
            }


            return target;
        }

        public BonusInformations GetNearestBonus(PlayerInformations me)
        {
            BonusInformations nearest = null;
            List<BonusInformations> bonusInfos = AIGameWorldUtils.GetBonusInfosList();

            float dist = 9999999;
            if (bonusInfos.Count != 0)
            {
                foreach (BonusInformations bonus in bonusInfos)
                {

                    if (Vector3.Distance(me.Transform.Position, bonus.Position) < dist)
                    {
                        dist = Vector3.Distance(me.Transform.Position, bonus.Position);
                        distBonus = dist;
                        nearest = bonus;
                    }
                }
            }
            

            return nearest;
        }

        public Vector3 getTargetDirection(PlayerInformations target)
        {
            Vector3 dir = target.Transform.Position - lastTargetPos;

            return dir;
        }


        public bool isTargetOnVision(PlayerInformations me)
        {
            Vector3 dirTarget = target.Transform.Position - me.Transform.Position;


            if (Vector3.Dot(dirTarget, me.Transform.Rotation * Vector3.forward)>0.75f)
            {
                return true;
            }
            return false;
        }
    }
}
