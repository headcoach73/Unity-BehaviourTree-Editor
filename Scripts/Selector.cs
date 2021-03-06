using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTree
{
    [System.Serializable]
    public class Selector : Node
    {
        /** The child nodes for this selector */
        [SerializeField]
        protected List<Node> m_nodes = new List<Node>();

        /** The constructor requires a list of child nodes to be  
        * passed in*/
        public Selector(List<Node> nodes, string nodeGuid)
        {
            m_nodes = nodes;
            m_nodeGuid = nodeGuid;
        }
        /* If any of the children reports a success, the selector will 
        * immediately report a success upwards. If all children fail, 
        * it will report a failure instead.*/
        public override NodeStates Evaluate(Context context)
        {
            if (!context.CompositeNodeIndex.ContainsKey(m_nodeGuid))
            {
                context.CompositeNodeIndex[m_nodeGuid] = 0;
            }

            while (context.CompositeNodeIndex[m_nodeGuid] < m_nodes.Count)
            {
                switch (m_nodes[context.CompositeNodeIndex[m_nodeGuid]].Evaluate(context))
                {
                    case NodeStates.FAILURE:
                        context.CompositeNodeIndex[m_nodeGuid]++;
                        break;
                    case NodeStates.SUCCESS:
                        m_nodeState = NodeStates.SUCCESS;
                        //If Success reset nodes progress
                        context.CompositeNodeIndex[m_nodeGuid] = 0;
                        return m_nodeState;
                    case NodeStates.RUNNING:
                        m_nodeState = NodeStates.RUNNING;
                        return m_nodeState;
                    default:
                        continue;
                }
            }
            context.CompositeNodeIndex[m_nodeGuid] = 0;
            m_nodeState = NodeStates.FAILURE;
            return m_nodeState;
        }
    }
}
