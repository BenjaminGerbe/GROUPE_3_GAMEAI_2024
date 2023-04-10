using AI_BehaviorTree_AIGameUtility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AI_BehaviorTree_AIImplementation
{
    public class AIDecisionMaker
    {

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
        public string GetName() { return "MyAIName"; }

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }

        //Fin du bloc de fonction nécessaire (Attention ComputeAIDecision en fait aussi partit)


        private float BestDistanceToFire = 10.0f;
        public List<AIAction> ComputeAIDecision()
        {
            List<AIAction> actionList = new List<AIAction>();

            List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();

            PlayerInformations target = null;
            foreach (PlayerInformations playerInfo in playerInfos)
            {
                if (!playerInfo.IsActive)
                    continue;

                if (playerInfo.PlayerId == AIId)
                    continue;

                target = playerInfo;
                break;
            }

            if (target == null)
                return actionList;

            PlayerInformations myPlayerInfo = GetPlayerInfos(AIId, playerInfos);
            if (myPlayerInfo == null)
                return actionList;

            if (Vector3.Distance(myPlayerInfo.Transform.Position, target.Transform.Position) < BestDistanceToFire)
            {
                AIActionStopMovement actionStop = new AIActionStopMovement();
                actionList.Add(actionStop);
            }
            else
            {
                AIActionMoveToDestination actionMove = new AIActionMoveToDestination();
                actionMove.Position = target.Transform.Position;
                actionList.Add(actionMove);
            }


            AIActionLookAtPosition actionLookAt = new AIActionLookAtPosition();
            actionLookAt.Position = target.Transform.Position;
            actionList.Add(actionLookAt);
            actionList.Add(new AIActionFire());

            return actionList;
        }

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
    }
}
