using DAL;
using static DAL.DB;
using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using static Controllers.AccessControl;

[UserAccess(Access.View)]
public class MediasController : Controller
{

    private void InitSessionVariables()
    {
        // Session is a dictionary that hold keys values specific to a session
        // Each user of this web application have their own Session
        // A Session has a default time out of 20 minutes, after time out it is cleared

        if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
        if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
        if (Session["Search"] == null) Session["Search"] = false;
        if (Session["SearchString"] == null) Session["SearchString"] = "";
        if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
        if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
        if (Session["SortByTitle"] == null) Session["SortByTitle"] = true;
        if (Session["MediaSortBy"] == null) Session["MediaSortBy"] = MediaSortBy.PublishDate;
        if (Session["SortAscending"] == null) Session["SortAscending"] = false;
        ValidateSelectedCategory();

        // paging handling
        if (Session["pageNum"] == null) Session["pageNum"] = 1;
        if (Session["firstPageSize"] == null) Session["firstPageSize"] = 12;
        if (Session["pageSize"] == null) Session["pageSize"] = 3;
        if (Session["EndOfMedias"] == null) Session["EndOfMedias"] = false;
    }

    private void ResetMediasPaging()
    {
        Session["pageNum"] = 1;
        Session["EndOfMedias"] = false;
    }
    private List<Media> _getItems(int index, int nbItems)
    {
        try
        {
            IEnumerable<Media> result = null;

            InitSessionVariables();

            bool search = (bool)Session["Search"];
            string searchString = (string)Session["SearchString"];

            if (Models.User.ConnectedUser.IsAdmin)
                result = DB.Medias.ToList();
            else
                result = DB.Medias.ToList().Where(c => c.Shared || Models.User.ConnectedUser.Id == c.OwnerId);

            if (search)
            {
                result = result.Where(c => (c.Title.ToLower() + c.Description.ToLower()).Contains(searchString));

                string SelectedCategory = (string)Session["SelectedCategory"];
                if (SelectedCategory != "")
                    result = result.Where(c => c.Category == SelectedCategory);
            }


            if ((bool)Session["SortAscending"])
            {
                switch ((MediaSortBy)Session["MediaSortBy"])
                {
                    case MediaSortBy.Title:
                        result = result.OrderBy(c => c.Title); break;
                    case MediaSortBy.PublishDate:
                        result = result.OrderBy(c => c.PublishDate); break;
                }
            }
            else
            {
                switch ((MediaSortBy)Session["MediaSortBy"])
                {
                    case MediaSortBy.Title:
                        result = result.OrderByDescending(c => c.Title); break;
                    case MediaSortBy.PublishDate:
                        result = result.OrderByDescending(c => c.PublishDate); break;
                }
            }
            if (result.Count() < nbItems + index)
            {
                nbItems = result.Count() - index;
                Session["EndOfMedias"] = true;
            }
            return result.Skip(index).Take(nbItems).ToList();
        }
        catch (System.Exception ex)
        {
            return null;
        }
    }

    // /Medias/SetFirstPageSize?pageSize =
    public ActionResult SetFirstPageSize(int pageSize)
    {
        Session["firstPageSize"] = pageSize;
        return null; //  no need to respond
    }

    public ActionResult getNextMediasPage()
    {
        bool EndOfMedias = (bool)Session["EndOfMedias"];
        if (!EndOfMedias)
        {
            Session["pageNum"] = (int)Session["pageNum"] + 1;
            int pageNum = (int)Session["pageNum"];
            int pageSize = (int)Session["pageSize"];
            int firstPageSize = (int)Session["firstPageSize"];
            Debug.WriteLine("PageNum: " + pageNum);
            IEnumerable<Media> mediasPage = _getItems(
                pageNum == 1 ? 0 : (pageNum - 2) * pageSize + firstPageSize,
                pageNum == 1 ? firstPageSize : pageSize);
            return PartialView("GetMedias", mediasPage);
        }
        return null;
    }

    public ActionResult EndOfMedias()
    {
        bool EndOfMedias = (bool)Session["EndOfMedias"];
        return Json(EndOfMedias, JsonRequestBehavior.AllowGet);

    }

    private void ResetCurrentMediaInfo()
    {
        Session["CurrentMediaId"] = 0;
        Session["CurrentMediaTitle"] = "";
    }

    private void ValidateSelectedCategory()
    {
        if (Session["SelectedCategory"] != null)
        {
            var selectedCategory = (string)Session["SelectedCategory"];
            var Medias = DB.Medias.ToList().Where(c => c.Category == selectedCategory);
            if (Medias.Count() == 0)
                Session["SelectedCategory"] = "";
        }
    }

    public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            bool search = (bool)Session["Search"];

