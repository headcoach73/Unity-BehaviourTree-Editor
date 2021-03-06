using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System;
using UnityEditor;

namespace BehaviourTree.Editor
{
    public class BehaviourTreeGraphView : GraphView
    {
        public readonly Vector2 DefaultNodeSize = new Vector2(150, 200);

        private NodeSearchWindow m_searchWindow;

        public BehaviourTreeGraphView(EditorWindow window)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("BehaviourTreeGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddElement(GenerateEntryPointNode());
            AddSearchWindow(window);
        }

        public SelectorGraphNode GenerateEntryPointNode()
        {
            //Root node in a behaviour tree is always a selector node
            var node = new SelectorGraphNode
            {
                title = "Root Selector Node",
                Guid = Guid.NewGuid().ToString(),
                RootNode = true,
            };

            var generatedPort = GeneratePort(node, Direction.Input);
            generatedPort.portName = "Child 1";

            var deleteButton = new Button(() => RemovePort(node, generatedPort)) { text = "X" };
            generatedPort.contentContainer.Add(deleteButton);

            node.inputContainer.Add(generatedPort);

            var button = new Button(() => AddChoicePort(node));
            button.text = "New Child";
            node.titleContainer.Add(button);

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new Rect(250, 200, 100, 150));

            return node;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach((port) => 
            {
                if (startPort != port && startPort.node!=port.node)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        public SelectorGraphNode CreateSelectorGraphNode(Vector2 positon)
        {
            var selectorGraphNode = new SelectorGraphNode
            {
                title = "Selector Node",
                Guid = Guid.NewGuid().ToString(),
            };
            
            var outputPort = GeneratePort(selectorGraphNode, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "Output";
            selectorGraphNode.outputContainer.Add(outputPort);

            var button = new Button(() => AddChoicePort(selectorGraphNode));
            button.text = "New Child";
            selectorGraphNode.titleContainer.Add(button);

            selectorGraphNode.RefreshExpandedState();
            selectorGraphNode.RefreshPorts();
            selectorGraphNode.SetPosition(new Rect(positon, DefaultNodeSize));

            AddElement(selectorGraphNode);

            return selectorGraphNode;
        }

        public SequenceGraphNode CreateSequenceGraphNode(Vector2 positon)
        {
            var sequenceGraphNode = new SequenceGraphNode
            {
                title = "Sequence Node",
                Guid = Guid.NewGuid().ToString(),
            };


            var outputPort = GeneratePort(sequenceGraphNode, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "Output";
            sequenceGraphNode.outputContainer.Add(outputPort);

            var button = new Button(() => AddChoicePort(sequenceGraphNode));
            button.text = "New Child";
            sequenceGraphNode.titleContainer.Add(button);

            sequenceGraphNode.RefreshExpandedState();
            sequenceGraphNode.RefreshPorts();
            sequenceGraphNode.SetPosition(new Rect(positon, DefaultNodeSize));

            AddElement(sequenceGraphNode);

            return sequenceGraphNode;
        }

        public RepeaterGraphNode CreateRepeaterGraphNode(Vector2 positon, Repeater.Mode _repeatType = Repeater.Mode.UntilSuccess, string maxRepeats = "")
        {
            var repeaterGraphNode = new RepeaterGraphNode
            {
                title = "Repeater Node",
                Guid = Guid.NewGuid().ToString(),
                MaxNumberOfRepeats = maxRepeats,
                RepeatMode = _repeatType,
            };


            var outputPort = GeneratePort(repeaterGraphNode, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "Output";
            repeaterGraphNode.outputContainer.Add(outputPort);

            AddChoicePort(repeaterGraphNode, " ", false);

            repeaterGraphNode.CreateEnumField("Mode", repeaterGraphNode.RepeatMode);

            var fieldLabel = new Label("Number of Repeats:");

            var numberField = new TextField();
            numberField.value = repeaterGraphNode.MaxNumberOfRepeats;
            numberField.RegisterValueChangedCallback(evt => repeaterGraphNode.CheckNumberOfRepeats(evt.newValue, numberField));
            

            repeaterGraphNode.mainContainer.Add(fieldLabel);
            repeaterGraphNode.mainContainer.Add(numberField);

            repeaterGraphNode.RefreshExpandedState();
            repeaterGraphNode.RefreshPorts();
            repeaterGraphNode.SetPosition(new Rect(positon, DefaultNodeSize));

            AddElement(repeaterGraphNode);

            return repeaterGraphNode;
        }

        public LeafGraphNode CreateLeafGraphNode(Vector2 positon, LeafScript _leafScript = null)
        {
            var leafGraphNode = new LeafGraphNode
            {
                title = "Leaf Node",
                Guid = Guid.NewGuid().ToString(),
                LeafScript = _leafScript,
            };

            var scriptDescription = new Label();

            if (leafGraphNode.LeafScript != null)
            {
                leafGraphNode.title = leafGraphNode.LeafScript.name;
                scriptDescription.text = leafGraphNode.LeafScript.Description;
            }

            var outputPort = GeneratePort(leafGraphNode, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "Output";
            leafGraphNode.outputContainer.Add(outputPort);

            var fieldLabel = new Label("Leaf Script:");
            var logicField = new ObjectField();
            logicField.objectType = typeof(LeafScript);
            logicField.value = leafGraphNode.LeafScript;
            logicField.RegisterValueChangedCallback(evt => leafGraphNode.LeafScript = evt.newValue as LeafScript);

            //These events are used to update the nodes visual appearence when applying a LeafScript
            logicField.RegisterValueChangedCallback(evt => leafGraphNode.title = evt.newValue != null? evt.newValue.name : "Leaf Node");
            logicField.RegisterValueChangedCallback(evt => scriptDescription.text = evt.newValue != null ? ((LeafScript)evt.newValue).Description : "");

            leafGraphNode.inputContainer.Add(scriptDescription);
            leafGraphNode.mainContainer.Add(fieldLabel);
            leafGraphNode.mainContainer.Add(logicField);

            leafGraphNode.RefreshExpandedState();
            leafGraphNode.RefreshPorts();
            leafGraphNode.SetPosition(new Rect(positon, DefaultNodeSize));

            AddElement(leafGraphNode);

            return leafGraphNode;
        }

        public void AddChoicePort(BehaviourTreeGraphNode graphNode, string portName = "", bool createDeleteButton = true)
        {
            if (graphNode.AllowedChildren == AllowedChildren.None) return;

            var outputPortCount = graphNode.inputContainer.Query("connector").ToList().Count;

            //Don't allow more then one port if Allowed children is set to single
            if (graphNode.AllowedChildren == AllowedChildren.Single && outputPortCount > 0)
            {
                return;
            }

            var generatedPort = GeneratePort(graphNode, Direction.Input);

            generatedPort.portName = string.IsNullOrWhiteSpace(portName)? $"Child {outputPortCount + 1}" : portName;

            if (createDeleteButton) 
            {
                var deleteButton = new Button(() => RemovePort(graphNode, generatedPort)) { text = "X" };
                generatedPort.contentContainer.Add(deleteButton);
            }
            

            graphNode.inputContainer.Add(generatedPort);
            
            graphNode.RefreshPorts();
            graphNode.RefreshExpandedState();

        }

        public void RemovePort(BehaviourTreeGraphNode graphNode, Port generatedPort)
        {
            var targetEdge = edges.ToList().Where(x => x.input.portName == generatedPort.portName && x.input.node == generatedPort.node);
            
            if (targetEdge.Any())
            { 
                var edge = targetEdge.First();
                edge.output.Disconnect(edge);
                RemoveElement(targetEdge.First());
            }

            graphNode.inputContainer.Remove(generatedPort);
            graphNode.RefreshPorts();
            graphNode.RefreshExpandedState();
        }

        private void AddSearchWindow(EditorWindow window)
        {
            m_searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            m_searchWindow.Init(this, window);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_searchWindow);
        }

        private Port GeneratePort(BehaviourTreeGraphNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float)); //Arbitary Type
        }
    }
}




