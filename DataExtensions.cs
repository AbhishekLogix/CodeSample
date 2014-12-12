using SAMPLE.Models;
using SAMPLE.Models.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace SAMPLE.Utils
{
    // TODO: Separate this file out to logical file sections...
    // TODO: Move the ListDataService Extensions to their own space..
    //        Right now, this is kind of a dumping ground for most extensions :(

    public enum DbChangeType
    {
        Create,
        Update,
        Delete
    }

    public static class DataExtensions
    {
        /// <summary>
        /// Prep a CategoryDetail Item or ListDetail item for use with MVC Html Dropdowns
        /// </summary>
        /// <param name="cx"></param>
        /// <returns></returns>
        public static IList<SelectListItemExActive> ToSelectList(this IEnumerable<ListTypeDetail> cx)
        {
            var x = cx.Select(k => new SelectListItemExActive { Selected = false, Text = k.ItemText, Value = k.ListTypeDetailId.ToString(), IsActive = k.IsActive });
            return x.ToList();
        }

        /// <summary>
        /// Split a camel case string into individual parts.
        /// <para>trimEndString arg will be replaced with string.empty</para>
        /// <para>before splitting string.</para>
        /// </summary>
        /// <param name="str"></param>
        /// <param name="string2Trim"></param>
        /// <returns></returns>
        public static string SplitCamelCase(this string str, IList<string> strings2Trim = null)
        {
            string splitVal = Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");

            if (strings2Trim != null)
            {
                foreach (string item2Trim in strings2Trim)
                {
                    if (splitVal.EndsWith(item2Trim))
                    {
                        splitVal = splitVal.TrimEnd(item2Trim.ToCharArray());
                        break;
                    }
                }
            }

            return splitVal.Trim();
        }

        public static string GetItemText(this IEnumerable<SelectListItem> baseList, int? itemId, string defaultReturnText = "")
        {
            if (itemId <= 0 || itemId == null) return defaultReturnText;

            var selectItem = baseList.FirstOrDefault(f => f.Value == itemId.ToString());

            defaultReturnText = string.IsNullOrEmpty(defaultReturnText) ? "*Not Found" : defaultReturnText;

            return selectItem == null ? defaultReturnText : selectItem.Text;
        }

        public static string GetItemText(this IEnumerable<SelectListItem> baseList, string itemId, string defaultReturnText = "")
        {
            int id;
            if (!int.TryParse(itemId, out id)) return defaultReturnText;
            if (id <= 0) return defaultReturnText;

            var selectItem = baseList.FirstOrDefault(f => f.Value == itemId);

            defaultReturnText = string.IsNullOrEmpty(defaultReturnText) ? "*Not Found" : defaultReturnText;

            return selectItem == null ? defaultReturnText : selectItem.Text;
        }

        //public static IEnumerable<SelectListItem> TrimByRole(this IEnumerable<SelectListItemExRoleRestricted> baseList, string roleName)
        //{
        //    IEnumerable<SelectListItem> selectList = new List<SelectListItem>();
        //    return selectList;
        //}

        /// <summary>
        /// Resets any selected properties to false
        /// </summary>
        /// <param name="baseList"></param>
        public static void ClearSelectedState(this IEnumerable<SelectListItem> baseList)
        {
            foreach (var item in baseList)
            {
                item.Selected = false;
            }
        }
        
        public static void DateTimeStampRecord(this IAuditTrack baseRecord, DbChangeType changeType)
        {
            int userId = Convert.ToInt32(System.Web.HttpContext.Current.Session["SampleId"].ToString());

            if (userId <= 0) throw new ArgumentOutOfRangeException("Application UserId must be greater than zero.");

            bool isExistingRecord = baseRecord.CreateDate < new DateTime(1930, 1, 1) || baseRecord.CreateById <= 0;

            DateTime dtStamp = DateTime.UtcNow;

            if (changeType == DbChangeType.Create || isExistingRecord)
            {
                baseRecord.CreateById = userId;
                baseRecord.CreateDate = dtStamp;
            }

            baseRecord.LastModById = userId;
            baseRecord.LastModDate = dtStamp;
        }

        /// <summary>
        /// Fill a NoteDataPacket for the LeadDetail page note section javascript
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public static NoteDataPacket FillNoteDataPacket(this ClientNote note)
        {
            NoteDataPacket noteData = new NoteDataPacket 
            { 
                AuthorId = note.AuthorId,
                AuthorName = PersonUtils.GetAppUserFullName(note.AuthorId),
                AuthorTitle = PersonUtils.GetAppUserTitle(note.AuthorId),
                ClientConsent = ListDataService.ConsentType.GetItemText(note.ClientConsentId) ,
                ClientId = note.ClientId,
                ClientNoteId = Convert.ToInt32(note.ClientNoteId),
                ClientProgress = ListDataService.ClientStage.GetItemText(note.ClientProgressId),
                NoteDate = note.NoteDate,
                NoteText = note.NoteText,
                NoteTypeId = note.NoteTypeId
            };

            return noteData;
        }

        /// <summary>
        /// Fill and Return an IEnumerable list of AgentData for javascript calls.
        /// </summary>
        /// <param name="appUsers"></param>
        /// <returns></returns>
        public static IEnumerable<AgentData> FillAgentData(this IEnumerable<AppUser> appUsers, int currUserId = 0)
        {
            IEnumerable<AgentData> agentData = new List<AgentData>();

            if(currUserId > 0)
                agentData = appUsers.Where(k => k.AppUserId != currUserId).Select(k => new AgentData { FullName = k.FirstName + " " + k.LastName, UserName = k.UserName, AppUserId = k.AppUserId, RoleId = k.AppUserRoleId });
            else
                agentData = appUsers.Select(k => new AgentData { FullName = k.FirstName + " " + k.LastName, UserName = k.UserName, AppUserId = k.AppUserId, RoleId = k.AppUserRoleId });

            return agentData;
        }

        //public static IEnumerable<ClientDashVm> ToClientExList(this IEnumerable<Client> clients)
        //{
        //    List<ClientDashVm> clientExList = new List<ClientDashVm>();

        //    foreach (var item in clients)
        //    {
        //        ClientDashVm clientEx = new ClientDashVm(item);
        //        clientExList.Add(clientEx);
        //    }

        //    return clientExList;
        //}

        public static IEnumerable<SelectListItemEx> FilteredBy(this IEnumerable<SelectListItemEx> listData, int filterIdVal)
        {
            return listData.Where(k => k.FilterId == filterIdVal);
        }

        public static string ToCamelText(this Enum enumValue)
        {
            return enumValue.ToString().SplitCamelCase();
        }

        public static string ToDelimitedString(this string[] arrayData, char delimiter = ',')
        {
            string delimitedVal = string.Empty;

            for (int i = 0; i < arrayData.Length; i++)
                delimitedVal += arrayData[i] + delimiter;

            delimitedVal = delimitedVal.TrimEnd(delimiter);

            return delimitedVal;
        }

        public static string ToDelimitedString(this IEnumerable<string> listData, char delimiter = ',')
        {
            string delimitedVal = string.Empty;

            foreach (var item in listData)
                delimitedVal += item + delimiter;

            delimitedVal = delimitedVal.TrimEnd(delimiter);

            return delimitedVal;
        }

        public static string ToDelimitedString(this int[] arrayData, char delimiter = ',')
        {
            string delimitedVal = string.Empty;

            for (int i = 0; i < arrayData.Length; i++)
                delimitedVal += arrayData[i].ToString() + delimiter;

            delimitedVal = delimitedVal.TrimEnd(delimiter);

            return delimitedVal;
        }

        public static string ToDelimitedString(this IEnumerable<int> listData, char delimiter = ',')
        {
            string delimitedVal = string.Empty;

            foreach (var item in listData)
                delimitedVal += item.ToString() + delimiter;

            delimitedVal = delimitedVal.TrimEnd(delimiter);

            return delimitedVal;
        }

        /// <summary>
        /// Parses an int? to its int32 value. Returns zero (0) if null.
        /// </summary>
        /// <param name="nullable2Parse"></param>
        /// <returns>int32 value or zero (0) if null</returns>
        public static int ToInt32(this int? nullable2Parse)
        {
            if (nullable2Parse == null) return 0;
            return Convert.ToInt32(nullable2Parse);
        }


        #region Dynamic conversion of string type to other datatype(Used in Enrollment Form) 
        /// <summary>
        /// Parses an string to its int32 value. Returns zero (0) if null or not numeric.
        /// </summary>
        /// <param name="nullable2Parse"></param>
        /// <returns>int32 value or zero (0) if null</returns>
        public static int ToInt32(this string nullable2Parse)
        {
            if (nullable2Parse == null) return 0;
            try
            {
                return Convert.ToInt32(nullable2Parse);
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// Parses an string to bool value. Returns flase if null or not bool value.
        /// </summary>
        /// <param name="nullable2Parse"></param>
        /// <returns>bool value or false if null or not bool</returns>
        public static bool ToBoolean(this string nullable2Parse)
        {
            if (nullable2Parse == null) return false;
            try
            {
                return Convert.ToBoolean(nullable2Parse);
            }
            catch
            {
                return false;
            }
        } 
        #endregion

        /// <summary>
        /// Return bool value as a 'Yes' or 'No' string
        /// </summary>
        /// <param name="boolValue"></param>
        /// <returns></returns>
        public static string ToYesNo(this bool boolValue)
        { 
            return boolValue ? "Yes" : "No";
        }

        /// <summary>
        /// Return nullable bool value as a 'Yes' or 'No' string
        /// </summary>
        /// <param name="boolValue"></param>
        /// <returns></returns>
        public static string ToYesNo(this bool? boolValue)
        {
            if(boolValue == null)
                return "No";
            else
                return (bool)boolValue ? "Yes" : "No";
        }

        #region Kindly lifted from here (open source):
        //  http://sympletech.com

        public static T Parse<T>(this string thingToParse)
        {
            var retType = typeof(T);
            var tParse = retType.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, null,
                                            new[] { typeof(string), retType.MakeByRefType() }, null);

            if (tParse != null)
            {
                var parameters = new object[] { thingToParse, null };
                var success = (bool)tParse.Invoke(null, parameters);
                if (success)
                {
                    return (T)parameters[1];
                }
            }

            return default(T);
        }

        public static T Parse<T>(this string thingToParse, Func<string, T> parser)
        {
            return parser.Invoke(thingToParse);
        }
    
        #endregion    

        #region Kindly lifted from here (open source): 
        // http://blog.cincura.net/229310-sorting-in-iqueryable-using-string-as-column-name/

        private static IOrderedQueryable<T> OrderingHelper<T>(IQueryable<T> source, string propertyName, bool descending, bool anotherLevel)
        {
            ParameterExpression param = Expression.Parameter(typeof(T), string.Empty); // I don't care about some naming
            MemberExpression property = Expression.PropertyOrField(param, propertyName);
            LambdaExpression sort = Expression.Lambda(property, param);

            MethodCallExpression call = Expression.Call(
                typeof(Queryable),
                (!anotherLevel ? "OrderBy" : "ThenBy") + (descending ? "Descending" : string.Empty),
                new[] { typeof(T), property.Type },
                source.Expression,
                Expression.Quote(sort));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
        }

        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            return OrderingHelper(source, propertyName, false, false);
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
        {
            return OrderingHelper(source, propertyName, true, false);
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string propertyName)
        {
            return OrderingHelper(source, propertyName, false, true);
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string propertyName)
        {
            return OrderingHelper(source, propertyName, true, true);
        }

        #endregion
    }
}