            if (search)
            {
                return PartialView();
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }
    // This action produce a partial view of Medias
    // It is meant to be called by an AJAX request (from client script)
    public ActionResult GetMediaDetails(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            int mediaId = (int)Session["CurrentMediaId"];
            Media Media = DB.Medias.Get(mediaId);
            if (DB.Users.HasChanged || DB.Medias.HasChanged || forceRefresh)
            {
                return PartialView(Media);
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }
    public ActionResult GetMedias(bool forceRefresh = false)
    {
        {
            try
            {
                if (DB.Users.HasChanged ||
                    DB.Medias.HasChanged ||
                    forceRefresh)
                {
                    InitSessionVariables();
                    int pageNum = (int)Session["pageNum"];
                    int pageSize = (int)Session["pageSize"];
                    int firstPageSize = (int)Session["firstPageSize"];
                    return PartialView(_getItems(0, pageNum > 1 ? (pageNum - 1) * pageSize + firstPageSize : firstPageSize));
                }
                return null;
            }
            catch (System.Exception ex)
            {
                return Content("Erreur interne" + ex.Message, "text/html");
            }
        }
    }
    public ActionResult List()
    {
        ResetCurrentMediaInfo();
        return View();
    }
    public ActionResult ToggleSearch()
    {
        ResetMediasPaging();
        if (Session["Search"] == null) Session["Search"] = false;
        Session["Search"] = !(bool)Session["Search"];
        return RedirectToAction("List");
    }
    public ActionResult SetMediaSortBy(MediaSortBy mediaSortBy)
    {      // /Medias/SetMediasSortBy?mediaSortBy= 
        ResetMediasPaging();
        Session["MediaSortBy"] = mediaSortBy;
        return RedirectToAction("List");
    }
    public ActionResult ToggleMediaSort()
    {
        ResetMediasPaging();
        int mediaSortBy = (int)Session["MediaSortBy"] + 1;
        if (mediaSortBy >= Enum.GetNames(typeof(MediaSortBy)).Length) mediaSortBy = 0;
        Session["MediaSortBy"] = mediaSortBy;
        return RedirectToAction("List");
    }
    public ActionResult ToggleSort()
    {
        ResetMediasPaging();
        Session["SortAscending"] = !(bool)Session["SortAscending"];
        return RedirectToAction("List");
    }

    public void ToggleMediaLike(int id)
    {
        if (Medias.Get(id).LikedByConnectedUser())
        {
            int likeId =0;

            foreach(Like like in Likes.ToList())
            {
                if(like.MediaID == id && like.UserID == Models.User.ConnectedUser.Id)
                    likeId = like.Id;
            }
            Likes.Delete(likeId);
        }

        else
        {
            Like likeAdd = new Like();
            likeAdd.MediaID = id;
            likeAdd.UserID = Models.User.ConnectedUser.Id;
            Likes.Add(likeAdd);
        }
        
    }
    public ActionResult SetSearchString(string value)
    {
        ResetMediasPaging();
        Session["SearchString"] = value.ToLower();
        return RedirectToAction("List");
    }

    public ActionResult SetSearchCategory(string value)
    {
        ResetMediasPaging();
        Session["SelectedCategory"] = value;
        return RedirectToAction("List");
    }
    public ActionResult About()
    {
        return View();
    }

    public ActionResult Details(int id)
    {
        Session["CurrentMediaId"] = id;
        Media Media = DB.Medias.Get(id);
        Session["UserCanEditCurrentMedia"] = false;
        if (Media != null)
        {
            Session["CurrentMediaTitle"] = Media.Title;
            Session["UserCanEditCurrentMedia"] = Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin;
            return View(Media);
        }
        return RedirectToAction("List");
    }
    [UserAccess(Access.Write)]
    public ActionResult Create()
    {
        return View(new Media());
    }

    [HttpPost]
    [UserAccess(Access.Write)]
    [ValidateAntiForgeryToken()]
    public ActionResult Create(Media Media, string sharedCB = "off")
    {
        if (Media.IsValid())
        {
            Media.OwnerId = Models.User.ConnectedUser.Id;
            Media.Shared = sharedCB == "on";
            DB.Medias.Add(Media);
            DB.Events.Add("Create", Media.Title);
            return RedirectToAction("List");
        }
        DB.Events.Add("Illegal Create Media");
        return Redirect("/Accounts/Login?message=Erreur de creation de Media!&success=false");
    }

    [UserAccess(Access.Write)]
    public ActionResult Edit()
    {
        // Note that id is not provided has a parameter.
        // It use the Session["CurrentMediaId"] set within
        // Details(int id) action
        // This way we prevent from malicious requests that could
        // modify or delete programatically the all the Medias

        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media Media = DB.Medias.Get(id);
            if (Media != null)
            {
                if (Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                    return View(Media);
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    [UserAccess(Access.Write)]
    [HttpPost]
    [ValidateAntiForgeryToken()]
    public ActionResult Edit(Media Media, string sharedCB = "off")
    {
        // Has explained earlier, id of Media is stored server side an not provided in form data
        // passed in the method in order to prever from malicious requests

        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;

        // Make sure that the Media of id really exist
        Media storedMedia = DB.Medias.Get(id);
        if (storedMedia != null)
        {
            Media.Shared = sharedCB == "on";

            // restore non editable fields
            Media.Id = id;
            Media.OwnerId = storedMedia.OwnerId;
            Media.PublishDate = storedMedia.PublishDate;

            if (Media.IsValid())
            {
                DB.Medias.Update(Media);
                return RedirectToAction("Details/" + id);
            }
        }
        DB.Events.Add("Illegal Edit Media");
        return Redirect("/Accounts/Login?message=Erreur de modification de Media!&success=false");
    }

    [UserAccess(Access.Write)]
    public ActionResult Delete()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media Media = DB.Medias.Get(id);
            if (Media != null)
            {
                if (Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                {

                    List<int> likeIds = new List<int>();

                    foreach (Like like in Likes.ToList())
                    {
                        if(like.MediaID == id)
                            likeIds.Add(like.Id);
                    }

                    foreach (int likeId in likeIds)
                        Likes.Delete(likeId);

                    DB.Medias.Delete(id);


                    return RedirectToAction("List");
                }
                return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    // This action is meant to be called by an AJAX request
    // Return true if there is a name conflict
    // Look into validation.js for more details
    // and also into Views/Medias/MediaForm.cshtml
    public JsonResult CheckConflict(string YoutubeId)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        // Response json value true if name is used in other Medias than the current Media
        return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(),
                    JsonRequestBehavior.AllowGet /* must have for CORS verification by client browser */);
    }

}
