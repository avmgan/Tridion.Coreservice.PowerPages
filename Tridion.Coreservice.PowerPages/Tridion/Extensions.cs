using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;


namespace Tridion.Coreservice.PowerPages.Tridion
{
    /// <summary>
    /// Common Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Parse date based on the current site language
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime ParseDateBasedOnLanguage(this string date)
        {
            var returnValue = DateTime.MinValue;
            DateTime.TryParseExact(date, new string[] { "d MMM yy", "d MMMM yy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out returnValue);
            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="languageCode"></param>
        /// <returns></returns>
        public static string ToFormatNumber(this long amount, string languageCode)
        {
            return double.Parse(amount.ToString()).ToFormatNumber(languageCode);
        }

        public static string ToFormatNumber(this double amount, string languageCode)
        {

            var cultureInfo = CultureInfo.GetCultureInfo(languageCode);


            if (Math.Floor(amount) == amount)
                return amount.ToString("N0", cultureInfo);

            return amount.ToString("N2", cultureInfo);
        }

        /// <summary>
        /// Convert date to timezone
        /// </summary>
        /// <param name="timeZone"></param>
        /// <returns></returns>
        public static DateTime ToGMTTimeZone(this string timeZone)
        {
            var convertedDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(timeZone))
                return convertedDate;
            timeZone = timeZone.ToLowerInvariant().Replace("gmt", string.Empty);
            var hourAndMinutes = timeZone.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (hourAndMinutes.Any())
            {

                convertedDate = convertedDate.AddHours(int.Parse(hourAndMinutes[0]));
                if (hourAndMinutes.Count() > 1)
                {
                    convertedDate = convertedDate.AddMinutes(int.Parse(hourAndMinutes[1]));
                }
            }
            return convertedDate;
        }

 
        /// <summary>
        /// Gets the model sent from controller.
        /// </summary>
        /// <typeparam name="T">Model type</typeparam>
        /// <param name="page">The WebViewPage.</param>
        public static void GetModel<T>(this WebViewPage<T> page) where T : class
        {
            var models = page.ViewContext.TempData.Where(item => item.Value is T);
            string modelName = string.Empty;
            if (models.Any())
            {
                modelName = models.First().Key.ToString();
                var model = (T)models.First().Value;

                page.ViewData.Model = model;
                page.Html.ViewData.Model = model;
                page.ViewContext.ViewData.Model = model;

                page.ViewContext.TempData.Remove(models.First().Key);
            }

            var modelStates = page.ViewContext.TempData.Where(item => item.Key == string.Concat(modelName, "-ModelState")).ToList();

            if (modelStates.Any())
            {
                page.ViewData.ModelState.Clear();
                page.Html.ViewData.ModelState.Clear();
                page.ViewContext.ViewData.ModelState.Clear();

                if (modelStates.FirstOrDefault().Value is ModelStateDictionary)
                    foreach (var item in (ModelStateDictionary)modelStates.First().Value)
                    {
                        page.ViewData.ModelState.Add(item);
                        page.Html.ViewData.ModelState.Add(item);
                        page.ViewContext.ViewData.ModelState.Add(item);
                    }

                page.ViewContext.TempData.Remove(modelStates.First().Key);
            }

            var modelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(T));

            page.ViewData.ModelMetadata = modelMetadata;
            page.Html.ViewData.ModelMetadata = modelMetadata;
            page.ViewContext.ViewData.ModelMetadata = modelMetadata;
        }

        /// <summary>
        /// Sets the model for transfer on view.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller">The controller.</param>
        /// <param name="model">The model.</param>
        public static void SetModel<T>(this System.Web.Mvc.Controller controller, T model) where T : class
        {
            var modelId = Guid.NewGuid().ToString();
            controller.TempData.Add(modelId, model);
            controller.TempData.Add(string.Concat(modelId, "-ModelState"), controller.ModelState);
        }


        /// <summary>
        /// Renders the partial view to string.
        /// </summary>
        /// <param name="controller">The controller.</param>
        /// <param name="viewName">Name of the view.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static string RenderPartialViewToString(this Controller controller, string viewName, object model)
        {
            controller.ViewData.Model = model;

            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.RouteData.GetRequiredString("action");
            }

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
                var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

    }
}