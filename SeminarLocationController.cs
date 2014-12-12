using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Web.Mvc;
using DapperExtensions;
using SAMPLE.Models;
using SAMPLE.Utils;
using System.Diagnostics;
using BitFactory.Logging;
using System.Linq;

namespace SAMPLE.Controllers
{
    public class SeminarLocationController : Controller
    {
        private string _connectionString = string.Empty;

        // Constructor
        public SeminarLocationController()
        {
            // Initialize connection string
            _connectionString = SAMPLEDB.ConnectionString;
        }

        // GET: /SeminarLocation/
        public ActionResult Index()
        {
            ListDataService.RefreshLists();
            IEnumerable<SeminarLocation> seminarLocationModels = new List<SeminarLocation>();

            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                seminarLocationModels = cn.GetList<SeminarLocation>();
                cn.Close();
            }

            return View(seminarLocationModels);
        }

        // GET: /SeminarLocation/Details/7
        public ActionResult Details(int id)
        {
            SeminarLocation seminarLocationModel = new SeminarLocation();

            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                seminarLocationModel = cn.Get<SeminarLocation>(id);
                cn.Close();
            }

            return View(seminarLocationModel);
        }

        // GET: /SeminarLocation/Create
        public ActionResult Create()
        {
            ListDataService.SeminarStatus.ClearSelectedState();
            ListDataService.StateList.ClearSelectedState();
            return View();
        }

        // POST: /SeminarLocation/Create
        [HttpPost]
        public ActionResult Create(SeminarLocation seminarLocation)
        {
            // corresponding error messages so that the user can fix them:
            if (!ModelState.IsValid) return View(seminarLocation);

            int ownerId = -1;
            seminarLocation.DateTimeStampRecord(DbChangeType.Create);

            try
            {               
                using (var cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    ownerId = cn.Insert<SeminarLocation>(seminarLocation);
                    cn.Close();
                }
            }
            catch (Exception ex)
            {
                ConfigLogger.Instance.LogError(ex);
                Debugger.Break();
            }

            return RedirectToAction("Index");
        }

        // GET: /SeminarLocation/Edit/7
        public ActionResult Edit(int id)
        {
            SeminarLocation seminarLocationModel = new SeminarLocation();

            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                seminarLocationModel = cn.Get<SeminarLocation>(id);
                cn.Close();
            }

            return View(seminarLocationModel);
        }

        // POST: /SeminarLocation/Edit/7
        [HttpPost]
        public ActionResult Edit(int id, SeminarLocation seminarLocation)
        {
            int ownerId = id;
            seminarLocation.SeminarLocationId = id;
            seminarLocation.DateTimeStampRecord(DbChangeType.Update);

            try
            {
                using (IDbConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    var x = cn.Update<SeminarLocation>(seminarLocation);
                    cn.Close();
                }

                ListDataService.RefreshLists();
            }
            catch(Exception ex)
            {
                ConfigLogger.Instance.LogError(ex);
                Debugger.Break();
                return View();
            }

            return RedirectToAction("Index");
        }

        // GET: /SeminarLocation/Delete/7
        public ActionResult Delete(int id)
        {
            SeminarLocation seminarLocationModel = new SeminarLocation();

            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                seminarLocationModel = cn.Get<SeminarLocation>(id);
                cn.Close();
            }
            
            return View(seminarLocationModel);
        }

        // POST: /SeminarLocation/Delete/7
        [HttpPost]
        public ActionResult Delete(int id, SeminarLocation seminarLocation)
        {
            SeminarLocation seminarLocationModel = new SeminarLocation { SeminarLocationId = id };
            try
            {
                using (IDbConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    cn.Delete<SeminarLocation>(seminarLocationModel);
                    cn.Close();
                }

                ListDataService.RefreshLists();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ConfigLogger.Instance.LogError(ex);
                return View();
            }
        }


        /// <summary>
        /// Checks for a unique seminar location name.
        /// <para>Returns zero(0) for unique name</para>
        /// <para>Returns positive for non-unique names</para>
        /// <para>Returns negative-one(-1) for an empty locationName</para>
        /// </summary>
        /// <param name="locationName"></param>
        /// <returns>
        /// Zero(0) for Unique name
        /// Positive Number for Non-unique name
        /// Negative Number for empty or null locationName
        /// </returns>
        [HttpPost]
        public JsonResult CheckLocationName(string locationName, string orginalEditName = "")
        {
            int nameCount = -1;

            // if locationName is bad, return -1
            if (!string.IsNullOrWhiteSpace(locationName))
            {
                try
                {
                    using (IDbConnection cn = new SqlConnection(SAMPLEDB.ConnectionString))
                    {
                        cn.Open();
                        // Counts on the fact that sql is not case sensitive
                        var predicate = Predicates.Field<SeminarLocation>(x => x.LocationName, Operator.Eq, locationName);

                        if (string.IsNullOrWhiteSpace(orginalEditName))
                        {
                            nameCount = cn.Count<SeminarLocation>(predicate);                        
                        }
                        else
                        {
                            nameCount = cn.GetList<SeminarLocation>(predicate).Where(k => k.LocationName.ToLower() != orginalEditName.Trim().ToLower()).Count();
                        }

                        cn.Close();
                    }
                }
                catch (Exception ex)
                {
                    ConfigLogger.Instance.LogError(ex);
                    nameCount = -1;     // throws an error on the client side
                }            
            }

            return Json(nameCount, JsonRequestBehavior.DenyGet);
        }
    }
}
