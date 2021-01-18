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
using IDIV.Modules.Mmm.UI.Models;


namespace BExIS.Modules.Vim.UI.Controllers
{

    public class DQController : Controller
    {
        public class datasetInformation{
            public string type;
            public long datasetId;
            public int isPublic;
            public int metadataComplition;
            public int descriptionLength;
            public int structureDescriptionLength;
            public int structureUsage;
            public int datasetSizeTabular;            
            public int columnNumber;
            public int rowNumber;
            public double datasetSizeFile; 
            public int fileNumber;
            public List<int> performersRate = new List<int>();
        }
        public class datasetDescriptionLength
        {
            public int minDescriptionLength;
            public int currentDescriptionLength;
            public int maxDescriptionLength;
            public double medianDescriptionLength;
        }
        public class dataStrDescriptionLength
        {
            public int minDescriptionLength;
            public int currentDescriptionLength;
            public int maxDescriptionLength;
            public double medianDescriptionLength;
        }
        public class dataStrUsage
        {
            public int minDataStrUsage;
            public int currentDataStrUsage;
            public int maxDataStrUsage;
            public double medianDataStrUsage;
        }

        public class datasetTotalSize
        {
            public int minSizeTabular = -1;
            public int maxSizeTabular = -1;
            public double medianSizeTabular = -1;
            public double minSizeFile = -1;
            public double maxSizeFile = -1;
            public double medianSizeFile = -1; 
            public double currentTotalSize = -1;
        }

        public class datasetRowNumber
        {
            public int minRowNumber;
            public int currentRowNumber=-1;
            public int maxRowNumber;
            public double medianRowNumber;
        }
        public class datasetColNumber
        {
            public int minColNumber;
            public int currentColNumber =-1;
            public int maxColNumber;
            public double medianColNumber;
        }
        public class datasetFileNumber
        {
            public int minFileNumber;
            public int currentFileNumber=-1;
            public int maxFileNumber;
            public double medianFileNumber;
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
            public double medianActivity;
        }

        public class metadataComplition
        {
            public int totalFields;
            public int requiredFields;
            public int minRate;
            public int maxRate;
            public double medianRate;
        }

        public class fileInformation
        {
            public string fileName;
            public string fileDescription;
            public string fileFormat;
            public double fileSize;
        }
        public class varVariable
        {
            public string varLabel;
            public string varDescription;
            public string varType;
            public int varUsage;
            public double min;
            public double max;
            public int missing = 0;
            public bool uniqueValue = false;
            public int uniqueValueNumber;
            public string mostFrequent;
        }


