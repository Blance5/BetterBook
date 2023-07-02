using Google.Apis.Books.v1;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Threading.Tasks;
using Google.Apis.Books.v1.Data;
using Newtonsoft.Json;
using Microsoft.AspNet.Identity;
using Microsoft.Ajax.Utilities;
using FinalBookProj.Models;
using System.Threading;
using System.Web.UI;
using System.Web;

namespace FinalBookProj.Controllers
{
    public class BooksController : Controller
    {

        public async Task<ActionResult> Search(string query, int? page)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index", "Home", new { error = "Please enter a search query" });
            }

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new {error = "Please log in first"});
            }

            int PageSize = 8;
            int searched = 40;
            //int previousPage = page.HasValue && page > 1 ? page.Value - 1 : 1;
            int currentPage = page.HasValue && page > 0 ? page.Value : 1;
            //int nextPage = page.HasValue && page > 0 ? page.Value + 1 : 2;
            
            string apiKey = ConfigurationManager.AppSettings["GoogleBooksAPIKey"];
            var service = new BooksService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "FinalBookProj"
            });

            SearchViewModel model = new SearchViewModel();

            try
            {
                var request = service.Volumes.List(query);
                var tempResponse = await request.ExecuteAsync();
                var Totalpages = (int)Math.Ceiling((float)tempResponse.TotalItems / 40.0);
                request.OrderBy = VolumesResource.ListRequest.OrderByEnum.Relevance;
                request.MaxResults = searched;
                request.StartIndex = (currentPage - 1) * searched;
                request.PrintType = VolumesResource.ListRequest.PrintTypeEnum.BOOKS;
                var response = await request.ExecuteAsync();
                var searchResults = new List<Book>();
                var orderedBooks = response.Items.OrderByDescending(volume => volume.VolumeInfo.RatingsCount);

                for (int bookIndex = 0; bookIndex < PageSize; bookIndex++)
                {
                    var item = orderedBooks.ElementAt(bookIndex);
                    if (item.VolumeInfo != null && item.VolumeInfo.AverageRating > 0)
                    {
                        // foreachloop but with counter var if index = counter break;
                        var searchResult = new Book
                        {
                            Title = item.VolumeInfo.Title,
                            Rating = item.VolumeInfo.AverageRating ?? 0.0,
                            Author = item.VolumeInfo.Authors?.FirstOrDefault() ?? "None",
                            ID = item.Id,
                            Img = item.VolumeInfo.ImageLinks?.Thumbnail ?? "Image Unavailable",
                            Description = item.VolumeInfo.Description ?? "N/A",
                            Preview = item.VolumeInfo.PreviewLink ?? "N/A"
                        };

                        searchResults.Add(searchResult);
                    }
                }

                for (int bookIndex = 0; bookIndex < searchResults.Count(); bookIndex++)
                {
                    var request2 = service.Volumes.Get(searchResults[bookIndex].ID);
                    var volume2 = request2.Execute();

                    if (!string.IsNullOrEmpty(volume2.VolumeInfo?.ImageLinks?.Large ?? null))
                    {
                        searchResults[bookIndex].Img = volume2.VolumeInfo.ImageLinks.Large;
                    }
                }

                List<string> bookIDs;

                var uID = User.Identity.GetUserId();
                using (var dbContext = new UBITableContext())
                {
                    bookIDs = dbContext.UBITs
                        .Where(table => table.userID == uID)
                        .Select(table => table.bookID)
                        .ToList();
                }

                model.Query = query;
                model.SearchResults = searchResults;
                model.CurrentPage = currentPage;
                model.userList = bookIDs;
                model.TotalPages = Totalpages;

                return View(model);
            }
            catch (Exception ex)
            {
                string errorMessage = HttpUtility.UrlEncode(ex.Message);
                return RedirectToAction("Error", "Books", new { error = errorMessage });
            }

            if (string.IsNullOrEmpty(model.Query))
            {
                return RedirectToAction("Error", "Books", new { error = "API failed to retrieve the proper data" });
            }
            else
            {
                return View();
            }
        }



        public ActionResult addBook(string bID, List<string> userList)
        {
            if (userList == null)
            {
                userList = new List<string>();
            }

            if (!userList.Contains(bID))
            {
                string uID = User.Identity.GetUserId();
                using (var dbContext = new UBITableContext())
                {
                    var table = new UBITable
                    {
                        userID = uID,
                        bookID = bID
                    };

                    dbContext.UBITs.Add(table);
                    dbContext.SaveChanges();
                }
            }
            return Content(string.Empty);
        }

        public ActionResult displayList(int? Page)
        {
            List<string> userList = new List<string>();

            var uID = User.Identity.GetUserId();
            using (var dbContext = new UBITableContext())
            {
                userList = dbContext.UBITs
                    .Where(table => table.userID == uID)
                    .Select(table => table.bookID)
                    .ToList();
            }

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { error = "Please log in first" });
            }

            if (userList?.Count == 0)
            {
                return RedirectToAction("Index", "Home", new { error = "Add some books to your cart" });
            }
            int PageSize = 12;
            int currentPage = Page.HasValue && Page > 0 ? Page.Value : 1;
            int startIndex = (currentPage - 1) * PageSize;


            string apiKey = ConfigurationManager.AppSettings["GoogleBooksAPIKey"];

            var service = new BooksService(new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = "FinalBookProj"
            });

            List<Book> books = new List<Book>();


            for (int bookIndex = startIndex; bookIndex < Math.Min(PageSize * currentPage, userList.Count); bookIndex++)
            {
                var request = service.Volumes.Get(userList[bookIndex]);
                var volume = request.Execute();

                Book book = new Book
                {
                    Title = volume.VolumeInfo?.Title ?? "N/A",
                    Rating = volume.VolumeInfo?.AverageRating ?? 0.0,
                    Author = volume.VolumeInfo.Authors?[0] ?? "None",
                    pageCount = volume.VolumeInfo?.PageCount ?? 0,
                    Img = volume.VolumeInfo?.ImageLinks?.Large ?? "Image Unavailable",
                    Description = volume.VolumeInfo?.Description ?? "N/A",
                    ID = volume.Id,
                    Preview = volume.VolumeInfo.PreviewLink ?? "N/A"
                };

                if (book.Img == "Image Unavailable")
                {
                    book.Img = volume.VolumeInfo?.ImageLinks?.Thumbnail ?? "Image Unavailable";
                }

                books.Add(book);
            }




            SearchViewModel model = new SearchViewModel
            {
                SearchResults = books.OrderByDescending(b => b.Rating).ToList(),
                userList = userList,
                CurrentPage = currentPage
            };

            return View(model);
        }
        public ActionResult Remove(string bookID, int page)
        {
            var uID = User.Identity.GetUserId();
            using (var dbContext = new UBITableContext())
            {
                var recordToDelete = dbContext.UBITs
                    .FirstOrDefault(table => table.userID == uID && table.bookID == bookID);

                if (recordToDelete != null)
                {
                    dbContext.UBITs.Remove(recordToDelete);
                    dbContext.SaveChanges();
                }
            }

            return RedirectToAction("displayList", "Books", new { Page = page });
        }

        public ActionResult LoggedIn(string error)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { error = "Please log in first" });
            }

            ViewBag.Error = error;

            List<string> bookIDs;

            var uID = User.Identity.GetUserId();
            using (var dbContext = new UBITableContext())
            {
                bookIDs = dbContext.UBITs
                    .Where(table => table.userID == uID)
                    .Select(table => table.bookID)
                    .ToList();
            }

            SearchViewModel model = new SearchViewModel
            {
                userList = bookIDs,
            };

            return View(model);
        }

        public ActionResult Error(string error)
        {
            ViewBag.Error = error;
            return View();
        }
    }
}