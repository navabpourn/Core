using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Modules.Vim.UI.Models;
using BExIS.Modules.Vim.UI.Helper;
using BExIS.Security.Services.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Entities.Party;
using BExIS.Dlm.Services.Party;
using BExIS.Security.Services.Subjects;
using BExIS.Security.Services.Objects;
using BExIS.Modules.Rpm.UI.Models;
using System.Data;
using System.Xml;

namespace BExIS.Modules.Vim.UI.Controllers
{

    public class DQController : Controller
    {
        public class datasetDescriptionLength
        {
            public int minDescriptionLength;
            public int currentDescriptionLength;
            public int maxDescriptionLength;
        }
        public class dataStrDescriptionLength
        {
            public int minDescriptionLength;
            public int currentDescriptionLength;
            public int maxDescriptionLength;
        }
        public class dataStrUsage
        {
            public int minDataStrUsage;
            public int currentDataStrUsage;
            public int maxDataStrUsage;
        }

        public class datasetTotalSize
        {
            public int minTotalSize;
            public int currentTotalSize;
            public int maxTotalSize;
        }

        public class performer
        {
            public string performerName;
            public List<long> DatasetIds;
            public int performerRate;
        }
        public class performersActivity
        {
            public int minActivity;
            public int maxActivity;
        }

        public class metadataComplition
        {
            public int totalFields;
            public int requiredFields;
            public int minRate;
            public int maxRate;
        }
        public class varVariable
        {
            public string varLabel;
            public string varDescription;
            public string varType;
            public int varUsage;
            public string min = "";
            public string max = "";
        }


