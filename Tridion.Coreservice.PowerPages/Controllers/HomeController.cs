using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Xml.Linq;
using Tridion.ContentManager.CoreService.Client;
using Tridion.Coreservice.PowerPages.Tridion;
using Tridion.Coreservice.PowerPages.ViewModels;
using Tridion.CoreServices.PowerPages.Tridion;

namespace Tridion.Coreservice.PowerPages.Controllers
{
    public class HomeController : Controller
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private CoreServiceClient Client
        {
            get
            {
                return CoreService.GetClient("localhost:7086", "username", "password", false);
            }
        }

        /// <summary>
        /// /Home/Index
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Index()
        {
            return View(new FindAndReplaceViewModel());
        }

        // Declare the regex and match as class level variables
        public Regex Regex { get; set; }
        public Match Match { get; set; }


        [HttpPost]
        public ActionResult Index(FindAndReplaceViewModel model)
        {
            model.ServerErrorMessage = string.Empty;

            if (ModelState.IsValid)
            {
                ReadOptions ro = new ReadOptions { LoadFlags = LoadFlags.None };

                OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData()
                {
                    ItemTypes = new[] { ItemType.Component },
                    ComponentTypes = new[] { ComponentType.Normal },
                    Recursive = true,
                };

                try
                {
                    XElement xmlList = Client.GetListXml(model.FolderId, filter);

                    // Load all components based on the folderId
                    List<ComponentData> componentsList = (from itemId in xmlList.Elements().Attributes(XName.Get("ID", String.Empty))
                                                          select (ComponentData)Client.Read(itemId.Value, ro)).ToList<ComponentData>();

                    model.ComponentIdsMatchedListForFindAndReplace = new List<Component>();
                    foreach (var component in componentsList.Where(x => x.Content != "" && x.BluePrintInfo.IsShared == false && x.ComponentType.Value != ComponentType.Multimedia))
                    {
                        if (FindText(component.Content, model.SearchText, model.Matchcase))
                        {
                            log.Info(string.Format("Find and Replace Text Match component ID {0}, Title {1}", component.Id, component.Title));
                            model.ComponentIdsMatchedListForFindAndReplace.Add(new Component { Id = component.Id, Title = component.Title });
                        }
                    }

                    log.Info(string.Format("No of matched components {0}", model.ComponentIdsMatchedListForFindAndReplace.Count()));

                    Session["ComponentsListReplace"] = model.ComponentIdsMatchedListForFindAndReplace;
                    Session["SearchText"] = model.SearchText;
                    Session["ReplaceText"] = model.ReplaceText;
                    Session["Matchcase"] = model.Matchcase;

                    model.IsSuccess = true;
                    model.IsReplaceCompleted = false;
                    this.SetModel<FindAndReplaceViewModel>(model);
                    return Redirect(Request.Url.AbsolutePath);

                }
                catch (Exception ex)
                {
                    //model.ServerErrorMessage = "Error processing the items...";
                    log.Error("Index - Find and Replace Post Method Error", ex);
                }

            }

            this.SetModel<FindAndReplaceViewModel>(model);
            return Redirect(Request.Url.PathAndQuery);

        }

        [HttpPost]
        public ActionResult ReplaceAll(FindAndReplaceViewModel model)
        {
            ReadOptions ro = new ReadOptions { LoadFlags = LoadFlags.None };

            model.ComponentIdsMatchedListForFindAndReplace = (List<Component>)Session["ComponentsListReplace"];

            model.SearchText = Session["SearchText"].ToString();
            model.ReplaceText = Session["ReplaceText"].ToString();
            model.Matchcase = (bool)Session["Matchcase"];
            model.ComponentIdsNotReplaced = new List<Component>();

            foreach (var component in model.ComponentIdsMatchedListForFindAndReplace)
            {
                try
                {
                    var readOptions = new ReadOptions();
                    ComponentData comp = (ComponentData)Client.CheckOut(component.Id, true, ro);
                    comp.Content = ReplaceAll(comp.Content, model.SearchText, model.ReplaceText, model.Matchcase);
                    Client.Save(comp, ro);
                    Client.CheckIn(component.Id, true, "", ro);
                    log.Info(string.Format("Successfully replaced on component ID {0}, Title {1}", comp.Id, comp.Title));
                }
                catch (Exception ex)
                {
                    model.ComponentIdsNotReplaced.Add(new Component { Id = component.Id, Title = component.Title });
                    log.Error("Unable to update the component", ex);
                }

            }

            model.IsSuccess = true;
            model.IsReplaceCompleted = true;
            this.SetModel<FindAndReplaceViewModel>(model);
            return Redirect("/Home/Index?success=true");

        }

        /// <summary>
        /// /Home/Publish
        /// </summary>
        /// <returns></returns>
        public ActionResult Publish()
        {
            return View(new PublishViewModel());
        }

