using System;
using System.Net.Http;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;

namespace XgateSmsPackage
{
    /// <summary>
    /// Import package starter frame.
    /// </summary>
    [Export(typeof(IImportExtensions))]
    public class PackageImportExtension : ImportExtension
    {
        #region Metadata

        /// <summary>
        /// Folder name where package assets are located in the final output package zip.
        /// </summary>
        public override string GetImportPackageDataFolderName => "PkgAssets";

        /// <summary>
        /// Name of the Import Package to Use
        /// </summary>
        /// <param name="plural">if true, return plural version</param>
        public override string GetNameOfImport(bool plural) => "XgateSmsPackage";

        /// <summary>
        /// Long name of the Import Package.
        /// </summary>
        public override string GetLongNameOfImport => "XgateSmsPackage";

        /// <summary>
        /// Description of the package, used in the package selection UI
        /// </summary>
        public override string GetImportPackageDescriptionText => "XgateSmsPackage";

        #endregion

        /// <summary>
        /// Called to Initialize any functions in the Custom Extension.
        /// </summary>
        /// <see cref="ImportExtension.InitializeCustomExtension"/>
        public override void InitializeCustomExtension()
        {
        }

        /// <summary>
        /// Called before the Main Import process begins, after solutions and data.
        /// </summary>
        /// <see cref="ImportExtension.BeforeImportStage"/>
        /// <returns></returns>
        public override bool BeforeImportStage()
        {
            return true;
        }

        /// <summary>
        /// Raised before the named solution is imported to allow for any configuration settings to be made to the import process
        /// </summary>
        /// <see cref="ImportExtension.PreSolutionImport"/>
        /// <param name="solutionName">name of the solution about to be imported</param>
        /// <param name="solutionOverwriteUnmanagedCustomizations">Value of this field from the solution configuration entry</param>
        /// <param name="solutionPublishWorkflowsAndActivatePlugins">Value of this field from the solution configuration entry</param>
        /// <param name="overwriteUnmanagedCustomizations">If set to true, imports the Solution with Override Customizations enabled</param>
        /// <param name="publishWorkflowsAndActivatePlugins">If set to true, attempts to auto publish workflows and activities as part of solution deployment</param>
        public override void PreSolutionImport(string solutionName, bool solutionOverwriteUnmanagedCustomizations, bool solutionPublishWorkflowsAndActivatePlugins, out bool overwriteUnmanagedCustomizations, out bool publishWorkflowsAndActivatePlugins)
        {
            base.PreSolutionImport(solutionName, solutionOverwriteUnmanagedCustomizations, solutionPublishWorkflowsAndActivatePlugins, out overwriteUnmanagedCustomizations, out publishWorkflowsAndActivatePlugins);
        }

        /// <summary>
        /// Called during a solution upgrade when both solutions, old and new, are present in the system.
        /// This function can be used to provide a means to do data transformation or upgrade while a solution update is occurring.
        /// </summary>
        /// <see cref="ImportExtension.RunSolutionUpgradeMigrationStep"/>
        /// <param name="solutionName">Name of the solution</param>
        /// <param name="oldVersion">version number of the old solution</param>
        /// <param name="newVersion">Version number of the new solution</param>
        /// <param name="oldSolutionId">Solution ID of the old solution</param>
        /// <param name="newSolutionId">Solution ID of the new solution</param>
        public override void RunSolutionUpgradeMigrationStep(string solutionName, string oldVersion, string newVersion, Guid oldSolutionId, Guid newSolutionId)
        {
            base.RunSolutionUpgradeMigrationStep(solutionName, oldVersion, newVersion, oldSolutionId, newSolutionId);
        }

        /// <summary>
        /// Called After all Import steps are complete, allowing for final customizations or tweaking of the instance.
        /// </summary>
        /// <see cref="ImportExtension.AfterPrimaryImport"/>
        /// <returns></returns>
        public override bool AfterPrimaryImport()
        {
            System.Diagnostics.Debugger.Launch();

            try
            {
                Guid tenantId = this.CrmSvc.TenantId;
                Guid orgId = this.CrmSvc.ConnectedOrgId;
                
                string requestBody = $@"{{
                    ""action"": ""auto_provision"",
                    ""tenantId"": ""{tenantId}"",
                    ""orgId"": ""{orgId}""
                }}";

