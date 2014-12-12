using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Web.Mvc;
using Dapper;
using DapperExtensions;
using SAMPLE.Models;
using SAMPLE.Models.Utility;
using System.Collections;
using System.Linq;
using SAMPLE.Utils;
using System.Diagnostics;
using BitFactory.Logging;

namespace SAMPLE.Controllers
{
	public class SeminarController : Controller
	{
		private string _connectionString = string.Empty;

		// Constructor
		public SeminarController()
		{
			// Initialize connection string
			_connectionString = SAMPLEDB.ConnectionString;

		}

		// GET: /Seminar/
		public ActionResult Index()
		{
            //Clearing seleted state on Index load (On postback from Create and Edit page)
            ListDataService.ClearSelectedState(
                ListDataService.ProductList, 
                ListDataService.SeminarLocation, 
                ListDataService.VenueType, 
                ListDataService.PreferredLanguage, 
                ListDataService.SeminarStatus, 
                ListDataService.LiteratureList, 
                ListDataService.LiteratureQuantity);

            ListDataService.RefreshLists();
			IEnumerable<Seminar> seminarModels = new List<Seminar>();
            IEnumerable<SeminarLocation> seminarLocationModels = new List<SeminarLocation>();

            SeminarViewModel seminarViewModel = new SeminarViewModel();

			using (IDbConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
                seminarModels = cn.GetList<Seminar>().Where(x => x.SeminarDate.CompareTo(DateTime.Now) >= 0).OrderBy(x => x.SeminarDate).Concat<Seminar>(cn.GetList<Seminar>().Where(x => x.SeminarDate.CompareTo(DateTime.Now) < 0).OrderByDescending(x => x.SeminarDate));
                seminarLocationModels = cn.GetList<SeminarLocation>();                
				cn.Close();
			}

            seminarViewModel.Seminars = seminarModels.ToList();
            seminarViewModel.SeminarLocations = seminarLocationModels.ToList();

            return View(seminarViewModel);
		}

		// GET: /Seminar/Details/7
		public ActionResult Details(int id)
		{
			Seminar seminarModel = new Seminar();
            SeminarLocation seminarLocation = new SeminarLocation();

            string[] productIds;

            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                seminarModel = cn.Get<Seminar>(id);
                if (seminarModel.IsActive == false)
                {
                    return RedirectToAction("Index");
                }
                seminarLocation = cn.Get<SeminarLocation>(seminarModel.SeminarLocationId);    
                productIds = cn.GetList<SeminarProduct>().Where(x => x.SeminarId == id).Select(x => x.ProductId.ToString()).ToArray<string>();
                cn.Close();
            }

            SeminarDetailViewModel seminarDetailViewModel = new SeminarDetailViewModel(seminarModel, seminarLocation, productIds);
            
            return View(seminarDetailViewModel);
		}

		// GET: /Seminar/Create
		public ActionResult Create()
		{
			return View();
		}

		// POST: /Seminar/Create
		[HttpPost]
		public ActionResult Create(SeminarViewModel seminarData)
		{
            // the model is not valid => we redisplay the view and show the
            // corresponding error messages so that the user can fix them:
            if (!ModelState.IsValid) return View(seminarData);

            int seminarId = -1;
            seminarData.Seminar.DateTimeStampRecord(DbChangeType.Create);
            seminarData.Seminar.CmsCertified = false;

            try
            {                
                using (var cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    seminarData.Seminar.IsActive = true;
                    seminarId = cn.Insert<Seminar>(seminarData.Seminar);
                    seminarData.Seminar.SeminarId = seminarId;
                    cn.Close();
                }

                // Save any products
                SaveSelectedProducts(seminarData, seminarId);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ConfigLogger.Instance.LogError(ex); 
                return View();
            }
		}

