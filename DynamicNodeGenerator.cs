using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MvcSiteMapProvider;
using System.Web.Security;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Mvc;
using Dapper;
using DapperExtensions;
using SAMPLE.Models;
using System.Diagnostics;
using BitFactory.Logging;

namespace SAMPLE.Utils
{
    public class DynamicNodeGenerator : DynamicNodeProviderBase
    {
        private string _connectionString = string.Empty;
        
        public DynamicNodeGenerator()
        {
            //Initialize connection string
			_connectionString = SAMPLEDB.ConnectionString;
		}

        public override IEnumerable<DynamicNode> GetDynamicNodeCollection(ISiteMapNode node)
        {
            var returnValue = new List<DynamicNode>();
            bool isUserLogin = false;
            string UserName = string.Empty;
            
            if (HttpContext.Current.Session["Sample"] != null && HttpContext.Current.Session["Sample"].ToString().Trim() == "1")
            {
                isUserLogin = true;
                UserName = HttpContext.Current.Session["Sample-Name"].ToString();
            }
            else if (HttpContext.Current.User != null && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                isUserLogin = true;
                UserName = HttpContext.Current.User.Identity.Name;
            }
            else
                isUserLogin = false;

            if (isUserLogin)
            {
                string userRole = UserRoleTask.GetRoleForUser(UserName);

                if (userRole != null)
                {
                    IEnumerable<MenuSystem> taskList = new List<MenuSystem>();
                    HttpContext.Current.Session["RoleName"] = userRole;

                    using (IDbConnection cn = new SqlConnection(_connectionString))
                    {
                        cn.Open();
                        taskList = cn.GetList<MenuSystem>().Where(z => z.Active == true && z.ParentId==null).OrderBy(x=>x.DisplayOrder);
                    
                    // This should always be a unique set of items..
                    IEnumerable<RoleTask> usersTaskList = UserRoleTask.GetAllTaskOfGivenRole(userRole); 

                    HttpContext.Current.Session["TaskList"] = usersTaskList.ToList();

                    var tlist = (from ul in usersTaskList
                                 join tl in taskList on ul.MenuId equals tl.TaskId
                                 orderby tl.DisplayOrder
                                 select ul);

                    if (usersTaskList != null)
                    {
                        foreach (RoleTask userTask in tlist)
                        {
                            if (userTask.CanView || userTask.CanUpdate || userTask.CanCreate || userTask.CanDelete)
                            {
                                var d1 = taskList.Where(tl => tl.TaskId == userTask.MenuId).FirstOrDefault();

                                if (d1 != null)
                                {
                                    List<DynamicNode> listNewNodes = new List<DynamicNode>();
                                    DynamicNode nodes = new DynamicNode();

                                    nodes.Title = d1.NodeName;
                                    //node.Area = task.AreaName.Trim();

                                    if (d1.ControllerName != null)
                                    {
                                        nodes.Controller = d1.ControllerName.Trim();
                                        nodes.Action = d1.ActionName.Trim();
                                    }  
                                    else
                                    {
                                        nodes.Clickable = false;
                                    }
                                    
                                    nodes.Key = d1.TaskId.ToString();
                                    listNewNodes.Add(nodes);
                                    if (nodes.Controller != null && nodes.Action=="Index")//&& cn.GetList<MenuSystem>().Where(x => x.ControllerName == nodes.Controller).Count() < 2
                                        AddNode(listNewNodes, d1.TaskId.ToString(), d1.ControllerName.Trim());                                // Adding Site Map path for the task.
                                    GetDynamicNodeByTask(d1,listNewNodes);
                                   
                                    returnValue.AddRange(listNewNodes);
                                }
                                else 
                                {
                                    ConfigLogger.Instance.LogError("DynamicNodeGenerator", "Error:: TaskName \"" + d1.NodeName + "\" Not Found in Database.");
                                }
                            }
                        }
                        returnValue.AddRange(childslistNewNodes);
                        childslistNewNodes = new List<DynamicNode>();
                    }
                    cn.Close();
                    }
                }
                  
            }

            return returnValue;
        }

        //public override CacheDescription GetCacheDescription()
        //{
        //    return new CacheDescription("DynamicNodeGenerator")
        //    {
        //        SlidingExpiration = TimeSpan.FromMinutes(1)
        //    };
        //}

        private DynamicNode GetParentNode(string Key)
        {
            DynamicNode node = new DynamicNode();

            node.Title = Key;
            node.Clickable = false;
            node.Key = Key;

            return node;
        }

