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
    class NodeStructureManager
    {
        #region FileAdditionRemoval

        public static void addPath(NodeStructure nodeStructure, string path)
        {
            nodeStructure.path = path;
        }

        public static void removePath(NodeStructure nodeStructure)
        {
            nodeStructure.path = null;
        }

        #endregion

        #region FamilyRelations

        public static void addChild(NodeStructure parentNodeStructure, NodeStructure childNodeStructure)
        {
            NodeStructureListManager.addNodeStructure(childNodeStructure, parentNodeStructure.children);
            childNodeStructure.parentName = parentNodeStructure.name;
        }

        #endregion

        #region CheckUncheck

        public static void check(NodeStructureList nodeStructureList, NodeStructure nodeStructure, ENodeStructureCheckedEntryPoint nodeStructureCheckedEntryPoint)
        {
            if (nodeStructure.isChild)
            {
                NodeStructure parent = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                NodeStructureListManager.removeNodeStructure(nodeStructure, parent.children);
                nodeStructure.isChecked = true;
                NodeStructureListManager.addNodeStructure(nodeStructure, parent.children);
                if (nodeStructure.isParent)
                {
                    if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYPARENT)
                    {
                        for (int i = 0; i < nodeStructure.children.Count; i++)
                        {
                            check(nodeStructureList, nodeStructure.children[i], ENodeStructureCheckedEntryPoint.FORCEDBYPARENT);
                        }
                    }
                    else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYCHILD)
                    {
                        //Maybe check parent.
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                        if (NodeStructureManager.allChildrenChecked(parentNodeStructure))
                            check(nodeStructureList, parentNodeStructure, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                    }
                    else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.USER) {
                        //Do both of the above.
                        for (int i = 0; i < nodeStructure.children.Count; i++)
                        {
                            check(nodeStructureList, nodeStructure.children[i], ENodeStructureCheckedEntryPoint.FORCEDBYPARENT);
                        }
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                        if (NodeStructureManager.allChildrenChecked(parentNodeStructure))
                            check(nodeStructureList, parentNodeStructure, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                    }
                }
                else
                {
                    //Just a child that was selected.
                    if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.USER)
                    {
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                        if (NodeStructureManager.allChildrenChecked(parentNodeStructure))
                            check(nodeStructureList, parentNodeStructure, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                    
                    }
                    else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYPARENT)
                    {
                        //Do nothing...last level.
                    }
                }
            }
            else if (nodeStructure.isParent)
            {
                nodeStructure.isChecked = true;
                //Just a parent - top level
                if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYPARENT)
                {
                    //Impossible.
                }
                else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYCHILD)
                {
                    //Done - no more.
                }
                else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.USER)
                {
                    for (int i = 0; i < nodeStructure.children.Count; i++)
                    {
                        check(nodeStructureList, nodeStructure.children[i], ENodeStructureCheckedEntryPoint.FORCEDBYPARENT);
                    }
                }
            }
        }

        public static void uncheck(NodeStructureList nodeStructureList, NodeStructure nodeStructure, ENodeStructureCheckedEntryPoint nodeStructureCheckedEntryPoint)
        {
            if (nodeStructure.isChild)
            {
                NodeStructure parent = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                NodeStructureListManager.removeNodeStructure(nodeStructure, parent.children);
                nodeStructure.isChecked = false;
                NodeStructureListManager.addNodeStructure(nodeStructure, parent.children);
                if (nodeStructure.isParent)
                {
                    if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYPARENT)
                    {
                        for (int i = 0; i < nodeStructure.children.Count; i++)
                        {
                            uncheck(nodeStructureList, nodeStructure.children[i], ENodeStructureCheckedEntryPoint.FORCEDBYPARENT);
                        }
                    }
                    else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYCHILD)
                    {
                        //Uncheck parent
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                        uncheck(nodeStructureList, parentNodeStructure, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                    }
                    else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.USER)
                    {
                        //Do both of the above.
                        for (int i = 0; i < nodeStructure.children.Count; i++)
                        {
                            uncheck(nodeStructureList, nodeStructure.children[i], ENodeStructureCheckedEntryPoint.FORCEDBYPARENT);
                        }
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                        uncheck(nodeStructureList, parentNodeStructure, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                    }
                }
                else
                {
                    //Just a child was selected
                    if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.USER)
                    {
                        //Uncheck parent
                        NodeStructure parentNodeStructure = NodeStructureManager.getParentNodeStructure(nodeStructure, nodeStructureList);
                        uncheck(nodeStructureList, parentNodeStructure, ENodeStructureCheckedEntryPoint.FORCEDBYCHILD);
                    }
                    else if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.FORCEDBYPARENT)
                    {
                        //Do nothing...last level.
                    }
                }
            }
            else if (nodeStructure.isParent)
            {
                nodeStructure.isChecked = false;
                //Just a parent 
                //top level.
                //FORCEDBYPARENT = IMPOSSIBLE
                //FORCEDBYCHILD = DONE
                if (nodeStructureCheckedEntryPoint == ENodeStructureCheckedEntryPoint.USER)
                {
                    //Uncheck children and thus all below.
                    for (int i = 0; i < nodeStructure.children.Count; i++)
                    {
                        uncheck(nodeStructureList, nodeStructure.children[i], ENodeStructureCheckedEntryPoint.FORCEDBYPARENT);
                    }
                }
            }
        }

        #endregion

        #region HelperMethods

        public static bool allChildrenChecked(NodeStructure parentNodeStructure)
        {
            //Precondition: parentNodeStructure is a parent
            foreach (NodeStructure childNodeStructure in parentNodeStructure.children) {
                if (!childNodeStructure.isChecked)
                    return false;
            }
            return true;
        }

        public static TreeNode toTreeNode(NodeStructure nodeStructure)
        {
            TreeNode treeNode = new TreeNode();
            treeNode.Name = nodeStructure.name;
            treeNode.Text = nodeStructure.name;
            treeNode.Tag = nodeStructure;
            treeNode.Checked = nodeStructure.isChecked;
            return treeNode;
        }

        public static void getName(NodeStructure nodeStructure, ENodeSructureType nodeStructureType)
        {
            switch (nodeStructureType)
            {
                case (ENodeSructureType)0:
                { // Parent
                    System.IO.DirectoryInfo directoryInfo = new DirectoryInfo(nodeStructure.path);
                    nodeStructure.name = directoryInfo.Name;
                    break;
                }
                case (ENodeSructureType)1:
                { // Child
                    nodeStructure.name = Path.GetFileNameWithoutExtension(nodeStructure.path);
                    break;
                }
                case (ENodeSructureType)2:
                {
                    // Both
                    DirectoryInfo directoryInfo = new DirectoryInfo(nodeStructure.path);
                    nodeStructure.name = directoryInfo.Name;
                    break;
                }
            }
        }

        public static NodeStructure getParentNodeStructure(NodeStructure nodeStructure, NodeStructureList largeList)
        {
            for (int i = 0; i < largeList.Count; i++)
            {
                if (largeList[i].name == nodeStructure.parentName)
                    return largeList[i];
            }
            for (int j = 0; j < largeList.Count; j++)
            {
                for (int k = 0; k < largeList[j].children.Count; k++)
                {
                    if (largeList[j].children[k].name == nodeStructure.parentName)
                        return largeList[j].children[k];
                }
            }
            return null;
        }

        public static ENodeSructureType getStructureType(NodeStructure nodeStructure)
        {
            if (nodeStructure.isParent && nodeStructure.isChild)
            {
                return (ENodeSructureType)2;
            }
            else if (nodeStructure.isParent)
            {
                return (ENodeSructureType)0;
            }
            else if (nodeStructure.isChild)
            {
                return (ENodeSructureType)1;
            }
            return (ENodeSructureType)(-1);
        }

        public static void assignParentNames(NodeStructure parentNodeStructure)
        {
            for (int i = 0; i < parentNodeStructure.children.Count; i++)
            {
                parentNodeStructure.children[i].parentName = parentNodeStructure.name;
            }
        }

        #endregion
    }
}
