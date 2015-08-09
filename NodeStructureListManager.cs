/*
The MIT License (MIT)

Copyright (c) 2015 Kevin Lyons

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Net;

namespace TestingSuiteApplication
{
    class NodeStructureListManager
    {
        #region NodeStructureAdditonRemoval

        public static void addNodeStructure(NodeStructure nodeStructure, NodeStructureList nodeStructureList)
        {
            nodeStructureList.Add(nodeStructure);
            nodeStructureList = NodeStructureListManager.alphabetize(nodeStructureList);
            nodeStructureList = NodeStructureListManager.reindex(nodeStructureList);
        }

        public static void removeNodeStructure(NodeStructure nodeStructure, NodeStructureList nodeStructureList)
        {
            for (int i = 0; i < nodeStructureList.Count; i++)
            {
                if (nodeStructureList[i].name == nodeStructure.name)
                {
                    nodeStructureList.RemoveAt(i);
                }
            }
            nodeStructureList = NodeStructureListManager.alphabetize(nodeStructureList);
            nodeStructureList = NodeStructureListManager.reindex(nodeStructureList);
        }

        #endregion

        #region SortingAndIndexing

        public static NodeStructureList alphabetize(NodeStructureList nodeStructureList)
        {
            nodeStructureList.Sort((s1, s2) => s1.name.CompareTo(s2.name));
            return nodeStructureList;
        }

        public static NodeStructureList reindex(NodeStructureList nodeStructureList)
        {
            for (int i = 0; i < nodeStructureList.Count; i++)
            {
                nodeStructureList[i].index = i;
            }
            return nodeStructureList;
        }

        #endregion

        #region Helper Methods

        public static void uncheckAllNodeStructures(NodeStructureList nodeStructureList)
        {
            NodeStructure nodeStructure = null;
            for (int i = 0; i < nodeStructureList.Count; i++)
            {
                nodeStructure = nodeStructureList[i];
                NodeStructureManager.uncheck(nodeStructureList, nodeStructure, ENodeStructureCheckedEntryPoint.USER);
            }
        }

        public static bool containsNodeStructure(NodeStructureList nodeStructureList, NodeStructure nodeStructure)
        {
            foreach (NodeStructure node in nodeStructureList)
            {
                if (node.name == nodeStructure.name)
                {
                    return true;
                }
            }
            return false;
        }

        public static NodeStructureList getCheckedNodes(NodeStructureList nodeStructureList)
        {
            NodeStructureList nodeList = new NodeStructureList();
            foreach (NodeStructure nodeStructure in nodeStructureList)
            {
                if (nodeStructure.isParent)
                {
                    foreach (NodeStructure childNodeStructure in nodeStructure.children)
                    {
                        if (childNodeStructure.isParent)
                        {
                            foreach (NodeStructure endNodeStructure in childNodeStructure.children)
                            {
                                if (endNodeStructure.isChecked)
                                {
                                    NodeStructureListManager.addNodeStructure(endNodeStructure, nodeList);
                                }
                            }
                        }
                    }
                }
            }
            return nodeList;
        }

        public static NodeStructureList removeDuplicateNodes(NodeStructureList nodeStructureList)
        {
            NodeStructure nodeStructure = null;
            for (int j = 0; j < nodeStructureList.Count; j++)
            {
                nodeStructure = nodeStructureList[j];
                int index = j + 1;
                if (j != nodeStructureList.Count - 1)
                {
                    for (int i = index; i < nodeStructureList.Count; i++)
                    {
                        if (nodeStructureList[i].name == nodeStructure.name)
                        {
                            NodeStructureListManager.removeNodeStructure(nodeStructureList[i], nodeStructureList);
                            i--;
                        }
                    }
                }
            }
            return nodeStructureList;
        }

        public static NodeStructure getNodeStructureWithName(NodeStructureList nodeStructureList, string name)
        {
            for (int i = 0; i < nodeStructureList.Count; i++)
            {
                if (nodeStructureList[i].name == name)
                {
                    return nodeStructureList[i];
                }
            }
            return null;
        }

        public static NodeStructureList mergeNodeStructureLists(NodeStructureList highList, NodeStructureList lowList)
        {
            NodeStructure theNode = null;
            int numberParents = highList.Count;
            for (int i = 0; i < lowList.Count; i++)
            {
                if (i < numberParents)
                {
                    //Checking top level nodes for matches
                    if (NodeStructureListManager.containsNodeStructure(highList, lowList[i]))
                    {
                        theNode = NodeStructureListManager.getNodeStructureWithName(highList, lowList[i].name);
                        NodeStructureListManager.removeNodeStructure(theNode, highList);
                        theNode.isExpanded = lowList[i].isExpanded;
                        theNode.isChecked = lowList[i].isChecked;
                        NodeStructureListManager.addNodeStructure(theNode, highList);
                    }
                    int numberChildren = highList[i].children.Count;
                    for (int a = 0; a < lowList[i].children.Count; a++)
                    {
                        if (a < numberChildren)
                        {
                            //Checking mid level nodes for matches
                            if (NodeStructureListManager.containsNodeStructure(highList[i].children, lowList[i].children[a]))
                            {
                                theNode = NodeStructureListManager.getNodeStructureWithName(highList[i].children, lowList[i].children[a].name);
                                NodeStructureListManager.removeNodeStructure(theNode, highList[i].children);
                                theNode.isExpanded = lowList[i].children[a].isExpanded;
                                theNode.isChecked = lowList[i].children[a].isChecked;
                                NodeStructureListManager.addNodeStructure(theNode, highList[i].children);
                            }
                            int numberFiles = highList[i].children[a].children.Count;
                            for (int b = 0; b < lowList[i].children[a].children.Count; b++)
                            {
                                if (b < numberFiles)
                                {
                                    //Checking bottom level nodes for matches
                                    if (NodeStructureListManager.containsNodeStructure(highList[i].children[a].children, lowList[i].children[a].children[b]))
                                    {
                                        theNode = NodeStructureListManager.getNodeStructureWithName(highList[i].children[a].children, lowList[i].children[a].children[b].name);
                                        NodeStructureListManager.removeNodeStructure(theNode, highList[i].children[a].children);
                                        theNode.isExpanded = lowList[i].children[a].children[b].isExpanded;
                                        theNode.isChecked = lowList[i].children[a].children[b].isChecked;
                                        NodeStructureListManager.addNodeStructure(theNode, highList[i].children[a].children);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return highList;
        }

        #endregion
    }
}