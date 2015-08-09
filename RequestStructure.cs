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
using TestingSuiteApplication.ServiceReference;
using System.ServiceModel;
using System.Web;

namespace TestingSuiteApplication
{

    #region Classes

    public class RequestStructure
    {

        #region InstanceVariables

        public string requestName = String.Empty;
        public string requestFilePath = String.Empty;
        public string responseFilePath = String.Empty;
        public string resultFilePath = String.Empty;
        public string requestURL = String.Empty;
        public List<string> errors = new System.Collections.Generic.List<string>();
        public int index = 0;
        public ERequestStructureState requestStructureState;
        public RequestStructureList requestsStructureList = new RequestStructureList();

        #endregion

    }

    public class RequestStructureManager
    {

        #region StructureMethods

        public static event RequestStructureStateChangedEventHandler requestStructureStateChanged;

        public static RequestStructure raiseRequestStructureStateChanged(RequestStructureStateChangedArgs e)
        {
            RequestStructureStateChangedEventHandler handler = requestStructureStateChanged;
            if (handler != null)
            {
                handler(null, e);
            }
            return e.requestStructure;
        }

        public static ERequestStructureState RequestStructureManager_requestStructureStateChanged(object sender, RequestStructureStateChangedArgs e)
        {
            ERequestStructureState requestStructureState = ERequestStructureState.RUNNING;
            if (e != null)
            {
                requestStructureState = e.requestStructureState;
            }
            return requestStructureState;
        }

        public static RequestStructure returnRequestStructure(RequestStructure requestStructure)
        {
            return requestStructure;
        }

        public static void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Console.WriteLine(e.ProgressPercentage);
        }