        // GET: DQ
        public ActionResult ShowDQ(long datasetId, long versionId)
        {
            DQModels dqModel = new DQModels();
            Dictionary<string, string> datasetInfo = new Dictionary<string, string>();
            //List<string> performers = new List<string>();
            List<performer> performers = new List<performer>();
            List<varVariable> varVariables = new List<varVariable>();
            List<datasetInformation> datasetsInformation = new List<datasetInformation>();
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

            #region performers
            List<int> activities = new List<int>();

            foreach (var user in um.Users)
            {
                int activity = FindDatasetsFromPerformerUsername(dm, um, user.Name).Count();
                if (activity > 0)
                {
                    activities.Add(FindDatasetsFromPerformerUsername(dm, um, user.Name).Count());
                }
            }
            dqModel.performersActivity.minActivity = activities.Min();
            dqModel.performersActivity.maxActivity = activities.Max();
            dqModel.performersActivity.medianActivity = medianCalc(activities);

            List<string> performerUsernames = FindDatasetPerformers(dm, datasetId, versionId); //Find performers of the current dataset.
            foreach (var username in performerUsernames) //foreach performer of the current dataset
            {
                performer p = new performer();
                p.performerName = FindPerformerNameFromUsername(um, username);
                List<long> pfIds = FindDatasetsFromPerformerUsername(dm, um, username); //Find all datasets in wich the username is involved.
                p.DatasetIds = pfIds;
                p.performerRate = p.DatasetIds.Count();
                performers.Add(p);
            }
            dqModel.performers = performers;
            dqModel.isPublic = entityPermissionManager.GetRights(null, 1, datasetId);
            #endregion

            List<long> datasetIds = dm.GetDatasetLatestIds();
            List<int> metadataRates = new List<int>();
            List<int> dsDescLength = new List<int>();
            List<int> dstrDescLength = new List<int>();
            List<int> dstrUsage = new List<int>();
            List<int> datasetSizeTabular = new List<int>();
            List<int> datasetRows = new List<int>();
            List<int> datasetCols = new List<int>();
            List<double> datasetSizeFile = new List<double>();
            List<int> datasetFileNumber = new List<int>();
            List<int> restrictions = new List<int>();
            int fileNumber = 0;
            List<int> sizeTabular = new List<int>(); //collect size, column number, and row number for one dataset

            foreach (long Id in datasetIds)
            {
                if (dm.IsDatasetCheckedIn(Id))
                {
                    DatasetVersion datasetVersion = dm.GetDatasetLatestVersion(Id);  //get last dataset versions
                    
                    var publicRights = entityPermissionManager.GetRights(null, 1, Id); //1:public; 0:restricted
                    restrictions.Add(publicRights);
                    //bool b = entityPermissionManager.HasEffectiveRight(user, typeof(Dataset), Id, Security.Entities.Authorization.RightType.Read);
                    //int readUser = 0;
                    dqModel.userNumber = um.Users.Count();
                    //foreach (var user in um.Users)
                    //{
                    //    var b = entityPermissionManager.HasEffectiveRight(user, typeof(Dataset), Id, Security.Entities.Authorization.RightType.Read);
                    //    if ()
                    //    {
                    //        readUser += 1;
                    //    }
                    //}
                    //restrictions = datasetRestrictions(entityPermissionManager, datasetType, um, Id); //[0]:isPublic; [1]:% of read access
                    DataStructure dataStr = dsm.AllTypesDataStructureRepo.Get(datasetVersion.Dataset.DataStructure.Id);
                    int metadataRate = GetMetadataRate(datasetVersion);
                    metadataRates.Add(metadataRate);
                    dsDescLength.Add(datasetVersion.Description.Length);
                    dstrDescLength.Add(datasetVersion.Dataset.DataStructure.Description.Length);
                    dstrUsage.Add(dataStr.Datasets.Count());
                    string type = "file";
                    if (dataStr.Self.GetType() == typeof(StructuredDataStructure)) { type = "tabular"; }
                    if (type == "tabular")
                    {
                        sizeTabular = GetTabularSize(Id);
                        datasetSizeTabular.Add(sizeTabular[0]);
                        datasetCols.Add(sizeTabular[1]); //column number
                        datasetRows.Add(sizeTabular[2]); //row number  
                        fileNumber = -1;
                        datasetSizeFile.Add(-1);
                    }
                    else if (type == "file")
                    {
                        List<ContentDescriptor> contentDescriptors = datasetVersion.ContentDescriptors.ToList();
                        fileNumber = contentDescriptors.Count;
                        datasetSizeFile.Add(GetFileDatasetSize(datasetVersion));
                        datasetFileNumber.Add(fileNumber);
                        sizeTabular[0] = -1; //no tabular
                        sizeTabular[1] = -1; //no column
                        sizeTabular[2] = -1; //no row
                    }
                    List<int> pfRates = new List<int>();
                    List<string> pfs = FindDatasetPerformers(dm, Id); //A list of usernames
                    foreach (var username in pfs) //foreach performer of the current dataset
                    {
                        List<long> pfIds = FindDatasetsFromPerformerUsername(dm, um, username); //Find all datasets in wich the username is involved.
                        int pfRate = pfIds.Count();
                        pfRates.Add(pfRate);
                    }

                    //Collect information for each dataset.
                    datasetInformation datasetInformation = new datasetInformation();
                    datasetInformation.type = type;
                    datasetInformation.datasetId = Id;
                    datasetInformation.isPublic = restrictions[0];
                    datasetInformation.metadataComplition = metadataRate;
                    datasetInformation.descriptionLength = datasetVersion.Description.Length;
                    datasetInformation.structureDescriptionLength = datasetVersion.Dataset.DataStructure.Description.Length;
                    datasetInformation.structureUsage = dataStr.Datasets.Count();
                    datasetInformation.datasetSizeTabular = sizeTabular[0];
                    datasetInformation.columnNumber = sizeTabular[1];
                    datasetInformation.rowNumber = sizeTabular[2];
                    datasetInformation.fileNumber = fileNumber;
                    datasetInformation.datasetSizeFile = datasetSizeFile.Sum();
                    datasetInformation.performersRate = pfRates;                    
                    datasetsInformation.Add(datasetInformation);
                }
            }

            dqModel.datasetsInformation = datasetsInformation;

            //Add information about min and max metadata complition
            dqModel.metadataComplition.minRate = metadataRates.Min();
            dqModel.metadataComplition.maxRate = metadataRates.Max();
            dqModel.metadataComplition.medianRate = medianCalc(metadataRates);

            //Add information about min and max dataset description length
            dqModel.datasetDescriptionLength.minDescriptionLength = dsDescLength.Min();
            dqModel.datasetDescriptionLength.maxDescriptionLength = dsDescLength.Max();
            dqModel.datasetDescriptionLength.medianDescriptionLength = medianCalc(dsDescLength);

            //Add information about min and max data structure description length
            dqModel.dataStrDescriptionLength.minDescriptionLength = dstrDescLength.Min();
            dqModel.dataStrDescriptionLength.maxDescriptionLength = dstrDescLength.Max();
            dqModel.dataStrDescriptionLength.medianDescriptionLength = medianCalc(dstrDescLength);

            //Add information about min and max data structure usage
            dqModel.dataStrUsage.minDataStrUsage = dstrUsage.Min();
            dqModel.dataStrUsage.maxDataStrUsage = dstrUsage.Max();
            dqModel.dataStrUsage.medianDataStrUsage = medianCalc(dstrUsage);

            //Add information about dataset total size
            if (datasetSizeTabular.Count() > 0)
            {
                dqModel.datasetTotalSize.minSizeTabular = datasetSizeTabular.Min();
                dqModel.datasetTotalSize.maxSizeTabular = datasetSizeTabular.Max();
                dqModel.datasetTotalSize.medianSizeTabular = medianCalc(datasetSizeTabular);

                dqModel.datasetRowNumber.minRowNumber = datasetRows.Min();
                dqModel.datasetRowNumber.maxRowNumber = datasetRows.Max();
                dqModel.datasetRowNumber.medianRowNumber = medianCalc(datasetRows);

                dqModel.datasetColNumber.minColNumber = datasetCols.Min();
                dqModel.datasetColNumber.maxColNumber = datasetCols.Max();
                dqModel.datasetColNumber.medianColNumber = medianCalc(datasetCols);
            }
            if (datasetSizeFile.Count() > 0)
            {
                dqModel.datasetTotalSize.minSizeFile = datasetSizeFile.Min();
                dqModel.datasetTotalSize.maxSizeFile = datasetSizeFile.Max();
                dqModel.datasetTotalSize.medianSizeFile = medianCalc(datasetSizeFile);
            }
            if (datasetFileNumber.Count > 0)
            {
                dqModel.datasetFileNumber.minFileNumber = datasetFileNumber.Min();
                dqModel.datasetFileNumber.maxFileNumber = datasetFileNumber.Max();
                dqModel.datasetFileNumber.medianFileNumber = medianCalc(datasetFileNumber);
            }

            //CURRENT DATASET VERSION
            dqModel.metadataComplition.totalFields = GetMetadataRate(currentDatasetVersion); //current dataset version: metadata rate
            dqModel.metadataComplition.requiredFields = 100; //Need to calculate: metadataStructureId = dsv.Dataset.MetadataStructure.Id;
            dqModel.datasetDescriptionLength.currentDescriptionLength = currentDatasetVersion.Description.Length; // Current dataset vesion: dataset description length
            dqModel.dataStrDescriptionLength.currentDescriptionLength = currentDatasetVersion.Dataset.DataStructure.Description.Length; // Current dataset version: data structure description length
            dqModel.dataStrUsage.currentDataStrUsage = currentDataStr.Datasets.Count();
            //dqModel.datasetTotalSize.currentTotalSize = GetDatasetTotalSize(datasetId, currentDatasetVersion, currentDataStr, currentDatasetType);
            /////////////////////////////////////////////////////////////////////////

            #region TABULAR FORMAT DATASET                
            if (currentDatasetType == "tabular")
            {

                int count = 0;
                try
                {
                    count = dm.GetDatasetVersionEffectiveTupleIds(currentDatasetVersion).Count();
                }
                catch
                {

                }
                StructuredDataStructure sds = dsm.StructuredDataStructureRepo.Get(currentDatasetVersion.Dataset.DataStructure.Id);


                var variables = sds.Variables;
                dqModel.columnNumber = variables.Count();

                int columnNumber = -1; //First four columns are added from system.
                if (variables.Count() > 0)
                {
                    foreach (var variable in variables)
                    {
                        columnNumber += 1;
                        //string missingValue = variable.MissingValue; //MISSING VALUE
                        List<string> missingValues = new List<string>(); //creat a list contains missing values
                        foreach (var missValue in variable.MissingValues)
                        {
                            missingValues.Add(missValue.Placeholder);
                        }
                        //List<string> missingValues = variable.MissingValues;
                        varVariable varV = new varVariable();
                        varV.varLabel = variable.Label; // variable name
                        varV.varDescription = variable.Description;
                        varV.varUsage = variable.DataAttribute.UsagesAsVariable.Count(); //How many other data structures are using the same variable template
                        varV.varType = variable.DataAttribute.DataType.SystemType; // What is the system type?
                        varV.missing = 100;
                        try
                        {
                            DataTable table = dm.GetDatasetVersionTuples(versionId, 0, count);
                            DataColumnCollection columns = table.Columns;
                            DataRowCollection rows = table.Rows;

                            dqModel.datasetTotalSize.currentTotalSize = columns.Count * rows.Count;
                            dqModel.rowNumber = rows.Count;

                            double min = 0;
                            double max = 0;
                            int missing = rows.Count;
                            bool b = true; //first value
                            Dictionary<string, int> frequency = new Dictionary<string, int>();
                            foreach (DataRow row in rows)
                            {
                                var value = row.ItemArray[columnNumber];//.ToString();
                                if (value == null || missingValues.Contains(value.ToString())) //check if cell is emty or contains a missing value
                                {
                                    missing -= 1;
                                }

                                //if value is numeric and it is in the first row
                                else if (varV.varType != "String" && varV.varType != "DateTime" && varV.varType != "Boolean") //&& varV.varType != "DateTime"
                                {
                                    if (b == true)
                                    {
                                        min = Convert.ToDouble(value);
                                        max = Convert.ToDouble(value);
                                        b = false;
                                    }
                                    else
                                    {
                                        if (Convert.ToDouble(value) < min) { min = Convert.ToDouble(value); }
                                        if (Convert.ToDouble(value) > max) { max = Convert.ToDouble(value); }
                                    }
                                }

                                else //if data type is string or date or bool
                                {
                                    if (frequency.ContainsKey(value.ToString()))
                                    {
                                        frequency[value.ToString()] += 1;
                                    }
                                    else
                                    {
                                        frequency[value.ToString()] = 1;
                                    }
                                }
                            }
                            varV.min = min;
                            varV.max = max;
                            if (rows.Count > 0) { varV.missing = 100 * missing / rows.Count; }
                            if (frequency.Count() > 0)
                            {
                                var sortedDict = from entry in frequency orderby entry.Value ascending select entry;
                                if (sortedDict.First().Value == 1)
                                {
                                    varV.uniqueValue = true;
                                    varV.uniqueValueNumber = sortedDict.Count();
                                }
                                else
                                {
                                    varV.uniqueValue = false;
                                    varV.uniqueValueNumber = 0;
                                    varV.mostFrequent = sortedDict.First().Key;
                                }
                            }
                            varVariables.Add(varV);
                        }
                        catch
                        {
                            dqModel.datasetTotalSize.currentTotalSize = -1;
                        }

                    }
                    dqModel.varVariables = varVariables;
                }
            }

            #endregion

            #region file format dataset
            // File data structure
            else
            {
                //List<FileInformation> files = new List<FileInformation>();                
                List<fileInformation> filesInformation = new List<fileInformation>();
                if (currentDatasetVersion != null)
                {
                    List<ContentDescriptor> contentDescriptors = currentDatasetVersion.ContentDescriptors.ToList();

                    if (contentDescriptors.Count > 0)
                    {                        
                        foreach (ContentDescriptor cd in contentDescriptors)
                        {
                            if (cd.Name.ToLower().Equals("unstructureddata"))
                            {
                                fileInformation fileInformation = new fileInformation();
                                string uri = cd.URI;
                                string name = uri.Split('\\').Last();
                                fileInformation.fileName = name.Split('.')[0];
                                fileInformation.fileFormat = name.Split('.')[1];
                                double volum = -1;
                                fileInformation.fileSize = volum;

                                filesInformation.Add(fileInformation);
                            }

                        }                        
                    }
                    dqModel.fileNumber = contentDescriptors.Count;
                    dqModel.datasetTotalSize.currentTotalSize = -1;
                }
                dqModel.filesInformation = filesInformation;
                //files = getFilesByDatasetId(datasetId, entityType, versionId);
                //dqModel.datasetSize.Add("", 0);
            }
            #endregion

            return PartialView(dqModel);
        }

