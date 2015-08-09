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
using System.Xml.Serialization;

namespace TestingSuiteApplication
{
    class RequestHandlingManager
    {
        #region InstanceVariables

        public static ENodeStructureCheckedEntryPoint nodeStructureCheckedEntryPoint;

        #endregion

        #region UploadDownload

        public static NodeStructureList getNewRequests(NodeStructureList nodeStructureList, TreeView treeView, string rootDirectory, bool isFresh, int occurance, TreeNode treeNode)
        {
            try
            {
                if (isFresh)
                    nodeStructureList = new NodeStructureList();
                if (occurance == 1)
                    saveExpandedState(nodeStructureList, treeView);
                string[] nodeStructures = System.IO.Directory.GetDirectories(rootDirectory);
                NodeStructure nodeStructure = null, requestNodeStructure = null, nodeStructureFile = null;
                string[] files = null;
                int i = 0;
                foreach (string nodeStructureString in nodeStructures)
                {
                    nodeStructure = new NodeStructure();
                    nodeStructure.path = nodeStructureString;
                    nodeStructure.isParent = true;
                    NodeStructureManager.getName(nodeStructure, NodeStructureManager.getStructureType(nodeStructure));
                    if (!NodeStructureListManager.containsNodeStructure(nodeStructureList, nodeStructure))
                    {
                        NodeStructureListManager.addNodeStructure(nodeStructure, nodeStructureList);
                    }
                    else
                    {
                        nodeStructure = NodeStructureListManager.getNodeStructureWithName(nodeStructureList, nodeStructure.name);
                        nodeStructure.children = NodeStructureListManager.getNodeStructureWithName(nodeStructureList, nodeStructure.name).children;
                        NodeStructureManager.assignParentNames(nodeStructure);
                    }
                    string[] requestNodeStructures = System.IO.Directory.GetDirectories(nodeStructure.path + "\\Requests");
                    foreach (string requestNodeStructureString in requestNodeStructures)
                    {
                        requestNodeStructure = new NodeStructure();
                        requestNodeStructure.path = requestNodeStructureString;
                        requestNodeStructure.isChild = true;
                        requestNodeStructure.isParent = true;
                        NodeStructureManager.getName(requestNodeStructure, ENodeSructureType.BOTH);
                        if (!NodeStructureListManager.containsNodeStructure(nodeStructure.children, requestNodeStructure))
                        {
                            NodeStructureListManager.removeNodeStructure(nodeStructure, nodeStructureList);
                            NodeStructureManager.addChild(nodeStructure, requestNodeStructure);
                            NodeStructureListManager.addNodeStructure(nodeStructure, nodeStructureList);
                        }
                        else
                        {
                            requestNodeStructure = NodeStructureListManager.getNodeStructureWithName(nodeStructure.children, requestNodeStructure.name);
                            requestNodeStructure.children = NodeStructureListManager.getNodeStructureWithName(nodeStructure.children, requestNodeStructure.name).children;
                            NodeStructureManager.assignParentNames(requestNodeStructure);
                        }
                        files = System.IO.Directory.GetFiles(requestNodeStructure.path);
                        foreach (string file in files)
                        {
                            nodeStructureFile = new NodeStructure();
                            nodeStructureFile.path = file;
                            nodeStructureFile.isChild = true;
                            NodeStructureManager.getName(nodeStructureFile, NodeStructureManager.getStructureType(nodeStructureFile));
                            if (!NodeStructureListManager.containsNodeStructure(requestNodeStructure.children, nodeStructureFile))
                            {
                                NodeStructure node3 = requestNodeStructure;
                                NodeStructureListManager.removeNodeStructure(node3, nodeStructure.children);
                                NodeStructureManager.addChild(node3, nodeStructureFile);
                                NodeStructureManager.uncheck(nodeStructureList, node3, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                                NodeStructureListManager.addNodeStructure(node3, nodeStructure.children);
                                requestNodeStructure = node3;
                            }
                        }
                        for (int h = 0; h < requestNodeStructure.children.Count; h++)
                        {
                            if (!files.Contains(requestNodeStructure.children[h].path))
                            {
                                NodeStructureListManager.removeNodeStructure(requestNodeStructure.children[h], requestNodeStructure.children);
                                if (NodeStructureManager.allChildrenChecked(requestNodeStructure))
                                    NodeStructureManager.check(nodeStructureList, requestNodeStructure, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                            }
                        }
                        requestNodeStructure.children = NodeStructureListManager.alphabetize(requestNodeStructure.children);
                        requestNodeStructure.children = NodeStructureListManager.reindex(requestNodeStructure.children);
                        requestNodeStructure.children = NodeStructureListManager.removeDuplicateNodes(requestNodeStructure.children);
                    }
                    for (int x = 0; x < nodeStructure.children.Count; x++)
                    {
                        if (!requestNodeStructures.Contains(nodeStructure.children[x].path))
                            NodeStructureListManager.removeNodeStructure(nodeStructure.children[x], nodeStructure.children);
                    }
                    nodeStructureList[i].children = NodeStructureListManager.alphabetize(nodeStructureList[i].children);
                    nodeStructureList[i].children = NodeStructureListManager.reindex(nodeStructureList[i].children);
                    nodeStructureList[i].children = NodeStructureListManager.removeDuplicateNodes(nodeStructureList[i].children);
                    i++;
                }
                for (int k = 0; k < nodeStructureList.Count; k++)
                {
                    if (!nodeStructures.Contains(nodeStructureList[k].path))
                        NodeStructureListManager.removeNodeStructure(nodeStructureList[k], nodeStructureList);
                }
                nodeStructureList = NodeStructureListManager.alphabetize(nodeStructureList);
                nodeStructureList = NodeStructureListManager.reindex(nodeStructureList);
                nodeStructureList = NodeStructureListManager.removeDuplicateNodes(nodeStructureList);
                return nodeStructureList;
            }
            catch (Exception)
            {
                MessageBox.Show("Error loading files.  Please check directory formatting.");
                Form1.Logger.Error("Error loading files.  Please check directory formatting.");
                Application.Exit();
                return null;
            }
        }

        public static void updateRequests(NodeStructureList nodeStructureList, TreeView treeView, ENodeStructureCheckedEntryPoint nodeStructureCheckedEntryPoint, TreeNode treeNode, int occurance)
        {
            if (occurance == 1)
                saveExpandedState(nodeStructureList, treeView);
            NodeStructure nodeStructure = null, childNodeStructure = null, endNodeStructure = null;
            for (int i = 0; i < treeView.Nodes.Count; i++)
            {
                nodeStructure = (NodeStructure)(treeView.Nodes[i].Tag);
                if (differencesExist(nodeStructure, treeView.Nodes[i]) == (EDifferenceToMake)(0) || differencesExist(nodeStructure, treeView.Nodes[i]) == (EDifferenceToMake)(1))
                    synchronize(differencesExist(nodeStructure, treeView.Nodes[i]), nodeStructure, nodeStructureList, treeView, nodeStructureCheckedEntryPoint, treeNode, occurance);
                if (nodeStructure.isParent)
                {
                    for (int j = 0; j < treeView.Nodes[i].Nodes.Count; j++)
                    {
                        childNodeStructure = (NodeStructure)(treeView.Nodes[i].Nodes[j].Tag);
                        if (differencesExist(childNodeStructure, treeView.Nodes[i].Nodes[j]) == (EDifferenceToMake)(0) || differencesExist(childNodeStructure, treeView.Nodes[i].Nodes[j]) == (EDifferenceToMake)(1))
                            synchronize(differencesExist(childNodeStructure, treeView.Nodes[i].Nodes[j]), childNodeStructure, nodeStructureList, treeView, nodeStructureCheckedEntryPoint, treeNode, occurance);
                        if (childNodeStructure.isParent)
                        {
                            for (int k = 0; k < treeView.Nodes[i].Nodes[j].Nodes.Count; k++)
                            {
                                endNodeStructure = (NodeStructure)(treeView.Nodes[i].Nodes[j].Nodes[k].Tag);
                                if (differencesExist(endNodeStructure, treeView.Nodes[i].Nodes[j].Nodes[k]) == (EDifferenceToMake)(0) || differencesExist(endNodeStructure, treeView.Nodes[i].Nodes[j].Nodes[k]) == (EDifferenceToMake)(1))
                                    synchronize(differencesExist(endNodeStructure, treeView.Nodes[i].Nodes[j].Nodes[k]), endNodeStructure, nodeStructureList, treeView, nodeStructureCheckedEntryPoint, treeNode, occurance);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region TreeViewMethods

        public static void fillTreeView(NodeStructureList nodeStructureList, TreeView treeView, TreeNode treeNode, int occurance)
        {
            treeView.Nodes.Clear();
            treeView.BeginUpdate();
            int i = 0, j = 0;
            foreach (NodeStructure nodeStructure in nodeStructureList)
            {
                treeView.Nodes.Add(NodeStructureManager.toTreeNode(nodeStructure));
                if (nodeStructure.isExpanded)
                    treeView.Nodes[i].Expand();
                else
                    treeView.Nodes[i].Collapse(false);
                if (nodeStructure.isParent)
                {
                    foreach (NodeStructure childNodeStructure in nodeStructure.children)
                    {
                        treeView.Nodes[i].Nodes.Add(NodeStructureManager.toTreeNode(childNodeStructure));
                        if (childNodeStructure.isExpanded)
                            treeView.Nodes[i].Nodes[j].Expand();
                        else
                            treeView.Nodes[i].Nodes[j].Collapse(false);
                        if (childNodeStructure.isParent)
                        {
                            foreach (NodeStructure requestNodeStructure in childNodeStructure.children)
                            {
                                treeView.Nodes[i].Nodes[j].Nodes.Add(NodeStructureManager.toTreeNode(requestNodeStructure));
                            }
                        }
                        j++;
                    }
                }
                i++;
                j = 0;
            }
            expandProperNodes(nodeStructureList, treeView);
            if (occurance == 1)
            {
                NodeStructure childNodeStructure = (NodeStructure)(treeNode).Tag;
                if (childNodeStructure.isChild)
                {
                    if (childNodeStructure.isParent)
                    {
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(childNodeStructure, nodeStructureList);
                        treeView.TopNode = treeView.Nodes[parentNodeStructure.index].Nodes[childNodeStructure.index];
                    }
                    else
                    {
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(childNodeStructure, nodeStructureList);
                        NodeStructure theParent = NodeStructureManager.getParentNodeStructure(parentNodeStructure, nodeStructureList);
                        treeView.TopNode = treeView.Nodes[theParent.index].Nodes[parentNodeStructure.index].Nodes[childNodeStructure.index];
                    }
                }
                else
                    treeView.TopNode = treeView.Nodes[treeNode.Index];
            }
            else
            {
                treeView.TopNode = treeView.Nodes[0];
            }
            treeView.EndUpdate();
        }

        public static void expandProperNodes(NodeStructureList nodeStructureList, TreeView treeView)
        {
            //Precondition: nodeStructureList.Count (and all child counts) = treeView.Nodes.Count (and all child counts)
            for (int i = 0; i < nodeStructureList.Count; i++)
            {
                if (nodeStructureList[i].isExpanded)
                    treeView.Nodes[i].Expand();
                else
                    treeView.Nodes[i].Collapse(true);
                for (int j = 0; j < nodeStructureList[i].children.Count; j++)
                {
                    if (nodeStructureList[i].children[j].isExpanded)
                        treeView.Nodes[i].Nodes[j].Expand();
                    else
                        treeView.Nodes[i].Nodes[j].Collapse(true);
                    for (int k = 0; k < nodeStructureList[i].children[j].children.Count; k++)
                    {
                        if (nodeStructureList[i].children[j].children[k].isExpanded)
                            treeView.Nodes[i].Nodes[j].Nodes[k].Expand();
                        else
                            treeView.Nodes[i].Nodes[j].Nodes[k].Collapse(true);
                    }
                }
            }
        }

        #endregion

        #region HelperMethods

        public static EDifferenceToMake differencesExist(NodeStructure nodeStructure, TreeNode treeNode)
        {
            if (treeNode.Checked)
            {
                if (nodeStructure.isChecked == false)
                {
                    return (EDifferenceToMake)(0);
                }
                else
                    return (EDifferenceToMake)(2);
            }
            else if (!treeNode.Checked)
            {
                if (nodeStructure.isChecked == true)
                {
                    return (EDifferenceToMake)(1);
                }
                else
                    return (EDifferenceToMake)(2);
            }
            else
                return (EDifferenceToMake)(2);
        }

        public static void synchronize(EDifferenceToMake differenceToMake, NodeStructure nodeStructure, NodeStructureList nodeStructureList, TreeView treeView, ENodeStructureCheckedEntryPoint nodeStructureCheckedEntryPoint, TreeNode treeNode, int occurance)
        {
            switch (nodeStructureCheckedEntryPoint)
            {
                case (ENodeStructureCheckedEntryPoint)(0):
                    {
                        switch (differenceToMake)
                        {
                            case (EDifferenceToMake)(0):
                                {
                                    nodeStructureCheckedEntryPoint = ENodeStructureCheckedEntryPoint.USER;
                                    NodeStructureManager.check(nodeStructureList, nodeStructure, (ENodeStructureCheckedEntryPoint)(0));
                                    break;
                                }
                            case (EDifferenceToMake)(1):
                                {
                                    nodeStructureCheckedEntryPoint = ENodeStructureCheckedEntryPoint.USER;
                                    NodeStructureManager.uncheck(nodeStructureList, nodeStructure, (ENodeStructureCheckedEntryPoint)(0));
                                    break;
                                }
                        }
                        break;
                    }
                case (ENodeStructureCheckedEntryPoint)(1):
                    {
                        switch (differenceToMake)
                        {
                            case (EDifferenceToMake)(0):
                                {
                                    //nodeStructureCheckedEntryPoint = ENodeStructureCheckedEntryPoint.FORCED;
                                    NodeStructureManager.uncheck(nodeStructureList, nodeStructure, (ENodeStructureCheckedEntryPoint)(1));
                                    nodeStructureCheckedEntryPoint = ENodeStructureCheckedEntryPoint.USER;
                                    break;
                                }
                            case (EDifferenceToMake)(1):
                                {
                                    //nodeStructureCheckedEntryPoint = ENodeStructureCheckedEntryPoint.FORCED;
                                    NodeStructureManager.check(nodeStructureList, nodeStructure, (ENodeStructureCheckedEntryPoint)(1));
                                    nodeStructureCheckedEntryPoint = ENodeStructureCheckedEntryPoint.USER;
                                    break;
                                }

                        }
                        break;
                    }
            }
            fillTreeView(nodeStructureList, treeView, treeNode, occurance);
        }

        public static ENodeStructureCheckedEntryPoint getNodeStructureCheckedEntryPoint()
        {
            return nodeStructureCheckedEntryPoint;
        }

        public static void saveExpandedState(NodeStructureList nodeStructureList, TreeView treeView)
        {
            //Precondition: nodeStructureList.Count (and all child counts) = treeView.Nodes.Count (and all child counts)
            for (int i = 0; i < treeView.Nodes.Count; i++)
            {
                nodeStructureList[i].isExpanded = treeView.Nodes[i].IsExpanded;
                for (int j = 0; j < treeView.Nodes[i].Nodes.Count; j++)
                {
                    nodeStructureList[i].children[j].isExpanded = treeView.Nodes[i].Nodes[j].IsExpanded;
                    for (int k = 0; k < treeView.Nodes[i].Nodes[j].Nodes.Count; k++)
                    {
                        nodeStructureList[i].children[j].children[k].isExpanded = treeView.Nodes[i].Nodes[j].Nodes[k].IsExpanded;
                    }
                }
            }
        }

        #endregion
    }

    #region ENUM

    public enum ERequestHandlingState
    {
        SETUP = 0,
        RUN = 1,
        VIEWERRORS = 2
    }

    #endregion
}