        private void SaveSelectedProducts(SeminarViewModel seminarData, int ownerId)
        {
            // Bail if we don't need to be here
            if (seminarData.ProductIds == null || ownerId <= 0) return;

            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    foreach (string prodId in seminarData.ProductIds)
                    {
                        SeminarProduct sp = new SeminarProduct(ownerId, Convert.ToInt32(prodId));
                        cn.Insert<SeminarProduct>(sp);
                    }
                }
            }
            catch(Exception ex)
            {
                ConfigLogger.Instance.LogError(ex);
                Debugger.Break();
            }
        }

		// GET: /Seminar/Edit/7
		public ActionResult Edit(int id)
		{
            string[] productIds;
            Seminar seminar = new Seminar();

			using (IDbConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				seminar = cn.Get<Seminar>(id);
                if (seminar.IsActive == false)
                {
                    return RedirectToAction("Index");
                }
                productIds = cn.GetList<SeminarProduct>().Where(x => x.SeminarId == id).Select(x=>x.ProductId.ToString()).ToArray<string>();
                cn.Close();
			}

            SeminarViewModel seminarViewModel = new SeminarViewModel(seminar, productIds);
			return View(seminarViewModel);
		}

		// POST: /Seminar/Edit/7
		[HttpPost]
		public ActionResult Edit(int id, SeminarViewModel seminarData)
		{
            // the model is not valid => we redisplay the view and show the
            // corresponding error messages so that the user can fix them:
            if (!ModelState.IsValid) return View(seminarData);
            
            try
            {
                // Just the way it is.. 
                seminarData.Seminar.SeminarId = id;
                seminarData.Seminar.DateTimeStampRecord(DbChangeType.Update);

                using (var cn = new SqlConnection(_connectionString))
                {
                    cn.Open();

                    // Update Seminar ... this is ok since the id already exists
                    var x = cn.Update<Seminar>(seminarData.Seminar);

                    if (seminarData.ProductIds != null)
                    {
                        // Delete Any Product Changes
                        var predicate = Predicates.Field<SeminarProduct>(z=>z.SeminarId, Operator.Eq, id);
                        var p = cn.Delete<SeminarProduct>(predicate);

                        // Re-add the list
                        foreach (string pid in seminarData.ProductIds)
                        {
                            SeminarProduct sp = new SeminarProduct(id, Convert.ToInt32(pid));
                            cn.Insert<SeminarProduct>(sp);
                        }
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ConfigLogger.Instance.LogError(ex); 
                return View(seminarData);
            }
		}

		// GET: /Seminar/Delete/7
		public ActionResult Delete(int id)
		{
			Seminar seminarModel = new Seminar();

			using (IDbConnection cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				seminarModel = cn.Get<Seminar>(id);
				cn.Close();
			}

			return View(seminarModel);
		}

                //public string[] productIds { get; set; }

		// POST: /Seminar/Delete/7
		[HttpPost]
		public ActionResult Delete(int id, Seminar seminarData)
		{
			Seminar seminarModel = new Seminar {SeminarId = id};
			try
			{
				using (IDbConnection cn = new SqlConnection(_connectionString))
				{
					cn.Open();
					cn.Delete<Seminar>(seminarModel);
					cn.Close();
				}

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
                ConfigLogger.Instance.LogError(ex);
                return View(seminarData);
			}
		}

       
        public JsonResult GetSeminarLocation(int seminarLocationId = 0)
        {
            IEnumerable<SeminarLocation> seminarLocModel = new List<SeminarLocation>();
            try
            {
                using (IDbConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    seminarLocModel = cn.GetList<SeminarLocation>();
                    cn.Close();
                }

                foreach (SeminarLocation sml in seminarLocModel)
                {
                    sml.Website = ListDataService.StateList.GetItemText(sml.StateId);
                }
                return Json(seminarLocModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ConfigLogger.Instance.LogError(ex);
                return Json(seminarLocModel, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Attendees(int seminarId)
        {
            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                ViewBag.Seminar = cn.Get<Seminar>(seminarId).Name;
                cn.Close();
                return View();
            }
        
        }

        public JsonResult GetSeminarAttendees(int seminarId)
        {
            IEnumerable<SeminarAttendees> attendeesModels = new List<SeminarAttendees>();
            try
            {
                
                using (IDbConnection cn = new SqlConnection(_connectionString))
                {
                    cn.Open();
                    attendeesModels = cn.Query<SeminarAttendees>("GetSeminarAttendees", new {seminarId = seminarId }, commandType:CommandType.StoredProcedure);
                    cn.Close();
                    ViewBag.Seminar = attendeesModels.Select(x => x.Name).FirstOrDefault();
                }

            }
            catch(Exception ex)
            {
              
            }
            return Json(attendeesModels, JsonRequestBehavior.AllowGet);
        }

        public JsonResult UpdateSeminarReservation(SeminarAttendees seminarAttendees)
        {
            SeminarReservation seminarReservation = new SeminarReservation();
            using (IDbConnection cn = new SqlConnection(_connectionString))
            {
                cn.Open();
                seminarReservation = cn.GetList<SeminarReservation>().Where(x => x.ClientId ==seminarAttendees.ClientId && x.SeminarId ==seminarAttendees.SeminarId).FirstOrDefault();
                seminarReservation.Attended = seminarAttendees.Attended;
                seminarReservation.DateTimeStampRecord(DbChangeType.Update);
                cn.Update<SeminarReservation>(seminarReservation);
                cn.Close();
             
            }
            return Json(seminarAttendees, JsonRequestBehavior.AllowGet);
        }
        
    }
}