        private double GetFileDatasetSize(DatasetVersion datasetVersion)
        {
            double size = -1;
            return (size);
        }


        private List<int> GetTabularSize(long id)
        {
            List<int> sizeTabular = new List<int>();
            DatasetManager dm = new DatasetManager();
            try
            {
                DataTable table = dm.GetLatestDatasetVersionTuples(id, true);
                DataRowCollection rows = table.Rows;
                DataColumnCollection columns = table.Columns;
                sizeTabular.Add(rows.Count * columns.Count);
                sizeTabular.Add(columns.Count);
                sizeTabular.Add(rows.Count);
            }
            catch
            {
                sizeTabular.Add(-1);
                sizeTabular.Add(-1);
                sizeTabular.Add(-1);
            }
            return (sizeTabular);
        }
        
        /// <summary>
        /// This function calculates the median of a list
        /// </summary>
        /// <param name="intList">a list of integers</param>
        /// <returns>double median number</returns>
        private double medianCalc(List<int> intList)
        {
            List<int> sortedList = intList.OrderBy(i => i).ToList();
            
            //get the median
            int size = sortedList.Count();
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedList[mid] : ((double)sortedList[mid] + (double)sortedList[mid - 1]) / 2;
            
            return (median);
        }
        private double medianCalc(List<double> doubleList)
        {
            List<int> intList = new List<int>();
            foreach(double d in doubleList)
            {
                intList.Add((int)Math.Round(d));
            }
            List<int> sortedList = intList.OrderBy(i => i).ToList();

            //get the median
            int size = sortedList.Count();
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedList[mid] : ((double)sortedList[mid] + (double)sortedList[mid - 1]) / 2;

            return (median);
        }