                using (HttpClient client = new HttpClient())
                { 
                    client.Timeout = TimeSpan.FromSeconds(30); 
                    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    
                    HttpResponseMessage response = client.PostAsync("https://api.xgate.com/webhook/d365-provision", content).Result;
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseStr = response.Content.ReadAsStringAsync().Result;
                        JObject gatewayData = JObject.Parse(responseStr);
                        
                        string appId = gatewayData["appId"]?.ToString();
                        string appSecret = gatewayData["appSecret"]?.ToString();
                        string apiUrl = gatewayData["apiUrl"]?.ToString();
                        string senderId = gatewayData["senderId"]?.ToString();
                        if (string.IsNullOrEmpty(senderId)) senderId = "XGATE";
 

                        if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(senderId))
                        {
                            // ========================================================
                            // 步骤 1：获取系统的 Channel Definition ID
                            // 定义在 /xgatesmschannel.solution/unmanaged/Other/relationships/customizations.xml 中
                            // ========================================================
                            Guid channelDefId = new Guid("d78c79f8-d725-41ff-b0de-87988baf2643");
                            // ========================================================
                            // 步骤 2：创建专属账户表 (xgate_...account)
                            // ========================================================
                            Entity customAccount = new Entity("xgate_xgatesmschannelinstanceaccount"); 
                            customAccount["xgate_accountid"] = appId; 
                            customAccount["xgate_accountsecret"] = appSecret; 
                            customAccount["xgate_apibaseurl"] = apiUrl;
                            Guid customAccountId = this.CrmSvc.Create(customAccount);
                            this.PackageLog.Log($"[XGATE] 专属账户创建成功: {customAccountId}");

                            // ========================================================
                            // 步骤 3：创建全局账户表 (msdyn_...account)，并指向专属账户
                            // ========================================================
                            Entity baseAccount = new Entity("msdyn_channelinstanceaccount");
                            baseAccount["msdyn_name"] = "XGATE SMS Configuration";
                            baseAccount["msdyn_channeldefinitionid"] = new EntityReference("msdyn_channeldefinition", channelDefId);
                            baseAccount["msdyn_extendedentityid"] = new EntityReference("xgate_xgatesmschannelinstanceaccount", customAccountId);
                            Guid baseAccountId = this.CrmSvc.Create(baseAccount);

                            // ========================================================
                            // 步骤 4：创建专属发件人表 (xgate_...instance) 
                            // ========================================================
                            Entity customSender = new Entity("xgate_xgatesmschannelinstance");
                            customSender["xgate_type"] = new OptionSetValue(698540002); // 写入你的 OptionSet 值
                            Guid customSenderId = this.CrmSvc.Create(customSender);
                            this.PackageLog.Log($"[XGATE] 专属发件人创建成功: {customSenderId}");

                            // ========================================================
                            // 步骤 5：创建全局发件人表 (msdyn_channelinstance)，实现终极闭环
                            // ========================================================
                            Entity baseSender = new Entity("msdyn_channelinstance");
                            baseSender["msdyn_name"] = senderId; 
                            baseSender["msdyn_contactpoint"] = senderId;
                            baseSender["msdyn_channeldefinitionid"] = new EntityReference("msdyn_channeldefinition", channelDefId);

                            baseSender["msdyn_channelinstanceaccountid"] = new EntityReference("msdyn_channelinstanceaccount", baseAccountId); 
                            baseSender["msdyn_extendedentityid"] = new EntityReference("xgate_xgatesmschannelinstance", customSenderId);
                            // 绑定“消费应用程序 (Customer Journey Orchestration)”,gemini说这个id全球统一，都是这个
                            // 表名通常为 msdyn_consumingapplication，直接填入固定的 Guid
                            baseSender["msdyn_consumingapplicationid"] = new EntityReference("msdyn_consumingapplication", new Guid ("268d20d7-fa57-4fff-aa8a-faea2d83519f"));
                            Guid baseSenderId = this.CrmSvc.Create(baseSender);
                            this.PackageLog.Log($"[XGATE] 全局发件人创建成功，整个通道已全自动打通！");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.PackageLog.Log($"[XGATE ERROR] 自动配置失败: {ex.Message}");
            }

            return true;
        }
    }
}
