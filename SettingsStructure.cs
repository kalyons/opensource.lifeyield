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
using System.Xml.Linq;
using System.Net;
using System.Xml.Serialization;
using System.Xml;

namespace TestingSuiteApplication
{

    #region Classes

    public class SettingsStructure
    {
        
        #region InstanceVariables

        public ESettingsStructureType settingsStructureType;
        public string filePath;
        public DefaultSettingsStructure defaultSettingsStructure;
        public RequestHandlerSettingsStructure requestHandlerSettingsStructure;
        public string topTreeNodePath;

        #endregion

        #region Constructors

        public SettingsStructure()
        {
            settingsStructureType = (ESettingsStructureType)(-1);
        }

        #endregion

    }

    public class SettingsStructureManager
    {

        #region DownloadSave

        public static SettingsStructure downloadSettings(string settingsStructureFilePath, ESettingsStructureType settingsStructureType)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(settingsStructureFilePath);
            XmlAttributeOverrides over = new XmlAttributeOverrides();
            XmlAttributes att = new XmlAttributes();
            switch (settingsStructureType)
            {
                case ((ESettingsStructureType)(-1)):
                    {
                        var compressedStructureObjectXML = xmlDocument.OuterXml;
                        var compressedStructureObject = SettingsStructureManager.DeserializeXml<CompressedStructure>(compressedStructureObjectXML, "CompressedStructure", over);
                        DecompressedStructure decompressedStructure = new DecompressedStructure();
                        over = new XmlAttributeOverrides();
                        att = new XmlAttributes();
                        att.XmlIgnore = true;
                        att.XmlElements.Add(new XmlElementAttribute("topNode"));
                        over.Add(typeof(CompressedStructure), "topNode", att);
                        CompressionUtil.TryDecompress(compressedStructureObject.content, out decompressedStructure.content);
                        return SettingsStructureManager.DeserializeXml<SettingsStructure>(decompressedStructure.content, "SettingsStructure", over);
                    }
                case ((ESettingsStructureType)(1)):
                    {
                        var compressedStructureObjectXML = xmlDocument.OuterXml;
                        over = new XmlAttributeOverrides();
                        var compressedStructureObject = SettingsStructureManager.DeserializeXml<CompressedStructure>(compressedStructureObjectXML, "CompressedStructure", over);
                        DecompressedStructure decompressedStructure = new DecompressedStructure();
                        CompressionUtil.TryDecompress(compressedStructureObject.content, out decompressedStructure.content);
                        return (RequestHandlerSettingsStructure)SettingsStructureManager.DeserializeXml<RequestHandlerSettingsStructure>(decompressedStructure.content, "SettingsStructure", over);
                    }
            }
            return null;
        }

        public static void saveSettings(SettingsStructure settingsStructure)
        {
            var settingsStructureToSave = settingsStructure;
            var settingsStructureSaved = SettingsStructureManager.SerializeXml<SettingsStructure>(settingsStructureToSave, "");
            DecompressedStructure decompressedStructure = new DecompressedStructure();
            decompressedStructure.content = settingsStructureSaved;
            CompressedStructure compressedStructure = new CompressedStructure();
            compressedStructure.content = CompressionUtil.Compress(decompressedStructure.content);
            var compressedStructureSaved = SettingsStructureManager.SerializeXml<CompressedStructure>(compressedStructure, "");
            XDocument xDocument = XDocument.Parse(compressedStructureSaved);
            if (settingsStructure.GetType().FullName == "TestingSuiteApplication.RequestHandlerSettingsStructure")
            {
                //Save as request object.
                xDocument.Save(((RequestHandlerSettingsStructure)(settingsStructure)).requestHandlerSettingsStructureFileName);
            }
            else
            { 
                //Save as a regular object.
                xDocument.Save(settingsStructure.filePath);
            }
        }

        #endregion

        #region XMLSerializationDeserialization

        // Serializes a structure to xml
        public static String SerializeXml<T>(T obj, String xmlRootName)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T), new XmlRootAttribute(xmlRootName));
            XmlWriterSettings writerSettings = new XmlWriterSettings() { OmitXmlDeclaration = true };

            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
            ns.Add("", "");

            StringBuilder responseBodyText = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(responseBodyText, writerSettings))
            {
                serializer.Serialize(writer, obj, ns);
            }

            return responseBodyText.ToString();
        }

        // Deserializes xml back to appropriate structure
        public static T DeserializeXml<T>(String xml, String xmlRootName, XmlAttributeOverrides overrides)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), overrides, new Type[] { }, new XmlRootAttribute(xmlRootName), String.Empty);
            XmlReaderSettings bodySettings = new XmlReaderSettings() { IgnoreWhitespace = true, IgnoreComments = true, IgnoreProcessingInstructions = true};

            using (StringReader reader = new StringReader(xml))
            using (XmlReader body = XmlReader.Create(reader, bodySettings))
            {
                return (T)serializer.Deserialize(body);
            }
        }

        #endregion

        #region HelperMethods

        public static List<string> removeDuplicates(List<string> theList)
        {
            return theList.Distinct().Cast<string>().ToList();
        }

        #endregion

    }

    public class DefaultSettingsStructure : SettingsStructure
    {

        #region InstanceVariables

        public string defaultUsername = String.Empty;
        public string defaultURL = String.Empty;
        public string defaultDirectory = String.Empty;
        public string organization = String.Empty;
        public List<string> usernames = new List<string>();
        public List<string> URLs = new List<string>();
        public List<string> directories = new List<string>();

        #endregion

        #region Constructors

        public DefaultSettingsStructure()
        {
            settingsStructureType = (ESettingsStructureType)(0);
        }

        #endregion

    }

    public class RequestHandlerSettingsStructure : SettingsStructure
    {

        #region InstanceVariables

        public string requestHandlerSettingsStructureFileName = String.Empty;
        public NodeStructureList nodeStructureList = new NodeStructureList();
        public string configurationName = String.Empty;

        #endregion

        #region Constructors

        public RequestHandlerSettingsStructure()
        {
            settingsStructureType = (ESettingsStructureType)(1);
        }

        #endregion
    }

    public class CompressedStructure
    {
        public string content;
    }

    public class DecompressedStructure
    {
        public string content;
    }

    #endregion

    #region ENUM

    public enum ESettingsStructureType
    {
        ROOT = -1,
        DEFAULT = 0,
        REQUESTHANDLER = 1
    }

    #endregion

}