        // GET: DQ
        public ActionResult ShowDQ(long datasetId, long versionId)
        {
            DQModels dqModel = new DQModels();
            Dictionary<string, string> datasetInfo = new Dictionary<string, string>();
            //List<string> performers = new List<string>();
            List<performer> performers = new List<performer>();
            List<varVariable> varVariables = new List<varVariable>();
            Dictionary<string, double> datasetSize = new Dictionary<string, double>();
            //--------

            DatasetManager dm = new DatasetManager();
            DataStructureManager dsm = new DataStructureManager();
            EntityPermissionManager entityPermissionManager = new EntityPermissionManager();
            PartyManager pm = new PartyManager();

            UserManager um = new UserManager();
            //DataContainerManager dataContainerManager = new DataContainerManager();
            //DataAttributeManagerModel daModel = new DataAttributeManagerModel();

            //XmlDocument metadata = new XmlDocument();
            //long metadataStructureId = -1;
            DatasetVersion dsv = new DatasetVersion();
            //CURRENT DATASET VERSION
            //EntityManager entityManager = new EntityManager();

            //////////////////////////////////////////////////////////////////////////
            DatasetVersion currentDatasetVersion = dm.GetDatasetVersion(versionId); //Current dataset version
            DataStructure currentDataStr = dsm.AllTypesDataStructureRepo.Get(currentDatasetVersion.Dataset.DataStructure.Id);            
            
            //Find the dataset Type
            string currentDatasetType = "file";
            if (currentDataStr.Self.GetType() == typeof(StructuredDataStructure)) { currentDatasetType = "tabular";  }
            dqModel.type = currentDatasetType;


            List<long> datasetIds = dm.GetDatasetLatestIds();
            List<int> metadataRates = new List<int>();
            List<int> dsDescLength = new List<int>();
            List<int> dstrDescLength = new List<int>();
            List<int> dstrUsage = new List<int>();
            List<int> datasetTotalSize = new List<int>();

            foreach (long Id in datasetIds)
            {
                if (dm.IsDatasetCheckedIn(Id))
                {
                    // get last dataset versions
                    DatasetVersion datasetVersion = dm.GetDatasetVersions(Id).Last();
                    DataStructure dataStr = dsm.AllTypesDataStructureRepo.Get(datasetVersion.Dataset.DataStructure.Id);
                    metadataRates.Add(GetMetadataRate(datasetVersion));
                    dsDescLength.Add(datasetVersion.Description.Length);
                    dstrDescLength.Add(datasetVersion.Dataset.DataStructure.Description.Length);
                    dstrUsage.Add(dataStr.Datasets.Count());
                    datasetTotalSize.Add(GetDatasetTotalSize(Id, datasetVersion, dataStr, currentDatasetType)); //return null or number                    
                }
            }
            //Add information about min and max metadata complition
            dqModel.metadataComplition.minRate = metadataRates.Min();
            dqModel.metadataComplition.maxRate = metadataRates.Max();

            //Add information about min and max dataset description length
            dqModel.datasetDescriptionLength.minDescriptionLength = dsDescLength.Min();
            dqModel.datasetDescriptionLength.maxDescriptionLength = dsDescLength.Max();

            //Add information about min and max data structure description length
            dqModel.dataStrDescriptionLength.minDescriptionLength = dstrDescLength.Min();
            dqModel.dataStrDescriptionLength.maxDescriptionLength = dstrDescLength.Max();
            
            //Add information about min and max data structure usage
            dqModel.dataStrUsage.minDataStrUsage = dstrUsage.Min();
            dqModel.dataStrUsage.maxDataStrUsage = dstrUsage.Max();

            //Add information about dataset total size
            dqModel.datasetTotalSize.minTotalSize = datasetTotalSize.Min();
            dqModel.datasetTotalSize.maxTotalSize = datasetTotalSize.Max();

            //CURRENT DATASET VERSION
            dqModel.metadataComplition.totalFields = GetMetadataRate(currentDatasetVersion); //current dataset version: metadata rate
            dqModel.metadataComplition.requiredFields = 100; //Need to calculate: metadataStructureId = dsv.Dataset.MetadataStructure.Id;
            dqModel.datasetDescriptionLength.currentDescriptionLength = currentDatasetVersion.Description.Length; // Current dataset vesion: dataset description length
            dqModel.dataStrDescriptionLength.currentDescriptionLength = currentDatasetVersion.Dataset.DataStructure.Description.Length; // Current dataset version: data structure description length
            dqModel.dataStrUsage.currentDataStrUsage = currentDataStr.Datasets.Count();
            dqModel.datasetTotalSize.currentTotalSize = GetDatasetTotalSize(datasetId, currentDatasetVersion, currentDataStr, currentDatasetType);
            /////////////////////////////////////////////////////////////////////////

            #region performers
            List<int> activities = new List<int>();
            foreach (var user in um.Users)
            {
                activities.Add(FindDatasetsFromPerformerUsername(dm, um, user.Name).Count());
            }
            dqModel.performersActivity.minActivity = activities.Min();
            dqModel.performersActivity.maxActivity = activities.Max();

            List<string> performerUsernames = FindDatasetPerformers(dm, datasetId, versionId); //Find performers of the current dataset.
            foreach (var username in performerUsernames) //foreach performer of the current dataset
            {
                performer p = new performer();
                p.performerName = FindPerformerNameFromUsername(um, username);
                List<long> pfIds = FindDatasetsFromPerformerUsername(dm, um, username); //Find all datasets in wich the username is involved.
                p.DatasetIds = pfIds;
                p.performerRate = (100 * p.DatasetIds.Count()) / (activities.Max() - activities.Min()); //A number between 0-100
                performers.Add(p);
            }
            dqModel.performers = performers;
            #endregion

            #region TABULAR FORMAT DATASET                
            if (currentDatasetType == "tabular")
            {
                StructuredDataStructure sds = dsm.StructuredDataStructureRepo.Get(currentDatasetVersion.Dataset.DataStructure.Id);
                    
                var variables = sds.Variables;
                dqModel.ColumnRowNumbers.Add(variables.Count()); //number of columns
                long rowCount = dm.RowCount(datasetId, null);
                dqModel.ColumnRowNumbers.Add(Convert.ToInt32(rowCount)); // number of rows

                DataTable table = null;
                if (rowCount > 0) table = dm.GetLatestDatasetVersionTuples(datasetId, null, null, null, 0, (int)rowCount);
                DataRowCollection dataRows = table.Rows;

                int columnNumber = 3; //First four columns are added from system.
                foreach (var variable in variables)
                {
                    columnNumber += 1;
                    try
                    {
                        varVariable varV = new varVariable();
                        varV.varLabel = variable.Label; // variable name
                        varV.varDescription = variable.Description;
                        varV.varUsage = variable.DataAttribute.UsagesAsVariable.Count(); //How many other data structures are using the same variable template
                        varV.varType = variable.DataAttribute.DataType.SystemType; // What is the system type?
                        // Find data values of a variable
                        List<string> values = new List<string>();
                        foreach (DataRow row in dataRows)
                        {
                            var value = row.ItemArray[columnNumber];//.ToString();
                            values.Add(value.ToString());
                        }

                        if (varV.varType != "string")
                        {
                            varV.min = values.Min().ToString();
                            varV.max = values.Max().ToString();
                        }
                        else if (varV.varType == "String")
                        {
                            List<string> dataFrequency = FindMostFrequenceValue(values);
                            varV.max = dataFrequency[0];
                            varV.min = dataFrequency[1];
                        }

                        varVariables.Add(varV);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                    }
                    dqModel.varVariables = varVariables;                    
                }
                #endregion

                #region file format dataset
                // File data structure
                else
                {
                    //dqModel.datasetSize.Add("", 0);
                }
                #endregion
                           
            return PartialView(dqModel);
        }

        /// <summary>
        /// Find the most frequence value in a list of data values
        /// </summary>
        /// <param name="values">A list of data values</param>
        /// <returns>A list of two elements</returns>
        /// <returns>If most frequent finds, 0:value, 1:population</returns>
        /// <returns>If most population is 1, 0:"unique values", 1:numbers of uique values </returns>
        private List<string> FindMostFrequenceValue(List<string> values)
        {
            List<string> mostFrequency = new List<string>();

            var counts = new Dictionary<string, int>();
            int population = 0;
            foreach (var value in values)
            {
                int count;
                counts.TryGetValue(value, out count);
                count++;
                //Automatically replaces the entry if it exists;
                //no need to use 'Contains'
                counts[value] = count;
            }

            string mostCommonValue = null;
            foreach (var pair in counts)
            {
                if (pair.Value > population)
                {
                    population = pair.Value;
                    mostCommonValue = pair.Key;
                }
            }
            if (population == 1)
            {
                mostFrequency.Add("unique values");
                mostFrequency.Add(values.Count().ToString());
            }
            else
            {
                mostFrequency.Add(mostCommonValue);
                mostFrequency.Add(population.ToString());
            }           
            return (mostFrequency);
        }

        private int GetDatasetTotalSize(long datasetId, DatasetVersion datasetVersion, DataStructure dataStr, string currentDatasetType)
        {
            int size = -1; 
            string datasetType = "file";            
            if (dataStr.Self.GetType() == typeof(StructuredDataStructure)) 
            { 
                datasetType = "tabular";                
            }
            
            if (datasetType == currentDatasetType && datasetType == "tabular")
            {
                size = GetDatasetTotalSizeTabular(datasetId, datasetVersion, dataStr);
            }
            if (datasetType == currentDatasetType && datasetType == "file")
            {
                size = 1; // GetDatasetTotalSizeFile();
            }

            return (size);

        }

        private int GetDatasetTotalSizeTabular(long datasetId, DatasetVersion datasetVersion, DataStructure dataSt)
        {
            int size = -1;
           
            DataStructureManager dsm = new DataStructureManager();
            StructuredDataStructure sds = dsm.StructuredDataStructureRepo.Get(datasetVersion.Dataset.DataStructure.Id);
            var variables = sds.Variables;
            int numberOfColumns = variables.Count();
            DatasetManager dm = new DatasetManager();
            int numberOfRows = Convert.ToInt32(dm.RowCount(datasetId, null));
            if (numberOfRows < 0) { numberOfRows = 0;  }

            size = numberOfColumns * numberOfRows;
            return (size);

        }

        /// <summary>
        /// calculate the percentage of a metadata completeness.
        /// </summary>
        /// <param name="dsv">dataset version</param>
        /// <returns>rate: percentage of the metadata completeness</returns>
        private int GetMetadataRate(DatasetVersion dsv)
        {
            XmlDocument metadata = dsv.Metadata;
            string xmlFrag = metadata.OuterXml;
            List<int> metaInfo = GetMetaInfoArray(xmlFrag);
            int rate = (metaInfo[1] * 100) / metaInfo[0]; //percentage of all metadata fields contains information
            return (rate);
        }

        private List<int> GetMetadataComplitionRates(DatasetManager dm, int percent)
        {
            List<int> minMaxRate = new List<int>();
            List<long> datasetIds = dm.GetDatasetLatestIds();
            List<int> ratings = new List<int>();
            foreach (long datasetId in datasetIds)
            {
                if (dm.IsDatasetCheckedIn(datasetId))
                {
                    // get all dataset versions
                    var dsv = dm.GetDatasetVersions(datasetId).Last();
                    XmlDocument metadata = dsv.Metadata;
                    string xmlFrag = metadata.OuterXml;
                    List<int> metaInfo = GetMetaInfoArray(xmlFrag);
                    int rate = (metaInfo[1] * 100) / metaInfo[0]; //percentage of all metadata fields contains information
                    ratings.Add(rate);
                }
            }
            int minRate = ratings.Min();
            int maxRate = ratings.Max();

            minMaxRate.Add(minRate);
            minMaxRate.Add(maxRate);
            return (minMaxRate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlFrag">A string of xml reader</param>
        /// <returns>A list of four elements</returns>
        /// <returns>element1: Number of all fields of the metadata</returns>
        /// <returns>element2: Number of all fields contains information</returns>
        /// <returns>element3: Number of all required fields of the metadata</returns>
        /// <returns>element4: Number of all required fields contains information</returns>
        private List<int> GetMetaInfoArray(string xmlFrag)
        {
            List<int> metaInfo = new List<int>();
            NameTable nt = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            // Create the XmlParserContext.
            XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);
            // Create the reader.
            XmlTextReader reader = new XmlTextReader(xmlFrag, XmlNodeType.Element, context);

            int countMetaAttr = 0;
            int countMetaComplition = 0;

            // Parse the XML and display each node.
            while (reader.Read())
            {
                //XmlTextReader myReader = reader;
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.HasAttributes && reader.GetAttribute("type") == "MetadataAttribute")
                    {
                        countMetaAttr += 1;
                        reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            string text = reader.Value;
                            countMetaComplition += 1;
                        }
                    }
                }
            }

