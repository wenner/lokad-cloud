﻿using System.IO;
using System.Web;
using System.Web.Mvc;
using Lokad.Cloud.Console.WebRole.Behavior;
using Lokad.Cloud.Console.WebRole.Controllers.ObjectModel;
using Lokad.Cloud.Console.WebRole.Framework.Discovery;
using Lokad.Cloud.Console.WebRole.Models.Assemblies;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Console.WebRole.Controllers
{
    [RequireAuthorization, RequireDiscovery]
    public sealed class AssembliesController : TenantController
    {
        public AssembliesController(AzureDiscoveryInfo discoveryInfo)
            : base(discoveryInfo)
        {
        }

        public override ActionResult ByHostedService(string hostedServiceName)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudAssemblies = new CloudAssemblies(Storage.BlobStorage);
            var appDefinition = cloudAssemblies.GetApplicationDefinition();

            return View(new AssembliesModel
                {
                    ApplicationAssemblies = appDefinition.Convert(ad => ad.Assemblies)
                });
        }

        [HttpPost]
        public ActionResult UploadPackage(string hostedServiceName, HttpPostedFileBase package)
        {
            InitializeDeploymentTenant(hostedServiceName);
            var cloudAssemblies = new CloudAssemblies(Storage.BlobStorage);

            byte[] bytes;
            using (var reader = new BinaryReader(package.InputStream))
            {
                bytes = reader.ReadBytes(package.ContentLength);
            }

            switch ((Path.GetExtension(package.FileName) ?? string.Empty).ToLowerInvariant())
            {
                case ".dll":
                    cloudAssemblies.UploadAssemblyDll(bytes, package.FileName);
                    break;

                default:
                    cloudAssemblies.UploadAssemblyZipContainer(bytes);
                    break;
            }

            return RedirectToAction("ByHostedService");
        }
    }
}