        /// <summary>
        /// This function calculates the percentage of a metadata completeness.
        /// </summary>
        /// <param name="dsv">dataset version</param>
        /// <returns>rate: percentage of the metadata completeness</returns>
        private int GetMetadataRate(DatasetVersion dsv)
        {
            XmlDocument metadata = dsv.Metadata;
            string xmlFrag = metadata.OuterXml;
            List<int> metaInfo = GetMetaInfoArray(xmlFrag);
            // [0] number of all metadata qttributes, [1] number of filled metadata atributes
            // [2] number of all required metadata attributes, [3] number of filled required metadata attributes
            int rate = (metaInfo[1] * 100) / metaInfo[0]; //percentage of all metadata fields contains information
            return (rate);
        }

        /// <summary>
        /// This function gives an xml format of a filled metadata structure, read all lines and select the lines related to fields,
        /// counts all fields and all fields that contain information.
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
            metaInfo.Add(countMetaAttr); // number of all metadata qttributes
            metaInfo.Add(countMetaComplition); // number of filled metadata atributes
            metaInfo.Add(100); // number of all required metadata attributes
            metaInfo.Add(100); // number of filled required metadata attributes
            return (metaInfo);
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

        private List<string> FindDatasetPerformers(DatasetManager dm, long datasetId) //latest version
        {
            var dsvs = dm.GetDatasetVersions(datasetId);
            string performer;
            List<string> performerUsernames = new List<string>();
            foreach (var d in dsvs)
            {
                performer = d.ModificationInfo.Performer;
                if (performer != null && !performerUsernames.Contains(performer))
                {
                    performerUsernames.Add(performer);
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


        /// <summary>
        /// THIS IS CREATED AUTOMATICALLY!
        /// </summary>
        /// <param name="datasetIds"></param>
        /// <param name="performerName"></param>
        /// <returns></returns>
        public ActionResult ShowDatasetList(string datasetIds, string performerName)
        {
            DatasetListModel dsModel = new DatasetListModel();
            return View(dsModel);
        }

    }
}