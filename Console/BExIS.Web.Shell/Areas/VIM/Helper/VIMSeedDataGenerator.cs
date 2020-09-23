﻿using BExIS.Security.Entities.Objects;
using BExIS.Security.Services.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Vaiona.Web.Mvc.Modularity;

namespace BExIS.Modules.Vim.UI.Helper
{
    public class VIMSeedDataGenerator : IModuleSeedDataGenerator
    {
        public void GenerateSeedData()
        {
            FeatureManager featureManager = new FeatureManager();
            OperationManager operationManager = new OperationManager();

            try
            {
                Feature visualization =
                    featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Visualization"));
                if (visualization == null)
                    visualization = featureManager.Create("Visualization", "Visualization");

                Feature dataQuality =
                    featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("DataQuality"));
                if (dataQuality == null)
                    dataQuality = featureManager.Create("DataQuality", "DataQuality");

                operationManager.Create("VIM", "Visualization", "*", visualization);
                operationManager.Create("VIM", "DQ", "*", dataQuality);
                operationManager.Create("VIM", "Help", "*");

            }
            catch (Exception ex)
            {
                throw ex;
            }

            finally
            {
                featureManager?.Dispose();
            }
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
