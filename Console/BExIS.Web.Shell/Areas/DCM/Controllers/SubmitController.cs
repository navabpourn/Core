﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using BExIS.Io.Transform.Input;
using BExIS.Io.Transform.Validation.Exceptions;
using BExIS.Dlm.Entities.Administration;
using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Entities.DataStructure;
using BExIS.Dlm.Services.Administration;
using BExIS.Dlm.Services.Data;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Web.Shell.Areas.DCM.Models;
using BExIS.Dcm.UploadWizard;
using Vaiona.Util.Cfg;
using System.Diagnostics;
using BExIS.Io.Transform.Output;
using BExIS.Xml.Services;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Dcm.Wizard;

namespace BExIS.Web.Shell.Areas.DCM.Controllers
{
    public class SubmitController : Controller
    {
        //
        // GET: /Collect/Home/

        
        List<string> ids = new List<string>();
        private TaskManager TaskManager;
        private FileStream Stream;

        public ActionResult Index()
        {
            return View();
        }

        #region Upload Wizard

        public ActionResult UploadWizard()
        {
            Session["TaskManager"] = null;

            if (TaskManager == null) TaskManager = (TaskManager)Session["TaskManager"];

            if (TaskManager == null)
            {
                try
                {
                    string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DCM"), "SubmitTaskInfo.xml");
                    XmlDocument xmlTaskInfo = new XmlDocument();
                    xmlTaskInfo.Load(path);


                    Session["TaskManager"] = TaskManager.Bind(xmlTaskInfo);
                }
                catch (Exception e)
                {
                    ModelState.AddModelError(String.Empty, e.Message);
                }

                Session["Filestream"] = Stream;

                TaskManager = (TaskManager)Session["TaskManager"];

                // get Lists of Dataset and Datastructure
                Session["DatasetVersionViewList"] = LoadDatasetVersionViewList();
                Session["DataStructureViewList"] = LoadDataStructureViewList();
                Session["ResearchPlanViewList"] = LoadResearchPlanViewList();
                
            }


            return View((TaskManager)Session["TaskManager"]);
        }

     
        #region UploadNavigation

        [HttpPost]
        public ActionResult RefreshNavigation()
        {
            TaskManager = (TaskManager)Session["TaskManager"];

            return PartialView("_uploadWizardNav", TaskManager);
        }

        [HttpPost]
        public ActionResult RefreshTaskList()
        {
            TaskManager = (TaskManager)Session["TaskManager"];

            return PartialView("_taskListView", TaskManager.GetStatusOfStepInfos());
        }

        #endregion

        #region Finish

        [HttpGet]
        public ActionResult FinishUpload()
        {
            TaskManager = (TaskManager)Session["TaskManager"];
            //TaskManager.SetCurrent(null);

            FinishUploadModel finishModel = new FinishUploadModel();
            finishModel.DatasetTitle = TaskManager.Bus[TaskManager.DATASET_TITLE].ToString();
            finishModel.Filename = TaskManager.Bus[TaskManager.FILENAME].ToString();

            Session["TaskManager"] = null;
            try
            {
                string path = Path.Combine(AppConfiguration.GetModuleWorkspacePath("DCM"), "SubmitTaskInfo.xml");
                XmlDocument xmlTaskInfo = new XmlDocument();
                xmlTaskInfo.Load(path);


                Session["TaskManager"] = TaskManager.Bind(xmlTaskInfo);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(String.Empty, e.Message);
            }


            return View(finishModel);
        }

        #endregion
        
        #region Navigation options

        public ActionResult CloseUpload()
        {
            Session["TaskManager"] = null;
            TaskManager = null;

            return RedirectToAction("UploadWizard");
        }

        #endregion

        #region Helper functions

            // chekc if user exist
            // if true return usernamem otherwise "DEFAULT"
            public string GetUserNameOrDefault()
            {
                string userName = string.Empty;
                try
                {
                    userName = HttpContext.User.Identity.Name;
                }
                catch { }

                return !string.IsNullOrWhiteSpace(userName) ? userName : "DEFAULT";
            }

            public List<ListViewItem> LoadDatasetVersionViewList()
            {
                DatasetManager dm = new DatasetManager();
                Dictionary<long, XmlDocument> dmtemp = new Dictionary<long, XmlDocument>();
                dmtemp = dm.GetDatasetLatestMetadataVersions();
                List<ListViewItem> temp = new List<ListViewItem>();

                foreach (long datasetid in dmtemp.Keys)
                {
                    if (dmtemp[datasetid] != null)
                    {
                        XmlNodeList xnl = dmtemp[datasetid].SelectNodes("Metadata/Description/Description/Title/Title");
                        string title = "";

                        if (xnl.Count > 0)
                        {
                            title = xnl[0].InnerText;
                        }

                        temp.Add(new ListViewItem(datasetid, title));
                    }
                }

               return temp.OrderBy(p => p.Title).ToList();
            }

            public List<ListViewItem> LoadDataStructureViewList()
            {
                DataStructureManager dsm = new DataStructureManager();
                List<ListViewItem> temp = new List<ListViewItem>();

                foreach (DataStructure datasStructure in dsm.StructuredDataStructureRepo.Get())
                {
                    string title = datasStructure.Name;

                    temp.Add(new ListViewItem(datasStructure.Id, title));
                }



                return temp.OrderBy(p => p.Title).ToList();
            }

            public List<ListViewItem> LoadResearchPlanViewList()
            {
                ResearchPlanManager rpm = new ResearchPlanManager();
                List<ListViewItem> temp = new List<ListViewItem>();

                foreach (ResearchPlan researchPlan in rpm.Repo.Get())
                {
                    string title = researchPlan.Title;

                    temp.Add(new ListViewItem(researchPlan.Id, title));
                }

                return temp.OrderBy(p => p.Title).ToList();
            }

        #endregion

        #endregion

       




    }

    public class UpdateNameModel
    {
        public string Name { get; set; }
        public IEnumerable<int> Numbers { get; set; }
    } 

}