        public void AddNode(List<DynamicNode> returnValue, string key, string controller)
        {
           
            var createNode = new DynamicNode("Create_" + Guid.NewGuid().ToString(), key, "Create", "Create", controller, "Create");
            var editNode = new DynamicNode("Edit_" + key, key, "Edit", "Edit", controller, "Edit");
            var detailNode = new DynamicNode("Detail_" + key, key, "Details", "Details", controller, controller == "Lead" ? "LeadDetail" : "Details");
            
            createNode.PreservedRouteParameters.Add("id");
            editNode.PreservedRouteParameters.Add("id");
            detailNode.PreservedRouteParameters.Add("id");

            if(!returnValue.Contains(createNode)) returnValue.Add(createNode);
            if(!returnValue.Contains(editNode)) returnValue.Add(editNode);
            if(!returnValue.Contains(detailNode)) returnValue.Add(detailNode);
            if (controller == "Seminar")
            {
                var attendeesNode = new DynamicNode("Attendees_" + key, key, "Attendees", "Attendees", controller, "Attendees");
                attendeesNode.PreservedRouteParameters.Add("id");
                if (!returnValue.Contains(attendeesNode)) returnValue.Add(attendeesNode);
            }

            #region To Show Site Map with Detail also.
            //using (IDbConnection cn = new SqlConnection(_connectionString))
            //{
              
            //    cn.Open();
            //    if (controller == "Lead")
            //    {
            //        IEnumerable<Client> clientList = new List<Client>();
            //        clientList = cn.GetList<Client>();
            //        var createNode = new DynamicNode("Create", key, "Create", "Create", controller, "Create");
            //        returnValue.Add(createNode);

            //        foreach (var client in clientList)
            //        {
            //            var clientKey = "client_" + client.ClientId.ToString();

            //            // Create the "Details" node for the client
            //            var clientNode = new DynamicNode(clientKey, key, client.FirstName, client.FirstName, controller, controller == "Lead" ? "LeadDetail" : "Details");

            //            // Set the "id" route value so the match will work.
            //            clientNode.RouteValues.Add("id", client.ClientId);

            //            // Set our visibility. This will override what we have configured on the Dynamicclients node. We need to 
            //            // do this to ensure our clients are visible in the /sitemap.xml path.
            //            clientNode.Attributes["visibility"] = "SiteMapPathHelper,XmlSiteMapResult,!*";

            //            // Add the node to the result
            //            returnValue.Add(clientNode);


            //            // Create the "Edit" node for the client
            //            var clientEditNode = new DynamicNode("clientEdit_" + client.ClientId.ToString(), clientKey, "Edit", "Edit", controller, "Edit");

            //            // Set the "id" route value of the edit node
            //            clientEditNode.RouteValues.Add("id", client.ClientId);

            //            // Add the node to the result
            //            returnValue.Add(clientEditNode);


            //            // Create the "Delete" node for the client
            //            var clientDeleteNode = new DynamicNode("clientDelete_" + client.ClientId.ToString(), clientKey, "Delete", "Delete", controller, "Delete");

            //            // Set the "id" route value of the delete node
            //            clientDeleteNode.RouteValues.Add("id", client.ClientId);

            //            // Add the node to the result
            //            returnValue.Add(clientDeleteNode);
            //        }
            //    }
            //    else if (controller == "Agent")
            //    {
            //        IEnumerable<AppUser> agentList = new List<AppUser>();
            //        agentList = cn.GetList<AppUser>();
            //        var createNode = new DynamicNode("agent_Create", key, "Create", "Create", controller, "Create");
            //        returnValue.Add(createNode);

            //        foreach (var agent in agentList)
            //        {
            //            var clientKey = "agent_" + agent.AppUserId.ToString();

            //            // Create the "Details" node for the client
            //            var clientNode = new DynamicNode(clientKey, key, agent.FirstName, agent.FirstName, controller, controller == "Lead" ? "LeadDetail" : "Details");

            //            // Set the "id" route value so the match will work.
            //            clientNode.RouteValues.Add("id", agent.AppUserId);

            //            // Set our visibility. This will override what we have configured on the Dynamicclients node. We need to 
            //            // do this to ensure our clients are visible in the /sitemap.xml path.
            //            clientNode.Attributes["visibility"] = "SiteMapPathHelper,XmlSiteMapResult,!*";

            //            // Add the node to the result
            //            returnValue.Add(clientNode);


            //            // Create the "Edit" node for the client
            //            var clientEditNode = new DynamicNode("agentEdit_" + agent.AppUserId.ToString(), clientKey, "Edit", "Edit", controller, "Edit");

            //            // Set the "id" route value of the edit node
            //            clientEditNode.RouteValues.Add("id", agent.AppUserId);

            //            // Add the node to the result
            //            returnValue.Add(clientEditNode);


            //            // Create the "Delete" node for the client
            //            var clientDeleteNode = new DynamicNode("agentDelete_" + agent.AppUserId.ToString(), clientKey, "Delete", "Delete", controller, "Delete");

            //            // Set the "id" route value of the delete node
            //            clientDeleteNode.RouteValues.Add("id", agent.AppUserId);

            //            // Add the node to the result
            //            returnValue.Add(clientDeleteNode);
            //        }
            //    }
            //    cn.Close();
            //}
            #endregion
        }
       