        static void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                using (ServiceReference.XmlServiceFacadeClient client = GetClient(((RequestStructure)e.Argument).requestURL))
                {
                    RequestStructure requestStructure = (RequestStructure)e.Argument;
                    requestStructure.requestStructureState = ERequestStructureState.RUNNING;
                    XmlDocument requestXMLDocument = new XmlDocument();
                    requestXMLDocument.Load(requestStructure.requestFilePath);
                    string requestString = requestXMLDocument.OuterXml;
                    XmlDocument responseXMLDocument = new XmlDocument();
                    responseXMLDocument.Load(requestStructure.responseFilePath);
                    string responseString = responseXMLDocument.OuterXml;
                    string compressedResponseString = CompressionUtil.Compress(responseString);
                    string compressedRequest = CompressionUtil.Compress(requestString);
                    string resultString = null;
                    resultString = client.Process(compressedRequest);
                    String decompressedResultString;
                    CompressionUtil.TryDecompress(resultString, out decompressedResultString);
                    if (decompressedResultString.Contains("AuthToken"))
                    {
                        int startIndex = decompressedResultString.IndexOf("<AuthToken>");
                        int endIndex = decompressedResultString.IndexOf("</AuthToken>") + 11;
                        decompressedResultString = decompressedResultString.Replace(decompressedResultString.Substring(startIndex, (endIndex - startIndex + 1)), "<AuthToken />");
                    }
                    if (responseString == decompressedResultString)
                        requestStructure.requestStructureState = ERequestStructureState.COMPLETED;
                    else
                    {

                        Form1.findDifferences(responseString, decompressedResultString, requestStructure);
                        requestStructure.requestStructureState = ERequestStructureState.ERROR;
                    }
                    e.Result = requestStructure;
                }
            }
        }

        static void client_ProcessCompleted(object sender, ServiceReference.ProcessCompletedEventArgs e)
        {
            string resultString = e.Result;
            string decompressedResultString = null;
            CompressionUtil.TryDecompress(resultString, out decompressedResultString);
        }

        #endregion

        #region HelperMethods

        public static string getResponseFilePath(RequestStructure requestStructure)
        {
            FileInfo fileInfo = new FileInfo(requestStructure.requestFilePath);
            DirectoryInfo directoryInfo = new DirectoryInfo(fileInfo.DirectoryName);
            DirectoryInfo parentDirectoryInfo = directoryInfo.Parent;
            DirectoryInfo theParentDirectory = parentDirectoryInfo.Parent;
            string y = theParentDirectory.FullName + "\\" + parentDirectoryInfo.Name.Replace("Request", "Response") + "\\" + directoryInfo.Name.Replace("Request", "Response") + "\\" + fileInfo.Name.Replace("Request", "Response");
            return y;
        }

        private static TestingSuiteApplication.ServiceReference.XmlServiceFacadeClient GetClient(string URI)
        {
            if (URI.StartsWith("https", StringComparison.CurrentCultureIgnoreCase))
                return new XmlServiceFacadeClient(new BasicHttpBinding(BasicHttpSecurityMode.Transport) { MaxBufferSize = 2147483647, MaxReceivedMessageSize = 2147483647 }, new EndpointAddress(URI));
            else
                return new XmlServiceFacadeClient(new BasicHttpBinding(BasicHttpSecurityMode.None) { MaxBufferSize = 2147483647, MaxReceivedMessageSize = 2147483647 }, new EndpointAddress(URI));
        }

        public static string returnErrorString(RequestStructure requestStructure, bool addLines)
        {
            string lineToWrite = "", theError = null;
            for (int j = 0; j < requestStructure.errors.Count; j++)
            {
                theError = requestStructure.errors[j];
                lineToWrite += theError;
                if (j != requestStructure.errors.Count - 1)
                    if (addLines)
                        lineToWrite += "\n";
                    else
                        lineToWrite += " / ";
            }
            return lineToWrite;
        }

        #endregion

    }

    public class RequestStructureList : List<RequestStructure>
    {

        #region InstanceVariable

        public string URL = String.Empty;
        public int currentIndex = 0;
        public int currentSection = 0;
        public string currentName = String.Empty;

        #endregion

    }

    public class RequestStructureListManager
    {

        #region ListPopulation

        public static RequestStructureList populateRequestStructureList(RequestStructureList requestStructureList, NodeStructureList nodeStructureList)
        {
            RequestStructure requestStructure = null;
            requestStructureList = new RequestStructureList();
            foreach (NodeStructure nodeStructure in nodeStructureList)
            {
                requestStructure = new RequestStructure();
                requestStructure.requestName = nodeStructure.name;
                requestStructure.requestFilePath = nodeStructure.path;
                requestStructure.responseFilePath = RequestStructureManager.getResponseFilePath(requestStructure);
                requestStructure.requestsStructureList = requestStructureList;
                addRequestStructure(requestStructure, requestStructureList);
            }
            return requestStructureList;
        }

        #endregion

        #region AdditionRemoval

        public static void addRequestStructure(RequestStructure requestStructure, RequestStructureList requestStructureList)
        {
            requestStructureList.Add(requestStructure);
            requestStructureList = alphabetize(requestStructureList);
            reindex(requestStructureList);
        }

        public static void removeRequestStructure(RequestStructure requestStructure, RequestStructureList requestStructureList)
        {
            requestStructure.requestsStructureList = null;
            requestStructureList.Remove(requestStructure);
            requestStructureList = alphabetize(requestStructureList);
            reindex(requestStructureList);
        }

        #endregion

        #region SortingIndexing

        public static RequestStructureList alphabetize(RequestStructureList requestStructureList)
        {
            requestStructureList.Sort((s1, s2) => s1.requestName.CompareTo(s2.requestName));
            return requestStructureList;
        }

        public static void reindex(RequestStructureList requestStructureList)
        {
            for (int i = 0; i < requestStructureList.Count; i++)
            {
                requestStructureList[i].index = i;
            }
        }

        #endregion
    }

    #endregion

    #region ENUM

    public enum ERequestStructureState
    {
        WAITING = 0,
        RUNNING = 1,
        COMPLETED = 2,
        ERROR = 3
    }

    #endregion

    #region EventHandling

    public class RequestStructureStateChangedArgs : EventArgs
    {
        public RequestStructure requestStructure = new RequestStructure();
        public ERequestStructureState requestStructureState;

        public RequestStructureStateChangedArgs(RequestStructure theRequestStructure, ERequestStructureState theRequestStructureState)
        {
            requestStructure = theRequestStructure;
            requestStructureState = theRequestStructureState;
        }
    }

    public delegate ERequestStructureState RequestStructureStateChangedEventHandler(object sender, RequestStructureStateChangedArgs e);

       #endregion

}
