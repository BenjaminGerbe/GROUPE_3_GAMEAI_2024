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
        private static int AIId = -1;
        public static List<AIAction> listAI = new List<AIAction>();
        public static GameWorldUtils AIGameWorldUtils = new GameWorldUtils();
        public static List<PlayerInformations> playerInfos = AIGameWorldUtils.GetPlayerInfosList();
        public static List<BonusInformations> bonusInfos = AIGameWorldUtils.GetBonusInfosList();
        public static PlayerInformations myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
        public static PlayerInformations playerTarget;
        public static BonusInformations bonusTarget;  
        public static Node tree;
        public static string nextTarget;
        public static Vector3 TargetPos;
        public static string nextDash = "left";
        public static bool needToDash = false;
        /// <summary>
        /// Ne pas supprimer des fonctions, ou changer leur signature sinon la DLL ne fonctionnera plus
        /// Vous pouvez unitquement modifier l'intérieur des fonctions si nécessaire (par exemple le nom)
        /// ComputeAIDecision en fait partit
        /// </summary>


        public List<PlayerInformations> PlayerInfos { get => playerInfos; set => playerInfos = value; }
        public List<PlayerInformations> PlayerInfos1 { get => playerInfos; set => playerInfos = value; }

        // Ne pas utiliser cette fonction, elle n'est utile que pour le jeu qui vous Set votre Id, si vous voulez votre Id utilisez AIId
        public void SetAIId(int parAIId) { AIId = parAIId; }

        // Vous pouvez modifier le contenu de cette fonction pour modifier votre nom en jeu
        public string GetName() { return "Sharkio"; }

        public void SetAIGameWorldUtils(GameWorldUtils parGameWorldUtils) { AIGameWorldUtils = parGameWorldUtils; }

        public void OnMyAIDeath() {
            createTree();
        }

        //Fin du bloc de fonction nécessaire (Attention ComputeAIDecision en fait aussi partit)

        public List<AIAction> ComputeAIDecision()
        {
            playerInfos = AIGameWorldUtils.GetPlayerInfosList();
            bonusInfos = AIGameWorldUtils.GetBonusInfosList();
            myPlayerInfos = GetPlayerInfos(AIId, playerInfos);
            createTree();
            
            return AIDecisionMaker.listAI;
        }

        public static PlayerInformations GetPlayerInfos(int parPlayerId, List<PlayerInformations> parPlayerInfosList)
        {
            foreach (PlayerInformations playerInfo in parPlayerInfosList)
            {
                if (playerInfo.PlayerId == parPlayerId)
                    return playerInfo;
            }

            Assert.IsTrue(false, "GetPlayerInfos : PlayerId not Found");
            return null;
        }

        private void createTree()
        {
            LookAtNode lookAt = new LookAtNode();
            GoToNode goTo = new GoToNode();
            DashNode dash = new DashNode();
            FireNode fire = new FireNode();
            StopMovementNode stop = new StopMovementNode();
            ChooseTargetNode chooseNext = new ChooseTargetNode();
            NeedToDashNode needToDash = new NeedToDashNode();
            NeedToFireNode needToFire = new NeedToFireNode();
            Node n = new Node();
            n.SetActionFunc(() => { Debug.Log("aucune action"); return State.Success; });
            List<Node> DashList = new List<Node>() {needToDash,dash};
            List<Node> FireList = new List<Node>() {needToFire,fire};
            Sequence DashSeq = new Sequence(DashList);
            Sequence FireSeq = new Sequence(FireList);
            List<Node> dashSel= new List<Node>() {DashSeq, n};
            Selector DashSelect = new Selector(dashSel);
            List<Node> fireSel = new List<Node>() { FireSeq, n};
            Selector FireSelect = new Selector(fireSel);
            List<Node> chooseList = new List<Node>() { chooseNext,goTo, lookAt};
            Sequence targetChooseSeq = new Sequence(chooseList); 
            List<Node> afterFire = new List<Node>() { targetChooseSeq, DashSelect};
            Selector AFSelect = new Selector(afterFire);
            List<Node> start = new List<Node>() { fire, AFSelect};
            Sequence StartSeq = new Sequence(start);
            tree =  StartSeq;
            tree.Compute_Node();
        }
    }

    public class StopMovementNode : Node
    {
        public StopMovementNode() : base() 
        {
            this.SetActionFunc(() =>
            {
                AIDecisionMaker.listAI.Add(new AIActionStopMovement());
                return State.Success;
            });
        }
    }

    public class GoToNode : Node
    {
        public GoToNode() : base()
        {
            this.SetActionFunc(() =>
            {
                Debug.Log(AIDecisionMaker.nextTarget.ToString());
                if (AIDecisionMaker.nextTarget == "Player")
                {
                    
                    Debug.Log("Go to Player");
                    AIDecisionMaker.TargetPos = AIDecisionMaker.playerTarget.Transform.Position;
                }
                else
                {
                    Debug.Log("Go to Bonus");
                    AIDecisionMaker.TargetPos = AIDecisionMaker.bonusTarget.Position;
                }
                AIDecisionMaker.listAI.Add(new AIActionMoveToDestination(AIDecisionMaker.TargetPos));
                return State.Success;
            });
        }
    }

    public class LookAtNode : Node
    {
        public LookAtNode() : base()
        {
            this.SetActionFunc(() =>
            {
                
                    AIDecisionMaker.listAI.Add(new AIActionLookAtPosition(AIDecisionMaker.TargetPos));
               
                
                return State.Success;
            });
        }
    }

    public class DashNode : Node
    {
        int delai = 0;
        public DashNode() : base() {
            this.SetActionFunc(() => {
                if (delai == 0)
                {
                    delai = 10;
                    Debug.Log("Dash");
                    if (AIDecisionMaker.myPlayerInfos.IsDashAvailable)
                    {
                        Debug.Log(AIDecisionMaker.nextDash);
                        Vector3 direction = AIDecisionMaker.TargetPos - AIDecisionMaker.myPlayerInfos.Transform.Position;
                        if (AIDecisionMaker.nextDash == "left")
                        {
                            direction = Quaternion.Euler(0,-90,0) * direction;
                            
                        }
                        else if (AIDecisionMaker.nextDash == "right")
                        {
                            direction = Quaternion.Euler(0, 90, 0) * direction;
                            
                        }
                        else if (AIDecisionMaker.nextDash == "back")
                        {
                            direction = Quaternion.Euler(0, 180, 0) * direction;
                        }
                        Debug.Log(direction.ToString());
                        AIDecisionMaker.listAI.Add(new AIActionDash(direction));
                    }
                }
                else
                {
                    delai--;
                }
                return State.Success;
            });
        }
    }

    public class FireNode : Node
    {
        public FireNode() : base()
        {
            this.SetActionFunc(() =>
            {
                
                AIDecisionMaker.listAI.Add(new AIActionFire());
  
                return State.Success;
            });
        }
    }

    
    public class ChooseTargetNode : Node
    {
        PlayerInformations tempTargetP = null;
        BonusInformations tempTargetB = null;
        float minDistP, minDistB;
        int delai = 0;
        int life;
        public ChooseTargetNode() : base() 
        {


            this.SetActionFunc(() =>
            {
                tempTargetP = null;
                tempTargetB = null;
                minDistP = int.MaxValue;
                minDistB = int.MaxValue;
                if (delai == 0)
                {
                    delai = 25;
                    foreach (PlayerInformations otherinfos in AIDecisionMaker.playerInfos)
                    {

                        if (otherinfos.PlayerId != AIDecisionMaker.myPlayerInfos.PlayerId & otherinfos.IsActive)
                        {
                            float dist = Vector3.Distance(AIDecisionMaker.myPlayerInfos.Transform.Position, otherinfos.Transform.Position);
                            if (tempTargetP == null)
                            {

                                tempTargetP = otherinfos;
                                minDistP = dist;
                                life = otherinfos.CurrentHealth;
                            }
                            else
                            {
                                if (otherinfos.CurrentHealth < life)
                                {
                                    tempTargetP = otherinfos;
                                    minDistP = dist;
                                    life = otherinfos.CurrentHealth;
                                }
                            }
                        }
                    }
                    Debug.Log("ok");

                    foreach (BonusInformations BonusInfo in AIDecisionMaker.bonusInfos)
                    {
                        if (BonusInfo.Position != null)
                        {
                            float dist = Vector3.Distance(AIDecisionMaker.myPlayerInfos.Transform.Position, BonusInfo.Position);
                            Debug.Log("ok distance");
                            if (tempTargetB == null)
                            {
                                tempTargetB = BonusInfo;
                                minDistB = dist;
                            }
                            else
                            {
                                if (dist < minDistB)
                                {
                                    tempTargetB = BonusInfo;
                                    minDistB = dist;
                                }
                            }
                        }

                    }

                    AIDecisionMaker.bonusTarget = tempTargetB;
                    AIDecisionMaker.playerTarget = tempTargetP;
                    if (tempTargetB == null)
                    {
                        if (tempTargetP == null)
                        {
                            return State.Failure;
                        }
                        else
                        {
                            AIDecisionMaker.nextTarget = "Player";
                        }
                    }
                    else
                    {
                        if (AIDecisionMaker.myPlayerInfos.BonusOnPlayer.Count < 1)
                        {
                            AIDecisionMaker.nextTarget = "Bonus";
                        }
                        else
                        {
                            if (minDistP < minDistB)
                            {

                                AIDecisionMaker.nextTarget = "Player";
                            }
                            else
                            {

                                AIDecisionMaker.nextTarget = "Bonus";
                            }
                        }
                        
                    }
                }
                else
                {
                    delai--;
                }
                
                return State.Success;
            });
        }

        
    }

    public class NeedToDashNode : Node
    {
        float range = 5f;
        public NeedToDashNode() : base()
        {
            this.SetActionFunc(() =>
            {
                if (AIDecisionMaker.nextTarget == "bonus" )
                {
                    AIDecisionMaker.nextDash = "forward"; 
                }
                else
                {
                    if (Vector3.Distance(AIDecisionMaker.myPlayerInfos.Transform.Position,AIDecisionMaker.TargetPos) < range)
                    {
                        if (AIDecisionMaker.nextDash != "right" & AIDecisionMaker.nextDash != "left")
                        {
                            AIDecisionMaker.nextDash = "right";
                        }
                        else
                        {
                            if (AIDecisionMaker.nextDash== "left")
                            {
                                AIDecisionMaker.nextDash = "right";
                            }
                            else
                            {
                                AIDecisionMaker.nextDash = "left";
                            }
                        }
                    }
                }
                return State.Success;
            });
        }
    }

    public class NeedToFireNode : Node
    {
        public NeedToFireNode() : base() {
            this.SetActionFunc(() =>
            {
                if (AIDecisionMaker.nextTarget == "Player")
                {
                    return State.Success;
                }
                else
                {
                    return State.Failure;
                }
            });
        }
    }
}
