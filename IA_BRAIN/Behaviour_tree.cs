using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition.Scenes;

namespace IA_BRAIN
{
    public enum State{
        Note_Executed,
        Running,
        Failure,
        Success
    }

    public enum Special{
        Always_Success,
        Always_Failure,
        Invert
        //Repeat,
        //Retry
    }
    public class Node{
        protected State state;
        protected List<Node> nodes;
        protected List<Special> specials;
        protected Func<State> Action;

        public Node()
        {
            state = State.Note_Executed;
            this.nodes = new List<Node>();
            this.specials = new List<Special>();
        }


        public Node(List<Node> nodes)
        {
            state = State.Note_Executed;
            this.nodes = nodes;
            this.specials = new List<Special>();
        }

        public Node(List<Node> nodes,List<Special> specials){
            state = State.Note_Executed;
            this.nodes = nodes;
            this.specials = specials;
        }

        public void SetActionFunc(Func<State> Conditions){
            this.Action = Conditions;
        }

        public virtual void Compute_Node(){
            this.state = this.Action.Invoke();
            for (int i = 0; i < this.specials.Count; i++)
            {
                if (this.specials[i] == Special.Invert &&
                    this.state == State.Success || this.state == State.Failure){
                    if(this.state == State.Success){
                        this.state = State.Failure;
                    }
                    else{
                        this.state = State.Success;
                    }
                }

                if (this.specials[i] == Special.Always_Success)
                {
                    this.state = State.Success;
                }

                if (this.specials[i] == Special.Always_Failure)
                {
                    this.state = State.Failure;
                }
            }
        }

        public State GetState() { return state; }
        public void Add(Node node) { nodes.Add(node); }
    }


    public class Selector : Node
    {

        public Selector() : base()
        {
        }
        public Selector(List<Node> nodes) : base(nodes)
        {
        }
        public Selector(List<Node> nodes, List<Special> specials) : base(nodes, specials)
        {
        }

        public override void Compute_Node(){
            this.state = State.Running;
            for (int i =0;i< this.nodes.Count; i++)
            {
                this.nodes[i].Compute_Node();
                if (this.nodes[i].GetState() == State.Success){
                    this.state = State.Success;
                    return;
                }
            }
            this.state = State.Failure;
        }
    }

    public class Sequence : Node{
        public Sequence() : base(){
        }
        public Sequence(List<Node> nodes) : base(nodes){
        }
        public Sequence(List<Node> nodes, List<Special> specials) : base(nodes, specials){
        }

     
        public override void Compute_Node(){
            this.state = State.Running;
            for (int i = 0; i < this.nodes.Count; i++)
            {
                this.nodes[i].Compute_Node();
                if (this.nodes[i].GetState() == State.Failure){
                    this.state = State.Failure;
                    return;
                }
            }
            this.state = State.Success;
        }
    }

}