        [HttpPost]
        public ActionResult Publish(PublishViewModel model)
        {
            model.ServerErrorMessage = string.Empty;

            if (ModelState.IsValid)
            {
                ReadOptions ro = new ReadOptions { LoadFlags = LoadFlags.None };

                SearchQueryData searchfilter = new SearchQueryData()
                {
                    SearchIn = new LinkToIdentifiableObjectData { IdRef = "tcm:5-1-2" },
                    ItemTypes = new[] { ItemType.Component },
                    ModifiedAfter = model.FromDateModified,
                    ModifiedBefore = model.ToDateModified,
                    SearchInSubtree = true,
                    FullTextQuery = "",
                };

                try
                {
                    XElement xmlSearchResult = Client.GetSearchResultsXml(searchfilter);

                    model.ComponentsList = new List<Component>();

                    // Load all components based on the folderId
                    List<ComponentData> componentsList = (from itemId in xmlSearchResult.Elements().Attributes(XName.Get("ID", String.Empty))
                                                          select (ComponentData)Client.Read(itemId.Value, ro)).ToList<ComponentData>();
                    foreach (var component in componentsList)
                    {
                        log.Info(string.Format("Datemodified Search Match component ID {0}, Title {1}", component.Id, component.Title));
                        model.ComponentsList.Add(new Component { Id = component.Id, Title = component.Title });
                    }

                    log.Info(string.Format("No of components matched for this search query {0}", model.ComponentsList.Count()));

                    Session["ComponentsListPublish"] = model.ComponentsList;

                    model.IsSuccess = true;
                    model.IsPublishCompleted = false;
                    this.SetModel<PublishViewModel>(model);
                    return Redirect(Request.Url.AbsolutePath);

                }
                catch (Exception ex)
                {
                    log.Error("Publish - Post Method Error", ex);
                }

            }

            this.SetModel<PublishViewModel>(model);
            return Redirect(Request.Url.PathAndQuery);

        }

        [HttpPost]
        public ActionResult PublishAll(PublishViewModel model)
        {
            model.ServerErrorMessage = string.Empty;
            string targetTcmIdToPublish = "tcm:0-2-65538";

            ReadOptions ro = new ReadOptions { LoadFlags = LoadFlags.None };

            try
            {
                model.ComponentsList = (List<Component>) Session["ComponentsListPublish"];

                PublishInstructionData instruction = new PublishInstructionData
                {
                    ResolveInstruction = new ResolveInstructionData() { IncludeChildPublications = false },
                    RenderInstruction = new RenderInstructionData()
                };

                Client.Publish(model.ComponentsList.Select(x => x.Id).ToArray(), instruction, new[] { targetTcmIdToPublish }, PublishPriority.Normal, ro);


                model.IsSuccess = true;
                model.IsPublishCompleted = true;
            }
            catch (Exception ex)
            {
                log.Error("PublishAll - Post Method Error", ex);
            }

            this.SetModel<PublishViewModel>(model);
            return Redirect("/Home/Publish?success=true");

        }

        private string ReplaceAll(string contentText, string searchText, string newReplaceText, bool matchCase)
        {
            Regex replaceRegex = GetRegExpression(searchText, matchCase);
            String replacedString;

            // get the replaced string
            replacedString = replaceRegex.Replace(contentText, newReplaceText);

            // Is the text changed?
            if (contentText != replacedString)
                contentText = replacedString;
            //else // inform user if no replacements are made
            // Console.WriteLine("Cannot find '{0}", searchText);

            return contentText;
        }

        /// <summary>
        /// finds the text in searchTextBox in contentTextBox
        /// </summary>
        private bool FindText(string contentText, string searchText, bool matchCase)
        {
            Regex = GetRegExpression(searchText, matchCase);

            Match = Regex.Match(contentText);

            // found a match?
            if (Match.Success)
                return true;
            else // didn't find? bad luck.
                return false;
        }

        /// <summary>
        /// This function makes and returns a RegEx object
        /// depending on user input
        /// </summary>
        /// <returns></returns>
        private Regex GetRegExpression(string searchText, bool matchCase)
        {
            Regex result;
            String regExString;

            // Get what the user entered
            regExString = searchText;

            regExString = regExString.Replace("*", @"\w*");     // multiple characters wildcard (*)
            regExString = regExString.Replace("?", @"\w");      // single character wildcard (?)

            // if wild cards selected, find whole words only
            regExString = String.Format("{0}{1}{0}", @"\b", regExString);

            // Is match case?
            if (matchCase)
            {
                result = new Regex(regExString);
            }
            else
            {
                result = new Regex(regExString, RegexOptions.IgnoreCase);
            }

            return result;
        }

    }
}