            // Close the reader.
            reader.Close();
            metaInfo.Add(countMetaAttr);
            metaInfo.Add(countMetaComplition);
            metaInfo.Add(100);
            metaInfo.Add(100);
            return (metaInfo);
        }

        public ActionResult ShowDatasetList(string datasetIds, string performerName)
        {
            DatasetListModel dsModel = new DatasetListModel();
            return View(dsModel);
        }

        /// <summary>
        /// This funcion finds all performers of a dataset.
        /// </summary>
        /// <param name="dsvs">A list of dataset versions of a dataset.</param>
        /// <param name="versionId">The current version Id of a dataset. </param>
        /// <returns>A list of performers</returns>
        private List<string> FindDatasetPerformers(DatasetManager dm, long datasetId, long versionId)
        {
            var dsvs = dm.GetDatasetVersions(datasetId);
            string performer;
            List<string> performerUsernames = new List<string>();
            foreach (var d in dsvs)
            {
                if (d.Id <= versionId)
                {
                    performer = d.ModificationInfo.Performer;
                    if (performer != null && !performerUsernames.Contains(performer))
                    {
                        performerUsernames.Add(performer);
                    }
                }
            }
            return (performerUsernames);
        }

        /// <summary>
        /// This funcion finds all datasets that a performer is involved.
        /// </summary>
        /// <param name="dm">A list of all datasets</param>
        /// <param name="userName">The performer's username</param>
        /// <returns>A list of dataset IDs</returns>
        private List<long> FindDatasetsFromPerformerUsername(DatasetManager dm, UserManager um, string username)
        {
            List<long> datasetIds = dm.GetDatasetLatestIds();
            List<long> Ids = new List<long>();
            foreach(long datasetId in datasetIds)
            {
                List<string> names = FindDatasetPerformers(dm, datasetId, dm.GetDatasetLatestVersionId(datasetId));
                if (names.Contains(username)){
                    Ids.Add(datasetId);
                }
            }            
            return (Ids);
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Find the full name of a performer from the username
        /// </summary>
        /// <param name="um"></param>
        /// <param name="userName"></param>
        /// <returns>The full name</returns>
        private string FindPerformerNameFromUsername(UserManager um, string userName)
        {
            string fullName = ""; // = dm.Select(v => v.CreationInfo.Performer).ToList();
            try
            {
                foreach(var user in um.Users)
                {
                        if(user.Name == userName)
                        {
                            fullName = user.FullName;
                        }
                }            
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return (fullName); 
        }

        
    }
}