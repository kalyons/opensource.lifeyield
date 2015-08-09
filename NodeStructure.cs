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
    public class NodeStructure
    {
        #region InstanceVariables

        private bool _isParent;
        public bool _isChild;
        [XmlIgnore()]
        public string parentName;
        public NodeStructureList children;
        public bool isChecked;
        public int level;
        public int index;
        public string name;
        public string path;
        public bool isExpanded;

        #endregion

        #region Constructors

        public NodeStructure()
        {
            parentName = String.Empty;
            children = new NodeStructureList();
            isChecked = false;
            level = 0;
            index = 0;
            name = null;
            path = null;
            isExpanded = true;
        }

        public NodeStructure(string theName)
        {
            name = theName;
        }

        #endregion

        #region GetterSetterMethods

        public bool isParent { get { return _isParent; } set { _isParent = value; } }

        public bool isChild { get { return _isChild; } set { _isChild = value; } }

        #endregion
    }
}