        private DynamicNode GetNodeDetails(MenuSystem task)
        {
            DynamicNode node = new DynamicNode();

            node.Title = task.NodeName;
            //node.Area = task.AreaName.Trim();
            
            node.Controller = task.ControllerName.Trim();
            node.Action = task.ActionName.Trim();
            //node.Action = "ShowElectionResult";
            node.Key = task.TaskId.ToString();
            
            return node;
        }

        private DynamicNode GetChildNodeDetails(MenuSystem task, string ParentKey)
        {
            DynamicNode node = new DynamicNode();

            node.Title = task.NodeName;
            //node.Area = task.AreaName.Trim();

            node.Controller = task.ControllerName.Trim();
            node.Action = task.ActionName.Trim();
            
            //node.Action = "ShowElectionResult";
            node.Key = task.TaskId.ToString();
            node.ParentKey = ParentKey;

            return node;
        }

        private DynamicNode GetSeparatorNode(string ParentKey)
        {
            DynamicNode node = new DynamicNode();

            node.Title = "Separator";
            node.Clickable = false;
            node.ParentKey = ParentKey;

            return node;
        }

        List<DynamicNode> childslistNewNodes = new List<DynamicNode>();
        private void GetDynamicNodeByTask(MenuSystem task, List<DynamicNode> listNewNodes,bool Recursive=false)
        {
            IEnumerable<MenuSystem> taskList = new List<MenuSystem>();

            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                taskList = cn.GetList<MenuSystem>().Where(z => z.ParentId == task.TaskId && z.Active==true).OrderBy(x=>x.DisplayOrder);
                
            if (taskList.Count() > 0)
            {
                int i = 1;
                foreach (var item in taskList)
                {
                    IEnumerable<RoleTask> usersTaskList = new List<RoleTask>(); //.GroupBy(k => k.TaskName).Select(k => k.First());
                    usersTaskList = (List<RoleTask>)HttpContext.Current.Session["TaskList"];
                    RoleTask roletask = usersTaskList.Where(x => x.MenuId == item.TaskId).FirstOrDefault();
                    if (roletask != null && (roletask.CanView || roletask.CanUpdate || roletask.CanCreate || roletask.CanDelete))
                    {
                        List<DynamicNode> childlistNewNodes = new List<DynamicNode>();
                        DynamicNode nd = new DynamicNode();
                        nd.Title = item.NodeName;



                        if (item.ControllerName != null)
                        {
                            nd.Controller = item.ControllerName.Trim();
                            nd.Action = item.ActionName.Trim();
                        }
                        else
                        {
                            nd.Clickable = false;
                        }

                        nd.Key = item.TaskId.ToString();
                        nd.ParentKey = task.TaskId.ToString();

                        if (i > 1 & listNewNodes.Count>1)
                        {
                            listNewNodes.Add(GetSeparatorNode(task.TaskId.ToString()));
                        }
                        listNewNodes.Add(nd);
                       
                        if (nd.Controller != null )   //&& (cn.GetList<MenuSystem>().Where(x=>x.ControllerName==nd.Controller).Count()<2 )     // Checking if create task available or not
                        {
                            if (Recursive == true)
                            {
                                AddNode(childslistNewNodes, item.TaskId.ToString(), item.ControllerName.Trim());
                            }//Adding Site Map path for the task.
                            else
                            {
                                AddNode(listNewNodes, item.TaskId.ToString(), item.ControllerName.Trim());
                            }
                        }
                        if (Recursive == true)
                        {
                            childslistNewNodes.Add(nd);
                            if (i < taskList.Count())
                                childslistNewNodes.Add(GetSeparatorNode(task.TaskId.ToString()));
                        }
                        GetDynamicNodeByTask(item, childlistNewNodes, true);                       
                    }
                    i++;
                }
            }
            cn.Close();
            }
        }
    }
}