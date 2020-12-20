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

namespace BExIS.Modules.Vim.UI.Controllers
{

    public class DQController : Controller
    {
        public class performer
        {
            public string performerName;
            public List<long> DatasetIds;
            //public List<string> dsLinks;
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
            //PartyManager pm = new PartyManager();
            UserManager um = new UserManager();
            DataContainerManager dataContainerManager = new DataContainerManager();
            DataAttributeManagerModel daModel = new DataAttributeManagerModel();

            //EntityManager entityManager = new EntityManager();

            try
            {
                if (dm.IsDatasetCheckedIn(datasetId))
                {
                    
                    // get all dataset versions
                    var dsvs = dm.GetDatasetVersions(datasetId);
                    //List<string> performerUserNames = new List<string>();
                    List<string> performerUsernames = FindDatasetPerformers(dm, datasetId, versionId);

                    
                    foreach(var username in performerUsernames)
                    {

                        performer p = new performer();
                        p.performerName = FindPerformerNameFromUsername(um, username);
                        List<long> datasetIds = FindDatasetsFromPerformerUsername(dm, um, username);
                        p.DatasetIds = datasetIds;
                        //p.DatasetIds = FindDatasetsFromPerformerUsername(dm, username);
                        performers.Add(p);
                    }
                    dqModel.performers = performers;
                    //var performers = dsvs.Select(v => v.CreationInfo.Performer).ToList();

                    // get datasetversion
                    var dsv = dm.GetDatasetVersion(versionId);
                    string title = dsv.Title;
                    dqModel.title = title;
                    string description = dsv.Description;
                    dqModel.description = description;

                    var metadata = dsv.Metadata;

                    string type = "file"; // Suppose a File format dataset

                    string dStrDescription = dsv.Dataset.DataStructure.Description;
                    dqModel.dStrDescription = dStrDescription;

                    DataStructure ds = dsm.AllTypesDataStructureRepo.Get(dsv.Dataset.DataStructure.Id);

                    #region
                    // Tabular data structure
                    if (ds.Self.GetType() == typeof(StructuredDataStructure))
                    {
                        type = "tabular";
                        StructuredDataStructure sds = dsm.StructuredDataStructureRepo.Get(dsv.Dataset.DataStructure.Id);
                        var dataStrUsage = sds.Datasets;
                        dqModel.dStrUsage = dataStrUsage.Count();
                        
                        var variables = sds.Variables;
                        double numberOfColumns = variables.Count();
                        datasetSize.Add("NumberOfColumns", numberOfColumns); //number of columns
                        //Discover a data table
                        //The limit of row number is count = max int number
                        DataTable table = null;
                        long rowCount = dm.RowCount(datasetId, null);                        
                        datasetSize.Add("NumberOfRows", (double)rowCount); // number of rows
                        
                        if (rowCount > 0) table = dm.GetLatestDatasetVersionTuples(datasetId, null, null, null, 0, (int)rowCount);
                        DataRowCollection dataRows = table.Rows;

                        int columnNumber = 3;
                        //Read all variables and provide information
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
                                                               
                                varV.min = values.Min().ToString() + " - " + values.Max().ToString();
                                //if (varV.varType == "String")
                                //{
                                //    //string mostFrequence = FindMostFrequenceValue(variable);
                                //}

                                varVariables.Add(varV);

                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }

                        }
                        varVariables.Count();
                        dqModel.varVariables = varVariables;
                        dqModel.varVariables.Count();
                        //List<string> variables = 

                        ////////////////////////////////////////////////////////////////////////////////////////////
                       
                        string fake2 = "nn";
                    }
                    #endregion

                    // File data structure
                    else
                    {
                        //dqModel.datasetSize.Add("", 0);
                    }
                    dqModel.type = type;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                dm.Dispose();
                dsm.Dispose();
                entityPermissionManager.Dispose();
            }
            
            dqModel.datasetInfo = datasetInfo;
            dqModel.datasetSize = datasetSize;
            dqModel.performers = performers;
            return PartialView(dqModel);
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